using System.Text.RegularExpressions;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Printing;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>Paper redesign spec 2026-11-17, Amendment 2 (owner): the order-taking copy goes into the
/// restaurant (small, no money, no logo, no QR); the driver's paper goes to the customer (full
/// receipt with logo, prices, totals, what to collect and the menu QR).</summary>
public class DeliveryPapersTests
{
    private const string Menu = "https://example.test/menu";
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 17, 42, 0, TimeSpan.Zero);

    private static ReceiptHeader Header(string? menu = Menu, bool pending = false) =>
        new("ord-1", "فالح أبو العنبه", "ألذ سندوتش", "المعادي", "#0287", Now, "منى", OrderType.Delivery, null,
            "عميل تجريبي", "15 شارع 9", "المعادي", ElectronicPaymentPending: pending, MenuUrl: menu, IsPaid: false);

    private static readonly TicketItem[] Items =
    {
        new("كفتة", "نص كيلو", 1, Money.FromEgp(245), Money.FromEgp(245), "حار", null, null, "Charcoal"),
        new("بيبسي", null, 2, Money.FromEgp(20), Money.FromEgp(40), null, null, null, null),
    };

    private static readonly ReceiptTotals Totals = new(Money.FromEgp(285), Money.Zero, Money.Zero, null, Money.FromEgp(25), Money.Zero,
        Money.FromEgp(310), Money.Zero, Money.Zero, PaymentMethod.Cash);

    private static readonly DeliveryOrderTicketInfo Collect = new("01000000000", "الجرس بايظ", Money.FromEgp(310), true);

    private static List<string> Texts(ComposedTicket t) =>
        t.Lines.SelectMany(l => l switch
        {
            TicketText x => new[] { x.Text },
            TicketRow r => r.Cells.Select(c => c.Text).ToArray(),
            _ => Array.Empty<string>(),
        }).ToList();

    private static bool IsBand(TicketElement e, string title) => e is TicketRow { Inverted: true, Cells: [var only] } && only.Text == title;

    [Fact]
    public void The_order_taking_copy_is_a_small_prep_paper_without_money_logo_or_qr()
    {
        var t = PrintRules.ComposeReceiptTicket(PrintRoute.CallCenter, Header(), Items, Totals);
        var text = Texts(t);

        Assert.Empty(t.Lines.OfType<TicketLogo>());
        Assert.Empty(t.Lines.OfType<TicketQr>());
        Assert.DoesNotContain(text, s => Regex.IsMatch(s, @"\d+(,\d{3})*(\.\d+)? ج"));   // no amount anywhere
        Assert.DoesNotContain(t.Lines, e => IsBand(e, "الحساب"));
        Assert.Contains(t.Lines, e => IsBand(e, "الأصناف"));
        Assert.Contains(text, s => s.StartsWith("كفتة"));
        Assert.Contains(text, s => s.Contains("حار"));
        Assert.Contains(text, s => s.Contains("عميل تجريبي"));
        Assert.Contains(text, s => s.Contains("15 شارع 9"));
        Assert.Contains(t.Lines.OfType<TicketText>(), x => x.Text == "#0287" && x.Size == TicketTextSize.Huge);
    }

    /// <summary>Owner, 2026-11-18: a delivery order taken at the till printed the till's priced customer
    /// receipt at order time, as well as the driver's paper later - two customer receipts. It now prints
    /// the same «تحضير طلب» as the call center, on the till's own printer. Takeaway and pickup receipts
    /// are unchanged.</summary>
    [Fact]
    public void A_delivery_order_rung_up_at_the_till_prints_the_prep_paper_on_the_till_printer()
    {
        var t = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, Header(), Items, Totals);
        var text = Texts(t);

        Assert.Equal(PrintRoute.Receipt, t.Route);
        Assert.Contains(text, s => s == "تحضير طلب");
        Assert.DoesNotContain(text, s => Regex.IsMatch(s, @"\d+(,\d{3})*(\.\d+)? ج"));
        Assert.Empty(t.Lines.OfType<TicketLogo>());
        Assert.Contains(text, s => s.Contains("15 شارع 9"));

        var takeaway = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, Header() with { OrderType = OrderType.Takeaway }, Items, Totals);
        Assert.Contains(Texts(takeaway), s => s == "الحساب");
        Assert.DoesNotContain(Texts(takeaway), s => s == "تحضير طلب");
    }

    [Fact]
    public void The_order_taking_copy_keeps_the_unconfirmed_transfer_warning() =>
        Assert.Contains("غير مدفوع - في انتظار تأكيد التحويل",
            Texts(PrintRules.ComposeReceiptTicket(PrintRoute.CallCenter, Header(pending: true), Items, Totals)));

    /// <summary>Owner, 2026-11-17 (final review, Important 2): the prep paper prints at once even when
    /// preparation is held for an unconfirmed transfer, but says on a black band not to prepare yet.</summary>
    [Fact]
    public void An_unconfirmed_transfer_puts_a_do_not_prepare_band_on_the_prep_paper()
    {
        Assert.Contains(PrintRules.ComposeReceiptTicket(PrintRoute.CallCenter, Header(pending: true), Items, Totals).Lines,
            e => IsBand(e, "لا يُحضَّر قبل تأكيد التحويل"));
        Assert.DoesNotContain(PrintRules.ComposeReceiptTicket(PrintRoute.CallCenter, Header(pending: false), Items, Totals).Lines,
            e => IsBand(e, "لا يُحضَّر قبل تأكيد التحويل"));
    }

    [Fact]
    public void The_driver_paper_is_the_customers_full_receipt_with_the_menu_qr()
    {
        var t = PrintRules.ComposeDriverDispatchTicket(Header(), Items, Totals, Collect, "محمود السواق", Now.AddMinutes(20), isReassignment: false);
        var text = Texts(t);

        Assert.Equal(PrintRoute.DriverDispatch, t.Route);
        Assert.IsType<TicketLogo>(t.Lines[0]);
        Assert.Contains(t.Lines, e => IsBand(e, "الأصناف"));
        Assert.Contains(t.Lines, e => IsBand(e, "الحساب"));
        Assert.Contains("245 ج", text);
        Assert.Contains("310 ج", text);
        Assert.Contains(text, s => s.Contains("المطلوب تحصيله"));
        Assert.Contains(text, s => s.Contains("محمود السواق"));
        Assert.Contains(text, s => s.Contains("01000000000"));
        Assert.Contains(text, s => s.Contains("الجرس بايظ"));
        Assert.Equal(Menu, Assert.Single(t.Lines.OfType<TicketQr>()).Payload);
        Assert.DoesNotContain("إعادة إسناد", text);
    }

    /// <summary>Owner, 2026-11-25: an item's note ("حار", "من غير بصل") is for the kitchen - it prints on the prep
    /// paper only, never on the paper that travels with the driver. The delivery note stays: it is for the driver.</summary>
    [Fact]
    public void The_driver_paper_leaves_item_notes_to_the_prep_paper()
    {
        var driver = Texts(PrintRules.ComposeDriverDispatchTicket(Header(), Items, Totals, Collect, "محمود", Now, false));
        Assert.DoesNotContain(driver, s => s.Contains("حار"));
        Assert.Contains(driver, s => s.Contains("الجرس بايظ"));

        Assert.Contains(Texts(PrintRules.ComposeReceiptTicket(PrintRoute.CallCenter, Header(), Items, Totals)), s => s.Contains("حار"));
    }

    /// <summary>Owner, 2026-11-25: a line an offer gives free prints as a gift, not as "offer instead of 25 ج".</summary>
    [Fact]
    public void A_gift_line_prints_as_a_gift_and_an_offer_line_keeps_its_old_price()
    {
        var items = new[]
        {
            new TicketItem("كولا لتر", null, 1, Money.Zero, Money.Zero, null, "هدية الكولا", Money.FromEgp(25), null),
            new TicketItem("كفتة", null, 1, Money.FromEgp(80), Money.FromEgp(80), null, "عرض الكفتة", Money.FromEgp(95), null),
        };
        var text = Texts(PrintRules.ComposeDriverDispatchTicket(Header(), items, Totals, Collect, "محمود", Now, false));
        Assert.Contains(text, s => s == "هدية: هدية الكولا");
        Assert.Contains(text, s => s.StartsWith("عرض: عرض الكفتة") && s.Contains("بدل"));
        Assert.DoesNotContain(text, s => s.Contains("هدية الكولا") && s.Contains("بدل"));
    }

    [Fact]
    public void A_reassigned_driver_paper_says_so() =>
        Assert.Contains("إعادة إسناد", Texts(PrintRules.ComposeDriverDispatchTicket(
            Header(), Items, Totals, Collect, "محمود", Now, isReassignment: true)));

    [Fact]
    public void Without_a_menu_link_the_driver_paper_has_no_qr() =>
        Assert.Empty(PrintRules.ComposeDriverDispatchTicket(Header(menu: null), Items, Totals, Collect, "محمود", Now, false).Lines.OfType<TicketQr>());
}
