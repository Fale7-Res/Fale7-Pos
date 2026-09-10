namespace Falah.Domain.Inventory;

using Falah.Domain.Common;

/// <summary>Where a cost figure came from, which matters as much as the figure. "Estimated" is a
/// number the owner typed because no invoice exists yet; it is never presented as though a real
/// purchase produced it, and a real purchase always wins over it.</summary>
public enum CostResultSource { Purchase, Prep, Estimated, Unknown }

/// <summary>
/// Pure recipe/costing engine ported from lib/costing-domain.mjs. Core principle enforced
/// everywhere here: UNKNOWN COST IS NOT ZERO. An ingredient with no purchase history, or a prep
/// batch with any missing input, makes the cost it feeds into UNDEFINED (never a silent zero) -
/// callers get completeness/missing information instead of a fake precise profit number.
/// </summary>
public static class CostingRules
{
    public readonly record struct IngredientCostResult(Money? UnitCost, CostResultSource Source);

    public static IngredientCostResult IngredientUnitCost(InventoryItem? item, CostingContext ctx)
    {
        if (item is null) return new IngredientCostResult(null, CostResultSource.Unknown);

        if (item.CostSource == CostSource.Prep)
        {
            var prep = ctx.PrepRecipes.FirstOrDefault(p => p.OutputInventoryItemId == item.Id && p.Active);
            if (prep is null) return new IngredientCostResult(null, CostResultSource.Unknown);
            var lines = ctx.PrepRecipeLines.Where(l => l.PrepRecipeId == prep.Id).ToList();
            var result = ComputePrepRecipeCost(prep, lines, ctx);
            return new IngredientCostResult(result.UnitCost, CostResultSource.Prep);
        }

        // A real purchase always wins. What the restaurant actually paid is a fact; what somebody
        // estimated is a placeholder, and the moment the fact exists the placeholder stops being used.
        var cost = PurchasingRules.LastPurchaseUnitCost(ctx.StockMovements, item.Id);
        if (cost is not null) return new IngredientCostResult(cost, CostResultSource.Purchase);

        return item.EstimatedUnitCostPiastres is { } estimated and > 0
            ? new IngredientCostResult(Money.FromPiastres(estimated), CostResultSource.Estimated)
            : new IngredientCostResult(null, CostResultSource.Unknown);
    }

    public readonly record struct PrepRecipeCostResult(Money? UnitCost, Money KnownCost, double Completeness, IReadOnlyList<string> Missing, int LineCount, int KnownLineCount);

    /// <summary>Cost per whole output unit of a prep/batch recipe (tahini, toum, a house sauce...).
    /// Deliberately returns UnitCost=null unless EVERY input line resolves to a known cost - a
    /// batch that is 90% known is still an unknown-cost item, never a quietly-underpriced one.</summary>
    public static PrepRecipeCostResult ComputePrepRecipeCost(PrepRecipe? prep, IReadOnlyList<PrepRecipeLine> lines, CostingContext ctx)
    {
        if (prep is null || prep.OutputQuantityMilliUnits <= 0)
            return new PrepRecipeCostResult(null, Money.Zero, 0, Array.Empty<string>(), lines.Count, 0);

        var knownCost = Money.Zero;
        var knownLines = 0;
        var missing = new List<string>();

        foreach (var line in lines)
        {
            var item = ctx.InventoryItems.FirstOrDefault(i => i.Id == line.InventoryItemId);
            var (unitCost, _) = IngredientUnitCost(item, ctx);
            if (unitCost is null) { missing.Add(item?.Name ?? line.InventoryItemId); continue; }
            knownCost += PurchasingRules.PurchaseLineTotal(line.QuantityMilliUnits, unitCost.Value);
            knownLines++;
        }

        var completeness = lines.Count > 0 ? (double)knownLines / lines.Count : 0;
        Money? unitCostResult = completeness == 1 && lines.Count > 0
            ? Money.FromPiastres((long)Math.Round(knownCost.Piastres * 1000m / prep.OutputQuantityMilliUnits))
            : null;

        return new PrepRecipeCostResult(unitCostResult, knownCost, completeness, missing, lines.Count, knownLines);
    }

    /// <summary>Converts a recipe line's declared quantity into the ingredient's OWN base unit
    /// (milliUnits), resolving 'Yield' lines (portions/pieces per kg) back to a raw weight/volume.
    /// Returns null when a yield line is missing its conversion ratio - a genuinely incomplete
    /// line, not a math error. Checkout's stock-deduction path must use this SAME conversion, or
    /// stock tracking and cost reporting would silently drift apart.</summary>
    public static long? BaseQuantityMilliUnits(RecipeLine line)
    {
        if (line.Kind != RecipeLineKind.Yield) return line.QuantityMilliUnits;
        if (line.YieldUnitsPerBaseUnit is not { } ratio || ratio <= 0) return null;
        return (long)Math.Round((double)line.QuantityMilliUnits / ratio);
    }

    public readonly record struct RecipeCostResult(Money KnownCost, Money FoodCost, Money PackagingCost, int CompletenessPercent, IReadOnlyList<string> Missing, int LineCount, int KnownLineCount);

    public static RecipeCostResult ComputeRecipeCost(IReadOnlyList<RecipeLine> lines, CostingContext ctx)
    {
        var foodCost = Money.Zero;
        var packagingCost = Money.Zero;
        var knownLines = 0;
        var missing = new List<string>();

        foreach (var line in lines)
        {
            var item = ctx.InventoryItems.FirstOrDefault(i => i.Id == line.InventoryItemId);
            var label = item?.Name ?? line.InventoryItemId;
            var qty = BaseQuantityMilliUnits(line);
            if (qty is null) { missing.Add(label); continue; }

            var (unitCost, _) = IngredientUnitCost(item, ctx);
            if (unitCost is null) { missing.Add(label); continue; }

            var lineCost = PurchasingRules.PurchaseLineTotal(qty.Value, unitCost.Value);
            if (line.Kind == RecipeLineKind.Packaging) packagingCost += lineCost; else foodCost += lineCost;
            knownLines++;
        }

        var completeness = lines.Count > 0 ? (double)knownLines / lines.Count : 0;
        return new RecipeCostResult(foodCost + packagingCost, foodCost, packagingCost, (int)Math.Round(completeness * 100), missing, lines.Count, knownLines);
    }

    /// <summary>Order-level packaging (e.g. one delivery bag per order), applied once per
    /// qualifying order type - never duplicated per item.</summary>
    public static (Money Cost, bool Known, string? Name) OrderPackagingCost(string orderType, IEnumerable<OrderPackagingRule> rules, CostingContext ctx)
    {
        var rule = rules.FirstOrDefault(r => r.OrderType == orderType && r.Active);
        if (rule is null) return (Money.Zero, true, null);

        var item = ctx.InventoryItems.FirstOrDefault(i => i.Id == rule.InventoryItemId);
        var (unitCost, _) = IngredientUnitCost(item, ctx);
        if (unitCost is null) return (Money.Zero, false, item?.Name ?? rule.InventoryItemId);
        return (PurchasingRules.PurchaseLineTotal(rule.QuantityMilliUnits, unitCost.Value), true, item?.Name);
    }

    public readonly record struct ProfitabilityResult(double? FoodCostPercent, Money? Contribution, double? ContributionPercent);

    public static ProfitabilityResult MenuItemProfitability(Money sellingPrice, Money knownCost)
    {
        if (!sellingPrice.IsPositive) return new ProfitabilityResult(null, null, null);
        var contribution = sellingPrice - knownCost;
        var foodCostPercent = Math.Round((double)knownCost.Piastres / sellingPrice.Piastres * 100, 2);
        var contributionPercent = Math.Round((double)contribution.Piastres / sellingPrice.Piastres * 100, 2);
        return new ProfitabilityResult(foodCostPercent, contribution, contributionPercent);
    }
}
