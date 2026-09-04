namespace Falah.Application.Inventory;

/// <summary>Ported from addInventoryItem/updateInventoryItem. RequestId mints a deterministic id
/// for create (`inv-{RequestId}`) - the reference app had no idempotency key here at all (a plain
/// form-button create), but minting one from a client-supplied RequestId costs nothing and matches
/// every other Create handler in this codebase, guarding against a genuine network-level retry
/// rather than a same-task double-click (which the reference app also never guarded here).</summary>
public sealed record CreateInventoryItemRequest(
    string RequestId, string Name, string Category, string Unit, long MinimumMilliUnits,
    bool IsPackaging, string CostSource, long? StandardDailyUsageMilliUnits, string ActorUserId);

public sealed record CreateInventoryItemResult(string Id, bool WasReplayed);

/// <summary>Ported from updateInventoryItem: metadata-only (name/category/unit/minimum/active/
/// packaging/standard-usage/cost-source). Never touches balance or any historical movement. The
/// unit is locked once any movement exists against this item - see InventoryRules.AssertUnitChangeAllowed.</summary>
public sealed record UpdateInventoryItemRequest(
    string Id, string Name, string Category, string Unit, long MinimumMilliUnits, bool Active,
    bool IsPackaging, string CostSource, long? StandardDailyUsageMilliUnits, string ActorUserId);

public sealed record InventoryItemDto(
    string Id, string Name, string Category, string Unit, long MinimumMilliUnits, bool Active,
    bool IsPackaging, string CostSource, long? StandardDailyUsageMilliUnits, long BalanceMilliUnits, bool IsLowStock);
