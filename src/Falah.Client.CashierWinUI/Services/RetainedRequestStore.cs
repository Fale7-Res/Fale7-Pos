using System.Text.Json;

namespace Falah.Client.CashierWinUI.Services;

/// <summary>
/// Where an in-flight request survives the process that sent it.
///
/// A retained request exists because an ambiguous response cannot prove the server did not commit
/// the operation. Holding it only in memory answered that for a crash of the screen, but not for the
/// thing this hardware actually suffers: the station losing power mid-payment. On the next start the
/// evidence was simply gone, and the operator had no way to ask the server what had happened.
///
/// So the exact bytes that were submitted are written to disk before they are sent, and removed only
/// once the outcome is known. Nothing here decides anything - it preserves the question so a person
/// can still answer it tomorrow.
/// </summary>
public interface IRetainedRequestStore
{
    void Save(string ownerKey, object request);
    void Clear(string ownerKey);

    /// <summary>Returns the stored request for this owner, or null. The stored type name decides how
    /// it is deserialized, so an owner whose request type varies (a screen that submits several
    /// different commands) recovers the right one.</summary>
    object? Load(string ownerKey);

    IReadOnlyList<RetainedRequestRecord> List();
}

public sealed record RetainedRequestRecord(string OwnerKey, string TypeName, DateTimeOffset SavedAtUtc);

public sealed class FileRetainedRequestStore : IRetainedRequestStore
{
    private sealed record Envelope(string Type, DateTimeOffset SavedAtUtc, JsonElement Payload);

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };
    private readonly string _directory;
    private readonly Action<Exception>? _onError;

    public FileRetainedRequestStore(string directory, Action<Exception>? onError = null)
    {
        _directory = directory;
        _onError = onError;
        Directory.CreateDirectory(_directory);
    }

    /// <summary>Scoped per station: both clients can run on one machine, and neither should surface
    /// the other's pending work as its own.</summary>
    public static string DirectoryFor(string station) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Falah", "pending-operations", station);

    private string PathFor(string ownerKey)
    {
        var safe = string.Concat(ownerKey.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return Path.Combine(_directory, safe + ".json");
    }

    public void Save(string ownerKey, object request)
    {
        try
        {
            var envelope = new
            {
                Type = request.GetType().AssemblyQualifiedName,
                SavedAtUtc = DateTimeOffset.UtcNow,
                Payload = request,
            };
            // Written whole, then moved into place: a station losing power mid-write must never leave
            // a half-written file that reads back as a different request than the one submitted.
            var path = PathFor(ownerKey);
            var temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(envelope, Options));
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            // Losing the ability to persist must not stop the operation itself - the in-memory
            // retention still protects this session, which is what the behaviour was before.
            _onError?.Invoke(ex);
        }
    }

    public void Clear(string ownerKey)
    {
        try
        {
            var path = PathFor(ownerKey);
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex) { _onError?.Invoke(ex); }
    }

    public object? Load(string ownerKey)
    {
        try
        {
            var path = PathFor(ownerKey);
            if (!File.Exists(path)) return null;
            var envelope = JsonSerializer.Deserialize<Envelope>(File.ReadAllText(path), Options);
            if (envelope is null) return null;
            var type = Type.GetType(envelope.Type);
            // A request type that no longer exists cannot be re-sent; leaving the file in place would
            // strand the screen, so it is dropped and the operator sees no false pending state.
            if (type is null) { Clear(ownerKey); return null; }
            return envelope.Payload.Deserialize(type, Options);
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
            return null;
        }
    }

    public IReadOnlyList<RetainedRequestRecord> List()
    {
        try
        {
            var records = new List<RetainedRequestRecord>();
            foreach (var path in Directory.EnumerateFiles(_directory, "*.json"))
            {
                var envelope = JsonSerializer.Deserialize<Envelope>(File.ReadAllText(path), Options);
                if (envelope is null) continue;
                records.Add(new RetainedRequestRecord(
                    Path.GetFileNameWithoutExtension(path),
                    envelope.Type.Split(',')[0].Split('.').Last(),
                    envelope.SavedAtUtc));
            }
            return records;
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
            return Array.Empty<RetainedRequestRecord>();
        }
    }
}

/// <summary>Retained requests are declared as field initializers on view models, which are
/// constructed without a container, so the store is resolved through this ambient hook. Left null in
/// tests unless a test sets it, which keeps the class's behaviour identical to before persistence
/// existed.</summary>
public static class RetainedRequestStorage
{
    public static IRetainedRequestStore? Current { get; set; }
}
