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
}
