namespace Falah.Domain.Orders;

using Falah.Domain.Common;

/// <summary>Splits an order's value into the three buckets that must stay separate everywhere
/// downstream: what the menu says, what an approved offer gave up, and what the restaurant
/// absorbed as a comp. `Collected` is what the customer is actually asked to pay. Ported from
/// orderValueBreakdown in lib/pricing-domain.mjs.</summary>
public readonly record struct OrderValueBreakdown(Money ListValue, Money OfferValue, Money Subtotal, Money CompValue, Money Collected)
{
    public static OrderValueBreakdown Compute(IReadOnlyList<OrderItem> items, Money compAmount)
    {
        var listValue = Money.FromPiastres(items.Sum(item => item.ListUnitPrice.Piastres * item.Quantity));
        var chargedValue = Money.FromPiastres(items.Sum(item => item.TotalPrice.Piastres));
        var offerValue = listValue - chargedValue;
        if (offerValue.IsNegative) offerValue = Money.Zero;
        var comp = compAmount.IsNegative ? Money.Zero : compAmount;
        var collected = chargedValue - comp;
        if (collected.IsNegative) collected = Money.Zero;
        return new OrderValueBreakdown(listValue, offerValue, chargedValue, comp, collected);
    }
}
