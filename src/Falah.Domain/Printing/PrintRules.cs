namespace Falah.Domain.Printing;

using Falah.Domain.Common;
using Falah.Domain.Orders;

/// <summary>
/// What each of this restaurant's tickets says, and in what order. How any of it is SHAPED - column
/// widths, separators, the money line - lives in TicketLayout, so the six tickets stay one visual
/// system instead of six.
///
/// The physical topology these compose for: a cashier printer, a call-center printer, and two
/// independent kitchen station printers (charcoal, grill). A kitchen ticket never carries a price;
/// a customer or call-center ticket always does. Same order number across all of them.
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

    /// <summary>One station's ticket, containing only the items assigned to THAT station - a chef
    /// reading a Charcoal ticket must never see a Grill item and wonder why it's there. No prices:
    /// a prep station has no use for them and showing them only invites the wrong conversation at
    /// the pass.</summary>
    public static ComposedTicket ComposeKitchenTicket(
        PrintRoute route, string restaurantName, string orderId, string orderNumber, OrderType orderType,
        IReadOnlyList<TicketItem> items, DateTimeOffset createdAtUtc,
        DateTimeOffset? requestedPickupAtUtc = null)
    {
        if (!KitchenRoutes.Contains(route)) throw new DomainException($"{route} ليست محطة تجهيز.");

        var stationItems = items.Where(i => i.PreparationStation == route.ToString()).ToList();
        var stationName = route == PrintRoute.Charcoal ? "الفحم" : "الجريل";

        var lines = new List<TicketElement>
        {
            TicketLayout.Brand(restaurantName),
            new TicketText(stationName, Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Large),
        };
        lines.AddRange(TicketLayout.LargeOrderNumber(orderNumber));
        lines.AddRange(new[]
        {
            TicketLayout.Rule(),
            TicketLayout.Field("نوع الطلب", DescribeOrderType(orderType)),
            TicketLayout.Field("الوقت", FormatCairo(createdAtUtc)),
        });

        // The one thing a prep station needs that the order type alone does not tell it: an order
        // booked for later must not be cooked now. Bold because it changes what the station does.
        if (DescribeRequestedPickup(requestedPickupAtUtc) is { } wanted)
            lines.Add(new TicketText(wanted, Bold: true));

        lines.Add(TicketLayout.Rule());
        lines.Add(TicketLayout.ItemTableHeader(withPrice: false));
        lines.Add(TicketLayout.Rule());
        foreach (var item in stationItems)
        {
            lines.Add(TicketLayout.ItemRow(item, withPrice: false));
            lines.AddRange(TicketLayout.ItemAnnotations(item, withPriceRule: false));
            lines.Add(TicketLayout.Rule());
        }

        lines.Add(TicketLayout.Caption("يُسلَّم هذا البون للمسؤول عن المحطة"));
        return new ComposedTicket(route, orderId, orderNumber, lines);
    }

    /// <summary>
    /// The cashier's output and the call center's copy.
    ///
    /// Receipt prints TWO physically separate tickets from this one job: a compact ticket carrying
    /// only the order number, for the customer to hold, then a cut, then the detailed preparation
    /// ticket the counter works from. Both are submitted in one transport write; a physical
    /// interruption can still leave a partial pair, so uncertain attempts require inspection.
    ///
    /// CallCenter gets the detailed copy only. The compact ticket exists to be handed to a customer
    /// standing at the counter; there is nobody to hand it to on the phone.
    /// </summary>
    public static ComposedTicket ComposeReceiptTicket(PrintRoute route, ReceiptHeader header, IReadOnlyList<TicketItem> items, ReceiptTotals totals)
    {
        if (!CustomerRoutes.Contains(route)) throw new DomainException($"{route} ليس مسار إيصال عميل.");

        var lines = new List<TicketElement>();
        if (route == PrintRoute.Receipt)
        {
            lines.AddRange(ComposeCustomerNumberSection(header));
            lines.Add(new TicketCut());
            lines.AddRange(ComposeCashierDetailSection(header, items, totals));
        }
        else
        {
            // CallCenter keeps the layout it already had. The cashier redesign is deliberately
            // scoped to the till's own two tickets; converging the two is a separate decision once
            // the cashier output has been accepted on paper.
            lines.AddRange(ComposeDetailedOrderSection(header, items, totals));
        }
        return new ComposedTicket(route, header.OrderId, header.OrderNumber, lines);
    }

    /// <summary>
    /// The ticket the customer walks away with - the first half of the till's single print job.
    ///
    /// Identity only: who we are, which order this is, when, and who took it. No items, no prices,
    /// nothing to read out or dispute - just the number that will be called, printed as large as
    /// the paper allows. Kept compact: it is a token to hold, not a document to read.
    /// </summary>
    private static IEnumerable<TicketElement> ComposeCustomerNumberSection(ReceiptHeader header)
    {
        foreach (var element in ComposeBrandHeader(header)) yield return element;

        yield return TicketLayout.CenteredLine(DescribeOrderType(header.OrderType), bold: true);
        yield return TicketLayout.CenteredLine("رقم الطلب");
        yield return TicketLayout.CenteredLine(header.OrderNumber, bold: true, size: TicketTextSize.Display);
        yield return TicketLayout.Space();

        yield return TicketLayout.DateTimeRow(FormatCairoDate(header.CreatedAtUtc), FormatCairoTime(header.CreatedAtUtc));
        if (DescribeRequestedPickup(header.RequestedPickupAtUtc) is { } wanted)
            yield return TicketLayout.CenteredLine(wanted, bold: true);
        // Once, here. A name printed twice on a ticket this small reads as a mistake.
        if (!string.IsNullOrWhiteSpace(header.CashierName))
            yield return TicketLayout.CenteredLine($"الكاشير: {header.CashierName}");
        yield return TicketLayout.ThinRule();
        yield return TicketLayout.CenteredLine("احتفظ بهذا التيكت حتى استلام الطلب", bold: true);
    }

    /// <summary>
    /// The cashier's detailed receipt - the second half of the till's single print job.
    ///
    /// Five blocks, separated by whitespace and thin rules only: the brand, the order's identity,
    /// the items, the money, and the payment. Then the menu QR (only when a real menu link is
    /// configured) and the brand once more as the sign-off. No boxes, no filled bands, no vertical
    /// grid lines, no slogans.
    ///
    /// Separate from ComposeDetailedOrderSection, which CallCenter still uses, so that redesigning
    /// the till's output cannot silently change what the call center prints.
    /// </summary>
    private static IEnumerable<TicketElement> ComposeCashierDetailSection(
        ReceiptHeader header, IReadOnlyList<TicketItem> items, ReceiptTotals totals)
    {
        // Brand, then the order's identity, then an open three-column list. Weight and whitespace
        // carry the hierarchy: no grid, no rules between items, no filled bars.
        yield return TicketLayout.BrandTitle(header.RestaurantName);
        if (!string.IsNullOrWhiteSpace(header.SubName)) yield return TicketLayout.Tagline(header.SubName.Trim());
        yield return TicketLayout.ThinRule();

        // 2. Order identity: the number alone and largest, then the facts about it in two pairs.
        yield return TicketLayout.CenteredLine("رقم الطلب", size: TicketTextSize.Normal);
        yield return TicketLayout.CenteredLine(header.OrderNumber, bold: true, size: TicketTextSize.Huge);
        yield return TicketLayout.Space();
        yield return TicketLayout.SlipLine(DescribeOrderType(header.OrderType), $"الكاشير: {header.CashierName}");
        yield return TicketLayout.SlipLine(FormatCairoDate(header.CreatedAtUtc), FormatCairoTime(header.CreatedAtUtc));
        if (DescribeRequestedPickup(header.RequestedPickupAtUtc) is { } wanted)
            yield return TicketLayout.CenteredLine(wanted, bold: true);
        if (header.OrderType is OrderType.Delivery or OrderType.Pickup)
        {
            if (!string.IsNullOrWhiteSpace(header.CustomerName)) yield return TicketLayout.Field("العميل", header.CustomerName!);
            if (header.OrderType == OrderType.Delivery)
            {
                if (!string.IsNullOrWhiteSpace(header.DeliveryArea)) yield return TicketLayout.Field("المنطقة", header.DeliveryArea!);
                if (!string.IsNullOrWhiteSpace(header.DeliveryAddress)) yield return TicketLayout.Field("العنوان", header.DeliveryAddress!);
            }
        }

        // 3. Items.
        yield return TicketLayout.ThinRule();
        yield return TicketLayout.CashierItemHeader();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            yield return TicketLayout.CashierItemRow(item);
            if (!string.IsNullOrWhiteSpace(item.Notes))
                yield return TicketLayout.CashierItemNote($"ملاحظات: {item.Notes}");
            // Printed from the order's own frozen snapshot, so a ticket reprinted after an offer is
            // edited or disabled still shows the offer that was actually applied at sale time.
            if (!string.IsNullOrWhiteSpace(item.PriceRuleName))
                yield return TicketLayout.CashierItemNote($"عرض: {item.PriceRuleName}"
                    + (item.ListUnitPrice is { IsPositive: true } list ? $" (بدل {list.Egp} ج)" : ""));
            // A gap only where one is earned: after an item that carried a note or an offer, so the
            // next product cannot be read as another line of that note.
            var annotated = !string.IsNullOrWhiteSpace(item.Notes) || !string.IsNullOrWhiteSpace(item.PriceRuleName);
            if (annotated && i < items.Count - 1) yield return TicketLayout.Space();
        }
        yield return TicketLayout.ThinRule();

        // 4 and 5. Totals, then payment.
        foreach (var element in ComposeCashierTotals(header, totals)) yield return element;

        // Menu QR, then the brand as the sign-off.
        foreach (var element in ComposeBrandFooter(header)) yield return element;
    }

    /// <summary>The top of both cashier tickets: the name, the owner's tagline, and the short
    /// signature rule beneath them. Typography and whitespace only.</summary>
    private static IEnumerable<TicketElement> ComposeBrandHeader(ReceiptHeader header)
    {
        yield return TicketLayout.BrandTitle(header.RestaurantName);
        if (!string.IsNullOrWhiteSpace(header.SubName)) yield return TicketLayout.Tagline(header.SubName.Trim());
        yield return TicketLayout.AccentRule();
        yield return TicketLayout.Space();
    }

    /// <summary>
    /// The end of the detailed receipt: the menu QR with its one-line invitation, then the name and
    /// tagline - and nothing else. No thank-you line and no slogan.
    ///
    /// The QR appears only when Settings holds a real menu link. There is no placeholder or guessed
    /// address: a code that opens the wrong page is worse than no code at all.
    /// </summary>
    private static IEnumerable<TicketElement> ComposeBrandFooter(ReceiptHeader header)
    {
        yield return TicketLayout.ThinRule();
        if (!string.IsNullOrWhiteSpace(header.MenuUrl))
        {
            yield return TicketLayout.CenteredLine("امسح الكود لعرض المنيو");
            yield return new TicketQr(header.MenuUrl.Trim());
        }
        yield return TicketLayout.CenteredLine(header.RestaurantName, bold: true);
        if (!string.IsNullOrWhiteSpace(header.SubName))
            yield return new TicketText(header.SubName.Trim(), Align: TicketAlign.Center);
    }

    /// <summary>The till's totals and payment blocks. Every figure is read straight from the
    /// order's own frozen financial snapshot - nothing here adds, subtracts or re-derives money.
    /// Rows that would print a zero are omitted entirely, matching how the cashier screen itself
    /// shows them.</summary>
    private static IEnumerable<TicketElement> ComposeCashierTotals(ReceiptHeader header, ReceiptTotals totals)
    {
        // No +/- signs: the label already says which way each line goes, and a sign next to an
        // Arabic currency word is bidi-neutral - it detaches from the digits and prints "15-".
        // Always stated, even when nothing was adjusted: the customer should be able to see what the
        // items came to before reading what they paid.
        yield return TicketLayout.SlipLine("إجمالي الأصناف", $"{totals.Subtotal.Egp} ج");
        if (totals.Discount.IsPositive) yield return TicketLayout.MoneyLine("الخصم", $"{totals.Discount.Egp} ج", size: TicketTextSize.Large);
        if (totals.CompAmount.IsPositive) yield return TicketLayout.MoneyLine("ضيافة", $"{totals.CompAmount.Egp} ج", size: TicketTextSize.Large);
        if (totals.DeliveryFee.IsPositive) yield return TicketLayout.MoneyLine("رسوم التوصيل", $"{totals.DeliveryFee.Egp} ج", size: TicketTextSize.Large);
        if (totals.TaxAmount.IsPositive) yield return TicketLayout.MoneyLine(totals.TaxRuleName ?? "الضريبة", $"{totals.TaxAmount.Egp} ج", size: TicketTextSize.Large);

        // A thin rule, then the grand total bold and a size up - the strongest line on the ticket,
        // with no filled bar behind it.
        yield return TicketLayout.MoneyLine("الإجمالي الكلي", $"{totals.Total.Egp} ج", bold: true, size: TicketTextSize.Title);

        // Read from the order's own frozen electronic-payment flag, never inferred from the amounts.
        var isPaid = header.IsPaid ?? (!header.ElectronicPaymentPending && totals.AmountPaid >= totals.Total);
        var paymentState = header.ElectronicPaymentPending ? "لم يُؤكَّد"
            : isPaid ? "مدفوع"
            : CheckoutRules.IsCashOnDelivery(header.OrderType, totals.PaymentMethod, totals.Total) ? "عند الاستلام" : "غير مدفوع";
        yield return TicketLayout.MoneyLine($"طريقة الدفع: {DescribePaymentMethod(totals.PaymentMethod)}", paymentState, size: TicketTextSize.Large);
        if (totals.AmountPaid.IsPositive) yield return TicketLayout.MoneyLine("المدفوع", $"{totals.AmountPaid.Egp} ج", size: TicketTextSize.Large);
        if (totals.ChangeDue.IsPositive) yield return TicketLayout.MoneyLine("الباقي", $"{totals.ChangeDue.Egp} ج", bold: true, size: TicketTextSize.Large);
    }

    /// <summary>The full order: who took it, what is in it, what each line cost and what is owed.
    /// Shared verbatim by the cashier's second ticket and the call center's single copy - the
    /// content is identical, only the printer differs.</summary>
    private static IEnumerable<TicketElement> ComposeDetailedOrderSection(
        ReceiptHeader header, IReadOnlyList<TicketItem> items, ReceiptTotals totals)
    {
        yield return TicketLayout.Brand(header.RestaurantName);
        yield return TicketLayout.Caption("تذكرة تحضير الطلب");
        foreach (var element in TicketLayout.LargeOrderNumber(header.OrderNumber)) yield return element;
        yield return TicketLayout.Rule();

        yield return TicketLayout.Field("نوع الطلب", DescribeOrderType(header.OrderType));
        yield return TicketLayout.Field("التاريخ", FormatCairo(header.CreatedAtUtc));
        yield return TicketLayout.Field("الكاشير", header.CashierName);
        if (DescribeRequestedPickup(header.RequestedPickupAtUtc) is { } wanted)
            yield return new TicketText(wanted, Bold: true);
        if (header.ElectronicPaymentPending)
            yield return new TicketText("غير مدفوع - في انتظار تأكيد التحويل", Bold: true, Align: TicketAlign.Center);

        if (header.OrderType == OrderType.Delivery)
        {
            if (!string.IsNullOrWhiteSpace(header.CustomerName)) yield return TicketLayout.Field("العميل", header.CustomerName!);
            if (!string.IsNullOrWhiteSpace(header.DeliveryArea)) yield return TicketLayout.Field("المنطقة", header.DeliveryArea!);
            if (!string.IsNullOrWhiteSpace(header.DeliveryAddress)) yield return TicketLayout.Field("العنوان", header.DeliveryAddress!);
        }

        foreach (var element in ComposeItemTable(items, withPrice: true)) yield return element;
        foreach (var element in ComposeTotals(totals)) yield return element;

        yield return TicketLayout.Caption(string.IsNullOrWhiteSpace(header.Footer) ? "شكرًا لزيارتكم" : header.Footer!);
    }

    /// <summary>The delivery ORDER ticket - printed once when a delivery order is created at
    /// checkout, before any driver exists. Distinct from the driver dispatch ticket (printed later,
    /// at assignment, naming a driver) and from the customer-facing ticket. Carries the full order
    /// so the dispatch desk can work it end to end: who it is for, where it goes, what is in it,
    /// and exactly what will have to be collected - or that there is nothing to collect.</summary>
    public static ComposedTicket ComposeDeliveryOrderTicket(
        ReceiptHeader header, IReadOnlyList<TicketItem> items, ReceiptTotals totals, DeliveryOrderTicketInfo delivery)
    {
        var lines = new List<TicketElement>
        {
            TicketLayout.Brand(header.RestaurantName),
            new TicketText("أوردر دليفري", Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Large),
        };
        lines.AddRange(TicketLayout.LargeOrderNumber(header.OrderNumber));
        lines.Add(TicketLayout.Rule());
        lines.Add(TicketLayout.Field("وقت الطلب", FormatCairo(header.CreatedAtUtc)));

        if (!string.IsNullOrWhiteSpace(header.CustomerName)) lines.Add(new TicketText($"العميل: {header.CustomerName}", Bold: true));
        if (!string.IsNullOrWhiteSpace(delivery.CustomerPhone)) lines.Add(new TicketText($"الهاتف: {delivery.CustomerPhone}", Bold: true));
        if (!string.IsNullOrWhiteSpace(header.DeliveryArea)) lines.Add(TicketLayout.Field("المنطقة", header.DeliveryArea!));
        if (!string.IsNullOrWhiteSpace(header.DeliveryAddress)) lines.Add(TicketLayout.Field("العنوان", header.DeliveryAddress!));

        lines.AddRange(ComposeItemTable(items, withPrice: true));
        lines.AddRange(ComposeTotals(totals));
        lines.AddRange(ComposeCollectionInstruction(delivery.IsCashOnDelivery, header.ElectronicPaymentPending, delivery.AmountToCollect));

        if (!string.IsNullOrWhiteSpace(delivery.DeliveryNotes))
        {
            lines.Add(TicketLayout.Rule());
            lines.Add(new TicketText("ملاحظات التوصيل:", Bold: true));
            lines.Add(new TicketText(delivery.DeliveryNotes!));
        }

        lines.Add(TicketLayout.Caption("بون تشغيل - ليس إيصال العميل"));
        return new ComposedTicket(PrintRoute.DeliveryOrder, header.OrderId, header.OrderNumber, lines);
    }

    /// <summary>The driver dispatch ticket - printed when a delivery is assigned or reassigned,
    /// never touching the customer-facing ticket or any financial field of the order. isReassignment
    /// adds a prominent marker so a driver holding a second copy for the same order can never
    /// mistake it for the original dispatch. Whether there is cash to collect is decided by the
    /// caller from CheckoutRules.IsCashOnDelivery - the same rule checkout itself used, and the
    /// same one the delivery order ticket above is given, so the two can never contradict.</summary>
    public static ComposedTicket ComposeDriverDispatchTicket(
        string restaurantName, string orderId, string orderNumber, string driverName,
        string? customerName, string? customerPhone, string? deliveryAddress, string? deliveryArea,
        Money amountToCollect, bool isCashOnDelivery, bool electronicPaymentPending,
        string? deliveryNotes, DateTimeOffset assignedAtUtc, bool isReassignment)
    {
        var lines = new List<TicketElement> { TicketLayout.Brand(restaurantName) };
        if (isReassignment)
            lines.Add(new TicketText("إعادة إسناد", Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Large));
        lines.Add(TicketLayout.Caption("تيكت إسناد للسائق"));
        lines.AddRange(TicketLayout.LargeOrderNumber(orderNumber));
        lines.Add(TicketLayout.Rule());

        lines.Add(new TicketText($"السائق: {driverName}", Bold: true, Size: TicketTextSize.Large));
        lines.Add(TicketLayout.Field("وقت الإسناد", FormatCairo(assignedAtUtc)));
        lines.Add(TicketLayout.Rule());

        if (!string.IsNullOrWhiteSpace(customerName)) lines.Add(new TicketText($"العميل: {customerName}", Bold: true));
        if (!string.IsNullOrWhiteSpace(customerPhone)) lines.Add(new TicketText($"الهاتف: {customerPhone}", Bold: true));
        if (!string.IsNullOrWhiteSpace(deliveryArea)) lines.Add(TicketLayout.Field("المنطقة", deliveryArea!));
        if (!string.IsNullOrWhiteSpace(deliveryAddress)) lines.Add(TicketLayout.Field("العنوان", deliveryAddress!));

        lines.AddRange(ComposeCollectionInstruction(isCashOnDelivery, electronicPaymentPending, amountToCollect));

        if (!string.IsNullOrWhiteSpace(deliveryNotes))
        {
            lines.Add(TicketLayout.Rule());
            lines.Add(new TicketText("ملاحظات التوصيل:", Bold: true));
            lines.Add(new TicketText(deliveryNotes!));
        }

        lines.Add(TicketLayout.Caption("يُسلَّم هذا البون للسائق"));
        return new ComposedTicket(PrintRoute.DriverDispatch, orderId, orderNumber, lines);
    }

    private static IEnumerable<TicketElement> ComposeItemTable(IReadOnlyList<TicketItem> items, bool withPrice)
    {
        yield return TicketLayout.Rule();
        yield return TicketLayout.ItemTableHeader(withPrice);
        yield return TicketLayout.Rule();
        foreach (var item in items)
        {
            yield return TicketLayout.ItemRow(item, withPrice);
            // Printed from the order's own frozen snapshot, so a ticket reprinted after an offer is
            // edited or disabled still shows the offer that was actually applied at sale time.
            foreach (var annotation in TicketLayout.ItemAnnotations(item, withPriceRule: withPrice)) yield return annotation;
            yield return TicketLayout.Rule();
        }
    }

    private static IEnumerable<TicketElement> ComposeTotals(ReceiptTotals totals)
    {
        yield return TicketLayout.MoneyLine("المجموع الفرعي", $"{totals.Subtotal.Egp} ج");
        if (totals.Discount.IsPositive) yield return TicketLayout.MoneyLine("الخصم", $"-{totals.Discount.Egp} ج");
        if (totals.CompAmount.IsPositive) yield return TicketLayout.MoneyLine("ضيافة", $"-{totals.CompAmount.Egp} ج");
        if (totals.TaxAmount.IsPositive) yield return TicketLayout.MoneyLine(totals.TaxRuleName ?? "الضريبة", $"{totals.TaxAmount.Egp} ج");
        if (totals.DeliveryFee.IsPositive) yield return TicketLayout.MoneyLine("خدمة التوصيل", $"+{totals.DeliveryFee.Egp} ج");
        yield return TicketLayout.Rule();
        yield return TicketLayout.MoneyLine("الإجمالي", $"{totals.Total.Egp} ج", bold: true, size: TicketTextSize.Large);
        yield return TicketLayout.Rule();
        yield return TicketLayout.Field("طريقة الدفع", DescribePaymentMethod(totals.PaymentMethod));
        // The amount actually tendered is its own line, distinct from the change line below it.
        if (totals.AmountPaid.IsPositive) yield return TicketLayout.MoneyLine("المدفوع", $"{totals.AmountPaid.Egp} ج");
        if (totals.ChangeDue.IsPositive) yield return TicketLayout.MoneyLine("الباقي", $"{totals.ChangeDue.Egp} ج");
    }

    /// <summary>The one instruction the dispatch desk and the driver both act on, shaped identically
    /// on both delivery tickets. Said explicitly either way - "nothing to collect" is an instruction
    /// too, and leaving it unsaid is how a prepaid order gets charged twice.</summary>
    private static IEnumerable<TicketElement> ComposeCollectionInstruction(
        bool isCashOnDelivery, bool electronicPaymentPending, Money amountToCollect)
    {
        yield return TicketLayout.Rule();
        if (isCashOnDelivery)
        {
            yield return TicketLayout.MoneyLine("المطلوب تحصيله", $"{amountToCollect.Egp} ج",
                bold: true, size: TicketTextSize.Large);
        }
        else if (electronicPaymentPending)
        {
            yield return new TicketText("تحويل إلكتروني غير مؤكَّد", Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Large);
            yield return new TicketText("لا تُسلَّم قبل التأكيد", Bold: true, Align: TicketAlign.Center);
        }
        else
        {
            yield return new TicketText("مدفوع مسبقًا", Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Large);
            yield return new TicketText("لا يوجد مبلغ للتحصيل", Bold: true, Align: TicketAlign.Center);
        }
    }

    /// <summary>The requested collection time on a Cairo wall clock, which is the only clock the
    /// customer and the counter share. Null when nobody named one - printed as nothing at all rather
    /// than as an empty or guessed line.</summary>
    private static string? DescribeRequestedPickup(DateTimeOffset? requestedAtUtc) =>
        requestedAtUtc is not { } at ? null : $"موعد الاستلام: {FormatCairo(at)}";

    private static string FormatCairo(DateTimeOffset utc) =>
        TimeZoneInfo.ConvertTime(utc, BusinessClock.CairoTimeZone).ToString("yyyy-MM-dd HH:mm");

    /// <summary>The date as dd-MM-yyyy, led by an invisible left-to-right mark (U+200E).
    ///
    /// Without the mark, a date printed after an Arabic label ("التاريخ: 18-09-2026") comes out
    /// reversed as "2026-09-18": digits that follow Arabic letters are reclassified as Arabic numbers,
    /// and a hyphen does not bind Arabic-number segments together, so the three parts are laid out
    /// right to left. The mark is a strong left-to-right character, so the digits after it stay
    /// ordinary numbers and the whole date stays one left-to-right run. It prints nothing.</summary>
    private static string FormatCairoDate(DateTimeOffset utc) =>
        "\u200E" + TimeZoneInfo.ConvertTime(utc, BusinessClock.CairoTimeZone).ToString("dd-MM-yyyy");

    /// <summary>A 12-hour Cairo wall clock with an Arabic meridiem, which is how the counter and the
    /// customer both read the time. The ص/م marker is written explicitly rather than taken from a
    /// culture's own designator, so the ticket prints the same regardless of the locale the till
    /// happens to be running under.</summary>
    private static string FormatCairoTime(DateTimeOffset utc)
    {
        var local = TimeZoneInfo.ConvertTime(utc, BusinessClock.CairoTimeZone);
        return $"{local:hh:mm} {(local.Hour < 12 ? "ص" : "م")}";
    }

    private static string DescribeOrderType(OrderType orderType) => orderType switch
    {
        OrderType.Pickup => "استلام من المطعم",
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

    // ---- Reprint marking -------------------------------------------------------------------

    /// <summary>
    /// The same ticket, with "إعادة طباعة" stated at the top of EVERY physical piece it produces.
    ///
    /// Marking is applied to the composed content rather than to each composition, so no route can
    /// be reprinted silently: whatever the ticket is - a customer receipt, a kitchen ticket, a shift
    /// slip - the paper says it is a reproduction, and of what. Nothing about the order, its money
    /// or its state is touched; this only adds lines to a printout.
    /// </summary>
    public static ComposedTicket MarkAsReprint(ComposedTicket ticket, DateTimeOffset reprintedAtUtc, string byUserName)
    {
        var lines = new List<TicketElement>();
        var startOfPiece = true;
        foreach (var element in ticket.Lines)
        {
            if (startOfPiece)
            {
                lines.AddRange(ReprintBanner(reprintedAtUtc, byUserName));
                startOfPiece = false;
            }
            lines.Add(element);
            // A cut ends a physical piece: whatever follows starts the next one, and gets its own banner.
            if (element is TicketCut) startOfPiece = true;
        }
        if (lines.Count == 0) lines.AddRange(ReprintBanner(reprintedAtUtc, byUserName));
        return ticket with { Lines = lines };
    }

    private static IEnumerable<TicketElement> ReprintBanner(DateTimeOffset reprintedAtUtc, string byUserName)
    {
        yield return TicketLayout.CenteredLine("إعادة طباعة", bold: true, size: TicketTextSize.Title);
        yield return TicketLayout.CenteredLine(
            $"{FormatCairoDate(reprintedAtUtc)} - {FormatCairoTime(reprintedAtUtc)} - {byUserName}",
            size: TicketTextSize.Normal);
        yield return TicketLayout.ThinRule();
    }

    // ---- Shift and expense slips -----------------------------------------------------------

    /// <summary>The slip a cashier keeps when a shift is opened: who, when, and the float they were
    /// handed. Compact on purpose - it is a receipt for cash, not a report.</summary>
    public static ComposedTicket ComposeShiftOpenTicket(ShiftSlipIdentity shift, Money openingCash)
    {
        var lines = new List<TicketElement>();
        lines.AddRange(SlipHeading(shift.RestaurantName, shift.SubName, "افتتاح وردية"));
        lines.Add(TicketLayout.SlipLine("الكاشير", shift.CashierName));
        lines.Add(TicketLayout.SlipLine("الوردية", shift.ShiftName));
        lines.Add(TicketLayout.SlipLine("رقم الوردية", shift.ShiftReference));
        lines.Add(TicketLayout.SlipLine("التاريخ", FormatCairoDate(shift.OpenedAtUtc)));
        lines.Add(TicketLayout.SlipLine("وقت الافتتاح", FormatCairoTime(shift.OpenedAtUtc)));
        lines.Add(TicketLayout.ThinRule());
        lines.Add(TicketLayout.MoneyLine("رصيد افتتاحي", $"{openingCash.Egp} ج", bold: true, size: TicketTextSize.Title));
        lines.Add(TicketLayout.Space());
        lines.Add(TicketLayout.CenteredLine("توقيع الكاشير: ..............................", size: TicketTextSize.Normal));
        return new ComposedTicket(PrintRoute.Receipt, shift.ShiftReference, ShiftOpenLabel, lines);
    }

    /// <summary>
    /// The handover slip: every figure that produced the expected cash, then the count and the
    /// difference. The cashier has already entered their count by the time this prints - a blind
    /// close is about what they are shown BEFORE counting, which is nothing.
    /// </summary>
    public static ComposedTicket ComposeShiftCloseTicket(
        ShiftSlipIdentity shift, DateTimeOffset closedAtUtc, ShiftCashBreakdown cash,
        IReadOnlyList<ExpenseSummaryLine> expenses, IReadOnlyList<string> warnings, string? note)
    {
        var lines = new List<TicketElement>();
        lines.AddRange(SlipHeading(shift.RestaurantName, shift.SubName, "إغلاق وتسليم وردية"));
        lines.Add(TicketLayout.SlipLine("رقم الوردية", shift.ShiftReference));
        lines.Add(TicketLayout.SlipLine("الكاشير", shift.CashierName));
        // Date and times on their own lines: a date and a time together overflow the value column
        // and wrap, which on a handover slip reads as a mistake.
        lines.Add(TicketLayout.SlipLine("التاريخ", FormatCairoDate(shift.OpenedAtUtc)));
        lines.Add(TicketLayout.SlipLine("وقت الفتح", FormatCairoTime(shift.OpenedAtUtc)));
        lines.Add(TicketLayout.SlipLine("وقت الإغلاق", FormatCairoTime(closedAtUtc)));
        // Only when the shift ran past midnight, where the closing date is genuinely a different day.
        if (FormatCairoDate(closedAtUtc) != FormatCairoDate(shift.OpenedAtUtc))
            lines.Add(TicketLayout.SlipLine("تاريخ الإغلاق", FormatCairoDate(closedAtUtc)));

        lines.Add(TicketLayout.ThinRule());
        lines.Add(TicketLayout.MoneyLine("رصيد الافتتاح", $"{cash.OpeningCash.Egp} ج", size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("مبيعات الكاش", $"{cash.CashSales.Egp} ج", size: TicketTextSize.Large));
        if (cash.CashIn.IsPositive) lines.Add(TicketLayout.MoneyLine("إيداعات / إضافات", $"{cash.CashIn.Egp} ج", size: TicketTextSize.Large));
        if (cash.Expenses.IsPositive) lines.Add(TicketLayout.MoneyLine("المصاريف", $"{cash.Expenses.Egp} ج", size: TicketTextSize.Large));
        if (cash.Refunds.IsPositive) lines.Add(TicketLayout.MoneyLine("المرتجعات", $"{cash.Refunds.Egp} ج", size: TicketTextSize.Large));
        if (cash.Withdrawals.IsPositive) lines.Add(TicketLayout.MoneyLine("المسحوبات", $"{cash.Withdrawals.Egp} ج", size: TicketTextSize.Large));
        if (cash.SafeDrops.IsPositive) lines.Add(TicketLayout.MoneyLine("توريد للخزنة", $"{cash.SafeDrops.Egp} ج", size: TicketTextSize.Large));

        lines.Add(TicketLayout.ThinRule());
        lines.Add(TicketLayout.MoneyLine("النقدية المتوقعة", $"{cash.ExpectedCash.Egp} ج", bold: true, size: TicketTextSize.Title));
        lines.Add(TicketLayout.MoneyLine("النقدية الفعلية", $"{cash.ActualCash.Egp} ج", bold: true, size: TicketTextSize.Large));
        // No sign character: a bare "-" beside an Arabic currency word detaches from its digits and
        // prints on the wrong side. The word says which way it went.
        lines.Add(TicketLayout.MoneyLine(DescribeDifference(cash.Difference), $"{cash.Difference.Abs().Egp} ج",
            bold: true, size: TicketTextSize.Large));

        if (expenses.Count > 0)
        {
            lines.Add(TicketLayout.ThinRule());
            lines.Add(TicketLayout.CenteredLine("تفصيل المصاريف", bold: true, size: TicketTextSize.Normal));
            foreach (var line in expenses)
                lines.Add(TicketLayout.MoneyLine(line.Category, $"{line.Amount.Egp} ج", size: TicketTextSize.Large));
            lines.Add(TicketLayout.MoneyLine("إجمالي المصاريف", $"{cash.Expenses.Egp} ج", bold: true, size: TicketTextSize.Large));
        }

        if (!string.IsNullOrWhiteSpace(note))
        {
            lines.Add(TicketLayout.ThinRule());
            lines.Add(new TicketText($"ملاحظات: {note!.Trim()}", Size: TicketTextSize.Large));
        }

        if (warnings.Count > 0)
        {
            lines.Add(TicketLayout.ThinRule());
            lines.Add(TicketLayout.CenteredLine("تنبيهات قبل التسليم", bold: true));
            foreach (var warning in warnings)
                lines.Add(new TicketText($"- {warning}", Size: TicketTextSize.Large));
        }

        lines.Add(TicketLayout.Space());
        lines.Add(TicketLayout.SlipLine("تسليم الكاشير", "..................."));
        lines.Add(TicketLayout.SlipLine("استلام المسؤول", "..................."));
        return new ComposedTicket(PrintRoute.Receipt, shift.ShiftReference, ShiftCloseLabel, lines);
    }

    /// <summary>One expense as its own voucher: what was paid, for what, by whom, out of which
    /// account. Printing it posts nothing - the expense was already recorded when it was entered.</summary>
    public static ComposedTicket ComposeExpenseTicket(ExpenseSlipInfo expense)
    {
        var lines = new List<TicketElement>();
        lines.AddRange(SlipHeading(expense.RestaurantName, expense.SubName, "مصروف"));
        lines.Add(TicketLayout.MoneyLine("المبلغ", $"{expense.Amount.Egp} ج", bold: true, size: TicketTextSize.Title));
        lines.Add(TicketLayout.ThinRule());
        lines.Add(TicketLayout.SlipLine("التاريخ", FormatCairoDate(expense.CreatedAtUtc)));
        lines.Add(TicketLayout.SlipLine("الوقت", FormatCairoTime(expense.CreatedAtUtc)));
        lines.Add(TicketLayout.SlipLine("التصنيف", expense.Category));
        lines.Add(TicketLayout.SlipLine("طريقة الدفع", expense.PaymentMethodLabel));
        lines.Add(TicketLayout.SlipLine("الموظف", expense.EmployeeName));
        if (!string.IsNullOrWhiteSpace(expense.Description))
            lines.Add(new TicketText($"الوصف: {expense.Description.Trim()}", Size: TicketTextSize.Large));
        if (!string.IsNullOrWhiteSpace(expense.Notes))
            lines.Add(new TicketText($"ملاحظات: {expense.Notes!.Trim()}", Size: TicketTextSize.Large));
        lines.Add(TicketLayout.Space());
        lines.Add(TicketLayout.SlipLine("توقيع المستلم", "..................."));
        return new ComposedTicket(PrintRoute.Receipt, expense.Reference, ExpenseLabel, lines);
    }

    /// <summary>What the Printing screen calls each slip - these stand where an order number stands
    /// on an order's ticket, so a queue of jobs reads as words rather than as ids.</summary>
    public const string ShiftOpenLabel = "افتتاح وردية";
    public const string ShiftCloseLabel = "إغلاق وردية";
    public const string ExpenseLabel = "مصروف";

    private static IEnumerable<TicketElement> SlipHeading(string restaurantName, string? subName, string title)
    {
        yield return TicketLayout.BrandTitle(restaurantName);
        if (!string.IsNullOrWhiteSpace(subName)) yield return TicketLayout.Tagline(subName!.Trim());
        yield return TicketLayout.SlipTitle(title);
        yield return TicketLayout.ThinRule();
    }

    private static string DescribeDifference(Money difference) =>
        difference.IsZero ? "الفرق - مطابق" : difference.IsNegative ? "الفرق - عجز" : "الفرق - زيادة";
}
