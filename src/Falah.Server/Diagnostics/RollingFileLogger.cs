namespace Falah.Server.Diagnostics;

using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

/// <summary>Where and how the server writes its own log file. Resolved from the final host configuration
/// (so a test host's overrides apply): <c>Logging:File:Enabled</c> (default true),
/// <c>Logging:File:Directory</c>, else <c>%FALAH_LOG_DIR%\Server</c>, else
/// <c>%ProgramData%\Falah\Logs\Server</c>; <c>Logging:File:MaxFileMegabytes</c> (10) and
/// <c>Logging:File:RetentionDays</c> (30).</summary>
public sealed record FileLogOptions(bool Enabled, string Directory, long MaxFileBytes, int RetentionDays)
{
    public static FileLogOptions From(IConfiguration configuration)
    {
        var enabled = !bool.TryParse(configuration["Logging:File:Enabled"], out var on) || on;
        var directory = configuration["Logging:File:Directory"];
        if (string.IsNullOrWhiteSpace(directory))
            directory = Environment.GetEnvironmentVariable("FALAH_LOG_DIR") is { Length: > 0 } root
                ? Path.Combine(root, "Server")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Falah", "Logs", "Server");
        var megabytes = int.TryParse(configuration["Logging:File:MaxFileMegabytes"], out var mb) && mb > 0 ? mb : 10;
        var days = int.TryParse(configuration["Logging:File:RetentionDays"], out var d) && d > 0 ? d : 30;
        return new FileLogOptions(enabled, directory, megabytes * 1024L * 1024L, days);
    }
}

/// <summary>
/// Runtime logging audit 2026-11-23 (L-01, docs/FINAL_RUNTIME_LOGGING_REPORT.md): the server's own log
/// file, beside the Windows Event Log it already wrote errors to. One file a day
/// (server-yyyyMMdd.log, then .1, .2 … when a file reaches its size cap), files older than the retention
/// deleted, lines in UTC ISO 8601. Writing happens on one background thread from a bounded queue, so a
/// request never waits on the disk, and nothing here ever throws into the caller: a full disk or a
/// missing folder costs log lines, never a sale. Only what the application itself logs is written -
/// request bodies, PINs and tokens are not logged anywhere in this server.
/// </summary>
[ProviderAlias("File")]
public sealed class RollingFileLoggerProvider : ILoggerProvider
{
    public static string Version { get; } =
        typeof(RollingFileLoggerProvider).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0]
        ?? typeof(RollingFileLoggerProvider).Assembly.GetName().Version?.ToString() ?? "0";

    private readonly FileLogOptions _options;
    private readonly BlockingCollection<string> _queue = new(boundedCapacity: 10_000);
    private readonly Thread? _writer;
    private readonly object _fileGate = new();
    private string? _day;
    private int _part;

    public bool Enabled => _options.Enabled;

    public RollingFileLoggerProvider(FileLogOptions options)
    {
        _options = options;
        if (!options.Enabled) return;
        DeleteOldFiles();
        _writer = new Thread(WriteLoop) { IsBackground = true, Name = "falah-file-log" };
        _writer.Start();
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    internal void Enqueue(string line)
    {
        if (!_options.Enabled || _queue.IsAddingCompleted) return;
        try { _queue.TryAdd(line); } catch (InvalidOperationException) { /* shutting down */ }
    }

    private void WriteLoop()
    {
        foreach (var line in _queue.GetConsumingEnumerable())
        {
            WriteGuarded(line);
        }
    }

    /// <summary>A critical line (the server failing to start, say) is written at once rather than queued:
    /// the process may end straight after it, before the background writer gets to it.</summary>
    internal void WriteNow(string line)
    {
        if (_options.Enabled) WriteGuarded(line);
    }

    private void WriteGuarded(string line)
    {
        lock (_fileGate)
        {
            try { Append(line); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException or NotSupportedException) { }
        }
    }

    private void Append(string line)
    {
        Directory.CreateDirectory(_options.Directory);
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        if (today != _day) { _day = today; _part = 0; DeleteOldFiles(); }
        var path = PathFor(today, _part);
        while (File.Exists(path) && new FileInfo(path).Length >= _options.MaxFileBytes) path = PathFor(today, ++_part);
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(line);
    }

    private string PathFor(string day, int part) =>
        Path.Combine(_options.Directory, part == 0 ? $"server-{day}.log" : $"server-{day}.{part}.log");

    private void DeleteOldFiles()
    {
        try
        {
            if (!Directory.Exists(_options.Directory)) return;
            var cutoff = DateTime.UtcNow.AddDays(-_options.RetentionDays);
            foreach (var file in Directory.EnumerateFiles(_options.Directory, "server-*.log"))
                if (File.GetLastWriteTimeUtc(file) < cutoff) File.Delete(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException or NotSupportedException) { }
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _writer?.Join(TimeSpan.FromSeconds(5));
        _queue.Dispose();
    }

    private sealed class FileLogger(RollingFileLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && provider.Enabled;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            string message;
            try { message = formatter(state, exception); } catch { message = "(message could not be formatted)"; }
            var level = logLevel switch
            {
                LogLevel.Trace => "TRC", LogLevel.Debug => "DBG", LogLevel.Information => "INF",
                LogLevel.Warning => "WRN", LogLevel.Error => "ERR", _ => "CRT",
            };
            var line = new StringBuilder()
                .Append(DateTimeOffset.UtcNow.ToString("O")).Append(' ').Append(level).Append(' ')
                .Append('[').Append(category).Append(eventId.Id != 0 ? $":{eventId.Id}" : "").Append("] ")
                .Append(message).Append(Environment.NewLine);
            if (exception is not null) line.Append(exception).Append(Environment.NewLine);
            if (logLevel >= LogLevel.Critical) provider.WriteNow(line.ToString());
            else provider.Enqueue(line.ToString());
        }
    }
}
