namespace Falah.Domain.Pricing;

using Falah.Domain.Common;
using Falah.Domain.Orders;

/// <summary>What an offer does (owner, 2026-11-25). Every offer is optional: the cashier picks it on the line, or the
/// normal price. A gift offer's gift is added by the cashier or by itself, as the offer says.</summary>
public enum OfferKind
{
    /// <summary>The chosen items sell at <see cref="PriceRule.FixedPrice"/>, one price for every size (the original offer).</summary>
    ItemFixedPrice,
    /// <summary>The chosen items sell <see cref="PriceRule.PercentBasisPoints"/> below their price.</summary>
    ItemPercent,
    /// <summary>Every item of the chosen categories sells <see cref="PriceRule.AmountOff"/> below its price (never below 0).</summary>
    CategoryAmount,
    /// <summary>Every item of the chosen categories sells <see cref="PriceRule.PercentBasisPoints"/> below its price.</summary>
    CategoryPercent,
    /// <summary>An order whose total reaches a tier gets that tier's gift free - the highest tier reached only.</summary>
    OrderGift,
}

/// <summary>One step of a gift offer: an order of <paramref name="Threshold"/> or more gets <paramref name="Quantity"/>
/// of the product (and size) free.</summary>
public sealed record GiftTier(Money Threshold, string ProductId, string? VariantId, int Quantity);

/// <summary>A manager-defined offer, optionally gated to days of week, a time-of-day window, a date range, order types
/// and stations, and limited in quantity. Ported from lib/pricing-domain.mjs (fixed price) and extended (owner,
/// 2026-11-25). The positional part is the original fixed-price offer; every addition has a default that keeps a
/// fixed-price offer behaving exactly as before.</summary>
public sealed record PriceRule(
    string Id,
    string Name,
    IReadOnlyList<string> ProductIds,
    Money FixedPrice,
    IReadOnlyList<DayOfWeek>? DaysOfWeek,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    bool Active)
{
    public OfferKind Kind { get; init; } = OfferKind.ItemFixedPrice;
    /// <summary>10% = 1000. For ItemPercent and CategoryPercent.</summary>
    public int PercentBasisPoints { get; init; }
    /// <summary>For CategoryAmount.</summary>
    public Money AmountOff { get; init; } = Money.Zero;
    public IReadOnlyList<string> CategoryIds { get; init; } = Array.Empty<string>();
    /// <summary>Cairo dates, both inclusive; null is open-ended.</summary>
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    /// <summary>Null or empty: every order type.</summary>
    public IReadOnlyList<OrderType>? OrderTypes { get; init; }
    /// <summary>"till" and/or "callcenter". Null or empty: both.</summary>
    public IReadOnlyList<string>? Stations { get; init; }
    /// <summary>Pieces sold at this offer: in one order, in one Cairo day, over the offer's whole life. Null: no limit.</summary>
    public int? MaxPerOrder { get; init; }
    public int? MaxPerDay { get; init; }
    public int? MaxTotal { get; init; }
    public IReadOnlyList<GiftTier> GiftTiers { get; init; } = Array.Empty<GiftTier>();
    /// <summary>A gift's threshold counts the delivery fee (true) or not (false). Takeaway and pickup have none.</summary>
    public bool GiftIncludesDelivery { get; init; } = true;
    /// <summary>The gift is put in the cart by itself (true) or offered to the cashier as a button (false).</summary>
    public bool GiftAutoAdd { get; init; }

    public bool IsItemOffer => Kind != OfferKind.OrderGift;
}
