using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Xunit;

namespace Falah.Domain.Tests;

public class AuthorizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 10, 0, 0, TimeSpan.Zero);

    private static Actor MakeActor(string role, bool active = true, params string[] permissions) =>
        new("u1", "Test User", role, active, permissions.ToHashSet());

    [Fact]
    public void Actor_holding_all_can_do_anything()
    {
        var actor = MakeActor("cashier", true, "all");
        Assert.True(AuthorizationPolicy.HasPermission(actor, "orders_refund"));
        Assert.True(AuthorizationPolicy.HasPermission(actor, "shifts_manage"));
    }

    [Fact]
    public void Inactive_actor_has_no_permission_even_with_all()
    {
        var actor = MakeActor("owner", active: false, "all");
        Assert.False(AuthorizationPolicy.HasPermission(actor, "orders_refund"));
    }

    [Fact]
    public void Cashier_without_refund_permission_cannot_authorize_a_refund_without_approval()
    {
        var registry = new InMemoryApprovalRegistry();
        var cashier = MakeActor("cashier", true, "pos");
        Assert.Throws<DomainException>(() =>
            AuthorizationPolicy.AuthorizeCommand(registry, cashier, "orders_refund"));
    }

    [Fact]
    public void A_valid_manager_approval_authorizes_the_exact_action_it_was_issued_for()
    {
        var registry = new InMemoryApprovalRegistry();
        var manager = MakeActor("manager", true, "all");
        var cashier = MakeActor("cashier", true, "pos");

        var approval = AuthorizationPolicy.IssueManagerApproval(registry, manager, "orders_refund", "عميل غير راضٍ", Now);
        var result = AuthorizationPolicy.AuthorizeCommand(registry, cashier, "orders_refund", approval, requireApproval: true);

        Assert.Equal(manager.Name, result.Approver!.ApproverName);
    }

    [Fact]
    public void A_consumed_approval_cannot_authorize_a_second_command()
    {
        var registry = new InMemoryApprovalRegistry();
        var manager = MakeActor("manager", true, "all");
        var cashier = MakeActor("cashier", true, "pos");
        var approval = AuthorizationPolicy.IssueManagerApproval(registry, manager, "orders_refund", null, Now);

        AuthorizationPolicy.AuthorizeCommand(registry, cashier, "orders_refund", approval, requireApproval: true);

        Assert.Throws<DomainException>(() =>
            AuthorizationPolicy.AuthorizeCommand(registry, cashier, "orders_refund", approval, requireApproval: true));
    }

    [Fact]
    public void An_approval_issued_for_a_different_action_cannot_be_reused()
    {
        var registry = new InMemoryApprovalRegistry();
        var manager = MakeActor("manager", true, "all");
        var cashier = MakeActor("cashier", true, "pos");
        var approval = AuthorizationPolicy.IssueManagerApproval(registry, manager, "orders_comp", null, Now);

        Assert.Throws<DomainException>(() =>
            AuthorizationPolicy.AuthorizeCommand(registry, cashier, "orders_refund", approval, requireApproval: true));
    }

    [Fact]
    public void A_regular_cashier_cannot_issue_a_manager_approval_for_themselves()
    {
        var registry = new InMemoryApprovalRegistry();
        var cashier = MakeActor("cashier", true, "pos");
        Assert.Throws<DomainException>(() =>
            AuthorizationPolicy.IssueManagerApproval(registry, cashier, "orders_refund", null, Now));
    }

    [Fact]
    public void Shift_close_with_no_discrepancy_needs_only_the_base_permission()
    {
        var registry = new InMemoryApprovalRegistry();
        var cashier = MakeActor("cashier", true, "shifts_blind_close");
        AuthorizationPolicy.AuthorizeShiftClose(registry, cashier, approval: null, discrepancyNeedsManager: false);
    }

    [Fact]
    public void Shift_close_with_a_discrepancy_is_blocked_without_a_manager_approval_even_for_a_manager_role_string()
    {
        var registry = new InMemoryApprovalRegistry();
        // Holds the ROLE "manager" as a label but was stripped of shifts_manage from Users -
        // revocation must actually take effect, not be bypassed by the role name.
        var strippedManager = MakeActor("manager", true, "shifts_blind_close");
        Assert.Throws<DomainException>(() =>
            AuthorizationPolicy.AuthorizeShiftClose(registry, strippedManager, approval: null, discrepancyNeedsManager: true));
    }
}
