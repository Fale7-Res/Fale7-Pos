namespace Falah.Domain.Orders;

using Falah.Domain.Common;
using Falah.Domain.Financial;

/// <summary>
/// The ONE place that decides how much of an order's money actually became cash / card / instapay
/// / wallet revenue. The Shifts blind-count drawer reconciliation, the Reports screen, and the
/// frozen Business Day close snapshot must all read these same numbers. Ported from
/// paymentMethodTotals / cashSalesFromOrders in lib/financial-domain.mjs.
/// </summary>
public static class OrderPaymentQueries
{
    private static readonly PaymentMethod[] TrackedMethods =
        { PaymentMethod.Cash, PaymentMethod.Card, PaymentMethod.InstaPay, PaymentMethod.Wallet };

    /// <summary>
    /// A leg that settles AT CHECKOUT (cash, card) counts as soon as it posts - for a split order
    /// that is IMMEDIATELY, even while a sibling InstaPay/wallet leg on the very same order is
    /// still an unverified claim and keeps the order's overall IsPaid false. A leg that requires
    /// separate confirmation counts only once actually confirmed. Note the deliberate asymmetry: a
    /// split leg's own amount is counted as-is (refunds are not netted against individual split
    /// legs, matching the original), while a single-payment-method order counts Total minus
    /// RefundAmount.
    /// </summary>
    public static IReadOnlyDictionary<PaymentMethod, Money> PaymentMethodTotals(IEnumerable<Order> orders)
    {
        var totals = TrackedMethods.ToDictionary(m => m, _ => Money.Zero);

        foreach (var order in orders)
        {
            if (order.Status == OrderStatus.Cancelled) continue;
            var netTotal = order.Total - order.RefundAmount;

            if (order.PaymentMethod == PaymentMethod.Multi && order.SplitPayments is { } splits)
            {
                foreach (var part in splits)
                {
                    var isElectronic = part.Method is PaymentMethod.InstaPay or PaymentMethod.Wallet;
                    var confirmed = order.ElectronicPayment is { Status: ElectronicPaymentStatus.Confirmed } leg && leg.Method == part.Method;
                    if ((isElectronic && confirmed) || !isElectronic)
                    {
                        if (totals.ContainsKey(part.Method)) totals[part.Method] += part.Amount;
                    }
                }
            }
            else if (order.IsPaid && totals.ContainsKey(order.PaymentMethod))
            {
                totals[order.PaymentMethod] += netTotal;
            }
        }

        return totals;
    }

    /// <summary>Physical cash that a set of orders put into the drawer - the cash channel of
    /// PaymentMethodTotals.</summary>
    public static Money CashSalesFromOrders(IEnumerable<Order> orders) =>
        PaymentMethodTotals(orders)[PaymentMethod.Cash];

    /// <summary>
    /// Cash handed back out of those same sales - reporting only, never a second subtraction.
    ///
    /// CashSalesFromOrders already counts a cash order as Total MINUS RefundAmount, so a drawer
    /// reconciliation that subtracted this again would charge every refund twice. It exists so the
    /// handover slip can show the gross take and the refunds on their own lines while their net
    /// stays exactly what the drawer should hold. The conditions mirror the cash channel above:
    /// same skipped cancellations, same single-method paid orders (a split order's legs are counted
    /// as-is there, so their refunds are not netted and are not reported here either).
    /// </summary>
    public static Money CashRefundsFromOrders(IEnumerable<Order> orders)
    {
        var refunds = Money.Zero;
        foreach (var order in orders)
        {
            if (order.Status == OrderStatus.Cancelled) continue;
            if (order.PaymentMethod == PaymentMethod.Multi && order.SplitPayments is not null) continue;
            if (order.IsPaid && order.PaymentMethod == PaymentMethod.Cash) refunds += order.RefundAmount;
        }
        return refunds;
    }
}
