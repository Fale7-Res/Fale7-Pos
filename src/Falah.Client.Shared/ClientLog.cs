using System.Reflection;
using System.Text;

namespace Falah.Client.Diagnostics;

/// <summary>
/// Runtime logging audit 2026-11-23 (L-03 / L-04, docs/FINAL_RUNTIME_LOGGING_REPORT.md): the station's
/// own log of what went wrong with the server - a server error, a lost connection - beside the crash log,
/// in <c>&lt;logs folder&gt;\&lt;station&gt;-yyyyMMdd.log</c>. One line per event: UTC time (ISO 8601), level,
/// station, version, machine, message. It holds what the caller writes and nothing else: no request
/// body, no PIN, no query string (CorrelationLoggingHandler writes only the method and path).
/// Writing is best-effort and never throws; a day's file stops growing at 10 MB; logs older than the
/// retention are deleted at start-up. Not configured (as in the tests) means it writes nothing.
/// </summary>
public static class ClientLog
{
    public const long MaxDailyBytes = 10L * 1024 * 1024;
    private static readonly object Gate = new();
    private static string? _directory;
    private static string _station = "client";

    /// <summary>This build's version, as written on every line and in the crash log.</summary>
    public static string Version { get; } =
        typeof(ClientLog).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0]
        ?? typeof(ClientLog).Assembly.GetName().Version?.ToString() ?? "0";

    /// <summary>Where the logs go. <c>FALAH_LOG_DIR</c> overrides the folder the app passes in.</summary>
    public static void Configure(string? directory, string station)
    {
        lock (Gate)
        {
            _directory = directory;
            _station = station;
        }
    }

    public static string? ResolveDirectory(string fallback) =>
        Environment.GetEnvironmentVariable("FALAH_LOG_DIR") is { Length: > 0 } root ? root : fallback;

    public static void Write(string level, string message)
    {
        lock (Gate)
        {
            if (_directory is null) return;
            try
            {
                Directory.CreateDirectory(_directory);
                var path = Path.Combine(_directory, $"{_station}-{DateTime.UtcNow:yyyyMMdd}.log");
                if (File.Exists(path) && new FileInfo(path).Length >= MaxDailyBytes) return;
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.WriteLine($"{DateTimeOffset.UtcNow:O} {level} {_station} v{Version} {Environment.MachineName} {message}");
            }
            catch
            {
                // Logging must never take a till down: a full or read-only disk only loses the line.
            }
        }
    }

    /// <summary>Deletes *.log files (station and crash logs) older than <paramref name="retentionDays"/>.</summary>
    public static void DeleteOldLogs(int retentionDays)
    {
        string? directory;
        lock (Gate) directory = _directory;
        if (directory is null) return;
        try
        {
            if (!Directory.Exists(directory)) return;
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            foreach (var file in Directory.EnumerateFiles(directory, "*.log"))
                if (File.GetLastWriteTimeUtc(file) < cutoff) File.Delete(file);
        }
        catch
        {
            // Same rule: cleaning up is never worth failing the start-up for.
        }
    }
}
