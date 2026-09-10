namespace Falah.Application.Inventory;

/// <summary>
/// The storeroom, answered in one read.
///
/// The inventory screen could say what was on the shelf and what the minimum was, and nothing else -
/// not what the stock was worth, not when it last moved, not why. Everything needed to answer that
/// is already in the movement log and the costing engine; these projections put it in one place so
/// the screen does not have to ask per item and stitch the answer together itself.
/// </summary>
public sealed record InventoryOverviewRowDto(
    string Id, string Name, string Category, string Unit, bool IsPackaging, bool Active,
    long BalanceMilliUnits, long MinimumMilliUnits, bool IsLowStock,
    /// <summary>Null means the item has no cost basis at all - no purchase, no estimate, or a prep
    /// batch that is itself incomplete. It is NOT zero, and StockValuePiastres is null with it.</summary>
    long? UnitCostPiastres, string CostSource, long? StockValuePiastres,
    DateTimeOffset? LastMovementAtUtc, string? LastMovementType);

/// <summary>What the storeroom is worth, and how much of it cannot be valued at all - the second
/// figure is what stops the first from being read as the whole truth.</summary>
public sealed record InventoryOverviewDto(
    IReadOnlyList<InventoryOverviewRowDto> Items,
    long KnownStockValuePiastres, int ItemCount, int LowStockCount,
    int ItemsWithoutCostCount, int ItemsWithoutCostHoldingStockCount);

/// <summary>One movement, with the name of the item it moved. The list used to carry only the id,
/// which made a movement history unreadable without a second lookup per row.</summary>
public sealed record StockMovementRowDto(
    string InventoryItemId, string ItemName, string Unit, string Type, string Direction,
    long QuantityMilliUnits, long? UnitCostPiastres, long? LossValuePiastres,
    string ReferenceType, string ReferenceId, string Reason, string CreatedBy, string? ApprovedBy,
    DateTimeOffset CreatedAtUtc);

/// <summary>One ingredient, in full: what is there, what it is worth, what moved it, and whether
/// what the recipes say should have been used matches what was actually recorded.</summary>
public sealed record InventoryItemDetailDto(
    string Id, string Name, string Category, string Unit, bool IsPackaging, bool Active,
    long BalanceMilliUnits, long MinimumMilliUnits, bool IsLowStock,
    long? UnitCostPiastres, string CostSource, DateTimeOffset? CostAsOfUtc, long? StockValuePiastres,
    long? EstimatedUnitCostPiastres,
    MovementTotalsDto Totals, VarianceDto Variance,
    IReadOnlyList<StockMovementRowDto> Movements);
