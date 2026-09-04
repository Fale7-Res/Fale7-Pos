namespace Falah.Infrastructure.ControlCenter;

using Falah.Application.BusinessDay;
using Falah.Domain.Authorization;
using Falah.Domain.BusinessDay;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from reopenBusinessDay in lib/enterprise-store.tsx. Explicit and permissioned:
/// gated on business_day_reopen, same permission-OR-matching-approval shape as every other gate in
/// this codebase (AuthorizationPolicy.AuthorizeCommand) - an actor holding the permission directly
/// needs no approval at all, exactly like the reference app's own authorizeCommand call for this
/// action. Every attempt - successful or blocked - is audited, matching the reference app's own
/// try/catch around this command.</summary>
public sealed class ReopenBusinessDayHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public ReopenBusinessDayHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<BusinessDayDto> HandleAsync(ReopenBusinessDayRequest request, CancellationToken ct = default)
    {
        var day = await _db.BusinessDays.FirstOrDefaultAsync(d => d.Id == request.BusinessDayId, ct)
            ?? throw new InvalidOperationException("يوم العمل غير موجود.");

        // A retried reopen (network retry of the same click) must not fail BusinessDayRules.Reopen's
        // own "day must be Closed" guard just because the FIRST attempt already succeeded.
        if (day.Status == nameof(BusinessDayStatus.Reopened) && day.ReopenRequestId == request.RequestId)
            return ToDto(day);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        var now = DateTimeOffset.UtcNow;
        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, now, "business_day_reopen", request.Reason);
        }

        var attemptedAudit = new ControlAuditEventEntity
        {
            Id = $"audit-{Guid.NewGuid():N}", Action = "business_day_reopen_attempted", Actor = actorUser.Name,
            EntityId = day.Id, Reason = request.Reason, CreatedAtUtc = now,
        };

        try
        {
            var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "business_day_reopen", approval, requireApproval: true);

            // Only "is any OTHER day active" matters to Reopen's own guard - the current day's own
            // record fields beyond Id/Status are irrelevant to that check.
            var otherDayEntities = await _db.BusinessDays.AsNoTracking().Where(d => d.Id != day.Id).ToListAsync(ct);
            var otherDays = otherDayEntities
                .Select(d => new BusinessDayRecord(d.Id, d.BusinessDate, d.OpenedBy, d.OpenedAtUtc, Enum.Parse<BusinessDayStatus>(d.Status), d.CloseRevision))
                .ToList();
            var currentRecord = new BusinessDayRecord(day.Id, day.BusinessDate, day.OpenedBy, day.OpenedAtUtc, Enum.Parse<BusinessDayStatus>(day.Status), day.CloseRevision, day.SnapshotId);

            var reopened = BusinessDayRules.Reopen(currentRecord, otherDays, request.Reason, authorization.Approver?.ApproverName ?? actor.Name, now);

            day.Status = nameof(BusinessDayStatus.Reopened);
            day.PreviousSnapshotId = day.SnapshotId;
            day.PreviousClosedAtUtc = day.ClosedAtUtc;
            day.PreviousClosedBy = day.ClosedBy;
            day.ReopenReason = reopened.ReopenReason;
            day.ReopenedBy = actor.Name;
            day.ReopenedAtUtc = now;
            day.ReopenApprovalId = request.ManagerApprovalId;
            day.ReopenApprovedBy = authorization.Approver?.ApproverName;
            day.ReopenRequestId = request.RequestId;

            // Reopening wipes the CLOSING checklist for this day - it must be redone before the day
            // can close again, exactly like the reference app's own reopenBusinessDay.
            var closingItemIds = await _db.ChecklistItems.AsNoTracking().Where(i => i.Phase == "Closing").Select(i => i.Id).ToListAsync(ct);
            var closingCompletions = await _db.ChecklistCompletions.Where(c => c.BusinessDayId == day.Id && closingItemIds.Contains(c.ItemId)).ToListAsync(ct);
            _db.ChecklistCompletions.RemoveRange(closingCompletions);

            _db.ControlAuditEvents.Add(attemptedAudit);
            _db.ControlAuditEvents.Add(new ControlAuditEventEntity
            {
                Id = $"audit-{Guid.NewGuid():N}", Action = "business_day_reopened", Actor = actorUser.Name,
                EntityId = day.Id, Reason = request.Reason, ApprovedBy = authorization.Approver?.ApproverName, CreatedAtUtc = now,
            });

            await _db.SaveChangesAsync(ct);
            return ToDto(day);
        }
        catch (DomainException ex)
        {
            _db.ControlAuditEvents.Add(attemptedAudit);
            _db.ControlAuditEvents.Add(new ControlAuditEventEntity
            {
                Id = $"audit-{Guid.NewGuid():N}", Action = "business_day_reopen_blocked", Actor = actorUser.Name,
                EntityId = day.Id, Reason = ex.Message, CreatedAtUtc = now,
            });
            await _db.SaveChangesAsync(ct);
            throw;
        }
    }

    private static BusinessDayDto ToDto(BusinessDayEntity d) => new(
        d.Id, d.BusinessDate.ToString("yyyy-MM-dd"), d.Status, d.CloseRevision, d.SnapshotId,
        d.ReopenReason, d.ReopenedBy, d.ReopenedAtUtc, d.ReopenApprovedBy);
}
