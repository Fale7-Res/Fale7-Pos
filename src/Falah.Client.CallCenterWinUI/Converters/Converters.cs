using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Falah.Client.CallCenterWinUI.Converters;

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
        ["cashier"] = "",
        ["orders"] = "",
        ["delivery"] = "",
        ["customers"] = "",
        ["reports"] = "",
        ["treasury"] = "",
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
        return global::Microsoft.UI.Xaml.Application.Current.Resources[key];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>
/// Turns a product-photo URL into something an Image control can show, and keeps one bitmap per URL
/// for the life of the process. Mirrors the cashier client's converter of the same name - the two
/// clients render the same menu, and a photo that appears on one till and not the other would read
/// as a fault rather than a difference.
///
/// The URL carries the photo's version stamp, so a replaced photo is a different key and a stale
/// one is never served.
/// </summary>
public sealed class ImageUriToSourceConverter : IValueConverter
{
    private static readonly Dictionary<string, Microsoft.UI.Xaml.Media.Imaging.BitmapImage> Cache = new();

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
