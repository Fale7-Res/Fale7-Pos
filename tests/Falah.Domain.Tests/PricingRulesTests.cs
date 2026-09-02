using Falah.Domain.Common;
using Falah.Domain.Pricing;
using Xunit;

namespace Falah.Domain.Tests;

public class PricingRulesTests
{
    private static readonly TimeZoneInfo Cairo = BusinessClock.CairoTimeZone;

    private static PriceRule MakeRule(
        IReadOnlyList<DayOfWeek>? days = null, TimeOnly? start = null, TimeOnly? end = null, bool active = true) =>
        new("rule-1", "عرض الغداء", new[] { "prod-1" }, Money.FromEgp(50m), days, start, end, active);

    [Fact]
    public void Inactive_rule_is_never_active()
    {
        var rule = MakeRule(active: false);
        Assert.False(PricingRules.IsActiveAt(rule, new DateTimeOffset(2026, 3, 5, 10, 0, 0, TimeSpan.Zero), Cairo));
    }

    [Fact]
    public void Rule_with_no_time_window_is_always_active_on_an_allowed_day()
    {
        var rule = MakeRule();
        Assert.True(PricingRules.IsActiveAt(rule, new DateTimeOffset(2026, 3, 5, 3, 0, 0, TimeSpan.Zero), Cairo));
    }

    [Fact]
    public void Rule_restricted_to_specific_days_is_inactive_on_other_days()
    {
        // 2026-03-05 is a Thursday.
        var rule = MakeRule(days: new[] { DayOfWeek.Friday, DayOfWeek.Saturday });
        Assert.False(PricingRules.IsActiveAt(rule, new DateTimeOffset(2026, 3, 5, 10, 0, 0, TimeSpan.Zero), Cairo));
    }

    [Fact]
    public void Ordinary_same_day_window_is_active_only_inside_the_window()
    {
        var rule = MakeRule(start: new TimeOnly(12, 0), end: new TimeOnly(16, 0));
        // 10:00 Cairo (UTC+2) = 08:00 UTC -> before window.
        Assert.False(PricingRules.IsActiveAt(rule, new DateTimeOffset(2026, 3, 5, 8, 0, 0, TimeSpan.Zero), Cairo));
        // 14:00 Cairo = 12:00 UTC -> inside window.
        Assert.True(PricingRules.IsActiveAt(rule, new DateTimeOffset(2026, 3, 5, 12, 0, 0, TimeSpan.Zero), Cairo));
    }

    [Fact]
    public void A_window_crossing_midnight_is_treated_as_a_late_shift_window()
    {
        // 22:00 -> 02:00: active late at night and just after midnight, inactive mid-afternoon.
        var rule = MakeRule(start: new TimeOnly(22, 0), end: new TimeOnly(2, 0));
        // 23:00 Cairo = 21:00 UTC.
        Assert.True(PricingRules.IsActiveAt(rule, new DateTimeOffset(2026, 3, 5, 21, 0, 0, TimeSpan.Zero), Cairo));
        // 01:00 Cairo next day = 23:00 UTC same calendar day.
        Assert.True(PricingRules.IsActiveAt(rule, new DateTimeOffset(2026, 3, 5, 23, 0, 0, TimeSpan.Zero), Cairo));
        // 14:00 Cairo = 12:00 UTC -> outside the overnight window.
        Assert.False(PricingRules.IsActiveAt(rule, new DateTimeOffset(2026, 3, 5, 12, 0, 0, TimeSpan.Zero), Cairo));
    }

    [Fact]
    public void ValidateAppliedRule_rejects_a_rule_that_expired_since_the_chip_was_tapped()
    {
        var rule = MakeRule(start: new TimeOnly(12, 0), end: new TimeOnly(13, 0));
        var rules = new[] { rule };
        // Checkout happens at 20:00 Cairo, well after the offer window closed.
        var checkoutTime = new DateTimeOffset(2026, 3, 5, 18, 0, 0, TimeSpan.Zero);
        Assert.Null(PricingRules.ValidateAppliedRule("prod-1", "rule-1", rules, checkoutTime, Cairo));
    }

    [Fact]
    public void AssertCompAmount_rejects_a_comp_larger_than_the_order_value()
    {
        Assert.Throws<DomainException>(() =>
            PricingRules.AssertCompAmount(Money.FromEgp(200m), Money.FromEgp(100m)));
    }

    [Fact]
    public void AssertCompAmount_rejects_zero_or_negative_comp()
    {
        Assert.Throws<DomainException>(() => PricingRules.AssertCompAmount(Money.Zero, Money.FromEgp(100m)));
    }

    [Fact]
    public void AssertCompAmount_accepts_a_comp_equal_to_the_full_order_value()
    {
        PricingRules.AssertCompAmount(Money.FromEgp(100m), Money.FromEgp(100m));
    }
}
