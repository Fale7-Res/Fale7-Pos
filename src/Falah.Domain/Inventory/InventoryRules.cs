namespace Falah.Domain.Inventory;

using Falah.Domain.Common;

/// <summary>
/// Ported from the inventory-adjustment commands in lib/enterprise-store.tsx
/// (recordInventoryCountAdjustment, recordInventoryWaste, updateInventoryItem's unit-lock) and the
/// reporting helpers in lib/costing-domain.mjs (actualMovementTotals, unexplainedVariance).
/// Deliberately does NOT re-derive or second-guess a physical count: the direction/quantity of a
/// count adjustment is an observed fact only a human counting the shelf can know, so - exactly like
/// the reference app - this validates shape (reason present, quantity positive) but never recomputes
/// or overrides what was counted. Loss quantities are likewise never checked against the current
/// balance: real stock can legitimately lag the system, and blocking on that would hide the very
/// discrepancy this command exists to record.
/// </summary>
public static class InventoryRules
{
    private static readonly IReadOnlySet<StockMovementType> LossTypes =
        new HashSet<StockMovementType> { StockMovementType.Waste, StockMovementType.Spoilage, StockMovementType.Breakage };

    /// <summary>Ported from LARGE_WASTE_MILLI_UNITS in features/inventory/InventoryScreen.tsx (10
    /// base units) - a waste/spoilage/breakage posting at or above this quantity is audited as
    /// 'critical' rather than 'warning'. A real gap found during the legacy-parity audit:
    /// RecordWasteHandler always logged 'warning' regardless of amount.</summary>
    public const long LargeWasteMilliUnitsThreshold = 10_000;

    public static void AssertCountAdjustmentInput(long quantityMilliUnits, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("سبب فرق الجرد مطلوب.");
        if (quantityMilliUnits <= 0) throw new DomainException("كمية فرق الجرد غير صالحة.");
    }

    public static void AssertWasteInput(long quantityMilliUnits, string reason, StockMovementType lossType)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("سبب الهالك مطلوب.");
        if (quantityMilliUnits <= 0) throw new DomainException("كمية الهالك غير صالحة.");
        if (!LossTypes.Contains(lossType)) throw new DomainException("نوع الفقد غير صالح.");
    }

    /// <summary>The unit is not just a label - every stock movement for an item stores its quantity
    /// as a bare milliUnit count of "whatever the item's unit was when it was entered," with no
    /// per-movement unit snapshot. Changing it after movements already exist would silently
    /// reinterpret every past quantity under the new unit.</summary>
    public static void AssertUnitChangeAllowed(bool hasExistingMovements, bool unitChanged)
    {
        if (hasExistingMovements && unitChanged)
            throw new DomainException("لا يمكن تغيير وحدة خامة لها حركات مخزون مسجلة بالفعل.");
    }

    public static long StockBalanceMilliUnits(IEnumerable<StockMovementFacts> movements, string inventoryItemId) =>
        movements.Where(m => m.InventoryItemId == inventoryItemId)
            .Sum(m => m.Direction == StockDirection.In ? m.QuantityMilliUnits : -m.QuantityMilliUnits);

    public readonly record struct MovementTotals(
        long PurchasedMilliUnits, long UsageMilliUnits, long WasteMilliUnits, long CountAdjustmentNetMilliUnits);

    /// <summary>Four distinct buckets over one item's movement history within an optional date
    /// range - never merged, because each answers a different operational question (how much came
    /// in, how much sold, how much was lost, how much a physical count already corrected).</summary>
    public static MovementTotals ActualMovementTotals(
        IEnumerable<StockMovementFacts> movements, string inventoryItemId, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        var rows = movements.Where(m => m.InventoryItemId == inventoryItemId
            && (from is null || m.CreatedAtUtc >= from) && (to is null || m.CreatedAtUtc <= to)).ToList();

        var purchased = rows.Where(m => m.Type is StockMovementType.PurchaseReceipt or StockMovementType.ManualReceipt).Sum(m => m.QuantityMilliUnits);
        var usage = rows.Where(m => m.Type == StockMovementType.Usage).Sum(m => m.QuantityMilliUnits);
        var waste = rows.Where(m => LossTypes.Contains(m.Type)).Sum(m => m.QuantityMilliUnits);
        var countAdjustmentNet = rows.Where(m => m.Type == StockMovementType.CountAdjustment)
            .Sum(m => m.Direction == StockDirection.In ? m.QuantityMilliUnits : -m.QuantityMilliUnits);

        return new MovementTotals(purchased, usage, waste, countAdjustmentNet);
    }

    public readonly record struct VarianceResult(
        long TheoreticalUsageMilliUnits, long RecordedConsumptionMilliUnits, long VarianceMilliUnits, long CountAdjustmentNetMilliUnits);

    /// <summary>Two neutral signals, never collapsed into an accusation: theoretical (sales x
    /// current recipe) vs recorded (usage + waste actually logged) - a gap usually means a recipe
    /// changed, isn't attached yet, or a loss wasn't logged, not necessarily theft. A physical
    /// count's own net adjustment is passed through as-is, alongside, never merged into the
    /// variance figure itself.</summary>
    public static VarianceResult UnexplainedVariance(long theoreticalUsageMilliUnits, MovementTotals actual)
    {
        var recordedConsumption = actual.UsageMilliUnits + actual.WasteMilliUnits;
        return new VarianceResult(theoreticalUsageMilliUnits, recordedConsumption,
            theoreticalUsageMilliUnits - recordedConsumption, actual.CountAdjustmentNetMilliUnits);
    }

    public readonly record struct SharedConsumptionResult(
        long? ActualDailyAvgMilliUnits, long? StandardDailyUsageMilliUnits, long? VarianceMilliUnitsPerDay, long ConsumedMilliUnits);

    /// <summary>Ported verbatim from sharedConsumptionReport in lib/costing-domain.mjs - standard
    /// vs actual daily usage for shared/communal items (frying oil, ketchup, gas) that have no
    /// per-order recipe line, no invented per-portion allocation. A real gap found during the
    /// legacy-parity audit: the reference app actively uses this as a real owner workflow
    /// (CostingScreen.tsx's own shared-consumption tab), and InventoryItemEntity.
    /// StandardDailyUsageMilliUnits was already ported specifically to support it, but the report
    /// function itself was never written.</summary>
    public static SharedConsumptionResult SharedConsumptionReport(long? standardDailyUsageMilliUnits, MovementTotals actual, int days)
    {
        var consumedMilliUnits = actual.UsageMilliUnits + actual.WasteMilliUnits;
        long? actualDailyAvgMilliUnits = days > 0 ? (long)Math.Round(consumedMilliUnits / (double)days) : null;
        long? varianceMilliUnitsPerDay = standardDailyUsageMilliUnits is not null && actualDailyAvgMilliUnits is not null
            ? actualDailyAvgMilliUnits - standardDailyUsageMilliUnits
            : null;
        return new SharedConsumptionResult(actualDailyAvgMilliUnits, standardDailyUsageMilliUnits, varianceMilliUnitsPerDay, consumedMilliUnits);
    }
}
