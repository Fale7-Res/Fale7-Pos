using System.Text.Json;
using System.Security.Cryptography;
using Falah.Client.Security;

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
    string? RecoveryError => null;
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
    private sealed record Envelope(string Type, DateTimeOffset SavedAtUtc, JsonElement? Payload = null, string? Protected = null);

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };
    private readonly string _directory;
    private readonly ILocalDataProtector _protector;
    public string? RecoveryError { get; private set; }
    private readonly Action<Exception>? _onError;

    public FileRetainedRequestStore(string directory, Action<Exception>? onError = null, ILocalDataProtector? protector = null)
    {
        _directory = directory;
        _protector = protector ?? WindowsUserDataProtection.Instance;
        _onError = onError;
        Directory.CreateDirectory(_directory);
        // These retired commands contain PINs, not financial operations. Never recover their
        // plaintext files from older versions, including interrupted atomic-write temp files.
        foreach (var key in new[] { "users.create", "users.change-pin" })
        {
            try
            {
                File.Delete(PathFor(key));
                File.Delete(PathFor(key) + ".tmp");
            }
            catch (Exception ex) { ReportError(ex); }
        }
        ValidateAndMigrate();
    }

    /// <summary>Scoped per station: both clients can run on one machine, and neither should surface
    /// the other's pending work as its own.</summary>
    public static string DirectoryFor(string station) => Path.Combine(DataRoot(), "pending-operations", station);

    /// <summary>Where this station keeps its local state. FALAH_CLIENT_DATA_ROOT (process-scoped) lets an
    /// isolated verification session run the real client without reading, clearing or replaying the
    /// station's own pending operations; LOCALAPPDATA alone does not redirect the known-folder API.</summary>
    public static string DataRoot() =>
        Environment.GetEnvironmentVariable("FALAH_CLIENT_DATA_ROOT") is { Length: > 0 } root
            ? root
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Falah");

    private string PathFor(string ownerKey)
    {
        var safe = string.Concat(ownerKey.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return Path.Combine(_directory, safe + ".json");
    }

    private void ReportError(Exception ex)
    {
        if (ex is PendingDataProtectionException)
            RecoveryError = "تعذر فتح محاولة محفوظة بحساب ويندوز الحالي. الملفات محفوظة؛ استخدم حساب ويندوز الأصلي أو استعد مفاتيحه قبل تسجيل عمليات جديدة.";
        _onError?.Invoke(ex);
    }
    private Envelope ProtectEnvelope(Envelope envelope)
    {
        var payload = envelope.Payload ?? throw new JsonException("Pending payload is missing.");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Options);
        try { return envelope with { Payload = null, Protected = Convert.ToBase64String(_protector.Protect(bytes)) }; }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private void WriteEnvelope(string path, Envelope envelope)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(envelope, Options));
        File.Move(temp, path, overwrite: true);
    }
    private JsonElement ReadPayload(Envelope envelope, string path)
    {
        if (envelope.Protected is null)
        {
            var payload = envelope.Payload ?? throw new JsonException("Pending payload is missing.");
            WriteEnvelope(path, ProtectEnvelope(envelope));
            return payload;
        }
        byte[]? bytes = null;
        try
        {
            bytes = _protector.Unprotect(Convert.FromBase64String(envelope.Protected));
            return JsonSerializer.Deserialize<JsonElement>(bytes, Options);
        }
        catch (Exception ex) when (ex is PendingDataProtectionException or FormatException or CryptographicException or JsonException)
        { throw new PendingDataProtectionException("تعذر فتح الطلب المحمي. لم يُحذف الملف.", ex); }
        finally { if (bytes is not null) CryptographicOperations.ZeroMemory(bytes); }
    }
    private void ValidateAndMigrate()
    {
        foreach (var path in Directory.EnumerateFiles(_directory, "*.json").Concat(Directory.EnumerateFiles(_directory, "*.json.tmp")).ToArray())
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<Envelope>(File.ReadAllText(path), Options);
                if (envelope is not null) _ = ReadPayload(envelope, path);
            }
            catch (Exception ex) { ReportError(ex); }
        }
    }

    public void Save(string ownerKey, object request)
    {
        try
        {
            var payload = JsonSerializer.SerializeToElement(request, request.GetType(), Options);
            var envelope = ProtectEnvelope(new Envelope(request.GetType().AssemblyQualifiedName!, DateTimeOffset.UtcNow, payload));
            WriteEnvelope(PathFor(ownerKey), envelope);
        }
        catch (Exception ex)
        {
            // Losing the ability to persist must not stop the operation itself - the in-memory
            // retention still protects this session, which is what the behaviour was before.
            ReportError(ex);
        }
    }

    public void Clear(string ownerKey)
    {
        try
        {
            var path = PathFor(ownerKey);
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex) { ReportError(ex); }
    }

    public object? Load(string ownerKey)
    {
        try
        {
            var path = PathFor(ownerKey);
            if (!File.Exists(path)) return null;
            var envelope = JsonSerializer.Deserialize<Envelope>(File.ReadAllText(path), Options);
            if (envelope is null) return null;
            var payload = ReadPayload(envelope, path);
            var type = Type.GetType(envelope.Type);
            // A request type that no longer exists cannot be re-sent; leaving the file in place would
            // strand the screen, so it is dropped and the operator sees no false pending state.
            if (type is null) { Clear(ownerKey); return null; }
            return payload.Deserialize(type, Options);
        }
        catch (Exception ex)
        {
            ReportError(ex);
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
                _ = ReadPayload(envelope, path);
                records.Add(new RetainedRequestRecord(
                    Path.GetFileNameWithoutExtension(path),
                    envelope.Type.Split(',')[0].Split('.').Last(),
                    envelope.SavedAtUtc));
            }
            return records;
        }
        catch (Exception ex)
        {
            ReportError(ex);
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
