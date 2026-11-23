namespace Falah.Infrastructure.Orders;

using Falah.Domain.Financial;
using Falah.Domain.Orders;

public static class OrderEnumMapping
{
    public static OrderType ParseOrderType(string value) => (value ?? "").ToLowerInvariant() switch
    {
        // "dine_in"/"dinein" is the pre-rename spelling. Orders written before the type became
        // Pickup still carry it in the database and are still read back, so parsing keeps
        // accepting it; only ToWireString moved on, so nothing new is written under the old name.
        "pickup" or "dine_in" or "dinein" => OrderType.Pickup,
        "takeaway" => OrderType.Takeaway,
        "delivery" => OrderType.Delivery,
        // QA-08 (audit 2026-11-23): a validation message (422), not an unexpected error (500).
        _ => throw new Falah.Domain.Common.DomainException($"نوع طلب غير معروف: {value}"),
    };

    public static PaymentMethod ParsePaymentMethod(string value) => (value ?? "").ToLowerInvariant() switch
    {
        "cash" => PaymentMethod.Cash,
        "card" => PaymentMethod.Card,
        "instapay" => PaymentMethod.InstaPay,
        "wallet" => PaymentMethod.Wallet,
        "multi" => PaymentMethod.Multi,
        _ => throw new Falah.Domain.Common.DomainException($"طريقة دفع غير معروفة: {value}"),
    };

    public static string ToWireString(this OrderType type) => type switch
    {
        OrderType.Pickup => "pickup",
        OrderType.Takeaway => "takeaway",
        OrderType.Delivery => "delivery",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static string ToWireString(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "cash",
        PaymentMethod.Card => "card",
        PaymentMethod.InstaPay => "instapay",
        PaymentMethod.Wallet => "wallet",
        PaymentMethod.Multi => "multi",
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };

    /// <summary>The stored form of a cancelled order, for queries (audit 2026-11-23, F-14): comparing with
    /// nameof(OrderStatus.Cancelled) ("Cancelled") matched only because the database collation ignores case.</summary>
    public const string CancelledWire = "cancelled";

    public static string ToWireString(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "pending",
        OrderStatus.Preparing => "preparing",
        OrderStatus.Ready => "ready",
        OrderStatus.Completed => "completed",
        OrderStatus.Cancelled => CancelledWire,
        OrderStatus.Refunded => "refunded",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static string ToWireString(this ElectronicPaymentStatus status) => status switch
    {
        ElectronicPaymentStatus.Pending => "pending",
        ElectronicPaymentStatus.Confirmed => "confirmed",
        ElectronicPaymentStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };
}
