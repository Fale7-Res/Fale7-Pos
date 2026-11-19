using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// The Call Center had no dark theme: one flat colour dictionary, views binding brushes with
/// StaticResource and literal "White"/hex surfaces. On a Windows set to dark, WinUI's own controls
/// turned dark while these panels stayed light - two themes on one screen. These tests read the XAML
/// itself (no WinUI needed): every brush must exist in both themes with the same keys, and no view
/// may paint a surface with a literal colour the theme cannot reach.
/// </summary>
public class CallCenterThemeTests
{
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace P = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static string CallCenterDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Falah.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", "Falah.Client.CallCenterWinUI");
    }

    private static HashSet<string> KeysOf(XElement dictionary) =>
        dictionary.Elements().Select(e => (string?)e.Attribute(X + "Key")).OfType<string>().ToHashSet();

    [Fact]
    public void Every_colour_and_brush_is_defined_for_both_light_and_dark()
    {
        var root = XDocument.Load(Path.Combine(CallCenterDir(), "Styles", "Colors.xaml")).Root!;
        var themes = root.Element(P + "ResourceDictionary.ThemeDictionaries");
        Assert.NotNull(themes);
        var light = themes!.Elements().Single(e => (string?)e.Attribute(X + "Key") == "Light");
        var dark = themes.Elements().Single(e => (string?)e.Attribute(X + "Key") == "Dark");

        var lightKeys = KeysOf(light);
        var darkKeys = KeysOf(dark);
        Assert.Empty(lightKeys.Except(darkKeys));
        Assert.Empty(darkKeys.Except(lightKeys));
        // Brushes carry literal colours inside each palette: a {ThemeResource}/{StaticResource} colour
        // reference across theme dictionaries resolved Dark brushes to Light colours in the Cashier.
        Assert.DoesNotContain(light.Descendants().Concat(dark.Descendants()),
            e => e.Name == P + "SolidColorBrush" && ((string?)e.Attribute("Color"))?.StartsWith("{") == true);
        foreach (var required in new[] { "CardBackgroundBrush", "OverlayBrush", "AccentTextBrush", "TextPrimaryBrush", "SurfaceBrush" })
            Assert.Contains(required, lightKeys);
    }

    public static IEnumerable<object[]> Views() =>
        new[] { "Views", "Controls" }
            .SelectMany(d => Directory.GetFiles(Path.Combine(CallCenterDir(), d), "*.xaml"))
            .Append(Path.Combine(CallCenterDir(), "MainWindow.xaml"))
            .Select(f => new object[] { Path.GetRelativePath(CallCenterDir(), f) });

    [Theory]
    [MemberData(nameof(Views))]
    public void No_view_paints_with_a_colour_the_theme_cannot_change(string relativePath)
    {
        var text = File.ReadAllText(Path.Combine(CallCenterDir(), relativePath));
        var literal = Regex.Matches(text, "(Background|Foreground|BorderBrush|Fill)=\"(#[0-9A-Fa-f]{6,8}|White|Black)\"")
            .Select(m => m.Value).ToList();
        var staticBrush = Regex.Matches(text, @"\{StaticResource \w+Brush\}").Select(m => m.Value).ToList();
        Assert.Empty(literal);
        Assert.Empty(staticBrush);
    }
}
