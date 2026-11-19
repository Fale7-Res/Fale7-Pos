using System.Xml.Linq;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// UI pass 2026-11-08: the Shifts history's "إعادة طباعة إيصال الافتتاح/التسليم" set ShiftsViewModel.ReprintMessage,
/// but the Shifts screen never showed it. A reprint gave no sign it was sent, and a refused one ("تعذر إرسال
/// أمر إعادة الطباعة") was silent. The message must be on the screen the buttons are on.
/// </summary>
public class ShiftsReprintFeedbackTests
{
    [Fact]
    public void The_shifts_screen_shows_the_reprint_outcome()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Falah.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var view = XDocument.Load(Path.Combine(dir!.FullName, "src", "Falah.Client.CashierWinUI", "Views", "ShiftsView.xaml"));

        var message = Assert.Single(view.Descendants(), e => e.Name.LocalName == "TextBlock"
            && ((string?)e.Attribute("Text"))?.Contains("ReprintMessage") == true);
        Assert.Equal("Shifts_ReprintMessage", (string?)message.Attributes().FirstOrDefault(a => a.Name.LocalName == "AutomationProperties.AutomationId"));
    }
}
