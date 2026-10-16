using System.Text.Json;

namespace Falah.PrintAgent;

/// <summary>
/// Attempts that physically happened but that the server has not acknowledged.
///
/// Reporting is the one step that can fail AFTER the printer has already printed. When it failed,
/// the job stayed Pending on the server, the next poll handed the agent the same job, and the agent
/// printed it again - three failed reports, three tickets out of the kitchen printer, for one order.
/// The kitchen cooks what it is handed.
///
/// So an attempt whose outcome could not be delivered is written down here first, on disk, before
/// the agent moves on. Whatever happens next - another poll, a crash, the station losing power - the
/// agent finds the record, re-reports the SAME attempt, and prints nothing. The record is removed
/// only once the server has actually taken it.
///
/// Written to a file rather than kept in memory for the same reason RetainedRequest is: a station
/// that loses power between printing and reporting is exactly the case this exists for.
/// </summary>
public sealed class UnreportedAttemptStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private Dictionary<string, StoredAttempt> _attempts;

    public sealed record StoredAttempt(string AttemptRequestId, bool Succeeded, string? Error, DateTimeOffset AtUtc);

    public UnreportedAttemptStore(string path)
    {
        _path = path;
        _attempts = Load(path);
    }

    public static UnreportedAttemptStore ForRoute(string directory, string route)
    {
        var canonical = Enum.Parse<Falah.Domain.Printing.PrintRoute>(route, true).ToString();
        var store = new UnreportedAttemptStore(Path.Combine(directory, $"unreported-print-attempts-{canonical}.json"));
        // Preserve any outcomes recorded by the earlier shared journal. Each route only ever
        // looks up its own pending job ids; do not delete the legacy file during an upgrade.
        foreach (var entry in Load(Path.Combine(directory, "unreported-print-attempts.json")))
            if (store.Find(entry.Key) is null) store.Remember(entry.Key, entry.Value);
        return store;
    }

    private static Dictionary<string, StoredAttempt> Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new();
            return JsonSerializer.Deserialize<Dictionary<string, StoredAttempt>>(File.ReadAllText(path)) ?? new();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Cannot read the print-attempt journal. Restore or inspect it before printing; ignoring it could duplicate tickets.", ex);
        }
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        // Written to a temp file and moved into place, so a station losing power mid-write finds
        // either the old file or the new one, never half of either.
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(_attempts));
        File.Move(temporary, _path, overwrite: true);
    }

    public StoredAttempt? Find(string printJobId)
    {
        lock (_gate) return _attempts.GetValueOrDefault(printJobId);
    }

    public void Remember(string printJobId, StoredAttempt attempt)
    {
        lock (_gate)
        {
            _attempts[printJobId] = attempt;
            Save();
        }
    }

    public void Forget(string printJobId)
    {
        lock (_gate)
        {
            if (_attempts.Remove(printJobId)) Save();
        }
    }

    public int Count
    {
        get { lock (_gate) return _attempts.Count; }
    }
}
