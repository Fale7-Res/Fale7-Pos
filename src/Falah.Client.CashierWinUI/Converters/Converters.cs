using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Falah.Client.CashierWinUI.Converters;

public sealed class BoolToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

public sealed class InverseBoolToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Null/empty (a cleared error message, no product in the variant picker, etc.) collapses
/// the bound element; anything else shows it.</summary>
public sealed class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is null or (string and "") ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Money that may genuinely be unknown rather than zero. Costing and loss valuation both
/// treat a null cost as "we have no purchase history to value this against" - a real, displayable
/// state. Rendering it as 0.00 would assert the item was free, which is the one thing
/// CostingRules/PurchasingRules are careful never to claim.</summary>
public sealed class NullablePiastresToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is long l ? Services.CashInput.Format(l) + " ج" : "غير مقيّم";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Arabic label for a treasury movement type. The stored values are the system's own
/// snake_case vocabulary and must never be renamed (reports and the ledger group by them), but an
/// Arabic ledger should not show "shift_handover" to a manager. Unknown types fall through unchanged
/// so a movement type added server-side stays visible as itself rather than rendering blank.</summary>
public sealed class TreasuryMovementTypeLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => (value as string) switch
    {
        "sale_collection" => "تحصيل مبيعات",
        "shift_handover" => "تسليم وردية",
        "shift_opening_float" => "عهدة افتتاح وردية",
        "safe_drop" => "إيداع بالخزنة",
        "cash_in" => "إيداع نقدي بالدرج",
        "cash_out" => "سحب نقدي من الدرج",
        "driver_settlement" => "تسوية عهدة سائق",
        "owner_deposit" => "إيداع من المالك",
        "capital_injection" => "ضخ رأس مال",
        "other_external_inflow" => "تدفق خارجي آخر",
        "owner_withdrawal" => "سحب المالك",
        "transfer" => "تحويل بين الحسابات",
        "expense" or "expense_payment" => "مصروف",
        "refund" => "مرتجع",
        "purchase_payment" => "سداد مشتريات",
        "supplier_payment" => "سداد مورد",
        "reconciliation" => "تسوية",
        // Written by shift close, employee cash-outs, payroll and driver settlement - all of which
        // reached the ledger in English before these were added.
        "cash_overage" => "زيادة نقدية في الدرج",
        "cash_shortage" => "عجز نقدي في الدرج",
        "cash_in_transfer" => "تحويل إلى الدرج",
        "cash_out_transfer" => "تحويل من الدرج",
        "employee_advance" => "سلفة موظف",
        "employee_withdrawal" => "مسحوبات موظف",
        "employee_payment" => "صرف مستحقات موظف",
        "payroll_payment" => "صرف رواتب",
        "settlement_overage" => "زيادة في تسوية سائق",
        "settlement_shortage" => "عجز في تسوية سائق",
        null or "" => "",
        var other => other,
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>
/// Arabic labels for the system's own stored vocabularies - payment methods, user roles, employee
/// ledger entries, stock movements and print routes. The stored values are English identifiers that
/// reports group by and must never be renamed; these only change what a person reads. An unknown
/// value falls through unchanged, so something added server-side later is still visible as itself
/// rather than blank.
///
/// Shared converters rather than per-screen helpers: several screens used to bind these values raw,
/// while others had their own private maps, so the same word appeared in Arabic on one screen and in
/// English on the next.
/// </summary>
public sealed class PaymentMethodLabelConverter : IValueConverter
{
    public static string Label(string? method) => method?.ToLowerInvariant() switch
    {
        "cash" => "كاش",
        "card" => "فيزا",
        "instapay" => "إنستاباي",
        "wallet" => "محفظة",
        "multi" => "دفع مقسّم",
        null or "" => "",
        _ => method!,
    };

    public object Convert(object value, Type targetType, object parameter, string language) => Label(value as string);
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

public sealed class UserRoleLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => (value as string) switch
    {
        "owner" => "المالك",
        "manager" => "المدير",
        "supervisor" => "مشرف",
        "cashier" => "كاشير",
        null or "" => "",
        var other => other,
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Every value of the domain's LedgerEntryType.</summary>
public sealed class EmployeeLedgerTypeLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => (value as string) switch
    {
        "DailyWage" => "يومية",
        "Salary" => "راتب",
        "Advance" => "سلفة",
        "Withdrawal" => "مسحوبات",
        "Overtime" => "ساعات إضافية",
        "ExtraShift" => "وردية إضافية",
        "Deduction" => "خصم",
        "Bonus" => "مكافأة",
        "OtherEarning" => "مستحقات أخرى",
        "PayrollSettlement" => "تسوية رواتب",
        "Adjustment" => "تعديل",
        "Settlement" => "تسوية",
        null or "" => "",
        var other => other,
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Every value of the domain's StockMovementType, worded as the inventory screen already
/// words them.</summary>
public sealed class StockMovementTypeLabelConverter : IValueConverter
{
    public static string Label(string? type) => type switch
    {
        "PurchaseReceipt" => "استلام شراء",
        "ManualReceipt" => "إدخال يدوي",
        "Usage" => "استهلاك مبيعات",
        "Waste" => "هدر تحضير",
        "Spoilage" => "تلف وانتهاء صلاحية",
        "Breakage" => "كسر",
        "CountAdjustment" => "تسوية جرد",
        null or "" => "",
        _ => type!,
    };

    public object Convert(object value, Type targetType, object parameter, string language) => Label(value as string);
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Every value of the domain's PrintRoute.</summary>
public sealed class PrintRouteLabelConverter : IValueConverter
{
    public static string Label(string? route) => route switch
    {
        "Receipt" => "إيصال الكاشير",
        "CallCenter" => "إيصال مركز الاتصال",
        "Charcoal" => "محطة الفحم",
        "Grill" => "محطة الشواية",
        "DeliveryOrder" => "أوردر الدليفري",
        "DriverDispatch" => "تذكرة السائق",
        null or "" => "",
        _ => route!,
    };

    public object Convert(object value, Type targetType, object parameter, string language) => Label(value as string);
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>The "type" of a significant action depends on where the row came from - a treasury
/// movement type, an expense category (already Arabic), a stock movement type or a delivery event -
/// so this converter takes the whole row.</summary>
public sealed class SignificantActionTypeLabelConverter : IValueConverter
{
    private static readonly TreasuryMovementTypeLabelConverter Treasury = new();

    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is not Falah.Application.Reports.SignificantActionDto action ? "" : action.Category switch
        {
            "treasury" => Treasury.Convert(action.Type, targetType, parameter, language),
            "inventory" => StockMovementTypeLabelConverter.Label(action.Type),
            "delivery" => action.Type switch
            {
                "assignment" => "إسناد طلب لسائق",
                "reassignment" => "إعادة إسناد طلب",
                "ready" => "طلب جاهز للتوصيل",
                var other => other,
            },
            _ => action.Type,
        };

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Arabic label for a fixed treasury account id. The ids are the system's own stable
/// vocabulary (tre-main, tre-drawer, ...) and must never be renamed; this is presentation only.
/// A null/empty account is rendered as "خارج النظام" - a cash-lifecycle movement with no
/// counterpart account (a safe drop) is a real, meaningful case, not missing data.</summary>
public sealed class TreasuryAccountLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => (value as string) switch
    {
        "tre-main" => "الخزينة الرئيسية",
        "tre-drawer" => "درج الكاشير",
        "tre-bank" => "البنك",
        "tre-instapay" => "إنستاباي",
        "tre-wallet" => "المحفظة",
        null or "" => "خارج النظام",
        var other => other,
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Inventory quantity (milli-units) to a readable number - see Services.MilliUnits.</summary>
public sealed class MilliUnitsToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is long l ? Services.MilliUnits.Format(l) : "";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Shows the bound element only when a money amount is actually greater than zero - used
/// for figures that must be called out when present (a pending electronic transfer) but must not
/// leave an empty "0.00" banner sitting on the screen the rest of the time.</summary>
public sealed class PositiveAmountToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is long l && l > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Arabic label for a DeliveryStatus enum name. The API deliberately returns the enum's
/// own name (the state machine's vocabulary); the dispatcher reads Arabic. Unknown values fall
/// through unchanged rather than rendering blank, so a future status added server-side is visible
/// as itself instead of silently disappearing from the board.</summary>
public sealed class DeliveryStatusLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => (value as string) switch
    {
        // DeliveryStatus is persisted as nameof(...), i.e. PascalCase - unlike OrderStatus.
        "New" => "جديد",
        "Preparing" => "قيد التحضير",
        "Ready" => "جاهز للإسناد",
        "Assigned" => "مُسند",
        "HandedToDriver" => "مع السائق",
        "OutForDelivery" => "في الطريق",
        "Delivered" => "تم التسليم",
        "Failed" => "فشل التسليم",
        "Returned" => "مرتجع",
        "Settled" => "تمت التسوية",
        "Cancelled" => "ملغي",
        var other => other ?? "",
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Order.Status (OrderStatus: Pending/Preparing/Ready/Completed/Cancelled/Refunded) in
/// words - the Orders table used to bind this raw, showing an English enum name straight from the
/// wire (final pre-go-live localization pass: user-visible internal values must be translated at
/// the presentation layer, never by changing the wire/database value itself).</summary>
public sealed class OrderStatusLabelConverter : IValueConverter
{
    // Lower-cased first: OrderStatus travels the wire and sits in the database in OrderEnumMapping's
    // lower-case spelling ("preparing"), not as the enum's own name, so a PascalCase switch here
    // silently matched nothing and the table showed the raw English word.
    public object Convert(object value, Type targetType, object parameter, string language) => (value as string)?.ToLowerInvariant() switch
    {
        "pending" => "قيد الانتظار",
        "preparing" => "قيد التحضير",
        "ready" => "جاهز",
        "completed" => "مكتمل",
        "cancelled" => "ملغي",
        "refunded" => "مسترجع",
        _ => value as string ?? "",
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Order.OrderType (OrderEnumMapping's wire spelling: pickup/takeaway/delivery) in words.
/// Same presentation-layer-only rule as OrderStatusLabelConverter: the wire and database values are
/// untouched.</summary>
public sealed class OrderTypeLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => (value as string)?.ToLowerInvariant() switch
    {
        "pickup" => "استلام من المحل",
        "takeaway" => "تيك أواي",
        "delivery" => "توصيل",
        // Pre-rename spelling still present on historical Pickup orders (see OrderEnumMapping).
        "dine_in" or "dinein" => "استلام من المحل",
        _ => value as string ?? "",
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>One label converter for every OTHER internal status vocabulary this app shows raw in a
/// table cell (BusinessDayStatus, PayrollRunStatus, backup Status/VerificationStatus) - each has its
/// own enum with no collision across them, so one case-insensitive switch covers all of them rather
/// than one tiny single-purpose converter per screen. OrderStatusLabelConverter and
/// DeliveryStatusLabelConverter stay separate: their values (Pending/Ready/...) DO collide with each
/// other and with these, so merging would make one context's word wrong in another's.</summary>
public sealed class GeneralStatusLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => (value as string)?.ToLowerInvariant() switch
    {
        // BusinessDayStatus
        "open" => "مفتوح",
        "closing" => "قيد الإغلاق",
        "closed" => "مغلق",
        "reopened" => "أُعيد فتحه",
        // PayrollRunStatus
        "draft" => "مسودة",
        "calculated" => "تم الاحتساب",
        "reviewed" => "تمت المراجعة",
        "approved" => "معتمد",
        "paid" => "مدفوع",
        "cancelled" => "ملغي",
        // Backup Status / VerificationStatus
        "requested" => "قيد التنفيذ",
        "succeeded" => "نجحت",
        "failed" => "فشلت",
        "blocked" => "مرفوضة",
        // Backup VerificationStatus is literally "Valid"/"Invalid" (see VerifyBackupHandler) -
        // "verified" kept alongside for any older row written before that settled.
        "valid" => "سليمة",
        "invalid" => "تالفة",
        "verified" => "تم التحقق",
        // AuditEvent Severity
        "normal" => "عادي",
        "warning" => "تحذير",
        "critical" => "حرج",
        _ => value as string ?? "",
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>The exact inverse of NullToCollapsedConverter: shows the bound element only while the
/// value is null/empty. Used for placeholder text that must disappear the moment real input
/// arrives.</summary>
public sealed class EmptyStringToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is null or (string and "") ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>The header's shift-status dot: emerald when a shift is open, slate when closed -
/// matching AppShell.tsx's bg-emerald-400/bg-slate-500 exactly.</summary>
public sealed class ShiftDotBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Open = new(Color.FromArgb(0xFF, 0x34, 0xD3, 0x99));
    private static readonly SolidColorBrush Closed = new(Color.FromArgb(0xFF, 0x64, 0x74, 0x8B));

    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Open : Closed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Picks one of two named Brush resources based on a bool - used for selected-state
/// coloring (category tile, order-type tab) where WinUI has no per-item VisualState trigger tied to
/// a bound ViewModel flag the way WPF's ControlTemplate.Triggers would. Deliberately swaps a Brush,
/// not the whole Style object: rebinding Style itself on a realized GridView/ItemsControl container
/// forces template reapplication and was found to crash Microsoft.ui.xaml.dll natively (0xc000027b)
/// the first time it ran inside a virtualized container - swapping just Background/Foreground is the
/// well-established safe pattern. ConverterParameter is "SelectedBrushKey|UnselectedBrushKey".</summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var keys = ((string)parameter).Split('|');
        var key = value is true ? keys[0] : keys[1];
        return Services.ThemeResources.Get(key)!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Same idea as BoolToBrushConverter, but the "selected" test is a string equality
/// (OrderType == "pickup", PaymentMethod == "cash", etc.) rather than a plain bool.
/// ConverterParameter is "ExpectedValue|SelectedBrushKey|UnselectedBrushKey".</summary>
/// <summary>Picks one of two literal labels from ConverterParameter ("trueText|falseText"). Kept
/// distinct from BoolToBrushConverter, which resolves resource KEYS - passing label text to that one
/// would look up a resource that does not exist.</summary>
/// <summary>True when the bound value is present. InfoBar.IsOpen is a bool, so a nullable message
/// needs this rather than the Visibility converters the rest of the app uses.</summary>
/// <summary>Money the server could not determine. Renders "غير معروف" rather than a dash or a zero:
/// on the owner dashboard a blank invites the reader to assume nothing happened, and a zero invites
/// something far worse - reading an unpriced menu as a free one.</summary>
public sealed class MoneyOrUnknownConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is long piastres ? Services.CashInput.Format(piastres) + " ج" : "غير معروف";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>A ratio that exists, or the plain statement that it does not. A percentage of no sales,
/// or of an unknown cost, is not zero percent.</summary>
public sealed class PercentOrUnknownConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is double percent ? percent.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%" : "غير معروف";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Visible only when a count is greater than zero - for rows that exist to report a
/// problem and should disappear entirely when there is none.</summary>
public sealed class CountToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Just the file's name, for a table cell. A backup row showed its entire filesystem path,
/// which pushed the columns that matter (status, verification) off the side of the card to display
/// a directory that is the same on every row. The full path stays on the record itself for anyone
/// who needs to go and find the file.</summary>
public sealed class FileNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is string path && path.Length > 0 ? System.IO.Path.GetFileName(path) : "";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>A timestamp in the reader's own time, or a dash when the event never happened.</summary>
public sealed class LocalTimestampConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is DateTimeOffset at ? at.ToLocalTime().ToString("yyyy/MM/dd HH:mm") : "—";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>The colour stripe on an attention row. Severity is the view model's own vocabulary
/// (bad / warn / info), never a judgement made in XAML.</summary>
public sealed class SeverityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var key = (value as string) switch
        {
            "bad" => "DangerBrush",
            "warn" => "WarningBrush",
            "info" => "InfoBrush",
            _ => "SuccessBrush",
        };
        return Services.ThemeResources.Get(key)!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Green for up, red for down, muted for a change with no meaningful ratio. Expenses rising
/// is not coloured as failure here - the dashboard reports direction, it does not decide whether a
/// direction is good.</summary>
public sealed class DeltaStateToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var key = (value as string) switch
        {
            "up" => "SuccessBrush",
            "down" => "DangerBrush",
            _ => "TextMutedBrush",
        };
        return Services.ThemeResources.Get(key)!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>A 0..1 share as a bar width. Fixed track so the bars stay comparable to each other,
/// which is the only reason to draw them side by side at all.</summary>
public sealed class FractionToWidthConverter : IValueConverter
{
    private const double TrackWidth = 150;

    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is double fraction && fraction > 0 ? Math.Max(4, Math.Min(1, fraction) * TrackWidth) : 0d;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

public sealed class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is not null && value is not string { Length: 0 };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

public sealed class BoolToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var parts = ((string)parameter).Split('|');
        return value is true ? parts[0] : parts[1];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

public sealed class StringEqualsToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var parts = ((string)parameter).Split('|');
        var isMatch = string.Equals(value as string, parts[0], StringComparison.Ordinal);
        return Services.ThemeResources.Get(isMatch ? parts[1] : parts[2])!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Same shape as StringEqualsToBrushConverter for an int comparand - e.g. highlighting the
/// selected hours button (1-5) in the overtime picker.</summary>
public sealed class IntEqualsToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var parts = ((string)parameter).Split('|');
        var isMatch = value is int i && int.TryParse(parts[0], out var target) && i == target;
        return Services.ThemeResources.Get(isMatch ? parts[1] : parts[2])!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Visible only when the bound string equals ConverterParameter - e.g. the dine-in table
/// pickup details button only shows when OrderType == "pickup".</summary>
public sealed class StringEqualsToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        string.Equals(value as string, parameter as string, StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Shows a "has options" style badge only when a product has more than one variant.</summary>
public sealed class CountGreaterThanOneToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is System.Collections.ICollection { Count: > 1 } ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Piastres (long, e.g. 12550) to an Arabic-numeral "125.50 ج" display string. Accepts
/// long, long?, and int so it can bind directly to any DTO's *Piastres field without a cast in the
/// view model.</summary>
public sealed class PiastresToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value switch
    {
        long piastres => Format(piastres),
        int piastres => Format(piastres),
        null => "—",
        _ => "0.00 ج",
    };

    private static string Format(long piastres) => $"{piastres / 100m:0.00} ج";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Plain bool negation - IsEnabled="{Binding IsBusy, Converter={StaticResource
/// InverseBoolConverter}}" being the most common use, since WinUI has no built-in "!" binding
/// operator the way some XAML dialects do.</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value is not true;
    public object ConvertBack(object value, Type targetType, object parameter, string language) => value is not true;
}

/// <summary>Visible only when a bound collection is empty - the standard "empty state" trigger
/// used across every management list screen (no products, no drivers, no purchases...).</summary>
public sealed class EmptyCollectionToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is System.Collections.ICollection { Count: 0 } or null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Inverse of EmptyCollectionToVisibleConverter - visible only when the collection has at
/// least one item.</summary>
public sealed class NonEmptyCollectionToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is System.Collections.ICollection { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Cairo business-date presentation - every date this system shows an operator must read
/// as the restaurant's OWN business day, not whatever the client machine's local timezone happens
/// to be (a Call Center laptop and the Cashier host could theoretically differ). Accepts
/// DateTimeOffset, DateTime, or a "yyyy-MM-dd" string (the wire format most business-date fields
/// travel as), and renders Arabic day-name + day/month/year, matching how the reference app's own
/// UI always spelled out the business date rather than showing a bare ISO string.</summary>
public sealed class CairoBusinessDateConverter : IValueConverter
{
    private static readonly string[] ArabicDayNames =
        { "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        DateOnly? date = value switch
        {
            DateOnly d => d,
            DateTimeOffset dto => DateOnly.FromDateTime(dto.DateTime),
            DateTime dt => DateOnly.FromDateTime(dt),
            string s when DateOnly.TryParse(s, out var parsed) => parsed,
            _ => null,
        };
        if (date is not { } d2) return "—";
        var dayName = ArabicDayNames[(int)d2.DayOfWeek];
        return $"{dayName} {d2:dd/MM/yyyy}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Plain time-of-day presentation for timestamps that are about a moment (an audit event,
/// a print attempt), not a business date - "hh:mm ص/م" instead of a full date, matching the
/// reference app's own receipt/timeline formatting.</summary>
public sealed class TimeOfDayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value switch
    {
        DateTimeOffset dto => dto.ToLocalTime().ToString("hh:mm tt", new System.Globalization.CultureInfo("ar-EG")),
        DateTime dt => dt.ToString("hh:mm tt", new System.Globalization.CultureInfo("ar-EG")),
        _ => "—",
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Shell nav-item key ("cashier", "reports", "delivery"...) to a Segoe Fluent Icons glyph -
/// kept as a View-layer concern (not on ShellNavItem itself) since which glyph represents a module
/// is presentation, not navigation state.</summary>
public sealed class NavKeyToGlyphConverter : IValueConverter
{
    private static readonly Dictionary<string, string> Glyphs = new()
    {
        ["dashboard"] = "",
        ["cashier"] = "",
        ["orders"] = "",
        ["delivery"] = "",
        ["customers"] = "",
        ["reports"] = "",
        ["treasury"] = "",
        ["shifts"] = "",
        ["inventory"] = "",
        ["recipes"] = "",
        ["waste"] = "",
        ["purchasing"] = "",
        ["suppliers"] = "",
        ["employees"] = "",
        ["attendance"] = "",
        ["payroll"] = "",
        ["menu"] = "",
        ["users"] = "",
        ["audit"] = "",
        ["controlcenter"] = "",
        ["backup"] = "",
        ["settings"] = "",
    };

    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is string key && Glyphs.TryGetValue(key, out var glyph) ? glyph : "";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Status word (order/delivery/payroll/backup status strings like "Pending"/"Approved"/
/// "Failed") to one of the StatusPill brush resources - ConverterParameter is unused; the mapping
/// is a fixed, evidence-based vocabulary shared across every module rather than one bespoke
/// converter per screen, so a new status string picks a sensible bucket by keyword rather than
/// silently rendering unstyled.</summary>
public sealed class AttendanceStatusLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        (value as string)?.ToLowerInvariant() switch
        {
            "present" => "حاضر", "late" => "متأخر", "absent" => "غائب",
            "leave" => "إجازة", "missingfingerprint" => "بصمة ناقصة", _ => value ?? "—"
        };
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

public sealed class StatusToPillBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var s = (value as string ?? "").ToLowerInvariant();
        var key = s switch
        {
            _ when s.Contains("paid") || s.Contains("delivered") || s.Contains("approved") || s.Contains("settled")
                || s.Contains("completed") || s.Contains("posted") || s.Contains("succeeded") || s.Contains("active") => "SuccessPillBrush",
            _ when s.Contains("failed") || s.Contains("cancelled") || s.Contains("rejected") || s.Contains("blocked")
                || s.Contains("returned") || s.Contains("critical") => "DangerPillBrush",
            _ when s.Contains("pending") || s.Contains("warning") || s.Contains("requested") || s.Contains("draft")
                || s.Contains("reviewed") || s.Contains("calculated") => "WarningPillBrush",
            _ => "NeutralPillBrush",
        };
        return Services.ThemeResources.Get(key)!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>
/// Turns a product-photo URL into something an Image control can show, and keeps one bitmap per URL
/// for the life of the process.
///
/// The cache is the reason this is a converter rather than a plain binding. The catalogue rebuilds
/// its tiles on every keystroke in the search box and on every category tap; a fresh BitmapImage
/// each time would put the same picture back on the wire, and on a till with a slow link the cards
/// would visibly blank and refill while someone is typing. The URL carries the photo's version
/// stamp, so a replaced photo is a different key and a stale one is never served.
/// </summary>
public sealed class ImageUriToSourceConverter : IValueConverter
{
    private static readonly Dictionary<string, Microsoft.UI.Xaml.Media.Imaging.BitmapImage> Cache = new();

    /// <summary>Cards are around 250px wide; decoding at full size would hold several times the
    /// pixels the screen can use, multiplied by every product on the menu.</summary>
    private const int DecodeWidth = 320;

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not Uri uri) return null;
        var key = uri.AbsoluteUri;
        lock (Cache)
        {
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var image = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage { DecodePixelWidth = DecodeWidth, UriSource = uri };
            Cache[key] = image;
            return image;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Renders a missing value as a stated absence rather than an empty cell. An empty cell
/// reads as "nobody filled this in yet"; the words say what the blank actually means. The parameter
/// carries the text to use.</summary>
public sealed class NullToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var text = value as string;
        if (!string.IsNullOrWhiteSpace(text)) return text!;
        return parameter as string ?? "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>An empty-state panel's visibility, driven by the collection's own Count so it updates
/// when rows arrive. Binding the collection property itself never re-evaluates - filling an
/// ObservableCollection raises CollectionChanged, not PropertyChanged on the property holding it -
/// which left every empty state in this app sitting on top of the rows it said were not there.</summary>
public sealed class ZeroCountToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is int and 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
