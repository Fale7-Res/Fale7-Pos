namespace Falah.Infrastructure.ControlCenter;

using Falah.Application.BusinessDay;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from setExceptionStatus. Exceptions themselves are never stored (see
/// ControlCenterRules.BuildExceptions' own doc comment) - this only persists a human's
/// acknowledge/resolve action against one, keyed by its own deterministic id, and always requires a
/// typed reason, matching every other reviewed-action gate in this codebase.</summary>
public sealed class SetExceptionStatusHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public SetExceptionStatusHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task HandleAsync(SetExceptionStatusRequest request, CancellationToken ct = default)
    {
        if (request.Status is not ("acknowledged" or "resolved")) throw new DomainException("حالة المراجعة غير صالحة.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب إجراء المراجعة مطلوب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "control_center", request.Reason);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "control_center", approval, requireApproval: true);

        var now = DateTimeOffset.UtcNow;
        var existing = await _db.ControlExceptionStates.FirstOrDefaultAsync(e => e.Id == request.ExceptionId, ct);
        if (existing is null)
        {
            _db.ControlExceptionStates.Add(new ControlExceptionStateEntity
            {
                Id = request.ExceptionId, Status = request.Status, Actor = actor.Name, Reason = request.Reason.Trim(), TimestampUtc = now,
            });
        }
        else
        {
            existing.Status = request.Status;
            existing.Actor = actor.Name;
            existing.Reason = request.Reason.Trim();
            existing.TimestampUtc = now;
        }

        _db.ControlAuditEvents.Add(new ControlAuditEventEntity
        {
            Id = $"audit-{Guid.NewGuid():N}", Action = request.Status == "resolved" ? "exception_resolved" : "exception_acknowledged",
            Actor = actor.Name, EntityId = request.ExceptionId, Reason = request.Reason.Trim(),
            ApprovedBy = authorization.Approver?.ApproverName, CreatedAtUtc = now,
        });

        await _db.SaveChangesAsync(ct);
    }
}
