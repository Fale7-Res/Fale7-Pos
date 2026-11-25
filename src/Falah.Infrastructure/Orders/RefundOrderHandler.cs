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
        // QA-03 (audit 2026-11-23): an empty id is no replay key; refused before the lookup.
        InputRules.AssertRequestId(request.RefundRequestId);
        var refundKey = $"refund-{request.RefundRequestId}";

        var existing = await FindExistingAsync(refundKey, ct);
        if (existing is not null) return await ToResultAsync(existing, wasReplayed: true, ct);

        // QA-07 / QA-09 (audit 2026-11-23): the till always required a reason and a positive amount; the
        // server now does too, with the right message (a zero amount used to read "nothing left to refund").
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب الاسترداد مطلوب.");
        InputRules.AssertMaxLength(request.Reason.Trim(), InputRules.MaxReasonLength, "سبب الاسترداد");
        if (request.AmountPiastres <= 0) throw new DomainException("مبلغ الاسترداد يجب أن يكون أكبر من صفر.");

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
            // Owner decision 2026-11-24: a cash delivery is refundable once its driver settled it, up to what the driver
            // actually collected - an approved collection shortfall no longer makes the order unrefundable for good,
            // and nothing that never came in can go back out.
            long? collectedCap = null;
            if (CheckoutRules.IsCashOnDelivery(order.OrderType, order.PaymentMethod, order.Total))
                collectedCap = await _db.DriverCustodies.AsNoTracking()
                    .Where(c => c.OrderId == order.Id && c.Status == nameof(Falah.Domain.Delivery.CustodyStatus.Settled))
                    .MaxAsync(c => (long?)c.CollectedPiastres, ct);
            var cashOnDeliverySettled = collectedCap is > 0;
            order.AssertCollectedForRefund(cashOnDeliverySettled);
            var refundable = RefundRules.RefundableRemaining(order.Total, order.RefundAmount);
            if (collectedCap is { } collected)
                refundable = Money.FromPiastres(Math.Min(refundable.Piastres, Math.Max(0, collected - order.RefundAmount.Piastres)));
            // Matches the current app's behaviour exactly: a request for more than what remains is
            // CLAMPED down to what remains, not rejected outright - see RefundRules.ClampRefundAmount.
            var safeAmount = RefundRules.ClampRefundAmount(Money.FromPiastres(request.AmountPiastres), refundable);
            if (!safeAmount.IsPositive) throw new DomainException("لا يوجد مبلغ متبقٍ للاسترجاع.");

            // Owner, 2026-11-18: a large refund needs a manager even from an orders_refund holder. A holder
            // passed AuthorizeCommand above without spending anything, so the approval is spent here; one
            // who lacks the permission has already spent theirs there.
            // A manager refunding themselves is the person the rule asks for (they could issue the
            // approval to their own session with their PIN), so no second manager is fetched.
            if (RefundRules.NeedsManagerApproval(safeAmount) && authorization.Approver is null
                && !AuthorizationPolicy.CanIssueApproval(cashier, "orders_refund"))
            {
                if (approval is null) throw new DomainException(RefundRules.LargeRefundNeedsManagerMessage);
                authorization = authorization with { Approver = AuthorizationPolicy.ValidateApproval(_approvalRegistry, "orders_refund", approval, cashier.Id) };
                authorizedByName = authorization.Approver?.ApproverName ?? cashier.Name;
            }

            // Ported from postRefund in lib/enterprise-store.tsx: a refund can only leave a
            // protected account (drawer/main/bank/instapay - never wallet) if it actually holds
            // enough posted balance. A real gap found during the legacy-parity audit: every other
            // money-out handler in this codebase (expenses, purchasing, treasury, shift-open) calls
            // this same guard; RefundOrderHandler alone skipped it, so a refund could silently drive
            // an account negative.
            // FIN-0005 (owner decision 2026-11-22): a split-paid order goes back through the ONE method
            // chosen, which must be one it was paid with (Order.ApplyRefund checks that too); any other
            // order through its own method. Everything below - account, shift rule, movement - uses it.
            var refundMethod = order.PaymentMethod;
            if (order.PaymentMethod == PaymentMethod.Multi)
            {
                if (string.IsNullOrWhiteSpace(request.RefundMethod))
                    throw new DomainException("الطلب ده اتدفع بأكتر من طريقة؛ اختار الطريقة اللي الفلوس هترجع بيها.");
                refundMethod = OrderEnumMapping.ParsePaymentMethod(request.RefundMethod.Trim());
                if (refundMethod == PaymentMethod.Multi || order.SplitPayments?.Any(s => s.Method == refundMethod) != true)
                    throw new DomainException("اختار طريقة استرجاع من الطرق اللي اتدفع بيها الطلب.");
            }
            var refundSourceAccount = TreasuryMovementFactory.PaymentAccountFor(refundMethod);
            var refundSourceMovements = await TreasuryBalanceQuery.LockAndReadBalanceAsync(_db, refundSourceAccount, ct);
            FinancialRules.AssertProtectedSourceBalance(refundSourceMovements, refundSourceAccount, safeAmount);

            var originalShiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == order.ShiftId, ct)
                ?? throw new InvalidOperationException("وردية الطلب الأصلية غير موجودة.");
            var activeShiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == nameof(ShiftStatus.Open), ct);

            var shiftPolicy = RefundRules.ResolveOperationalShift(
                order.ShiftId, Enum.Parse<ShiftStatus>(originalShiftEntity.Status), activeShiftEntity?.Id, refundMethod);
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
                refundMethod, safeAmount, businessDayId, operationalShiftId,
                cashierUser.Name, authorizedByName, request.Reason, now);

            var refund = new OrderRefund($"order-refund-{Guid.NewGuid():N}", movement.OperationId, refundKey,
                safeAmount, refundMethod, movement.Id, now, cashierUser.Name, authorizedByName,
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
