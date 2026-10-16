using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Printing;
using Xunit;

namespace Falah.Domain.Tests;

public class PrintRulesTests
{
    [Fact]
    public void An_uncollected_cash_delivery_is_never_labelled_paid()
    {
        var header = CashierHeader() with { OrderType = OrderType.Delivery, IsPaid = false };
        var totals = CashierTotals() with { AmountPaid = Money.Zero, ChangeDue = Money.Zero };
        var text = TextOf(PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, header, CashierItems(), totals));
        Assert.Contains("عند الاستلام", text);
        Assert.DoesNotContain("مدفوع", text);
    }

    /// <summary>Every printable string on a ticket, in order - text elements plus the cells of any
    /// table row, so an assertion about content keeps working wherever the layout puts it.</summary>
    private static List<string> TextOf(ComposedTicket ticket) =>
        ticket.Lines.SelectMany(e => e switch
        {
            TicketText t => new[] { t.Text },
            TicketRow r => r.Cells.Select(c => c.Text).ToArray(),
            _ => Array.Empty<string>(),
        }).ToList();

    private static readonly DateTimeOffset Now = new(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);

    private static TicketItem Item(string name, string? station, int quantity = 1, string? notes = null) =>
        new(name, null, quantity, Money.FromEgp(50m), Money.FromEgp(50m * quantity), notes, null, null, station);

    [Fact]
    public void An_order_with_no_kitchen_bound_items_needs_no_kitchen_routes()
    {
        var routes = PrintRules.DetermineKitchenRoutes(new string?[] { null, null });
        Assert.Empty(routes);
    }

    [Fact]
    public void An_order_touching_both_stations_needs_both_kitchen_routes()
    {
        var routes = PrintRules.DetermineKitchenRoutes(new[] { "Charcoal", "Grill", null });
        Assert.Equal(new HashSet<PrintRoute> { PrintRoute.Charcoal, PrintRoute.Grill }, routes);
    }

    [Fact]
    public void Duplicate_station_values_collapse_to_one_route()
    {
        var routes = PrintRules.DetermineKitchenRoutes(new[] { "Grill", "Grill", "Grill" });
        Assert.Single(routes);
    }

    [Fact]
    public void An_unrecognized_station_value_is_ignored_rather_than_throwing()
    {
        var routes = PrintRules.DetermineKitchenRoutes(new[] { "SomeFutureStation" });
        Assert.Empty(routes);
    }

    [Fact]
    public void A_charcoal_ticket_contains_only_charcoal_items_never_grill_items()
    {
        var items = new[] { Item("كباب", "Charcoal"), Item("فراخ مشوية", "Grill"), Item("عصير", null) };
        var ticket = PrintRules.ComposeKitchenTicket(PrintRoute.Charcoal, "فالح أبو العنبه", "ord-1", "#0001", OrderType.Takeaway, items, Now);

        var text = string.Join('\n', TextOf(ticket));
        Assert.Contains("كباب", text);
        Assert.DoesNotContain("فراخ مشوية", text);
        Assert.DoesNotContain("عصير", text);
    }

    [Fact]
    public void A_kitchen_ticket_never_contains_a_price()
    {
        var items = new[] { Item("كباب", "Charcoal") };
        var ticket = PrintRules.ComposeKitchenTicket(PrintRoute.Charcoal, "فالح أبو العنبه", "ord-1", "#0001", OrderType.Takeaway, items, Now);

        var text = string.Join('\n', TextOf(ticket));
        Assert.DoesNotContain("50.00", text);
        Assert.DoesNotContain(" ج", text);
    }

    [Fact]
    public void A_kitchen_ticket_surfaces_a_note_as_a_warning_line()
    {
        var items = new[] { Item("كباب", "Charcoal", notes: "بدون بصل") };
        var ticket = PrintRules.ComposeKitchenTicket(PrintRoute.Charcoal, "فالح أبو العنبه", "ord-1", "#0001", OrderType.Takeaway, items, Now);

        Assert.Contains(TextOf(ticket), l => l.Contains("بدون بصل"));
    }

    [Fact]
    public void Composing_a_kitchen_ticket_for_a_customer_facing_route_is_rejected()
    {
        Assert.Throws<DomainException>(() => PrintRules.ComposeKitchenTicket(PrintRoute.Receipt, "فالح أبو العنبه", "ord-1", "#0001", OrderType.Takeaway, Array.Empty<TicketItem>(), Now));
    }

    [Fact]
    public void Composing_a_customer_receipt_for_a_kitchen_route_is_rejected()
    {
        var header = new ReceiptHeader("ord-1", "فالح", "مطاعم", "القاهرة", "#0001", Now, "كاشير", OrderType.Takeaway, null, null, null, null);
        var totals = new ReceiptTotals(Money.FromEgp(100m), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(100m), Money.FromEgp(100m), Money.Zero, PaymentMethod.Cash);
        Assert.Throws<DomainException>(() => PrintRules.ComposeReceiptTicket(PrintRoute.Grill, header, Array.Empty<TicketItem>(), totals));
    }

    [Fact]
    public void A_customer_receipt_shows_the_total_and_payment_method()
    {
        var header = new ReceiptHeader("ord-1", "فالح", "مطاعم", "القاهرة", "#0001", Now, "أحمد", OrderType.Takeaway, null, null, null, null);
        var totals = new ReceiptTotals(Money.FromEgp(100m), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(100m), Money.FromEgp(100m), Money.Zero, PaymentMethod.Cash);
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, header, new[] { Item("برجر", null) }, totals);

        // The total is a two-column row now, not a "label: value" string, so this asserts on the
        // row itself: the label and the amount must be the two cells of ONE line, which is what
        // makes every total on the ticket align in the same place.
        var total = ticket.Lines.OfType<TicketRow>()
            .SingleOrDefault(r => r.Cells.Count == 2 && r.Cells[0].Text == "الإجمالي الكلي");
        Assert.NotNull(total);
        Assert.Equal("100 ج", total!.Cells[1].Text);
        Assert.True(total.Cells[0].Bold);
        Assert.Equal(TicketTextSize.Title, total.Size);
        Assert.False(total.Inverted);

        var text = string.Join('\n', TextOf(ticket));
        Assert.Contains("نقدي", text);
        Assert.Contains("#0001", text);
    }

    // ---- Final cashier ticket design ----

    private static ReceiptHeader CashierHeader() => new(
        "ord-1", "فالح أبو العنبه", "ألذ طعم", "6 أكتوبر", "#0276", Now, "مدير المحل",
        OrderType.Takeaway, null, null, null, null, "0100", null, null, false,
        "شكراً لزيارتكم مطاعم فالح أبو العنبه. نسعد بخدمتكم", null);

    private static ReceiptTotals CashierTotals() => new(
        Money.FromEgp(175m), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero,
        Money.FromEgp(175m), Money.FromEgp(200m), Money.FromEgp(25m), PaymentMethod.Cash);

    private static IReadOnlyList<TicketItem> CashierItems() => new[]
    {
        new TicketItem("شاورما فراخ", "جامبو", 2, Money.FromEgp(85m), Money.FromEgp(170m),
            "بدون بصل - زيادة طحينة", null, null, "Charcoal"),
        new TicketItem("بيبسي", null, 1, Money.FromEgp(5m), Money.FromEgp(5m), null, null, null, null),
    };

    private static (List<TicketElement> Customer, List<TicketElement> Detail) SplitCashier(ComposedTicket ticket)
    {
        var cut = ticket.Lines.ToList().FindIndex(e => e is TicketCut);
        return (ticket.Lines.Take(cut).ToList(), ticket.Lines.Skip(cut + 1).ToList());
    }

    private static List<string> TextOf(IEnumerable<TicketElement> elements) =>
        elements.SelectMany(e => e switch
        {
            TicketText t => new[] { t.Text },
            TicketRow r => r.Cells.Select(c => c.Text).ToArray(),
            _ => Array.Empty<string>(),
        }).ToList();

    /// <summary>The customer's half is an identification stub. Anything priced or itemised on it is
    /// a defect: it exists to be handed over and called out, not read.</summary>
    [Fact]
    public void The_customer_ticket_carries_identity_only_and_never_products_or_prices()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeader(), CashierItems(), CashierTotals());
        var (customer, _) = SplitCashier(ticket);
        var text = TextOf(customer);

        Assert.Contains(customer.OfType<TicketText>(), t => t.Text == "#0276" && t.Size == TicketTextSize.Display);
        Assert.Contains("فالح أبو العنبه", text);
        Assert.Contains("ألذ طعم", text);
        Assert.Contains(text, l => l.Contains("مدير المحل"));
        Assert.Contains(text, l => l.Contains("احتفظ بهذا التيكت"));

        Assert.DoesNotContain(text, l => l.Contains("شاورما"));
        Assert.DoesNotContain(text, l => l.Contains("بيبسي"));
        Assert.DoesNotContain(text, l => l.Contains("ج"));
        Assert.Empty(customer.OfType<TicketRow>().Where(r => r.Cells.Count > 2));
    }

    /// <summary>The till's detailed half: every item fully described, and the cashier named exactly
    /// once. A name printed twice on one ticket reads as a bug.</summary>
    [Fact]
    public void The_cashier_detail_ticket_shows_every_item_and_names_the_cashier_once()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeader(), CashierItems(), CashierTotals());
        var (_, detail) = SplitCashier(ticket);
        var text = TextOf(detail);

        Assert.Contains("شاورما فراخ - جامبو", text);
        Assert.Contains("2", text);
        Assert.Contains("170 ج", text);
        Assert.Contains("بيبسي", text);
        Assert.DoesNotContain("الحجم", text);

        Assert.Equal(1, text.Count(l => l.Contains("مدير المحل")));

        var totalRow = detail.OfType<TicketRow>().Single(r => r.Cells.Count == 2 && r.Cells[0].Text == "الإجمالي الكلي");
        Assert.Equal("175 ج", totalRow.Cells[1].Text);
        Assert.Equal(TicketTextSize.Title, totalRow.Size);
        Assert.Contains(text, l => l.Contains("الباقي"));
        Assert.Contains("25 ج", text);
    }

    /// <summary>A note belongs to the item above it. Asserted by position, not by mere presence:
    /// "somewhere on the ticket" would still pass if the note drifted under the wrong product.</summary>
    [Fact]
    public void An_item_note_is_printed_immediately_under_its_own_item()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeader(), CashierItems(), CashierTotals());
        var (_, detail) = SplitCashier(ticket);

        var shawarmaRow = detail.FindIndex(e => e is TicketRow r && r.Cells.Any(c => c.Text == "شاورما فراخ - جامبو"));
        var noteRow = detail.FindIndex(e => e is TicketText t && t.Text.Contains("بدون بصل"));
        Assert.True(shawarmaRow >= 0 && noteRow == shawarmaRow + 1,
            "the note must be the very next element after its own item row");
        // No separator between an item and its note - the separator comes after the note.
        Assert.IsType<TicketText>(detail[noteRow]);
    }

    /// <summary>The retired slogans must not come back through any path - neither hardcoded in a
    /// composition nor carried in from the settings footer, which the till ticket no longer prints.</summary>
    [Fact]
    public void The_retired_slogans_never_appear_on_a_cashier_ticket()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeader(), CashierItems(), CashierTotals());
        var text = string.Join('\n', TextOf(ticket.Lines));

        foreach (var slogan in new[] { "بالهناء والشفاء", "لحظات جميلة", "مذاق أصيل" })
            Assert.DoesNotContain(slogan, text);
        // The settings footer is deliberately not printed on the till ticket any more.
        Assert.DoesNotContain("شكراً لزيارتكم", text);
    }

    /// <summary>The till's tickets are drawn with weight and whitespace only: no white-on-black pill
    /// or band and no boxed grid anywhere. Measured on the real 80mm paper, those read as heavy and
    /// cramped rather than as emphasis.</summary>
    [Fact]
    public void Cashier_tickets_use_no_inverted_or_boxed_elements()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeader(), CashierItems(), CashierTotals());

        Assert.DoesNotContain(ticket.Lines.OfType<TicketText>(), t => t.Inverted);
        Assert.DoesNotContain(ticket.Lines.OfType<TicketRow>(), r => r.Inverted || r.Boxed || r.Continuation);

        var (customer, detail) = SplitCashier(ticket);
        Assert.Contains(customer.OfType<TicketText>(), t => t.Text == "رقم الطلب");
        Assert.Contains(detail.OfType<TicketText>(), t => t.Text == "#0276" && t.Size == TicketTextSize.Huge);
    }

    // ---- Falah print identity ----

    private const string MenuLink = "https://menu.example/falah";

    private static ReceiptHeader CashierHeaderWithMenu(string? menuUrl = MenuLink) => CashierHeader() with { MenuUrl = menuUrl };

    /// <summary>Both cashier tickets open the same way: the name, the tagline, and the short
    /// signature rule - the one ornament the tickets carry.</summary>
    [Fact]
    public void Both_cashier_tickets_open_with_the_brand_and_the_signature_rule()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeaderWithMenu(), CashierItems(), CashierTotals());
        var (customer, detail) = SplitCashier(ticket);

        foreach (var part in new[] { customer, detail })
        {
            var name = Assert.IsType<TicketText>(part[0]);
            Assert.Equal("فالح أبو العنبه", name.Text);
            Assert.True(name.Bold);
            Assert.Equal(TicketTextSize.Title, name.Size);
            Assert.Equal("ألذ طعم", Assert.IsType<TicketText>(part[1]).Text);
            Assert.Equal(part == customer ? TicketRuleStyle.Accent : TicketRuleStyle.Thin, Assert.IsType<TicketRule>(part[2]).Style);
        }
    }

    /// <summary>The detail ticket's identity block leads with the number, then the facts about the
    /// order - type and cashier, then date and time.</summary>
    [Fact]
    public void The_detail_ticket_leads_with_the_order_number_then_its_metadata()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeaderWithMenu(), CashierItems(), CashierTotals());
        var (_, detail) = SplitCashier(ticket);
        var text = TextOf(detail);

        var label = text.IndexOf("رقم الطلب");
        var number = text.IndexOf("#0276");
        var type = text.IndexOf("تيك أواي");
        var cashier = text.FindIndex(l => l.StartsWith("الكاشير:"));
        var date = text.FindIndex(l => l.StartsWith("\u200E", StringComparison.Ordinal));
        var time = text.FindIndex(l => l.EndsWith(" م") || l.EndsWith(" ص"));
        var items = text.IndexOf("الصنف");

        // Label, then the number alone, then the facts about it: the order type beside the cashier,
        // the date beside the time - and all of that before the items.
        Assert.True(label >= 0 && label < number, "the number follows its own label");
        Assert.True(number < type && type < date, "the metadata follows the number");
        Assert.Equal(type + 1, cashier);
        Assert.Equal(date + 1, time);
        Assert.True(time < items, "the items come last");
        // The old "preparation ticket" caption is not part of the identity.
        Assert.DoesNotContain("تذكرة تحضير الطلب", text);
    }

    /// <summary>With a menu link configured, the detailed receipt ends: thin rule, the invitation,
    /// the QR, then the name and tagline - and nothing after them.</summary>
    [Fact]
    public void The_detail_ticket_ends_with_the_menu_qr_and_the_brand()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeaderWithMenu(), CashierItems(), CashierTotals());
        var (_, detail) = SplitCashier(ticket);

        var qrIndex = detail.FindIndex(e => e is TicketQr);
        Assert.Equal(MenuLink, Assert.IsType<TicketQr>(detail[qrIndex]).Payload);
        Assert.Equal("امسح الكود لعرض المنيو", Assert.IsType<TicketText>(detail[qrIndex - 1]).Text);
        Assert.Equal(TicketRuleStyle.Thin, Assert.IsType<TicketRule>(detail[qrIndex - 2]).Style);

        // After the QR: only blank space, the name, and the tagline.
        var after = detail.Skip(qrIndex + 1).Where(e => e is not TicketRule { Style: TicketRuleStyle.Blank }).ToList();
        Assert.Equal(new[] { "فالح أبو العنبه", "ألذ طعم" }, after.Select(e => Assert.IsType<TicketText>(e).Text));
        Assert.True(((TicketText)after[0]).Bold);

        // Exactly one code, on the detail ticket only.
        Assert.Single(ticket.Lines.OfType<TicketQr>());
    }

    /// <summary>The customer's token never carries the QR - it is identity only.</summary>
    [Fact]
    public void The_customer_ticket_never_carries_the_menu_qr()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeaderWithMenu(), CashierItems(), CashierTotals());
        var (customer, _) = SplitCashier(ticket);

        Assert.Empty(customer.OfType<TicketQr>());
        Assert.DoesNotContain(TextOf(customer), l => l.Contains("امسح الكود"));
    }

    /// <summary>No configured link means no code and no invitation to scan one - never a guessed or
    /// placeholder address. The brand sign-off is still printed.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Without_a_menu_link_no_qr_is_printed(string? menuUrl)
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeaderWithMenu(menuUrl), CashierItems(), CashierTotals());
        var (_, detail) = SplitCashier(ticket);

        Assert.Empty(ticket.Lines.OfType<TicketQr>());
        Assert.DoesNotContain(TextOf(detail), l => l.Contains("امسح الكود"));
        var tail = detail.OfType<TicketText>().TakeLast(2).Select(t => t.Text);
        Assert.Equal(new[] { "فالح أبو العنبه", "ألذ طعم" }, tail);
    }

    /// <summary>The call center's copy is out of this redesign's scope and must not pick up the QR
    /// by side effect.</summary>
    [Fact]
    public void The_call_center_copy_is_untouched_by_the_menu_qr()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.CallCenter, CashierHeaderWithMenu(), CashierItems(), CashierTotals());
        Assert.Empty(ticket.Lines.OfType<TicketQr>());
    }

    /// <summary>The item table is plain rows: captions, then one row per item with the name,
    /// size, quantity and price in their own columns. The grand total is bold and large - the
    /// strongest line on the ticket - without being filled.</summary>
    [Fact]
    public void The_cashier_item_table_is_plain_rows_and_the_total_is_bold_not_filled()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeader(), CashierItems(), CashierTotals());
        var (_, detail) = SplitCashier(ticket);
        var rows = detail.OfType<TicketRow>().ToList();

        var header = rows.Single(r => r.Cells.Count == 3 && r.Cells[0].Text == "الصنف");
        Assert.Equal(new[] { "الصنف", "الكمية", "السعر" }, header.Cells.Select(c => c.Text));

        var itemRows = rows.Where(r => r.Cells.Count == 3 && r != header).ToList();
        Assert.Equal(2, itemRows.Count);
        Assert.All(itemRows, r => Assert.Equal(TicketTextSize.Large, r.Size));
        // The product name takes the widest share of the row.
        Assert.All(itemRows, r => Assert.Equal(r.Cells.Max(c => c.WidthPercent), r.Cells[0].WidthPercent));

        var total = rows.Single(r => r.Cells.Count == 2 && r.Cells[0].Text == "الإجمالي الكلي");
        Assert.True(total.Cells[0].Bold && total.Cells[1].Bold);
        Assert.Equal(TicketTextSize.Title, total.Size);
        Assert.False(total.Inverted);
    }

    /// <summary>A date after an Arabic label must keep its own left-to-right order. Without the
    /// leading LRM the digits are reclassified as Arabic numbers and "18-09-2026" prints as
    /// "2026-09-18" - found on a real 576-dot preview, not in theory.</summary>
    [Fact]
    public void A_labelled_date_is_protected_from_bidi_reversal()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeader(), CashierItems(), CashierTotals());
        var (_, detail) = SplitCashier(ticket);

        // The mark leads the date itself now that it sits in its own cell rather than after a label.
        // Ordinal: the mark is ignorable in a culture-sensitive comparison, which would match any line.
        Assert.Contains(TextOf(detail), l => l.StartsWith("\u200E", StringComparison.Ordinal));
    }

    /// <summary>A discount is labelled, not signed. "-15 ج" inside a right-to-left line prints as
    /// "15-" because the minus is bidi-neutral; the label already says which way the money goes.</summary>
    [Fact]
    public void Cashier_money_lines_carry_no_sign_characters()
    {
        var totals = new ReceiptTotals(Money.FromEgp(260m), Money.FromEgp(15m), Money.Zero, null,
            Money.Zero, Money.Zero, Money.FromEgp(245m), Money.FromEgp(250m), Money.FromEgp(5m), PaymentMethod.Cash);
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeader(), CashierItems(), totals);
        var (_, detail) = SplitCashier(ticket);

        var discount = detail.OfType<TicketRow>().Single(r => r.Cells.Count == 2 && r.Cells[0].Text == "الخصم");
        Assert.Equal("15 ج", discount.Cells[1].Text);
        Assert.DoesNotContain(TextOf(detail), l => l.StartsWith("-") || l.StartsWith("+"));
    }

    /// <summary>The cashier prints two physically separate tickets from one job: a compact ticket
    /// carrying only the order number for the customer to hold, then the detailed one. The call
    /// center gets the detailed copy alone - there is nobody at the counter to hand a stub to.</summary>
    [Fact]
    public void The_cashier_receipt_is_two_tickets_and_the_call_center_copy_is_one()
    {
        var header = new ReceiptHeader("ord-1", "فالح", "مطاعم", "القاهرة", "#0001", Now, "أحمد", OrderType.Takeaway, null, null, null, null);
        var totals = new ReceiptTotals(Money.FromEgp(100m), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(100m), Money.FromEgp(100m), Money.Zero, PaymentMethod.Cash);
        var items = new[] { Item("برجر", null) };

        var cashier = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, header, items, totals);
        var callCenter = PrintRules.ComposeReceiptTicket(PrintRoute.CallCenter, header, items, totals);

        Assert.Equal(1, cashier.Lines.Count(e => e is TicketCut));
        Assert.Empty(callCenter.Lines.OfType<TicketCut>());

        // The customer's half carries identity only - no prices, no item lines.
        var customerHalf = cashier.Lines.TakeWhile(e => e is not TicketCut).ToList();
        Assert.Contains(customerHalf.OfType<TicketText>(), t => t.Size == TicketTextSize.Display && t.Text == "#0001");
        // A two-cell date/time row is expected here; an ITEM row (four cells) never is.
        Assert.Empty(customerHalf.OfType<TicketRow>().Where(r => r.Cells.Count > 2));
        Assert.DoesNotContain(customerHalf.OfType<TicketText>(), t => t.Text.Contains("برجر"));
    }

    [Fact]
    public void A_delivery_receipt_shows_the_customer_and_area_but_a_pickup_receipt_does_not()
    {
        var deliveryHeader = new ReceiptHeader("ord-1", "فالح", "مطاعم", "القاهرة", "#0001", Now, "أحمد", OrderType.Delivery, null, "سارة", "شارع 9", "المعادي");
        var pickupHeader = deliveryHeader with { OrderType = OrderType.Pickup };
        var totals = new ReceiptTotals(Money.FromEgp(100m), Money.Zero, Money.Zero, null, Money.FromEgp(10m), Money.Zero, Money.FromEgp(110m), Money.FromEgp(110m), Money.Zero, PaymentMethod.Cash);

        var deliveryTicket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, deliveryHeader, Array.Empty<TicketItem>(), totals);
        var pickupTicket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, pickupHeader, Array.Empty<TicketItem>(), totals);

        Assert.Contains(TextOf(deliveryTicket), l => l.Contains("المعادي"));
        Assert.DoesNotContain(TextOf(pickupTicket), l => l.Contains("المعادي"));
        Assert.Contains(TextOf(pickupTicket), l => l.Contains("استلام من المطعم"));
    }

    [Fact]
    public void A_zero_discount_and_zero_comp_never_print_a_bare_zero_line()
    {
        // A falsy-but-present zero previously slipped through in the reference app for delivery fee;
        // the same care applies here for discount/comp - only a POSITIVE value gets its own line.
        var header = new ReceiptHeader("ord-1", "فالح", "مطاعم", "القاهرة", "#0001", Now, "أحمد", OrderType.Takeaway, null, null, null, null);
        var totals = new ReceiptTotals(Money.FromEgp(100m), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(100m), Money.FromEgp(100m), Money.Zero, PaymentMethod.Cash);
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, header, Array.Empty<TicketItem>(), totals);

        Assert.DoesNotContain(TextOf(ticket), l => l.StartsWith("الخصم"));
        Assert.DoesNotContain(TextOf(ticket), l => l.StartsWith("ضيافة"));
    }

    [Fact]
    public void A_pickup_receipt_and_kitchen_ticket_both_carry_the_requested_collection_time()
    {
        // The counter needs it to hand the right bag to the right person; the station needs it so an
        // order booked for later is not cooked now.
        var wantedAtUtc = new DateTimeOffset(2026, 9, 12, 16, 30, 0, TimeSpan.Zero);
        var header = new ReceiptHeader("ord-1", "فالح", "مطاعم", "القاهرة", "#0001", Now, "أحمد",
            OrderType.Pickup, null, "سارة", null, null, RequestedPickupAtUtc: wantedAtUtc);
        var totals = new ReceiptTotals(Money.FromEgp(100m), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero,
            Money.FromEgp(100m), Money.FromEgp(100m), Money.Zero, PaymentMethod.Cash);

        var receipt = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, header, Array.Empty<TicketItem>(), totals);
        Assert.Contains(TextOf(receipt), l => l.Contains("موعد الاستلام"));

        var items = new[] { new TicketItem("كفتة", null, 1, Money.FromEgp(90m), Money.FromEgp(90m), null, null, null, "Charcoal") };
        var kitchen = PrintRules.ComposeKitchenTicket(PrintRoute.Charcoal, "فالح أبو العنبه", "ord-1", "#0001", OrderType.Pickup, items, Now, wantedAtUtc);
        Assert.Contains(TextOf(kitchen), l => l.Contains("موعد الاستلام"));
    }

    [Fact]
    public void A_pickup_with_no_requested_time_prints_no_time_line_at_all()
    {
        // Printed as nothing rather than as an empty or guessed line.
        var header = new ReceiptHeader("ord-1", "فالح", "مطاعم", "القاهرة", "#0001", Now, "أحمد",
            OrderType.Pickup, null, "سارة", null, null);
        var totals = new ReceiptTotals(Money.FromEgp(100m), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero,
            Money.FromEgp(100m), Money.FromEgp(100m), Money.Zero, PaymentMethod.Cash);

        var receipt = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, header, Array.Empty<TicketItem>(), totals);
        Assert.DoesNotContain(TextOf(receipt), l => l.Contains("موعد الاستلام"));
    }
    // ---- Reprint marking ----

    /// <summary>A reprint says so on every piece of paper it produces. The customer's token and the
    /// detailed receipt are two separate pieces; a banner on only the first would leave the second
    /// looking like an original.</summary>
    [Fact]
    public void A_reprint_is_marked_at_the_top_of_every_piece_it_prints()
    {
        var original = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeaderWithMenu(), CashierItems(), CashierTotals());
        var pieceCount = original.Lines.Count(e => e is TicketCut) + 1;

        var reprint = PrintRules.MarkAsReprint(original, Now.AddHours(1), "مدير المحل");

        var banners = reprint.Lines.Count(e => e is TicketText { Text: "إعادة طباعة" });
        Assert.Equal(pieceCount, banners);
        Assert.Equal("إعادة طباعة", Assert.IsType<TicketText>(reprint.Lines[0]).Text);
        // Immediately after each cut, before anything else on the next piece.
        for (var i = 0; i < reprint.Lines.Count - 1; i++)
            if (reprint.Lines[i] is TicketCut)
                Assert.Equal("إعادة طباعة", Assert.IsType<TicketText>(reprint.Lines[i + 1]).Text);

        // Who and when, so a reproduced receipt can be traced back.
        Assert.Contains(TextOf(reprint.Lines), l => l.Contains("مدير المحل"));
        // The original composition is untouched - marking adds lines, it never edits a ticket.
        Assert.DoesNotContain(original.Lines, e => e is TicketText { Text: "إعادة طباعة" });
    }

    // ---- Redesigned cashier receipt ----

    /// <summary>The item table is three columns - الصنف | الكمية | السعر - and a size is part of the
    /// product's name, not a column of its own.</summary>
    [Fact]
    public void The_receipt_item_table_has_no_size_column_and_appends_the_variant_to_the_name()
    {
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeaderWithMenu(), CashierItems(), CashierTotals());
        var (_, detail) = SplitCashier(ticket);
        var rows = detail.OfType<TicketRow>().Where(r => r.Cells.Count == 3).ToList();

        var header = rows[0];
        Assert.Equal(new[] { "الصنف", "الكمية", "السعر" }, header.Cells.Select(c => c.Text));
        Assert.All(rows, r => Assert.Equal(3, r.Cells.Count));
        Assert.Contains(rows, r => r.Cells[0].Text == "شاورما فراخ - جامبو");
        Assert.DoesNotContain(TextOf(detail), l => l == "الحجم");
    }

    /// <summary>Totals read as a sum: the items' total, then the grand total as the strongest line.
    /// Change is only stated when there is any.</summary>
    [Fact]
    public void The_receipt_states_the_items_total_and_omits_change_when_there_is_none()
    {
        var exact = new ReceiptTotals(Money.FromEgp(175m), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero,
            Money.FromEgp(175m), Money.FromEgp(175m), Money.Zero, PaymentMethod.Cash);

        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, CashierHeaderWithMenu(), CashierItems(), exact);
        var (_, detail) = SplitCashier(ticket);

        var subtotal = detail.OfType<TicketRow>().Single(r => r.Cells.Count == 2 && r.Cells[0].Text == "إجمالي الأصناف");
        Assert.Equal("175 ج", subtotal.Cells[1].Text);
        var total = detail.OfType<TicketRow>().Single(r => r.Cells.Count == 2 && r.Cells[0].Text == "الإجمالي الكلي");
        Assert.Equal(TicketTextSize.Title, total.Size);
        Assert.True(total.Cells[0].Bold);
        Assert.DoesNotContain(detail.OfType<TicketRow>(), r => r.Cells[0].Text == "الباقي");
    }

    // ---- Shift and expense slips ----

    private static ShiftSlipIdentity Shift() => new(
        "فالح أبو العنبه", "ألذ طعم", "A7F3C1", "صباحية", "أحمد محمد", Now);

    [Fact]
    public void The_shift_opening_slip_states_who_when_and_the_float()
    {
        var slip = PrintRules.ComposeShiftOpenTicket(Shift(), Money.FromEgp(500m));
        var text = TextOf(slip.Lines);

        Assert.Equal(PrintRoute.Receipt, slip.Route);
        Assert.Contains("افتتاح وردية", text);
        Assert.Contains("أحمد محمد", text);
        Assert.Contains("A7F3C1", text);
        Assert.Contains("رصيد افتتاحي", text);
        Assert.Contains("500 ج", text);
        // A slip is one piece of paper and carries no menu QR.
        Assert.Empty(slip.Lines.OfType<TicketQr>());
        Assert.Empty(slip.Lines.OfType<TicketCut>());
    }

    /// <summary>The handover slip states every figure that produced the expected cash, then the
    /// count and the difference - and the difference is worded, never signed.</summary>
    [Fact]
    public void The_handover_slip_shows_the_whole_cash_story_and_its_warnings()
    {
        var cash = new ShiftCashBreakdown(
            OpeningCash: Money.FromEgp(500m), CashSales: Money.FromEgp(4310m), Refunds: Money.FromEgp(95m),
            CashIn: Money.FromEgp(200m), Expenses: Money.FromEgp(260m), Withdrawals: Money.FromEgp(150m),
            SafeDrops: Money.FromEgp(1000m), ExpectedCash: Money.FromEgp(3505m), ActualCash: Money.FromEgp(3470m),
            Difference: Money.FromEgp(-35m));

        var slip = PrintRules.ComposeShiftCloseTicket(Shift(), Now.AddHours(9), cash,
            new[] { new ExpenseSummaryLine("مستلزمات", Money.FromEgp(260m)) },
            new[] { "طلبات بانتظار تأكيد تحويل إلكتروني: 2" }, note: "باقي عميل لم يُستلم");
        var text = TextOf(slip.Lines);

        foreach (var label in new[] { "إغلاق وتسليم وردية", "رصيد الافتتاح", "مبيعات الكاش", "إيداعات / إضافات",
                                      "المصاريف", "المرتجعات", "المسحوبات", "توريد للخزنة",
                                      "النقدية المتوقعة", "النقدية الفعلية", "إجمالي المصاريف", "تنبيهات قبل التسليم" })
            Assert.Contains(label, text);

        Assert.Contains("الفرق - عجز", text);
        Assert.Contains("35 ج", text);                       // the shortage, unsigned
        Assert.DoesNotContain(text, l => l.Contains("-35"));
        Assert.Contains(text, l => l.Contains("طلبات بانتظار"));
        Assert.Contains(text, l => l.Contains("باقي عميل لم يُستلم"));

        // The stated figures reconcile: opening + (sales - refunds) + in - expenses - out = expected.
        var reconciled = cash.OpeningCash + (cash.CashSales - cash.Refunds) + cash.CashIn
            - cash.Expenses - cash.Withdrawals - cash.SafeDrops;
        Assert.Equal(cash.ExpectedCash, reconciled);
    }

    [Fact]
    public void The_expense_slip_states_the_amount_category_payer_and_account()
    {
        var slip = PrintRules.ComposeExpenseTicket(new ExpenseSlipInfo(
            "فالح أبو العنبه", "ألذ طعم", "exp-1", Now, Money.FromEgp(160m), "مستلزمات مطبخ",
            "شراء أكياس", "أحمد محمد", "درج الكاشير", "اعتماد: مدير المحل"));
        var text = TextOf(slip.Lines);

        Assert.Contains("مصروف", text);
        Assert.Contains("160 ج", text);
        Assert.Contains("مستلزمات مطبخ", text);
        Assert.Contains("درج الكاشير", text);
        Assert.Contains("أحمد محمد", text);
        Assert.Contains(text, l => l.Contains("شراء أكياس"));
        Assert.Contains(text, l => l.Contains("اعتماد"));
        Assert.Empty(slip.Lines.OfType<TicketQr>());
    }
}
