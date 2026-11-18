namespace Falah.Client.Diagnostics;

/// <summary>
/// Audit 2026-11-18. An exception nothing caught closed the till and left no trace anywhere a person
/// could find it: neither app registered a handler, and WinUI writes only a terse Windows event.
/// The station's own recovery already covers the money (an in-flight command is on disk and is offered
/// back on the next start), so a crash is still allowed to end the process - swallowing it could leave a
/// screen half-way through an operation - but what happened is now written down first, beside the
/// station's other local state.
/// </summary>
public static class CrashLog
{
    /// <summary>Appends the exception to crash-yyyyMMdd.log under <paramref name="directory"/>. Never
    /// throws: a full or read-only disk must not turn a logged crash into a second one.</summary>
    public static string? Write(string directory, string station, Exception? exception, string source)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"crash-{DateTime.UtcNow:yyyyMMdd}.log");
            File.AppendAllText(path,
                $"[{DateTimeOffset.UtcNow:O}] {station} {source}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
            return path;
        }
        catch
        {
            return null;
        }
    }
}
