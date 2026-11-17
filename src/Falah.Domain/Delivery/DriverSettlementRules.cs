namespace Falah.Domain.Delivery;

using Falah.Domain.Common;

/// <summary>
/// One driver settlement's figures, all in piastres.
///
/// - Due = CarriedIn + CashCollected - FeeEntitlement. Positive: the driver owes the restaurant. Negative: the
///   restaurant owes the driver.
/// - Remaining = Due - Received + PaidToDriver.
/// - A final settlement leaves CarriedOut = 0 and records -Remaining as its Difference (negative = shortage,
///   positive = overage).
/// - A partial settlement carries Remaining forward as CarriedOut, with no Difference: it is still owed, not lost.
/// </summary>
public readonly record struct DriverSettlementFigures(
    Money CarriedIn, Money CashCollected, Money FeeEntitlement, Money Due,
    Money Received, Money PaidToDriver, Money Remaining, Money CarriedOut, Money Difference)
{
    public bool RestaurantOwesDriver => Due.IsNegative;
}

/// <summary>
/// Owner-confirmed driver settlement (docs/UI_OWNER_REQUESTS.md, "Confirmed counter and delivery workflow").
///
/// - The driver keeps the FULL delivery charge recorded on each order he successfully delivered.
/// - He settles with the cashier who actually receives his money.
/// - What he hands over is the cash he collected minus the fees he earned, plus any balance carried forward.
/// - When that is negative the restaurant owes him: it is paid out explicitly or carried, never clamped to zero.
///
/// Fees are recognised once, because a delivery leaves the settlement scope (Settled) when its settlement commits.
/// A delivery that failed or was returned earns no fee: the agreed rule covers completed deliveries only. No
/// policy is assumed here for refunds, waived fees or splitting a fee between drivers. See
/// docs/UI_OWNER_REQUESTS.md for the open questions.
/// </summary>
public static class DriverSettlementRules
{
    public static Money FeeEntitlement(IEnumerable<DeliveryFacts> resolvedDeliveries) =>
        Money.FromPiastres(resolvedDeliveries
            .Where(d => DeliveryRules.FinalOutcome(d) == DeliveryStatus.Delivered)
            .Sum(d => d.DeliveryFee.Piastres));

    public static DriverSettlementFigures Calculate(
        Money carriedIn, Money cashCollected, Money fees, Money received, Money paidToDriver, bool keepRemainderOpen)
    {
        if (received.IsNegative || paidToDriver.IsNegative)
            throw new DomainException("المبالغ المستلمة أو المصروفة للسائق يجب أن تكون صفرًا أو أكثر.");
        if (received.IsPositive && paidToDriver.IsPositive)
            throw new DomainException("لا يمكن استلام نقدية من السائق وصرف نقدية له في نفس التسوية.");

        var due = carriedIn + cashCollected - fees;
        var owedToDriver = due.IsNegative ? -due : Money.Zero;
        if (paidToDriver > owedToDriver)
            throw new DomainException("لا يمكن صرف للسائق أكثر من المستحق له في هذه التسوية.");

        var remaining = due - received + paidToDriver;
        return keepRemainderOpen
            ? new DriverSettlementFigures(carriedIn, cashCollected, fees, due, received, paidToDriver, remaining, remaining, Money.Zero)
            : new DriverSettlementFigures(carriedIn, cashCollected, fees, due, received, paidToDriver, remaining, Money.Zero, -remaining);
    }
}
