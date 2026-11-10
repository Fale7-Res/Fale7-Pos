using System.Text;
namespace Falah.PrintAgent;

/// <summary>Bounded per-route file history; logging failures never stop the printing worker.</summary>
public sealed class AgentLog
{
    private readonly object _gate = new();
    private readonly string _directory;
    private readonly string _path;
    private readonly long _maxBytes;
    private readonly int _keepFiles;
    private readonly Action<string> _console;
    private bool _warned;

    public AgentLog(string directory, string route, long maxBytes = 5 * 1024 * 1024, int keepFiles = 5, Action<string>? console = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(keepFiles);
        _directory = directory;
        var safeRoute = Enum.TryParse<Falah.Domain.Printing.PrintRoute>(route, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed.ToString() : "unknown";
        _path = Path.Combine(directory, $"print-agent-{safeRoute}.log");
        _maxBytes = maxBytes; _keepFiles = keepFiles; _console = console ?? Console.WriteLine;
    }

    public void Write(string message)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} {message}";
        _console(message);
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                if (File.Exists(_path) && new FileInfo(_path).Length >= _maxBytes)
                {
                    File.Delete(_path + "." + _keepFiles);
                    for (var i = _keepFiles - 1; i >= 1; i--)
                        if (File.Exists(_path + "." + i)) File.Move(_path + "." + i, _path + "." + (i + 1), overwrite: true);
                    File.Move(_path, _path + ".1", overwrite: true);
                }
                using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.WriteLine(line);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (!_warned) { _warned = true; _console($"WARNING: print agent file log unavailable: {ex.Message}"); }
            }
        }
    }
}
