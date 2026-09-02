namespace Falah.Domain.Idempotency;

using Falah.Domain.Common;

public enum JournalStatus { Pending, Complete, Failed, RecoveryRequired }

/// <summary>
/// One row per financial operation attempt, keyed by a stable idempotency key that the caller
/// mints once per real business event (a single checkout click's session, one expense form
/// opening, one treasury transfer form session) and never rotates on retry. Ported from
/// lib/financial-journal.mjs, which is the pattern that made same-task double-click and
/// network-retry duplication provably impossible in the current web app.
/// </summary>
public sealed class FinancialJournalEntry
{
    public string IdempotencyKey { get; }
    public string Kind { get; }
    public JournalStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }

    internal FinancialJournalEntry(string idempotencyKey, string kind, DateTimeOffset createdAtUtc)
    {
        IdempotencyKey = idempotencyKey;
        Kind = kind;
        CreatedAtUtc = createdAtUtc;
        Status = JournalStatus.Pending;
    }

    internal void MarkComplete() => Status = JournalStatus.Complete;
}

public interface IFinancialJournal
{
    /// <summary>
    /// Returns false when an entry for this key already exists (in-flight or posted). Callers
    /// MUST treat false as "this is the same business event, skip posting again" - never as an
    /// error to surface to the operator. This single check is what stops a same-task double
    /// click or a network retry from posting a second movement.
    /// </summary>
    bool TryStart(string idempotencyKey, string kind);

    /// <summary>Marks a pending entry as posted. Throws if there is no pending entry for the key -
    /// that would mean Complete was called without a matching TryStart, which is a bug upstream.</summary>
    void Complete(string idempotencyKey);

    /// <summary>
    /// Removes a still-pending entry whose operation threw before any financial side effect was
    /// applied (e.g. insufficient source balance) - so a rejected command doesn't leave a
    /// permanent pending entry that later blocks business-day close. A 'complete' entry is never
    /// touched: it describes a real posted movement.
    /// </summary>
    bool DiscardPending(string idempotencyKey);

    FinancialJournalEntry? Find(string idempotencyKey);
}

/// <summary>
/// In-process reference implementation, used directly by domain unit tests. Production wires an
/// EF Core-backed journal (Falah.Infrastructure) so a pending/complete entry survives an app or
/// machine restart - the exact case a crash mid-payment must be safe against.
/// </summary>
public sealed class InMemoryFinancialJournal : IFinancialJournal
{
    private readonly Dictionary<string, FinancialJournalEntry> _entries = new();

    public bool TryStart(string idempotencyKey, string kind)
    {
        if (_entries.ContainsKey(idempotencyKey)) return false;
        _entries[idempotencyKey] = new FinancialJournalEntry(idempotencyKey, kind, DateTimeOffset.UtcNow);
        return true;
    }

    public void Complete(string idempotencyKey)
    {
        if (!_entries.TryGetValue(idempotencyKey, out var entry) || entry.Status != JournalStatus.Pending)
            throw new DomainException($"لا يوجد قيد معلّق بهذا المفتاح: {idempotencyKey}");
        entry.MarkComplete();
    }

    public bool DiscardPending(string idempotencyKey)
    {
        if (!_entries.TryGetValue(idempotencyKey, out var entry) || entry.Status != JournalStatus.Pending) return false;
        _entries.Remove(idempotencyKey);
        return true;
    }

    public FinancialJournalEntry? Find(string idempotencyKey) => _entries.GetValueOrDefault(idempotencyKey);
}
