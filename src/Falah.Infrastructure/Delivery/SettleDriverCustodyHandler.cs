namespace Falah.Infrastructure.Delivery;

using System.Text.Json;
using Falah.Application.Delivery;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Domain.Financial;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Ported from settleDriverCustody - driver-centric, settles ALL of a driver's currently unsettled
/// custodies and resolved deliveries in one pass, matching how dispatch actually works now (one order
/// assigned at a time, not a manually-batched multi-order trip). Full settlement only: refuses if any
/// of the driver's orders is still out_for_delivery - there is deliberately no partial-settlement
/// path, exactly matching BuildDriverSettlementScope's own tested contract.
/// </summary>
public sealed class SettleDriverCustodyHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public SettleDriverCustodyHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<SettleDriverCustodyResult> HandleAsync(SettleDriverCustodyRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = $"driver-settlement-{request.RequestId}";
        var existing = await _db.DriverSettlements.AsNoTracking().FirstOrDefaultAsync(s => s.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        if (request.ActualCashPiastres < 0) throw new DomainException("النقدية الفعلية يجب أن تكون عدد قروش صحيحًا وغير سالب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? baseApproval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            baseApproval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "delivery_manage", request.Reason);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "delivery_manage", baseApproval, requireApproval: true);
        var approvedByName = authorization.Approver?.ApproverName ?? actorUser.Name;

        var driverEntity = await _db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DriverId, ct);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await Falah.Infrastructure.BusinessDay.FinancialAdmission.AcquireAsync(_db, ct);
            var admissionReplay = await _db.DriverSettlements.AsNoTracking().FirstOrDefaultAsync(s => s.IdempotencyKey == idempotencyKey, ct);
            if (admissionReplay is not null) return ToResult(admissionReplay, wasReplayed: true);
            var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
            // The driver settles with the cashier who RECEIVES his money (owner, 2026-11-08): the cash goes into the
            // drawer of the shift that is open now - only one can be - and is that shift's to count and hand over.
            var receivingShift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == nameof(Falah.Domain.Shifts.ShiftStatus.Open), ct)
                ?? throw new DomainException("لا توجد وردية مفتوحة لاستلام نقدية السائق. افتح وردية أولًا ثم أعد التسوية.");
            // One settlement per driver at a time. The row locks below cover his deliveries and custodies, but a
            // settlement of nothing but a carried balance has no such rows, and two of those must not both read
            // the same balance. The same request retried while the first held the lock replays here.
            await AcquireDriverLockAsync(request.DriverId, ct);
            var lockReplay = await _db.DriverSettlements.AsNoTracking().FirstOrDefaultAsync(s => s.IdempotencyKey == idempotencyKey, ct);
            if (lockReplay is not null) return ToResult(lockReplay, wasReplayed: true);
            // Two concurrent settlement requests for the SAME driver (different RequestIds, so the
            // idempotency-key check above cannot catch this) must not both read the same unsettled
            // custodies/deliveries and both post a movement. UPDLOCK on the full row-set - not just
            // one row, since a driver can have many - makes the second request block here until the
            // first fully commits, then see nothing left to settle instead of double-posting.
            var deliveryEntities = await _db.Deliveries
                .FromSqlRaw("SELECT * FROM Deliveries WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE DriverId = {0} AND Status NOT IN ({1}, {2})",
                    request.DriverId, nameof(DeliveryStatus.Settled), nameof(DeliveryStatus.Cancelled))
                .ToListAsync(ct);
            var orderIds = deliveryEntities.Select(d => d.OrderId).ToList();
            var orders = orderIds.Count == 0 ? new List<OrderEntity>() : await _db.Orders.AsNoTracking().Where(o => orderIds.Contains(o.Id)).ToListAsync(ct);
            var ordersById = orders.ToDictionary(o => o.Id);
            var facts = deliveryEntities.Select(d => DeliveryFactsComposer.Compose(d, ordersById[d.OrderId])).ToList();

            var custodyEntities = await _db.DriverCustodies
                .FromSqlRaw("SELECT * FROM DriverCustodies WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE DriverId = {0} AND Status != {1}",
                    request.DriverId, nameof(CustodyStatus.Settled))
                .ToListAsync(ct);
            var custodies = custodyEntities.Select(DeliveryFactsComposer.ToDriverCustody).ToList();

            var scope = DeliveryRules.BuildDriverSettlementScope(request.DriverId, facts, custodies);
            var previous = await _db.DriverSettlements.AsNoTracking().Where(s => s.DriverId == request.DriverId)
                .OrderByDescending(s => s.CreatedAtUtc).ThenByDescending(s => s.Id).FirstOrDefaultAsync(ct);
            var carriedIn = Money.FromPiastres(previous?.CarriedOutPiastres ?? 0);
            if (custodies.Count == 0 && scope.ResolvedDeliveries.Count == 0 && carriedIn.IsZero)
                throw new DomainException("لا توجد طلبات مكتملة أو عهدة غير مسوّاة لهذا السائق.");
            if (scope.ActiveDeliveries.Count > 0)
                throw new DomainException("يجب تسجيل نتيجة كل الطلبات (تم/فشل/مرتجع) قبل تسوية هذا السائق.");

            var actual = Money.FromPiastres(request.ActualCashPiastres);
            var paidToDriver = Money.FromPiastres(request.PaidToDriverPiastres);
            var collected = Money.FromPiastres(scope.UnsettledCustodies.Sum(c => c.Collected.Piastres));
            var fees = DriverSettlementRules.FeeEntitlement(scope.ResolvedDeliveries);
            var figures = DriverSettlementRules.Calculate(carriedIn, collected, fees, actual, paidToDriver, request.KeepRemainderOpen);
            var totals = new DeliveryRules.TripSettlementResult(figures.Due, actual, figures.Difference);

            if (!totals.Difference.IsZero)
            {
                if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("فرق تسوية السائق يتطلب سببًا.");
                // baseApproval's own .Action field is a fixed local literal ("delivery_manage") set
                // above purely so it can satisfy that first, base gate - it is NOT the real action the
                // approval was issued for. AuthorizeCommand looks that real action up itself from the
                // registry by ApprovalId, so the same token is passed through here unfiltered; it will
                // only actually authorize this stricter gate if it was genuinely issued for
                // delivery_settlement_approve.
                AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "delivery_settlement_approve", baseApproval, requireApproval: true);
            }


            var journal = new EfFinancialJournal(_db);
            string? movementId = null;
            if (actual.IsPositive)
            {
                if (!journal.TryStart(idempotencyKey, "driver_settlement"))
                    throw new DeliveryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

                var now0 = DateTimeOffset.UtcNow;
                var reasonSuffix = string.IsNullOrWhiteSpace(request.Reason) ? "" : $" - {request.Reason}";
                // Into the receiving shift's drawer, attributed to that shift: it is counted at that shift's blind
                // close and leaves the drawer with its handover. It is not a sale - the order was the sale.
                var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                    $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", "driver_settlement",
                    sourceAccountId: null, destinationAccountId: TreasuryAccounts.Drawer, actual,
                    "delivery_trip", request.DriverId, businessDayId, receivingShift.Id,
                    $"توريد السائق {driverEntity?.Name ?? request.DriverId}{reasonSuffix}", actorUser.Name, approvedByName, now0);
                _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));
                journal.Complete(idempotencyKey);
                movementId = movement.Id;
            }

            // The restaurant owed the driver (fees on prepaid deliveries exceed the cash he collected): an explicit
            // payout from the same drawer, under the same authority, checked against what the drawer really holds.
            string? payoutMovementId = null;
            if (paidToDriver.IsPositive)
            {
                var payoutKey = $"{idempotencyKey}-payout";
                var drawerMovements = await Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, TreasuryAccounts.Drawer, ct);
                FinancialRules.AssertProtectedSourceBalance(drawerMovements, TreasuryAccounts.Drawer, paidToDriver);
                if (!journal.TryStart(payoutKey, "driver_fee_payout"))
                    throw new DeliveryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");
                var payout = TreasuryMovementFactory.BuildCashLifecycleMovement(
                    $"tm-{Guid.NewGuid():N}", payoutKey, $"op-{Guid.NewGuid():N}", "driver_fee_payout",
                    sourceAccountId: TreasuryAccounts.Drawer, destinationAccountId: null, paidToDriver,
                    "delivery_trip", request.DriverId, businessDayId, receivingShift.Id,
                    $"صرف مستحقات توصيل للسائق {driverEntity?.Name ?? request.DriverId}", actorUser.Name, approvedByName, DateTimeOffset.UtcNow);
                _db.TreasuryMovements.Add(OrderMapper.ToEntity(payout));
                journal.Complete(payoutKey);
                payoutMovementId = payout.Id;
            }

            var now = DateTimeOffset.UtcNow;
            var custodyIds = scope.UnsettledCustodies.Select(c => c.Id).ToHashSet();
            var resolvedDeliveryIds = scope.ResolvedDeliveries.Select(d => d.Id).ToHashSet();

            foreach (var custodyEntity in custodyEntities.Where(c => custodyIds.Contains(c.Id)))
            {
                custodyEntity.Status = nameof(CustodyStatus.Settled);
                custodyEntity.SettledPiastres = custodyEntity.CollectedPiastres;
                custodyEntity.SettledAtUtc = now;
            }
            foreach (var deliveryEntity in deliveryEntities.Where(d => resolvedDeliveryIds.Contains(d.Id)))
            {
                // Already tracked from the UPDLOCK fetch above - no second round trip needed.
                deliveryEntity.Status = nameof(DeliveryStatus.Settled);
                deliveryEntity.SettledAtUtc = now;
                deliveryEntity.Version++;
            }

            var settlementEventType = totals.Difference.IsNegative ? "settlement_shortage" : totals.Difference.IsPositive ? "settlement_overage" : "settlement";
            foreach (var d in scope.ResolvedDeliveries)
            {
                _db.DeliveryEvents.Add(new DeliveryEventEntity
                {
                    Id = $"delivery-event-{Guid.NewGuid():N}", DeliveryId = d.Id, OrderId = d.OrderId, DriverId = request.DriverId,
                    Type = settlementEventType, Reason = request.Reason, Actor = actorUser.Name, Approver = approvedByName,
                    OperationId = idempotencyKey, CreatedAtUtc = now,
                });
            }

            var settlementEntity = new DriverSettlementEntity
            {
                Id = $"driver-settlement-{request.RequestId}", DriverId = request.DriverId, DriverName = driverEntity?.Name,
                OrderIdsJson = JsonSerializer.Serialize(scope.OrderIds), OrderCount = scope.OrderIds.Count,
                GrossOrderPiastres = scope.GrossOrderValue.Piastres, PrepaidPiastres = scope.PrepaidValue.Piastres,
                ExpectedPiastres = totals.Expected.Piastres, ActualPiastres = totals.Actual.Piastres, DifferencePiastres = totals.Difference.Piastres,
                Reason = request.Reason, ApprovedBy = approvedByName, OperationId = idempotencyKey, IdempotencyKey = idempotencyKey,
                TreasuryMovementId = movementId, ReceivedBy = actorUser.Name, CreatedAtUtc = now,
                ShiftId = receivingShift.Id, ReceivedByUserId = actorUser.Id,
                CarriedInPiastres = figures.CarriedIn.Piastres, CashCollectedPiastres = figures.CashCollected.Piastres,
                FeeEntitlementPiastres = figures.FeeEntitlement.Piastres, PaidToDriverPiastres = figures.PaidToDriver.Piastres,
                CarriedOutPiastres = figures.CarriedOut.Piastres, KeepsBalanceOpen = request.KeepRemainderOpen,
                PayoutTreasuryMovementId = payoutMovementId,
            };
            _db.DriverSettlements.Add(settlementEntity);

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "تسوية عهدة سائق", "cash",
                $"تسوية السائق {driverEntity?.Name ?? request.DriverId} - محصّل {figures.CashCollected.Piastres} - رسوم توصيل {figures.FeeEntitlement.Piastres}"
                + $" - رصيد سابق {figures.CarriedIn.Piastres} - المستحق {figures.Due.Piastres} - المستلم {figures.Received.Piastres}"
                + $" - المصروف للسائق {figures.PaidToDriver.Piastres} - المرحّل {figures.CarriedOut.Piastres}",
                totals.Difference.IsZero ? "normal" : "critical", now, Reason: request.Reason, AuthorizedBy: approvedByName,
                OperationId: idempotencyKey, ReferenceId: request.DriverId));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToResult(settlementEntity, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or DeliveryConflictException
                                   || Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
        {
            await transaction.RollbackAsync(ct);
            var replay = await _db.DriverSettlements.AsNoTracking().FirstOrDefaultAsync(s => s.IdempotencyKey == idempotencyKey, ct);
            // The loser of a balance-lock race carries the same request id as the sibling that
            // won it, and that sibling may not have committed yet at this instant. Looking once
            // and giving up turns "somebody else is doing it" into an error.
            if (replay is null && Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
                replay = await Falah.Infrastructure.Treasury.BalanceLockContention.AwaitSiblingAsync(
                    async () => await _db.DriverSettlements.AsNoTracking().FirstOrDefaultAsync(s => s.IdempotencyKey == idempotencyKey, ct), ct);
            if (replay is not null) return ToResult(replay, wasReplayed: true);
            // Losing the balance-lock race wrote nothing at all, so telling the operator to go and
            // review a half-finished movement would send them looking for something that is not there.
            if (Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
                throw new DeliveryConflictException(Falah.Infrastructure.Treasury.BalanceLockContention.RetryMessage);
            throw new DeliveryConflictException("تعذر تسوية عهدة السائق بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
    }

    private static SettleDriverCustodyResult ToResult(DriverSettlementEntity e, bool wasReplayed) =>
        new(e.Id, e.DriverId, e.ExpectedPiastres, e.ActualPiastres, e.DifferencePiastres, e.OrderCount, e.TreasuryMovementId, wasReplayed,
            e.CarriedInPiastres, e.CashCollectedPiastres, e.FeeEntitlementPiastres, e.PaidToDriverPiastres, e.CarriedOutPiastres,
            e.ShiftId, e.PayoutTreasuryMovementId);

    private async Task AcquireDriverLockAsync(string driverId, CancellationToken ct)
    {
        var result = new Microsoft.Data.SqlClient.SqlParameter("@driverLock", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
        var resource = new Microsoft.Data.SqlClient.SqlParameter("@driverLockResource", System.Data.SqlDbType.NVarChar, 255) { Value = $"FalahDriverSettlement:{driverId}" };
        await _db.Database.ExecuteSqlRawAsync(
            "EXEC @driverLock = sys.sp_getapplock @Resource=@driverLockResource, @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=15000",
            new object[] { result, resource }, ct);
        if (result.Value is not int code || code < 0)
            throw new DeliveryConflictException(Falah.Infrastructure.Treasury.BalanceLockContention.RetryMessage);
    }
}
