using System.Text.Json;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// UI pass 2026-11-08: every row of the menu screen had a button named just "تبديل التوفر". A screen reader
/// heard the same words on every row, with nothing saying which product it would switch, or which way.
/// </summary>
public class MenuRowAccessibleNameTests
{
    private static MenuViewModel.MenuProductRow Row(bool available) => new()
    {
        Product = JsonSerializer.Deserialize<Falah.Application.Menu.MenuProductDto>(
            """{"id":"p","name":"كبدة اسكندراني","variants":[],"basePricePiastres":4500}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!,
        CategoryName = "سندوتشات",
        IsAvailable = available,
    };

    [Fact]
    public void An_available_product_says_it_will_be_made_unavailable()
    {
        var row = Row(true);
        Assert.Equal("إيقاف توفر كبدة اسكندراني", row.ToggleAccessibleName);
    }

    [Fact]
    public void An_unavailable_product_says_it_will_be_made_available_and_follows_the_change()
    {
        var row = Row(false);
        Assert.Equal("إتاحة كبدة اسكندراني", row.ToggleAccessibleName);
        string? changed = null;
        row.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(row.ToggleAccessibleName)) changed = row.ToggleAccessibleName; };
        row.IsAvailable = true;
        Assert.Equal("إيقاف توفر كبدة اسكندراني", changed);
    }
}
