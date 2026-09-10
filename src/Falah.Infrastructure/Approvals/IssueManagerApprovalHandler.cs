namespace Falah.Infrastructure.Approvals;

using Falah.Application.Approvals;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
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
        // The PIN establishes who is approving. Before this, the caller named the approver and the
        // PIN was checked in the client - so anything that bypassed the client could mint a manager
        // approval for itself simply by naming a manager.
        //
        // Every active user is loaded because a PIN carries no username: the whole approval gesture
        // is a manager typing four digits on someone else's screen. Each PIN is hashed with its own
        // salt, so there is nothing to look up by - the same shape the login screen already has.
        var candidates = await _db.Users.AsNoTracking().Where(u => u.Active).ToListAsync(ct);
        var approverUser = candidates.FirstOrDefault(u =>
            PinHasher.Verify(request.Pin ?? string.Empty, u.PinHash, u.PinSalt, u.PinIterations))
            ?? throw new DomainException("رمز المدير غير صحيح.");
        var approver = OrderMapper.ToActor(approverUser);

        var approval = AuthorizationPolicy.IssueManagerApproval(_approvalRegistry, approver, request.Action, request.Reason, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(ct);

        return new IssueManagerApprovalResult(approval.ApprovalId, approval.ApproverUserId, approval.ApproverName, approval.Action, approval.ApprovedAtUtc);
    }
}
