using System.Text.Json;
using Microsoft.UI.Xaml;

namespace Falah.Client.CashierWinUI.Services;

/// <summary>The theme a person picked. System is the default: it was never asked for a colour,
/// it should not look like the app disagrees with the rest of Windows.</summary>
public enum AppTheme { Light, Dark, System }

/// <summary>
/// One process-wide theme preference, applied live to the whole window without a restart.
///
/// There is no Next.js reference for Dark (the reference ships no dark theme at all - see
/// Styles/Colors.xaml's own note) - Light is the parity theme and must track the reference exactly;
/// Dark is a documented C#-only extension built from the same semantic resource keys. Both live in
/// Colors.xaml's ResourceDictionary.ThemeDictionaries under matching key names, so applying a theme
/// is just telling the root element which of the two to resolve - see MainWindow's use of
/// <see cref="ToElementTheme"/> against its RootGrid.
///
/// Persisted the same way RetainedRequestStore persists pending operations: LocalApplicationData,
/// written to a temp file and moved into place so a crash mid-write never leaves a half-written,
/// unreadable preference file.
/// </summary>
public sealed class ThemeService
{
    public static ThemeService Instance { get; } = new();

    private readonly string _filePath;
    private readonly object _lock = new();

    public AppTheme Current { get; private set; }

    /// <summary>Fired after a change is persisted, so a subscriber can re-apply immediately. Carries
    /// the new theme rather than requiring a re-read of <see cref="Current"/>, since the two are
    /// always in sync by the time this fires.</summary>
    public event Action<AppTheme>? Changed;

    private ThemeService()
    {
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Falah", "preferences", "cashier-appearance.json");
        Current = Load();
    }

    public void Set(AppTheme theme)
    {
        lock (_lock)
        {
            if (theme == Current) return;
            Current = theme;
            Save(theme);
        }
        Changed?.Invoke(theme);
    }

    /// <summary>ElementTheme.Default is what makes "System" actually follow Windows: WinUI resolves
    /// it to whichever of Light/Dark the OS is currently set to, and keeps resolving it live if the
    /// OS setting changes while the app is running - this service does not need to poll anything.</summary>
    public static ElementTheme ToElementTheme(AppTheme theme) => theme switch
    {
        AppTheme.Light => ElementTheme.Light,
        AppTheme.Dark => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };

    private sealed record PreferenceFile(string Theme);

    private AppTheme Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return AppTheme.System;
            var stored = JsonSerializer.Deserialize<PreferenceFile>(File.ReadAllText(_filePath));
            return Enum.TryParse<AppTheme>(stored?.Theme, out var theme) ? theme : AppTheme.System;
        }
        catch
        {
            // A corrupted or unreadable preference file must never stop the app from launching -
            // it falls back to System, exactly the state a fresh install starts in.
            return AppTheme.System;
        }
    }

    private void Save(AppTheme theme)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath)!;
            Directory.CreateDirectory(directory);
            var temp = _filePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(new PreferenceFile(theme.ToString())));
            File.Move(temp, _filePath, overwrite: true);
        }
        catch
        {
            // Losing the ability to persist must not stop the theme from applying for this
            // session - it just will not survive a restart.
        }
    }
}
