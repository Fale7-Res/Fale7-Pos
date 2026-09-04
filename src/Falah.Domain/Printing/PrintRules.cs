namespace Falah.Domain.Printing;

using Falah.Domain.Common;
using Falah.Domain.Orders;

/// <summary>
/// Ported from the customer/grill receipt tabs in components/modals/ReceiptModal.tsx, adapted to
/// the current physical topology: the reference app printed ONE combined "بون تجهيز المشويات
/// والفحم" ticket filtered by a single sendToGrill flag; this restaurant now has two independent
/// kitchen printers (charcoal, grill), so the same idea splits into one ticket per station, each
/// containing only that station's items - "same order number, separate station tickets" is the
/// explicit requirement this method exists to satisfy. A kitchen ticket never carries a price,
/// matching the reference app exactly; a customer/call-center receipt always does.
/// </summary>
public static class PrintRules
{
    private static readonly IReadOnlySet<PrintRoute> KitchenRoutes = new HashSet<PrintRoute> { PrintRoute.Charcoal, PrintRoute.Grill };
    private static readonly IReadOnlySet<PrintRoute> CustomerRoutes = new HashSet<PrintRoute> { PrintRoute.Receipt, PrintRoute.CallCenter };

    /// <summary>Which kitchen station tickets an order's own items require - the caller (whichever
    /// station/client requested printing) never decides this; it is entirely derived from each
    /// item's own PreparationStation. Returns an empty set for an order with no kitchen-bound items
    /// (e.g. drinks only) - no ticket is created for a station with nothing to prepare.</summary>
    public static IReadOnlySet<PrintRoute> DetermineKitchenRoutes(IEnumerable<string?> itemPreparationStations) =>
        itemPreparationStations
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => Enum.TryParse<PrintRoute>(s, out var route) ? route : (PrintRoute?)null)
            .Where(r => r is not null && KitchenRoutes.Contains(r.Value))
            .Select(r => r!.Value)
            .ToHashSet();

    /// <summary>One station's ticket, containing only the items assigned to THAT station - a driver
    /// or chef reading a Charcoal ticket must never see a Grill item and wonder why it's there.</summary>
    public static ComposedTicket ComposeKitchenTicket(
        PrintRoute route, string orderId, string orderNumber, OrderType orderType, string? tableNumber,
        IReadOnlyList<TicketItem> items, DateTimeOffset createdAtUtc)
    {
        if (!KitchenRoutes.Contains(route)) throw new DomainException($"{route} ليست محطة تجهيز.");

        var stationItems = items.Where(i => i.PreparationStation == route.ToString()).ToList();
        var title = route == PrintRoute.Charcoal ? "بون تجهيز الفحم" : "بون تجهيز الشواية";

        var lines = new List<TicketLine>
        {
            new(title, Bold: true, Centered: true),
            new(orderNumber, Bold: true, Centered: true, LargeFont: true),
            new($"{createdAtUtc:yyyy-MM-dd HH:mm}   {DescribeOrderType(orderType, tableNumber)}"),
            new("الأصناف المطلوبة للتجهيز:", Bold: true),
        };
        foreach (var item in stationItems)
        {
            lines.Add(new TicketLine($"{item.Quantity} × {item.ProductName}", Bold: true, LargeFont: true));
            if (!string.IsNullOrWhiteSpace(item.VariantName)) lines.Add(new TicketLine($"  {item.VariantName}"));
            if (!string.IsNullOrWhiteSpace(item.Notes)) lines.Add(new TicketLine($"  تنبيه: {item.Notes}"));
        }
        lines.Add(new TicketLine("*** يُسلم هذا البون للمسؤول عن المحطة ***", Centered: true));
        return new ComposedTicket(route, orderId, orderNumber, lines);
    }

    /// <summary>The full customer-facing receipt (prices, totals, payment method) - used for both
    /// the Cashier's own Receipt printer and, when a Call Center operator wants their own copy of
    /// what was just phoned in, the CallCenter printer. Same content either way; only the physical
    /// destination differs, decided by which route the caller asked for.</summary>
    public static ComposedTicket ComposeReceiptTicket(PrintRoute route, ReceiptHeader header, IReadOnlyList<TicketItem> items, ReceiptTotals totals)
    {
        if (!CustomerRoutes.Contains(route)) throw new DomainException($"{route} ليس مسار إيصال عميل.");

        var lines = new List<TicketLine>
        {
            new(header.RestaurantName, Bold: true, Centered: true),
            new(header.SubName, Centered: true),
            new(header.Address, Centered: true),
        };
        if (!string.IsNullOrWhiteSpace(header.Phone1) || !string.IsNullOrWhiteSpace(header.Phone2))
            lines.Add(new TicketLine(string.Join(" - ", new[] { header.Phone1, header.Phone2 }.Where(p => !string.IsNullOrWhiteSpace(p))), Centered: true));
        if (!string.IsNullOrWhiteSpace(header.ComplaintsPhone))
            lines.Add(new TicketLine($"الشكاوى: {header.ComplaintsPhone}", Centered: true));
        lines.AddRange(new[]
        {
            new TicketLine($"رقم الطلب: {header.OrderNumber}", Bold: true, LargeFont: true, Centered: true),
            new TicketLine($"{header.CreatedAtUtc:yyyy-MM-dd HH:mm}"),
            new TicketLine($"الكاشير: {header.CashierName}"),
            new TicketLine($"نوع الطلب: {DescribeOrderType(header.OrderType, header.TableNumber)}"),
        });
        if (header.ElectronicPaymentPending)
            lines.Add(new TicketLine("⚠ غير مدفوع - في انتظار تأكيد التحويل", Bold: true, Centered: true));
        if (header.OrderType == OrderType.Delivery)
        {
            if (!string.IsNullOrWhiteSpace(header.CustomerName)) lines.Add(new TicketLine($"العميل: {header.CustomerName}"));
            if (!string.IsNullOrWhiteSpace(header.DeliveryArea)) lines.Add(new TicketLine($"المنطقة: {header.DeliveryArea}"));
            if (!string.IsNullOrWhiteSpace(header.DeliveryAddress)) lines.Add(new TicketLine($"العنوان: {header.DeliveryAddress}"));
        }

        lines.Add(new TicketLine("الصنف / الكمية / الإجمالي", Bold: true));
        foreach (var item in items)
        {
            var itemLine = $"{item.ProductName} × {item.Quantity}   {item.TotalPrice.Egp} ج";
            lines.Add(new TicketLine(itemLine));
            if (!string.IsNullOrWhiteSpace(item.VariantName)) lines.Add(new TicketLine($"  [{item.VariantName}]"));
            if (!string.IsNullOrWhiteSpace(item.Notes)) lines.Add(new TicketLine($"  ({item.Notes})"));
            // Printed from the order's own frozen snapshot, so a receipt reprinted after the offer
            // is edited or disabled still shows the offer that was actually applied at sale time.
            if (!string.IsNullOrWhiteSpace(item.PriceRuleName))
                lines.Add(new TicketLine($"  {item.PriceRuleName}" + (item.ListUnitPrice is { IsPositive: true } lp ? $" · بدل {lp.Egp} ج" : "")));
        }

        lines.Add(new TicketLine($"المجموع الفرعي: {totals.Subtotal.Egp} ج"));
        if (totals.Discount.IsPositive) lines.Add(new TicketLine($"الخصم: -{totals.Discount.Egp} ج"));
        if (totals.CompAmount.IsPositive) lines.Add(new TicketLine($"ضيافة: -{totals.CompAmount.Egp} ج"));
        if (totals.TaxAmount.IsPositive) lines.Add(new TicketLine($"{totals.TaxRuleName ?? "الضريبة"}: {totals.TaxAmount.Egp} ج"));
        if (totals.DeliveryFee.IsPositive) lines.Add(new TicketLine($"خدمة التوصيل: +{totals.DeliveryFee.Egp} ج"));
        lines.Add(new TicketLine($"الإجمالي: {totals.Total.Egp} ج", Bold: true, LargeFont: true));
        lines.Add(new TicketLine($"طريقة الدفع: {DescribePaymentMethod(totals.PaymentMethod)}"));
        // Ported from ReceiptModal.tsx: the amount actually tendered is its own line, distinct from
        // the change line below it (a real gap found during the legacy-parity audit - AmountPaid
        // was already a ReceiptTotals field but never rendered).
        if (totals.AmountPaid.IsPositive) lines.Add(new TicketLine($"المدفوع: {totals.AmountPaid.Egp} ج"));
        if (totals.ChangeDue.IsPositive) lines.Add(new TicketLine($"الباقي: {totals.ChangeDue.Egp} ج"));

        lines.Add(new TicketLine(string.IsNullOrWhiteSpace(header.Footer) ? "شكرًا لزيارتكم" : header.Footer, Centered: true));
        return new ComposedTicket(route, header.OrderId, header.OrderNumber, lines);
    }

    private static string DescribeOrderType(OrderType orderType, string? tableNumber) => orderType switch
    {
        OrderType.DineIn => $"صالة ({tableNumber ?? "طاولة"})",
        OrderType.Takeaway => "تيك أواي",
        OrderType.Delivery => "دليفري",
        _ => orderType.ToString(),
    };

    private static string DescribePaymentMethod(Financial.PaymentMethod method) => method switch
    {
        Financial.PaymentMethod.Cash => "نقدي",
        Financial.PaymentMethod.Card => "بطاقة",
        Financial.PaymentMethod.InstaPay => "إنستاباي",
        Financial.PaymentMethod.Wallet => "محفظة إلكترونية",
        Financial.PaymentMethod.Multi => "دفع متعدد",
        _ => method.ToString(),
    };
}
