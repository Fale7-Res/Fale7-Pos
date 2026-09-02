namespace Falah.Domain.Pricing;

using Falah.Domain.Common;

/// <summary>
/// The one place that turns "this product/variant, this requested offer" into what an order line
/// actually freezes. The client requests a price rule (a chip tap, or a Call Center dispatcher's
/// choice) but never dictates a price - see ValidateAppliedRule's own contract: a stale/disabled/
/// out-of-window rule is silently ignored in favour of the list price rather than blocking the
/// sale, exactly as it already behaved when re-validated at checkout in the current app.
/// </summary>
public static class ItemPricingRules
{
    public readonly record struct ResolvedItemPrice(Money ListUnitPrice, Money EffectiveUnitPrice, string? AppliedPriceRuleId, string? AppliedPriceRuleName)
    {
        public bool RuleApplied => AppliedPriceRuleId is not null;
    }

    public static ResolvedItemPrice Resolve(
        string productId, Money listUnitPrice, string? requestedPriceRuleId,
        IEnumerable<PriceRule> allRules, DateTimeOffset whenUtc, TimeZoneInfo timeZone)
    {
        var validRule = PricingRules.ValidateAppliedRule(productId, requestedPriceRuleId, allRules, whenUtc, timeZone);
        return validRule is null
            ? new ResolvedItemPrice(listUnitPrice, listUnitPrice, null, null)
            : new ResolvedItemPrice(listUnitPrice, validRule.FixedPrice, validRule.Id, validRule.Name);
    }
}
