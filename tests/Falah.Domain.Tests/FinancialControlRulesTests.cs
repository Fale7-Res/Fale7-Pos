namespace Falah.Domain.Tests;

using Falah.Domain.Common;
using Falah.Domain.ControlCenter;
using Falah.Domain.Financial;
using Falah.Domain.Purchasing;
using Xunit;

/// <summary>P1-P4 rules: what counts as customer money, how mistakes are undone, how a shortfall is classified.</summary>
public class FinancialControlRulesTests
{
    [Theory]
    [InlineData("sale_collection", MoneyFlowCategory.CustomerCollection, true)]
    [InlineData("driver_settlement", MoneyFlowCategory.DriverRemittance, true)]
    [InlineData("transfer", MoneyFlowCategory.InternalTransfer, false)]
    [InlineData("shift_handover", MoneyFlowCategory.InternalTransfer, false)]
    [InlineData("safe_drop", MoneyFlowCategory.InternalTransfer, false)]
    [InlineData("capital_injection", MoneyFlowCategory.ExternalFunding, false)]
    [InlineData("expense_correction", MoneyFlowCategory.ExpenseCorrection, false)]
    [InlineData("expense_refund", MoneyFlowCategory.ExpenseReimbursement, false)]
    [InlineData("movement_correction", MoneyFlowCategory.EntryCorrection, false)]
    [InlineData("cash_overage", MoneyFlowCategory.CashCountDifference, false)]
    [InlineData("something_new", MoneyFlowCategory.Unclassified, false)]
    public void Only_customer_money_is_a_collection(string type, MoneyFlowCategory category, bool isReceipt)
    {
        Assert.Equal(category, MoneyFlowRules.Classify(type));
        Assert.Equal(isReceipt, MoneyFlowRules.IsCustomerReceipt(MoneyFlowRules.Classify(type)));
    }

    [Fact]
    public void An_expense_reversal_must_say_what_happened_to_the_money()
    {
        Assert.Equal("expense_correction", ExpenseCorrectionRules.MovementTypeFor("not_paid"));
        Assert.Equal("expense_refund", ExpenseCorrectionRules.MovementTypeFor("cash_returned"));
        Assert.Throws<DomainException>(() => ExpenseCorrectionRules.MovementTypeFor(null));
        Assert.Throws<DomainException>(() => ExpenseCorrectionRules.MovementTypeFor("paid_and_gone"));
    }

    [Fact]
    public void A_reversal_is_never_more_than_the_expense_and_never_zero()
    {
        var recorded = Money.FromPiastres(5_000);
        Assert.Equal(recorded, ExpenseCorrectionRules.ReversedAmount(recorded, null));
        Assert.Equal(Money.FromPiastres(4_500), ExpenseCorrectionRules.ReversedAmount(recorded, 4_500));
        Assert.Throws<DomainException>(() => ExpenseCorrectionRules.ReversedAmount(recorded, 5_001));
        Assert.Throws<DomainException>(() => ExpenseCorrectionRules.ReversedAmount(recorded, 0));
    }

    [Theory]
    [InlineData("transfer", true)]
    [InlineData("safe_drop", true)]
    [InlineData("owner_deposit", true)]
    [InlineData("sale_collection", false)]
    [InlineData("refund", false)]
    [InlineData("shift_handover", false)]
    [InlineData("movement_correction", false)]
    public void Only_hand_typed_treasury_entries_are_corrected_from_the_treasury(string type, bool correctable)
    {
        if (correctable) TreasuryCorrectionRules.EnsureCorrectable(type, "Posted");
        else Assert.Throws<DomainException>(() => TreasuryCorrectionRules.EnsureCorrectable(type, "Posted"));
    }

    [Fact]
    public void A_shortfall_classification_needs_a_known_kind_and_evidence()
    {
        CollectionShortfallRules.EnsureValid("write_off", "موافقة المالك - إيصال 7");
        Assert.Throws<DomainException>(() => CollectionShortfallRules.EnsureValid("lost", "موافقة المالك"));
        Assert.Throws<DomainException>(() => CollectionShortfallRules.EnsureValid("write_off", "  "));
        Assert.True(CollectionShortfallRules.IsReceivable("customer_receivable"));
        Assert.True(CollectionShortfallRules.IsReceivable("driver_liability"));
        Assert.True(CollectionShortfallRules.IsRealisedReduction("approved_discount"));
        Assert.False(CollectionShortfallRules.IsRealisedReduction("customer_receivable"));
    }

    [Fact]
    public void A_supplier_credit_is_never_more_than_is_still_owed()
    {
        Assert.Equal("supplier_return", SupplierCreditRules.LedgerTypeFor("goods_returned"));
        Assert.Throws<DomainException>(() => SupplierCreditRules.LedgerTypeFor("refund"));
        SupplierCreditRules.AssertWithinOutstanding(Money.FromPiastres(3_000), Money.FromPiastres(3_000));
        Assert.Throws<DomainException>(() => SupplierCreditRules.AssertWithinOutstanding(Money.FromPiastres(3_001), Money.FromPiastres(3_000)));
        Assert.Throws<DomainException>(() => SupplierCreditRules.AssertWithinOutstanding(Money.Zero, Money.FromPiastres(3_000)));
    }

    [Fact]
    public void Unconfirmed_handovers_and_unresolved_shortfalls_are_on_the_review_list()
    {
        var exceptions = ControlCenterRules.BuildExceptions(new ControlCenterRules.BuildExceptionsInputs(
            [], 5_000, [], [], [], [],
            UnconfirmedHandovers: [("s1", "صباحية", "pending"), ("s2", "مسائية", "disputed")],
            UnresolvedShortfalls: [("d1", "42", 2_000)]));

        Assert.Contains(exceptions, e => e.Id == "handover-pending-s1" && e.Severity == "warning");
        Assert.Contains(exceptions, e => e.Id == "handover-disputed-s2" && e.Severity == "critical");
        Assert.Contains(exceptions, e => e.Id == "shortfall-d1" && e.Category == "delivery");
    }
}
