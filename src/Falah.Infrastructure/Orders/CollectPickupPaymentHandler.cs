namespace Falah.Infrastructure.Orders;

using Falah.Application.Orders;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Shifts;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// UI-0025 (owner decision 2026-11-22): the customer collects a pickup that was registered unpaid and
/// pays for it now, by any method. This is the moment it becomes a sale: the order's sale instant and
/// shift move to now and to the shift taking the money (the registration is kept in RegisteredAtUtc /
/// RegisteredShiftId), and the money posts exactly as a checkout's would - cash and card now, a transfer
/// as a claim awaiting confirmation. Until then the order was in no sales figure, so nothing already
/// reported moves. Replays by RequestId before anything is validated.
/// </summary>
public sealed class CollectPickupPaymentHandler
{
    private readonly FalahDbContext _db;
    private readonly IAuditLog _auditLog;

    public CollectPickupPaymentHandler(FalahDbContext db, IAuditLog auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<CheckoutOrderResult> HandleAsync(CollectPickupPaymentRequest request, CancellationToken ct = default)
    {
        InputRules.AssertRequestId(request.RequestId);   // QA-03 (audit 2026-11-23): before the replay lookup
        if (await FindPaidByAsync(request.RequestId, ct) is { } replay) return ToResult(replay, wasReplayed: true);
        var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
            ?? throw new InvalidOperationException("الكاشير غير موجود.");
        if (!AuthorizationPolicy.HasPermission(OrderMapper.ToActor(cashierUser), "pos"))
            throw new DomainException("ليس لديك صلاحية تحصيل طلب.");

        var paymentMethod = OrderEnumMapping.ParsePaymentMethod(request.PaymentMethod);
        var splits = request.SplitPayments?
            .Select(s => new PaymentSplit(OrderEnumMapping.ParsePaymentMethod(s.Method), Money.FromPiastres(s.AmountPiastres)))
            .ToList();

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await Falah.Infrastructure.BusinessDay.FinancialAdmission.AcquireAsync(_db, ct);
            if (await FindPaidByAsync(request.RequestId, ct) is { } admissionReplay) return ToResult(admissionReplay, wasReplayed: true);
            var businessDayId = await Falah.Infrastructure.BusinessDay.BusinessDayGate.RequireOpenAsync(_db, ct);
            var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct);
            if (shift?.Status != nameof(ShiftStatus.Open))
                throw new ShiftNotOpenException("يجب فتح وردية قبل تحصيل أي طلب.");

            var order = await _db.Orders
                .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
                .FirstOrDefaultAsync(ct)
                ?? throw new DomainException("الطلب غير موجود.");
            if (order.Status == OrderStatus.Cancelled.ToWireString()) throw new DomainException("الطلب ده اتلغى.");
            if (!order.AwaitingCollection) throw new DomainException("الطلب ده مش مستني دفع عند الاستلام - غالبًا اتدفع قبل كده.");

            var total = Money.FromPiastres(order.TotalPiastres);
            var plan = CheckoutRules.BuildCollectionPlan(total, paymentMethod, Money.FromPiastres(request.AmountPaidPiastres), splits, request.WalletProvider);
            var now = DateTimeOffset.UtcNow;
            var saleKey = $"sale-collect-{request.RequestId}";
            if (!plan.DefersAllMoney && !new EfFinancialJournal(_db).TryStart(saleKey, "sale"))
                throw new CheckoutConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            order.RegisteredAtUtc = order.CreatedAtUtc;
            order.RegisteredShiftId = order.ShiftId;
            order.CreatedAtUtc = now;
            order.ShiftId = request.ShiftId;
            order.PaymentMethod = plan.PaymentMethod.ToWireString();
            order.SplitPaymentsJson = OrderMapper.SerializeSplits(plan.SplitPayments);
            order.AmountPaidPiastres = plan.AmountPaid.Piastres;
            order.ChangeDuePiastres = plan.ChangeDue.Piastres;
            order.IsPaid = plan.IsPaid;
            order.ElectronicPaymentMethod = plan.ElectronicLeg?.Method.ToWireString();
            order.ElectronicPaymentProvider = plan.ElectronicLeg?.Provider;
            order.ElectronicPaymentAmountPiastres = plan.ElectronicLeg?.Amount.Piastres;
            order.ElectronicPaymentStatus = plan.ElectronicLeg?.Status.ToWireString();
            order.AwaitingCollection = false;
            order.CollectionRequestId = request.RequestId;
            order.CollectedBy = cashierUser.Name;

            if (!plan.DefersAllMoney)
            {
                if (plan.PaymentMethod == PaymentMethod.Multi && plan.SplitPayments is { } legs)
                {
                    var idx = 0;
                    foreach (var leg in legs)
                    {
                        if (leg.Method is PaymentMethod.InstaPay or PaymentMethod.Wallet) { idx++; continue; }
                        _db.TreasuryMovements.Add(OrderMapper.ToEntity(TreasuryMovementFactory.BuildSaleMovement($"tm-{Guid.NewGuid():N}",
                            $"op-{Guid.NewGuid():N}", $"collect-{request.RequestId}-{idx}", order.Id,
                            leg.Method, leg.Amount, businessDayId, request.ShiftId, cashierUser.Name, now)));
                        idx++;
                    }
                }
                else
                {
                    _db.TreasuryMovements.Add(OrderMapper.ToEntity(TreasuryMovementFactory.BuildSaleMovement($"tm-{Guid.NewGuid():N}",
                        $"op-{Guid.NewGuid():N}", $"collect-{request.RequestId}", order.Id,
                        plan.PaymentMethod, total, businessDayId, request.ShiftId, cashierUser.Name, now)));
                }
                new EfFinancialJournal(_db).Complete(saleKey);
            }

            _auditLog.Append(new AuditEvent(
                $"audit-collect-{order.Id}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
                $"تحصيل طلب استلام {order.OrderNumber}", "order",
                $"العميل دفع {order.TotalPiastres} قرش ({order.PaymentMethod}) وهو بيستلم", "normal", now,
                ShiftId: request.ShiftId, ReferenceId: order.Id));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToResult(order, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or CheckoutConflictException)
        {
            await transaction.RollbackAsync(ct);
            if (await FindPaidByAsync(request.RequestId, ct) is { } conflictReplay) return ToResult(conflictReplay, wasReplayed: true);
            throw new CheckoutConflictException("الطلب ده اتحصّل من جهاز تاني في نفس اللحظة. افتح الطلب تاني وشوف حالته.");
        }
    }

    private Task<OrderEntity?> FindPaidByAsync(string requestId, CancellationToken ct) =>
        _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.CollectionRequestId == requestId, ct);

    private static CheckoutOrderResult ToResult(OrderEntity entity, bool wasReplayed) => new(
        entity.Id, entity.OrderNumber, entity.SequenceNumber, entity.TotalPiastres, entity.AmountPaidPiastres,
        entity.ChangeDuePiastres, entity.Status, entity.IsPaid, entity.ElectronicPaymentStatus, wasReplayed);
}
