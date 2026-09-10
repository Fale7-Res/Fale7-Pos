using System.Globalization;

namespace Falah.Client.CashierWinUI.Services;

/// <summary>
/// Inventory quantities travel as milli-units (1000 = one base unit of whatever the item's own Unit
/// is - kg, litre, piece). This is the quantity equivalent of CashInput: one place that parses and
/// formats them, accepting Arabic-Indic and Persian digits and the Arabic decimal separator, so a
/// storekeeper typing "١٫٥" gets 1500 milli-units rather than a validation error.
/// </summary>
public static class MilliUnits
{
    private const long PerUnit = 1000;

    public static bool TryParse(string? text, out long milliUnits)
    {
        milliUnits = 0;
        var normalized = string.Concat((text ?? "").Trim().Select(c =>
            c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') :
            c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') : c == '٫' ? '.' : c));
        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var units)
            || units < 0 || units > long.MaxValue / PerUnit || decimal.Round(units, 3) != units) return false;
        milliUnits = (long)(units * PerUnit);
        return true;
    }

    /// <summary>Trailing zeros trimmed: a whole 2 kg reads "2", not "2.000", while 1.5 keeps its
    /// meaningful digit. Stock quantities are read at a glance down a long column.</summary>
    public static string Format(long milliUnits) =>
        (milliUnits / (decimal)PerUnit).ToString("0.###", CultureInfo.InvariantCulture);
}
