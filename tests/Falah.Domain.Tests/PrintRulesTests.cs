using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Printing;
using Xunit;

namespace Falah.Domain.Tests;

public class PrintRulesTests
{
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
        var ticket = PrintRules.ComposeKitchenTicket(PrintRoute.Charcoal, "ord-1", "#0001", OrderType.Takeaway, null, items, Now);

        var text = string.Join('\n', ticket.Lines.Select(l => l.Text));
        Assert.Contains("كباب", text);
        Assert.DoesNotContain("فراخ مشوية", text);
        Assert.DoesNotContain("عصير", text);
    }

    [Fact]
    public void A_kitchen_ticket_never_contains_a_price()
    {
        var items = new[] { Item("كباب", "Charcoal") };
        var ticket = PrintRules.ComposeKitchenTicket(PrintRoute.Charcoal, "ord-1", "#0001", OrderType.Takeaway, null, items, Now);

        var text = string.Join('\n', ticket.Lines.Select(l => l.Text));
        Assert.DoesNotContain("50.00", text);
        Assert.DoesNotContain(" ج", text);
    }

    [Fact]
    public void A_kitchen_ticket_surfaces_a_note_as_a_warning_line()
    {
        var items = new[] { Item("كباب", "Charcoal", notes: "بدون بصل") };
        var ticket = PrintRules.ComposeKitchenTicket(PrintRoute.Charcoal, "ord-1", "#0001", OrderType.Takeaway, null, items, Now);

        Assert.Contains(ticket.Lines, l => l.Text.Contains("بدون بصل"));
    }

    [Fact]
    public void Composing_a_kitchen_ticket_for_a_customer_facing_route_is_rejected()
    {
        Assert.Throws<DomainException>(() => PrintRules.ComposeKitchenTicket(PrintRoute.Receipt, "ord-1", "#0001", OrderType.Takeaway, null, Array.Empty<TicketItem>(), Now));
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

        var text = string.Join('\n', ticket.Lines.Select(l => l.Text));
        Assert.Contains("الإجمالي: 100", text);
        Assert.Contains("نقدي", text);
        Assert.Contains("#0001", text);
    }

    [Fact]
    public void A_delivery_receipt_shows_the_customer_and_area_but_a_dine_in_receipt_does_not()
    {
        var deliveryHeader = new ReceiptHeader("ord-1", "فالح", "مطاعم", "القاهرة", "#0001", Now, "أحمد", OrderType.Delivery, null, "سارة", "شارع 9", "المعادي");
        var dineInHeader = deliveryHeader with { OrderType = OrderType.DineIn, TableNumber = "3" };
        var totals = new ReceiptTotals(Money.FromEgp(100m), Money.Zero, Money.Zero, null, Money.FromEgp(10m), Money.Zero, Money.FromEgp(110m), Money.FromEgp(110m), Money.Zero, PaymentMethod.Cash);

        var deliveryTicket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, deliveryHeader, Array.Empty<TicketItem>(), totals);
        var dineInTicket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, dineInHeader, Array.Empty<TicketItem>(), totals);

        Assert.Contains(deliveryTicket.Lines, l => l.Text.Contains("المعادي"));
        Assert.DoesNotContain(dineInTicket.Lines, l => l.Text.Contains("المعادي"));
    }

    [Fact]
    public void A_zero_discount_and_zero_comp_never_print_a_bare_zero_line()
    {
        // A falsy-but-present zero previously slipped through in the reference app for delivery fee;
        // the same care applies here for discount/comp - only a POSITIVE value gets its own line.
        var header = new ReceiptHeader("ord-1", "فالح", "مطاعم", "القاهرة", "#0001", Now, "أحمد", OrderType.Takeaway, null, null, null, null);
        var totals = new ReceiptTotals(Money.FromEgp(100m), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(100m), Money.FromEgp(100m), Money.Zero, PaymentMethod.Cash);
        var ticket = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, header, Array.Empty<TicketItem>(), totals);

        Assert.DoesNotContain(ticket.Lines, l => l.Text.StartsWith("الخصم"));
        Assert.DoesNotContain(ticket.Lines, l => l.Text.StartsWith("ضيافة"));
    }
}
