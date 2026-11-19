using System.Xml.Linq;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// UI consistency pass 2026-11-09: ItemsWrapGrid sizes every cell to the FIRST product card. On the Call Center a
/// first card with no station chip and a one-line name was 303 px tall, which clipped every taller card below it.
/// Cards with the "مشويات" chip or a two-line name lost their bottom edge, and the fifth best seller disappeared.
/// A card's height must not depend on its content: the chip row and the name keep fixed heights.
/// </summary>
public class ProductCardHeightTests
{
    [Theory]
    [InlineData("Falah.Client.CashierWinUI")]
    [InlineData("Falah.Client.CallCenterWinUI")]
    public void Every_product_card_has_the_same_height(string client)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Falah.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var view = XDocument.Load(Path.Combine(dir!.FullName, "src", client, "Views", "ProductCatalogueView.xaml"));

        var name = Assert.Single(view.Descendants(), e => e.Name.LocalName == "TextBlock" && (string?)e.Attribute("Text") == "{Binding Product.Name}");
        Assert.NotNull(name.Attribute("Height"));
        Assert.Equal("2", (string?)name.Attribute("MaxLines"));
        Assert.Equal("{Binding Product.Name}", (string?)name.Attributes().FirstOrDefault(a => a.Name.LocalName == "ToolTipService.ToolTip"));

        var chip = Assert.Single(view.Descendants(), e => e.Name.LocalName == "TextBlock" && (string?)e.Attribute("Text") == "{Binding StationLabel}");
        var chipRow = chip.Parent!.Parent!;
        Assert.Equal("Grid", chipRow.Name.LocalName);
        Assert.NotNull(chipRow.Attribute("MinHeight"));
    }
}
