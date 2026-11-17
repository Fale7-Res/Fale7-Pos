using Falah.Domain.Printing;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>UI-REQ-002: the closing statement's paper - sections, a shift crossing Cairo midnight,
/// drawer versus other-account expenses, the difference wording, and the recomputed banner.</summary>
public class ShiftCloseStatementCompositionTests
{
    private static ShiftCloseStatement Statement(DateTimeOffset opened, DateTimeOffset closed, long difference = 0,
        IReadOnlyList<string>? outstanding = null) => new(
        ShiftCloseStatement.KindName, 1, "فالح", null, "shift-abcdef123456", "123456", "مسائية", new DateOnly(2026, 11, 7),
        opened, closed, "أحمد", "منى", difference != 0 ? "المدير" : null,
        new ShiftStatementSales(3, 60_000, 0, 0, 0, 60_000, 0, 60_000, 40_000, 20_000, 0, 0, 0, 0, 60_000, 1),
        new[]
        {
            new ShiftStatementExpense("مستلزمات تشغيل", "ثلج", 3_000, true, "درج الكاشير", "أحمد", opened.AddHours(1)),
            new ShiftStatementExpense("صيانة سريعة", "ثلاجة", 5_000, false, "الخزينة الرئيسية", "المدير", opened.AddHours(2)),
        },
        new ShiftStatementCash(50_000, 40_000, 0, 0, 3_000, 0, 0, 87_000, 87_000 + difference, difference, 87_000 + difference, "الخزينة الرئيسية"),
        difference != 0 ? "عد مرة أخرى" : null,
        outstanding ?? Array.Empty<string>());

    private static List<string> Text(ComposedTicket ticket) =>
        ticket.Lines.SelectMany(l => l switch
        {
            TicketText t => new[] { t.Text },
            TicketRow r => r.Cells.Select(c => c.Text).ToArray(),
            _ => Array.Empty<string>(),
        }).ToList();

    [Fact]
    public void A_shift_crossing_cairo_midnight_shows_both_full_dates_and_its_own_business_day()
    {
        // 21:00 Cairo on 7 Nov -> 02:00 Cairo on 8 Nov (Cairo is UTC+2 in November).
        var text = Text(PrintRules.ComposeShiftCloseStatement(Statement(
            new DateTimeOffset(2026, 11, 7, 19, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 11, 8, 0, 0, 0, TimeSpan.Zero))));
        Assert.Contains(text, t => t.Contains("07-11-2026") && t.Contains("09:00"));
        Assert.Contains(text, t => t.Contains("08-11-2026") && t.Contains("02:00"));
        Assert.Contains(text, t => t.Contains("07-11-2026") && !t.Contains(":"));   // business day: the day it opened in
    }

    [Fact]
    public void Every_section_is_present_and_only_drawer_expenses_enter_the_cash_count()
    {
        var text = Text(PrintRules.ComposeShiftCloseStatement(Statement(DateTimeOffset.UtcNow.AddHours(-8), DateTimeOffset.UtcNow,
            outstanding: new[] { "تحويلات إلكترونية بانتظار التأكيد: 1 (#0131)" })));
        foreach (var heading in new[] { "المبيعات", "طرق الدفع", "المصروفات", "نقدية الدرج", "التسليم", "بنود مفتوحة عند التسليم" })
            Assert.Contains(heading, text);
        Assert.Contains(text, t => t.Contains("ثلج - من الدرج"));
        Assert.Contains(text, t => t.Contains("ثلاجة - من الخزينة الرئيسية"));
        Assert.Contains("مصروفات من الدرج", text);
        Assert.Contains("مصروفات من حسابات أخرى", text);
        Assert.Contains("طلبات ملغاة (خارج المبيعات)", text);
        Assert.Contains("أغلقها", text);                                  // closer differs from the cashier
        Assert.Contains("- تحويلات إلكترونية بانتظار التأكيد: 1 (#0131)", text);
        Assert.Contains("الفرق - مطابق", text);
        Assert.Contains("تسليم الكاشير", text);
        Assert.Contains("استلام المسؤول", text);
        Assert.DoesNotContain(text, t => t.Contains("محسوبة من السجلات الحالية"));
    }

    [Fact]
    public void A_shortage_says_so_with_its_reason_and_approver_and_a_recomputed_paper_says_it_was_recomputed()
    {
        var text = Text(PrintRules.ComposeShiftCloseStatement(Statement(DateTimeOffset.UtcNow.AddHours(-8), DateTimeOffset.UtcNow, difference: -1_000), recomputed: true));
        Assert.Contains("الفرق - عجز", text);
        Assert.Contains("10 ج", text);
        Assert.Contains(text, t => t.Contains("سبب الفرق: عد مرة أخرى"));
        Assert.Contains("اعتمد الفرق", text);
        Assert.Contains(text, t => t.Contains("محسوبة من السجلات الحالية"));
    }

    /// <summary>Schema 2 (2026-11-11): driver cash, payouts, an inherited float with a gap at opening, the float
    /// left for the next shift and the named receiver. A version-1 statement prints none of these lines.</summary>
    [Fact]
    public void The_handover_paper_states_driver_cash_the_inherited_and_retained_float_and_the_receiver()
    {
        var v1 = Statement(DateTimeOffset.UtcNow.AddHours(-8), DateTimeOffset.UtcNow);
        var v1Text = Text(PrintRules.ComposeShiftCloseStatement(v1));
        foreach (var absent in new[] { "توريدات السائقين", "عهدة متروكة", "المستلم", "عجز عند الاستلام" })
            Assert.DoesNotContain(v1Text, t => t.Contains(absent));

        var v2 = v1 with
        {
            SchemaVersion = ShiftCloseStatement.CurrentSchemaVersion,
            Cash = v1.Cash with
            {
                OpeningPiastres = 99_000, InheritedFloatPiastres = 100_000, CashInPiastres = 1_000,
                DriverCashPiastres = 27_500, DriverPayoutsPiastres = 2_500,
                RetainedFloatPiastres = 100_000, HandedOverPiastres = 25_000, HandoverReceiverName = "المدير",
            },
        };
        var text = Text(PrintRules.ComposeShiftCloseStatement(v2));
        Assert.Contains("عهدة متروكة من الوردية السابقة", text);
        Assert.Contains("عجز عند الاستلام (مسجل منفصلًا)", text);
        Assert.Contains("إيداعات / إضافات من الخزنة", text);
        Assert.Contains("توريدات السائقين", text);
        Assert.Contains("275 ج", text);
        Assert.Contains("صرف أجر توصيل للسائقين", text);
        Assert.Contains("عهدة متروكة في الدرج للوردية التالية", text);
        Assert.Contains("1,000 ج", text);   // paper redesign spec 2026-11-17, "Money on paper": thousands separators
        Assert.Contains("المستلم", text);
        Assert.Contains("المدير", text);
    }

    [Fact]
    public void A_drivers_daily_totals_count_his_fee_only_on_delivered_orders_and_state_the_net()
    {
        static Falah.Domain.Delivery.DriverStatementOrder Row(string status, long fee, long collected, bool cancelled = false) => new(
            $"o-{Guid.NewGuid():N}", "#1", DateTimeOffset.UtcNow, null, null, null, "cash", status,
            10_000, fee, 10_000 + fee, 0, 0, cancelled ? 0 : 10_000 + fee, collected, false, null, cancelled);

        var totals = Falah.Domain.Delivery.DriverStatementRules.Totals(new[]
        {
            Row("Delivered", 2_000, 12_000),
            Row("Delivered", 3_000, 13_000),
            Row("Failed", 1_500, 0, cancelled: true),
            Row("Returned", 2_500, 0),
        });
        Assert.Equal(5_000, totals.DriverFeesEarnedPiastres);
        Assert.Equal(25_000 - 5_000, totals.NetAfterFeesPiastres);

        var paper = Text(PrintRules.ComposeDriverStatement("فالح", null, "سائق", new DateOnly(2026, 11, 12), null,
            Array.Empty<Falah.Domain.Delivery.DriverStatementOrder>(), totals, detailed: false, "المدير", DateTimeOffset.UtcNow));
        Assert.Contains("أجر التوصيل المستحق للسائق", paper);
        Assert.Contains("50 ج", paper);
        Assert.Contains("صافي على السائق بعد أجره", paper);
        Assert.Contains("200 ج", paper);
    }
}
