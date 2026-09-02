namespace Falah.Infrastructure.Orders;

using System.Data;
using Falah.Application.Orders;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Shifts;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
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

    public RefundOrderHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
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
            var orderEntity = await _db.Orders
                .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
                .Include(o => o.Items).Include(o => o.Refunds)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("الطلب غير موجود.");

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
            var refundable = RefundRules.RefundableRemaining(order.Total, order.RefundAmount);
            // Matches the current app's behaviour exactly: a request for more than what remains is
            // CLAMPED down to what remains, not rejected outright - see RefundRules.ClampRefundAmount.
            var safeAmount = RefundRules.ClampRefundAmount(Money.FromPiastres(request.AmountPiastres), refundable);
            if (!safeAmount.IsPositive) throw new DomainException("لا يوجد مبلغ متبقٍ للاسترجاع.");

            var originalShiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == order.ShiftId, ct)
                ?? throw new InvalidOperationException("وردية الطلب الأصلية غير موجودة.");
            var activeShiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == nameof(ShiftStatus.Open), ct);

            var shiftPolicy = RefundRules.ResolveOperationalShift(
                order.ShiftId, Enum.Parse<ShiftStatus>(originalShiftEntity.Status), activeShiftEntity?.Id, order.PaymentMethod);

            var now = DateTimeOffset.UtcNow;
            var movement = TreasuryMovementFactory.BuildRefundMovement(
                $"tm-{Guid.NewGuid():N}", $"op-{Guid.NewGuid():N}", request.RefundRequestId, order.Id,
                order.PaymentMethod, safeAmount, order.BusinessDayId, shiftPolicy.OperationalShiftId!,
                cashierUser.Name, authorizedByName, request.Reason, now);

            var refund = new OrderRefund($"order-refund-{Guid.NewGuid():N}", movement.OperationId, refundKey,
                safeAmount, order.PaymentMethod, movement.Id, now, cashierUser.Name, authorizedByName,
                request.Reason, order.ShiftId, shiftPolicy.OperationalShiftId!);

            // Domain invariants enforced here too (cancelled/multi guard, amount<=refundable,
            // Refunded transition) - not just trusted from the computation above.
            order.ApplyRefund(refund);

            orderEntity.Status = order.Status.ToWireString();
            orderEntity.RefundAmountPiastres = order.RefundAmount.Piastres;
            orderEntity.Version++;
            var refundEntity = OrderMapper.ToEntity(refund);
            orderEntity.Refunds.Add(refundEntity);
            _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            // Use the entity's own (now database-assigned) identity for the result - the SAME
            // identifier shape a later replay lookup returns, so a caller can never see two
            // different id formats for what is logically the same refund.
            return ToResult(refundEntity, orderEntity, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await FindExistingAsync(refundKey, ct);
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
