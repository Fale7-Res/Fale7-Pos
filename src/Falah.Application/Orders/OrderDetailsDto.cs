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
    long DeliveryFeePiastres, long DiscountPiastres, string ShiftId,
    /// <summary>How a multi-method payment was actually split. Null for an ordinary single-method
    /// order. The legs were always persisted and always used to attribute each channel's total, but
    /// were not exposed here - so an order that read "multi" could not be reconciled from any
    /// screen, receipt or report. An accountant needs to see which part came from where.</summary>
    IReadOnlyList<OrderSplitPaymentDto>? SplitPayments = null,
    /// <summary>The dine-in table this order was for, as it was at the time. Dine-in is gone; this
    /// still reads back for the orders written before it was.</summary>
    string? TableNumber = null,
    /// <summary>When the customer said they would collect a pickup order. Null elsewhere.</summary>
    DateTimeOffset? RequestedPickupAtUtc = null);

public sealed record OrderSplitPaymentDto(string Method, long AmountPiastres);
