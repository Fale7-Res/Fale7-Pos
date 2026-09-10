namespace Falah.Infrastructure.Persistence.Entities;

public class RecipeEntity
{
    public string Id { get; set; } = default!;
    public string ProductId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool Active { get; set; }

    public List<RecipeItemEntity> Items { get; set; } = new();
}

public class RecipeItemEntity
{
    public int Id { get; set; }
    public string RecipeId { get; set; } = default!;
    public string InventoryItemId { get; set; } = default!;
    public long QuantityMilliUnits { get; set; }
    public string Kind { get; set; } = default!;
    public long? YieldUnitsPerBaseUnit { get; set; }
}

public class InventoryItemEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string CostSource { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public long MinimumMilliUnits { get; set; }
    public bool Active { get; set; }
    /// <summary>True for wrapping/bags/boxes etc. - lets costing report packaging cost separately
    /// from food cost. Recipe lines of kind 'Packaging' are expected to reference one of these.</summary>
    public bool IsPackaging { get; set; }

    /// <summary>A unit cost the owner entered by hand, used only while no real purchase exists for
    /// this item. It exists so a restaurant can see an approximate food cost before its first
    /// invoice is recorded, WITHOUT the alternative: fabricating purchase invoices, which would post
    /// supplier obligations, move treasury money and add stock that never arrived. Every report says
    /// which of the two a figure came from - see CostResultSource.</summary>
    public long? EstimatedUnitCostPiastres { get; set; }
    /// <summary>For shared/communal consumption (frying oil, ketchup, gas) that cannot be
    /// defensibly allocated to a single menu item - an editable reference figure to compare against
    /// actual recorded usage, without inventing a fake per-portion allocation into any recipe.</summary>
    public long? StandardDailyUsageMilliUnits { get; set; }
}

public class StockMovementEntity
{
    public int Id { get; set; }
    /// <summary>Same idempotency contract as TreasuryMovements/OrderRefunds - the database's own
    /// unique constraint, not just an application-level check, is what guarantees a retried
    /// checkout can never deduct the same stock twice.</summary>
    public string IdempotencyKey { get; set; } = default!;
    public string InventoryItemId { get; set; } = default!;
    public string Type { get; set; } = default!;
    /// <summary>'In' for receipts and a count adjustment that raises the balance, 'Out' for usage,
    /// every loss type, and a count adjustment that lowers it.</summary>
    public string Direction { get; set; } = default!;
    public long QuantityMilliUnits { get; set; }
    /// <summary>Only ever set for a loss movement (waste/spoilage/breakage): the LAST ACTUAL
    /// PURCHASE unit cost, frozen at posting time - see LossValuePiastres.</summary>
    public long? UnitCostPiastres { get; set; }
    /// <summary>Loss value in piastres, frozen when the loss was posted. Never recomputed from a
    /// later purchase price - see PurchasingRules.ValueStockLoss's own doc comment.</summary>
    public long? LossValuePiastres { get; set; }
    public string ReferenceType { get; set; } = default!;
    public string ReferenceId { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public string? ApprovedBy { get; set; }
    public string? OperationId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    /// <summary>Only ever set for a PurchaseReceipt movement - which supplier this stock came from.</summary>
    public string? SupplierId { get; set; }
}

/// <summary>Ported from PrepRecipe: a batch/prep recipe producing a fixed output quantity of an
/// in-house "prepared" InventoryItem (costSource=Prep) - e.g. tahini, toum, a house sauce. The
/// output item is never purchased directly; its unit cost is derived from this recipe instead of
/// purchase history (see CostingRules.IngredientUnitCost).</summary>
public class PrepRecipeEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string OutputInventoryItemId { get; set; } = default!;
    public long OutputQuantityMilliUnits { get; set; }
    public bool Active { get; set; }

    public List<PrepRecipeLineEntity> Lines { get; set; } = new();
}

public class PrepRecipeLineEntity
{
    public int Id { get; set; }
    public string PrepRecipeId { get; set; } = default!;
    public string InventoryItemId { get; set; } = default!;
    public long QuantityMilliUnits { get; set; }
}

/// <summary>Ported from OrderPackagingRule: order-level (not item-level) packaging - e.g. one
/// delivery bag per delivery order regardless of item count, applied once per qualifying order
/// type rather than duplicated per line (see CostingRules.OrderPackagingCost).</summary>
public class OrderPackagingRuleEntity
{
    public string Id { get; set; } = default!;
    public string OrderType { get; set; } = default!;
    public string InventoryItemId { get; set; } = default!;
    public long QuantityMilliUnits { get; set; }
    public bool Active { get; set; }
}
