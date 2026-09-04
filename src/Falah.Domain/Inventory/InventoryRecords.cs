namespace Falah.Domain.Inventory;

using Falah.Domain.Common;

public enum CostSource { Purchase, Prep }

public sealed record InventoryItem(string Id, string Name, CostSource CostSource);

public enum RecipeLineKind { Direct, Yield, Packaging }

/// <summary>One ingredient line of a menu recipe or a prep/batch recipe. A 'Yield' line's
/// QuantityMilliUnits is a portion/piece COUNT (e.g. "4 falafel pieces"), not yet a milliUnit
/// quantity of the ingredient's own unit - BaseQuantityMilliUnits resolves it back to a raw
/// weight/volume via YieldUnitsPerBaseUnit.</summary>
public sealed record RecipeLine(string InventoryItemId, long QuantityMilliUnits, RecipeLineKind Kind, long? YieldUnitsPerBaseUnit = null);

public sealed record PrepRecipe(string Id, string OutputInventoryItemId, long OutputQuantityMilliUnits, bool Active);

public enum StockMovementType { PurchaseReceipt, ManualReceipt, Usage, Waste, Spoilage, Breakage, CountAdjustment }

public enum StockDirection { In, Out }

/// <summary>QuantityMilliUnits/Direction default to 0/Out so every existing positional construction
/// of this record (costing, which only ever needed Type/UnitCost/CreatedAtUtc) keeps compiling
/// unchanged - added for InventoryRules' balance/variance functions, which need the full shape.</summary>
public sealed record StockMovementFacts(
    string InventoryItemId, StockMovementType Type, Money? UnitCost, DateTimeOffset CreatedAtUtc,
    long QuantityMilliUnits = 0, StockDirection Direction = StockDirection.Out);

public sealed record OrderPackagingRule(string OrderType, string InventoryItemId, long QuantityMilliUnits, bool Active);
