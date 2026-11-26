namespace Falah.Domain.Pricing;

using Falah.Domain.Common;
using Falah.Domain.Orders;

/// <summary>Pure pricing rules ported 1:1 from lib/pricing-domain.mjs so a cashier can never reach
/// a different answer than the server-side domain does.</summary>
public static class PricingRules
{
    /// <summary>Is this rule live at this instant? A window whose end is &lt;= its start is treated
    /// as crossing midnight (e.g. 22:00 -&gt; 02:00), which is ordinary for a restaurant late shift.
    /// A half-configured window (only one of start/end set) is ignored rather than guessed at.
    ///
    /// Owner, 2026-11-25: the hours after midnight of a window that crosses it belong to the day the window opened -
    /// an offer for Friday 22:00 -> 02:00 is still on at 01:00 Saturday, and not on at 01:00 Friday. The day of week and
    /// the date range are both read on that day.</summary>
    public static bool IsActiveAt(PriceRule rule, DateTimeOffset whenUtc, TimeZoneInfo timeZone)
    {
        if (!rule.Active) return false;

        var local = TimeZoneInfo.ConvertTime(whenUtc, timeZone);
        var now = TimeOnly.FromDateTime(local.DateTime);
        var day = DateOnly.FromDateTime(local.DateTime);

        var windowed = rule.StartTime is { } start && rule.EndTime is { } end;
        if (windowed)
        {
            var (s, e) = (rule.StartTime!.Value, rule.EndTime!.Value);
            if (s <= e) { if (now < s || now > e) return false; }
            else if (now >= s) { }                     // the evening part: today
            else if (now <= e) day = day.AddDays(-1);  // after midnight: the day it opened
            else return false;
        }

        if (rule.DaysOfWeek is { Count: > 0 } days && !days.Contains(day.DayOfWeek)) return false;
        if (rule.StartDate is { } from && day < from) return false;
        if (rule.EndDate is { } to && day > to) return false;
        return true;
    }

    /// <summary>Whether an item offer covers this product - chosen by name, or through its category.</summary>
    public static bool Targets(PriceRule rule, string productId, string? categoryId) => rule.Kind switch
    {
        OfferKind.ItemFixedPrice or OfferKind.ItemPercent => rule.ProductIds.Contains(productId),
        OfferKind.CategoryAmount or OfferKind.CategoryPercent => categoryId is not null && rule.CategoryIds.Contains(categoryId),
        _ => false,
    };

    /// <summary>Rules a cashier may legitimately offer for this product right now.</summary>
    public static IReadOnlyList<PriceRule> ApplicableRules(
        string productId, IEnumerable<PriceRule> rules, DateTimeOffset whenUtc, TimeZoneInfo timeZone, string? categoryId = null) =>
        rules.Where(rule => Targets(rule, productId, categoryId) && IsActiveAt(rule, whenUtc, timeZone)).ToList();

    /// <summary>Re-validated at checkout, not just when the chip was tapped: a cart can sit open
    /// across the end of an offer window, and the price actually charged must be one the rule
    /// really permits at that moment. Returns the rule, or null when it may no longer be applied.</summary>
    public static PriceRule? ValidateAppliedRule(
        string productId, string? ruleId, IEnumerable<PriceRule> rules, DateTimeOffset whenUtc, TimeZoneInfo timeZone, string? categoryId = null)
    {
        if (string.IsNullOrEmpty(ruleId)) return null;
        var rule = rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule is null) return null;
        return ApplicableRules(productId, new[] { rule }, whenUtc, timeZone, categoryId).Count > 0 ? rule : null;
    }

    /// <summary>The unit price an item offer charges for an item whose own price is <paramref name="listPrice"/>.
    /// A fixed price is charged as set, whatever the size (owner, 2026-11-25: as before). A percentage is rounded to
    /// the piastre (half away from zero); an amount off never takes the price below zero.</summary>
    public static Money OfferUnitPrice(PriceRule rule, Money listPrice) => rule.Kind switch
    {
        OfferKind.ItemFixedPrice => rule.FixedPrice,
        OfferKind.ItemPercent or OfferKind.CategoryPercent =>
            listPrice - Money.FromPiastres((long)Math.Round(listPrice.Piastres * (decimal)rule.PercentBasisPoints / 10_000m, MidpointRounding.AwayFromZero)),
        OfferKind.CategoryAmount => rule.AmountOff >= listPrice ? Money.Zero : listPrice - rule.AmountOff,
        _ => listPrice,
    };

    /// <summary>"till" for a till's order, "callcenter" for one taken at the call center.</summary>
    public static string StationOf(string? customerPrintRoute) => customerPrintRoute == "CallCenter" ? "callcenter" : "till";

    /// <summary>Whether the offer is open to this kind of order taken at this station.</summary>
    public static bool AllowsOrder(PriceRule rule, OrderType orderType, string station) =>
        (rule.OrderTypes is not { Count: > 0 } types || types.Contains(orderType))
        && (rule.Stations is not { Count: > 0 } stations || stations.Contains(station));

    /// <summary>The highest tier an order of <paramref name="amount"/> reaches, or null. Only one tier is given: an
    /// order over 500 gets the 500 gift, not the 200 one as well (owner, 2026-11-25).</summary>
    public static GiftTier? GiftTierFor(PriceRule rule, Money amount) =>
        rule.Kind != OfferKind.OrderGift ? null
        : rule.GiftTiers.Where(t => amount >= t.Threshold).OrderByDescending(t => t.Threshold.Piastres).FirstOrDefault();

    /// <summary>What a gift's threshold is measured on (owner, 2026-11-25): what the customer actually pays - items
    /// after offers, discount and comp, with tax - with or without the delivery fee as the offer says.</summary>
    public static Money GiftBasis(PriceRule rule, Money orderTotal, Money deliveryFee) =>
        rule.GiftIncludesDelivery ? orderTotal : orderTotal - deliveryFee;

    public static readonly IReadOnlySet<string> StationCodes = new HashSet<string> { "till", "callcenter" };

    /// <summary>The whole shape of an offer, checked when it is saved.</summary>
    public static void AssertOfferInput(PriceRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Name)) throw new DomainException("اسم العرض مطلوب.");
        switch (rule.Kind)
        {
            case OfferKind.ItemFixedPrice:
                if (rule.ProductIds.Count == 0) throw new DomainException("يجب اختيار صنف واحد على الأقل للعرض.");
                if (!rule.FixedPrice.IsPositive) throw new DomainException("سعر العرض يجب أن يكون رقمًا صحيحًا أكبر من صفر.");
                break;
            case OfferKind.ItemPercent:
                if (rule.ProductIds.Count == 0) throw new DomainException("يجب اختيار صنف واحد على الأقل للعرض.");
                AssertPercent(rule.PercentBasisPoints);
                break;
            case OfferKind.CategoryPercent:
                if (rule.CategoryIds.Count == 0) throw new DomainException("اختار قسم واحد على الأقل للعرض.");
                AssertPercent(rule.PercentBasisPoints);
                break;
            case OfferKind.CategoryAmount:
                if (rule.CategoryIds.Count == 0) throw new DomainException("اختار قسم واحد على الأقل للعرض.");
                if (!rule.AmountOff.IsPositive) throw new DomainException("مبلغ الخصم لازم يكون أكبر من صفر.");
                break;
            case OfferKind.OrderGift:
                if (rule.GiftTiers.Count == 0) throw new DomainException("حدد هدية واحدة على الأقل: المبلغ والصنف والعدد.");
                foreach (var tier in rule.GiftTiers)
                {
                    if (!tier.Threshold.IsPositive) throw new DomainException("مبلغ الهدية لازم يكون أكبر من صفر.");
                    if (string.IsNullOrWhiteSpace(tier.ProductId)) throw new DomainException("اختار صنف الهدية.");
                    if (tier.Quantity is < 1 or > 99) throw new DomainException("عدد الهدية من 1 لـ 99.");
                }
                if (rule.GiftTiers.GroupBy(t => t.Threshold.Piastres).Any(g => g.Count() > 1))
                    throw new DomainException("كل هدية لازم يكون ليها مبلغ مختلف.");
                break;
            default:
                throw new DomainException("نوع العرض غير صالح.");
        }
        if (rule.StartTime.HasValue != rule.EndTime.HasValue) throw new DomainException("حدد ساعة البداية والنهاية الاتنين، أو سيبهم فاضيين.");
        if (rule.StartDate is { } from && rule.EndDate is { } to && to < from) throw new DomainException("تاريخ نهاية العرض قبل تاريخ بدايته.");
        if (rule.Stations is { } stations && stations.Any(s => !StationCodes.Contains(s))) throw new DomainException("الجهاز غير صالح.");
        foreach (var (limit, label) in new[] { (rule.MaxPerOrder, "في الطلب"), (rule.MaxPerDay, "في اليوم"), (rule.MaxTotal, "طول العرض") })
            if (limit is < 1) throw new DomainException($"الحد الأقصى {label} لازم يكون 1 أو أكتر، أو سيبه فاضي.");
    }

    private static void AssertPercent(int basisPoints)
    {
        if (basisPoints is < 1 or > 10_000) throw new DomainException("نسبة الخصم من 0.01% لـ 100%.");
    }

    /// <summary>What a manual discount may reach without a manager, as basis points of the items'
    /// value (owner, 2026-11-18: 10%). Settings can change it; 0 means every discount needs one.</summary>
    public const int DefaultMaxDiscountWithoutApprovalBasisPoints = 1000;

    /// <summary>True when a manual discount is above <paramref name="limitBasisPoints"/> of the items'
    /// value. Above it the cashier's own orders_discount is not enough: a manager's approval is spent
    /// on it, the same way a comp's is. Integer arithmetic, so 10% of 100.00 ج is exactly 10.00 ج.</summary>
    public static bool DiscountNeedsApproval(Money discount, Money subtotal, int limitBasisPoints)
    {
        if (!discount.IsPositive) return false;
        if (limitBasisPoints < 0) limitBasisPoints = 0;
        return checked(discount.Piastres * 10_000L) > checked(subtotal.Piastres * limitBasisPoints);
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
