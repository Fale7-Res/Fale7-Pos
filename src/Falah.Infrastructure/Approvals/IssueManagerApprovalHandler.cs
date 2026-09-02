namespace Falah.Infrastructure.Approvals;

using Falah.Application.Approvals;
using Falah.Domain.Authorization;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Issues a single-use manager approval and persists it immediately - a manager approves
/// in one request (at the till, on their own PIN), and a cashier's later checkout/refund request
/// consumes it in a completely separate request. Both must see the SAME committed row regardless
/// of which one runs first in wall-clock time relative to a Server restart in between.</summary>
public sealed class IssueManagerApprovalHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public IssueManagerApprovalHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<IssueManagerApprovalResult> HandleAsync(IssueManagerApprovalRequest request, CancellationToken ct = default)
    {
        var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ApproverUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var approver = OrderMapper.ToActor(approverUser);

        var approval = AuthorizationPolicy.IssueManagerApproval(_approvalRegistry, approver, request.Action, request.Reason, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(ct);

        return new IssueManagerApprovalResult(approval.ApprovalId, approval.ApproverName, approval.Action, approval.ApprovedAtUtc);
    }
}
