using Falah.Application.Menu;
using Xunit;
using Cashier = Falah.Client.CashierWinUI.Models;
using CallCenter = Falah.Client.CallCenterWinUI.Models;

namespace Falah.Client.Tests;

/// <summary>Native UI pass 2026-11-08: a screen reader (and UI Automation) named every product tile
/// "ProductTile { Product = MenuProductDto { Id = prod-demo-shawarma, … }" and every category tile
/// "Falah.Client.CashierWinUI.Models.CategoryTile". A GridView item with no explicit name is named by
/// its data item's ToString(), so the tiles must say what a person sees on them.</summary>
public class TileAccessibleNameTests
{
    private static readonly MenuProductDto Shawarma = (MenuProductDto)Activator.CreateInstance(typeof(MenuProductDto),
        typeof(MenuProductDto).GetConstructors()[0].GetParameters().Select(p => Default(p)).ToArray())!;

    private static object? Default(System.Reflection.ParameterInfo p) => p.Name switch
    {
        "Id" => "prod-demo-shawarma",
        "Name" => "شاورما فراخ",
        "BasePricePiastres" => 5000L,
        "Variants" => new List<MenuVariantDto>(),
        _ when p.HasDefaultValue => p.DefaultValue,
        _ when p.ParameterType == typeof(string) => "",
        _ when p.ParameterType.IsValueType => Activator.CreateInstance(p.ParameterType),
        _ when p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) =>
            Activator.CreateInstance(typeof(List<>).MakeGenericType(p.ParameterType.GetGenericArguments()[0])),
        _ => null,
    };

    private static MenuCategoryDto Category() => (MenuCategoryDto)Activator.CreateInstance(typeof(MenuCategoryDto),
        typeof(MenuCategoryDto).GetConstructors()[0].GetParameters()
            .Select(p => p.Name == "Name" ? "مشويات فالح" : Default(p)).ToArray())!;

    [Fact]
    public void A_cashier_product_tile_is_named_by_its_product_and_price()
    {
        var name = new Cashier.ProductTile(Shawarma, Array.Empty<Cashier.OfferChoice>()).ToString();
        Assert.Contains("شاورما فراخ", name);
        Assert.Contains("50.00", name);
        Assert.DoesNotContain("ProductTile", name);
    }

    [Fact]
    public void A_call_center_product_tile_is_named_by_its_product_and_price()
    {
        var name = new CallCenter.ProductTile(Shawarma, Array.Empty<CallCenter.OfferChoice>()).ToString();
        Assert.Contains("شاورما فراخ", name);
        Assert.DoesNotContain("MenuProductDto", name);
    }

    /// <summary>The offer pill's content is a Grid of two TextBlocks, so the Button had no name at all;
    /// it now binds AutomationProperties.Name to this.</summary>
    [Fact]
    public void An_offer_pill_is_named_by_the_offer_and_its_price()
    {
        var offer = new MenuOfferDto("off-1", "عرض الشاورما", 4000);
        Assert.Equal("عرض الشاورما 40.00 ج", new Cashier.OfferChoice(Shawarma, offer).AccessibleName);
        Assert.Equal("عرض الشاورما 40.00 ج", new CallCenter.OfferChoice(Shawarma, offer).AccessibleName);
    }

    [Fact]
    public void Category_tiles_are_named_by_the_category()
    {
        Assert.Equal("مشويات فالح", new Cashier.CategoryTile(Category()).ToString());
        Assert.Equal("مشويات فالح", new CallCenter.CategoryTile(Category()).ToString());
    }
}
