using Falah.Application.Printing;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Paper redesign 2026-11-17 (Amendment 1): the on-screen shift-close statement reads the
/// same paper the printer receives, and must keep showing section titles as headings now that they
/// print as black bands, while ignoring the logo it cannot draw.</summary>
public class StatementPaperPreviewTests
{
    [Fact]
    public void A_section_band_shows_as_a_centred_bold_heading()
    {
        var band = new TicketElementDto("row", Size: "large",
            Cells: new[] { new TicketCellDto("المبيعات", 100, "center", true) }, Inverted: true);
        var line = PaperLine.From(band);
        Assert.NotNull(line);
        Assert.True(line!.IsText);
        Assert.Equal("المبيعات", line.Text);
        Assert.True(line.Bold);
        Assert.True(line.Centered);
    }

    [Fact]
    public void A_label_value_row_still_shows_as_a_row() =>
        Assert.True(PaperLine.From(new TicketElementDto("row",
            Cells: new[] { new TicketCellDto("صافي المبيعات", 60, "start", false), new TicketCellDto("12,155 ج", 40, "end", false) }, FitStart: true))!.IsRow);

    [Fact]
    public void The_logo_is_not_shown_on_screen() =>
        Assert.Null(PaperLine.From(new TicketElementDto("logo", Align: "center")));
}
