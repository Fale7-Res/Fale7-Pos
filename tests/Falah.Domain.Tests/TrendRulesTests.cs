namespace Falah.Domain.Tests;

using Falah.Domain.Common;
using Falah.Domain.Reports;
using Xunit;

/// <summary>
/// The comparison an owner reads at a glance, so its two failure modes both matter: comparing a
/// Friday against a Thursday, and announcing a huge percentage that describes nothing.
/// </summary>
public class TrendRulesTests
{
    [Fact]
    public void The_comparable_period_is_the_same_span_one_week_earlier()
    {
        var current = new BusinessDateRange(new DateOnly(2026, 9, 4), new DateOnly(2026, 9, 10));

        var previous = TrendRules.ComparablePeriod(current);

        Assert.Equal(new DateOnly(2026, 8, 28), previous.From);
        Assert.Equal(new DateOnly(2026, 9, 3), previous.To);
        Assert.Equal(current.DayCount, previous.DayCount);
    }

    [Fact]
    public void A_single_day_is_compared_with_the_same_weekday_last_week()
    {
        // A Friday against a Friday. Against yesterday it would be a Friday against a Thursday,
        // which in a restaurant is not a comparison at all.
        var friday = BusinessDateRange.SingleDay(new DateOnly(2026, 9, 11));

        var previous = TrendRules.ComparablePeriod(friday);

        Assert.Equal(new DateOnly(2026, 9, 4), previous.From);
        Assert.Equal(friday.From.DayOfWeek, previous.From.DayOfWeek);
    }

    [Fact]
    public void A_rise_reports_the_amount_and_the_ratio()
    {
        var change = TrendRules.Compare(currentPiastres: 1_245_000, previousPiastres: 1_112_000);

        Assert.Equal(133_000, change.DeltaPiastres);
        Assert.Equal(12.0d, change.Percent);
    }

    [Fact]
    public void A_fall_is_reported_as_a_negative_change_not_an_absolute_one()
    {
        var change = TrendRules.Compare(currentPiastres: 80_000, previousPiastres: 100_000);

        Assert.Equal(-20_000, change.DeltaPiastres);
        Assert.Equal(-20d, change.Percent);
    }

    [Fact]
    public void Growth_from_zero_has_no_percentage()
    {
        // The restaurant was closed last Friday. This Friday took 5,000 - a real increase, but
        // "+100%" or "+∞" would be the largest number on the dashboard and would mean nothing.
        var change = TrendRules.Compare(currentPiastres: 500_000, previousPiastres: 0);

        Assert.Equal(500_000, change.DeltaPiastres);
        Assert.Null(change.Percent);
    }

    [Fact]
    public void A_fall_to_zero_still_has_a_percentage_because_the_earlier_figure_exists()
    {
        var change = TrendRules.Compare(currentPiastres: 0, previousPiastres: 400_000);

        Assert.Equal(-400_000, change.DeltaPiastres);
        Assert.Equal(-100d, change.Percent);
    }

    [Fact]
    public void No_change_is_zero_percent_not_unknown()
    {
        var change = TrendRules.Compare(currentPiastres: 250_000, previousPiastres: 250_000);

        Assert.Equal(0, change.DeltaPiastres);
        Assert.Equal(0d, change.Percent);
    }

    [Fact]
    public void Both_periods_at_zero_report_no_ratio()
    {
        var change = TrendRules.Compare(currentPiastres: 0, previousPiastres: 0);

        Assert.Equal(0, change.DeltaPiastres);
        Assert.Null(change.Percent);
    }
}
