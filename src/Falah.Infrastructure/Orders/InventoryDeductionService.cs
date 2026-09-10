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
        string orderId, string orderType, IReadOnlyList<MenuPricingService.ResolvedItem> items,
        string createdBy, DateTimeOffset nowUtc, CancellationToken ct)
    {
        await DeductOrderPackagingAsync(orderId, orderType, createdBy, nowUtc, ct);

        var recipeIds = items.Where(i => i.RecipeId is not null).Select(i => i.RecipeId!).Distinct().ToList();
        if (recipeIds.Count == 0) return;

        var recipes = await _db.Recipes.Include(r => r.Items)
            .Where(r => recipeIds.Contains(r.Id) && r.Active).ToDictionaryAsync(r => r.Id, ct);

        foreach (var resolved in items)
        {
            if (resolved.RecipeId is null || !recipes.TryGetValue(resolved.RecipeId, out var recipe)) continue;

            var lineNumber = 0;
            foreach (var line in recipe.Items)
            {
                lineNumber++;
                var kind = Enum.Parse<RecipeLineKind>(line.Kind);
                var recipeLine = new RecipeLine(line.InventoryItemId, line.QuantityMilliUnits, kind, line.YieldUnitsPerBaseUnit);
                // A 'Yield' line missing its conversion ratio is skipped rather than posting a
                // nonsense quantity - the same rule computeRecipeCost applies for costing.
                var baseQuantity = CostingRules.BaseQuantityMilliUnits(recipeLine);
                if (baseQuantity is null) continue;

                _db.StockMovements.Add(new StockMovementEntity
                {
                    // The line's position is part of the key. Without it, a recipe that names the
                    // same ingredient on two lines - 200g of tomato as a filling and 50g as a
                    // garnish - produced two movements with one key, and the unique constraint
                    // turned a perfectly ordinary recipe into a failed sale at the till.
                    IdempotencyKey = $"usage-{orderId}-{resolved.Item.Id}-{line.InventoryItemId}-{lineNumber}",
                    InventoryItemId = line.InventoryItemId,
                    Type = nameof(StockMovementType.Usage),
                    Direction = nameof(StockDirection.Out),
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

    /// <summary>The delivery bag, applied once for the whole order rather than once per item.
    ///
    /// This rule could be configured and listed and nothing ever consumed it: the bag never left
    /// stock, and its cost never reached the order it was carried in. Keyed by the order alone, so
    /// a replayed checkout cannot take a second bag off the shelf.
    ///
    /// A rule with no active row for this order type means the restaurant has not said this order
    /// type consumes packaging - which is silence, not a quantity of zero, and nothing is posted.</summary>
    private async Task DeductOrderPackagingAsync(
        string orderId, string orderType, string createdBy, DateTimeOffset nowUtc, CancellationToken ct)
    {
        var rule = await _db.OrderPackagingRules.AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrderType == orderType && r.Active, ct);
        if (rule is null || rule.QuantityMilliUnits <= 0) return;

        _db.StockMovements.Add(new StockMovementEntity
        {
            IdempotencyKey = $"packaging-{orderId}",
            InventoryItemId = rule.InventoryItemId,
            Type = nameof(StockMovementType.Usage),
            Direction = nameof(StockDirection.Out),
            QuantityMilliUnits = rule.QuantityMilliUnits,
            ReferenceType = "order",
            ReferenceId = orderId,
            Reason = "تغليف الطلب",
            CreatedBy = createdBy,
            CreatedAtUtc = nowUtc,
        });
    }
}
