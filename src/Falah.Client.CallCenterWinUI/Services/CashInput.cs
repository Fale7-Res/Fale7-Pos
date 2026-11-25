using System.Globalization;

namespace Falah.Client.CallCenterWinUI.Services;

public static class CashInput
{
    public static bool TryParse(string? text, out long piastres)
    {
        piastres = 0;
        var normalized = string.Concat((text ?? "").Trim().Select(c =>
            c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') :
            c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') : c == '٫' ? '.' : c));
        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var egp)
            || egp < 0 || egp > long.MaxValue / 100m || decimal.Round(egp, 2) != egp) return false;
        piastres = (long)(egp * 100m);
        return true;
    }

    /// <summary>A whole count (notes, coins), read with the same Arabic-digit handling as amounts.</summary>
    public static bool TryParseCount(string? text, out long count)
    {
        var normalized = string.Concat((text ?? "").Trim().Select(c =>
            c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') :
            c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') : c));
        return long.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out count);
    }

    public static string Format(long piastres) => (piastres / 100m).ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Audit 2026-11-25 U6: an amount for reading - thousands separated ("5,000.00"). Never for a value a
    /// cashier edits: Format stays the input form, which TryParse reads back.</summary>
    public static string Display(long piastres) => (piastres / 100m).ToString("#,##0.00", CultureInfo.InvariantCulture);
}
