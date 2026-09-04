namespace Falah.Domain.Common;

/// <summary>
/// The restaurant's "business day" is the calendar date in Cairo local time - ported from
/// lib/time-policy.mjs in the current web app so historical dates line up exactly when both
/// systems are read side by side during the pilot period. There is no separate late-night
/// cutoff in the current system (e.g. a 4am rollover); midnight Cairo time is the boundary.
/// </summary>
public static class BusinessClock
{
    public static readonly TimeZoneInfo CairoTimeZone = ResolveCairoTimeZone();

    private static TimeZoneInfo ResolveCairoTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"); }
        catch (TimeZoneNotFoundException)
        {
            // Windows-only ID fallback for machines without the IANA database.
            return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }
    }

    public static DateOnly BusinessDate(DateTimeOffset instantUtc)
    {
        var local = TimeZoneInfo.ConvertTime(instantUtc, CairoTimeZone);
        return DateOnly.FromDateTime(local.DateTime);
    }

    public static DateOnly Today() => BusinessDate(DateTimeOffset.UtcNow);

    /// <summary>The [Start, End) UTC instant range covering one Cairo calendar day - the reverse of
    /// BusinessDate. Lets a query filter timestamped rows (Orders.CreatedAtUtc, etc.) by business
    /// date directly in SQL (a plain range comparison, translatable by EF) instead of loading every
    /// row into memory just to re-derive its business date client-side.</summary>
    public static (DateTimeOffset Start, DateTimeOffset End) UtcRangeForBusinessDate(DateOnly date)
    {
        var localMidnight = date.ToDateTime(TimeOnly.MinValue);
        var start = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localMidnight, DateTimeKind.Unspecified), CairoTimeZone);
        var end = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localMidnight.AddDays(1), DateTimeKind.Unspecified), CairoTimeZone);
        return (new DateTimeOffset(start, TimeSpan.Zero), new DateTimeOffset(end, TimeSpan.Zero));
    }
}
