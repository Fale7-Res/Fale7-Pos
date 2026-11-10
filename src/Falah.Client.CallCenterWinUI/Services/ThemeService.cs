using System.Text.Json;
using Microsoft.UI.Xaml;

namespace Falah.Client.CallCenterWinUI.Services;

/// <summary>The theme a person picked. System is the default: it follows Windows.</summary>
public enum AppTheme { Light, Dark, System }

/// <summary>
/// The Call Center's theme preference, the same mechanism as the Cashier's ThemeService: one
/// process-wide choice, applied live to the window root (MainWindow) without a restart, and kept in
/// this station's own preference file - never shared with the Cashier, which may sit on the same PC
/// with a different choice. Colors.xaml defines every key in both Light and Dark.
/// </summary>
public sealed class ThemeService
{
    public static ThemeService Instance { get; } = new();

    private readonly string _filePath;
    private readonly object _lock = new();

    public AppTheme Current { get; private set; }
    public event Action<AppTheme>? Changed;

    private ThemeService()
    {
        _filePath = Path.Combine(FileRetainedRequestStore.DataRoot(), "preferences", "callcenter-appearance.json");
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

    /// <summary>System → Light → Dark → System: one button in the header, no settings screen needed.</summary>
    public static AppTheme Next(AppTheme theme) => theme switch
    {
        AppTheme.System => AppTheme.Light,
        AppTheme.Light => AppTheme.Dark,
        _ => AppTheme.System,
    };

    public static string Label(AppTheme theme) => theme switch
    {
        AppTheme.Light => "المظهر: فاتح",
        AppTheme.Dark => "المظهر: داكن",
        _ => "المظهر: حسب ويندوز",
    };

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
            return AppTheme.System;   // an unreadable preference never stops the station from starting
        }
    }

    private void Save(AppTheme theme)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var temp = _filePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(new PreferenceFile(theme.ToString())));
            File.Move(temp, _filePath, overwrite: true);
        }
        catch
        {
            // Still applied for this session; it just will not survive a restart.
        }
    }
}
