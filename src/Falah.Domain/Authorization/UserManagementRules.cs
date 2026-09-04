namespace Falah.Domain.Authorization;

using Falah.Domain.Common;

/// <summary>Fixes a real, documented legacy gap: `updateUser` in lib/store.tsx has no
/// owner-immutability check at all - the ONLY place `role === 'owner'` is ever checked is inside
/// UsersPermissionsScreen.tsx (an early-return in one click handler, and a disabled button), both
/// trivially bypassable by any caller with `users_manage` invoking the underlying command directly.
/// This system enforces owner-immutability and no-self-escalation at the handler/domain level
/// instead, per IMPLEMENTATION_MATRIX.md row 4b's own stated fix target.</summary>
public static class UserManagementRules
{
    public const string OwnerRole = "owner";

    /// <summary>The owner account can never be deactivated, reassigned a role, or have its
    /// permissions changed through the administrative user-management commands - not even by
    /// another `users_manage` holder. (The owner MAY still change their own PIN - that path never
    /// calls this assertion, since it authorizes by "you proved you know your own current PIN," not
    /// by `users_manage`.)</summary>
    public static void AssertTargetIsNotOwner(string targetRole, string action)
    {
        if (targetRole == OwnerRole)
            throw new DomainException($"لا يمكن {action} حساب المالك.");
    }

    /// <summary>An actor can never grant a permission they do not themselves hold (including "all"
    /// itself) - a standard privilege-escalation guard, not a restaurant-specific policy. Revoking is
    /// deliberately NOT covered by this check - taking a permission away is never an escalation.</summary>
    public static void AssertNoPrivilegeEscalation(IReadOnlySet<string> actorPermissions, IEnumerable<string> permissionsToGrant)
    {
        if (actorPermissions.Contains("all")) return;
        var notHeld = permissionsToGrant.Where(p => !actorPermissions.Contains(p)).ToList();
        if (notHeld.Count > 0)
            throw new DomainException($"لا يمكنك منح صلاحيات لا تملكها بنفسك: {string.Join(", ", notHeld)}.");
    }

    public static void AssertKnownPermissions(IEnumerable<string> permissions)
    {
        var unknown = permissions.Where(p => !PermissionCatalog.Keys.Contains(p)).ToList();
        if (unknown.Count > 0)
            throw new DomainException($"صلاحيات غير معروفة: {string.Join(", ", unknown)}.");
    }
}
