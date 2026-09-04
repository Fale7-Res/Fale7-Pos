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

    /// <summary>The reference app's own comp-reason taxonomy (types.ts `CompReasonCode`) - kept as a
    /// closed set so "how much was given away, by reason" reporting is possible later, the same way
    /// legacy's own compReasonCode/compReasonLabel pair was structured (the free-text note is a
    /// separate field, unchanged).</summary>
    public static readonly IReadOnlySet<string> CompReasonCodes = new HashSet<string>
    {
        "hospitality", "complaint", "kitchen_error", "staff_meal", "owner_guest",
    };

    public static void AssertCompReasonCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || !CompReasonCodes.Contains(code))
            throw new DomainException("سبب الضيافة غير صالح.");
    }

    /// <summary>Ported verbatim from assertPriceRuleInput in lib/pricing-domain.mjs. Deliberately does
    /// NOT check for overlap with another rule on the same product/window - the reference app never
    /// validates that either (ApplicableRules/ValidateAppliedRule above are both already built to
    /// tolerate several rules matching the same product at once; the cashier/dispatcher picks one
    /// explicitly), so inventing an overlap-rejection rule here would be a new policy this system's
    /// own evidence does not support.</summary>
    public static void AssertPriceRuleInput(string name, IReadOnlyCollection<string> productIds, Money fixedPrice, IReadOnlyCollection<int>? daysOfWeek)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("اسم العرض مطلوب.");
        if (productIds.Count == 0) throw new DomainException("يجب اختيار صنف واحد على الأقل للعرض.");
        if (!fixedPrice.IsPositive) throw new DomainException("سعر العرض يجب أن يكون رقمًا صحيحًا أكبر من صفر.");
        if (daysOfWeek is { Count: > 0 } days && days.Any(d => d is < 0 or > 6))
            throw new DomainException("أيام العرض غير صالحة.");
    }
}
