using Falah.Domain.Common;
using Falah.Domain.Orders;
using Falah.Domain.Pricing;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>Owner, 2026-11-25: offers - a price or a percentage on items, an amount or a percentage on a whole
/// category, a gift on an order over an amount (highest tier only); days, hours (across midnight), dates, order types,
/// stations and limits; every offer optional.</summary>
public class OfferRulesTests
{
    private static readonly TimeZoneInfo Cairo = BusinessClock.CairoTimeZone;
    private static DateTimeOffset CairoAt(int y, int m, int d, int h, int min = 0) =>
        new DateTimeOffset(y, m, d, h, min, 0, TimeSpan.FromHours(2)).ToUniversalTime();   // Cairo is UTC+2 in November

    private static PriceRule Rule(OfferKind kind = OfferKind.ItemFixedPrice) =>
        new("r", "عرض", new[] { "p" }, Money.FromEgp(40), null, null, null, true) { Kind = kind };

    // ---- price ----

    [Fact]
    public void A_fixed_price_is_charged_as_set_for_every_size() =>
        Assert.Equal(Money.FromEgp(40), PricingRules.OfferUnitPrice(Rule(), Money.FromEgp(95)));

    [Fact]
    public void A_percentage_comes_off_each_size_rounded_to_the_piastre()
    {
        var tenPercent = Rule(OfferKind.ItemPercent) with { PercentBasisPoints = 1000 };
        Assert.Equal(Money.FromPiastres(8_550), PricingRules.OfferUnitPrice(tenPercent, Money.FromEgp(95)));
        var third = Rule(OfferKind.ItemPercent) with { PercentBasisPoints = 3333 };
        Assert.Equal(Money.FromPiastres(667), PricingRules.OfferUnitPrice(third, Money.FromPiastres(1_000)));   // 333.3 -> 333 off
    }

    [Fact]
    public void An_amount_off_a_category_never_goes_below_zero()
    {
        var tenOff = Rule(OfferKind.CategoryAmount) with { AmountOff = Money.FromEgp(10), CategoryIds = new[] { "c" } };
        Assert.Equal(Money.FromEgp(40), PricingRules.OfferUnitPrice(tenOff, Money.FromEgp(50)));
        Assert.Equal(Money.Zero, PricingRules.OfferUnitPrice(tenOff, Money.FromEgp(8)));
    }

    [Fact]
    public void A_category_offer_covers_the_categorys_items_and_an_item_offer_only_its_own()
    {
        var category = Rule(OfferKind.CategoryPercent) with { PercentBasisPoints = 500, CategoryIds = new[] { "drinks" } };
        Assert.True(PricingRules.Targets(category, "any", "drinks"));
        Assert.False(PricingRules.Targets(category, "p", "food"));
        Assert.True(PricingRules.Targets(Rule(), "p", null));
        Assert.False(PricingRules.Targets(Rule(), "other", "drinks"));
        Assert.False(PricingRules.Targets(Rule(OfferKind.OrderGift), "p", null));   // a gift is never an item's price
    }

    [Fact]
    public void Resolve_uses_the_offers_own_price_kind()
    {
        var category = Rule(OfferKind.CategoryAmount) with { AmountOff = Money.FromEgp(5), CategoryIds = new[] { "c" } };
        var resolved = ItemPricingRules.Resolve("x", Money.FromEgp(30), "r", new[] { category }, CairoAt(2026, 11, 26, 12), Cairo, "c");
        Assert.Equal((Money.FromEgp(25), Money.FromEgp(30), "r"), (resolved.EffectiveUnitPrice, resolved.ListUnitPrice, resolved.AppliedPriceRuleId));
    }

    // ---- when ----

    [Fact]
    public void A_window_across_midnight_belongs_to_the_day_it_opened()
    {
        // Thursday 26 Nov 2026, 22:00 -> 02:00, Thursdays only.
        var rule = Rule() with { DaysOfWeek = new[] { DayOfWeek.Thursday }, StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(2, 0) };
        Assert.True(PricingRules.IsActiveAt(rule, CairoAt(2026, 11, 26, 23), Cairo));
        Assert.True(PricingRules.IsActiveAt(rule, CairoAt(2026, 11, 27, 1), Cairo));    // Friday 01:00: Thursday's night
        Assert.False(PricingRules.IsActiveAt(rule, CairoAt(2026, 11, 26, 1), Cairo));   // Thursday 01:00: Wednesday's night
        Assert.False(PricingRules.IsActiveAt(rule, CairoAt(2026, 11, 26, 12), Cairo));
    }

    [Fact]
    public void A_date_range_is_inclusive_on_cairo_dates()
    {
        var rule = Rule() with { StartDate = new DateOnly(2026, 11, 26), EndDate = new DateOnly(2026, 11, 30) };
        Assert.False(PricingRules.IsActiveAt(rule, CairoAt(2026, 11, 25, 23, 59), Cairo));
        Assert.True(PricingRules.IsActiveAt(rule, CairoAt(2026, 11, 26, 0, 1), Cairo));
        Assert.True(PricingRules.IsActiveAt(rule, CairoAt(2026, 11, 30, 23, 59), Cairo));
        Assert.False(PricingRules.IsActiveAt(rule, CairoAt(2026, 12, 1, 0, 1), Cairo));
    }

    [Fact]
    public void Order_types_and_stations_limit_who_may_use_an_offer()
    {
        var rule = Rule() with { OrderTypes = new[] { OrderType.Delivery }, Stations = new[] { "callcenter" } };
        Assert.True(PricingRules.AllowsOrder(rule, OrderType.Delivery, "callcenter"));
        Assert.False(PricingRules.AllowsOrder(rule, OrderType.Takeaway, "callcenter"));
        Assert.False(PricingRules.AllowsOrder(rule, OrderType.Delivery, "till"));
        Assert.True(PricingRules.AllowsOrder(Rule(), OrderType.Pickup, "till"));
        Assert.Equal("callcenter", PricingRules.StationOf("CallCenter"));
        Assert.Equal("till", PricingRules.StationOf(null));
    }

    // ---- gift ----

    private static PriceRule Gift(bool includesDelivery = true) => Rule(OfferKind.OrderGift) with
    {
        ProductIds = Array.Empty<string>(), GiftIncludesDelivery = includesDelivery,
        GiftTiers = new[] { new GiftTier(Money.FromEgp(200), "cola", null, 1), new GiftTier(Money.FromEgp(500), "cola", null, 2) },
    };

    [Theory]
    [InlineData(199_99, 0)]
    [InlineData(200_00, 1)]
    [InlineData(499_99, 1)]
    [InlineData(500_00, 2)]
    [InlineData(900_00, 2)]
    public void A_gift_gives_the_highest_tier_reached_only(long totalPiastres, int colas) =>
        Assert.Equal(colas, PricingRules.GiftTierFor(Gift(), Money.FromPiastres(totalPiastres))?.Quantity ?? 0);

    [Fact]
    public void A_gift_threshold_counts_the_delivery_fee_only_when_the_offer_says_so()
    {
        Assert.Equal(Money.FromEgp(510), PricingRules.GiftBasis(Gift(includesDelivery: true), Money.FromEgp(510), Money.FromEgp(20)));
        Assert.Equal(Money.FromEgp(490), PricingRules.GiftBasis(Gift(includesDelivery: false), Money.FromEgp(510), Money.FromEgp(20)));
    }

    // ---- saving ----

    [Fact]
    public void Each_kind_asks_for_what_it_needs()
    {
        Assert.Throws<DomainException>(() => PricingRules.AssertOfferInput(Rule(OfferKind.ItemPercent)));                         // no percent
        Assert.Throws<DomainException>(() => PricingRules.AssertOfferInput(Rule(OfferKind.CategoryAmount) with { AmountOff = Money.FromEgp(5) }));   // no category
        Assert.Throws<DomainException>(() => PricingRules.AssertOfferInput(Rule(OfferKind.OrderGift)));                           // no tier
        Assert.Throws<DomainException>(() => PricingRules.AssertOfferInput(Rule() with { StartTime = new TimeOnly(10, 0) }));    // half a window
        Assert.Throws<DomainException>(() => PricingRules.AssertOfferInput(Rule() with { StartDate = new DateOnly(2026, 12, 2), EndDate = new DateOnly(2026, 12, 1) }));
        Assert.Throws<DomainException>(() => PricingRules.AssertOfferInput(Rule() with { MaxPerDay = 0 }));
        Assert.Throws<DomainException>(() => PricingRules.AssertOfferInput(Rule(OfferKind.ItemPercent) with { PercentBasisPoints = 10_001 }));
        Assert.Throws<DomainException>(() => PricingRules.AssertOfferInput(Gift() with
        { GiftTiers = new[] { new GiftTier(Money.FromEgp(200), "a", null, 1), new GiftTier(Money.FromEgp(200), "b", null, 1) } }));

        PricingRules.AssertOfferInput(Rule());
        PricingRules.AssertOfferInput(Gift());
        PricingRules.AssertOfferInput(Rule(OfferKind.CategoryPercent) with { PercentBasisPoints = 1000, CategoryIds = new[] { "c" } });
    }
}
