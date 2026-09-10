using System.Globalization;

namespace Falah.Client.CashierWinUI.Services;

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

    public static string Format(long piastres) => (piastres / 100m).ToString("0.00", CultureInfo.InvariantCulture);
}
