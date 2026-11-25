namespace Falah.Infrastructure.Orders;

using System.Data;
using Falah.Application.Orders;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// FIN-0051 (owner decision 2026-11-22): a cash-on-delivery order that has not been handed to a driver
/// can be cancelled - the customer rang back, the address is out of range. Before this there was no
/// path at all: it could not be refunded (nothing was collected) and it could not be voided until it
/// had gone out and failed. It is the same void a failed delivery gets
/// (<c>Order.VoidUndeliveredCashOnDelivery</c>): out of sales, no money moves because none was taken,
/// and the food is accounted for the way the operator says it went.
///
/// Who: delivery_override, or a manager's approval issued for that action. Only while the delivery is
/// still New, Preparing or Ready (DeliveryTransitions allows Cancelled from those), only for an unpaid
/// cash-on-delivery order with no refund, and only inside the business day it was sold in, so a closed
/// day never changes. RequestId replays through a delivery event.
/// </summary>
public sealed class CancelUndispatchedDeliveryHandler
{
    private const string Permission = "delivery_override";

    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public CancelUndispatchedDeliveryHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<CancelUndispatchedDeliveryResult> HandleAsync(CancelUndispatchedDeliveryRequest request, CancellationToken ct = default)
    {
        var eventId = $"delivery-cancel-{request.RequestId}";
        if (await ReplayAsync(eventId, ct) is { } replay) return replay;

        if (string.IsNullOrWhiteSpace(request.RequestId)) throw new DomainException("رقم العملية مطلوب.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب الإلغاء مطلوب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, Permission, request.Reason);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await FinancialAdmission.AcquireAsync(_db, ct);
        if (await ReplayAsync(eventId, ct) is { } admissionReplay) return admissionReplay;

        var orderEntity = await _db.Orders
            .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
            .Include(o => o.Items).Include(o => o.Refunds)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("الطلب غير موجود.");
        var delivery = await _db.Deliveries
            .FromSqlRaw("SELECT * FROM Deliveries WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE OrderId = {0}", request.OrderId)
            .FirstOrDefaultAsync(ct)
            ?? throw new DomainException("هذا الطلب ليس طلب توصيل.");

        var order = OrderMapper.ToDomain(orderEntity);
        if (order.Status == Falah.Domain.Orders.OrderStatus.Cancelled) throw new DomainException("الطلب ملغى بالفعل.");
        if (!Falah.Domain.Orders.CheckoutRules.IsCashOnDelivery(order.OrderType, order.PaymentMethod, order.Total) || order.IsPaid)
            throw new DomainException("الإلغاء قبل خروج الطيار متاح لطلب دفع عند الاستلام لم يُحصَّل فقط. الطلب المدفوع يُرد بالاسترجاع.");
        var deliveryStatus = Enum.Parse<DeliveryStatus>(delivery.Status);
        if (deliveryStatus is not (DeliveryStatus.New or DeliveryStatus.Preparing or DeliveryStatus.Ready))
            throw new DomainException("الطلب اتسلم للطيار. سجّل نتيجة التوصيل (فشل أو مرتجع) بدلًا من الإلغاء.");
        var openDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
        if (orderEntity.BusinessDayId is not null && orderEntity.BusinessDayId != openDayId)
            throw new DomainException("الطلب من يوم عمل مقفول؛ لا يمكن إلغاؤه بعد قفل اليوم.");

        // Workflow redesign (owner decision 2026-11-24): before the kitchen has its paper the order is cancelled with no
        // admin code and every ingredient goes back (nothing was made); after it, a manager authorises and the operator
        // says whether the food was prepared. Checked last, so a refused cancellation never spends the approval.
        var actor = OrderMapper.ToActor(actorUser);
        Falah.Domain.Inventory.StockDisposition disposition;
        ManagerApproval? approvedBy = null;
        if (!await KitchenTicket.IsPrintedAsync(_db, orderEntity.Id, ct))
        {
            if (!AuthorizationPolicy.HasPermission(actor, "pos") && !AuthorizationPolicy.HasPermission(actor, "delivery_manage"))
                throw new DomainException("ليس لديك صلاحية إلغاء الطلبات.");
            disposition = Falah.Domain.Inventory.StockDisposition.NotPrepared;
        }
        else
        {
            disposition = Falah.Domain.Inventory.InventoryRules.ParseStockDisposition(request.StockDisposition);
            approvedBy = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, Permission, approval, requireApproval: true).Approver;
        }
        var authorization = new AuthorizationPolicy.AuthorizationResult(actor, approvedBy);

        var now = DateTimeOffset.UtcNow;
        var reason = request.Reason.Trim();
        order.VoidUndeliveredCashOnDelivery(reason, actorUser.Name, now);
        orderEntity.Status = order.Status.ToWireString();
        orderEntity.CancelledAtUtc = order.CancelledAtUtc;
        orderEntity.CancelReason = order.CancelReason;
        orderEntity.CancelledBy = order.CancelledBy;
        orderEntity.Version++;

        DeliveryTransitions.AssertTransition(deliveryStatus, DeliveryStatus.Cancelled);
        delivery.Status = nameof(DeliveryStatus.Cancelled);
        _db.DeliveryEvents.Add(new DeliveryEventEntity
        {
            Id = eventId, DeliveryId = delivery.Id, OrderId = delivery.OrderId, DriverId = delivery.DriverId, TripId = delivery.TripId,
            Type = "cancelled", Reason = reason, Actor = actorUser.Name, Approver = authorization.Approver?.ApproverName, CreatedAtUtc = now,
        });

        await OrderStockDisposition.ApplyAsync(_db, orderEntity.Id, disposition, reason, actorUser.Name, now, ct);
        _auditLog.Append(new AuditEvent(
            $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
            $"إلغاء طلب توصيل قبل خروجه {orderEntity.OrderNumber}", "order",
            $"دفع عند الاستلام لم يُحصَّل - {orderEntity.TotalPiastres} قرش - السبب: {reason}",
            "warning", now, AuthorizedBy: authorization.Approver?.ApproverName, ShiftId: orderEntity.ShiftId, Reason: reason, ReferenceId: orderEntity.Id));

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new CancelUndispatchedDeliveryResult(orderEntity.Id, orderEntity.Status, delivery.Status, WasReplayed: false);
    }

    private async Task<CancelUndispatchedDeliveryResult?> ReplayAsync(string eventId, CancellationToken ct)
    {
        var existing = await _db.DeliveryEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (existing is null) return null;
        var orderStatus = await _db.Orders.AsNoTracking().Where(o => o.Id == existing.OrderId).Select(o => o.Status).FirstAsync(ct);
        var deliveryStatus = await _db.Deliveries.AsNoTracking().Where(d => d.Id == existing.DeliveryId).Select(d => d.Status).FirstAsync(ct);
        return new CancelUndispatchedDeliveryResult(existing.OrderId, orderStatus, deliveryStatus, WasReplayed: true);
    }
}
