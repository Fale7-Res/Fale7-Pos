namespace Falah.Domain.Authorization;

/// <summary>
/// A single-use token proving a specific manager/owner (or a supervisor who separately holds the
/// permission) approved one specific action, once. It is never a standing power - see
/// IApprovalRegistry, which enforces that a consumed approval can never authorize a second command.
///
/// <paramref name="RequestedByUserId"/> is the user whose session asked for it (the cashier at whose
/// till the manager typed their PIN); only that user can spend it. Null on approvals issued before
/// this was recorded, and on approvals issued outside a session (unit tests).
/// </summary>
public sealed record ManagerApproval(
    string ApprovalId,
    string ApproverUserId,
    string ApproverName,
    string ApproverRole,
    DateTimeOffset ApprovedAtUtc,
    string Action,
    string? Reason,
    string? RequestedByUserId = null);
