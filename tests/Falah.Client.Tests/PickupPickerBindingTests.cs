using System.Xml.Linq;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// UI pass 2026-11-08: the pickup editor bound CalendarDatePicker.Date and TimePicker.SelectedTime to
/// the view model's nullable PickupDate/PickupTime. Bound, a null reached the pickers as a default value,
/// so a pickup with no time showed "1/1/1926" and "0:00" as if a time had been chosen. A converter did not
/// help. The view now copies the values in code-behind. This keeps the binding from coming back.
/// </summary>
public class PickupPickerBindingTests
{
    private static readonly System.Xml.Linq.XName XName = System.Xml.Linq.XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");

    private static XDocument PickupView()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Falah.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return XDocument.Load(Path.Combine(dir!.FullName, "src", "Falah.Client.CashierWinUI", "Views", "PickupDetailsView.xaml"));
    }

    [Fact]
    public void The_optional_pickup_pickers_are_not_bound_and_are_wired_to_the_code_behind()
    {
        var elements = PickupView().Descendants().ToList();
        var date = Assert.Single(elements, e => e.Name.LocalName == "CalendarDatePicker");

        Assert.Null(date.Attribute("Date"));
        Assert.Equal("OnPickupDateChanged", (string?)date.Attribute("DateChanged"));
        // Audit 2026-11-25 U16: the time is two lists (hour, minute) with Arabic placeholders - the framework TimePicker
        // showed "hour"/"minute" in English. Still not bound (a bound null showed "0:00"); the code-behind owns them.
        Assert.DoesNotContain(elements, e => e.Name.LocalName == "TimePicker");
        var boxes = elements.Where(e => e.Name.LocalName == "ComboBox" && ((string?)e.Attribute(XName))?.EndsWith("Box") == true).ToList();
        Assert.Equal(new[] { "PickupHourBox", "PickupMinuteBox" }, boxes.Select(b => (string?)b.Attribute(XName)));
        Assert.All(boxes, b => Assert.Null(b.Attribute("SelectedItem")));
        Assert.All(boxes, b => Assert.Equal("OnPickupTimePartChanged", (string?)b.Attribute("SelectionChanged")));
        Assert.Equal(new[] { "الساعة", "الدقيقة" }, boxes.Select(b => (string?)b.Attribute("PlaceholderText")));
        Assert.Equal("اختر التاريخ", (string?)date.Attribute("PlaceholderText"));
    }
}
