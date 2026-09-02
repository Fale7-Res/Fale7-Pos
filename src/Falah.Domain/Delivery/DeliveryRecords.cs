namespace Falah.Domain.Delivery;

using Falah.Domain.Common;
using Falah.Domain.Financial;

public enum CustodyStatus { Open, Settled }

/// <summary>A driver's trip - one dispatch covering one or more orders. Ported from
/// createDeliveryTrip in lib/delivery-domain.mjs.</summary>
public sealed record DeliveryTrip(string Id, string DriverId, IReadOnlyList<string> OrderIds, string CreatedBy, DateTimeOffset CreatedAtUtc)
{
    public static DeliveryTrip Create(string tripId, string driverId, IReadOnlyList<string> orderIds, string createdBy, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrEmpty(driverId) || orderIds.Count == 0)
            throw new DomainException("يجب اختيار سائق وطلب واحد على الأقل للرحلة.");
        if (orderIds.Distinct().Count() != orderIds.Count)
            throw new DomainException("لا يمكن تكرار الطلب داخل الرحلة.");
        return new DeliveryTrip(tripId, driverId, orderIds, createdBy, createdAtUtc);
    }
}

/// <summary>How much of one delivery's money is the driver physically responsible for. Ported from
/// createCashCustody / reassignCustody.</summary>
public sealed record DriverCustody(
    string Id, string TripId, string DriverId, string OrderId, string DeliveryId,
    Money Expected, Money Collected, Money Settled, Money Difference, CustodyStatus Status, DateTimeOffset CreatedAtUtc)
{
    public DriverCustody ReassignTo(string newDriverId) =>
        Status == CustodyStatus.Settled ? this : this with { DriverId = newDriverId };
}

/// <summary>The minimal facts DeliveryRules needs about one delivery-tagged order. This is a
/// read-model shape for the pure functions below, not necessarily the eventual persistence entity
/// - Infrastructure may compose it from Order + a delivery-specific table.</summary>
public sealed record DeliveryFacts(
    string Id, string OrderId, string? OrderNumber, string DriverId, DeliveryStatus Status,
    PaymentMethod PaymentMethod, bool PaymentAlreadySettled, Money AmountToCollect, Money OrderTotal,
    string? ShiftId, string? TripId, Money CompAmount,
    DateTimeOffset? DeliveredAtUtc = null, DateTimeOffset? FailedAtUtc = null, DateTimeOffset? ReturnedAtUtc = null);

public sealed record Employee(string Id, bool IsActive, bool CanDeliver);
public sealed record Driver(string Id, string Name, bool Active, string? EmployeeId);
