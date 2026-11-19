using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Printing;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>Paper redesign spec 2026-11-17, Amendment 1 (owner): black section bands, ruled item
/// grids with notes inside their item, and the menu QR on the call-center copy only.</summary>
public class DesignAmendmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 17, 42, 0, TimeSpan.Zero);

    private static ReceiptHeader Header(OrderType type = OrderType.Takeaway) =>
        new("ord-1", "فالح أبو العنبه", "ألذ سندوتش", "المعادي", "#0286", Now, "أحمد", type, null,
            type == OrderType.Delivery ? "عميل" : null, type == OrderType.Delivery ? "15 شارع 9" : null, type == OrderType.Delivery ? "المعادي" : null,
            MenuUrl: "https://example.test/menu");

    private static readonly TicketItem[] Items =
    {
        new("كفتة", "نص كيلو", 1, Money.FromEgp(245), Money.FromEgp(245), "حار", null, null, "Charcoal"),
        new("بيبسي", null, 2, Money.FromEgp(20), Money.FromEgp(40), null, null, null, null),
    };

    private static readonly ReceiptTotals Totals = new(Money.FromEgp(285), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero,
        Money.FromEgp(285), Money.FromEgp(300), Money.FromEgp(15), PaymentMethod.Cash);

    private static bool IsBand(TicketElement e, string title) =>
        e is TicketRow { Inverted: true, Cells: [var only] } && only.Text == title;

    private static void AssertBoxedItemGrid(ComposedTicket t)
    {
        var header = t.Lines.OfType<TicketRow>().Single(r => r.Cells.Count >= 3 && r.Cells[0].Text == "الصنف");
        Assert.True(header.Boxed);
        var kofta = t.Lines.OfType<TicketRow>().Single(r => r.Cells.Count >= 3 && r.Cells[0].Text.StartsWith("كفتة"));
        Assert.True(kofta.Boxed);
        var index = t.Lines.ToList().IndexOf(kofta);
        var note = Assert.IsType<TicketRow>(t.Lines[index + 1]);
        Assert.True(note.Boxed && note.Continuation, "the item's note sits inside the grid, joined to its item");
        Assert.Contains("حار", note.Cells.Single().Text);
    }

    [Fact]
    public void Neither_the_takeaway_receipt_nor_the_order_taking_copy_carries_the_menu_qr()
    {
        // Amendment 2 moved the QR from the call-center copy to the driver's paper (DeliveryPapersTests).
        Assert.Empty(PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, Header(), Items, Totals).Lines.OfType<TicketQr>());
        Assert.Empty(PrintRules.ComposeReceiptTicket(PrintRoute.CallCenter, Header(OrderType.Delivery), Items, Totals).Lines.OfType<TicketQr>());
    }

    [Fact]
    public void Papers_title_their_sections_on_black_bands_and_rule_their_items()
    {
        var receipt = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, Header(), Items, Totals);
        Assert.Contains(receipt.Lines, e => IsBand(e, "الأصناف"));
        Assert.Contains(receipt.Lines, e => IsBand(e, "الحساب"));
        AssertBoxedItemGrid(receipt);

        // The order-taking copy has items but no money (Amendment 2): an items band, no totals band.
        var callCenter = PrintRules.ComposeReceiptTicket(PrintRoute.CallCenter, Header(OrderType.Delivery), Items, Totals);
        Assert.Contains(callCenter.Lines, e => IsBand(e, "الأصناف"));
        Assert.DoesNotContain(callCenter.Lines, e => IsBand(e, "الحساب"));
        AssertBoxedItemGrid(callCenter);
    }

    [Fact]
    public void Kitchen_and_delivery_item_tables_are_ruled_grids()
    {
        AssertBoxedItemGrid(PrintRules.ComposeKitchenTicket(PrintRoute.Charcoal, "فالح أبو العنبه", "o", "#0286", OrderType.Takeaway, Items, Now));
        var delivery = PrintRules.ComposeDeliveryOrderTicket(Header(OrderType.Delivery), Items, Totals,
            new DeliveryOrderTicketInfo("0100", null, Money.FromEgp(285), true));
        Assert.Contains(delivery.Lines, e => IsBand(e, "الأصناف"));
        Assert.Contains(delivery.Lines, e => IsBand(e, "الحساب"));
        AssertBoxedItemGrid(delivery);
    }

    [Fact]
    public void Slip_sections_open_on_black_bands()
    {
        var statement = PrintRules.ComposeShiftCloseStatement(new ShiftCloseStatement(
            ShiftCloseStatement.KindName, 1, "فالح أبو العنبه", null, "shift-abcdef123456", "123456", "مسائية", new DateOnly(2026, 11, 17),
            Now.AddHours(-8), Now, "أحمد", "منى", null,
            new ShiftStatementSales(3, 60_000, 0, 0, 0, 60_000, 0, 60_000, 40_000, 20_000, 0, 0, 0, 0, 60_000, 1),
            Array.Empty<ShiftStatementExpense>(),
            new ShiftStatementCash(50_000, 40_000, 0, 0, 0, 0, 0, 90_000, 90_000, 0, 90_000, "الخزينة الرئيسية"),
            null, Array.Empty<string>()));
        foreach (var title in new[] { "المبيعات", "طرق الدفع", "نقدية الدرج", "التسليم" })
            Assert.Contains(statement.Lines, e => IsBand(e, title));
    }
}
