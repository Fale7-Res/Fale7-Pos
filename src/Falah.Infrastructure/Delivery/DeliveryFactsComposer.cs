namespace Falah.Infrastructure.Delivery;

using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence.Entities;

/// <summary>Composes Falah.Domain.Delivery.DeliveryFacts from a DeliveryEntity + its Order, exactly
/// as that record's own doc comment instructs ("Infrastructure may compose it from Order + a
/// delivery-specific table") - no financial fact is ever duplicated onto DeliveryEntity itself.</summary>
internal static class DeliveryFactsComposer
{
    public static DeliveryFacts Compose(DeliveryEntity delivery, OrderEntity order)
    {
        var total = Money.FromPiastres(order.TotalPiastres);
        var refunded = Money.FromPiastres(order.RefundAmountPiastres);
        return new DeliveryFacts(
            Id: delivery.Id,
            OrderId: order.Id,
            OrderNumber: order.OrderNumber,
            DriverId: delivery.DriverId ?? "",
            Status: Enum.Parse<DeliveryStatus>(delivery.Status),
            PaymentMethod: OrderEnumMapping.ParsePaymentMethod(order.PaymentMethod),
            // order.IsPaid is the single source of truth for "has this order's money already
            // changed hands" - true immediately for card, true once ConfirmElectronicPayment fires
            // for InstaPay/Wallet, and always false for cash-on-delivery until the driver actually
            // collects it. Reusing it here (rather than re-deriving from ElectronicPaymentStatus)
            // keeps this in lockstep with Order's own tested definition of "paid".
            PaymentAlreadySettled: order.IsPaid,
            AmountToCollect: total - refunded,
            OrderTotal: total,
            ShiftId: delivery.ShiftId,
            TripId: delivery.TripId,
            CompAmount: Money.FromPiastres(order.CompAmountPiastres),
            DeliveredAtUtc: delivery.DeliveredAtUtc,
            FailedAtUtc: delivery.FailedAtUtc,
            ReturnedAtUtc: delivery.ReturnedAtUtc);
    }

    public static DriverCustody ToDriverCustody(DriverCustodyEntity e) => new(
        e.Id, e.TripId, e.DriverId, e.OrderId, e.DeliveryId, Money.FromPiastres(e.ExpectedPiastres),
        Money.FromPiastres(e.CollectedPiastres), Money.FromPiastres(e.SettledPiastres), Money.FromPiastres(e.DifferencePiastres),
        Enum.Parse<CustodyStatus>(e.Status), e.CreatedAtUtc);
}
