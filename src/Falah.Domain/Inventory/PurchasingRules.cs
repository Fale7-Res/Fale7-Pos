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

    /// <summary>WHEN the cost above was set. An ingredient cost that moves on its own is alarming;
    /// the same figure with a date next to it is a purchase somebody made, and can be checked.</summary>
    public static DateTimeOffset? LastPurchaseAt(IEnumerable<StockMovementFacts> stockMovements, string inventoryItemId) =>
        stockMovements
            .Where(m => m.InventoryItemId == inventoryItemId && m.Type == StockMovementType.PurchaseReceipt && m.UnitCost is { IsPositive: true })
            .OrderByDescending(m => m.CreatedAtUtc)
            .Select(m => (DateTimeOffset?)m.CreatedAtUtc)
            .FirstOrDefault();

    /// <summary>
    /// How much a newly received purchase moves an ingredient's cost basis.
    ///
    /// This system values an ingredient at its LAST purchase price, not a weighted average (see
    /// LastPurchaseUnitCost above), so one received line becomes THE cost immediately - and with it
    /// every recipe using that ingredient, the food-cost percentage, and the gross margin the owner
    /// reads. A decimal slipped while keying an invoice therefore rewrites the restaurant's whole
    /// margin picture silently.
    ///
    /// This describes the move so it can be recorded. It deliberately does NOT judge whether a
    /// price is acceptable and does not block anything: prices genuinely do move, pack sizes and
    /// units genuinely do change, and deciding what a reasonable cost is belongs to the restaurant.
    /// The order-of-magnitude flag is a data-entry signal, not a price policy - a tenfold move is
    /// far more often a misplaced decimal than a real one, and is worth an operator's attention
    /// either way.
    /// </summary>
    public readonly record struct UnitCostChange(
        Money? PreviousUnitCost,
        Money NewUnitCost,
        bool IsFirstEverPurchase,
        bool HasChanged,
        double? Factor,
        bool IsOrderOfMagnitude);

    public static UnitCostChange DescribeUnitCostChange(Money? previousUnitCost, Money newUnitCost)
    {
        // Nothing to compare against. A first purchase establishes the basis rather than moving it.
        if (previousUnitCost is not { IsPositive: true } previous)
            return new UnitCostChange(null, newUnitCost, IsFirstEverPurchase: true, HasChanged: false, null, false);

        if (previous.Piastres == newUnitCost.Piastres)
            return new UnitCostChange(previous, newUnitCost, false, HasChanged: false, 1, false);

        var factor = (double)newUnitCost.Piastres / previous.Piastres;
        return new UnitCostChange(previous, newUnitCost, false, HasChanged: true,
            Math.Round(factor, 3), IsOrderOfMagnitude: factor >= 10 || factor <= 0.1);
    }

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

    /// <summary>Ported from purchasePaymentAccountFor in lib/financial-domain.mjs. "credit" (آجل) is
    /// deliberately not a real treasury account - it means no cash moved yet, the whole invoice is
    /// outstanding on the supplier ledger until a later payPurchase settles some or all of it.</summary>
    public static string? PurchasePaymentAccountFor(string paymentMethod) => paymentMethod switch
    {
        "cash" => Financial.TreasuryAccounts.Main,
        "bank" => Financial.TreasuryAccounts.Bank,
        "instapay" => Financial.TreasuryAccounts.InstaPay,
        "credit" => null,
        _ => throw new DomainException("مصدر دفع المشتريات غير مدعوم."),
    };

    public static Money SupplierOutstandingFromLedger(IEnumerable<SupplierLedgerEntry> entries, string supplierId, string? purchaseId) =>
        Money.FromPiastres(entries
            .Where(e => e.SupplierId == supplierId && (purchaseId is null || e.PurchaseId == purchaseId || e.ReferenceId == purchaseId))
            .Sum(e => e.Debit.Piastres - e.Credit.Piastres));
}
