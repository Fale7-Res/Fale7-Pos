namespace Falah.Infrastructure.Orders;

using Falah.Domain.Common;
using Falah.Domain.Inventory;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Accounts for the ingredients of an order voided after its checkout took them out of stock
/// (InventoryDeductionService), the way the operator says the food went.
///
/// Every Usage movement the checkout posted for the order is given back by a UsageReversal (In), so
/// the order no longer counts as consumption by a sale. If the food was prepared, the same quantities
/// are then recorded as Waste (Out) valued at the ingredient's last purchase cost - the stock stays
/// where the cooking left it and the loss is visible as a loss. If it was not prepared, the reversal
/// alone puts the ingredients back. Keys derive from the original usage keys, so applying it twice
/// posts nothing the second time. Adds to the context only; the caller's transaction saves it.
/// </summary>
internal static class OrderStockDisposition
{
    public static async Task ApplyAsync(
        FalahDbContext db, string orderId, StockDisposition disposition, string reason, string createdBy,
        DateTimeOffset nowUtc, CancellationToken ct)
    {
        await Falah.Infrastructure.Inventory.InventoryWriteLock.AcquireAsync(db, ct);
        var usages = await db.StockMovements.AsNoTracking()
            .Where(m => m.ReferenceType == "order" && m.ReferenceId == orderId
                && m.Type == nameof(StockMovementType.Usage) && m.Direction == nameof(StockDirection.Out))
            .ToListAsync(ct);
        if (usages.Count == 0) return;

        var reversalKeys = usages.Select(u => $"usage-reversal-{u.IdempotencyKey}").ToList();
        var alreadyReversed = (await db.StockMovements.AsNoTracking()
            .Where(m => reversalKeys.Contains(m.IdempotencyKey)).Select(m => m.IdempotencyKey).ToListAsync(ct)).ToHashSet();

        var unitCosts = disposition == StockDisposition.Prepared
            ? await LastPurchaseUnitCostsAsync(db, usages.Select(u => u.InventoryItemId).Distinct().ToList(), ct)
            : new Dictionary<string, Money?>();
        var operationId = $"op-{Guid.NewGuid():N}";

        foreach (var usage in usages)
        {
            var reversalKey = $"usage-reversal-{usage.IdempotencyKey}";
            if (alreadyReversed.Contains(reversalKey)) continue;

            db.StockMovements.Add(new StockMovementEntity
            {
                IdempotencyKey = reversalKey, InventoryItemId = usage.InventoryItemId,
                Type = nameof(StockMovementType.UsageReversal), Direction = nameof(StockDirection.In),
                QuantityMilliUnits = usage.QuantityMilliUnits, ReferenceType = "order", ReferenceId = orderId,
                Reason = $"إلغاء طلب - {reason}", CreatedBy = createdBy, OperationId = operationId, CreatedAtUtc = nowUtc,
            });

            if (disposition != StockDisposition.Prepared) continue;
            var unitCost = unitCosts.GetValueOrDefault(usage.InventoryItemId);
            var lossValue = PurchasingRules.ValueStockLoss(usage.QuantityMilliUnits, unitCost);
            db.StockMovements.Add(new StockMovementEntity
            {
                IdempotencyKey = $"order-waste-{usage.IdempotencyKey}", InventoryItemId = usage.InventoryItemId,
                Type = nameof(StockMovementType.Waste), Direction = nameof(StockDirection.Out),
                QuantityMilliUnits = usage.QuantityMilliUnits,
                UnitCostPiastres = unitCost?.Piastres, LossValuePiastres = lossValue?.Piastres,
                ReferenceType = "order", ReferenceId = orderId,
                Reason = $"طلب ملغي بعد التحضير - {reason}", CreatedBy = createdBy, OperationId = operationId, CreatedAtUtc = nowUtc,
            });
        }
    }

    /// <summary>The same valuation RecordWasteHandler uses for a logged loss (PurchasingRules.LossUnitCost): each
    /// item's last purchase cost, else its estimated cost, unknown (null) when it has neither.</summary>
    private static async Task<Dictionary<string, Money?>> LastPurchaseUnitCostsAsync(
        FalahDbContext db, IReadOnlyList<string> itemIds, CancellationToken ct)
    {
        var receipts = await db.StockMovements.AsNoTracking()
            .Where(m => itemIds.Contains(m.InventoryItemId) && m.Type == nameof(StockMovementType.PurchaseReceipt) && m.UnitCostPiastres != null)
            .Select(m => new { m.InventoryItemId, m.UnitCostPiastres, m.CreatedAtUtc })
            .ToListAsync(ct);
        var facts = receipts.Select(m => new StockMovementFacts(m.InventoryItemId, StockMovementType.PurchaseReceipt,
            Money.FromPiastres(m.UnitCostPiastres!.Value), m.CreatedAtUtc)).ToList();
        var estimates = await db.InventoryItems.AsNoTracking().Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.EstimatedUnitCostPiastres, ct);
        return itemIds.ToDictionary(id => id,
            id => PurchasingRules.LossUnitCost(PurchasingRules.LastPurchaseUnitCost(facts, id), estimates.GetValueOrDefault(id)));
    }
}
