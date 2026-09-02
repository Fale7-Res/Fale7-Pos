namespace Falah.Domain.Orders;

using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Shifts;

public static class RefundRules
{
    public static Money RefundableRemaining(Money total, Money alreadyRefunded)
    {
        var remaining = total - alreadyRefunded;
        return remaining.IsNegative ? Money.Zero : remaining;
    }

    public static Money ClampRefundAmount(Money requested, Money remaining)
    {
        var floored = requested.IsNegative ? Money.Zero : requested;
        var cap = remaining.IsNegative ? Money.Zero : remaining;
        return floored < cap ? floored : cap;
    }

    public static void AssertOrderCanBeCancelled(Money refundAmount)
    {
        if (refundAmount.IsPositive)
            throw new DomainException("هذا الطلب يحتوي على مرتجع سابق. استكمل رد المبلغ المتبقي بدلًا من إلغاء الطلب.");
    }

    public readonly record struct RefundShiftPolicy(bool HistoricalImmutable, string? OperationalShiftId);

    /// <summary>A closed historical shift is immutable - a refund against an order from a closed
    /// shift posts operationally against the CURRENTLY OPEN shift instead. A cash refund
    /// specifically needs an open shift to book the resulting cash-out against; a card/instapay/
    /// wallet refund does not touch any physical drawer, so it has no such requirement.</summary>
    public static RefundShiftPolicy ResolveOperationalShift(
        string originalShiftId, ShiftStatus originalShiftStatus, string? activeShiftId, PaymentMethod paymentMethod)
    {
        var historicalImmutable = originalShiftStatus == ShiftStatus.Closed;
        if (historicalImmutable && paymentMethod == PaymentMethod.Cash && activeShiftId is null)
            throw new DomainException("يجب فتح وردية قبل تنفيذ مرتجع نقدي لطلب من وردية مغلقة.");

        return new RefundShiftPolicy(historicalImmutable, historicalImmutable ? activeShiftId : originalShiftId);
    }
}
