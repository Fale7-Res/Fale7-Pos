namespace Falah.Domain.Tests;

using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Xunit;

/// <summary>SEC3: an approval is spent by the session that asked for it, within its lifetime.</summary>
public class ApprovalLifecycleRulesTests
{
    private static readonly DateTimeOffset Issued = new(2026, 10, 26, 18, 0, 0, TimeSpan.Zero);
    private static readonly Actor Manager = new("user-manager", "المدير", "manager", true, new HashSet<string> { "all" });

    private static (InMemoryApprovalRegistry Registry, Func<DateTimeOffset, DateTimeOffset> SetNow) Registry(TimeSpan? lifetime)
    {
        var now = Issued;
        var registry = new InMemoryApprovalRegistry { ApprovalLifetime = lifetime, Clock = () => now };
        return (registry, value => now = value);
    }

    [Fact]
    public void The_cashier_who_asked_can_spend_it()
    {
        var (registry, _) = Registry(TimeSpan.FromMinutes(15));
        var approval = AuthorizationPolicy.IssueManagerApproval(registry, Manager, "orders_refund", null, Issued, requestedByUserId: "user-cashier");

        Assert.Equal(approval.ApprovalId, AuthorizationPolicy.ValidateApproval(registry, "orders_refund", approval, "user-cashier").ApprovalId);
        Assert.True(registry.IsConsumed(approval.ApprovalId));
    }

    [Fact]
    public void Another_user_cannot_and_does_not_spend_it_by_trying()
    {
        var (registry, _) = Registry(TimeSpan.FromMinutes(15));
        var approval = AuthorizationPolicy.IssueManagerApproval(registry, Manager, "orders_refund", null, Issued, requestedByUserId: "user-cashier");

        var refused = Assert.Throws<DomainException>(() => AuthorizationPolicy.ValidateApproval(registry, "orders_refund", approval, "user-other"));
        Assert.Equal(AuthorizationPolicy.ApprovalInvalidMessage, refused.Message);
        Assert.False(registry.IsConsumed(approval.ApprovalId));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(15 * 60, true)]       // exactly at the lifetime: still good
    [InlineData(15 * 60 + 1, false)]  // one second later: expired
    public void It_is_spendable_for_its_lifetime_and_not_after(int secondsLater, bool accepted)
    {
        var (registry, setNow) = Registry(TimeSpan.FromMinutes(15));
        var approval = AuthorizationPolicy.IssueManagerApproval(registry, Manager, "orders_comp", null, Issued, requestedByUserId: "user-cashier");
        setNow(Issued.AddSeconds(secondsLater));

        if (accepted)
        {
            AuthorizationPolicy.ValidateApproval(registry, "orders_comp", approval, "user-cashier");
        }
        else
        {
            var refused = Assert.Throws<DomainException>(() => AuthorizationPolicy.ValidateApproval(registry, "orders_comp", approval, "user-cashier"));
            Assert.Equal(AuthorizationPolicy.ApprovalExpiredMessage, refused.Message);
            Assert.False(registry.IsConsumed(approval.ApprovalId));
        }
    }

    [Fact]
    public void An_approval_recorded_before_requesters_were_kept_is_not_bound_to_anyone()
    {
        var (registry, _) = Registry(TimeSpan.FromMinutes(15));
        var legacy = AuthorizationPolicy.IssueManagerApproval(registry, Manager, "orders_comp", null, Issued);

        AuthorizationPolicy.ValidateApproval(registry, "orders_comp", legacy, "user-anyone");
    }

    [Fact]
    public void Someone_elses_expired_approval_says_only_that_it_is_not_valid()
    {
        var (registry, setNow) = Registry(TimeSpan.FromMinutes(15));
        var approval = AuthorizationPolicy.IssueManagerApproval(registry, Manager, "orders_comp", null, Issued, requestedByUserId: "user-cashier");
        setNow(Issued.AddHours(2));

        var refused = Assert.Throws<DomainException>(() => AuthorizationPolicy.ValidateApproval(registry, "orders_comp", approval, "user-other"));
        Assert.Equal(AuthorizationPolicy.ApprovalInvalidMessage, refused.Message);
    }

    [Fact]
    public void A_permission_gate_binds_the_approval_to_the_acting_user()
    {
        var (registry, _) = Registry(TimeSpan.FromMinutes(15));
        var approval = AuthorizationPolicy.IssueManagerApproval(registry, Manager, "orders_refund", null, Issued, requestedByUserId: "user-cashier");
        var other = new Actor("user-other", "كاشير آخر", "cashier", true, new HashSet<string> { "pos" });

        Assert.Throws<DomainException>(() => AuthorizationPolicy.AuthorizeCommand(registry, other, "orders_refund", approval, requireApproval: true));
    }
}
