using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Falah.Client.Cashier.Converters;

public sealed class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is null || (value is string s && string.IsNullOrWhiteSpace(s))) ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class BoolToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class InverseBoolToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Picks a busy/idle label pair by the ConverterParameter, so one converter instance can
/// serve every "this button is mid-request" label in the app instead of one per screen.</summary>
public sealed class BoolToLoginLabelConverter : IValueConverter
{
    private static readonly Dictionary<string, (string Busy, string Idle)> Labels = new()
    {
        ["login"] = ("جارٍ الدخول...", "دخول"),
        ["checkout"] = ("جارٍ تسجيل العملية...", "تأكيد الدفع"),
        ["confirm"] = ("جارٍ التأكيد...", "تأكيد وصول المبلغ"),
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var pair = Labels.GetValueOrDefault(parameter as string ?? "login", Labels["login"]);
        return value is true ? pair.Busy : pair.Idle;
    }
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => !(value is true);
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => !(value is true);
}

public sealed class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i && i == 0 ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class CashOnlyVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string) == "cash" ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class OnlineStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "متصل بالسيرفر" : "غير متصل بالسيرفر";
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class OnlineStatusBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? System.Windows.Media.Brushes.MediumSeaGreen : System.Windows.Media.Brushes.Crimson;
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Compares two bound values for equality - used to drive a "this tile is the currently
/// selected one" DataTrigger (category, order type, payment method) without a dedicated
/// IsSelected property on every item.</summary>
/// <summary>Packages a cart line and a fixed note phrase into one CommandParameter, so one shared
/// "append a quick note" command can serve every note chip without a dedicated command per chip
/// text.</summary>
public sealed class NoteAppendPairConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        (values[0] as Falah.Client.Cashier.Models.CartLine, values[1] as string);
    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class EqualsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Length == 2 && Equals(values[0], values[1]);
    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class PiastresToEgpConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is long p ? $"{p / 100m:0.00} ج" : "0.00 ج";
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
