namespace Falah.Infrastructure.ControlCenter;

using System.Text.Json;
using Falah.Application.BusinessDay;
using Falah.Domain.Authorization;
using Falah.Domain.BusinessDay;
using Falah.Domain.ControlCenter;
using Falah.Domain.Delivery;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from closeBusinessDay in lib/enterprise-store.tsx. Posts NOTHING financial - a
/// close is purely a state transition plus a frozen, reproducible snapshot of reconciliation data
/// that ALREADY exists from other commands; if a business day close command ever needed to touch
/// Treasury/Orders/Inventory itself, that would be a sign something is deeply wrong (see the
/// "successful order/payment != successful print" invariant elsewhere in this codebase for the same
/// spirit: administrative actions must never silently mutate financial truth).</summary>
public sealed class CloseBusinessDayHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly ReportingQueryService _reporting;

    public CloseBusinessDayHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, ReportingQueryService reporting)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _reporting = reporting;
    }

    public async Task<BusinessDayCloseSnapshotDto> HandleAsync(CloseBusinessDayRequest request, CancellationToken ct = default)
    {
        var snapshotId = $"snapshot-{request.RequestId}";
        var existing = await _db.BusinessDayCloseSnapshots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == snapshotId, ct);
        if (existing is not null) return ToDto(existing, wasReplayed: true);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new Falah.Domain.Common.DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "business_day_close", null);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "business_day_close", approval, requireApproval: true);

        var day = await _db.BusinessDays.FirstOrDefaultAsync(d => d.Id == request.BusinessDayId, ct)
            ?? throw new InvalidOperationException("يوم العمل غير موجود.");

        var currentStatus = Enum.Parse<BusinessDayStatus>(day.Status);
        BusinessDayRules.AssertTransition(currentStatus, BusinessDayStatus.Closing);
        BusinessDayRules.AssertTransition(BusinessDayStatus.Closing, BusinessDayStatus.Closed);

        var operationId = $"business-close-{day.Id}-r{day.CloseRevision + 1}";
        var now = DateTimeOffset.UtcNow;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _db.ControlAuditEvents.Add(new ControlAuditEventEntity
            {
                Id = $"audit-{Guid.NewGuid():N}", Action = "business_day_close_started", Actor = actorUser.Name,
                EntityId = day.Id, OperationId = operationId, CreatedAtUtc = now,
            });

            var blockers = await BusinessDayCloseEvaluator.EvaluateAsync(_db, day, ct);
            if (blockers.Count > 0)
            {
                _db.ControlAuditEvents.Add(new ControlAuditEventEntity
                {
                    Id = $"audit-{Guid.NewGuid():N}", Action = "business_day_close_blocked", Actor = actorUser.Name,
                    EntityId = day.Id, OperationId = operationId,
                    BlockerCodesJson = JsonSerializer.Serialize(blockers.Select(b => b.Code)), CreatedAtUtc = now,
                });
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                throw new BusinessDayCloseBlockedException(blockers.Select(b => new CloseBlockerDto(b.Code, b.EntityType, b.EntityId, b.Message)).ToList());
            }

            var reconciliation = await _reporting.GetBusinessDayReconciliationAsync(day.Id, ct);
            var exceptions = await BuildDayExceptionsAsync(day, ct);
            var completions = await _db.ChecklistCompletions.AsNoTracking().Where(c => c.BusinessDayId == day.Id).ToListAsync(ct);

            var revision = await _db.BusinessDayCloseSnapshots.CountAsync(s => s.BusinessDayId == day.Id, ct) + 1;
            await _db.BusinessDayCloseSnapshots.Where(s => s.BusinessDayId == day.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Authoritative, false), ct);

            var snapshot = new BusinessDayCloseSnapshotEntity
            {
                Id = snapshotId, BusinessDayId = day.Id, CloseRevision = revision, Authoritative = true,
                PreviousSnapshotId = day.SnapshotId, BusinessDate = day.BusinessDate, GeneratedAtUtc = now,
                ClosedBy = actorUser.Name, OperationId = operationId,
                ReconciliationJson = JsonSerializer.Serialize(reconciliation),
                ExceptionsJson = JsonSerializer.Serialize(exceptions),
                ChecklistJson = JsonSerializer.Serialize(completions.Select(c => new { c.ItemId, c.Completed, c.CompletedAtUtc, c.CompletedBy })),
            };
            _db.BusinessDayCloseSnapshots.Add(snapshot);

            day.Status = nameof(BusinessDayStatus.Closed);
            day.CloseRevision = revision;
            day.SnapshotId = snapshot.Id;
            day.ClosedAtUtc = now;
            day.ClosedBy = actorUser.Name;

            _db.ControlAuditEvents.Add(new ControlAuditEventEntity
            {
                Id = $"audit-{Guid.NewGuid():N}", Action = "business_day_closed", Actor = actorUser.Name,
                EntityId = day.Id, OperationId = operationId, ApprovedBy = authorization.Approver?.ApproverName, CreatedAtUtc = now,
            });

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToDto(snapshot, wasReplayed: false);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await _db.BusinessDayCloseSnapshots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == snapshotId, ct);
            if (replay is not null) return ToDto(replay, wasReplayed: true);
            throw;
        }
    }

    /// <summary>Day-scoped where the underlying record naturally is (shift discrepancy, refunds,
    /// expenses); unsettled custody and incomplete journal entries are checked system-wide, matching
    /// how EvaluateAsync's own close-blocker inputs are scoped - a stale unresolved item from a
    /// PRIOR day would already have blocked that day's own close, so in steady state this is
    /// equivalent, and system-wide is simpler and cannot under-count a real problem.</summary>
    private async Task<IReadOnlyList<ControlException>> BuildDayExceptionsAsync(BusinessDayEntity day, CancellationToken ct)
    {
        var dayShifts = await _db.Shifts.AsNoTracking()
            .Where(s => s.BusinessDate == day.BusinessDate && s.Status == "Closed" && s.CashDifferencePiastres != null)
            .Select(s => new { s.Id, Diff = s.CashDifferencePiastres!.Value }).ToListAsync(ct);
        var dayOrders = await _db.Orders.AsNoTracking()
            .Where(o => o.BusinessDayId == day.Id && o.RefundAmountPiastres > 0)
            .Select(o => new { o.Id, o.RefundAmountPiastres }).ToListAsync(ct);
        var dayExpenses = await _db.Expenses.AsNoTracking()
            .Where(e => e.BusinessDayId == day.Id).Select(e => new { e.Id, e.AmountPiastres }).ToListAsync(ct);
        var unsettledCustodyIds = await _db.DriverCustodies.AsNoTracking()
            .Where(c => c.Status != nameof(CustodyStatus.Settled)).Select(c => c.Id).ToListAsync(ct);
        var incompleteJournalIds = await _db.FinancialJournalEntries.AsNoTracking()
            .Where(j => j.Status != "Complete").Select(j => j.IdempotencyKey).ToListAsync(ct);

        var discrepancyThreshold = await Falah.Infrastructure.Shifts.PrepareShiftCloseHandler.ReadDiscrepancyThresholdAsync(_db, ct);
        var input = new ControlCenterRules.BuildExceptionsInputs(
            ClosedShiftsWithDifference: dayShifts.Select(s => (s.Id, s.Diff)).ToList(),
            ShiftDiscrepancyThresholdPiastres: discrepancyThreshold,
            RefundedOrders: dayOrders.Select(o => (o.Id, o.RefundAmountPiastres)).ToList(),
            Expenses: dayExpenses.Select(e => (e.Id, e.AmountPiastres)).ToList(),
            UnsettledCustodyIds: unsettledCustodyIds,
            IncompleteJournalIds: incompleteJournalIds);
        return ControlCenterRules.BuildExceptions(input);
    }

    internal static BusinessDayCloseSnapshotDto ToDto(BusinessDayCloseSnapshotEntity s, bool wasReplayed) => new(
        s.Id, s.BusinessDayId, s.CloseRevision, s.Authoritative, s.PreviousSnapshotId,
        s.BusinessDate.ToString("yyyy-MM-dd"), s.GeneratedAtUtc, s.ClosedBy, s.OperationId,
        s.ReconciliationJson, s.ExceptionsJson, s.ChecklistJson, wasReplayed);
}
