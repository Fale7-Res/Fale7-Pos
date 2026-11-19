using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Printing;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>Paper redesign 2026-11-17, spec Goal 2 (logo on customer papers only) and Goal 5
/// (the cashier receipt becomes one piece; the call-center copy converges with it).</summary>
public class CustomerPaperTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 17, 42, 0, TimeSpan.Zero);

    private static ReceiptHeader Header(OrderType type = OrderType.Takeaway, bool pending = false, string? footer = null) =>
        new("ord-1", "فالح أبو العنبه", "ألذ سندوتش", "المعادي", "#0286", Now, "أحمد", type, null,
            type == OrderType.Delivery ? "عميل" : null, type == OrderType.Delivery ? "15 شارع 9" : null, type == OrderType.Delivery ? "المعادي" : null,
            ElectronicPaymentPending: pending, Footer: footer, MenuUrl: "https://example.test/menu");

    private static readonly TicketItem[] Items =
    {
        new("كفتة", "نص كيلو", 1, Money.FromEgp(245), Money.FromEgp(245), "حار", null, null, "Charcoal"),
        new("بيبسي", null, 2, Money.FromEgp(20), Money.FromEgp(40), null, null, null, null),
    };

    private static readonly ReceiptTotals Totals = new(Money.FromEgp(285), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero,
        Money.FromEgp(285), Money.FromEgp(300), Money.FromEgp(15), PaymentMethod.Cash);

    private static List<string> Texts(IEnumerable<TicketElement> lines) =>
        lines.SelectMany(l => l switch
        {
            TicketText t => new[] { t.Text },
            TicketRow r => r.Cells.Select(c => c.Text).ToArray(),
            _ => Array.Empty<string>(),
        }).ToList();

    /// <summary>Owner, 2026-11-18 (trial on paper): the cashier receipt drops the logo and the brand
    /// sign-off at its end. «فالح أبو العنبه» and the sub-name stay at the top, once. The driver's paper
    /// (the delivery customer's receipt) keeps its logo.</summary>
    [Fact]
    public void Cashier_receipt_is_one_piece_led_by_the_name_with_no_logo_and_no_sign_off()
    {
        var t = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, Header(), Items, Totals);
        var text = Texts(t.Lines);
        Assert.Empty(t.Lines.OfType<TicketCut>());
        Assert.Empty(t.Lines.OfType<TicketLogo>());
        Assert.Equal("فالح أبو العنبه", text[0]);
        Assert.Equal(1, text.Count(s => s == "فالح أبو العنبه"));
        Assert.Equal(1, text.Count(s => s == "ألذ سندوتش"));
        // The big number the customer now collects with.
        Assert.Contains(t.Lines.OfType<TicketText>(), x => x.Text == "#0286" && x.Size >= TicketTextSize.Huge);
        // It now ends with the money, not with the brand repeated.
        Assert.NotEqual("ألذ سندوتش", text.Last());
    }

    [Fact]
    public void The_driver_paper_still_carries_the_logo()
    {
        var driver = PrintRules.ComposeDriverDispatchTicket(Header(OrderType.Delivery), Items, Totals,
            new DeliveryOrderTicketInfo("01000000000", null, Money.FromEgp(285), true), "محمود", Now, isReassignment: false);
        Assert.IsType<TicketLogo>(driver.Lines[0]);
    }

    [Fact]
    public void Call_center_copy_is_the_restaurants_prep_paper_with_the_customer_block()
    {
        // Amendment 2 (owner): printed when the order is taken, it goes into the restaurant.
        var callCenter = PrintRules.ComposeReceiptTicket(PrintRoute.CallCenter, Header(OrderType.Delivery), Items, Totals);

        Assert.Empty(callCenter.Lines.OfType<TicketLogo>());
        Assert.Empty(callCenter.Lines.OfType<TicketQr>());
        var text = Texts(callCenter.Lines);
        Assert.Contains(text, l => l.Contains("15 شارع 9"));
        Assert.Contains(text, l => l.Contains("المعادي"));
        Assert.Contains(text, l => l.StartsWith("كفتة"));
        Assert.DoesNotContain(text, l => l.Contains("245 ج"));
    }

    [Fact]
    public void Call_center_copy_keeps_its_pending_transfer_warning_and_closes_as_a_prep_paper()
    {
        var callCenter = PrintRules.ComposeReceiptTicket(PrintRoute.CallCenter, Header(OrderType.Delivery, pending: true, footer: "نورتونا"), Items, Totals);
        var text = Texts(callCenter.Lines);
        Assert.Contains("غير مدفوع - في انتظار تأكيد التحويل", text);
        // The customer-facing footer moved to the driver's paper (PrintRulesTests.The_driver_paper_ends_...).
        Assert.Equal("بون تحضير - يُسلَّم للمطعم", text.Last());
    }

    [Fact]
    public void Reprint_banner_precedes_the_logo()
    {
        // The driver's paper is the customer paper that still carries the logo (owner, 2026-11-18).
        var driver = PrintRules.ComposeDriverDispatchTicket(Header(OrderType.Delivery), Items, Totals,
            new DeliveryOrderTicketInfo("01000000000", null, Money.FromEgp(285), true), "محمود", Now, isReassignment: false);
        var reprint = PrintRules.MarkAsReprint(driver, Now.AddMinutes(5), "مدير المحل");
        var logoAt = reprint.Lines.ToList().FindIndex(e => e is TicketLogo);
        var bannerAt = reprint.Lines.ToList().FindIndex(e => e is TicketText { Text: "إعادة طباعة" });
        Assert.Equal(0, bannerAt);
        Assert.True(logoAt > bannerAt);
    }

    [Fact]
    public void Kitchen_and_slips_carry_no_logo()
    {
        var kitchen = PrintRules.ComposeKitchenTicket(PrintRoute.Charcoal, "فالح", "ord-1", "#0286", OrderType.Takeaway, Items, Now);
        var expense = PrintRules.ComposeExpenseTicket(new ExpenseSlipInfo("فالح", null, "e1", Now, Money.FromEgp(300), "تشغيل", "ثلج", "أحمد", "الدرج", null));
        Assert.Empty(kitchen.Lines.OfType<TicketLogo>());
        Assert.Empty(expense.Lines.OfType<TicketLogo>());
    }
}
