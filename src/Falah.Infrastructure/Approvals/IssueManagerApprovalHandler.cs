namespace Falah.Infrastructure.Approvals;

using Falah.Application.Approvals;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Issues a single-use manager approval and persists it immediately - a manager approves
/// in one request (at the till, on their own PIN), and a cashier's later checkout/refund request
/// consumes it in a completely separate request. Both must see the SAME committed row regardless
/// of which one runs first in wall-clock time relative to a Server restart in between.</summary>
public sealed class IssueManagerApprovalHandler
{
    // One answer for every PIN that does not approve, whoever it belongs to: telling a guesser that
    // a PIN is real but not a manager's hands them a cashier's or supervisor's login.
    private const string PinRefused = "رمز المدير غير صحيح أو غير مخوّل باعتماد هذا الإجراء.";

    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly ApprovalPinAttemptGuard _guard;

    public IssueManagerApprovalHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, ApprovalPinAttemptGuard guard)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _guard = guard;
    }

    public async Task<IssueManagerApprovalResult> HandleAsync(IssueManagerApprovalRequest request, ApprovalCaller caller, CancellationToken ct = default)
    {
        // Before any PIN is looked at, so a request without an action is neither a guess nor a way
        // to learn whether a PIN is right.
        AuthorizationPolicy.EnsureApprovalAction(request.Action);

        await _guard.EnsureLoadedAsync(_db, ct);
        var attempt = _guard.TryBegin(caller, out var retryAfter)
            ?? throw new ApprovalGuessingLimitException(retryAfter);

        // The PIN establishes who is approving. Before this, the caller named the approver and the
        // PIN was checked in the client - so anything that bypassed the client could mint a manager
        // approval for itself simply by naming a manager.
        //
        // Every active user who could approve this action is tried, because a PIN carries no
        // username: the whole approval gesture is a manager typing four digits on someone else's
        // screen. Each PIN is hashed with its own salt, so there is nothing to look up by - the same
        // shape the login screen already has. Users who could not approve are never compared at all.
        var candidates = await _db.Users.AsNoTracking().Where(u => u.Active).ToListAsync(ct);
        var approverUser = candidates.FirstOrDefault(u =>
            AuthorizationPolicy.CanIssueApproval(OrderMapper.ToActor(u), request.Action)
            && PinHasher.Verify(request.Pin ?? string.Empty, u.PinHash, u.PinSalt, u.PinIterations));
        if (approverUser is null)
        {
            // The guess itself is never written down - only who sent it, and for what.
            _db.ControlAuditEvents.Add(new ControlAuditEventEntity
            {
                Id = $"audit-{Guid.NewGuid():N}", Action = ApprovalPinAttemptGuard.FailedAuditAction, Actor = caller.UserName,
                EntityId = caller.UserId, Reason = request.Action, CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await _db.SaveChangesAsync(ct);
            throw new DomainException(PinRefused);
        }
        attempt.Succeeded();
        var approver = OrderMapper.ToActor(approverUser);

        // Bound to the session that asked: only this caller can spend it (ValidateApproval).
        var approval = AuthorizationPolicy.IssueManagerApproval(_approvalRegistry, approver, request.Action, request.Reason, _approvalRegistry.UtcNow, caller.UserId);
        await _db.SaveChangesAsync(ct);

        return new IssueManagerApprovalResult(approval.ApprovalId, approval.ApproverUserId, approval.ApproverName, approval.Action, approval.ApprovedAtUtc);
    }
}
