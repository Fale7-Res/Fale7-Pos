namespace Falah.Infrastructure.Persistence;

using Falah.Domain.Common;
using Falah.Domain.Idempotency;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// EF Core-backed IFinancialJournal - the production implementation that replaces
/// InMemoryFinancialJournal (kept in Falah.Domain for unit tests only). Operates against the
/// caller's ambient DbContext/transaction so a journal entry commits or rolls back together with
/// whatever financial write it is guarding, rather than as a separate round trip.
/// IdempotencyKey is the table's PRIMARY KEY, so TryStart's uniqueness is enforced by the database
/// itself, not just by this class's own logic - a concurrent request racing in from the other
/// device hits a real constraint violation, not a check that could itself be raced.
/// </summary>
public sealed class EfFinancialJournal : IFinancialJournal
{
    private readonly FalahDbContext _db;
    public EfFinancialJournal(FalahDbContext db) => _db = db;

    public bool TryStart(string idempotencyKey, string kind)
    {
        if (_db.FinancialJournalEntries.Local.Any(e => e.IdempotencyKey == idempotencyKey)
            || _db.FinancialJournalEntries.Any(e => e.IdempotencyKey == idempotencyKey))
            return false;

        _db.FinancialJournalEntries.Add(new FinancialJournalEntryEntity
        {
            IdempotencyKey = idempotencyKey,
            Kind = kind,
            Status = nameof(JournalEntryStatus.Pending),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        return true;
    }

    public void Complete(string idempotencyKey)
    {
        var entry = _db.FinancialJournalEntries.Local.FirstOrDefault(e => e.IdempotencyKey == idempotencyKey)
            ?? _db.FinancialJournalEntries.FirstOrDefault(e => e.IdempotencyKey == idempotencyKey);
        if (entry is null || entry.Status != nameof(JournalEntryStatus.Pending))
            throw new DomainException($"لا يوجد قيد معلّق بهذا المفتاح: {idempotencyKey}");
        entry.Status = nameof(JournalEntryStatus.Complete);
    }

    public bool DiscardPending(string idempotencyKey)
    {
        var entry = _db.FinancialJournalEntries.Local.FirstOrDefault(e => e.IdempotencyKey == idempotencyKey)
            ?? _db.FinancialJournalEntries.FirstOrDefault(e => e.IdempotencyKey == idempotencyKey);
        if (entry is null || entry.Status != nameof(JournalEntryStatus.Pending)) return false;
        _db.FinancialJournalEntries.Remove(entry);
        return true;
    }

    public FinancialJournalEntry? Find(string idempotencyKey)
    {
        // Include unsaved additions/completions, and hide a deletion already staged by this unit
        // of work. Returned domain state is detached and cannot mutate the persistence entity.
        var tracked = _db.ChangeTracker.Entries<FinancialJournalEntryEntity>()
            .FirstOrDefault(e => e.Entity.IdempotencyKey == idempotencyKey);
        if (tracked?.State == EntityState.Deleted) return null;
        var entry = tracked?.Entity ?? _db.FinancialJournalEntries.AsNoTracking()
            .FirstOrDefault(e => e.IdempotencyKey == idempotencyKey);
        if (entry is null) return null;
        if (!Enum.TryParse<JournalStatus>(entry.Status, true, out var status) || !Enum.IsDefined(status))
            throw new DomainException("حالة القيد المالي المحفوظ غير صالحة.");
        return FinancialJournalEntry.Restore(entry.IdempotencyKey, entry.Kind, status, entry.CreatedAtUtc);
    }
}
