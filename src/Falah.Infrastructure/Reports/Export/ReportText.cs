namespace Falah.Infrastructure.Reports.Export;

using System.Globalization;
using Falah.Application.Reports.Export;
using Falah.Domain.Common;

/// <summary>
/// The one place exported values become text, shared by every format so the PDF, Word, PNG and the Excel fallback
/// all print the same characters. Money is integer piastres formatted with integer arithmetic (no floating point):
/// "1,234.50 ج.م"; negative with a leading minus; unavailable as "غير متاح". Instants are shown in Cairo time.
/// </summary>
public static class ReportText
{
    public const string Unavailable = "غير متاح";
    public const string Currency = "ج.م";

    public static string Money(long piastres)
    {
        var negative = piastres < 0;
        var abs = negative ? -(decimal)piastres : piastres;
        var pounds = decimal.Truncate(abs / 100m);
        var cents = (int)(abs - pounds * 100m);
        return (negative ? "-" : "") + pounds.ToString("#,##0", CultureInfo.InvariantCulture) + "." + cents.ToString("00", CultureInfo.InvariantCulture) + " " + Currency;
    }

    /// <summary>Pounds as an exact invariant decimal string ("1234.5"), for numeric spreadsheet cells.</summary>
    public static string PoundsInvariant(long piastres) => (piastres / 100m).ToString(CultureInfo.InvariantCulture);

    public static string Count(long count) => count.ToString("#,##0", CultureInfo.InvariantCulture);

    /// <summary>A date or time printed inside Arabic text is wrapped in a left-to-right embedding (LRE … PDF). Without it the
    /// bidi algorithm reorders it: "2026-11-14" came out "14-11-2026", and "2026-11-14 12:00" came out "12:00 2026-11-14"
    /// on rendered pages. Invisible; spreadsheet dates are real date cells and are not affected.</summary>
    private const string Lre = "‪";
    private const string Pdf = "‬";

    public static string Day(DateOnly date) => Lre + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + Pdf;

    public static DateTime Cairo(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, BusinessClock.CairoTimeZone).DateTime;

    public static string At(DateTimeOffset instant) => Lre + Cairo(instant).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + Pdf;

    public static string Cell(ReportCell cell) => cell.Kind switch
    {
        ReportCellKind.Money => cell.Number is { } p ? Money(p) : Unavailable,
        ReportCellKind.Count => cell.Number is { } n ? Count(n) : Unavailable,
        ReportCellKind.Date => cell.Date is { } d ? Day(d) : "",
        ReportCellKind.DateTime => cell.Instant is { } i ? At(i) : "",
        _ => cell.Text ?? "",
    };

    public static string Status(string status) => status switch
    {
        "estimate" => "تقديري",
        "partial" => "جزئي",
        "unavailable" => "غير متاح",
        _ => "فعلي",
    };

    public static string Period(DateOnly from, DateOnly to) => from == to ? Day(from) : $"من {Day(from)} إلى {Day(to)}";

    public static string Account(string? accountId) => accountId switch
    {
        "tre-main" => "الخزينة الرئيسية",
        "tre-drawer" => "درج الكاشير",
        "tre-bank" => "البنك",
        "tre-instapay" => "إنستاباي",
        "tre-wallet" => "المحفظة",
        null or "" => "خارج النظام",
        _ => accountId,
    };

    public static string PaymentMethod(string method) => method switch
    {
        "cash" => "نقدي",
        "card" => "بطاقة",
        "instapay" => "إنستاباي",
        "wallet" => "محفظة",
        "split" => "مقسّم",
        _ => method,
    };

    public static string MovementType(string type) => type switch
    {
        "sale_collection" or "sale" => "تحصيل مبيعات",
        "refund" => "مرتجع",
        "void_return" => "إرجاع نقدية طلب ملغي",
        "expense" or "expense_payment" => "مصروف",
        "expense_correction" => "تصحيح مصروف مسجل بالخطأ",
        "expense_refund" => "مبلغ مصروف عاد نقدًا",
        "movement_correction" => "تصحيح حركة مسجلة بالخطأ",
        "owner_deposit" => "إيداع من المالك",
        "capital_injection" => "ضخ رأس مال",
        "other_external_inflow" => "تدفق خارجي",
        "owner_withdrawal" => "سحب المالك",
        "transfer" or "treasury_transfer" => "تحويل داخلي",
        "safe_drop" => "توريد للخزنة",
        "shift_handover" => "تسليم وردية",
        "shift_opening_float" => "عهدة افتتاح وردية",
        "cash_in" => "إيداع نقدية بالدرج",
        "cash_out" => "سحب نقدية من الدرج",
        "driver_settlement" => "توريد سائق",
        "driver_fee_payout" => "صرف أجر سائق",
        "supplier_payment" or "purchase_payment" or "purchase" => "سداد مورد",
        "payroll_payment" or "employee_payment" => "صرف رواتب",
        "employee_advance" => "سلفة موظف",
        "employee_withdrawal" => "سحب موظف من رصيده",
        "cash_shortage" => "عجز وردية",
        "cash_overage" => "زيادة وردية",
        "opening_float_shortfall" => "عجز عهدة الافتتاح",
        "opening_float_overage" => "زيادة عهدة الافتتاح",
        _ => type,
    };

    /// <summary>A file name made only of ASCII letters, digits and dashes: nothing a caller can steer into a path.</summary>
    public static string SafeFileName(string reportKey, DateOnly from, DateOnly to, string extension)
    {
        var key = new string(reportKey.Where(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-').ToArray());
        if (key.Length == 0) key = "report";
        var ext = new string(extension.Where(char.IsAsciiLetterOrDigit).ToArray());
        return $"falah-{key}-{from:yyyyMMdd}-{to:yyyyMMdd}.{ext}";
    }
}
