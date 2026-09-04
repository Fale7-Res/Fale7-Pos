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
        return global::Microsoft.UI.Xaml.Application.Current.Resources[key];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Same idea as BoolToBrushConverter, but the "selected" test is a string equality
/// (OrderType == "dine_in", PaymentMethod == "cash", etc.) rather than a plain bool.
/// ConverterParameter is "ExpectedValue|SelectedBrushKey|UnselectedBrushKey".</summary>
public sealed class StringEqualsToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var parts = ((string)parameter).Split('|');
        var isMatch = string.Equals(value as string, parts[0], StringComparison.Ordinal);
        return global::Microsoft.UI.Xaml.Application.Current.Resources[isMatch ? parts[1] : parts[2]];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Visible only when the bound string equals ConverterParameter - e.g. the dine-in table
/// number box only shows when OrderType == "dine_in".</summary>
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

/// <summary>Piastres (long, e.g. 12550) to an Arabic-numeral "125.50 ج" display string.</summary>
public sealed class PiastresToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is long piastres ? $"{piastres / 100m:0.00} ج" : "0.00 ج";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
