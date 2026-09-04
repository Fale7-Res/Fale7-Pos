namespace Falah.Application.Orders;

/// <summary>What a client sees when it (re)opens an order to display or print it - a proper API
/// contract, not the raw EF persistence entity. Read-only by construction: nothing about fetching
/// this can ever mutate financial state.</summary>
public sealed record OrderItemDto(
    string ProductName, string? VariantName, long UnitPricePiastres, int Quantity,
    long TotalPricePiastres, string? PriceRuleName, string? Notes);

public sealed record OrderDetailsDto(
    string Id, string OrderNumber, long SequenceNumber, string OrderType, string PaymentMethod,
    IReadOnlyList<OrderItemDto> Items, long SubtotalPiastres, long TaxAmountPiastres, long CompAmountPiastres,
    long TotalPiastres, long AmountPaidPiastres, long ChangeDuePiastres, string Status, bool IsPaid,
    string? ElectronicPaymentStatus, long RefundAmountPiastres, DateTimeOffset CreatedAtUtc, string CashierId,
    string? CustomerName, string? CustomerPhone, string? DeliveryArea, string? DeliveryAddress,
    string? DeliveryStreet, string? DeliveryBuilding, string? DeliveryFloor, string? DeliveryApartment,
    string? DeliveryLandmark, string? DeliveryNotes, string? DeliveryServiceId, string? DeliveryServiceName,
    long DeliveryFeePiastres, long DiscountPiastres, string ShiftId);
