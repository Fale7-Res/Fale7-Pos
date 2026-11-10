using Falah.Domain.Common;
using Falah.Domain.Orders;
using Falah.Domain.Financial;
using Falah.Domain.BusinessDay;
namespace Falah.Domain.Tests;
public class SignedInputValidationTests
{
    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Checkout_rejects_negative_adjustments(long discount, long fee, long comp)
    {
        Assert.Throws<DomainException>(() => CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Cash,
            Array.Empty<OrderItem>(), Money.FromPiastres(discount), null, Money.FromPiastres(fee),
            Money.FromPiastres(comp), Money.FromPiastres(100000), null, null));
    }
    [Theory]
    [InlineData(1, 1)]
    [InlineData(-100, 1)]
    [InlineData(20000, long.MaxValue)]
    public void Cash_count_refuses_unknown_denominations_and_overflow(long denomination, long quantity)
    {
        Assert.Throws<DomainException>(() => BusinessDayRules.CalculateDenominationCount(new[] { (denomination, quantity) }, 0));
    }
}
