namespace Falah.Infrastructure.Pricing;

using Falah.Application.Pricing;
using Falah.Domain.Common;
using Falah.Domain.Orders;
using Falah.Domain.Pricing;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Owner, 2026-11-25: the one translation between a stored offer and the pricing rules, so the menu, checkout
/// and the offers screen read the same offer the same way.</summary>
public static class PriceRuleMapping
{
    /// <summary>An offer with everything it is made of.</summary>
    public static IQueryable<PriceRuleEntity> Full(FalahDbContext db) =>
        db.PriceRules.Include(r => r.Products).Include(r => r.Days).Include(r => r.Categories).Include(r => r.GiftTiers);

    private static IReadOnlyList<string>? Split(string? csv) =>
        string.IsNullOrWhiteSpace(csv) ? null : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? Join(IReadOnlyList<string>? values) =>
        values is not { Count: > 0 } ? null : string.Join(",", values.Select(v => v.Trim().ToLowerInvariant()).Distinct().OrderBy(v => v));

    public static OfferKind ParseKind(string? kind) =>
        Enum.TryParse<OfferKind>(kind, ignoreCase: false, out var k) && Enum.IsDefined(k) ? k : throw new DomainException("نوع العرض غير صالح.");

    public static PriceRule ToDomain(PriceRuleEntity e) => new(
        e.Id, e.Name, e.Products.Select(p => p.ProductId).ToList(), Money.FromPiastres(e.FixedPricePiastres),
        e.Days.Count > 0 ? e.Days.Select(d => (DayOfWeek)d.DayOfWeek).ToList() : null, e.StartTime, e.EndTime, e.Active)
    {
        Kind = Enum.TryParse<OfferKind>(e.Kind, out var k) ? k : OfferKind.ItemFixedPrice,
        PercentBasisPoints = e.PercentBasisPoints,
        AmountOff = Money.FromPiastres(e.AmountOffPiastres),
        CategoryIds = e.Categories.Select(c => c.CategoryId).ToList(),
        StartDate = e.StartDate, EndDate = e.EndDate,
        OrderTypes = Split(e.OrderTypes)?.Select(OrderEnumMapping.ParseOrderType).ToList(),
        Stations = Split(e.Stations),
        MaxPerOrder = e.MaxPerOrder, MaxPerDay = e.MaxPerDay, MaxTotal = e.MaxTotal,
        GiftTiers = e.GiftTiers.OrderBy(t => t.ThresholdPiastres)
            .Select(t => new GiftTier(Money.FromPiastres(t.ThresholdPiastres), t.ProductId, t.VariantId, t.Quantity)).ToList(),
        GiftIncludesDelivery = e.GiftIncludesDelivery,
        GiftAutoAdd = e.GiftAutoAdd,
    };

    public static PriceRuleDto ToDto(PriceRuleEntity e) => new(
        e.Id, e.Name, e.Products.Select(p => p.ProductId).ToList(), e.FixedPricePiastres,
        e.Days.Count > 0 ? e.Days.Select(d => d.DayOfWeek).ToList() : null, e.StartTime, e.EndTime, e.Active,
        e.Kind, e.PercentBasisPoints, e.AmountOffPiastres, e.Categories.Select(c => c.CategoryId).ToList(),
        e.StartDate, e.EndDate, Split(e.OrderTypes), Split(e.Stations), e.MaxPerOrder, e.MaxPerDay, e.MaxTotal,
        e.GiftTiers.OrderBy(t => t.ThresholdPiastres).Select(t => new GiftTierDto(t.ThresholdPiastres, t.ProductId, t.VariantId, t.Quantity)).ToList(),
        e.GiftIncludesDelivery, e.GiftAutoAdd);

    /// <summary>The offer a save request describes, as the pricing rules see it - validated before anything is stored.</summary>
    public static PriceRule FromRequest(string ruleId, SavePriceRuleRequest r)
    {
        var kind = ParseKind(r.Kind);
        var orderTypes = r.OrderTypes is { Count: > 0 } ot ? ot.Select(t => OrderEnumMapping.ParseOrderType(t)).Distinct().ToList() : null;
        return new PriceRule(ruleId, r.Name?.Trim() ?? "", r.ProductIds.Distinct().ToList(), Money.FromPiastres(r.FixedPricePiastres),
            r.DaysOfWeek is { Count: > 0 } d ? d.Distinct().Select(x => (DayOfWeek)x).ToList() : null, r.StartTime, r.EndTime, r.Active)
        {
            Kind = kind, PercentBasisPoints = r.PercentBasisPoints, AmountOff = Money.FromPiastres(r.AmountOffPiastres),
            CategoryIds = (r.CategoryIds ?? Array.Empty<string>()).Distinct().ToList(),
            StartDate = r.StartDate, EndDate = r.EndDate, OrderTypes = orderTypes,
            Stations = r.Stations is { Count: > 0 } s ? s.Select(x => x.Trim().ToLowerInvariant()).Distinct().ToList() : null,
            MaxPerOrder = r.MaxPerOrder, MaxPerDay = r.MaxPerDay, MaxTotal = r.MaxTotal,
            GiftTiers = (r.GiftTiers ?? Array.Empty<GiftTierDto>())
                .Select(t => new GiftTier(Money.FromPiastres(t.ThresholdPiastres), t.ProductId, string.IsNullOrWhiteSpace(t.VariantId) ? null : t.VariantId, t.Quantity)).ToList(),
            GiftIncludesDelivery = r.GiftIncludesDelivery, GiftAutoAdd = r.GiftAutoAdd,
        };
    }

    /// <summary>Writes a validated offer onto its row (the row's children are replaced whole).</summary>
    public static void Apply(PriceRuleEntity e, PriceRule rule, IReadOnlyList<string>? orderTypeWire)
    {
        e.Name = rule.Name;
        e.Kind = rule.Kind.ToString();
        // A field the kind does not use is stored empty, so a row never carries a price it does not charge.
        e.FixedPricePiastres = rule.Kind == OfferKind.ItemFixedPrice ? rule.FixedPrice.Piastres : 0;
        e.PercentBasisPoints = rule.Kind is OfferKind.ItemPercent or OfferKind.CategoryPercent ? rule.PercentBasisPoints : 0;
        e.AmountOffPiastres = rule.Kind == OfferKind.CategoryAmount ? rule.AmountOff.Piastres : 0;
        e.StartTime = rule.StartTime; e.EndTime = rule.EndTime;
        e.StartDate = rule.StartDate; e.EndDate = rule.EndDate;
        e.OrderTypes = Join(orderTypeWire);
        e.Stations = Join(rule.Stations);
        e.MaxPerOrder = rule.MaxPerOrder; e.MaxPerDay = rule.MaxPerDay; e.MaxTotal = rule.MaxTotal;
        e.GiftIncludesDelivery = rule.GiftIncludesDelivery;
        e.GiftAutoAdd = rule.Kind == OfferKind.OrderGift && rule.GiftAutoAdd;
        e.Active = rule.Active;
        var isItem = rule.Kind is OfferKind.ItemFixedPrice or OfferKind.ItemPercent;
        var isCategory = rule.Kind is OfferKind.CategoryAmount or OfferKind.CategoryPercent;
        e.Products = isItem ? rule.ProductIds.Select(id => new PriceRuleProductEntity { PriceRuleId = e.Id, ProductId = id }).ToList() : new();
        e.Categories = isCategory ? rule.CategoryIds.Select(id => new PriceRuleCategoryEntity { PriceRuleId = e.Id, CategoryId = id }).ToList() : new();
        e.Days = (rule.DaysOfWeek ?? Array.Empty<DayOfWeek>()).Select(d => new PriceRuleDayEntity { PriceRuleId = e.Id, DayOfWeek = (int)d }).ToList();
        e.GiftTiers = rule.Kind == OfferKind.OrderGift
            ? rule.GiftTiers.Select(t => new PriceRuleGiftTierEntity
            {
                Id = $"gift-tier-{Guid.NewGuid():N}", PriceRuleId = e.Id, ThresholdPiastres = t.Threshold.Piastres,
                ProductId = t.ProductId, VariantId = t.VariantId, Quantity = t.Quantity,
            }).ToList()
            : new();
    }
}
