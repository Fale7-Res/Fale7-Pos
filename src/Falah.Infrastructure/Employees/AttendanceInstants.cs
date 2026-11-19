namespace Falah.Infrastructure.Employees;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence.Entities;
using System.Globalization;

/// <summary>Legacy biometric clock strings are UTC, paired with a Cairo business date.
/// Manual/imported wall clocks are left unchanged; no historical pay is rewritten.</summary>
internal static class AttendanceInstants
{
    internal static DateTimeOffset? CheckIn(AttendanceRecordEntity record)
    {
        if (record.CheckInAtUtc is { } instant) return instant;
        if (record.Source != "biometric" || !TimeOnly.TryParseExact(record.CheckIn, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var clock)) return null;
        foreach (var date in new[] { record.Date, record.Date.AddDays(-1) })
        {
            var candidate = new DateTimeOffset(date.ToDateTime(clock, DateTimeKind.Utc));
            if (BusinessClock.BusinessDate(candidate) == record.Date) return candidate;
        }
        return null;
    }
    internal static DateTimeOffset? CheckOut(AttendanceRecordEntity record)
    {
        if (record.CheckOutAtUtc is { } instant) return instant;
        if (CheckIn(record) is not { } checkIn || !TimeOnly.TryParseExact(record.CheckOut, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var clock)) return null;
        var candidate = new DateTimeOffset(checkIn.UtcDateTime.Date.Add(clock.ToTimeSpan()), TimeSpan.Zero);
        return candidate < checkIn ? candidate.AddDays(1) : candidate;
    }
    internal static string? Display(DateTimeOffset? instant, string? fallback) => instant is { } value
        ? TimeZoneInfo.ConvertTime(value, BusinessClock.CairoTimeZone).ToString("HH:mm", CultureInfo.InvariantCulture) : fallback;
}
