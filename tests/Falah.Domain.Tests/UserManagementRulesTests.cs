using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Xunit;

namespace Falah.Domain.Tests;

public class UserManagementRulesTests
{
    [Fact]
    public void The_owner_role_is_immune_to_administrative_actions()
    {
        Assert.Throws<DomainException>(() => UserManagementRules.AssertTargetIsNotOwner("owner", "تعطيل"));
    }

    [Fact]
    public void A_non_owner_role_passes_the_owner_immutability_check()
    {
        UserManagementRules.AssertTargetIsNotOwner("manager", "تعطيل");
        UserManagementRules.AssertTargetIsNotOwner("cashier", "تعطيل");
    }

    [Fact]
    public void An_actor_holding_all_can_grant_any_permission_including_all_itself()
    {
        var actorPermissions = new HashSet<string> { "all" };
        UserManagementRules.AssertNoPrivilegeEscalation(actorPermissions, new[] { "all", "inventory_manage", "users_manage" });
    }

    [Fact]
    public void An_actor_cannot_grant_a_permission_they_do_not_hold()
    {
        var actorPermissions = new HashSet<string> { "menu_manage", "inventory_manage" };
        Assert.Throws<DomainException>(() => UserManagementRules.AssertNoPrivilegeEscalation(actorPermissions, new[] { "treasury_manage" }));
    }

    [Fact]
    public void An_actor_can_grant_a_permission_they_already_hold_themselves()
    {
        var actorPermissions = new HashSet<string> { "menu_manage", "inventory_manage" };
        UserManagementRules.AssertNoPrivilegeEscalation(actorPermissions, new[] { "menu_manage" });
    }

    [Fact]
    public void An_actor_without_all_can_never_grant_all_even_if_they_hold_every_individual_permission()
    {
        var actorPermissions = new HashSet<string>(PermissionCatalog.Keys.Where(k => k != "all"));
        Assert.Throws<DomainException>(() => UserManagementRules.AssertNoPrivilegeEscalation(actorPermissions, new[] { "all" }));
    }

    [Fact]
    public void Assigning_an_unknown_permission_string_is_rejected()
    {
        Assert.Throws<DomainException>(() => UserManagementRules.AssertKnownPermissions(new[] { "not_a_real_permission" }));
    }

    [Fact]
    public void Every_catalog_permission_is_accepted_as_known()
    {
        UserManagementRules.AssertKnownPermissions(PermissionCatalog.Keys);
    }

    // ---- Administrative lockout ----

    [Fact]
    public void All_counts_as_being_able_to_administer_users()
    {
        // Mirrors the authorization policy itself, where "all" satisfies every permission check -
        // counting an owner-equivalent as unable would block changes that are perfectly safe.
        Assert.True(UserManagementRules.CanManageUsers(new[] { "all" }));
        Assert.True(UserManagementRules.CanManageUsers(new[] { "pos", "users_manage" }));
        Assert.False(UserManagementRules.CanManageUsers(new[] { "pos", "reports_view" }));
        Assert.False(UserManagementRules.CanManageUsers(Array.Empty<string>()));
    }

    [Fact]
    public void Leaving_nobody_able_to_administer_users_is_refused()
    {
        // Unrecoverable from inside the product: no break-glass path exists, so the only way back
        // would be editing the database by hand.
        Assert.Throws<DomainException>(() => UserManagementRules.AssertUserAdministrationSurvives(0));
        Assert.Throws<DomainException>(() => UserManagementRules.AssertUserAdministrationSurvives(-1));
    }

    [Fact]
    public void Leaving_one_administrator_behind_is_allowed()
    {
        // One is enough. Requiring two would block a single-manager restaurant from ever tidying
        // permissions at all, which is the normal shape of the business this runs in.
        UserManagementRules.AssertUserAdministrationSurvives(1);
    }
}