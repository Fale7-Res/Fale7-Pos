namespace Falah.Infrastructure.Orders;

using Falah.Domain.Inventory;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Deducts stock for a checkout's sold items, one StockMovement row per recipe ingredient line,
/// added to the SAME DbContext/transaction the order and its payment are already part of - so
/// stock deduction and the sale it came from can never exist independently of each other. Each
/// row's IdempotencyKey ("usage-{orderId}-{orderItemId}-{inventoryItemId}") carries a unique
/// database constraint, matching TreasuryMovements/OrderRefunds, so a retried checkout that
/// somehow reached this step twice still could not double-deduct.
/// </summary>
public sealed class InventoryDeductionService
{
    private readonly FalahDbContext _db;
    public InventoryDeductionService(FalahDbContext db) => _db = db;

    public async Task DeductForOrderAsync(
        string orderId, IReadOnlyList<MenuPricingService.ResolvedItem> items, string createdBy, DateTimeOffset nowUtc, CancellationToken ct)
    {
        var recipeIds = items.Where(i => i.RecipeId is not null).Select(i => i.RecipeId!).Distinct().ToList();
        if (recipeIds.Count == 0) return;

        var recipes = await _db.Recipes.Include(r => r.Items)
            .Where(r => recipeIds.Contains(r.Id) && r.Active).ToDictionaryAsync(r => r.Id, ct);

        foreach (var resolved in items)
        {
            if (resolved.RecipeId is null || !recipes.TryGetValue(resolved.RecipeId, out var recipe)) continue;

            foreach (var line in recipe.Items)
            {
                var kind = Enum.Parse<RecipeLineKind>(line.Kind);
                var recipeLine = new RecipeLine(line.InventoryItemId, line.QuantityMilliUnits, kind, line.YieldUnitsPerBaseUnit);
                // A 'Yield' line missing its conversion ratio is skipped rather than posting a
                // nonsense quantity - the same rule computeRecipeCost applies for costing.
                var baseQuantity = CostingRules.BaseQuantityMilliUnits(recipeLine);
                if (baseQuantity is null) continue;

                _db.StockMovements.Add(new StockMovementEntity
                {
                    IdempotencyKey = $"usage-{orderId}-{resolved.Item.Id}-{line.InventoryItemId}",
                    InventoryItemId = line.InventoryItemId,
                    Type = "usage",
                    QuantityMilliUnits = baseQuantity.Value * resolved.Item.Quantity,
                    ReferenceType = "order",
                    ReferenceId = orderId,
                    Reason = $"طلب {resolved.Item.ProductName}",
                    CreatedBy = createdBy,
                    CreatedAtUtc = nowUtc,
                });
            }
        }
    }
}
