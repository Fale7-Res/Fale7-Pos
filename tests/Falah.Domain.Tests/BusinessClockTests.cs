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
        // 2026-03-05 22:30 UTC = 2026-03-06 00:30 Cairo (UTC+2, no DST in Egypt) - a classic
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
        // Cairo is UTC+2 (no DST) - 2026-03-06 00:00 Cairo = 2026-03-05 22:00 UTC.
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
}
