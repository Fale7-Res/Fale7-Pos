using Microsoft.UI.Xaml;

namespace Falah.Client.CashierWinUI.Services;

/// <summary>
/// Looks a resource up in whichever of Colors.xaml's Light/Dark ThemeDictionaries the window is
/// actually showing right now - for the handful of converters that resolve a resource BY NAME in
/// C# (BoolToBrushConverter and its siblings) rather than through a `{ThemeResource}` binding.
///
/// `{ThemeResource}` in XAML is theme-correct on its own: WinUI resolves it against the element's
/// own ActualTheme and re-resolves it automatically when that theme changes. A bare
/// `Application.Current.Resources[key]` indexer call from code, which is what these converters used
/// to do, has no element to ask and is not guaranteed to track the window's live per-element theme -
/// so it is resolved explicitly here instead, against the one window this app actually has.
/// </summary>
public static class ThemeResources
{
    public static object? Get(string key)
    {
        var app = global::Microsoft.UI.Xaml.Application.Current;
        var root = App.MainAppWindow?.Content as FrameworkElement;
        var themeName = root?.ActualTheme == ElementTheme.Dark ? "Dark" : "Light";

        if (app.Resources.ThemeDictionaries.TryGetValue(themeName, out var dictionary)
            && dictionary is ResourceDictionary themed
            && themed.TryGetValue(key, out var themedValue))
        {
            return themedValue;
        }

        // Falls through to the flat (non-themed) resources - WhiteBrush, TransparentBrush, the
        // gradients - for any key that is not one of the themed ones above.
        return app.Resources.TryGetValue(key, out var flatValue) ? flatValue : null;
    }
}
