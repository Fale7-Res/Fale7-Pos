namespace Falah.Domain.Pricing;

using Falah.Domain.Common;

/// <summary>Pure pricing rules ported 1:1 from lib/pricing-domain.mjs so a cashier can never reach
/// a different answer than the server-side domain does.</summary>
public static class PricingRules
{
    /// <summary>Is this rule live at this instant? A window whose end is &lt;= its start is treated
    /// as crossing midnight (e.g. 22:00 -&gt; 02:00), which is ordinary for a restaurant late shift.
    /// A half-configured window (only one of start/end set) is ignored rather than guessed at.</summary>
    public static bool IsActiveAt(PriceRule rule, DateTimeOffset whenUtc, TimeZoneInfo timeZone)
    {
        if (!rule.Active) return false;

        var local = TimeZoneInfo.ConvertTime(whenUtc, timeZone);
        if (rule.DaysOfWeek is { Count: > 0 } days && !days.Contains(local.DayOfWeek)) return false;

        if (rule.StartTime is not { } start || rule.EndTime is not { } end) return true;

        var now = TimeOnly.FromDateTime(local.DateTime);
        return start <= end
            ? now >= start && now <= end
            : now >= start || now <= end;
    }

    /// <summary>Rules a cashier may legitimately offer for this product right now.</summary>
    public static IReadOnlyList<PriceRule> ApplicableRules(
        string productId, IEnumerable<PriceRule> rules, DateTimeOffset whenUtc, TimeZoneInfo timeZone) =>
        rules.Where(rule => rule.ProductIds.Contains(productId) && IsActiveAt(rule, whenUtc, timeZone)).ToList();

    /// <summary>Re-validated at checkout, not just when the chip was tapped: a cart can sit open
    /// across the end of an offer window, and the price actually charged must be one the rule
    /// really permits at that moment. Returns the rule, or null when it may no longer be applied.</summary>
    public static PriceRule? ValidateAppliedRule(
        string productId, string? ruleId, IEnumerable<PriceRule> rules, DateTimeOffset whenUtc, TimeZoneInfo timeZone)
    {
        if (string.IsNullOrEmpty(ruleId)) return null;
        var rule = rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule is null) return null;
        return ApplicableRules(productId, new[] { rule }, whenUtc, timeZone).Count > 0 ? rule : null;
    }

    /// <summary>A comp may never exceed the value actually being given away, and may never be
    /// zero/negative.</summary>
    public static void AssertCompAmount(Money compAmount, Money orderValue)
    {
        if (compAmount.Piastres <= 0) throw new DomainException("قيمة الضيافة يجب أن تكون أكبر من صفر.");
        if (orderValue.Piastres <= 0) throw new DomainException("لا يمكن تسجيل ضيافة على طلب بدون قيمة.");
        if (compAmount > orderValue) throw new DomainException("قيمة الضيافة لا يمكن أن تتجاوز قيمة الطلب.");
    }
}
