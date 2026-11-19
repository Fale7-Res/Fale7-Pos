using System.Text.RegularExpressions;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// UI pass 2026-11-08: ten Cashier toasts painted white text on TextPrimaryBrush - dark in Light, but
/// near-white (#E6E9EE) in Dark, so the toast was unreadable; the offer banner in the size picker used
/// fixed light-green hex colours. Any literal surface or text colour in a view is a colour the theme
/// cannot change; views must use theme brushes.
/// </summary>
public class CashierThemeLiteralTests
{
    private static string CashierDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Falah.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", "Falah.Client.CashierWinUI");
    }

    public static IEnumerable<object[]> Views() =>
        new[] { "Views", "Controls" }
            .SelectMany(d => Directory.GetFiles(Path.Combine(CashierDir(), d), "*.xaml"))
            .Select(f => new object[] { Path.GetRelativePath(CashierDir(), f) });

    /// <summary>A malformed attribute (an edit that dropped a closing brace) only surfaced as an
    /// unexplained XamlCompiler exit code at native build time; a regex-based check above cannot see it.</summary>
    [Fact]
    public void Every_xaml_file_in_both_clients_is_well_formed()
    {
        var src = Path.GetDirectoryName(CashierDir())!;
        var bad = new[] { "Falah.Client.CashierWinUI", "Falah.Client.CallCenterWinUI" }
            .SelectMany(c => Directory.GetFiles(Path.Combine(src, c), "*.xaml", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => { try { System.Xml.Linq.XDocument.Load(f); return null; } catch (System.Xml.XmlException e) { return $"{f}: {e.Message}"; } })
            .OfType<string>().ToList();
        Assert.Empty(bad);
    }

    [Theory]
    [MemberData(nameof(Views))]
    public void No_view_paints_with_a_colour_the_theme_cannot_change(string relativePath)
    {
        var text = File.ReadAllText(Path.Combine(CashierDir(), relativePath));
        var literal = Regex.Matches(text, "(Background|Foreground|BorderBrush|Fill)=\"(#[0-9A-Fa-f]{6,8}|White|Black)\"")
            .Select(m => m.Value).ToList();
        Assert.Empty(literal);
    }
}
