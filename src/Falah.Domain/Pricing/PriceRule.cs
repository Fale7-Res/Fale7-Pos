namespace Falah.Domain.Pricing;

using Falah.Domain.Common;

/// <summary>A manager-defined fixed-price offer for specific products, optionally gated to certain
/// days of week and a time-of-day window. Ported from lib/pricing-domain.mjs.</summary>
public sealed record PriceRule(
    string Id,
    string Name,
    IReadOnlyList<string> ProductIds,
    Money FixedPrice,
    IReadOnlyList<DayOfWeek>? DaysOfWeek,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    bool Active);
