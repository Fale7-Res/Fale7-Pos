using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Inventory;
using Xunit;

namespace Falah.Domain.Tests;

public class PurchasingRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Purchase_line_total_rounds_to_the_nearest_piastre()
    {
        var total = PurchasingRules.PurchaseLineTotal(1500, Money.FromPiastres(333));
        Assert.Equal(Money.FromPiastres(500), total); // 1500 * 333 / 1000 = 499.5 -> 500
    }

    [Fact]
    public void Last_purchase_unit_cost_picks_the_most_recent_receipt_ignoring_other_movement_types()
    {
        var movements = new[]
        {
            new StockMovementFacts("inv-1", StockMovementType.PurchaseReceipt, Money.FromPiastres(1000), Now.AddDays(-5)),
            new StockMovementFacts("inv-1", StockMovementType.PurchaseReceipt, Money.FromPiastres(1200), Now.AddDays(-1)),
            new StockMovementFacts("inv-1", StockMovementType.Usage, Money.FromPiastres(9999), Now),
        };
        Assert.Equal(Money.FromPiastres(1200), PurchasingRules.LastPurchaseUnitCost(movements, "inv-1"));
    }

    [Fact]
    public void Last_purchase_unit_cost_is_null_when_the_item_was_never_purchased()
    {
        Assert.Null(PurchasingRules.LastPurchaseUnitCost(Array.Empty<StockMovementFacts>(), "inv-1"));
    }

    [Fact]
    public void Assert_purchase_payment_rejects_a_negative_amount()
    {
        Assert.Throws<DomainException>(() => PurchasingRules.AssertPurchasePayment(Money.FromPiastres(-1), Money.FromPiastres(1000)));
    }

    [Fact]
    public void Assert_purchase_payment_rejects_paying_more_than_the_invoice_total()
    {
        Assert.Throws<DomainException>(() => PurchasingRules.AssertPurchasePayment(Money.FromPiastres(1001), Money.FromPiastres(1000)));
    }

    [Fact]
    public void Assert_purchase_payment_allows_zero_up_to_the_full_total()
    {
        PurchasingRules.AssertPurchasePayment(Money.FromPiastres(0), Money.FromPiastres(1000));
        PurchasingRules.AssertPurchasePayment(Money.FromPiastres(1000), Money.FromPiastres(1000));
    }

    [Theory]
    [InlineData("cash", TreasuryAccounts.Main)]
    [InlineData("bank", TreasuryAccounts.Bank)]
    [InlineData("instapay", TreasuryAccounts.InstaPay)]
    public void Purchase_payment_account_maps_each_real_payment_method_to_its_treasury_account(string method, string expectedAccount)
    {
        Assert.Equal(expectedAccount, PurchasingRules.PurchasePaymentAccountFor(method));
    }

    [Fact]
    public void Credit_purchase_payment_has_no_real_treasury_account_yet()
    {
        Assert.Null(PurchasingRules.PurchasePaymentAccountFor("credit"));
    }

    [Fact]
    public void An_unsupported_purchase_payment_method_is_rejected()
    {
        Assert.Throws<DomainException>(() => PurchasingRules.PurchasePaymentAccountFor("wallet"));
    }

    [Fact]
    public void Supplier_outstanding_nets_every_ledger_entry_for_that_supplier_and_purchase()
    {
        var entries = new[]
        {
            new SupplierLedgerEntry("sup-1", "purchase-1", "purchase-1", Money.FromPiastres(10000), Money.FromPiastres(0)),
            new SupplierLedgerEntry("sup-1", "purchase-1", "purchase-1", Money.FromPiastres(0), Money.FromPiastres(4000)),
            new SupplierLedgerEntry("sup-1", "purchase-2", "purchase-2", Money.FromPiastres(5000), Money.FromPiastres(0)),
        };
        Assert.Equal(Money.FromPiastres(6000), PurchasingRules.SupplierOutstandingFromLedger(entries, "sup-1", "purchase-1"));
        Assert.Equal(Money.FromPiastres(11000), PurchasingRules.SupplierOutstandingFromLedger(entries, "sup-1", null));
    }
}
