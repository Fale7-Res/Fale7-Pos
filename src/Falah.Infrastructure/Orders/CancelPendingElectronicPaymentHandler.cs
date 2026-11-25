namespace Falah.Infrastructure.Orders;

using System.Data;
using Falah.Application.Orders;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Treasury;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Wires up Order.CancelElectronicPayment (a fully-ported, tested domain method that had no
/// Application/Infrastructure/endpoint at all until this pass - a real gap found during the
/// legacy-parity audit: a cashier had no way to void a bad InstaPay/wallet claim). Ported from
/// cancelPendingElectronicPayment in lib/store.tsx: 'pos' or 'cash_drawer_sale' permission, reason
/// required, idempotent no-op if already cancelled, blocked if already confirmed (use a refund
/// instead). Unlike legacy, the ingredients checkout took out of stock are accounted for: the
/// operator says whether the food was prepared (recorded as waste) or not (restored to stock) - the
/// owner's decision, applied by OrderStockDisposition.
/// </summary>
public sealed class CancelPendingElectronicPaymentHandler
{
    private readonly FalahDbContext _db;
    private readonly IAuditLog _auditLog;
    private readonly IApprovalRegistry _approvalRegistry;

    public CancelPendingElectronicPaymentHandler(FalahDbContext db, IAuditLog auditLog, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _auditLog = auditLog;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<CancelPendingElectronicPaymentResult> HandleAsync(CancelPendingElectronicPaymentRequest request, CancellationToken ct = default)
    {
        var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
            ?? throw new InvalidOperationException("الكاشير غير موجود.");
        var actor = OrderMapper.ToActor(cashierUser);
        if (!AuthorizationPolicy.HasPermission(actor, "pos") && !AuthorizationPolicy.HasPermission(actor, "cash_drawer_sale"))
            throw new DomainException("ليس لديك صلاحية إلغاء محاولة الدفع.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        await FinancialAdmission.AcquireAsync(_db, ct);

        var orderEntity = await _db.Orders
            .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
            .Include(o => o.Items).Include(o => o.Refunds)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("الطلب غير موجود.");

        if (orderEntity.Status == "cancelled")
        {
            var returnedCash = await _db.TreasuryMovements.AsNoTracking()
                .Where(m => m.ReferenceId == orderEntity.Id && m.Type == "void_return" && m.SourceAccountId == TreasuryAccounts.Drawer)
                .SumAsync(m => (long?)m.AmountPiastres, ct) ?? 0;
            await transaction.CommitAsync(ct);
            return new CancelPendingElectronicPaymentResult(orderEntity.Id, orderEntity.Status, orderEntity.ElectronicPaymentStatus ?? "cancelled", true, returnedCash);
        }
        // Workflow redesign (owner decision 2026-11-24): a delivery or a pickup is cancelled before its kitchen paper
        // printed with no admin code, every ingredient back in stock; after it, only with a manager's approval and the
        // operator's word on whether the food was prepared. A walk-in takeaway keeps its own rule (pos, and the question).
        var isQueued = orderEntity.OrderType is "delivery" or "pickup";
        var kitchenPrinted = isQueued && await KitchenTicket.IsPrintedAsync(_db, orderEntity.Id, ct);
        var disposition = isQueued && !kitchenPrinted
            ? Falah.Domain.Inventory.StockDisposition.NotPrepared
            : Falah.Domain.Inventory.InventoryRules.ParseStockDisposition(request.StockDisposition);
        string? approvedBy = null;
        if (kitchenPrinted)
        {
            var action = orderEntity.OrderType == "delivery" ? "delivery_override" : CancelUncollectedPickupHandler.ApprovalAction;
            if (AuthorizationPolicy.CanIssueApproval(actor, action)) approvedBy = cashierUser.Name;
            else if (string.IsNullOrEmpty(request.ManagerApprovalId) || string.IsNullOrEmpty(request.ManagerApproverUserId))
                throw new DomainException("إلغاء الطلب بعد ما ورقة المطبخ اتطبعت محتاج رمز المدير.");
            else
            {
                var approver = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                    ?? throw new DomainException("المعتمد غير موجود.");
                approvedBy = AuthorizationPolicy.ValidateApproval(_approvalRegistry, action, new ManagerApproval(
                    request.ManagerApprovalId, approver.Id, approver.Name, approver.Role, DateTimeOffset.UtcNow, action, request.Reason),
                    cashierUser.Id).ApproverName;
            }
        }

        // Delivery audit, owner decision 2026-11-24 (D2): a delivery order's own delivery is cancelled with it while it
        // is still at the restaurant - before this it stayed on the dispatch board and could still be sent out. One
        // that already left (only possible before D1) is resolved by recording its outcome, not by this.
        var delivery = await _db.Deliveries
            .FromSqlRaw("SELECT * FROM Deliveries WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE OrderId = {0}", orderEntity.Id)
            .FirstOrDefaultAsync(ct);
        if (delivery is not null && delivery.Status is not (nameof(Falah.Domain.Delivery.DeliveryStatus.New)
                or nameof(Falah.Domain.Delivery.DeliveryStatus.Preparing) or nameof(Falah.Domain.Delivery.DeliveryStatus.Ready)
                or nameof(Falah.Domain.Delivery.DeliveryStatus.Cancelled)))
            throw new DomainException("الطلب خرج مع السائق. سجّل نتيجة التوصيل الأول قبل إلغاء التحويل.");

        var wasAlreadyCancelled = false;
        var order = OrderMapper.ToDomain(orderEntity);
        var now = DateTimeOffset.UtcNow;
        order.CancelElectronicPayment(request.Reason, cashierUser.Name, now); // throws if confirmed/no leg/empty reason

        orderEntity.Status = order.Status.ToWireString();
        orderEntity.IsPaid = order.IsPaid;
        orderEntity.ElectronicPaymentStatus = order.ElectronicPayment?.Status.ToWireString();
        orderEntity.CancelledAtUtc = order.CancelledAtUtc;
        orderEntity.CancelReason = order.CancelReason;
        orderEntity.CancelledBy = order.CancelledBy;
        orderEntity.Version++;

        long cashToHandBack = 0;
        if (!wasAlreadyCancelled)
        {
            // Reverse only money that actually posted, never the unconfirmed electronic claim.
            var collected = await _db.TreasuryMovements.AsNoTracking()
                .Where(m => m.ReferenceId == orderEntity.Id && m.Type == "sale_collection" && m.Status == nameof(MovementStatus.Posted))
                .OrderBy(m => m.DestinationAccountId).ThenBy(m => m.Id).ToListAsync(ct);
            if (collected.Count > 0)
            {
                var dayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
                var activeShift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == "Open", ct);
                if (collected.Any(m => m.DestinationAccountId == TreasuryAccounts.Drawer) && activeShift is null)
                    throw new DomainException("يجب فتح وردية قبل رد الجزء النقدي من الطلب الملغي.");
                foreach (var paid in collected)
                {
                    var source = paid.DestinationAccountId ?? throw new DomainException("حساب التحصيل الأصلي غير موجود.");
                    var amount = Money.FromPiastres(paid.AmountPiastres);
                    var balance = await TreasuryBalanceQuery.LockAndReadBalanceAsync(_db, source, ct);
                    FinancialRules.AssertProtectedSourceBalance(balance, source, amount);
                    var reversal = TreasuryMovementFactory.BuildCashLifecycleMovement(
                        $"tm-{Guid.NewGuid():N}", $"void-return-{paid.Id}", $"op-{Guid.NewGuid():N}", "void_return",
                        source, null, amount, "order_void", orderEntity.Id, dayId, activeShift?.Id,
                        request.Reason.Trim(), cashierUser.Name, null, now);
                    _db.TreasuryMovements.Add(OrderMapper.ToEntity(reversal));
                    // Make each debit visible to the next balance check in this same transaction.
                    await _db.SaveChangesAsync(ct);
                    if (source == TreasuryAccounts.Drawer) cashToHandBack += paid.AmountPiastres;
                }
            }
            await OrderStockDisposition.ApplyAsync(_db, orderEntity.Id, disposition, request.Reason.Trim(), cashierUser.Name, now, ct);
            if (delivery is not null && delivery.Status != nameof(Falah.Domain.Delivery.DeliveryStatus.Cancelled))
            {
                delivery.Status = nameof(Falah.Domain.Delivery.DeliveryStatus.Cancelled);
                delivery.Version++;
                _db.DeliveryEvents.Add(new Falah.Infrastructure.Persistence.Entities.DeliveryEventEntity
                {
                    Id = $"delivery-cancel-electronic-{orderEntity.Id}", DeliveryId = delivery.Id, OrderId = orderEntity.Id,
                    Type = "cancelled", Reason = request.Reason.Trim(), Actor = cashierUser.Name, CreatedAtUtc = now,
                });
            }
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
                $"إلغاء محاولة دفع إلكتروني {orderEntity.OrderNumber}", "cash",
                $"لم يصل التحويل - {order.ElectronicPayment?.Amount.Piastres} قرش - السبب: {request.Reason.Trim()}",
                "warning", now, ShiftId: orderEntity.ShiftId, Reason: request.Reason.Trim(), AuthorizedBy: approvedBy, ReferenceId: orderEntity.Id));
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new CancelPendingElectronicPaymentResult(
            orderEntity.Id, orderEntity.Status, orderEntity.ElectronicPaymentStatus!, WasReplayed: wasAlreadyCancelled, CashToHandBackPiastres: cashToHandBack);
    }
}
