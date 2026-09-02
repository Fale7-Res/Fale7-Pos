using Falah.Domain.Common;
using Falah.Domain.Inventory;
using Xunit;

namespace Falah.Domain.Tests;

public class CostingRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);

    private static CostingContext EmptyContext(IReadOnlyList<InventoryItem>? items = null, IReadOnlyList<StockMovementFacts>? movements = null,
        IReadOnlyList<PrepRecipe>? prepRecipes = null, IReadOnlyList<PrepRecipeLine>? prepLines = null) =>
        new(items ?? Array.Empty<InventoryItem>(), prepRecipes ?? Array.Empty<PrepRecipe>(), prepLines ?? Array.Empty<PrepRecipeLine>(), movements ?? Array.Empty<StockMovementFacts>());

    [Fact]
    public void An_ingredient_with_no_purchase_history_has_an_unknown_cost_not_a_zero_cost()
    {
        var item = new InventoryItem("inv-1", "طماطم", CostSource.Purchase);
        var (unitCost, source) = CostingRules.IngredientUnitCost(item, EmptyContext(new[] { item }));

        Assert.Null(unitCost);
        Assert.Equal(CostResultSource.Unknown, source);
    }

    [Fact]
    public void Ingredient_cost_comes_from_the_most_recent_purchase()
    {
        var item = new InventoryItem("inv-1", "طماطم", CostSource.Purchase);
        var movements = new[]
        {
            new StockMovementFacts("inv-1", StockMovementType.PurchaseReceipt, Money.FromEgp(10m), Now.AddDays(-5)),
            new StockMovementFacts("inv-1", StockMovementType.PurchaseReceipt, Money.FromEgp(12m), Now.AddDays(-1)),
        };
        var (unitCost, source) = CostingRules.IngredientUnitCost(item, EmptyContext(new[] { item }, movements));

        Assert.Equal(Money.FromEgp(12m), unitCost);
        Assert.Equal(CostResultSource.Purchase, source);
    }

    [Fact]
    public void A_prep_recipe_missing_one_input_costs_unknown_even_though_90_percent_is_known()
    {
        var tahini = new InventoryItem("inv-tahini", "طحينة", CostSource.Prep);
        var sesame = new InventoryItem("inv-sesame", "سمسم", CostSource.Purchase);
        var lemon = new InventoryItem("inv-lemon", "ليمون", CostSource.Purchase); // never purchased

        var prep = new PrepRecipe("prep-1", "inv-tahini", OutputQuantityMilliUnits: 1000, Active: true);
        var lines = new[]
        {
            new PrepRecipeLine("prep-1", "inv-sesame", 800),
            new PrepRecipeLine("prep-1", "inv-lemon", 200),
        };
        var movements = new[] { new StockMovementFacts("inv-sesame", StockMovementType.PurchaseReceipt, Money.FromEgp(1m), Now) };
        var ctx = EmptyContext(new[] { tahini, sesame, lemon }, movements, new[] { prep }, lines);

        var result = CostingRules.ComputePrepRecipeCost(prep, lines, ctx);

        Assert.Null(result.UnitCost);
        Assert.Contains("ليمون", result.Missing);
    }

    [Fact]
    public void A_fully_known_prep_recipe_resolves_to_a_per_unit_cost()
    {
        var sesame = new InventoryItem("inv-sesame", "سمسم", CostSource.Purchase);
        var prep = new PrepRecipe("prep-1", "inv-tahini", OutputQuantityMilliUnits: 1000, Active: true);
        var lines = new[] { new PrepRecipeLine("prep-1", "inv-sesame", 1000) };
        var movements = new[] { new StockMovementFacts("inv-sesame", StockMovementType.PurchaseReceipt, Money.FromEgp(1m), Now) };
        var ctx = EmptyContext(new[] { sesame }, movements, new[] { prep }, lines);

        var result = CostingRules.ComputePrepRecipeCost(prep, lines, ctx);

        Assert.Equal(Money.FromEgp(1m), result.UnitCost);
        Assert.Empty(result.Missing);
    }

    [Fact]
    public void A_yield_line_without_its_conversion_ratio_is_incomplete_not_a_math_error()
    {
        var line = new RecipeLine("inv-1", QuantityMilliUnits: 4, RecipeLineKind.Yield, YieldUnitsPerBaseUnit: null);
        Assert.Null(CostingRules.BaseQuantityMilliUnits(line));
    }

    [Fact]
    public void A_yield_line_converts_a_piece_count_back_to_raw_weight()
    {
        // 100 milliUnits of yield-declared quantity at a ratio of 4 units per base unit -> 25.
        var line = new RecipeLine("inv-1", QuantityMilliUnits: 100, RecipeLineKind.Yield, YieldUnitsPerBaseUnit: 4);
        Assert.Equal(25, CostingRules.BaseQuantityMilliUnits(line));
    }

    [Fact]
    public void Recipe_cost_splits_food_and_packaging_separately()
    {
        var meat = new InventoryItem("inv-meat", "لحمة", CostSource.Purchase);
        var bag = new InventoryItem("inv-bag", "كيس", CostSource.Purchase);
        var movements = new[]
        {
            new StockMovementFacts("inv-meat", StockMovementType.PurchaseReceipt, Money.FromEgp(200m), Now),
            new StockMovementFacts("inv-bag", StockMovementType.PurchaseReceipt, Money.FromEgp(2m), Now),
        };
        var lines = new[]
        {
            new RecipeLine("inv-meat", 200, RecipeLineKind.Direct),
            new RecipeLine("inv-bag", 1000, RecipeLineKind.Packaging),
        };
        var ctx = EmptyContext(new[] { meat, bag }, movements);

        var result = CostingRules.ComputeRecipeCost(lines, ctx);

        Assert.Equal(Money.FromEgp(40m), result.FoodCost);
        Assert.Equal(Money.FromEgp(2m), result.PackagingCost);
        Assert.Equal(100, result.CompletenessPercent);
    }

    [Fact]
    public void Menu_item_profitability_is_null_for_a_free_or_zero_priced_item()
    {
        var result = CostingRules.MenuItemProfitability(Money.Zero, Money.FromEgp(10m));
        Assert.Null(result.FoodCostPercent);
    }

    [Fact]
    public void Menu_item_profitability_computes_food_cost_and_contribution_percent()
    {
        var result = CostingRules.MenuItemProfitability(Money.FromEgp(100m), Money.FromEgp(30m));
        Assert.Equal(30, result.FoodCostPercent);
        Assert.Equal(Money.FromEgp(70m), result.Contribution);
        Assert.Equal(70, result.ContributionPercent);
    }
}
