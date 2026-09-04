namespace Falah.Application.Menu;

/// <summary>Read-only catalog shapes for a client to render a menu and let the cashier pick what
/// to sell - prices here are for on-screen display only. The server independently re-resolves and
/// freezes the real price at checkout time regardless of what a client last saw here.</summary>
public sealed record MenuOfferDto(string Id, string Name, long FixedPricePiastres);

public sealed record MenuVariantDto(string Id, string Name, long PricePiastres);

public sealed record MenuProductDto(
    string Id, string Name, long BasePricePiastres, bool SendToGrill, bool IsAvailable,
    IReadOnlyList<MenuVariantDto> Variants, IReadOnlyList<MenuOfferDto> ApplicableOffers, string? PreparationStation);

public sealed record MenuCategoryDto(string Id, string Name, string? Icon, IReadOnlyList<MenuProductDto> Products);

public sealed record MenuResponse(IReadOnlyList<MenuCategoryDto> Categories);
