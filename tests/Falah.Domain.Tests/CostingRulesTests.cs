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
        var result = CostingRules.MenuItemProfitability(Money.Zero, Money.FromEgp(10m), costIsComplete: true);
        Assert.Null(result.FoodCostPercent);
    }

    [Fact]
    public void Menu_item_profitability_computes_food_cost_and_contribution_percent()
    {
        var result = CostingRules.MenuItemProfitability(Money.FromEgp(100m), Money.FromEgp(30m), costIsComplete: true);
        Assert.Equal(30, result.FoodCostPercent);
        Assert.Equal(Money.FromEgp(70m), result.Contribution);
        Assert.Equal(70, result.ContributionPercent);
    }

    /// <summary>These two were a live crash, not a code-reading worry: the costing endpoint was
    /// asked for the cost of a batch that listed its own output, the resolver called itself until
    /// the stack ran out, and the server process ended. Every station loses its connection when
    /// that happens - mid-sale.</summary>
    [Fact]
    public void A_batch_that_lists_its_own_output_costs_unknown_instead_of_ending_the_process()
    {
        var tahini = new InventoryItem("inv-tahini", "طحينة", CostSource.Prep);
        var sesame = new InventoryItem("inv-sesame", "سمسم", CostSource.Purchase);

        var prep = new PrepRecipe("prep-1", "inv-tahini", OutputQuantityMilliUnits: 1000, Active: true);
        var lines = new[]
        {
            new PrepRecipeLine("prep-1", "inv-sesame", 800),
            new PrepRecipeLine("prep-1", "inv-tahini", 200), // itself
        };
        var movements = new[] { new StockMovementFacts("inv-sesame", StockMovementType.PurchaseReceipt, Money.FromEgp(1m), Now) };
        var ctx = EmptyContext(new[] { tahini, sesame }, movements, new[] { prep }, lines);

        var result = CostingRules.ComputePrepRecipeCost(prep, lines, ctx);

        Assert.Null(result.UnitCost);
        // The sesame line is still priced - only the loop is unknown - and the reason is named, so
        // nobody goes hunting for a purchase invoice that exists.
        Assert.Equal(Money.FromEgp(0.8m), result.KnownCost);
        Assert.Contains(result.Missing, m => m.Contains("طحينة") && m.Contains("دائرية"));
    }

    [Fact]
    public void A_loop_that_runs_through_a_second_batch_is_caught_the_same_way()
    {
        // Sauce is made from paste, paste is made from sauce. Neither one names itself.
        var sauce = new InventoryItem("inv-sauce", "صوص", CostSource.Prep);
        var paste = new InventoryItem("inv-paste", "عجينة", CostSource.Prep);

        var saucePrep = new PrepRecipe("prep-sauce", "inv-sauce", 1000, true);
        var pastePrep = new PrepRecipe("prep-paste", "inv-paste", 1000, true);
        var lines = new[]
        {
            new PrepRecipeLine("prep-sauce", "inv-paste", 500),
            new PrepRecipeLine("prep-paste", "inv-sauce", 500),
        };
        var ctx = EmptyContext(new[] { sauce, paste }, null, new[] { saucePrep, pastePrep }, lines);

        var result = CostingRules.ComputePrepRecipeCost(saucePrep, new[] { lines[0] }, ctx);

        Assert.Null(result.UnitCost);
        Assert.Equal(0, result.KnownLineCount);
    }

    [Fact]
    public void A_dish_using_a_circular_batch_is_uncostable_rather_than_cheap()
    {
        // The dangerous shape: the loop silently contributes nothing and the dish looks like pure
        // profit. It must come back incomplete instead.
        var tahini = new InventoryItem("inv-tahini", "طحينة", CostSource.Prep);
        var prep = new PrepRecipe("prep-1", "inv-tahini", 1000, true);
        var prepLines = new[] { new PrepRecipeLine("prep-1", "inv-tahini", 1000) };
        var ctx = EmptyContext(new[] { tahini }, null, new[] { prep }, prepLines);

        var result = CostingRules.ComputeRecipeCost(
            new[] { new RecipeLine("inv-tahini", 50, RecipeLineKind.Direct) }, ctx);

        Assert.Equal(0, result.CompletenessPercent);
        Assert.Contains("طحينة", result.Missing);
    }
}
