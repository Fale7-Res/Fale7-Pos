namespace Falah.Infrastructure.ControlCenter;

using Falah.Domain.BusinessDay;
using Falah.Domain.Delivery;
using Falah.Domain.Financial;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Shifts;
using Microsoft.EntityFrameworkCore;

/// <summary>Gathers every input BusinessDayRules.EvaluateCloseBlockers needs, straight from
/// persisted rows - shared by the actual close command and a read-only preview endpoint, so "why
/// can't I close" always reflects EXACTLY the same computation the close attempt itself will run.
///
/// Two of the reference app's own inputs are hardcoded true here, deliberately: PersistenceHealthy
/// and WriterOwned both existed to detect a corrupt/contested browser localStorage writer in the
/// original single-tab architecture - this system's persistence is a real SQL Server transaction per
/// request, with no "which tab owns the writer lock" concept, so there is nothing meaningful to
/// check; if the database itself were unreachable, this very query would already have thrown before
/// reaching the point of evaluating blockers at all. DeliveryTrips is always empty because this
/// system's Delivery slice deliberately has no separate "trip" concept - dispatch is per-order (see
/// the Delivery slice's own note) - so UNSETTLED_DELIVERY_TRIP can never fire here; unsettled
/// custody is still fully covered by its own direct check below.</summary>
public static class BusinessDayCloseEvaluator
{
    public static async Task<IReadOnlyList<CloseBlocker>> EvaluateAsync(FalahDbContext db, BusinessDayEntity day, CancellationToken ct)
    {
        var shifts = await db.Shifts.AsNoTracking().Select(s => new { s.Id, s.Status }).ToListAsync(ct);
        var driverCustodies = await db.DriverCustodies.AsNoTracking().Select(c => new { c.Id, c.Status }).ToListAsync(ct);
        var journalEntries = await db.FinancialJournalEntries.AsNoTracking().Select(j => new { j.IdempotencyKey, j.Status }).ToListAsync(ct);
        var pendingElectronicOrderIds = await db.Orders.AsNoTracking()
            .Where(o => o.ElectronicPaymentStatus == "Pending").Select(o => o.Id).ToListAsync(ct);

        var closingItems = await db.ChecklistItems.AsNoTracking()
            .Where(i => i.Phase == "Closing" && i.Active && i.Required).ToListAsync(ct);
        var completions = await db.ChecklistCompletions.AsNoTracking().Where(c => c.BusinessDayId == day.Id).ToListAsync(ct);
        var requiredChecklist = closingItems
            .Select(i => (i.Id, i.Label, completions.Any(c => c.ItemId == i.Id && c.Completed)))
            .ToList();

        var reconciliationChecks = await ComputeReconciliationChecksAsync(db, day, ct);

        var inputs = new BusinessDayRules.CloseBlockerInputs(
            Shifts: shifts.Select(s => (s.Id, s.Status == "Closed")).ToList(),
            DeliveryTrips: Array.Empty<(string, bool)>(),
            DriverCustodies: driverCustodies.Select(c => (c.Id, c.Status == nameof(CustodyStatus.Settled))).ToList(),
            JournalEntries: journalEntries.Select(j => (j.IdempotencyKey, j.Status == "Complete")).ToList(),
            Orders: pendingElectronicOrderIds.Select(id => (id, true)).ToList(),
            PersistenceHealthy: true,
            WriterOwned: true,
            RequiredChecklist: requiredChecklist,
            ReconciliationChecks: reconciliationChecks);

        return BusinessDayRules.EvaluateCloseBlockers(inputs);
    }

    /// <summary>Defensive checks only - every one of these should be structurally impossible given
    /// the unique indexes and gates already enforced elsewhere in this codebase (a unique
    /// IdempotencyKey index, protected-balance asserts on every withdrawal, the shift-close
    /// discrepancy-approval gate). They exist so that IF one of those upstream invariants is ever
    /// violated, a business day can never close over it silently.</summary>
    private static async Task<List<(string Code, bool Critical)>> ComputeReconciliationChecksAsync(FalahDbContext db, BusinessDayEntity day, CancellationToken ct)
    {
        var checks = new List<(string, bool)>();

        var duplicateKeyExists = await db.TreasuryMovements.AsNoTracking()
            .GroupBy(m => m.IdempotencyKey).Where(g => g.Count() > 1).AnyAsync(ct);
        checks.Add(("DUPLICATE_IDEMPOTENCY_KEY", duplicateKeyExists));

        var postedMovements = await db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Status == nameof(MovementStatus.Posted))
            .Select(m => new { m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres }).ToListAsync(ct);
        foreach (var accountId in TreasuryAccounts.Protected)
        {
            var balance = postedMovements.Sum(m => (m.DestinationAccountId == accountId ? m.AmountPiastres : 0) - (m.SourceAccountId == accountId ? m.AmountPiastres : 0));
            checks.Add(($"NEGATIVE_PROTECTED_BALANCE:{accountId}", balance < 0));
        }

        var threshold = await Falah.Infrastructure.Shifts.PrepareShiftCloseHandler.ReadDiscrepancyThresholdAsync(db, ct);
        var unauthorizedDrawerMismatch = await db.Shifts.AsNoTracking()
            .Where(s => s.BusinessDate == day.BusinessDate && s.CashDifferencePiastres != null && s.DifferenceAuthorizedBy == null)
            .AnyAsync(s => s.CashDifferencePiastres! > threshold || s.CashDifferencePiastres! < -threshold, ct);
        checks.Add(("DRAWER_RECONCILIATION", unauthorizedDrawerMismatch));

        return checks;
    }
}
