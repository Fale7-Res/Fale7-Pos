using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Falah.Client.CashierWinUI.Services;

/// <summary>Converters return stable mutable brush proxies. Updating those proxies on the root's
/// ActualThemeChanged event updates existing bindings, without waiting for their model value to
/// change. Explicitly search merged theme dictionaries so an element-level theme does not fall
/// back to Application.RequestedTheme.</summary>
public static class ThemeResources
{
    private static FrameworkElement? _root;
    private static readonly Dictionary<string, SolidColorBrush> LiveBrushes = new();
    private static readonly Dictionary<(string Key, string Theme), object> Palette = new();

    public static void ObserveThemeRoot(FrameworkElement root)
    {
        if (!ReferenceEquals(root, _root))
        {
            if (_root is not null) _root.ActualThemeChanged -= OnThemeChanged;
            _root = root;
            _root.ActualThemeChanged += OnThemeChanged;
        }
        RefreshBrushes();
    }

    public static object? Get(string key)
    {
        var value = ResolveCurrent(key);
        if (value is not SolidColorBrush source) return value;
        if (!LiveBrushes.TryGetValue(key, out var proxy)) LiveBrushes[key] = proxy = new SolidColorBrush();
        proxy.Color = source.Color; proxy.Opacity = source.Opacity;
        return proxy;
    }

    private static void OnThemeChanged(FrameworkElement sender, object args) => RefreshBrushes();
    private static void RefreshBrushes()
    {
        foreach (var (key, proxy) in LiveBrushes.ToArray())
            if (ResolveCurrent(key) is SolidColorBrush source)
            { proxy.Color = source.Color; proxy.Opacity = source.Opacity; }
    }

    private static object? ResolveCurrent(string key)
    {
        var root = _root ?? App.MainAppWindow?.Content as FrameworkElement;
        var theme = root?.ActualTheme == ElementTheme.Dark ? "Dark" : "Light";
        if (Palette.TryGetValue((key, theme), out var cached)) return cached;
        var resources = global::Microsoft.UI.Xaml.Application.Current.Resources;
        // Complete the explicit theme search before invoking WinUI's fallback lookup: a lookup
        // on an unrelated merged dictionary may fall through to the application theme.
        if (!TryResolveThemed(resources, theme, key, out var value) && !resources.TryGetValue(key, out value)) return null;
        Palette[(key, theme)] = value!;
        return value;
    }

    private static bool TryResolveThemed(ResourceDictionary resources, string theme, string key, out object? value)
    {
        if (resources.ThemeDictionaries.TryGetValue(theme, out var selected) && selected is ResourceDictionary themed
            && themed.Keys.Any(k => Equals(k, key)) && themed.TryGetValue(key, out value)) return true;
        for (var i = resources.MergedDictionaries.Count - 1; i >= 0; i--)
            if (TryResolveThemed(resources.MergedDictionaries[i], theme, key, out value)) return true;
        value = null; return false;
    }
}
