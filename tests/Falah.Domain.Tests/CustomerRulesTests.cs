using Falah.Domain.Customers;
using Xunit;

namespace Falah.Domain.Tests;

public class CustomerRulesTests
{
    [Fact]
    public void Differently_formatted_phone_numbers_normalize_to_the_same_value()
    {
        Assert.Equal(CustomerRules.NormalizePhone("010-1234-5678"), CustomerRules.NormalizePhone("01012345678"));
    }

    [Fact]
    public void FindByPhone_matches_regardless_of_formatting()
    {
        var customers = new[] { new Customer("cust-1", "010 1234 5678", "أحمد") };
        var found = CustomerRules.FindByPhone(customers, "01012345678");
        Assert.Equal("cust-1", found?.Id);
    }
}
