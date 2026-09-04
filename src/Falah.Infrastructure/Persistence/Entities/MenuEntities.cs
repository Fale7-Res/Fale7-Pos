namespace Falah.Infrastructure.Persistence.Entities;

public class CategoryEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool Active { get; set; }
}

public class ProductEntity
{
    public string Id { get; set; } = default!;
    public string CategoryId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public long BasePricePiastres { get; set; }
    public bool SendToGrill { get; set; }
    /// <summary>Which physical kitchen printer this product's ticket goes to - "Charcoal", "Grill",
    /// or null (no prep ticket at all, e.g. a bottled drink). This is the LIVE, current menu
    /// configuration only - it governs FUTURE orders. Every sold OrderItem freezes its own copy of
    /// this value at checkout (see MenuPricingService and OrderItem.PreparationStation), so editing
    /// this field can never retroactively change how an already-committed order routes or reprints -
    /// see TicketItem's own doc comment for the historical-consistency guarantee this preserves.
    /// Independent of SendToGrill, which only decides the order's own Preparing/Completed status and
    /// predates the two-station split - a product can carry either or both without conflict.</summary>
    public string? PreparationStation { get; set; }
    public bool IsAvailable { get; set; }
    public int SortOrder { get; set; }
    /// <summary>The recipe used when the sold line has no variant, or the variant itself carries
    /// no recipe of its own.</summary>
    public string? RecipeId { get; set; }

    public List<ProductVariantEntity> Variants { get; set; } = new();
}

public class ProductVariantEntity
{
    public string Id { get; set; } = default!;
    public string ProductId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public long PricePiastres { get; set; }
    public string? RecipeId { get; set; }
}
