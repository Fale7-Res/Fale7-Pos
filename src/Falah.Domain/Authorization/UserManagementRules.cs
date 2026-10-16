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

    /// <summary>The permission that lets anyone administer users at all. Losing every active holder
    /// of it is unrecoverable from inside the product: there is no break-glass path, so the only way
    /// back would be editing the database by hand.</summary>
    public const string UserManagementPermission = "users_manage";

    /// <summary>True when this permission set can administer users - "all" counts, exactly as it
    /// does in the authorization policy itself, so a holder of "all" is never miscounted as unable.</summary>
    public static bool CanManageUsers(IEnumerable<string> permissions)
    {
        var set = permissions as IReadOnlySet<string> ?? permissions.ToHashSet();
        return set.Contains("all") || set.Contains(UserManagementPermission);
    }

    /// <summary>Refuses any change that would leave nobody able to administer users.
    /// <para>SetUserActiveHandler already stops an admin deactivating their own account, but that
    /// guard does not cover the two ways the door still closes: revoking users_manage from the last
    /// holder (revocation is deliberately exempt from the escalation check, since taking a
    /// permission away is never an escalation), and deactivating the last holder while acting on a
    /// one-off manager approval rather than on a permission of your own. Either one leaves a till
    /// nobody can ever add a cashier to again.</para></summary>
    public static void AssertUserAdministrationSurvives(int remainingActiveAdministrators)
    {
        if (remainingActiveAdministrators <= 0)
            throw new DomainException(
                "لا يمكن تنفيذ العملية: لن يتبقى أي مستخدم نشط يملك صلاحية إدارة المستخدمين، "
                + "ولن يكون بالإمكان إضافة أو تعديل أي حساب بعدها.");
    }

    public static void AssertKnownPermissions(IEnumerable<string> permissions)
    {
        var unknown = permissions.Where(p => !PermissionCatalog.Keys.Contains(p)).ToList();
        if (unknown.Count > 0)
            throw new DomainException($"صلاحيات غير معروفة: {string.Join(", ", unknown)}.");
    }
}
