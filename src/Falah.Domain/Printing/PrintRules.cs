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

        var lines = TicketLayout.InternalHeader(restaurantName, stationName, orderNumber).ToList();
        // Type and time share one line (paper redesign 2026-11-17): the larger typeface must not make
        // the ticket a station reads at a glance any longer than it was.
        lines.Add(TicketLayout.SlipLine(DescribeOrderType(orderType), FormatCairo(createdAtUtc), bold: true));

        // The one thing a prep station needs that the order type alone does not tell it: an order
        // booked for later must not be cooked now. Bold because it changes what the station does.
        if (DescribeRequestedPickup(requestedPickupAtUtc) is { } wanted)
            lines.Add(new TicketText(wanted, Bold: true));

        // A ruled grid with each note inside its item (Amendment 1); the grid draws its own lines.
        lines.Add(TicketLayout.ItemTableHeader(withPrice: false));
        foreach (var item in stationItems)
        {
            lines.Add(TicketLayout.ItemRow(item, withPrice: false));
            lines.AddRange(TicketLayout.ItemAnnotations(item, withPriceRule: false));
        }

        lines.AddRange(TicketLayout.Footer("يُسلَّم هذا البون للمسؤول عن المحطة", afterGrid: true));
        return new ComposedTicket(route, orderId, orderNumber, lines);
    }

    /// <summary>
    /// The two papers printed at checkout for the customer routes (paper redesign 2026-11-17, with
    /// Amendment 2): the takeaway receipt - one piece, led by the logo, no QR - and the call center's
    /// copy, which goes into the restaurant as a preparation paper without money.
    /// </summary>
    public static ComposedTicket ComposeReceiptTicket(PrintRoute route, ReceiptHeader header, IReadOnlyList<TicketItem> items, ReceiptTotals totals)
    {
        if (!CustomerRoutes.Contains(route)) throw new DomainException($"{route} ليس مسار إيصال عميل.");

        // Paper redesign 2026-11-17 (owner): the takeaway receipt is one piece; the customer collects
        // with the large order number on it. The call center's copy is printed when an order is taken
        // and goes INTO the restaurant (Amendment 2), so it is a preparation paper without money; the
        // paper that travels to the delivery customer is the driver's (ComposeDriverDispatchTicket).
        if (route == PrintRoute.CallCenter) return ComposeCallCenterPrepTicket(header, items);
        // Owner, 2026-11-18: a delivery taken at the till gets the same prep paper, on the till's own
        // printer - the delivery customer's receipt is the driver's paper, so a priced one here was a
        // second customer receipt.
        if (header.OrderType == OrderType.Delivery) return ComposeCallCenterPrepTicket(header, items) with { Route = route };
        var lines = ComposeCashierDetailSection(header, items, totals).ToList();
        return new ComposedTicket(route, header.OrderId, header.OrderNumber, lines);
    }

    /// <summary>Amendment 2 (owner): what the restaurant prepares from when an order is taken by phone.
    /// The number, who and where, the items and their notes - no logo, no prices, no totals, no QR.
    /// The unconfirmed-transfer warning stays: it tells the staff something, it is not a figure.</summary>
    private static ComposedTicket ComposeCallCenterPrepTicket(ReceiptHeader header, IReadOnlyList<TicketItem> items)
    {
        var lines = TicketLayout.InternalHeader(header.RestaurantName, "تحضير طلب", header.OrderNumber).ToList();
        if (header.ElectronicPaymentPending)
        {
            // Owner, 2026-11-17: the prep paper prints at once even when preparation is held for an
            // unconfirmed transfer, so it says plainly, on a black band, not to start yet.
            lines.Add(TicketLayout.SectionBand("لا يُحضَّر قبل تأكيد التحويل"));
            lines.Add(new TicketText("غير مدفوع - في انتظار تأكيد التحويل", Bold: true, Align: TicketAlign.Center));
        }
        lines.Add(TicketLayout.SlipLine(DescribeOrderType(header.OrderType), FormatCairo(header.CreatedAtUtc), bold: true));
        if (DescribeRequestedPickup(header.RequestedPickupAtUtc) is { } wanted)
            lines.Add(new TicketText(wanted, Bold: true));
        if (!string.IsNullOrWhiteSpace(header.CustomerName)) lines.Add(TicketLayout.Field("العميل", header.CustomerName!));
        if (header.OrderType == OrderType.Delivery)
        {
            if (!string.IsNullOrWhiteSpace(header.DeliveryArea)) lines.Add(TicketLayout.Field("المنطقة", header.DeliveryArea!));
            if (!string.IsNullOrWhiteSpace(header.DeliveryAddress)) lines.Add(TicketLayout.Field("العنوان", header.DeliveryAddress!));
        }
        lines.Add(TicketLayout.SectionBand("الأصناف"));
        lines.Add(TicketLayout.ItemTableHeader(withPrice: false));
        foreach (var item in items)
        {
            lines.Add(TicketLayout.ItemRow(item, withPrice: false));
            lines.AddRange(TicketLayout.ItemAnnotations(item, withPriceRule: false));
        }
        lines.AddRange(TicketLayout.Footer("بون تحضير - يُسلَّم للمطعم", afterGrid: true));
        return new ComposedTicket(PrintRoute.CallCenter, header.OrderId, header.OrderNumber, lines);
    }

    /// <summary>
    /// Amendment 2 (owner): the driver's paper is the delivery customer's receipt. Printed when the
    /// order is handed to a driver, it travels with him: the logo, the order and who it is for, the
    /// driver, the priced items, the totals, exactly what to collect, the delivery notes, and the
    /// menu QR. A reassignment says so at the top, so a second copy can never pass for the first.
    /// </summary>
    public static ComposedTicket ComposeDriverDispatchTicket(
        ReceiptHeader header, IReadOnlyList<TicketItem> items, ReceiptTotals totals, DeliveryOrderTicketInfo delivery,
        string driverName, DateTimeOffset assignedAtUtc, bool isReassignment)
    {
        var lines = TicketLayout.CustomerHeader(header).ToList();
        if (isReassignment)
            lines.Add(new TicketText("إعادة إسناد", Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Large));
        lines.AddRange(TicketLayout.LargeOrderNumber(header.OrderNumber));
        lines.Add(TicketLayout.SlipLine("السائق", driverName, bold: true));
        lines.Add(TicketLayout.SlipLine("وقت الإسناد", FormatCairo(assignedAtUtc)));
        if (!string.IsNullOrWhiteSpace(header.CustomerName)) lines.Add(TicketLayout.Field("العميل", header.CustomerName!));
        if (!string.IsNullOrWhiteSpace(delivery.CustomerPhone)) lines.Add(TicketLayout.Field("الهاتف", TicketFormat.Code(delivery.CustomerPhone!)));
        if (!string.IsNullOrWhiteSpace(header.DeliveryArea)) lines.Add(TicketLayout.Field("المنطقة", header.DeliveryArea!));
        if (!string.IsNullOrWhiteSpace(header.DeliveryAddress)) lines.Add(TicketLayout.Field("العنوان", header.DeliveryAddress!));

        lines.AddRange(ComposeCustomerItems(items));
        lines.Add(TicketLayout.SectionBand("الحساب"));
        lines.AddRange(ComposeCashierTotals(header, totals));
        lines.AddRange(ComposeCollectionInstruction(delivery.IsCashOnDelivery, header.ElectronicPaymentPending, delivery.AmountToCollect));
        if (!string.IsNullOrWhiteSpace(delivery.DeliveryNotes))
        {
            lines.Add(new TicketText("ملاحظات التوصيل:", Bold: true));
            lines.Add(new TicketText(delivery.DeliveryNotes!));
        }
        lines.AddRange(ComposeBrandFooter(header, withMenuQr: true));
        // The settings footer the call center's copy used to carry travels with the customer's paper.
        lines.Add(TicketLayout.Caption(string.IsNullOrWhiteSpace(header.Footer) ? "شكرًا لزيارتكم" : header.Footer!));
        return new ComposedTicket(PrintRoute.DriverDispatch, header.OrderId, header.OrderNumber, lines);
    }


    /// <summary>
    /// The takeaway receipt.
    ///
    /// The logo and brand, the order's identity, the items on a black band and in a ruled grid, then
    /// the money and the payment on their own band, and the brand once more as the sign-off. The menu
    /// QR is not here: it travels to the delivery customer on the driver's paper (Amendment 2).
    /// </summary>
    private static IEnumerable<TicketElement> ComposeCashierDetailSection(
        ReceiptHeader header, IReadOnlyList<TicketItem> items, ReceiptTotals totals)
    {
        // The brand, then the order's identity. Owner 2026-11-18: no logo on this paper (the driver's
        // paper keeps it) and no brand sign-off at the end - the name and sub-name print once, on top.
        foreach (var element in TicketLayout.CustomerHeader(header, withLogo: false)) yield return element;

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

        // 3. Items: a black section band, then a ruled grid with each note inside its item (Amendment 1).
        foreach (var element in ComposeCustomerItems(items)) yield return element;

        // 4 and 5. Totals, then payment, under their own band.
        yield return TicketLayout.SectionBand("الحساب");
        foreach (var element in ComposeCashierTotals(header, totals)) yield return element;
    }

    /// <summary>The priced item grid of a customer paper (the takeaway receipt and the driver's paper).</summary>
    private static IEnumerable<TicketElement> ComposeCustomerItems(IReadOnlyList<TicketItem> items)
    {
        yield return TicketLayout.SectionBand("الأصناف");
        yield return TicketLayout.CashierItemHeader();
        foreach (var item in items)
        {
            yield return TicketLayout.CashierItemRow(item);
            if (!string.IsNullOrWhiteSpace(item.Notes))
                yield return TicketLayout.CashierItemNote($"ملاحظات: {item.Notes}");
            // Printed from the order's own frozen snapshot, so a ticket reprinted after an offer is
            // edited or disabled still shows the offer that was actually applied at sale time.
            if (!string.IsNullOrWhiteSpace(item.PriceRuleName))
                yield return TicketLayout.CashierItemNote($"عرض: {item.PriceRuleName}"
                    + (item.ListUnitPrice is { IsPositive: true } list ? $" (بدل {TicketFormat.Amount(list)})" : ""));
        }
    }


    /// <summary>
    /// The end of the detailed receipt: the menu QR with its one-line invitation, then the name and
    /// tagline - and nothing else. No thank-you line and no slogan.
    ///
    /// The QR appears only when Settings holds a real menu link. There is no placeholder or guessed
    /// address: a code that opens the wrong page is worse than no code at all.
    /// </summary>
    private static IEnumerable<TicketElement> ComposeBrandFooter(ReceiptHeader header, bool withMenuQr)
    {
        yield return TicketLayout.ThinRule();
        if (withMenuQr && !string.IsNullOrWhiteSpace(header.MenuUrl))
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
        yield return TicketLayout.SlipLine("إجمالي الأصناف", TicketFormat.Amount(totals.Subtotal));
        if (totals.Discount.IsPositive) yield return TicketLayout.MoneyLine("الخصم", TicketFormat.Amount(totals.Discount), size: TicketTextSize.Large);
        if (totals.CompAmount.IsPositive) yield return TicketLayout.MoneyLine("ضيافة", TicketFormat.Amount(totals.CompAmount), size: TicketTextSize.Large);
        if (totals.DeliveryFee.IsPositive) yield return TicketLayout.MoneyLine("رسوم التوصيل", TicketFormat.Amount(totals.DeliveryFee), size: TicketTextSize.Large);
        if (totals.TaxAmount.IsPositive) yield return TicketLayout.MoneyLine(totals.TaxRuleName ?? "الضريبة", TicketFormat.Amount(totals.TaxAmount), size: TicketTextSize.Large);

        // A thin rule, then the grand total bold and a size up - the strongest line on the ticket,
        // with no filled bar behind it.
        yield return TicketLayout.MoneyLine("الإجمالي الكلي", TicketFormat.Amount(totals.Total), bold: true, size: TicketTextSize.Title);

        // Read from the order's own frozen electronic-payment flag, never inferred from the amounts.
        var isPaid = header.IsPaid ?? (!header.ElectronicPaymentPending && totals.AmountPaid >= totals.Total);
        var paymentState = header.ElectronicPaymentPending ? "لم يُؤكَّد"
            : isPaid ? "مدفوع"
            : CheckoutRules.IsCashOnDelivery(header.OrderType, totals.PaymentMethod, totals.Total) ? "عند الاستلام" : "غير مدفوع";
        yield return TicketLayout.MoneyLine($"طريقة الدفع: {DescribePaymentMethod(totals.PaymentMethod)}", paymentState, size: TicketTextSize.Large);
        if (totals.AmountPaid.IsPositive) yield return TicketLayout.MoneyLine("المدفوع", TicketFormat.Amount(totals.AmountPaid), size: TicketTextSize.Large);
        if (totals.ChangeDue.IsPositive) yield return TicketLayout.MoneyLine("الباقي", TicketFormat.Amount(totals.ChangeDue), bold: true, size: TicketTextSize.Large);
    }


    /// <summary>The delivery ORDER ticket - printed once when a delivery order is created at
    /// checkout, before any driver exists. Distinct from the driver dispatch ticket (printed later,
    /// at assignment, naming a driver) and from the customer-facing ticket. Carries the full order
    /// so the dispatch desk can work it end to end: who it is for, where it goes, what is in it,
    /// and exactly what will have to be collected - or that there is nothing to collect.</summary>
    public static ComposedTicket ComposeDeliveryOrderTicket(
        ReceiptHeader header, IReadOnlyList<TicketItem> items, ReceiptTotals totals, DeliveryOrderTicketInfo delivery)
    {
        var lines = TicketLayout.InternalHeader(header.RestaurantName, "أوردر دليفري", header.OrderNumber).ToList();
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

        lines.AddRange(TicketLayout.Footer("بون تشغيل - ليس إيصال العميل"));
        return new ComposedTicket(PrintRoute.DeliveryOrder, header.OrderId, header.OrderNumber, lines);
    }


    private static IEnumerable<TicketElement> ComposeItemTable(IReadOnlyList<TicketItem> items, bool withPrice)
    {
        yield return TicketLayout.SectionBand("الأصناف");
        yield return TicketLayout.ItemTableHeader(withPrice);
        foreach (var item in items)
        {
            yield return TicketLayout.ItemRow(item, withPrice);
            // Printed from the order's own frozen snapshot, so a ticket reprinted after an offer is
            // edited or disabled still shows the offer that was actually applied at sale time.
            foreach (var annotation in TicketLayout.ItemAnnotations(item, withPriceRule: withPrice)) yield return annotation;
        }
    }

    private static IEnumerable<TicketElement> ComposeTotals(ReceiptTotals totals)
    {
        yield return TicketLayout.SectionBand("الحساب");
        yield return TicketLayout.MoneyLine("المجموع الفرعي", TicketFormat.Amount(totals.Subtotal));
        if (totals.Discount.IsPositive) yield return TicketLayout.MoneyLine("الخصم", "-" + TicketFormat.Amount(totals.Discount));
        if (totals.CompAmount.IsPositive) yield return TicketLayout.MoneyLine("ضيافة", "-" + TicketFormat.Amount(totals.CompAmount));
        if (totals.TaxAmount.IsPositive) yield return TicketLayout.MoneyLine(totals.TaxRuleName ?? "الضريبة", TicketFormat.Amount(totals.TaxAmount));
        if (totals.DeliveryFee.IsPositive) yield return TicketLayout.MoneyLine("خدمة التوصيل", "+" + TicketFormat.Amount(totals.DeliveryFee));
        yield return TicketLayout.Rule();
        yield return TicketLayout.MoneyLine("الإجمالي", TicketFormat.Amount(totals.Total), bold: true, size: TicketTextSize.Large);
        yield return TicketLayout.Rule();
        yield return TicketLayout.Field("طريقة الدفع", DescribePaymentMethod(totals.PaymentMethod));
        // The amount actually tendered is its own line, distinct from the change line below it.
        if (totals.AmountPaid.IsPositive) yield return TicketLayout.MoneyLine("المدفوع", TicketFormat.Amount(totals.AmountPaid));
        if (totals.ChangeDue.IsPositive) yield return TicketLayout.MoneyLine("الباقي", TicketFormat.Amount(totals.ChangeDue));
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
            yield return TicketLayout.MoneyLine("المطلوب تحصيله", TicketFormat.Amount(amountToCollect),
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

    // Paper redesign 2026-11-17: one date/time format on every paper (TicketFormat). These wrappers
    // keep the call sites unchanged.
    private static string FormatCairo(DateTimeOffset utc) => TicketFormat.DateTime(utc);

    /// <summary>The date as dd-MM-yyyy, led by an invisible left-to-right mark (U+200E).
    ///
    /// Without the mark, a date printed after an Arabic label ("التاريخ: 18-09-2026") comes out
    /// reversed as "2026-09-18": digits that follow Arabic letters are reclassified as Arabic numbers,
    /// and a hyphen does not bind Arabic-number segments together, so the three parts are laid out
    /// right to left. The mark is a strong left-to-right character, so the digits after it stay
    /// ordinary numbers and the whole date stays one left-to-right run. It prints nothing.</summary>
    private static string FormatCairoDate(DateTimeOffset utc) => TicketFormat.Date(utc);

    /// <summary>A 12-hour Cairo wall clock with an Arabic meridiem, which is how the counter and the
    /// customer both read the time. The ص/م marker is written explicitly rather than taken from a
    /// culture's own designator, so the ticket prints the same regardless of the locale the till
    /// happens to be running under.</summary>
    private static string FormatCairoTime(DateTimeOffset utc) => TicketFormat.Time(utc);

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
        lines.Add(TicketLayout.MoneyLine("رصيد افتتاحي", TicketFormat.Amount(openingCash), bold: true, size: TicketTextSize.Title));
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
        lines.Add(TicketLayout.SlipLine("الوردية", shift.ShiftName));
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
        lines.Add(TicketLayout.MoneyLine("رصيد الافتتاح", TicketFormat.Amount(cash.OpeningCash), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("مبيعات الكاش", TicketFormat.Amount(cash.CashSales), size: TicketTextSize.Large));
        if (cash.CashIn.IsPositive) lines.Add(TicketLayout.MoneyLine("إيداعات / إضافات", TicketFormat.Amount(cash.CashIn), size: TicketTextSize.Large));
        if (cash.Expenses.IsPositive) lines.Add(TicketLayout.MoneyLine("المصاريف", TicketFormat.Amount(cash.Expenses), size: TicketTextSize.Large));
        if (cash.Refunds.IsPositive) lines.Add(TicketLayout.MoneyLine("المرتجعات", TicketFormat.Amount(cash.Refunds), size: TicketTextSize.Large));
        if (cash.Withdrawals.IsPositive) lines.Add(TicketLayout.MoneyLine("المسحوبات", TicketFormat.Amount(cash.Withdrawals), size: TicketTextSize.Large));
        if (cash.SafeDrops.IsPositive) lines.Add(TicketLayout.MoneyLine("توريد للخزنة", TicketFormat.Amount(cash.SafeDrops), size: TicketTextSize.Large));

        lines.Add(TicketLayout.ThinRule());
        lines.Add(TicketLayout.MoneyLine("النقدية المتوقعة", TicketFormat.Amount(cash.ExpectedCash), bold: true, size: TicketTextSize.Title));
        lines.Add(TicketLayout.MoneyLine("النقدية الفعلية", TicketFormat.Amount(cash.ActualCash), bold: true, size: TicketTextSize.Large));
        // No sign character: a bare "-" beside an Arabic currency word detaches from its digits and
        // prints on the wrong side. The word says which way it went.
        lines.Add(TicketLayout.MoneyLine(DescribeDifference(cash.Difference), TicketFormat.Amount(cash.Difference.Abs()),
            bold: true, size: TicketTextSize.Large));

        if (expenses.Count > 0)
        {
            lines.Add(TicketLayout.ThinRule());
            lines.Add(TicketLayout.CenteredLine("تفصيل المصاريف", bold: true, size: TicketTextSize.Normal));
            foreach (var line in expenses)
                lines.Add(TicketLayout.MoneyLine(line.Category, TicketFormat.Amount(line.Amount), size: TicketTextSize.Large));
            lines.Add(TicketLayout.MoneyLine("إجمالي المصاريف", TicketFormat.Amount(cash.Expenses), bold: true, size: TicketTextSize.Large));
        }

        if (!string.IsNullOrWhiteSpace(note))
        {
            lines.Add(TicketLayout.ThinRule());
            lines.Add(new TicketText($"ملاحظات: {note!.Trim()}", Size: TicketTextSize.Large));
        }

        if (warnings.Count > 0)
        {
            lines.Add(TicketLayout.ThinRule());
            lines.Add(TicketLayout.SectionBand("تنبيهات قبل التسليم"));
            foreach (var warning in warnings)
                lines.Add(new TicketText($"- {warning}", Size: TicketTextSize.Large));
        }

        lines.Add(TicketLayout.Space());
        lines.Add(TicketLayout.SlipLine("تسليم الكاشير", "..................."));
        lines.Add(TicketLayout.SlipLine("استلام المسؤول", "..................."));
        return new ComposedTicket(PrintRoute.Receipt, shift.ShiftReference, ShiftCloseLabel, lines);
    }

    /// <summary>
    /// UI-REQ-002: the full closing and handover statement, from the statement the close froze.
    /// Sections: identity, the shift's sales (shared report definitions), how they were paid, every
    /// expense as entered, the drawer reconciliation, what was handed over and to where, what is still
    /// open, and signatures. <paramref name="recomputed"/> marks a statement rebuilt from today's
    /// records because the shift was closed before statements were frozen; the paper says so.
    /// </summary>
    public static ComposedTicket ComposeShiftCloseStatement(ShiftCloseStatement s, bool recomputed = false)
    {
        static string Egp(long piastres) => TicketFormat.Amount(Money.FromPiastres(piastres));
        var lines = new List<TicketElement>();
        lines.AddRange(SlipHeading(s.RestaurantName, s.SubName, "إغلاق وتسليم وردية"));
        if (recomputed)
            lines.Add(TicketLayout.CenteredLine("محسوبة من السجلات الحالية - أُغلقت قبل حفظ ورقة الإغلاق", size: TicketTextSize.Normal));

        lines.Add(TicketLayout.SlipLine("رقم الوردية", s.ShiftReference));
        lines.Add(TicketLayout.SlipLine("الوردية", s.ShiftName));
        // Same day-month-year order as the opening/closing lines below, so one paper reads one way.
        lines.Add(TicketLayout.SlipLine("يوم العمل", "\u200E" + s.BusinessDate.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture)));
        lines.Add(TicketLayout.SlipLine("الكاشير", s.CashierName));
        if (s.ClosedByName != s.CashierName) lines.Add(TicketLayout.SlipLine("أغلقها", s.ClosedByName));
        lines.Add(TicketLayout.SlipLine("الفتح", FormatCairo(s.OpenedAtUtc)));
        lines.Add(TicketLayout.SlipLine("الإغلاق", FormatCairo(s.ClosedAtUtc)));

        var sales = s.Sales;
        lines.Add(TicketLayout.ThinRule());
        lines.Add(TicketLayout.SectionBand("المبيعات"));
        lines.Add(TicketLayout.MoneyLine("عدد الطلبات", sales.OrderCount.ToString(System.Globalization.CultureInfo.InvariantCulture), size: TicketTextSize.Large));
        if (sales.CancelledOrderCount > 0)
            lines.Add(TicketLayout.MoneyLine("طلبات ملغاة (خارج المبيعات)", sales.CancelledOrderCount.ToString(System.Globalization.CultureInfo.InvariantCulture), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("المبيعات بأسعار القائمة", Egp(sales.ListSalesPiastres), size: TicketTextSize.Large));
        if (sales.OffersPiastres > 0) lines.Add(TicketLayout.MoneyLine("قيمة العروض", Egp(sales.OffersPiastres), size: TicketTextSize.Large));
        if (sales.CompsPiastres > 0) lines.Add(TicketLayout.MoneyLine("الضيافة", Egp(sales.CompsPiastres), size: TicketTextSize.Large));
        if (sales.ManualDiscountsPiastres > 0) lines.Add(TicketLayout.MoneyLine("خصم يدوي", Egp(sales.ManualDiscountsPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("إجمالي المبيعات", Egp(sales.GrossSalesPiastres), bold: true, size: TicketTextSize.Large));
        if (sales.RefundsPiastres > 0) lines.Add(TicketLayout.MoneyLine("المرتجعات", Egp(sales.RefundsPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("صافي المبيعات", Egp(sales.NetSalesPiastres), bold: true, size: TicketTextSize.Large));

        lines.Add(TicketLayout.ThinRule());
        lines.Add(TicketLayout.SectionBand("طرق الدفع"));
        lines.Add(TicketLayout.MoneyLine("نقدًا", Egp(sales.CashPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("بطاقة", Egp(sales.CardPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("إنستاباي (مؤكد)", Egp(sales.InstaPayPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("محفظة (مؤكد)", Egp(sales.WalletPiastres), size: TicketTextSize.Large));
        if (sales.PendingElectronicPiastres > 0)
            lines.Add(TicketLayout.MoneyLine("تحويلات لم تُؤكَّد بعد", Egp(sales.PendingElectronicPiastres), size: TicketTextSize.Large));
        if (sales.CashWithDriversPiastres > 0)
            lines.Add(TicketLayout.MoneyLine("نقدية مع السائقين لم تُسوَّ", Egp(sales.CashWithDriversPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("المحصّل فعليًا", Egp(sales.CollectedPiastres), bold: true, size: TicketTextSize.Large));

        if (s.Expenses.Count > 0)
        {
            lines.Add(TicketLayout.ThinRule());
            lines.Add(TicketLayout.SectionBand("المصروفات"));
            foreach (var e in s.Expenses)
            {
                lines.Add(TicketLayout.MoneyLine(e.Category, Egp(e.AmountPiastres), size: TicketTextSize.Large));
                var detail = string.IsNullOrWhiteSpace(e.Description) ? "" : e.Description.Trim() + " - ";
                lines.Add(new TicketText($"  {detail}{(e.PaidFromDrawer ? "من الدرج" : "من " + e.PaymentAccountName)} - {e.CreatedByName} {FormatCairoTime(e.CreatedAtUtc)}", Size: TicketTextSize.Normal));
            }
            lines.Add(TicketLayout.MoneyLine("مصروفات من الدرج", Egp(s.DrawerExpensesPiastres), bold: true, size: TicketTextSize.Large));
            if (s.OtherAccountExpensesPiastres > 0)
                lines.Add(TicketLayout.MoneyLine("مصروفات من حسابات أخرى", Egp(s.OtherAccountExpensesPiastres), size: TicketTextSize.Large));
        }

        var cash = s.Cash;
        lines.Add(TicketLayout.ThinRule());
        lines.Add(TicketLayout.SectionBand("نقدية الدرج"));
        if (cash.InheritedFloatPiastres is { } inherited)
        {
            lines.Add(TicketLayout.MoneyLine("عهدة متروكة من الوردية السابقة", Egp(inherited), size: TicketTextSize.Large));
            if (inherited != cash.OpeningPiastres)
                lines.Add(TicketLayout.MoneyLine(cash.OpeningPiastres < inherited ? "عجز عند الاستلام (مسجل منفصلًا)" : "زيادة عند الاستلام (مسجلة منفصلًا)",
                    Egp(Math.Abs(cash.OpeningPiastres - inherited)), size: TicketTextSize.Large));
        }
        lines.Add(TicketLayout.MoneyLine("رصيد الافتتاح", Egp(cash.OpeningPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("مبيعات الكاش", Egp(cash.CashSalesPiastres), size: TicketTextSize.Large));
        if (cash.CashInPiastres > 0) lines.Add(TicketLayout.MoneyLine("إيداعات / إضافات من الخزنة", Egp(cash.CashInPiastres), size: TicketTextSize.Large));
        if (cash.DriverCashPiastres > 0) lines.Add(TicketLayout.MoneyLine("توريدات السائقين", Egp(cash.DriverCashPiastres), size: TicketTextSize.Large));
        if (cash.DriverPayoutsPiastres > 0) lines.Add(TicketLayout.MoneyLine("صرف أجر توصيل للسائقين", Egp(cash.DriverPayoutsPiastres), size: TicketTextSize.Large));
        if (cash.RefundsPiastres > 0) lines.Add(TicketLayout.MoneyLine("مرتجعات نقدية من الدرج", Egp(cash.RefundsPiastres), size: TicketTextSize.Large));
        if (cash.DrawerExpensesPiastres > 0) lines.Add(TicketLayout.MoneyLine("مصروفات من الدرج", Egp(cash.DrawerExpensesPiastres), size: TicketTextSize.Large));
        if (cash.WithdrawalsPiastres > 0) lines.Add(TicketLayout.MoneyLine("المسحوبات", Egp(cash.WithdrawalsPiastres), size: TicketTextSize.Large));
        if (cash.SafeDropsPiastres > 0) lines.Add(TicketLayout.MoneyLine("توريد للخزنة", Egp(cash.SafeDropsPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("النقدية المتوقعة", Egp(cash.ExpectedPiastres), bold: true, size: TicketTextSize.Title));
        lines.Add(TicketLayout.MoneyLine("النقدية الفعلية", Egp(cash.CountedPiastres), bold: true, size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine(DescribeDifference(Money.FromPiastres(cash.DifferencePiastres)), Egp(Math.Abs(cash.DifferencePiastres)),
            bold: true, size: TicketTextSize.Large));
        if (!string.IsNullOrWhiteSpace(s.DifferenceReason))
            lines.Add(new TicketText($"سبب الفرق: {s.DifferenceReason!.Trim()}", Size: TicketTextSize.Large));
        if (!string.IsNullOrWhiteSpace(s.ApprovedByName))
            lines.Add(TicketLayout.SlipLine("اعتمد الفرق", s.ApprovedByName!));

        lines.Add(TicketLayout.ThinRule());
        lines.Add(TicketLayout.SectionBand("التسليم"));
        lines.Add(TicketLayout.MoneyLine($"سُلِّم إلى {cash.HandoverDestinationName}", Egp(cash.HandedOverPiastres), bold: true, size: TicketTextSize.Large));
        if (!string.IsNullOrWhiteSpace(cash.HandoverReceiverName)) lines.Add(TicketLayout.SlipLine("المستلم", cash.HandoverReceiverName!));
        if (cash.RetainedFloatPiastres > 0)
            lines.Add(TicketLayout.MoneyLine("عهدة متروكة في الدرج للوردية التالية", Egp(cash.RetainedFloatPiastres), bold: true, size: TicketTextSize.Large));

        lines.Add(TicketLayout.ThinRule());
        lines.Add(TicketLayout.SectionBand("بنود مفتوحة عند التسليم"));
        if (s.Outstanding.Count == 0)
            lines.Add(TicketLayout.CenteredLine("لا يوجد", size: TicketTextSize.Normal));
        foreach (var item in s.Outstanding)
            lines.Add(new TicketText($"- {item}", Size: TicketTextSize.Large));

        lines.Add(TicketLayout.Space());
        lines.Add(TicketLayout.SlipLine("تسليم الكاشير", "..................."));
        lines.Add(TicketLayout.SlipLine("استلام المسؤول", "..................."));
        return new ComposedTicket(PrintRoute.Receipt, s.ShiftReference, ShiftCloseLabel, lines);
    }

    public const string DriverStatementLabel = "كشف سائق";

    /// <summary>Arabic label of a driver-statement payment filter.</summary>
    public static string DriverStatementFilterLabel(string? filter) => filter switch
    {
        "cash" => "كاش", "card" => "بطاقة", "instapay" => "إنستاباي", "wallet" => "محفظة", "multi" => "دفع مقسّم",
        _ => "كل طرق الدفع",
    };

    /// <summary>
    /// UI-REQ-001: one driver's daily statement on paper - the header (driver, business day, payment
    /// filter, who printed it and when), the summary, and with <paramref name="detailed"/> one block per
    /// order. Built from the same rows and totals the screen shows (DriverStatementRules.Totals).
    /// Delivery charges are what the customer paid for delivery; on a delivered order that full charge is the
    /// driver's earning (owner rule 2026-11-08), and the paper shows it and the net after it.
    /// </summary>
    public static ComposedTicket ComposeDriverStatement(
        string restaurantName, string? subName, string driverName, DateOnly businessDate, string? paymentFilter,
        IReadOnlyList<Falah.Domain.Delivery.DriverStatementOrder> orders, Falah.Domain.Delivery.DriverStatementTotals totals,
        bool detailed, string printedBy, DateTimeOffset printedAtUtc)
    {
        static string Egp(long piastres) => TicketFormat.Amount(Money.FromPiastres(piastres));
        var lines = new List<TicketElement>();
        lines.AddRange(SlipHeading(restaurantName, subName, detailed ? "كشف يومية سائق - تفصيلي" : "كشف يومية سائق - ملخص"));
        lines.Add(TicketLayout.SlipLine("السائق", driverName));
        lines.Add(TicketLayout.SlipLine("يوم العمل", "‎" + businessDate.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture)));
        lines.Add(TicketLayout.SlipLine("طريقة الدفع", DriverStatementFilterLabel(paymentFilter)));
        lines.Add(TicketLayout.SlipLine("طُبع بواسطة", printedBy));
        lines.Add(TicketLayout.SlipLine("وقت الطباعة", FormatCairo(printedAtUtc)));

        lines.Add(TicketLayout.ThinRule());
        lines.Add(TicketLayout.MoneyLine("عدد الفواتير", totals.InvoiceCount.ToString(System.Globalization.CultureInfo.InvariantCulture), bold: true, size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("المبيعات بدون خدمة التوصيل", Egp(totals.FoodSalesPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("خدمة التوصيل (من العملاء)", Egp(totals.DeliveryFeesPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("الإجمالي", Egp(totals.OrderTotalPiastres), bold: true, size: TicketTextSize.Title));
        if (totals.RefundedPiastres > 0) lines.Add(TicketLayout.MoneyLine("مرتجعات على هذه الطلبات", Egp(totals.RefundedPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.ThinRule());
        lines.Add(TicketLayout.MoneyLine("مدفوع مسبقًا", Egp(totals.PrepaidPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("على السائق تحصيله", Egp(totals.CashToCollectPiastres), bold: true, size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("حصّله السائق", Egp(totals.CollectedPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("منه تمت تسويته", Egp(totals.CollectedSettledPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("منه لم يُسوَّ بعد", Egp(totals.CollectedUnsettledPiastres), bold: true, size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine("أجر التوصيل المستحق للسائق", Egp(totals.DriverFeesEarnedPiastres), size: TicketTextSize.Large));
        lines.Add(TicketLayout.MoneyLine(totals.NetAfterFeesPiastres >= 0 ? "صافي على السائق بعد أجره" : "صافي مستحق للسائق",
            Egp(Math.Abs(totals.NetAfterFeesPiastres)), bold: true, size: TicketTextSize.Large));
        if (totals.FailedOrReturnedCount > 0)
            lines.Add(TicketLayout.MoneyLine("توصيل فشل / مرتجع", totals.FailedOrReturnedCount.ToString(System.Globalization.CultureInfo.InvariantCulture), size: TicketTextSize.Large));
        if (totals.CancelledCount > 0)
            lines.Add(TicketLayout.MoneyLine("طلبات ملغاة (خارج المجاميع)", totals.CancelledCount.ToString(System.Globalization.CultureInfo.InvariantCulture), size: TicketTextSize.Large));

        if (detailed)
        {
            foreach (var o in orders)
            {
                lines.Add(TicketLayout.ThinRule());
                lines.Add(TicketLayout.MoneyLine($"{o.OrderNumber} - {FormatCairoTime(o.OrderedAtUtc)}", Egp(o.OrderTotalPiastres), bold: true, size: TicketTextSize.Large));
                if (!string.IsNullOrWhiteSpace(o.CustomerName) || !string.IsNullOrWhiteSpace(o.CustomerPhone))
                    lines.Add(new TicketText($"{o.CustomerName} {o.CustomerPhone}".Trim(), Size: TicketTextSize.Normal));
                if (!string.IsNullOrWhiteSpace(o.Address)) lines.Add(new TicketText(o.Address!, Size: TicketTextSize.Normal));
                lines.Add(new TicketText($"أكل {Egp(o.FoodSalesPiastres)} + توصيل {Egp(o.DeliveryFeePiastres)} - {DriverStatementFilterLabel(o.PaymentMethod)} - {DescribeDeliveryStatus(o.DeliveryStatus)}", Size: TicketTextSize.Normal));
                lines.Add(new TicketText($"يحصّل {Egp(o.CashToCollectPiastres)} - حصّل {Egp(o.CollectedPiastres)}{(o.CustodySettled ? " - مُسوّى" : "")}", Size: TicketTextSize.Normal));
                if (o.RefundedPiastres > 0) lines.Add(new TicketText($"مرتجع {Egp(o.RefundedPiastres)}", Size: TicketTextSize.Normal));
                if (!string.IsNullOrWhiteSpace(o.ReassignedFromName)) lines.Add(new TicketText($"أُعيد إسناده من {o.ReassignedFromName}", Size: TicketTextSize.Normal));
                if (o.Cancelled) lines.Add(new TicketText("ملغى - خارج المجاميع", Size: TicketTextSize.Normal));
            }
        }

        lines.Add(TicketLayout.Space());
        lines.Add(TicketLayout.SlipLine("توقيع السائق", "..................."));
        lines.Add(TicketLayout.SlipLine("توقيع المسؤول", "..................."));
        return new ComposedTicket(PrintRoute.Receipt, driverName, DriverStatementLabel, lines);
    }

    private static string DescribeDeliveryStatus(string status) => status switch
    {
        "Delivered" => "تم التسليم", "Failed" => "فشل", "Returned" => "مرتجع", "OutForDelivery" => "مع السائق",
        "Assigned" => "مُسند", "Settled" => "مُسوّى", "Cancelled" => "ملغى", "Ready" => "جاهز", _ => status,
    };

    /// <summary>One expense as its own voucher: what was paid, for what, by whom, out of which
    /// account. Printing it posts nothing - the expense was already recorded when it was entered.</summary>
    public static ComposedTicket ComposeExpenseTicket(ExpenseSlipInfo expense)
    {
        var lines = new List<TicketElement>();
        lines.AddRange(SlipHeading(expense.RestaurantName, expense.SubName, "مصروف"));
        lines.Add(TicketLayout.MoneyLine("المبلغ", TicketFormat.Amount(expense.Amount), bold: true, size: TicketTextSize.Title));
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

    /// <summary>Every slip opens with the shared internal header (paper redesign 2026-11-17). The
    /// sub-name is the customer header's line; internal papers carry the name and their own title.</summary>
    private static IEnumerable<TicketElement> SlipHeading(string restaurantName, string? subName, string title) =>
        TicketLayout.InternalHeader(restaurantName, title);

    private static string DescribeDifference(Money difference) =>
        difference.IsZero ? "الفرق - مطابق" : difference.IsNegative ? "الفرق - عجز" : "الفرق - زيادة";
}
