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

/// <summary>
/// A product photo, kept in its own table rather than as a column on ProductEntity. Every read of
/// the menu loads products; almost none of them want the bytes, and an image column would drag
/// tens of kilobytes per row into every one of those reads.
///
/// The bytes live in the database, not on a file share, so a restore of the database restores a
/// menu that still has its pictures - a directory of files beside it would be a second thing to
/// back up and a second thing to forget.
/// </summary>
public class ProductImageEntity
{
    /// <summary>The product this belongs to. One image per product, so the product's own id is the
    /// key - there is no separate image identity to manage or leak.</summary>
    public string ProductId { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public byte[] Bytes { get; set; } = default!;
    /// <summary>Changes on every upload. The clients put it in the image URL so a replaced photo
    /// actually appears instead of being served from an image cache keyed on the product id.</summary>
    public string Version { get; set; } = default!;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string UpdatedByUserId { get; set; } = default!;
}

public class ProductVariantEntity
{
    public string Id { get; set; } = default!;
    public string ProductId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public long PricePiastres { get; set; }
    public string? RecipeId { get; set; }
}
