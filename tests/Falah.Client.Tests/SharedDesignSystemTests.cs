using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// UI consistency pass 2026-11-09: the Call Center had its own green palette (brand #156B5A, green-tinted
/// surfaces and text), seven private brushes and ten differing control styles, so the two active clients
/// looked like different products. Both clients now carry the same three style dictionaries, with the Cashier's
/// as the reference. A change to one must be made to both.
/// </summary>
public class SharedDesignSystemTests
{
    private static string Src()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Falah.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src");
    }

    [Theory]
    [InlineData("Colors.xaml")]
    [InlineData("Typography.xaml")]
    [InlineData("Controls.xaml")]
    public void Both_clients_use_the_same_style_dictionary(string file)
    {
        static string Read(string p) => File.ReadAllText(p).Replace("\r\n", "\n").Trim();
        var cashier = Read(Path.Combine(Src(), "Falah.Client.CashierWinUI", "Styles", file));
        var callCenter = Read(Path.Combine(Src(), "Falah.Client.CallCenterWinUI", "Styles", file));
        Assert.True(cashier == callCenter, $"Styles/{file} differs between the Cashier and the Call Center.");
    }

    [Fact]
    public void The_call_center_no_longer_uses_its_private_palette()
    {
        var retired = new[] { "BrandTintBrush", "SubtleSurfaceBrush", "InputSurfaceBrush", "SuccessSoftBrush", "OfferBorderBrush", "OfferTextBrush", "SegmentTextBrush" };
        var root = Path.Combine(Src(), "Falah.Client.CallCenterWinUI");
        var hits = Directory.GetFiles(root, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(f => retired.Where(k => File.ReadAllText(f).Contains(k)).Select(k => $"{Path.GetFileName(f)}: {k}"))
            .ToList();
        Assert.Empty(hits);
    }
}
