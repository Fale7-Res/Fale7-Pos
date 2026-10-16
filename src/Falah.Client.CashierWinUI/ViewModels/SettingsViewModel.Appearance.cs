using CommunityToolkit.Mvvm.ComponentModel;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Theme is a per-machine appearance preference, not a restaurant setting - it never goes through
/// SettingsDto or the server. Server settings on this screen are gated on `settings_manage` and
/// only take effect after "حفظ التغييرات" is pressed; theme applies immediately and is never lost
/// to that gate, matching how a desktop app's own appearance choice normally behaves.
/// </summary>
public partial class SettingsViewModel
{
    public sealed record ThemeOption(AppTheme Value, string Label);

    public IReadOnlyList<ThemeOption> ThemeOptions { get; } = new[]
    {
        new ThemeOption(AppTheme.Light, "فاتح"),
        new ThemeOption(AppTheme.Dark, "داكن"),
        new ThemeOption(AppTheme.System, "تلقائي (حسب ويندوز)"),
    };

    [ObservableProperty] private ThemeOption _selectedTheme = new(AppTheme.System, "تلقائي (حسب ويندوز)");

    /// <summary>Runs in the constructor below rather than as a field initializer, because the field
    /// initializer order across this class's partials is not guaranteed and ThemeOptions must exist
    /// first to find the matching entry.</summary>
    partial void InitializeAppearance()
    {
        SelectedTheme = ThemeOptions.First(o => o.Value == ThemeService.Instance.Current);
    }

    partial void OnSelectedThemeChanged(ThemeOption value) => ThemeService.Instance.Set(value.Value);
}
