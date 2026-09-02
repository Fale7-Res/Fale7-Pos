using Falah.Domain.Common;
using Falah.Domain.Pricing;
using Xunit;

namespace Falah.Domain.Tests;

public class ItemPricingRulesTests
{
    private static readonly TimeZoneInfo Cairo = BusinessClock.CairoTimeZone;
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void No_requested_rule_falls_back_to_the_list_price()
    {
        var result = ItemPricingRules.Resolve("prod-1", Money.FromEgp(50m), null, Array.Empty<PriceRule>(), Now, Cairo);
        Assert.Equal(Money.FromEgp(50m), result.EffectiveUnitPrice);
        Assert.False(result.RuleApplied);
    }

    [Fact]
    public void A_valid_active_rule_overrides_the_list_price_and_is_captured_for_the_frozen_snapshot()
    {
        var rule = new PriceRule("rule-1", "عرض الغداء", new[] { "prod-1" }, Money.FromEgp(35m), null, null, null, true);
        var result = ItemPricingRules.Resolve("prod-1", Money.FromEgp(50m), "rule-1", new[] { rule }, Now, Cairo);

        Assert.Equal(Money.FromEgp(35m), result.EffectiveUnitPrice);
        Assert.Equal(Money.FromEgp(50m), result.ListUnitPrice);
        Assert.Equal("rule-1", result.AppliedPriceRuleId);
        Assert.Equal("عرض الغداء", result.AppliedPriceRuleName);
    }

    [Fact]
    public void A_rule_that_expired_since_it_was_requested_is_silently_ignored_not_rejected()
    {
        // The cart sat open past the offer's window - checkout re-validates and falls back to the
        // list price rather than blocking the sale or trusting the stale request.
        var rule = new PriceRule("rule-1", "عرض الغداء", new[] { "prod-1" }, Money.FromEgp(35m),
            null, new TimeOnly(12, 0), new TimeOnly(13, 0), true);
        // 18:00 Cairo (UTC+2) = 16:00 UTC - well outside the rule's noon window.
        var checkoutTime = new DateTimeOffset(2026, 3, 5, 16, 0, 0, TimeSpan.Zero);
        var result = ItemPricingRules.Resolve("prod-1", Money.FromEgp(50m), "rule-1", new[] { rule }, checkoutTime, Cairo);

        Assert.Equal(Money.FromEgp(50m), result.EffectiveUnitPrice);
        Assert.False(result.RuleApplied);
    }

    [Fact]
    public void A_rule_for_a_different_product_is_ignored()
    {
        var rule = new PriceRule("rule-1", "عرض", new[] { "prod-2" }, Money.FromEgp(35m), null, null, null, true);
        var result = ItemPricingRules.Resolve("prod-1", Money.FromEgp(50m), "rule-1", new[] { rule }, Now, Cairo);
        Assert.False(result.RuleApplied);
    }
}
