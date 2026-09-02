namespace Falah.Application.Approvals;

/// <summary>Action must match exactly what a later command (e.g. "orders_comp", "orders_refund")
/// will check for - an approval issued for one action can never authorize a different one.</summary>
public sealed record IssueManagerApprovalRequest(string ApproverUserId, string Action, string? Reason);

public sealed record IssueManagerApprovalResult(string ApprovalId, string ApproverName, string Action, DateTimeOffset ApprovedAtUtc);
