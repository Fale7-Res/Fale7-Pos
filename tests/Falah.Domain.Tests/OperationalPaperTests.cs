using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Printing;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>Paper redesign 2026-11-17, "Internal header" and "Order number": kitchen, delivery-order
/// and dispatch papers open the same way and show the order number at one size.</summary>
public class OperationalPaperTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 17, 42, 0, TimeSpan.Zero);
    private static readonly TicketItem[] Items =
    {
        new("كفتة", "نص كيلو", 1, Money.FromEgp(245), Money.FromEgp(245), null, null, null, "Charcoal"),
    };

    private static List<string> Texts(ComposedTicket t) =>
        t.Lines.SelectMany(l => l switch
        {
            TicketText x => new[] { x.Text },
            TicketRow r => r.Cells.Select(c => c.Text).ToArray(),
            _ => Array.Empty<string>(),
        }).ToList();

    private static void AssertInternalHeader(ComposedTicket t, string title)
    {
        var name = Assert.IsType<TicketText>(t.Lines[0]);
        Assert.Equal("فالح أبو العنبه", name.Text);
        Assert.True(name.Bold);
        Assert.Equal(TicketTextSize.Normal, name.Size);
        var heading = Assert.IsType<TicketText>(t.Lines[1]);
        Assert.Equal(title, heading.Text);
        Assert.Equal(TicketTextSize.Title, heading.Size);
        Assert.Contains(t.Lines.OfType<TicketText>(), x => x.Text == "#0286" && x.Size == TicketTextSize.Huge);
        Assert.Contains(t.Lines.OfType<TicketRule>(), r => r.Style == TicketRuleStyle.Heavy);
        Assert.Empty(t.Lines.OfType<TicketLogo>());
    }

    [Fact]
    public void Kitchen_tickets_open_with_the_internal_header() =>
        AssertInternalHeader(PrintRules.ComposeKitchenTicket(PrintRoute.Charcoal, "فالح أبو العنبه", "o", "#0286", OrderType.Takeaway, Items, Now), "الفحم");

    [Fact]
    public void The_delivery_order_opens_with_the_internal_header()
    {
        var header = new ReceiptHeader("o", "فالح أبو العنبه", null!, "", "#0286", Now, "منى", OrderType.Delivery, null, "عميل", "شارع 9", "المعادي");
        var totals = new ReceiptTotals(Money.FromEgp(245), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(245), Money.Zero, Money.Zero, PaymentMethod.Cash);
        AssertInternalHeader(PrintRules.ComposeDeliveryOrderTicket(header, Items, totals,
            new DeliveryOrderTicketInfo("0100", null, Money.FromEgp(245), true)), "أوردر دليفري");
    }

    // The driver dispatch paper is no longer an internal paper: Amendment 2 made it the delivery
    // customer's receipt. Its layout and the reassignment marker are pinned in DeliveryPapersTests.

    [Fact]
    public void Operational_papers_close_with_the_shared_footer()
    {
        var kitchen = PrintRules.ComposeKitchenTicket(PrintRoute.Grill, "فالح أبو العنبه", "o", "#0286", OrderType.Takeaway, Items, Now);
        Assert.Equal("يُسلَّم هذا البون للمسؤول عن المحطة", Assert.IsType<TicketText>(kitchen.Lines[^1]).Text);
        // The grid's own bottom edge closes it; a thin rule right under it read as a doubled line
        // (final review, Minor 7).
        Assert.True(Assert.IsType<TicketRow>(kitchen.Lines[^2]).Boxed);
    }
}
