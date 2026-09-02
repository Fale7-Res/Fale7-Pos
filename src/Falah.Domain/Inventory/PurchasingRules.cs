namespace Falah.Domain.Inventory;

using Falah.Domain.Common;

public sealed record SupplierLedgerEntry(string SupplierId, string? PurchaseId, string? ReferenceId, Money Debit, Money Credit);

/// <summary>Ported from the purchasing/costing helpers in lib/financial-domain.mjs.</summary>
public static class PurchasingRules
{
    public static Money PurchaseLineTotal(long quantityMilliUnits, Money unitCost) =>
        Money.FromPiastres((long)Math.Round(quantityMilliUnits * (decimal)unitCost.Piastres / 1000m));

    /// <summary>Owner-approved loss valuation source: the LAST ACTUAL PURCHASE unit cost (not a
    /// weighted average). Returns null when the item was never purchased, so the caller can record
    /// the loss without inventing a price.</summary>
    public static Money? LastPurchaseUnitCost(IEnumerable<StockMovementFacts> stockMovements, string inventoryItemId) =>
        stockMovements
            .Where(m => m.InventoryItemId == inventoryItemId && m.Type == StockMovementType.PurchaseReceipt && m.UnitCost is { IsPositive: true })
            .OrderByDescending(m => m.CreatedAtUtc)
            .Select(m => m.UnitCost)
            .FirstOrDefault();

    /// <summary>The value is computed ONCE, at the moment the loss is posted - it must never be
    /// recomputed later from current prices, so a purchase next week cannot restate last week's
    /// loss. Callers persist the returned value alongside the unit cost for exactly that
    /// reason.</summary>
    public static Money? ValueStockLoss(long quantityMilliUnits, Money? unitCost) =>
        unitCost is { IsPositive: true } cost ? PurchaseLineTotal(quantityMilliUnits, cost) : null;

    public static void AssertPurchasePayment(Money paid, Money total)
    {
        if (paid.IsNegative) throw new DomainException("المبلغ المدفوع غير صالح.");
        if (paid > total) throw new DomainException("المبلغ المدفوع لا يمكن أن يتجاوز إجمالي الفاتورة.");
    }

    public static Money SupplierOutstandingFromLedger(IEnumerable<SupplierLedgerEntry> entries, string supplierId, string? purchaseId) =>
        Money.FromPiastres(entries
            .Where(e => e.SupplierId == supplierId && (purchaseId is null || e.PurchaseId == purchaseId || e.ReferenceId == purchaseId))
            .Sum(e => e.Debit.Piastres - e.Credit.Piastres));
}
