namespace Falah.Application.Menu;

/// <summary>Read-only catalog shapes for a client to render a menu and let the cashier pick what
/// to sell - prices here are for on-screen display only. The server independently re-resolves and
/// freezes the real price at checkout time regardless of what a client last saw here.</summary>
public sealed record MenuOfferDto(string Id, string Name, long FixedPricePiastres,
    /// <summary>Owner, 2026-11-25: OfferKind by name, and what the station needs to show the offer only where it may
    /// be used and price it as the server will.</summary>
    string Kind = "ItemFixedPrice", int PercentBasisPoints = 0, long AmountOffPiastres = 0,
    IReadOnlyList<string>? OrderTypes = null, IReadOnlyList<string>? Stations = null, int? MaxPerOrder = null)
{
    /// <summary>What a line of an item priced <paramref name="listPiastres"/> costs at this offer - the same rule the
    /// server charges (PricingRules.OfferUnitPrice).</summary>
    public long UnitPriceFor(long listPiastres) => Falah.Domain.Pricing.PricingRules.OfferUnitPrice(
        new Falah.Domain.Pricing.PriceRule(Id, Name, Array.Empty<string>(), Falah.Domain.Common.Money.FromPiastres(FixedPricePiastres), null, null, null, true)
        {
            Kind = Enum.TryParse<Falah.Domain.Pricing.OfferKind>(Kind, out var k) ? k : Falah.Domain.Pricing.OfferKind.ItemFixedPrice,
            PercentBasisPoints = PercentBasisPoints, AmountOff = Falah.Domain.Common.Money.FromPiastres(AmountOffPiastres),
        }, Falah.Domain.Common.Money.FromPiastres(listPiastres)).Piastres;

    /// <summary>"10%" / "10 ج أقل" / "40 ج" - what the offer button says.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string PriceLabel => Kind switch
    {
        "ItemPercent" or "CategoryPercent" => $"خصم {PercentBasisPoints / 100m:0.##}%",
        "CategoryAmount" => $"{AmountOffPiastres / 100m:0.##} ج أقل",
        _ => $"{FixedPricePiastres / 100m:0.00} ج",
    };

    /// <summary>Whether a station may offer it on this order ("takeaway"/"delivery"/"pickup", "till"/"callcenter").</summary>
    public bool AllowsOrder(string orderType, string station) =>
        (OrderTypes is not { Count: > 0 } types || types.Contains(orderType)) && (Stations is not { Count: > 0 } s || s.Contains(station));
}

/// <summary>One step of a gift offer as a station shows it.</summary>
public sealed record MenuGiftTierDto(long ThresholdPiastres, string ProductId, string? VariantId, string ProductName, string? VariantName, int Quantity);

/// <summary>Owner, 2026-11-25: a gift offer live now - an order whose total reaches a tier gets that tier's gift free
/// (the highest tier reached only). IncludesDelivery: the threshold counts the delivery fee. AutoAdd: the gift goes in
/// the cart by itself; otherwise the cashier is offered a button.</summary>
public sealed record MenuGiftOfferDto(string Id, string Name, bool IncludesDelivery, bool AutoAdd,
    IReadOnlyList<string>? OrderTypes, IReadOnlyList<string>? Stations, IReadOnlyList<MenuGiftTierDto> Tiers)
{
    public bool AllowsOrder(string orderType, string station) =>
        (OrderTypes is not { Count: > 0 } types || types.Contains(orderType)) && (Stations is not { Count: > 0 } s || s.Contains(station));

    /// <summary>The highest tier an order of <paramref name="basisPiastres"/> reaches, or null.</summary>
    public MenuGiftTierDto? TierFor(long basisPiastres) =>
        Tiers.Where(t => basisPiastres >= t.ThresholdPiastres).OrderByDescending(t => t.ThresholdPiastres).FirstOrDefault();
}

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
    IReadOnlyList<string> BestSellerProductIds,
    /// <summary>Gift offers live now (owner, 2026-11-25). Null from a server older than gifts.</summary>
    IReadOnlyList<MenuGiftOfferDto>? GiftOffers = null);
