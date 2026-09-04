namespace Falah.Infrastructure.ControlCenter;

using Falah.Application.BusinessDay;
using Falah.Domain.BusinessDay;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Read-only projections for the Business Day close/reopen workflow and Control Center
/// exceptions. Every exception is recomputed fresh from live data on each call
/// (ControlCenterRules.BuildExceptions) with any persisted acknowledge/resolve overlaid by its own
/// deterministic id - never itself stored, matching the reference app's own re-derivation.</summary>
public sealed class ControlCenterQueryService
{
    private readonly FalahDbContext _db;
    public ControlCenterQueryService(FalahDbContext db) => _db = db;

    public async Task<BusinessDayDto> GetBusinessDayAsync(string id, CancellationToken ct = default)
    {
        var day = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new InvalidOperationException("يوم العمل غير موجود.");
        return new BusinessDayDto(day.Id, day.BusinessDate.ToString("yyyy-MM-dd"), day.Status, day.CloseRevision,
            day.SnapshotId, day.ReopenReason, day.ReopenedBy, day.ReopenedAtUtc, day.ReopenApprovedBy);
    }

    public async Task<IReadOnlyList<CloseBlockerDto>> PreviewCloseBlockersAsync(string businessDayId, CancellationToken ct = default)
    {
        var day = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.Id == businessDayId, ct)
            ?? throw new InvalidOperationException("يوم العمل غير موجود.");
        var blockers = await BusinessDayCloseEvaluator.EvaluateAsync(_db, day, ct);
        return blockers.Select(b => new CloseBlockerDto(b.Code, b.EntityType, b.EntityId, b.Message)).ToList();
    }

    public async Task<BusinessDayCloseSnapshotDto?> GetLatestCloseSnapshotAsync(string businessDayId, CancellationToken ct = default)
    {
        var snapshot = await _db.BusinessDayCloseSnapshots.AsNoTracking()
            .Where(s => s.BusinessDayId == businessDayId && s.Authoritative)
            .OrderByDescending(s => s.CloseRevision).FirstOrDefaultAsync(ct);
        return snapshot is null ? null : CloseBusinessDayHandler.ToDto(snapshot, wasReplayed: false);
    }

    public async Task<IReadOnlyList<BusinessDayCloseSnapshotDto>> ListCloseSnapshotsAsync(string businessDayId, CancellationToken ct = default) =>
        (await _db.BusinessDayCloseSnapshots.AsNoTracking().Where(s => s.BusinessDayId == businessDayId)
            .OrderBy(s => s.CloseRevision).ToListAsync(ct))
            .Select(s => CloseBusinessDayHandler.ToDto(s, wasReplayed: false)).ToList();

    public async Task<IReadOnlyList<ChecklistItemStatusDto>> GetChecklistAsync(string businessDayId, CancellationToken ct = default)
    {
        var items = await _db.ChecklistItems.AsNoTracking().Where(i => i.Active).OrderBy(i => i.Phase).ThenBy(i => i.SortOrder).ToListAsync(ct);
        var completions = await _db.ChecklistCompletions.AsNoTracking().Where(c => c.BusinessDayId == businessDayId).ToListAsync(ct);
        return items.Select(i =>
        {
            var completion = completions.FirstOrDefault(c => c.ItemId == i.Id);
            return new ChecklistItemStatusDto(i.Id, i.Label, i.Phase, i.Required, completion?.Completed ?? false, completion?.CompletedBy, completion?.CompletedAtUtc);
        }).ToList();
    }

    /// <summary>System-wide, matching how the close-blocker evaluation itself scopes unsettled
    /// custody/incomplete journal entries - see BusinessDayCloseEvaluator's own doc comment.
    /// Shift/refund/expense exceptions ARE day-scoped since those records naturally belong to one
    /// business day.</summary>
    public async Task<IReadOnlyList<ControlExceptionDto>> GetExceptionsAsync(string businessDayId, CancellationToken ct = default)
    {
        var day = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.Id == businessDayId, ct)
            ?? throw new InvalidOperationException("يوم العمل غير موجود.");

        var dayShifts = await _db.Shifts.AsNoTracking()
            .Where(s => s.BusinessDate == day.BusinessDate && s.Status == "Closed" && s.CashDifferencePiastres != null)
            .Select(s => new { s.Id, Diff = s.CashDifferencePiastres!.Value }).ToListAsync(ct);
        var dayOrders = await _db.Orders.AsNoTracking()
            .Where(o => o.BusinessDayId == day.Id && o.RefundAmountPiastres > 0)
            .Select(o => new { o.Id, o.RefundAmountPiastres }).ToListAsync(ct);
        var dayExpenses = await _db.Expenses.AsNoTracking()
            .Where(e => e.BusinessDayId == day.Id).Select(e => new { e.Id, e.AmountPiastres }).ToListAsync(ct);
        var unsettledCustodyIds = await _db.DriverCustodies.AsNoTracking()
            .Where(c => c.Status != nameof(Falah.Domain.Delivery.CustodyStatus.Settled)).Select(c => c.Id).ToListAsync(ct);
        var incompleteJournalIds = await _db.FinancialJournalEntries.AsNoTracking()
            .Where(j => j.Status != "Complete").Select(j => j.IdempotencyKey).ToListAsync(ct);
        // Ported per-delivery exceptions (ready_unassigned/out_too_long/failed/returned) - system-
        // wide, not day-scoped, matching the reference app's own selectDeliveryExceptions which
        // operates on whatever deliveries are currently live regardless of business date.
        var dayDeliveries = await _db.Deliveries.AsNoTracking()
            .Where(d => d.Status == "Ready" || d.Status == "OutForDelivery" || d.Status == "Failed" || d.Status == "Returned")
            .Select(d => new { d.Id, OrderNumber = _db.Orders.Where(o => o.Id == d.OrderId).Select(o => o.OrderNumber).FirstOrDefault(), d.Status, HasDriver = d.DriverId != null, d.OutForDeliveryAtUtc })
            .ToListAsync(ct);

        var discrepancyThreshold = await Falah.Infrastructure.Shifts.PrepareShiftCloseHandler.ReadDiscrepancyThresholdAsync(_db, ct);
        var input = new Falah.Domain.ControlCenter.ControlCenterRules.BuildExceptionsInputs(
            ClosedShiftsWithDifference: dayShifts.Select(s => (s.Id, s.Diff)).ToList(),
            ShiftDiscrepancyThresholdPiastres: discrepancyThreshold,
            RefundedOrders: dayOrders.Select(o => (o.Id, o.RefundAmountPiastres)).ToList(),
            Expenses: dayExpenses.Select(e => (e.Id, e.AmountPiastres)).ToList(),
            UnsettledCustodyIds: unsettledCustodyIds,
            IncompleteJournalIds: incompleteJournalIds,
            Deliveries: dayDeliveries.Select(d => (d.Id, d.OrderNumber, d.Status, d.HasDriver, d.OutForDeliveryAtUtc)).ToList());
        var exceptions = Falah.Domain.ControlCenter.ControlCenterRules.BuildExceptions(input);

        var states = await _db.ControlExceptionStates.AsNoTracking()
            .Where(s => exceptions.Select(e => e.Id).Contains(s.Id)).ToListAsync(ct);
        return exceptions.Select(e =>
        {
            var state = states.FirstOrDefault(s => s.Id == e.Id);
            return new ControlExceptionDto(e.Id, e.Category, e.Severity, e.Message, e.EntityType, e.EntityId,
                state?.Status ?? "open", state?.Actor, state?.Reason, state?.TimestampUtc);
        }).ToList();
    }

    public async Task<IReadOnlyList<ControlAuditEventDto>> ListAuditEventsAsync(int limit = 100, CancellationToken ct = default) =>
        (await _db.ControlAuditEvents.AsNoTracking().OrderByDescending(e => e.CreatedAtUtc).Take(limit).ToListAsync(ct))
            .Select(e => new ControlAuditEventDto(e.Id, e.Action, e.Actor, e.EntityId, e.AmountPiastres, e.OperationId,
                e.ApprovedBy, e.Reason, e.BlockerCodesJson is null ? null : System.Text.Json.JsonSerializer.Deserialize<List<string>>(e.BlockerCodesJson), e.CreatedAtUtc))
            .ToList();

    public async Task<IReadOnlyList<CashDenominationCountDto>> ListCashCountsAsync(string businessDayId, CancellationToken ct = default) =>
        (await _db.CashDenominationCounts.AsNoTracking().Where(c => c.BusinessDayId == businessDayId)
            .OrderByDescending(c => c.CreatedAtUtc).ToListAsync(ct))
            .Select(e => new CashDenominationCountDto(e.Id, e.BusinessDayId, e.ShiftId,
                System.Text.Json.JsonSerializer.Deserialize<List<Falah.Domain.BusinessDay.BusinessDayRules.DenominationRow>>(e.RowsJson)!
                    .Select(r => new DenominationRowDto(r.DenominationPiastres, r.Quantity, r.TotalPiastres)).ToList(),
                e.TotalPiastres, e.ExpectedPiastres, e.DifferencePiastres, e.CountedBy, e.CreatedAtUtc, e.FinalizedAtUtc))
            .ToList();
}
