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
        // PERF-0003 (2026-11-22): only rows that can block a close are read. EvaluateCloseBlockers looks at
        // open shifts, unsettled custodies and incomplete journal entries alone, so loading the whole
        // history to discard the closed ones changed nothing but the time the close took.
        var shifts = await db.Shifts.AsNoTracking().Where(s => s.Status != "Closed").Select(s => new { s.Id, s.Status }).ToListAsync(ct);
        var driverCustodies = await db.DriverCustodies.AsNoTracking().Where(c => c.Status != nameof(CustodyStatus.Settled))
            .Select(c => new { c.Id, c.Status }).ToListAsync(ct);
        var journalEntries = await db.FinancialJournalEntries.AsNoTracking().Where(j => j.Status != "Complete")
            .Select(j => new { j.IdempotencyKey, j.Status }).ToListAsync(ct);
        var pendingElectronicOrderIds = await db.Orders.AsNoTracking()
            .Where(o => o.ElectronicPaymentStatus == "pending").Select(o => o.Id).ToListAsync(ct);   // stored lower-case (OrderEnumMapping.ToWireString)

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
            ReconciliationChecks: reconciliationChecks,
            DriverBalances: (await Falah.Infrastructure.Delivery.DriverBalances.OpenAsync(db, ct))
                .Select(b => (b.DriverId, b.CarriedPiastres)).ToList());

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

        // Summed by SQL Server, not by loading every posted movement into this process (PERF-0003).
        var posted = db.TreasuryMovements.AsNoTracking().Where(m => m.Status == nameof(MovementStatus.Posted));
        foreach (var accountId in TreasuryAccounts.Protected)
        {
            var inflow = await posted.Where(m => m.DestinationAccountId == accountId).SumAsync(m => (long?)m.AmountPiastres, ct) ?? 0;
            var outflow = await posted.Where(m => m.SourceAccountId == accountId).SumAsync(m => (long?)m.AmountPiastres, ct) ?? 0;
            checks.Add(($"NEGATIVE_PROTECTED_BALANCE:{accountId}", inflow - outflow < 0));
        }

        var threshold = await Falah.Infrastructure.Shifts.PrepareShiftCloseHandler.ReadDiscrepancyThresholdAsync(db, ct);
        var unauthorizedDrawerMismatch = await Falah.Infrastructure.BusinessDay.BusinessDayShifts.Of(db.Shifts.AsNoTracking(), day)
            .Where(s => s.CashDifferencePiastres != null && s.DifferenceAuthorizedBy == null)
            .AnyAsync(s => s.CashDifferencePiastres! > threshold || s.CashDifferencePiastres! < -threshold, ct);
        checks.Add(("DRAWER_RECONCILIATION", unauthorizedDrawerMismatch));

        return checks;
    }
}
