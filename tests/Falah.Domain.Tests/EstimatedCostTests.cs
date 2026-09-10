namespace Falah.Domain.Tests;

using Falah.Domain.Common;
using Falah.Domain.Inventory;
using Xunit;

/// <summary>
/// An estimated cost exists so a restaurant can see an approximate food cost before it has recorded
/// a single supplier invoice. The alternative - the only one available before this - was to
/// fabricate purchase invoices, which would post supplier obligations, move treasury money and add
/// stock that never arrived. An estimate is a note about what something probably costs; an invoice
/// is a claim that goods and money changed hands.
///
/// So the rule that matters is precedence: a real purchase always wins, and the reported source
/// always says which of the two produced a figure.
/// </summary>
public class EstimatedCostTests
{
    private static CostingContext Ctx(params StockMovementFacts[] movements) =>
        new(Array.Empty<InventoryItem>(), Array.Empty<PrepRecipe>(), Array.Empty<PrepRecipeLine>(), movements);

    private static InventoryItem Item(long? estimate) =>
        new("inv-1", "لحم كفتة", CostSource.Purchase, estimate);

    private static StockMovementFacts Purchase(long unitCostPiastres) =>
        new("inv-1", StockMovementType.PurchaseReceipt, Money.FromPiastres(unitCostPiastres), DateTimeOffset.UtcNow);

    [Fact]
    public void With_no_purchase_and_no_estimate_the_cost_stays_unknown()
    {
        var (cost, source) = CostingRules.IngredientUnitCost(Item(null), Ctx());

        Assert.Null(cost);
        Assert.Equal(CostResultSource.Unknown, source);
    }

    [Fact]
    public void An_estimate_is_used_only_while_no_purchase_exists_and_says_so()
    {
        var (cost, source) = CostingRules.IngredientUnitCost(Item(28_000), Ctx());

        Assert.Equal(28_000, cost!.Value.Piastres);
        // The source is the point. A screen that cannot tell an estimate from an invoice would let
        // a guess harden into a fact nobody remembers entering.
        Assert.Equal(CostResultSource.Estimated, source);
    }

    [Fact]
    public void A_real_purchase_overrides_the_estimate_completely()
    {
        var (cost, source) = CostingRules.IngredientUnitCost(Item(28_000), Ctx(Purchase(31_500)));

        Assert.Equal(31_500, cost!.Value.Piastres);
        Assert.Equal(CostResultSource.Purchase, source);
    }

    [Fact]
    public void A_purchase_wins_even_when_it_is_far_cheaper_than_the_estimate()
    {
        // What the restaurant actually paid is a fact; an estimate is a placeholder. The placeholder
        // never gets to argue with the invoice, in either direction.
        var (cost, source) = CostingRules.IngredientUnitCost(Item(28_000), Ctx(Purchase(1_500)));

        Assert.Equal(1_500, cost!.Value.Piastres);
        Assert.Equal(CostResultSource.Purchase, source);
    }

    [Fact]
    public void An_estimate_of_zero_is_not_a_cost()
    {
        // Zero would mean the ingredient is free, which is never what somebody meant to record.
        var (cost, source) = CostingRules.IngredientUnitCost(Item(0), Ctx());

        Assert.Null(cost);
        Assert.Equal(CostResultSource.Unknown, source);
    }

    [Fact]
    public void A_recipe_priced_only_from_estimates_still_reports_complete_coverage()
    {
        // Coverage answers "do we know a cost for every line", not "is every cost an invoice".
        // The estimate/invoice distinction is carried by the source, not by pretending a line is
        // missing - a restaurant working from estimates must still get a food-cost figure.
        var item = Item(28_000);
        var ctx = new CostingContext(
            new[] { item }, Array.Empty<PrepRecipe>(), Array.Empty<PrepRecipeLine>(), Array.Empty<StockMovementFacts>());
        var lines = new[] { new RecipeLine("inv-1", 140, RecipeLineKind.Direct) };

        var result = CostingRules.ComputeRecipeCost(lines, ctx);

        Assert.Equal(100, result.CompletenessPercent);
        Assert.Empty(result.Missing);
        Assert.True(result.FoodCost.IsPositive);
    }
}
