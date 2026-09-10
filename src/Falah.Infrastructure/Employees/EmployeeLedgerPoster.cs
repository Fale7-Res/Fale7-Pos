namespace Falah.Infrastructure.Employees;

using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Shared plumbing behind every command that appends one row to an employee's ledger
/// (recordDailyWage/recordOvertime/recordExtraShift/recordManualLedgerEntry's own automatic
/// biometric/correction callers in lib/store.tsx) - computing the running balance and inserting the
/// new row with it baked in, deduped by the row's own deterministic Id exactly like legacy's own
/// `ledgerEntriesRef.current.some(entry => entry.id === entryId)` check.</summary>
internal static class EmployeeLedgerPoster
{
    public static async Task<Money> GetCurrentBalanceAsync(FalahDbContext db, string employeeId, CancellationToken ct)
    {
        var entries = await db.EmployeeLedgerEntries.AsNoTracking().Where(e => e.EmployeeId == employeeId)
            .Select(e => new { e.CreditPiastres, e.DebitPiastres }).ToListAsync(ct);
        // A single command can append more than one movement before SaveChanges (for
        // example, a late penalty followed by overtime). Include its earlier pending
        // appends, otherwise the second row's displayed running balance omits the first.
        var pendingDelta = db.ChangeTracker.Entries<EmployeeLedgerEntryEntity>()
            .Where(e => e.State == EntityState.Added && e.Entity.EmployeeId == employeeId)
            .Sum(e => e.Entity.CreditPiastres - e.Entity.DebitPiastres);
        return Money.FromPiastres(entries.Sum(e => e.CreditPiastres - e.DebitPiastres) + pendingDelta);
    }

    /// <summary>Returns null (posts nothing) if an entry with this exact Id already exists - the
    /// deterministic-id dedup that makes a retried biometric event or duplicate correction a safe
    /// no-op rather than a double-post.</summary>
    public static async Task<EmployeeLedgerEntryEntity?> PostIfNewAsync(
        FalahDbContext db, string id, string employeeId, string type, string description, Money debit, Money credit,
        string recordedBy, string? authorizedBy, string? shiftId, string? referenceId, double? hours,
        long? rateSnapshotPiastres, long? valueSnapshotPiastres, CancellationToken ct)
    {
        if (db.EmployeeLedgerEntries.Local.Any(e => e.Id == id)
            || await db.EmployeeLedgerEntries.AsNoTracking().AnyAsync(e => e.Id == id, ct)) return null;

        var currentBalance = await GetCurrentBalanceAsync(db, employeeId, ct);
        var newBalance = currentBalance + credit - debit;
        var entity = new EmployeeLedgerEntryEntity
        {
            Id = id, EmployeeId = employeeId, Type = type, Description = description,
            DebitPiastres = debit.Piastres, CreditPiastres = credit.Piastres, BalanceAfterPiastres = newBalance.Piastres,
            RecordedBy = recordedBy, AuthorizedBy = authorizedBy, ShiftId = shiftId, ReferenceId = referenceId,
            Hours = hours, RateSnapshotPiastres = rateSnapshotPiastres, ValueSnapshotPiastres = valueSnapshotPiastres,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.EmployeeLedgerEntries.Add(entity);
        return entity;
    }
}
