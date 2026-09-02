namespace Falah.Domain.Authorization;

using Falah.Domain.Common;

/// <summary>
/// Tracks issued approvals and which ones have already been consumed, so a token can never
/// authorize a second command. Ported from the module-level Maps in lib/authorization.mjs.
/// Production wires a server-side singleton (the Cashier/Call Center split means only the Cashier
/// device ever issues or consumes approvals today, since Call Center holds no financial authority).
/// </summary>
public interface IApprovalRegistry
{
    void Register(ManagerApproval approval);
    ManagerApproval? Find(string approvalId);
    bool IsConsumed(string approvalId);
    void MarkConsumed(string approvalId);
}

public sealed class InMemoryApprovalRegistry : IApprovalRegistry
{
    private readonly Dictionary<string, ManagerApproval> _issued = new();
    private readonly HashSet<string> _consumed = new();

    public void Register(ManagerApproval approval) => _issued[approval.ApprovalId] = approval;
    public ManagerApproval? Find(string approvalId) => _issued.GetValueOrDefault(approvalId);
    public bool IsConsumed(string approvalId) => _consumed.Contains(approvalId);
    public void MarkConsumed(string approvalId) => _consumed.Add(approvalId);
}

/// <summary>
/// Ported from lib/authorization.mjs (hasActorPermission, issueManagerApproval, authorizeCommand,
/// authorizeShiftClose). A permission is a plain string the actor's list either contains, or the
/// actor holds the literal "all" - there is no separate role-based bypass anywhere in this policy.
/// </summary>
public static class AuthorizationPolicy
{
    public static bool HasPermission(Actor? actor, string permission) =>
        actor is { Active: true } && (actor.Permissions.Contains("all") || actor.Permissions.Contains(permission));

    public static ManagerApproval IssueManagerApproval(
        IApprovalRegistry registry, Actor approver, string action, string? reason, DateTimeOffset nowUtc)
    {
        var allowedRole = approver.Role is "owner" or "manager"
            || (approver.Role == "supervisor" && HasPermission(approver, action));
        if (!allowedRole || !approver.Active)
            throw new DomainException("هذا المستخدم غير مخول لاعتماد العملية.");

        var approval = new ManagerApproval(
            ApprovalId: $"approval-{Guid.NewGuid()}",
            ApproverUserId: approver.Id,
            ApproverName: approver.Name,
            ApproverRole: approver.Role,
            ApprovedAtUtc: nowUtc,
            Action: action,
            Reason: reason);
        registry.Register(approval);
        return approval;
    }

    public readonly record struct AuthorizationResult(Actor Actor, ManagerApproval? Approver);

    public static AuthorizationResult AuthorizeCommand(
        IApprovalRegistry registry, Actor actor, string permission,
        ManagerApproval? approval = null, bool requireApproval = false, bool consumeApproval = true)
    {
        if (HasPermission(actor, permission)) return new AuthorizationResult(actor, null);
        if (!requireApproval || approval is null)
            throw new DomainException("ليس لديك صلاحية تنفيذ هذه العملية.");

        var issued = registry.Find(approval.ApprovalId);
        if (issued is null || issued.Action != permission || issued.ApproverUserId != approval.ApproverUserId || registry.IsConsumed(approval.ApprovalId))
            throw new DomainException("اعتماد المدير غير صالح أو لا يطابق العملية.");

        if (consumeApproval) registry.MarkConsumed(approval.ApprovalId);
        return new AuthorizationResult(actor, issued);
    }

    /// <summary>
    /// A discrepancy-gated shift close needs `shifts_manage` (or a matching approval) ON TOP of the
    /// base `shifts_blind_close` permission everyone who can close a shift already holds. There is
    /// deliberately no `actor.Role == "manager"` shortcut here: that would let a user keep closing
    /// with discrepancies after an admin revoked their shifts_manage permission from Users.
    /// </summary>
    public static void AuthorizeShiftClose(
        IApprovalRegistry registry, Actor actor, ManagerApproval? approval, bool discrepancyNeedsManager)
    {
        AuthorizeCommand(registry, actor, "shifts_blind_close");
        if (discrepancyNeedsManager)
            AuthorizeCommand(registry, actor, "shifts_manage", approval, requireApproval: true);
    }
}
