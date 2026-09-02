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
    public long QuantityMilliUnits { get; set; }
    public string ReferenceType { get; set; } = default!;
    public string ReferenceId { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
