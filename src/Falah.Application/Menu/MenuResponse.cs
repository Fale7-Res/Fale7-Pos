namespace Falah.Application.Menu;

/// <summary>Read-only catalog shapes for a client to render a menu and let the cashier pick what
/// to sell - prices here are for on-screen display only. The server independently re-resolves and
/// freezes the real price at checkout time regardless of what a client last saw here.</summary>
public sealed record MenuOfferDto(string Id, string Name, long FixedPricePiastres);

public sealed record MenuVariantDto(string Id, string Name, long PricePiastres);

public sealed record MenuProductDto(
    string Id, string Name, long BasePricePiastres, bool SendToGrill, bool IsAvailable,
    IReadOnlyList<MenuVariantDto> Variants, IReadOnlyList<MenuOfferDto> ApplicableOffers, string? PreparationStation,
    /// <summary>Stamp of the product's current photo, or null when it has none. The client builds
    /// the image URL from it; the menu response never carries the bytes themselves.</summary>
    string? ImageVersion = null);

public sealed record MenuCategoryDto(string Id, string Name, string? Icon, IReadOnlyList<MenuProductDto> Products);

public sealed record MenuResponse(
    IReadOnlyList<MenuCategoryDto> Categories,
    /// <summary>Product ids ordered by how much of each was actually sold over the recent window,
    /// most sold first. Computed from orders - there is no "favourite" flag anyone sets by hand,
    /// because a flag someone set once stops being true and nobody notices. Empty for a restaurant
    /// with no sales history yet, and the client shows nothing rather than an empty shortcut.</summary>
    IReadOnlyList<string> BestSellerProductIds);
