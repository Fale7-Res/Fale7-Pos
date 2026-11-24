namespace Falah.Infrastructure.Settings;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Owner decisions 2026-11-24 (group A), kept in the server's configuration (section OperatingLimits) rather than
/// the Settings table so no schema change is needed: the safe-drop warning, the driver custody cap and the PIN age.
/// Read straight off IConfiguration (this project has no binder package, like ScheduledBackupService); a missing or
/// unparseable value falls back to the owner's figure, and 0 switches a limit off.
/// </summary>
public static class OperatingLimits
{
    public const long DefaultSafeDropWarningPiastres = 300_000;
    public const long DefaultMaxDriverCustodyPiastres = 150_000;
    public const int DefaultPinMaxAgeDays = 90;

    public static long SafeDropWarningPiastres(IConfiguration? c) => Read(c, "SafeDropWarningPiastres", DefaultSafeDropWarningPiastres);
    public static long MaxDriverCustodyPiastres(IConfiguration? c) => Read(c, "MaxDriverCustodyPiastres", DefaultMaxDriverCustodyPiastres);
    public static int PinMaxAgeDays(IConfiguration? c) => (int)Math.Clamp(Read(c, "PinMaxAgeDays", DefaultPinMaxAgeDays), 0, 3650);

    private static long Read(IConfiguration? configuration, string key, long fallback)
    {
        var raw = configuration?[$"OperatingLimits:{key}"];
        return !string.IsNullOrWhiteSpace(raw)
               && long.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value)
               && value >= 0
            ? value : fallback;
    }
}
