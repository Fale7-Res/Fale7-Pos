namespace Falah.Domain.Authorization;

/// <summary>
/// A single-use token proving a specific manager/owner (or a supervisor who separately holds the
/// permission) approved one specific action, once. It is never a standing power - see
/// IApprovalRegistry, which enforces that a consumed approval can never authorize a second command.
/// </summary>
public sealed record ManagerApproval(
    string ApprovalId,
    string ApproverUserId,
    string ApproverName,
    string ApproverRole,
    DateTimeOffset ApprovedAtUtc,
    string Action,
    string? Reason);
