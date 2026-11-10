namespace Falah.Infrastructure.Orders;

using System.Data;
using Falah.Application.Orders;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.ControlCenter;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Shifts;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Treasury;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The Refund use case: an independent, auditable financial event against an already-completed
/// order. Never touches the order's original snapshot fields (items, subtotal, total, timestamps)
/// - only Status and RefundAmount move, and only by appending a new, immutable OrderRefund record
/// alongside a new TreasuryMovement. Old financial history is never edited or deleted.
///
/// Concurrency: the initial read of the order takes an UPDLOCK (plus ROWLOCK/HOLDLOCK) hint - SQL
/// Server's idiomatic "select for update" - so a second concurrent refund against the SAME order
/// blocks right there until the first fully commits or rolls back, instead of both reading the
/// same stale "amount remaining" and racing past each other into an overdraw. Plain
/// IsolationLevel.Serializable was deliberately NOT used here: on SQL Server it takes shared
/// range locks progressively rather than an upfront exclusive lock (unlike SQLite's BEGIN
/// IMMEDIATE), so two concurrent Serializable transactions reading then writing the same row can
/// deadlock on lock conversion instead of simply queuing.
/// </summary>
public sealed class RefundOrderHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public RefundOrderHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<RefundOrderResult> HandleAsync(RefundOrderRequest request, CancellationToken ct = default)
    {
        var refundKey = $"refund-{request.RefundRequestId}";

        var existing = await FindExistingAsync(refundKey, ct);
        if (existing is not null) return await ToResultAsync(existing, wasReplayed: true, ct);

        var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
            ?? throw new InvalidOperationException("الكاشير غير موجود.");
        var cashier = OrderMapper.ToActor(cashierUser);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await Falah.Infrastructure.BusinessDay.FinancialAdmission.AcquireAsync(_db, ct);
            var admissionReplay = await FindExistingAsync(refundKey, ct);
            if (admissionReplay is not null) return await ToResultAsync(admissionReplay, wasReplayed: true, ct);
            var businessDayId = await Falah.Infrastructure.BusinessDay.BusinessDayGate.RequireOpenAsync(_db, ct);
            var orderEntity = await _db.Orders
                .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
                .Include(o => o.Items).Include(o => o.Refunds)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("الطلب غير موجود.");

            // Another call may have committed this request while we waited for the order lock.
            // Replay before checking a spent approval or the now-exhausted refundable amount.
            var completed = await FindExistingAsync(refundKey, ct);
            if (completed is not null) return await ToResultAsync(completed, wasReplayed: true, ct);

            // Refunds always require either the permission or a manager approval - unlike a shift
            // close discrepancy, there is no threshold below which a refund is self-authorized.
            ManagerApproval? approval = null;
            if (!string.IsNullOrEmpty(request.ManagerApprovalId))
            {
                var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                    ?? throw new DomainException("المعتمد غير موجود.");
                approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                    approverUser.Role, DateTimeOffset.UtcNow, "orders_refund", request.Reason);
            }
            var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, cashier, "orders_refund", approval, requireApproval: true);
            var authorizedByName = authorization.Approver?.ApproverName ?? cashier.Name;

            var order = OrderMapper.ToDomain(orderEntity);
            var cashOnDeliverySettled = CheckoutRules.IsCashOnDelivery(order.OrderType, order.PaymentMethod, order.Total)
                && await _db.DriverCustodies.AsNoTracking().AnyAsync(c => c.OrderId == order.Id
                    && c.Status == nameof(Falah.Domain.Delivery.CustodyStatus.Settled) && c.CollectedPiastres >= order.Total.Piastres, ct);
            order.AssertCollectedForRefund(cashOnDeliverySettled);
            var refundable = RefundRules.RefundableRemaining(order.Total, order.RefundAmount);
            // Matches the current app's behaviour exactly: a request for more than what remains is
            // CLAMPED down to what remains, not rejected outright - see RefundRules.ClampRefundAmount.
            var safeAmount = RefundRules.ClampRefundAmount(Money.FromPiastres(request.AmountPiastres), refundable);
            if (!safeAmount.IsPositive) throw new DomainException("لا يوجد مبلغ متبقٍ للاسترجاع.");

            // Ported from postRefund in lib/enterprise-store.tsx: a refund can only leave a
            // protected account (drawer/main/bank/instapay - never wallet) if it actually holds
            // enough posted balance. A real gap found during the legacy-parity audit: every other
            // money-out handler in this codebase (expenses, purchasing, treasury, shift-open) calls
            // this same guard; RefundOrderHandler alone skipped it, so a refund could silently drive
            // an account negative.
            var refundSourceAccount = TreasuryMovementFactory.PaymentAccountFor(order.PaymentMethod);
            var refundSourceMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, refundSourceAccount, ct);
            FinancialRules.AssertProtectedSourceBalance(refundSourceMovements, refundSourceAccount, safeAmount);

            var originalShiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == order.ShiftId, ct)
                ?? throw new InvalidOperationException("وردية الطلب الأصلية غير موجودة.");
            var activeShiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == nameof(ShiftStatus.Open), ct);

            var shiftPolicy = RefundRules.ResolveOperationalShift(
                order.ShiftId, Enum.Parse<ShiftStatus>(originalShiftEntity.Status), activeShiftEntity?.Id, order.PaymentMethod);
            // A refund of a closed shift's card/InstaPay/wallet order is allowed with no shift open
            // (no drawer is touched), but every refund row records the shift that processed it, and
            // there is none. That used to reach the database as a NOT NULL violation and come back as
            // a "needs manual review" conflict for a refund that had simply not happened.
            var operationalShiftId = shiftPolicy.OperationalShiftId
                ?? throw new DomainException("لا توجد وردية مفتوحة لتسجيل المرتجع عليها. افتح وردية ثم أعد المحاولة.");

            // The refund is an event of the business day it is made in, not of the (possibly long
            // closed) day of the sale it reverses: stamping the order's own day put it into a day
            // already closed, counted and handed over. Like every other money-moving command it
            // needs that day to be open.

            var now = DateTimeOffset.UtcNow;
            var movement = TreasuryMovementFactory.BuildRefundMovement(
                $"tm-{Guid.NewGuid():N}", $"op-{Guid.NewGuid():N}", request.RefundRequestId, order.Id,
                order.PaymentMethod, safeAmount, businessDayId, operationalShiftId,
                cashierUser.Name, authorizedByName, request.Reason, now);

            var refund = new OrderRefund($"order-refund-{Guid.NewGuid():N}", movement.OperationId, refundKey,
                safeAmount, order.PaymentMethod, movement.Id, now, cashierUser.Name, authorizedByName,
                request.Reason, order.ShiftId, operationalShiftId);

            // Domain invariants enforced here too (cancelled/multi guard, amount<=refundable,
            // Refunded transition) - not just trusted from the computation above.
            order.ApplyRefund(refund, cashOnDeliverySettled);

            orderEntity.Status = order.Status.ToWireString();
            orderEntity.RefundAmountPiastres = order.RefundAmount.Piastres;
            orderEntity.Version++;
            var refundEntity = OrderMapper.ToEntity(refund);
            orderEntity.Refunds.Add(refundEntity);
            _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
                "استرداد طلب", "order", $"استرداد {safeAmount.Piastres} قرش لطلب {order.OrderNumber} - {request.Reason}",
                safeAmount.Piastres >= ControlCenterRules.LargeRefundThresholdPiastres ? "critical" : "normal", now,
                ShiftId: operationalShiftId, Reason: request.Reason, AuthorizedBy: authorizedByName,
                OperationId: movement.OperationId, ReferenceId: order.Id));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            // Use the entity's own (now database-assigned) identity for the result - the SAME
            // identifier shape a later replay lookup returns, so a caller can never see two
            // different id formats for what is logically the same refund.
            return ToResult(refundEntity, orderEntity, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbUpdateConcurrencyException
                                   || Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
        {
            await transaction.RollbackAsync(ct);
            var replay = await FindExistingAsync(refundKey, ct);
            // The loser of a balance-lock race carries the same request id as the sibling that
            // won it, and that sibling may not have committed yet at this instant. Looking once
            // and giving up turns "somebody else is doing it" into an error.
            if (replay is null && Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
                replay = await Falah.Infrastructure.Treasury.BalanceLockContention.AwaitSiblingAsync(
                    async () => await FindExistingAsync(refundKey, ct), ct);
            if (replay is not null) return await ToResultAsync(replay, wasReplayed: true, ct);
            throw new RefundConflictException(
                "تعذر إتمام عملية الاسترجاع بسبب تعارض في مفتاح منع التكرار، ولا يوجد مرتجع سابق لإعادته - يحتاج مراجعة يدوية.");
        }
    }

    private Task<OrderRefundEntity?> FindExistingAsync(string refundKey, CancellationToken ct) =>
        _db.OrderRefunds.AsNoTracking().FirstOrDefaultAsync(r => r.IdempotencyKey == refundKey, ct);

    private async Task<RefundOrderResult> ToResultAsync(OrderRefundEntity refund, bool wasReplayed, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking().FirstAsync(o => o.Id == refund.OrderId, ct);
        return ToResult(refund, order, wasReplayed);
    }

    private static RefundOrderResult ToResult(OrderRefundEntity refund, OrderEntity order, bool wasReplayed) => new(
        refund.Id.ToString(), order.Id, refund.AmountPiastres, order.Status,
        (order.TotalPiastres - order.RefundAmountPiastres), refund.TreasuryMovementId, wasReplayed);
}
