using Falah.Domain.Orders;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>Native UI pass 2026-11-08: a cashier typed "128" for order #0128 and was told "العنصر
/// المطلوب غير موجود.". People read the number off a receipt in whatever form is natural.</summary>
public class OrderNumberInputTests
{
    [Theory]
    [InlineData("#0128", 128)]
    [InlineData("0128", 128)]
    [InlineData("128", 128)]
    [InlineData(" #128 ", 128)]
    [InlineData("١٢٨", 128)]
    [InlineData("#٠١٢٨", 128)]
    public void A_typed_order_number_is_read_as_its_sequence(string typed, long expected)
    {
        Assert.True(OrderNumberInput.TryParseSequence(typed, out var sequence));
        Assert.Equal(expected, sequence);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("#")]
    [InlineData("12a")]
    [InlineData("-5")]
    [InlineData("0")]
    public void Anything_that_is_not_an_order_number_is_refused(string typed) =>
        Assert.False(OrderNumberInput.TryParseSequence(typed, out _));
}
