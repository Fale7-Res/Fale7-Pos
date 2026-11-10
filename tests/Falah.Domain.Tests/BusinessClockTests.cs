using Falah.Domain.Common;
using Xunit;

namespace Falah.Domain.Tests;

public class BusinessClockTests
{
    [Fact]
    public void Cairo_timezone_resolves_on_this_machine()
    {
        Assert.NotNull(BusinessClock.CairoTimeZone);
    }

    [Fact]
    public void Late_night_utc_instant_still_falls_on_the_correct_cairo_calendar_date()
    {
        // 2026-03-05 22:30 UTC = 2026-03-06 00:30 Cairo (UTC+2 in winter) - a classic
        // date-boundary case where naive UTC-date logic would report the wrong business day.
        var instant = new DateTimeOffset(2026, 3, 5, 22, 30, 0, TimeSpan.Zero);
        var businessDate = BusinessClock.BusinessDate(instant);
        Assert.Equal(new DateOnly(2026, 3, 6), businessDate);
    }

    [Fact]
    public void Just_before_midnight_cairo_is_still_the_earlier_business_date()
    {
        // 2026-03-05 21:59 UTC = 2026-03-05 23:59 Cairo.
        var instant = new DateTimeOffset(2026, 3, 5, 21, 59, 0, TimeSpan.Zero);
        Assert.Equal(new DateOnly(2026, 3, 5), BusinessClock.BusinessDate(instant));
    }

    [Fact]
    public void The_utc_range_for_a_business_date_starts_at_the_previous_days_22_00_utc()
    {
        // Cairo is UTC+2 in winter - 2026-03-06 00:00 Cairo = 2026-03-05 22:00 UTC.
        var (start, end) = BusinessClock.UtcRangeForBusinessDate(new DateOnly(2026, 3, 6));
        Assert.Equal(new DateTimeOffset(2026, 3, 5, 22, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 3, 6, 22, 0, 0, TimeSpan.Zero), end);
    }

    [Fact]
    public void Every_instant_at_or_after_the_range_start_and_before_the_end_reports_that_business_date()
    {
        var date = new DateOnly(2026, 3, 6);
        var (start, end) = BusinessClock.UtcRangeForBusinessDate(date);

        Assert.Equal(date, BusinessClock.BusinessDate(start));
        Assert.Equal(date, BusinessClock.BusinessDate(end.AddMinutes(-1)));
        Assert.NotEqual(date, BusinessClock.BusinessDate(end));
        Assert.NotEqual(date, BusinessClock.BusinessDate(start.AddMinutes(-1)));
    }

    // Egypt restored daylight saving in 2023: clocks jump from 00:00 to 01:00 on the last Friday of
    // April, so that Friday's local midnight never happens, and fall back from 24:00 to 23:00 on the
    // last Thursday of October. A business date must still have a range on both days (FIN-0028).
    [Theory]
    [InlineData("2026-04-24", "2026-04-23T22:00:00Z", "2026-04-24T21:00:00Z", 23)]
    [InlineData("2027-04-30", "2027-04-29T22:00:00Z", "2027-04-30T21:00:00Z", 23)]
    [InlineData("2026-04-23", "2026-04-22T22:00:00Z", "2026-04-23T22:00:00Z", 24)]
    [InlineData("2027-04-29", "2027-04-28T22:00:00Z", "2027-04-29T22:00:00Z", 24)]
    public void Business_date_range_on_cairo_dst_start_does_not_throw(string date, string expectedStart, string expectedEnd, int hours)
    {
        var day = DateOnly.Parse(date);

        var (start, end) = BusinessClock.UtcRangeForBusinessDate(day);

        Assert.Equal(DateTimeOffset.Parse(expectedStart), start);
        Assert.Equal(DateTimeOffset.Parse(expectedEnd), end);
        Assert.Equal(TimeSpan.FromHours(hours), end - start);
        Assert.Equal(day, BusinessClock.BusinessDate(start));
        Assert.Equal(day, BusinessClock.BusinessDate(end.AddTicks(-1)));
        Assert.NotEqual(day, BusinessClock.BusinessDate(end));
    }

    [Fact]
    public void Business_date_ranges_touching_dst_start_do_not_throw()
    {
        // Starts on the DST-start Friday itself (the 14-day best-sellers window on 2027-05-13).
        var range = new BusinessDateRange(new DateOnly(2027, 4, 30), new DateOnly(2027, 5, 13));

        var (start, end) = range.ToUtcRange();

        Assert.Equal(new DateTimeOffset(2027, 4, 29, 22, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2027, 5, 13, 21, 0, 0, TimeSpan.Zero), end);
        Assert.Equal((start, end), BusinessClock.UtcRangeForBusinessDates(range.From, range.To));
    }

    [Fact]
    public void Consecutive_business_date_ranges_tile_time_with_no_gap_or_overlap()
    {
        for (var day = new DateOnly(2025, 1, 1); day <= new DateOnly(2028, 12, 31); day = day.AddDays(1))
        {
            var (start, end) = BusinessClock.UtcRangeForBusinessDate(day);
            var (nextStart, _) = BusinessClock.UtcRangeForBusinessDate(day.AddDays(1));
            Assert.True(end == nextStart, $"{day}: ends {end:O} but {day.AddDays(1)} starts {nextStart:O}");
            Assert.InRange((end - start).TotalHours, 23, 25);
            Assert.Equal(day, BusinessClock.BusinessDate(start));
            Assert.Equal(day, BusinessClock.BusinessDate(end.AddTicks(-1)));
        }
    }

    [Theory]
    [InlineData("2026-10-29")]
    [InlineData("2027-10-28")]
    public void Business_date_range_on_dst_end_is_contiguous(string date)
    {
        var day = DateOnly.Parse(date);

        var (start, end) = BusinessClock.UtcRangeForBusinessDate(day);

        // The repeated 23:00 hour happens twice on that Thursday, so the day is 25 hours long.
        Assert.Equal(TimeSpan.FromHours(25), end - start);
        Assert.Equal(day, BusinessClock.BusinessDate(end.AddTicks(-1)));
    }

    [Fact]
    public void A_summer_business_date_starts_at_21_00_utc()
    {
        var (start, end) = BusinessClock.UtcRangeForBusinessDate(new DateOnly(2026, 7, 15));
        Assert.Equal(new DateTimeOffset(2026, 7, 14, 21, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 7, 15, 21, 0, 0, TimeSpan.Zero), end);
    }

    [Fact]
    public void Cairo_timezone_knows_egypts_daylight_saving_rule()
    {
        // If this fails the server's Windows lacks Egypt's 2023 rule; every summer report would be
        // an hour off. Install the Windows time-zone update before go-live.
        Assert.Equal(TimeSpan.FromHours(3), BusinessClock.CairoTimeZone.GetUtcOffset(new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero)));
        Assert.Equal(TimeSpan.FromHours(2), BusinessClock.CairoTimeZone.GetUtcOffset(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero)));
    }
}
