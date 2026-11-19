using Falah.Domain.Common;
using Falah.Domain.Printing;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>Paper redesign 2026-11-17, "Label/value lines": a label never wraps; the value wraps.</summary>
public class TicketLayoutTests
{
    [Fact]
    public void Slip_and_money_lines_fit_their_label()
    {
        var slip = PrintRules.ComposeExpenseTicket(new ExpenseSlipInfo("فالح", null, "exp-1",
            new DateTimeOffset(2026, 11, 17, 17, 42, 0, TimeSpan.Zero), Money.FromEgp(300), "مستلزمات تشغيل", "ثلج",
            "أحمد", "درج الكاشير", null));

        var labelValueRows = slip.Lines.OfType<TicketRow>().Where(r => r.Cells.Count == 2).ToList();
        Assert.NotEmpty(labelValueRows);
        Assert.All(labelValueRows, r => Assert.True(r.FitStart));
    }
}
