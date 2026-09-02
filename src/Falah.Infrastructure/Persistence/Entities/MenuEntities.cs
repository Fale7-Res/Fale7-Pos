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
