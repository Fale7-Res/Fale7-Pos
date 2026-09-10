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
            if (custodies.Count == 0 && scope.ResolvedDeliveries.Count == 0)
                throw new DomainException("لا توجد طلبات مكتملة أو عهدة غير مسوّاة لهذا السائق.");
            if (scope.ActiveDeliveries.Count > 0)
                throw new DomainException("يجب تسجيل نتيجة كل الطلبات (تم/فشل/مرتجع) قبل تسوية هذا السائق.");

            var actual = Money.FromPiastres(request.ActualCashPiastres);
            var totals = DeliveryRules.CalculateTripSettlement(scope.UnsettledCustodies, actual);

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

            var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);

            var journal = new EfFinancialJournal(_db);
            string? movementId = null;
            if (actual.IsPositive)
            {
                if (!journal.TryStart(idempotencyKey, "driver_settlement"))
                    throw new DeliveryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

                var now0 = DateTimeOffset.UtcNow;
                var reasonSuffix = string.IsNullOrWhiteSpace(request.Reason) ? "" : $" - {request.Reason}";
                var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                    $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", "driver_settlement",
                    sourceAccountId: null, destinationAccountId: TreasuryAccounts.Main, actual,
                    "delivery_trip", request.DriverId, businessDayId, shiftId: null,
                    $"تسوية عهدة السائق {request.DriverId}{reasonSuffix}", actorUser.Name, approvedByName, now0);
                _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));
                journal.Complete(idempotencyKey);
                movementId = movement.Id;
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
            };
            _db.DriverSettlements.Add(settlementEntity);

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "تسوية عهدة سائق", "cash", $"تسوية السائق {driverEntity?.Name ?? request.DriverId} - المتوقع {totals.Expected.Piastres} والفعلي {totals.Actual.Piastres}",
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
        new(e.Id, e.DriverId, e.ExpectedPiastres, e.ActualPiastres, e.DifferencePiastres, e.OrderCount, e.TreasuryMovementId, wasReplayed);
}
