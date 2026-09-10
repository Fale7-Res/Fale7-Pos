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
        => IngredientUnitCost(item, ctx, resolving: null);

    /// <summary><paramref name="resolving"/> holds the prep outputs whose cost is already being
    /// worked out further up the call stack. A prep item that appears in it again is a circular
    /// recipe - tahini made from tahini, directly or around a chain - and following it recurses
    /// until the process dies. Its cost is not zero: it is undefined until somebody breaks the
    /// loop, which is what this returns.</summary>
    private static IngredientCostResult IngredientUnitCost(InventoryItem? item, CostingContext ctx, HashSet<string>? resolving)
    {
        if (item is null) return new IngredientCostResult(null, CostResultSource.Unknown);

        if (item.CostSource == CostSource.Prep)
        {
            if (resolving is not null && resolving.Contains(item.Id))
                return new IngredientCostResult(null, CostResultSource.Unknown);

            var prep = ctx.PrepRecipes.FirstOrDefault(p => p.OutputInventoryItemId == item.Id && p.Active);
            if (prep is null) return new IngredientCostResult(null, CostResultSource.Unknown);
            var lines = ctx.PrepRecipeLines.Where(l => l.PrepRecipeId == prep.Id).ToList();

            resolving ??= new HashSet<string>(StringComparer.Ordinal);
            resolving.Add(item.Id);
            var result = ComputePrepRecipeCost(prep, lines, ctx, resolving);
            resolving.Remove(item.Id);
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

    /// <summary>Whether this ingredient's batch consumes its own output - directly, or around a
    /// chain of other batches. Its cost is undefined until somebody breaks the loop, and saying so
    /// is a very different message from "no purchase invoice yet": one is a data mistake to fix in
    /// the recipe, the other is an invoice to go and enter.</summary>
    public static bool IsCircularPrep(InventoryItem? item, CostingContext ctx)
    {
        if (item is null || item.CostSource != CostSource.Prep) return false;

        var seen = new HashSet<string>(StringComparer.Ordinal) { item.Id };
        var pending = new Queue<string>();
        pending.Enqueue(item.Id);

        while (pending.Count > 0)
        {
            var outputId = pending.Dequeue();
            var prep = ctx.PrepRecipes.FirstOrDefault(p => p.OutputInventoryItemId == outputId && p.Active);
            if (prep is null) continue;

            foreach (var line in ctx.PrepRecipeLines.Where(l => l.PrepRecipeId == prep.Id))
            {
                if (line.InventoryItemId == item.Id) return true;
                if (seen.Add(line.InventoryItemId)) pending.Enqueue(line.InventoryItemId);
            }
        }
        return false;
    }

    public readonly record struct PrepRecipeCostResult(Money? UnitCost, Money KnownCost, double Completeness, IReadOnlyList<string> Missing, int LineCount, int KnownLineCount);

    /// <summary>Cost per whole output unit of a prep/batch recipe (tahini, toum, a house sauce...).
    /// Deliberately returns UnitCost=null unless EVERY input line resolves to a known cost - a
    /// batch that is 90% known is still an unknown-cost item, never a quietly-underpriced one.</summary>
    /// <summary>Seeded with this batch's own output: pricing a batch IS resolving that output, so a
    /// line naming it is a self-reference even at the top of the stack - and gets told so by name
    /// rather than appearing as an ordinary ingredient nobody has bought yet.</summary>
    public static PrepRecipeCostResult ComputePrepRecipeCost(PrepRecipe? prep, IReadOnlyList<PrepRecipeLine> lines, CostingContext ctx)
        => ComputePrepRecipeCost(prep, lines, ctx,
            prep is null ? null : new HashSet<string>(StringComparer.Ordinal) { prep.OutputInventoryItemId });

    private static PrepRecipeCostResult ComputePrepRecipeCost(PrepRecipe? prep, IReadOnlyList<PrepRecipeLine> lines, CostingContext ctx, HashSet<string>? resolving)
    {
        if (prep is null || prep.OutputQuantityMilliUnits <= 0)
            return new PrepRecipeCostResult(null, Money.Zero, 0, Array.Empty<string>(), lines.Count, 0);

        var knownCost = Money.Zero;
        var knownLines = 0;
        var missing = new List<string>();

        foreach (var line in lines)
        {
            var item = ctx.InventoryItems.FirstOrDefault(i => i.Id == line.InventoryItemId);
            var label = item?.Name ?? line.InventoryItemId;

            // Say WHY this line has no cost. "Missing: tahini" on a batch whose every ingredient
            // has an invoice would send the cost controller looking for a purchase that is
            // already there; the problem is the loop, and only this line knows it is in one.
            if (resolving is not null && item is not null && resolving.Contains(item.Id))
            { missing.Add($"{label} (وصفة تجهيز دائرية)"); continue; }

            var (unitCost, _) = IngredientUnitCost(item, ctx, resolving);
            if (unitCost is null) { missing.Add(label); continue; }
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

    public readonly record struct RecipeCostResult(Money KnownCost, Money FoodCost, Money PackagingCost, int CompletenessPercent, IReadOnlyList<string> Missing, int LineCount, int KnownLineCount)
    {
        /// <summary>Whether KnownCost is the WHOLE cost of this dish. Callers used to decide that
        /// by asking whether anything was missing, which is true of a recipe with no lines at all -
        /// so a dish attached to an empty recipe was counted as costing a known zero, and a menu of
        /// them read as a hundred percent margin. A recipe with no ingredients is not free; nobody
        /// has said yet what it is made of.</summary>
        public bool IsComplete => LineCount > 0 && KnownLineCount == LineCount;
    }

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

    /// <param name="costIsComplete">False when some - or every - ingredient of the dish has no
    /// price yet. The margin is then unanswerable, not excellent: subtracting a partial cost from
    /// the full price produces a contribution the restaurant is not making.</param>
    public static ProfitabilityResult MenuItemProfitability(Money sellingPrice, Money knownCost, bool costIsComplete)
    {
        if (!sellingPrice.IsPositive || !costIsComplete) return new ProfitabilityResult(null, null, null);
        var contribution = sellingPrice - knownCost;
        var foodCostPercent = Math.Round((double)knownCost.Piastres / sellingPrice.Piastres * 100, 2);
        var contributionPercent = Math.Round((double)contribution.Piastres / sellingPrice.Piastres * 100, 2);
        return new ProfitabilityResult(foodCostPercent, contribution, contributionPercent);
    }
}
