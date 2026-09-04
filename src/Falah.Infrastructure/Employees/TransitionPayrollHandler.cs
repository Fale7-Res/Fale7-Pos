namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Payroll;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from transitionPayroll in lib/enterprise-store.tsx - see
/// TransitionPayrollRequest's own doc comment for the base+stacked gate shape.</summary>
public sealed class TransitionPayrollHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public TransitionPayrollHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<PayrollRunDto> HandleAsync(TransitionPayrollRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<PayrollRunStatus>(request.Status, ignoreCase: true, out var targetStatus))
            throw new DomainException("حالة Payroll غير صالحة.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "employees_manage");

        var run = await _db.PayrollRuns.FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, ct)
            ?? throw new InvalidOperationException("مسير الرواتب غير موجود.");
        var currentStatus = Enum.Parse<PayrollRunStatus>(run.Status);
        PayrollRules.AssertTransition(currentStatus, targetStatus);

        string? approvedByName = null;
        if (targetStatus == PayrollRunStatus.Approved)
        {
            ManagerApproval? approval = null;
            if (!string.IsNullOrEmpty(request.ManagerApprovalId))
            {
                var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                    ?? throw new DomainException("المعتمد غير موجود.");
                approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                    approverUser.Role, DateTimeOffset.UtcNow, "payroll_approve", "اعتماد Payroll");
            }
            var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "payroll_approve", approval, requireApproval: true);
            approvedByName = authorization.Approver?.ApproverName ?? actor.Name;
        }

        var now = DateTimeOffset.UtcNow;
        run.Status = targetStatus.ToString();
        if (targetStatus == PayrollRunStatus.Approved) run.ApprovedBy = approvedByName;
        if (targetStatus == PayrollRunStatus.Reviewed) { run.ReviewedBy = actor.Name; run.ReviewedAtUtc = now; }

        await _db.SaveChangesAsync(ct);
        return CreatePayrollHandler.ToDto(run, wasReplayed: false);
    }
}
