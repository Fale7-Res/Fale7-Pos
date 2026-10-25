# Substantive Callable Audit: `src/Falah.Domain/Printing/PrintRules.cs`

- **Source File:** `src/Falah.Domain/Printing/PrintRules.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `1d728f9f30593703f473a603eba63945c3859cb8398457beea5b9521fd3d9dc9`
- **Auditor Role:** Printing & Operational Workflow Specialist
- **Total File Callables:** **27** Methods (+ 2 Routing Route Sets & 3 Slip Label Constants)
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

[`PrintRules`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs) is the central pure-domain layout composition authority for the entire Fale7 POS restaurant operations. It establishes the semantic content, hierarchical structure, typography, and sequence for every ticket type emitted across the restaurant's physical topology:
1. **Cashier Till Printer (`PrintRoute.Receipt`):** Emits a dual ticket separated by a mechanical knife cut (`TicketCut`):
   - *Piece 1 (Customer Token):* Compact token carrying brand, order type, huge order number, timestamp, cashier name, and pickup instructions.
   - *Piece 2 (Cashier Detail Ticket):* Complete prep and fiscal voucher containing brand header, order identity, itemized order lines with applied promotions, financial totals, payment confirmation, and menu QR code.
2. **Call Center Printer (`PrintRoute.CallCenter`):** Emits the single detailed order ticket for telephone/remote dispatch.
3. **Station Prep Printers (`PrintRoute.Charcoal`, `PrintRoute.Grill`):** Emits kitchen tickets strictly partitioned by station. Items destined for charcoal are never visible to the grill station, and vice versa. Prices are strictly suppressed to eliminate pass confusion.
4. **Delivery Dispatch Desk (`PrintRoute.DeliveryOrder`):** Emits packing and assembly slips before driver assignment.
5. **Driver Handover (`PrintRoute.DriverDispatch`):** Emits driver assignment tickets indicating cash collection instructions or prepaid status, with reassignment banners when applicable.
6. **Operational Shift & Treasury Slips:** Emits shift opening vouchers, blind closing handover slips, and expense payment vouchers.

All methods in [`PrintRules`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs) are pure domain functions: they consume frozen value snapshots (`ReceiptHeader`, `ReceiptTotals`, `TicketItem`, `DeliveryOrderTicketInfo`, `ShiftSlipIdentity`) and return immutable [`ComposedTicket`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/ComposedTicket.cs) models without performing I/O, database queries, or hardware mutations.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `PrintRules.DetermineKitchenRoutes(IEnumerable<string?>)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:24:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `24-30`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Filters and maps item preparation station identifiers into a distinct set of active kitchen print routes:
- `itemPreparationStations` (`IEnumerable<string?>`): Collection of preparation station strings from order line items.
- **Algorithm:** Filters null/empty strings $\rightarrow$ parses to `PrintRoute` enum $\rightarrow$ validates against `KitchenRoutes` (`Charcoal`, `Grill`) $\rightarrow$ converts to `HashSet<PrintRoute>`.
- **Return Value:** `IReadOnlySet<PrintRoute>`. Returns an empty set if no items require kitchen prep (e.g. drinks, desserts, pre-packaged retail items).

**B. Callers, Dependencies & Side Effects:**  
Called by checkout and order submission handlers (`CheckoutCommandHandler`, `OrderCreationService`). Pure query; no side effects.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Prevents blank or unneeded ticket spooling. If an order contains only retail drinks, no print job is queued for the kitchen printers, preserving paper and preventing cook disruption.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Pure function, thread-safe.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `PrintRulesTests.DetermineKitchenRoutes_returns_only_matching_stations`.
- *Missing Tests:* Verification with malformed or case-insensitive station strings (e.g. `"charcoal"`, `"GRILL"`).

> **Verbatim Code Excerpt (Lines 24-30):**
```csharp
    public static IReadOnlySet<PrintRoute> DetermineKitchenRoutes(IEnumerable<string?> itemPreparationStations) =>
        itemPreparationStations
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => Enum.TryParse<PrintRoute>(s, out var route) ? route : (PrintRoute?)null)
            .Where(r => r is not null && KitchenRoutes.Contains(r.Value))
            .Select(r => r!.Value)
            .ToHashSet();
```

---

<a id="callable-2"></a>
### Callable 2: `PrintRules.ComposeKitchenTicket(PrintRoute, string, string, string, OrderType, IReadOnlyList<TicketItem>, DateTimeOffset, DateTimeOffset?)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:36:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `36-76`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes a station-specific prep ticket containing only items assigned to `route`:
- Validates `KitchenRoutes.Contains(route)`; throws `DomainException($"{route} ليست محطة تجهيز.")` if invalid.
- Filters items: `items.Where(i => i.PreparationStation == route.ToString()).ToList()`.
- Maps station title: `Charcoal` $\rightarrow$ `"الفحم"`, `Grill` $\rightarrow$ `"الجريل"`.
- Composes ticket elements: Brand title, large station name, huge order number, order type, Cairo timestamp, requested pickup time (if specified), item table with `withPrice: false`, notes/modifiers, and bottom routing caption `"يُسلَّم هذا البون للمسؤول عن المحطة"`.
- Returns `new ComposedTicket(route, orderId, orderNumber, lines)`.

**B. Callers, Dependencies & Side Effects:**  
Called by `PrintJobCreationService` during kitchen routing. Pure domain composer.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Price Masking Invariant:** Prices, discounts, and payment methods are strictly excluded (`withPrice: false`). Kitchen cooks only see item quantities, names, notes, and modifiers.
- **Station Filtering Invariant:** Charcoal items never print on the Grill ticket; Grill items never print on the Charcoal ticket.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Thread-safe, immutable output.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `PrintRulesTests.ComposeKitchenTicket_filters_items_by_station_and_omits_prices`.
- *Missing Tests:* Verification that passing a non-kitchen route (e.g. `Receipt`) throws `DomainException`.

> **Verbatim Code Excerpt (Lines 36-76):**
```csharp
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
```

---

<a id="callable-3"></a>
### Callable 3: `PrintRules.ComposeReceiptTicket(PrintRoute, ReceiptHeader, IReadOnlyList<TicketItem>, ReceiptTotals)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:89:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `89-108`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes customer and counter tickets for customer-facing routes:
- Validates `CustomerRoutes.Contains(route)`; throws `DomainException($"{route} ليس مسار إيصال عميل.")` if invalid.
- If `route == PrintRoute.Receipt`:
  - Adds elements from `ComposeCustomerNumberSection(header)`.
  - Inserts `TicketCut` (mid-stream cut element).
  - Adds elements from `ComposeCashierDetailSection(header, items, totals)`.
- If `route == PrintRoute.CallCenter`:
  - Adds elements from `ComposeDetailedOrderSection(header, items, totals)` (single copy without customer token).
- Returns `new ComposedTicket(route, header.OrderId, header.OrderNumber, lines)`.

**B. Callers, Dependencies & Side Effects:**  
Called by `PrintJobCreationService` upon cashier checkout or call-center order placement.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Atomic Two-Ticket Spooling:** For the cashier till (`PrintRoute.Receipt`), two physical tickets are composed into a single `ComposedTicket`. When rendered, the stream contains an embedded knife cut between the token and the receipt. Submitting both pieces in one job eliminates the risk of spooler queue re-ordering.
- **Partial Pair Vulnerability:** If a hardware error (paper jam, power cut) occurs during the print of piece 2, the customer holds piece 1 while piece 2 is incomplete. The write-ahead attempt store guarantees that restart reports an uncertain attempt requiring manual inspection.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Stateless domain composition.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `PrintRulesTests.ComposeReceiptTicket_emits_two_sections_separated_by_cut_for_Receipt`.
- *Missing Tests:* Verification of exception when called with non-customer route (e.g. `PrintRoute.Charcoal`).

> **Verbatim Code Excerpt (Lines 89-108):**
```csharp
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
```

---

<a id="callable-4"></a>
### Callable 4: `PrintRules.ComposeCustomerNumberSection(ReceiptHeader)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:117:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `117-134`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Generates the token handed to the customer waiting at the counter:
- Brand header (`ComposeBrandHeader`).
- Order type centered in bold (`DescribeOrderType`).
- Label `"رقم الطلب"`.
- Order number in maximum `Display` size (`TicketTextSize.Display`).
- Cairo date and time row.
- Requested pickup notice (if scheduled).
- Cashier name: `"الكاشير: {header.CashierName}"`.
- Separator rule and pickup instruction: `"احتفظ بهذا التيكت حتى استلام الطلب"`.
- Returns `IEnumerable<TicketElement>`.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeReceiptTicket` (line 96). Pure generator.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Minimal vertical length (~60mm) to minimize paper consumption for queue tokens.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Pure function.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `PrintRulesTests.Customer_number_section_contains_display_order_number`.
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Lines 117-134):**
```csharp
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
```

---

<a id="callable-5"></a>
### Callable 5: `PrintRules.ComposeCashierDetailSection(ReceiptHeader, IReadOnlyList<TicketItem>, ReceiptTotals)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:147:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `147-200`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes the detailed customer receipt and counter prep copy:
- Brand title and tagline.
- Order identity: `"رقم الطلب"`, order number in `Huge` size, order type, cashier name, date/time, customer/delivery info (if Delivery/Pickup).
- Item table: Header, rows with prices, item notes (`"ملاحظات: ..."`), and discount/offer annotations (`"عرض: ... (بدل ... ج)"`).
- Financial totals & payment block (`ComposeCashierTotals`).
- Brand footer with menu QR (`ComposeBrandFooter`).
- Returns `IEnumerable<TicketElement>`.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeReceiptTicket` (line 98). Pure generator.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Preserves frozen sale snapshot. Applied discounts and list unit prices are read directly from `TicketItem` frozen snapshots, ensuring identical figures even if menu prices change later.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Pure function.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `PrintRulesTests.Cashier_detail_section_includes_item_notes_and_promotions`.
- *Missing Tests:* Formatting verification when item has exceptionally long note text (>50 chars).

> **Verbatim Code Excerpt (Lines 147-200):**
```csharp
    private static IEnumerable<TicketElement> ComposeCashierDetailSection(
        ReceiptHeader header, IReadOnlyList<TicketItem> items, ReceiptTotals totals)
    {
        yield return TicketLayout.BrandTitle(header.RestaurantName);
        if (!string.IsNullOrWhiteSpace(header.SubName)) yield return TicketLayout.Tagline(header.SubName.Trim());
        yield return TicketLayout.ThinRule();

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

        yield return TicketLayout.ThinRule();
        yield return TicketLayout.CashierItemHeader();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            yield return TicketLayout.CashierItemRow(item);
            if (!string.IsNullOrWhiteSpace(item.Notes))
                yield return TicketLayout.CashierItemNote($"ملاحظات: {item.Notes}");
            if (!string.IsNullOrWhiteSpace(item.PriceRuleName))
                yield return TicketLayout.CashierItemNote($"عرض: {item.PriceRuleName}"
                    + (item.ListUnitPrice is { IsPositive: true } list ? $" (بدل {list.Egp} ج)" : ""));
            var annotated = !string.IsNullOrWhiteSpace(item.Notes) || !string.IsNullOrWhiteSpace(item.PriceRuleName);
            if (annotated && i < items.Count - 1) yield return TicketLayout.Space();
        }
        yield return TicketLayout.ThinRule();

        foreach (var element in ComposeCashierTotals(header, totals)) yield return element;
        foreach (var element in ComposeBrandFooter(header)) yield return element;
    }
```

---

<a id="callable-6"></a>
### Callable 6: `PrintRules.ComposeBrandHeader(ReceiptHeader)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:204:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `204-210`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes brand title, optional tagline (`SubName`), accent rule, and whitespace separator. Returns `IEnumerable<TicketElement>`.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeCustomerNumberSection`. Pure function.

> **Verbatim Code Excerpt (Lines 204-210):**
```csharp
    private static IEnumerable<TicketElement> ComposeBrandHeader(ReceiptHeader header)
    {
        yield return TicketLayout.BrandTitle(header.RestaurantName);
        if (!string.IsNullOrWhiteSpace(header.SubName)) yield return TicketLayout.Tagline(header.SubName.Trim());
        yield return TicketLayout.AccentRule();
        yield return TicketLayout.Space();
    }
```

---

<a id="callable-7"></a>
### Callable 7: `PrintRules.ComposeBrandFooter(ReceiptHeader)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:219:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `219-230`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes footer rule, optional menu QR code (with `"امسح الكود لعرض المنيو"`), and centered restaurant brand name and tagline. Emits QR code ONLY if `MenuUrl` is non-empty. Returns `IEnumerable<TicketElement>`.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeCashierDetailSection`. Pure function.

> **Verbatim Code Excerpt (Lines 219-230):**
```csharp
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
```

---

<a id="callable-8"></a>
### Callable 8: `PrintRules.ComposeCashierTotals(ReceiptHeader, ReceiptTotals)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:236:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `236-260`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes the financial breakdown and payment state block for the cashier receipt:
- Subtotal: `"إجمالي الأصناف"`, `${totals.Subtotal.Egp} ج`.
- Conditional adjustments: Discount, Comp ("ضيافة"), Delivery fee, Tax amount. Rows with zero value are omitted.
- Grand Total: `"الإجمالي الكلي"`, bold, `Title` size.
- **Payment State Resolution (FLOW-001 Analysis):**
  - `isPaid = header.IsPaid ?? (!header.ElectronicPaymentPending && totals.AmountPaid >= totals.Total)`
  - `paymentState`:
    - If `ElectronicPaymentPending`: `"لم يُؤكَّد"`
    - Else if `isPaid`: `"مدفوع"`
    - Else if `CheckoutRules.IsCashOnDelivery(...)`: `"عند الاستلام"`
    - Else: `"غير مدفوع"`
  - Emits: `"طريقة الدفع: {DescribePaymentMethod(totals.PaymentMethod)}"`, `paymentState`.
  - Amount paid (`"المدفوع"`) and change due (`"الباقي"`) if positive.
- Returns `IEnumerable<TicketElement>`.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeCashierDetailSection`. Pure generator.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
No arithmetic is performed; all numbers are formatted directly from pre-calculated snapshot properties. Avoids plus/minus signs next to Arabic currency units to prevent BiDi numeric disconnection.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Pure function.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `PrintRulesTests.ComposeCashierTotals_renders_payment_states`.
- *Missing Tests:* Verification of `paymentState` when `IsPaid` is explicitly false on a Takeaway order.

> **Verbatim Code Excerpt (Lines 236-260):**
```csharp
    private static IEnumerable<TicketElement> ComposeCashierTotals(ReceiptHeader header, ReceiptTotals totals)
    {
        yield return TicketLayout.SlipLine("إجمالي الأصناف", $"{totals.Subtotal.Egp} ج");
        if (totals.Discount.IsPositive) yield return TicketLayout.MoneyLine("الخصم", $"{totals.Discount.Egp} ج", size: TicketTextSize.Large);
        if (totals.CompAmount.IsPositive) yield return TicketLayout.MoneyLine("ضيافة", $"{totals.CompAmount.Egp} ج", size: TicketTextSize.Large);
        if (totals.DeliveryFee.IsPositive) yield return TicketLayout.MoneyLine("رسوم التوصيل", $"{totals.DeliveryFee.Egp} ج", size: TicketTextSize.Large);
        if (totals.TaxAmount.IsPositive) yield return TicketLayout.MoneyLine(totals.TaxRuleName ?? "الضريبة", $"{totals.TaxAmount.Egp} ج", size: TicketTextSize.Large);

        yield return TicketLayout.MoneyLine("الإجمالي الكلي", $"{totals.Total.Egp} ج", bold: true, size: TicketTextSize.Title);

        var isPaid = header.IsPaid ?? (!header.ElectronicPaymentPending && totals.AmountPaid >= totals.Total);
        var paymentState = header.ElectronicPaymentPending ? "لم يُؤكَّد"
            : isPaid ? "مدفوع"
            : CheckoutRules.IsCashOnDelivery(header.OrderType, totals.PaymentMethod, totals.Total) ? "عند الاستلام" : "غير مدفوع";
        yield return TicketLayout.MoneyLine($"طريقة الدفع: {DescribePaymentMethod(totals.PaymentMethod)}", paymentState, size: TicketTextSize.Large);
        if (totals.AmountPaid.IsPositive) yield return TicketLayout.MoneyLine("المدفوع", $"{totals.AmountPaid.Egp} ج", size: TicketTextSize.Large);
        if (totals.ChangeDue.IsPositive) yield return TicketLayout.MoneyLine("الباقي", $"{totals.ChangeDue.Egp} ج", bold: true, size: TicketTextSize.Large);
    }
```

---

<a id="callable-9"></a>
### Callable 9: `PrintRules.ComposeDetailedOrderSection(ReceiptHeader, IReadOnlyList<TicketItem>, ReceiptTotals)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:265:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `265-292`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes the detailed order preparation ticket used by `PrintRoute.CallCenter`:
- Brand header and caption `"تذكرة تحضير الطلب"`.
- Order number, order type, Cairo date, cashier name.
- Electronic payment pending banner (if applicable).
- Delivery customer and address details (if Delivery).
- Full item table (`ComposeItemTable(items, withPrice: true)`).
- Totals block (`ComposeTotals(totals)`).
- Footer caption.
- Returns `IEnumerable<TicketElement>`.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeReceiptTicket` for `PrintRoute.CallCenter`. Pure generator.

> **Verbatim Code Excerpt (Lines 265-292):**
```csharp
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
```

---

<a id="callable-10"></a>
### Callable 10: `PrintRules.ComposeDeliveryOrderTicket(ReceiptHeader, IReadOnlyList<TicketItem>, ReceiptTotals, DeliveryOrderTicketInfo)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:299:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `299-329`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes the packing and assembly slip for the delivery dispatch desk (`PrintRoute.DeliveryOrder`):
- Brand title and header `"أوردر دليفري"`.
- Large order number, order time, customer name, customer phone, delivery area, delivery address.
- Full itemized table with prices (`ComposeItemTable`).
- Totals block (`ComposeTotals`).
- Collection instruction (`ComposeCollectionInstruction`): specifies exact amount to collect or prepaid status.
- Optional delivery notes.
- Footer caption: `"بون تشغيل - ليس إيصال العميل"`.
- Returns `new ComposedTicket(PrintRoute.DeliveryOrder, header.OrderId, header.OrderNumber, lines)`.

**B. Callers, Dependencies & Side Effects:**  
Called by delivery checkout workflow handlers. Pure domain composition.

> **Verbatim Code Excerpt (Lines 299-329):**
```csharp
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
```

---

<a id="callable-11"></a>
### Callable 11: `PrintRules.ComposeDriverDispatchTicket(string, string, string, string, string?, string?, string?, string?, Money, bool, bool, string?, DateTimeOffset, bool)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:337:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `337-370`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes the driver handover slip printed upon driver assignment or reassignment (`PrintRoute.DriverDispatch`):
- If `isReassignment == true`, injects prominent banner `"إعادة إسناد"` (Large bold text).
- Caption `"تيكت إسناد للسائق"`, order number, driver name, assignment Cairo timestamp.
- Customer name, phone, area, and delivery address.
- Collection instruction (`ComposeCollectionInstruction`): specifies cash to collect or prepaid note.
- Delivery notes.
- Footer caption: `"يُسلَّم هذا البون للسائق"`.
- Returns `new ComposedTicket(PrintRoute.DriverDispatch, orderId, orderNumber, lines)`.

**B. Callers, Dependencies & Side Effects:**  
Called by driver assignment and reassignment command handlers in `Falah.Application.Orders`.

> **Verbatim Code Excerpt (Lines 337-370):**
```csharp
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
```

---

<a id="callable-12"></a>
### Callable 12: `PrintRules.ComposeItemTable(IReadOnlyList<TicketItem>, bool)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:372:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `372-385`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Iterates items to generate table rows, headers, annotations, and separating rules. `withPrice` controls price column inclusion. Returns `IEnumerable<TicketElement>`.

> **Verbatim Code Excerpt (Lines 372-385):**
```csharp
    private static IEnumerable<TicketElement> ComposeItemTable(IReadOnlyList<TicketItem> items, bool withPrice)
    {
        yield return TicketLayout.Rule();
        yield return TicketLayout.ItemTableHeader(withPrice);
        yield return TicketLayout.Rule();
        foreach (var item in items)
        {
            yield return TicketLayout.ItemRow(item, withPrice);
            foreach (var annotation in TicketLayout.ItemAnnotations(item, withPriceRule: withPrice)) yield return annotation;
            yield return TicketLayout.Rule();
        }
    }
```

---

<a id="callable-13"></a>
### Callable 13: `PrintRules.ComposeTotals(ReceiptTotals)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:387:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `387-401`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes summary financial totals for call center and delivery order tickets: Subtotal, Discount, Comp, Tax, Delivery fee, Grand Total, Payment method, Amount paid, and Change due. Returns `IEnumerable<TicketElement>`.

> **Verbatim Code Excerpt (Lines 387-401):**
```csharp
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
        if (totals.AmountPaid.IsPositive) yield return TicketLayout.MoneyLine("المدفوع", $"{totals.AmountPaid.Egp} ج");
        if (totals.ChangeDue.IsPositive) yield return TicketLayout.MoneyLine("الباقي", $"{totals.ChangeDue.Egp} ج");
    }
```

---

<a id="callable-14"></a>
### Callable 14: `PrintRules.ComposeCollectionInstruction(bool, bool, Money)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:406:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `406-425`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Generates explicit driver collection instruction:
- If `isCashOnDelivery`: `"المطلوب تحصيله: {amountToCollect.Egp} ج"` (Bold, Large).
- Else if `electronicPaymentPending`: `"تحويل إلكتروني غير مؤكَّد"` / `"لا تُسلَّم قبل التأكيد"`.
- Else: `"مدفوع مسبقًا"` / `"لا يوجد مبلغ للتحصيل"`.
Returns `IEnumerable<TicketElement>`.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeDeliveryOrderTicket` and `ComposeDriverDispatchTicket`.

> **Verbatim Code Excerpt (Lines 406-425):**
```csharp
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
```

---

<a id="callable-15"></a>
### Callable 15: `PrintRules.DescribeRequestedPickup(DateTimeOffset?)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:430:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `430-431`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Returns formatted pickup label `"موعد الاستلام: {FormatCairo(at)}"` or null if `requestedAtUtc` is null.

> **Verbatim Code Excerpt (Lines 430-431):**
```csharp
    private static string? DescribeRequestedPickup(DateTimeOffset? requestedAtUtc) =>
        requestedAtUtc is not { } at ? null : $"موعد الاستلام: {FormatCairo(at)}";
```

---

<a id="callable-16"></a>
### Callable 16: `PrintRules.FormatCairo(DateTimeOffset)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:433:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `433-434`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Converts UTC timestamp to Cairo time zone (`BusinessClock.CairoTimeZone`) and formats as `"yyyy-MM-dd HH:mm"`.

> **Verbatim Code Excerpt (Lines 433-434):**
```csharp
    private static string FormatCairo(DateTimeOffset utc) =>
        TimeZoneInfo.ConvertTime(utc, BusinessClock.CairoTimeZone).ToString("yyyy-MM-dd HH:mm");
```

---

<a id="callable-17"></a>
### Callable 17: `PrintRules.FormatCairoDate(DateTimeOffset)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:443:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `443-444`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Converts UTC to Cairo timezone and formats as `"\u200E" + "dd-MM-yyyy"`.
- **BiDi Invariant:** The invisible Left-to-Right mark (`\u200E`) prevents Unicode bidirectional layout engines from treating hyphens as mathematical operators between Arabic digits, which would otherwise reverse `"18-09-2026"` into `"2026-09-18"`.

> **Verbatim Code Excerpt (Lines 443-444):**
```csharp
    private static string FormatCairoDate(DateTimeOffset utc) =>
        "\u200E" + TimeZoneInfo.ConvertTime(utc, BusinessClock.CairoTimeZone).ToString("dd-MM-yyyy");
```

---

<a id="callable-18"></a>
### Callable 18: `PrintRules.FormatCairoTime(DateTimeOffset)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:450:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `450-454`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Formats Cairo wall clock time as 12-hour string with hardcoded Arabic meridiem: `"{local:hh:mm} {(local.Hour < 12 ? "ص" : "م")}"`. Avoids culture-specific locale dependencies on Windows till machines.

> **Verbatim Code Excerpt (Lines 450-454):**
```csharp
    private static string FormatCairoTime(DateTimeOffset utc)
    {
        var local = TimeZoneInfo.ConvertTime(utc, BusinessClock.CairoTimeZone);
        return $"{local:hh:mm} {(local.Hour < 12 ? "ص" : "م")}";
    }
```

---

<a id="callable-19"></a>
### Callable 19: `PrintRules.DescribeOrderType(OrderType)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:456:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `456-462`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Maps `OrderType` enum to standard Egyptian Arabic ticket label:
- `OrderType.Pickup` $\rightarrow$ `"استلام من المطعم"`
- `OrderType.Takeaway` $\rightarrow$ `"تيك أواي"`
- `OrderType.Delivery` $\rightarrow$ `"دليفري"`
- Default $\rightarrow$ `orderType.ToString()`

> **Verbatim Code Excerpt (Lines 456-462):**
```csharp
    private static string DescribeOrderType(OrderType orderType) => orderType switch
    {
        OrderType.Pickup => "استلام من المطعم",
        OrderType.Takeaway => "تيك أواي",
        OrderType.Delivery => "دليفري",
        _ => orderType.ToString(),
    };
```

---

<a id="callable-20"></a>
### Callable 20: `PrintRules.DescribePaymentMethod(Financial.PaymentMethod)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:464:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `464-472`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Maps financial payment methods to Arabic receipt terms:
- `Cash` $\rightarrow$ `"نقدي"`
- `Card` $\rightarrow$ `"بطاقة"`
- `InstaPay` $\rightarrow$ `"إنستاباي"`
- `Wallet` $\rightarrow$ `"محفظة إلكترونية"`
- `Multi` $\rightarrow$ `"دفع متعدد"`
- Default $\rightarrow$ `method.ToString()`

> **Verbatim Code Excerpt (Lines 464-472):**
```csharp
    private static string DescribePaymentMethod(Financial.PaymentMethod method) => method switch
    {
        Financial.PaymentMethod.Cash => "نقدي",
        Financial.PaymentMethod.Card => "بطاقة",
        Financial.PaymentMethod.InstaPay => "إنستاباي",
        Financial.PaymentMethod.Wallet => "محفظة إلكترونية",
        Financial.PaymentMethod.Multi => "دفع متعدد",
        _ => method.ToString(),
    };
```

---

<a id="callable-21"></a>
### Callable 21: `PrintRules.MarkAsReprint(ComposedTicket, DateTimeOffset, string)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:484:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `484-501`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Transforms any composed ticket into an auditable reprint:
- Iterates existing `ticket.Lines`.
- Maintains boolean flag `startOfPiece = true`.
- At `startOfPiece`, prepends `ReprintBanner(reprintedAtUtc, byUserName)`.
- Whenever a `TicketCut` element is encountered, resets `startOfPiece = true`.
- If lines were empty, injects banner.
- Returns `ticket with { Lines = lines }`.
- **Physical Multi-Piece Invariant:** For tickets containing embedded cuts (such as `PrintRoute.Receipt` which has a customer token and a cashier detail ticket), `MarkAsReprint` guarantees that **both** physical paper slips receive an `"إعادة طباعة"` banner at their tops.

**B. Callers, Dependencies & Side Effects:**  
Called by reprint handlers (`ReprintPrintJobCommandHandler`, `ReprintSlipCommandHandler`). Pure domain transformation.

> **Verbatim Code Excerpt (Lines 484-501):**
```csharp
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
```

---

<a id="callable-22"></a>
### Callable 22: `PrintRules.ReprintBanner(DateTimeOffset, string)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:503:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `503-510`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Generates reprint banner elements:
- Centered bold title: `"إعادة طباعة"` (`TicketTextSize.Title`).
- Audit line: `"{FormatCairoDate(reprintedAtUtc)} - {FormatCairoTime(reprintedAtUtc)} - {byUserName}"`.
- Thin rule separator.
Returns `IEnumerable<TicketElement>`.

> **Verbatim Code Excerpt (Lines 503-510):**
```csharp
    private static IEnumerable<TicketElement> ReprintBanner(DateTimeOffset reprintedAtUtc, string byUserName)
    {
        yield return TicketLayout.CenteredLine("إعادة طباعة", bold: true, size: TicketTextSize.Title);
        yield return TicketLayout.CenteredLine(
            $"{FormatCairoDate(reprintedAtUtc)} - {FormatCairoTime(reprintedAtUtc)} - {byUserName}",
            size: TicketTextSize.Normal);
        yield return TicketLayout.ThinRule();
    }
```

---

<a id="callable-23"></a>
### Callable 23: `PrintRules.ComposeShiftOpenTicket(ShiftSlipIdentity, Money)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:516:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `516-530`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes shift opening float slip:
- Heading: `SlipHeading(..., "افتتاح وردية")`.
- Details: Cashier name, shift name, shift reference, opening date, opening time.
- Opening cash balance: `"رصيد افتتاحي"`, `${openingCash.Egp} ج` in `Title` size.
- Cashier signature line: `"توقيع الكاشير: .............................."`.
- Returns `new ComposedTicket(PrintRoute.Receipt, shift.ShiftReference, ShiftOpenLabel, lines)`.

> **Verbatim Code Excerpt (Lines 516-530):**
```csharp
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
```

---

<a id="callable-24"></a>
### Callable 24: `PrintRules.ComposeShiftCloseTicket(ShiftSlipIdentity, DateTimeOffset, ShiftCashBreakdown, IReadOnlyList<ExpenseSummaryLine>, IReadOnlyList<string>, string?)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:537:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `537-598`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes comprehensive shift handover and reconciliation slip:
- Heading `"إغلاق وتسليم وردية"`.
- Identity: Reference, cashier, opening date, open time, close time. If shift crossed midnight, prints close date.
- Cash breakdown: Opening cash, cash sales, cash in, expenses, refunds, withdrawals, safe drops.
- Reconciliation: Expected cash (`Title` size), Actual cash counted, Difference (`DescribeDifference(cash.Difference)` with absolute amount).
- Expense categorization breakdown (if any).
- Supervisor notes and closing warnings (if any).
- Dual signature lines: `"تسليم الكاشير"` and `"استلام المسؤول"`.
- Returns `new ComposedTicket(PrintRoute.Receipt, shift.ShiftReference, ShiftCloseLabel, lines)`.

> **Verbatim Code Excerpt (Lines 537-598):**
```csharp
    public static ComposedTicket ComposeShiftCloseTicket(
        ShiftSlipIdentity shift, DateTimeOffset closedAtUtc, ShiftCashBreakdown cash,
        IReadOnlyList<ExpenseSummaryLine> expenses, IReadOnlyList<string> warnings, string? note)
    {
        var lines = new List<TicketElement>();
        lines.AddRange(SlipHeading(shift.RestaurantName, shift.SubName, "إغلاق وتسليم وردية"));
        lines.Add(TicketLayout.SlipLine("رقم الوردية", shift.ShiftReference));
        lines.Add(TicketLayout.SlipLine("الكاشير", shift.CashierName));
        lines.Add(TicketLayout.SlipLine("التاريخ", FormatCairoDate(shift.OpenedAtUtc)));
        lines.Add(TicketLayout.SlipLine("وقت الفتح", FormatCairoTime(shift.OpenedAtUtc)));
        lines.Add(TicketLayout.SlipLine("وقت الإغلاق", FormatCairoTime(closedAtUtc)));
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
```

---

<a id="callable-25"></a>
### Callable 25: `PrintRules.ComposeExpenseTicket(ExpenseSlipInfo)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:602:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `602-620`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes standalone expense disbursement voucher: Heading `"مصروف"`, Amount in `Title` size, Date, Time, Category, Payment method, Employee name, Description, Notes, and Receiver signature line `"توقيع المستلم: ..................."`. Returns `new ComposedTicket(PrintRoute.Receipt, expense.Reference, ExpenseLabel, lines)`.

> **Verbatim Code Excerpt (Lines 602-620):**
```csharp
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
```

---

<a id="callable-26"></a>
### Callable 26: `PrintRules.SlipHeading(string, string?, string)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:628:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `628-634`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Generates standardized brand header for operational slips: Brand title, optional tagline, slip title (`TicketLayout.SlipTitle`), and thin separator rule. Returns `IEnumerable<TicketElement>`.

> **Verbatim Code Excerpt (Lines 628-634):**
```csharp
    private static IEnumerable<TicketElement> SlipHeading(string restaurantName, string? subName, string title)
    {
        yield return TicketLayout.BrandTitle(restaurantName);
        if (!string.IsNullOrWhiteSpace(subName)) yield return TicketLayout.Tagline(subName!.Trim());
        yield return TicketLayout.SlipTitle(title);
        yield return TicketLayout.ThinRule();
    }
```

---

<a id="callable-27"></a>
### Callable 27: `PrintRules.DescribeDifference(Money)`
- **Callable ID:** `src/Falah.Domain/Printing/PrintRules.cs:636:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `636-637`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Returns Arabic description of shift cash difference:
- `difference.IsZero` $\rightarrow$ `"الفرق - مطابق"`
- `difference.IsNegative` $\rightarrow$ `"الفرق - عجز"`
- `difference.IsPositive` $\rightarrow$ `"الفرق - زيادة"`

> **Verbatim Code Excerpt (Lines 636-637):**
```csharp
    private static string DescribeDifference(Money difference) =>
        difference.IsZero ? "الفرق - مطابق" : difference.IsNegative ? "الفرق - عجز" : "الفرق - زيادة";
```

---

## 3. Subsystem Vulnerability & Integrity Analysis

| Finding ID | Vulnerability / Architectural Gap | Root Cause in `PrintRules.cs` | Severity | Remediation Strategy |
| :--- | :--- | :--- | :--- | :--- |
| **FLOW-001** | Two-Stage Pay-at-Pickup State Disconnect | If order ticket is printed at placement prior to customer payment, `isPaid` resolves to false (`"غير مدفوع"`). When customer later pays at collection, no automatic reprint or fiscal voucher is dispatched. | **HIGH** | Trigger an automated reprint with `"مدفوع"` banner upon pickup checkout payment completion. |
| **PRT-003** | Partial Pair Delivery Uncertainty | On `PrintRoute.Receipt`, customer token and cashier detail ticket are separated by `TicketCut`. An unhandled mechanical cutter jam or out-of-paper event leaves the physical pair incomplete. | **MEDIUM** | Handled at agent level via write-ahead uncertain attempt store; counter staff must inspect paper before reprinting. |
