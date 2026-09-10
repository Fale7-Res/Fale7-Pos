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

    /// <summary>The same range across several consecutive business dates, so a report can cover a
    /// week or a month without the caller stitching thirty single-day reports together. The
    /// boundaries stay Cairo midnights, which is what makes a monthly total add up to exactly the
    /// daily totals inside it rather than drifting by a few hours at each end.</summary>
    public static (DateTimeOffset Start, DateTimeOffset End) UtcRangeForBusinessDates(DateOnly from, DateOnly to)
    {
        if (to < from) (from, to) = (to, from);
        var (start, _) = UtcRangeForBusinessDate(from);
        var (_, end) = UtcRangeForBusinessDate(to);
        return (start, end);
    }
}

/// <summary>An inclusive span of Cairo business dates. A single day is a range whose ends are equal,
/// so every report takes one shape instead of two, and "today" never needs its own code path.</summary>
public readonly record struct BusinessDateRange(DateOnly From, DateOnly To)
{
    public static BusinessDateRange SingleDay(DateOnly date) => new(date, date);

    /// <summary>Builds the range a request actually asked for. `from` alone means that one day
    /// onward is NOT assumed - an open-ended report would silently change meaning as days pass, so
    /// a missing `to` means the same day.</summary>
    public static BusinessDateRange? Resolve(DateOnly? from, DateOnly? to, DateOnly? singleDate)
    {
        if (from is { } start) return new BusinessDateRange(start, to ?? start);
        if (to is { } end) return new BusinessDateRange(end, end);
        if (singleDate is { } day) return SingleDay(day);
        return null;
    }

    public int DayCount => To.DayNumber - From.DayNumber + 1;
    public bool IsSingleDay => From == To;

    public (DateTimeOffset Start, DateTimeOffset End) ToUtcRange() =>
        BusinessClock.UtcRangeForBusinessDates(From, To);

    public IEnumerable<DateOnly> Days()
    {
        for (var day = From; day <= To; day = day.AddDays(1)) yield return day;
    }
}
