using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Domain.Printing;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>Paper redesign 2026-11-17, "Internal header" and "Label/value lines" on the slips:
/// driver statements, the shift close statement and the expense voucher.</summary>
public class SlipPaperTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 17, 42, 0, TimeSpan.Zero);

    private static ShiftCloseStatement Statement(params string[] outstanding) => new(
        ShiftCloseStatement.KindName, 1, "فالح أبو العنبه", "ألذ سندوتش", "shift-abcdef123456", "123456", "مسائية", new DateOnly(2026, 11, 17),
        Now.AddHours(-8), Now, "أحمد", "منى", null,
        new ShiftStatementSales(3, 60_000, 0, 0, 0, 60_000, 0, 60_000, 40_000, 20_000, 0, 0, 0, 0, 60_000, 1),
        Array.Empty<ShiftStatementExpense>(),
        new ShiftStatementCash(50_000, 40_000, 0, 0, 0, 0, 0, 90_000, 90_000, 0, 90_000, "الخزينة الرئيسية"),
        null, outstanding);

    private static void AssertInternalHeader(ComposedTicket t, string title)
    {
        var name = Assert.IsType<TicketText>(t.Lines[0]);
        Assert.Equal("فالح أبو العنبه", name.Text);
        Assert.Equal(TicketTextSize.Normal, name.Size);
        var heading = Assert.IsType<TicketText>(t.Lines[1]);
        Assert.Equal(title, heading.Text);
        Assert.Equal(TicketTextSize.Title, heading.Size);
        Assert.Equal(TicketRuleStyle.Heavy, Assert.IsType<TicketRule>(t.Lines[2]).Style);
    }

    [Fact]
    public void Slips_open_with_the_internal_header()
    {
        AssertInternalHeader(PrintRules.ComposeExpenseTicket(new ExpenseSlipInfo("فالح أبو العنبه", "ألذ سندوتش", "e1", Now,
            Money.FromEgp(300), "تشغيل", "ثلج", "أحمد", "الدرج", null)), "مصروف");
        AssertInternalHeader(PrintRules.ComposeShiftCloseStatement(Statement()), "إغلاق وتسليم وردية");
        var totals = DriverStatementRules.Totals(Array.Empty<DriverStatementOrder>());
        AssertInternalHeader(PrintRules.ComposeDriverStatement("فالح أبو العنبه", "ألذ سندوتش", "محمود", new DateOnly(2026, 11, 17), null,
            Array.Empty<DriverStatementOrder>(), totals, false, "المدير", Now), "كشف يومية سائق - ملخص");
    }

    [Fact]
    public void Statement_label_rows_are_fitted_and_order_numbers_stay_isolated()
    {
        var paper = PrintRules.ComposeShiftCloseStatement(Statement("تحويلات إلكترونية بانتظار التأكيد: 1 (‎#0131‎)"));
        var twoCell = paper.Lines.OfType<TicketRow>().Where(r => r.Cells.Count == 2).ToList();
        Assert.NotEmpty(twoCell);
        Assert.All(twoCell, r => Assert.True(r.FitStart));
        Assert.Contains(paper.Lines.OfType<TicketText>(), t => t.Text.Contains("(‎#0131‎)"));
    }
}
