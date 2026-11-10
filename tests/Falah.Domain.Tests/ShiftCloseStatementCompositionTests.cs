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
}
