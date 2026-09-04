namespace Falah.Infrastructure.CashDrawer;

using Falah.Application.CashDrawer;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Shifts;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from addCashMovement's 'drawer_open' case - gated on `cash_drawer` (permission OR
/// a matching approval, matching the reference app's own authorizeCommand call), requires an open
/// shift exactly like the reference app's own `if (!activeShift) throw`. Never touches Treasury -
/// see CashDrawerOpenEntity's own doc comment for why this must never depend on, or be depended on
/// by, financial truth.</summary>
public sealed class OpenCashDrawerHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public OpenCashDrawerHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<CashDrawerOpenDto> HandleAsync(OpenCashDrawerRequest request, CancellationToken ct = default)
    {
        var id = $"cash-drawer-{request.RequestId}";
        var existing = await _db.CashDrawerOpens.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (existing is not null) return ToDto(existing);

        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب فتح الدرج مطلوب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
            ?? throw new InvalidOperationException("الوردية غير موجودة.");
        if (shift.Status != nameof(ShiftStatus.Open)) throw new DomainException("يجب فتح وردية قبل فتح درج النقدية.");

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "cash_drawer", request.Reason);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "cash_drawer", approval, requireApproval: true);

        var entity = new CashDrawerOpenEntity
        {
            Id = id, ShiftId = shift.Id, Reason = request.Reason.Trim(), RequestedBy = actorUser.Name,
            ApprovedBy = authorization.Approver?.ApproverName, Status = nameof(CashDrawerCommandStatus.Requested),
            RequestedAtUtc = DateTimeOffset.UtcNow,
        };
        try
        {
            _db.CashDrawerOpens.Add(entity);
            // Severity 'warning', matching the reference app's own choice for this exact action -
            // a drawer open with no attached sale is worth a manager's later glance, not a routine
            // 'normal' event, but nowhere near 'critical' either (no money has moved).
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "فتح درج النقدية", "cash", request.Reason.Trim(), "warning", entity.RequestedAtUtc,
                ShiftId: entity.ShiftId, AuthorizedBy: entity.ApprovedBy, ReferenceId: entity.Id));
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.CashDrawerOpens.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
            if (replay is null) throw;
            return ToDto(replay);
        }
        return ToDto(entity);
    }

    internal static CashDrawerOpenDto ToDto(CashDrawerOpenEntity e) =>
        new(e.Id, e.ShiftId, e.Reason, e.RequestedBy, e.ApprovedBy, e.Status, e.LastError, e.RequestedAtUtc);
}

/// <summary>Physical verification deferred until real hardware testing - see
/// CashDrawerOpenEntity's own doc comment. Mirrors ReportPrintJobResultHandler's shape: idempotent,
/// never touches anything beyond this one command's own status.</summary>
public sealed class ReportCashDrawerResultHandler
{
    private readonly FalahDbContext _db;
    public ReportCashDrawerResultHandler(FalahDbContext db) => _db = db;

    public async Task<CashDrawerOpenDto> HandleAsync(ReportCashDrawerResultRequest request, CancellationToken ct = default)
    {
        var entity = await _db.CashDrawerOpens.FirstOrDefaultAsync(c => c.Id == request.CashDrawerOpenId, ct)
            ?? throw new InvalidOperationException("طلب فتح الدرج غير موجود.");
        _ = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ReportedByUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");

        entity.Status = request.Succeeded ? nameof(CashDrawerCommandStatus.Succeeded) : nameof(CashDrawerCommandStatus.Failed);
        entity.LastError = request.Succeeded ? null : request.Error;
        entity.ReportedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return OpenCashDrawerHandler.ToDto(entity);
    }
}
