using System.Globalization;
using Falah.Domain.Common;

namespace Falah.Domain.Printing;

/// <summary>
/// How every printed paper writes money, dates, times and codes - one vocabulary, so the eleven
/// papers read as one restaurant's paperwork (paper redesign, 2026-11-17).
///
/// Everything here is culture-invariant on purpose: the till may run under ar-EG, whose number
/// format would print Arabic-Indic digits and an Arabic thousands separator on some papers and not
/// others. The left-to-right marks (U+200E) keep digit runs in reading order when they follow
/// Arabic text - see FormatCairoDate's history in PrintRules for the reversal they prevent.
/// </summary>
public static class TicketFormat
{
    private const string Ltr = "‎";

    /// <summary>"12,500 ج"; piastres only when there are any ("25.50 ج").</summary>
    public static string Amount(Money amount)
    {
        var format = amount.Piastres % 100 == 0 ? "#,0" : "#,0.00";
        return amount.Egp.ToString(format, CultureInfo.InvariantCulture) + " ج";
    }

    public static string Amount(long piastres) => Amount(Money.FromPiastres(piastres));

    /// <summary>Cairo calendar date, dd-MM-yyyy.</summary>
    public static string Date(DateTimeOffset utc) =>
        Ltr + Cairo(utc).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    /// <summary>Cairo 12-hour wall clock with an explicit Arabic meridiem (ص / م).</summary>
    public static string Time(DateTimeOffset utc)
    {
        var local = Cairo(utc);
        return Ltr + local.ToString("hh:mm", CultureInfo.InvariantCulture) + " " + (local.Hour < 12 ? "ص" : "م");
    }

    /// <summary>Date then time on one line. The right-to-left mark (U+200F) between them keeps the two
    /// digit runs apart; without it they merged into one left-to-right run and the Arabic meridiem
    /// printed against the date instead of the time (final review, 2026-11-17).</summary>
    public static string DateTime(DateTimeOffset utc) => Date(utc) + " ‏ " + Time(utc);

    /// <summary>An order number or reference inside Arabic text, kept as its own left-to-right run so
    /// "(#0281)" never prints as "(0281#)".</summary>
    public static string Code(string text) => Ltr + text + Ltr;

    private static DateTimeOffset Cairo(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, BusinessClock.CairoTimeZone);
}
