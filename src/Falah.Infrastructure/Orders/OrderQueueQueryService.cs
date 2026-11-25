namespace Falah.Infrastructure.Orders;

using Falah.Application.Orders;
using Falah.Domain.Delivery;
using Falah.Domain.Printing;
using Falah.Infrastructure.Delivery;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Workflow redesign (owner decision 2026-11-24): the live queue of deliveries and pickups, each with its stage and the
/// one next action that makes sense now. Read-only; every stage is a mapping of records the system already keeps:
///
/// DELIVERY (order + its Delivery row + its print jobs)
///   new        - delivery New/Preparing and the kitchen paper has not reached the printer   → mark_ready (+ print_kitchen beside it)
///   preparing  - delivery New/Preparing, kitchen paper printed                                → mark_ready
///   ready      - delivery Ready                                     → assign, or confirm_transfer while one is pending
///   out        - Assigned / HandedToDriver / OutForDelivery                                    → record_result
///   delivered / failed / returned - outcome recorded, driver not settled yet                   → settle_driver
///   (Settled or Cancelled leave the queue.)
/// PICKUP (order + its print jobs)
///   new / preparing - not handed over (new while its kitchen paper has not reached the printer, which adds print_kitchen
///                beside the main step)                       → collect (unpaid: pay and hand over) or hand_over (paid)
/// A missing kitchen paper never holds an order back - a printer that is off must not stop preparing, dispatching or
/// handing over; it only decides whether a cancellation needs a manager.
///   completed  - handed over during the open business day (shown, no action); older ones leave the queue.
///
/// Cancel is offered only on an unpaid order still at the restaurant - cash with the driver or an unconfirmed transfer
/// on a delivery not yet out; an unpaid or unconfirmed pickup not yet handed over. A paid order is refunded instead.
/// </summary>
public sealed class OrderQueueQueryService
{
    private readonly FalahDbContext _db;
    public OrderQueueQueryService(FalahDbContext db) => _db = db;

    public async Task<IReadOnlyList<QueueOrderDto>> ListAsync(CancellationToken ct = default)
    {
        var deliveries = await _db.Deliveries.AsNoTracking()
            .Where(d => d.Status != nameof(DeliveryStatus.Settled) && d.Status != nameof(DeliveryStatus.Cancelled))
            .ToListAsync(ct);
        var deliveryOrderIds = deliveries.Select(d => d.OrderId).ToList();

        var openDayId = await _db.BusinessDays.AsNoTracking()
            .Where(d => d.Status == "Open" || d.Status == "Closing" || d.Status == "Reopened").Select(d => d.Id).FirstOrDefaultAsync(ct);
        // A pickup not handed over yet belongs to the open business day (an unpaid one cannot outlive it - it blocks the
        // close) or to the last 24 hours. Older ones - before handover was recorded at all, or left unmarked - are not
        // shown as waiting forever; one handed over today is shown as completed.
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        var pickups = await _db.Orders.AsNoTracking()
            // A pickup refunded in full leaves the queue like a cancelled one: its money went back, so there is nothing
            // to hand over (HandOverPickupHandler refuses it). A partly refunded one stays.
            .Where(o => o.OrderType == "pickup" && o.Status != "cancelled" && o.Status != "refunded"
                && (o.HandedOverAtUtc == null
                        ? o.AwaitingCollection || (openDayId != null && o.BusinessDayId == openDayId) || o.CreatedAtUtc > since
                        : openDayId != null && o.BusinessDayId == openDayId))
            .ToListAsync(ct);

        var deliveryOrders = deliveryOrderIds.Count == 0 ? new List<OrderEntity>()
            : await _db.Orders.AsNoTracking().Where(o => deliveryOrderIds.Contains(o.Id) && o.Status != "cancelled").ToListAsync(ct);
        var allOrderIds = deliveryOrders.Select(o => o.Id).Concat(pickups.Select(p => p.Id)).ToList();
        var printed = allOrderIds.Count == 0 ? new HashSet<string>()
            : (await _db.PrintJobs.AsNoTracking()
                .Where(j => allOrderIds.Contains(j.OrderId) && !j.IsReprint
                    && j.Route != nameof(PrintRoute.DriverDispatch) && j.DocumentKind != nameof(PrintDocumentKind.PickupReceipt)
                    && (j.Status == nameof(PrintJobStatus.Succeeded) || j.Status == nameof(PrintJobStatus.Sending)))
                .Select(j => j.OrderId).Distinct().ToListAsync(ct)).ToHashSet();
        var driverIds = deliveries.Where(d => d.DriverId != null).Select(d => d.DriverId!).Distinct().ToList();
        var drivers = await _db.Drivers.AsNoTracking().Where(d => driverIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var queue = new List<QueueOrderDto>();
        var ordersById = deliveryOrders.ToDictionary(o => o.Id);
        foreach (var delivery in deliveries)
        {
            if (!ordersById.TryGetValue(delivery.OrderId, out var order)) continue;   // a cancelled order's leftover row
            queue.Add(ForDelivery(order, delivery, printed.Contains(order.Id),
                delivery.DriverId is { } id ? drivers.GetValueOrDefault(id) ?? delivery.DriverNameAtHandover : null));
        }
        foreach (var pickup in pickups) queue.Add(ForPickup(pickup, printed.Contains(pickup.Id)));

        return queue.OrderBy(q => q.CreatedAtUtc).ToList();
    }

    private static string PaymentStateOf(OrderEntity order) =>
        order.AwaitingCollection ? "pay_at_counter"
        : order.ElectronicPaymentStatus == "pending" ? "pending_transfer"
        : order.OrderType == "delivery" && order.PaymentMethod == "cash" && !order.IsPaid && order.TotalPiastres > 0 ? "cash_on_delivery"
        : "paid";

    internal static QueueOrderDto ForDelivery(OrderEntity order, DeliveryEntity delivery, bool kitchenPrinted, string? driverName)
    {
        var status = Enum.Parse<DeliveryStatus>(delivery.Status);
        var payment = PaymentStateOf(order);
        var pending = payment == "pending_transfer";
        var atRestaurant = status is DeliveryStatus.New or DeliveryStatus.Preparing or DeliveryStatus.Ready;
        // The kitchen paper not reaching the printer is a warning with its own button, never a gate: a printer that is
        // off must not stop the restaurant from preparing, dispatching or handing over (owner review, 2026-11-24).
        var (stage, label, action, actionLabel) = status switch
        {
            DeliveryStatus.New or DeliveryStatus.Preparing when !kitchenPrinted =>
                ("new", "جديد", "mark_ready", "جاهز للتسليم"),
            DeliveryStatus.New or DeliveryStatus.Preparing => ("preparing", "قيد التحضير", "mark_ready", "جاهز للتسليم"),
            DeliveryStatus.Ready when pending => ("ready", "جاهز - مستني تأكيد التحويل", "confirm_transfer", "أكّد التحويل"),
            DeliveryStatus.Ready => ("ready", "جاهز للتسليم", "assign", "سلّم للسائق"),
            DeliveryStatus.Assigned or DeliveryStatus.HandedToDriver or DeliveryStatus.OutForDelivery =>
                ("out", "مع السائق", "record_result", "سجّل نتيجة التوصيل"),
            DeliveryStatus.Delivered => ("delivered", "اتسلّم للعميل - مستني تسوية السائق", "settle_driver", "سوّي السائق"),
            DeliveryStatus.Failed => ("failed", "فشل التوصيل - مستني تسوية السائق", "settle_driver", "سوّي السائق"),
            DeliveryStatus.Returned => ("returned", "رجع للمطعم - مستني تسوية السائق", "settle_driver", "سوّي السائق"),
            _ => ("out", delivery.Status, (string?)null, (string?)null),
        };
        // A paid order that never reached its customer still holds their money: it is refunded by hand (owner, 2026-11-24).
        var needsRefund = status is DeliveryStatus.Failed or DeliveryStatus.Returned && payment == "paid"
            && order.RefundAmountPiastres < order.TotalPiastres;
        var printMissing = atRestaurant && !kitchenPrinted;
        var attention = needsRefund ? "مدفوع وما وصلش للعميل - محتاج مرتجع"
            : pending ? "التحويل لسه ما اتأكدش - ما يخرجش مع السائق قبل التأكيد"
            : printMissing ? "ورقة المطبخ ما وصلتش للطابعة" : null;
        var collect = payment == "cash_on_delivery" ? order.TotalPiastres - order.RefundAmountPiastres : 0;
        return new QueueOrderDto(order.Id, order.OrderNumber, "delivery", order.CustomerName, order.CustomerPhone,
            order.DeliveryArea, order.DeliveryAddress, order.TotalPiastres, order.PaymentMethod, payment,
            stage, label, action, actionLabel,
            CanCancel: atRestaurant && payment is ("cash_on_delivery" or "pending_transfer"),
            CancelNeedsAdmin: kitchenPrinted, KitchenPrinted: kitchenPrinted,
            delivery.Id, delivery.Status, delivery.DriverId, driverName, collect, needsRefund, attention,
            order.CreatedAtUtc, order.RequestedPickupAtUtc, null, order.ElectronicPaymentStatus, order.AwaitingCollection,
            printMissing ? "print_kitchen" : null, printMissing ? "اطبع ورقة المطبخ" : null);
    }

    internal static QueueOrderDto ForPickup(OrderEntity order, bool kitchenPrinted)
    {
        var payment = PaymentStateOf(order);
        string stage, label; string? action, actionLabel;
        var open = order.HandedOverAtUtc is null;
        var stageKey = !open ? "completed" : kitchenPrinted ? "preparing" : "new";
        var stageName = stageKey switch { "completed" => $"اتسلّم للعميل ({order.HandedOverBy})", "new" => "جديد", _ => "قيد التحضير" };
        // Handing over never waits for the printer (owner review, 2026-11-24): a missing kitchen paper is a warning.
        if (!open) (stage, label, action, actionLabel) = (stageKey, stageName, null, null);
        else if (order.AwaitingCollection) (stage, label, action, actionLabel) = (stageKey, stageName + " - يدفع وهو بيستلم", "collect", "العميل وصل: حصّل وسلّم");
        else (stage, label, action, actionLabel) = (stageKey, stageName + " - مدفوع", "hand_over", "العميل وصل: سلّم واطبع الإيصال");
        var printMissing = open && !kitchenPrinted;
        var attention = payment == "pending_transfer" ? "التحويل لسه ما اتأكدش"
            : printMissing ? "ورقة المطبخ ما وصلتش للطابعة" : null;
        return new QueueOrderDto(order.Id, order.OrderNumber, "pickup", order.CustomerName, order.CustomerPhone, null, null,
            order.TotalPiastres, order.PaymentMethod, payment, stage, label, action, actionLabel,
            CanCancel: open && payment is ("pay_at_counter" or "pending_transfer"),
            CancelNeedsAdmin: kitchenPrinted, KitchenPrinted: kitchenPrinted,
            null, null, null, null, 0, false, attention,
            order.CreatedAtUtc, order.RequestedPickupAtUtc, order.HandedOverAtUtc, order.ElectronicPaymentStatus, order.AwaitingCollection,
            printMissing ? "print_kitchen" : null, printMissing ? "اطبع ورقة المطبخ" : null);
    }
}
