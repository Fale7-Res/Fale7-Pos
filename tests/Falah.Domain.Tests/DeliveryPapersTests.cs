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

    [Fact]
    public void A_reassigned_driver_paper_says_so() =>
        Assert.Contains("إعادة إسناد", Texts(PrintRules.ComposeDriverDispatchTicket(
            Header(), Items, Totals, Collect, "محمود", Now, isReassignment: true)));

    [Fact]
    public void Without_a_menu_link_the_driver_paper_has_no_qr() =>
        Assert.Empty(PrintRules.ComposeDriverDispatchTicket(Header(menu: null), Items, Totals, Collect, "محمود", Now, false).Lines.OfType<TicketQr>());
}
