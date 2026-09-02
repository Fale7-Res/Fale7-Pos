using Falah.Domain.Common;
using Falah.Domain.Financial;
using Xunit;

namespace Falah.Domain.Tests;

public class FinancialRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Split_allocation_matching_the_total_exactly_is_accepted()
    {
        var splits = new[] { new PaymentSplit(PaymentMethod.Cash, Money.FromEgp(60m)), new PaymentSplit(PaymentMethod.Card, Money.FromEgp(40m)) };
        FinancialRules.AssertSplitPaymentAllocation(splits, Money.FromEgp(100m));
    }

    [Fact]
    public void Split_allocation_over_the_total_is_rejected_even_by_one_piastre()
    {
        var splits = new[] { new PaymentSplit(PaymentMethod.Cash, Money.FromEgp(60.01m)), new PaymentSplit(PaymentMethod.Card, Money.FromEgp(40m)) };
        Assert.Throws<DomainException>(() => FinancialRules.AssertSplitPaymentAllocation(splits, Money.FromEgp(100m)));
    }

    [Fact]
    public void Split_allocation_rejects_a_duplicated_payment_method()
    {
        var splits = new[] { new PaymentSplit(PaymentMethod.Cash, Money.FromEgp(50m)), new PaymentSplit(PaymentMethod.Cash, Money.FromEgp(50m)) };
        Assert.Throws<DomainException>(() => FinancialRules.AssertSplitPaymentAllocation(splits, Money.FromEgp(100m)));
    }

    [Fact]
    public void Sequence_allocation_never_goes_below_the_historical_floor_of_127()
    {
        Assert.Equal(128, FinancialRules.AllocateNextOrderSequence(0, 0));
    }

    [Fact]
    public void Sequence_allocation_recovers_from_a_lost_in_memory_counter_using_the_stored_max()
    {
        // The in-memory counter reset (e.g. app restart) but storage already has order #0250.
        Assert.Equal(251, FinancialRules.AllocateNextOrderSequence(0, 250));
    }

    [Fact]
    public void Order_number_is_formatted_as_a_zero_padded_four_digit_hash_tag()
    {
        Assert.Equal("#0128", FinancialRules.FormatOrderNumber(128));
    }

    [Fact]
    public void Treasury_balance_nets_incoming_against_outgoing_posted_movements_only()
    {
        var movements = new[]
        {
            MakeMovement("tm1", null, "tre-drawer", Money.FromEgp(100m), MovementStatus.Posted),
            MakeMovement("tm2", "tre-drawer", null, Money.FromEgp(30m), MovementStatus.Posted),
            MakeMovement("tm3", null, "tre-drawer", Money.FromEgp(1000m), MovementStatus.Reversed),
        };
        Assert.Equal(Money.FromEgp(70m), FinancialRules.TreasuryBalanceFromMovements(movements, "tre-drawer"));
    }

    [Fact]
    public void Discrepancy_within_threshold_needs_no_approval()
    {
        Assert.False(FinancialRules.DiscrepancyNeedsApproval(Money.FromEgp(10m), Money.FromEgp(20m)));
    }

    [Fact]
    public void Discrepancy_beyond_threshold_needs_approval_regardless_of_sign()
    {
        Assert.True(FinancialRules.DiscrepancyNeedsApproval(Money.FromEgp(-100m), Money.FromEgp(20m)));
        Assert.True(FinancialRules.DiscrepancyNeedsApproval(Money.FromEgp(100m), Money.FromEgp(20m)));
    }

    [Fact]
    public void Protected_account_cannot_be_drawn_below_zero()
    {
        var movements = new[] { MakeMovement("tm1", null, "tre-drawer", Money.FromEgp(50m), MovementStatus.Posted) };
        Assert.Throws<DomainException>(() =>
            FinancialRules.AssertProtectedSourceBalance(movements, "tre-drawer", Money.FromEgp(100m)));
    }

    [Fact]
    public void Wallet_account_is_not_protected_matching_the_original_omission()
    {
        // tre-wallet is deliberately absent from the protected set in the current app; a debit
        // beyond its posted balance is not blocked at this layer.
        FinancialRules.AssertProtectedSourceBalance(Array.Empty<TreasuryMovement>(), "tre-wallet", Money.FromEgp(999m));
    }

    private static TreasuryMovement MakeMovement(string id, string? source, string? destination, Money amount, MovementStatus status) =>
        new(id, $"key-{id}", $"op-{id}", "test", source, destination, amount, "order", "ord-1", null, "shift-1", "test", "cashier", null, Now, status);
}
