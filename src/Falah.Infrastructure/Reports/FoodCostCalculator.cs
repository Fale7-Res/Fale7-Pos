namespace Falah.Infrastructure.Reports;

using Falah.Domain.Common;
using Falah.Domain.Inventory;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>The live food-cost estimate: today's recipes x the latest purchase prices. One place, so the
/// food-cost report and the business-day close (which freezes these figures onto the day's orders,
/// INV-0011 / FIN-0044) can never price the same line two different ways.</summary>
internal sealed class FoodCostCalculator
{
    private readonly Dictionary<string, string?> _productRecipeIds;
    private readonly Dictionary<string, string?> _variantRecipeIds;
    private readonly Dictionary<string, Persistence.Entities.RecipeEntity> _recipes;
    private readonly List<OrderPackagingRule> _packagingRules;
    private readonly CostingContext _costing;

    private FoodCostCalculator(Dictionary<string, string?> productRecipeIds, Dictionary<string, string?> variantRecipeIds,
        Dictionary<string, Persistence.Entities.RecipeEntity> recipes, List<OrderPackagingRule> packagingRules, CostingContext costing)
    {
        _productRecipeIds = productRecipeIds;
        _variantRecipeIds = variantRecipeIds;
        _recipes = recipes;
        _packagingRules = packagingRules;
        _costing = costing;
    }

    public static async Task<FoodCostCalculator> LoadAsync(FalahDbContext db, CancellationToken ct)
    {
        var packagingRules = await db.OrderPackagingRules.AsNoTracking()
            .Select(r => new OrderPackagingRule(r.OrderType, r.InventoryItemId, r.QuantityMilliUnits, r.Active))
            .ToListAsync(ct);
        var productRecipeIds = await db.Products.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.RecipeId, ct);
        var variantRecipeIds = await db.ProductVariants.AsNoTracking().ToDictionaryAsync(v => v.Id, v => v.RecipeId, ct);
        var recipes = await db.Recipes.AsNoTracking().Include(r => r.Items).ToDictionaryAsync(r => r.Id, ct);

        var items = await db.InventoryItems.AsNoTracking()
            .Select(i => new InventoryItem(i.Id, i.Name, Enum.Parse<CostSource>(i.CostSource), i.EstimatedUnitCostPiastres)).ToListAsync(ct);
        var preps = await db.PrepRecipes.AsNoTracking().Select(p => new PrepRecipe(p.Id, p.OutputInventoryItemId, p.OutputQuantityMilliUnits, p.Active)).ToListAsync(ct);
        var prepLines = await db.PrepRecipeLines.AsNoTracking().Select(l => new PrepRecipeLine(l.PrepRecipeId, l.InventoryItemId, l.QuantityMilliUnits)).ToListAsync(ct);
        var purchaseMovements = await db.StockMovements.AsNoTracking()
            .Where(m => m.Type == nameof(StockMovementType.PurchaseReceipt) && m.UnitCostPiastres != null)
            .Select(m => new { m.InventoryItemId, m.UnitCostPiastres, m.CreatedAtUtc }).ToListAsync(ct);
        var movementFacts = purchaseMovements.Select(m => new StockMovementFacts(m.InventoryItemId, StockMovementType.PurchaseReceipt, Money.FromPiastres(m.UnitCostPiastres!.Value), m.CreatedAtUtc)).ToList();

        return new FoodCostCalculator(productRecipeIds, variantRecipeIds, recipes, packagingRules,
            new CostingContext(items, preps, prepLines, movementFacts));
    }

    /// <summary>One unit's whole food cost, or null when it is not fully known (no recipe, or an
    /// unpriced ingredient) - an unknown cost is never a zero.</summary>
    public long? UnitCost(string productId, string? variantId)
    {
        var recipeId = (variantId is not null && _variantRecipeIds.TryGetValue(variantId, out var vr) && vr is not null)
            ? vr : _productRecipeIds.GetValueOrDefault(productId);
        if (recipeId is null || !_recipes.TryGetValue(recipeId, out var recipe)) return null;

        var recipeLines = recipe.Items.Select(i => new RecipeLine(i.InventoryItemId, i.QuantityMilliUnits, Enum.Parse<RecipeLineKind>(i.Kind), i.YieldUnitsPerBaseUnit)).ToList();
        var cost = CostingRules.ComputeRecipeCost(recipeLines, _costing);
        // Not "is anything missing" - a recipe with no lines is missing nothing and knows nothing.
        return cost.IsComplete ? cost.KnownCost.Piastres : null;
    }

    /// <summary>The order-level packaging (the delivery bag) one order of this type consumes.</summary>
    public (long Cost, bool Known, string? Name) Packaging(string orderType)
    {
        var (cost, known, name) = CostingRules.OrderPackagingCost(orderType, _packagingRules, _costing);
        return (cost.Piastres, known, name);
    }
}
