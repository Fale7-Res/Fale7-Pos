namespace Falah.Application.Approvals;

/// <summary>Action must match exactly what a later command (e.g. "orders_comp", "orders_refund")
/// will check for - an approval issued for one action can never authorize a different one.</summary>
/// <param name="Pin">The approving manager's own PIN, typed on the cashier's screen. This used to
/// be an ApproverUserId instead - the client verified the PIN itself and then told the server which
/// manager had approved, so anything that skipped the client could mint a manager approval by
/// naming a manager. The PIN is now the only thing that identifies the approver, and it is checked
/// here rather than trusted from elsewhere.</param>
public sealed record IssueManagerApprovalRequest(string Pin, string Action, string? Reason);

/// <param name="ApproverUserId">Who the PIN turned out to belong to. The caller no longer supplies
/// this, so the server has to hand it back - the command being approved records it alongside the
/// approval id.</param>
public sealed record IssueManagerApprovalResult(
    string ApprovalId, string ApproverUserId, string ApproverName, string Action, DateTimeOffset ApprovedAtUtc);
