namespace Falah.Infrastructure.Persistence.Entities;

/// <summary>
/// Plain, EF-friendly persistence shape for an Order. Deliberately NOT the Falah.Domain.Orders.Order
/// aggregate itself - EF Core mapping concerns (parameterless constructors, mutable public setters,
/// simple scalar columns) never leak into the Domain project; OrderMapper translates between the
/// two. Money is stored as a plain long (piastres); enums as strings for a readable database file.
/// </summary>
public class OrderEntity
{
    public string Id { get; set; } = default!;
    public string OrderNumber { get; set; } = default!;
    public long SequenceNumber { get; set; }
    public string CashierId { get; set; } = default!;
    public string ShiftId { get; set; } = default!;
    public string OrderType { get; set; } = default!;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryArea { get; set; }
    public long DeliveryFeePiastres { get; set; }
    public long SubtotalPiastres { get; set; }
    public long CompAmountPiastres { get; set; }
    public string? CompReasonCode { get; set; }
    public string? CompAuthorizedBy { get; set; }
    public string? CompApprovalId { get; set; }
    public long TaxAmountPiastres { get; set; }
    public long TotalPiastres { get; set; }
    public string PaymentMethod { get; set; } = default!;
    /// <summary>Small, order-scoped list serialized as JSON - not a candidate for its own table
    /// with independent identity or queries.</summary>
    public string? SplitPaymentsJson { get; set; }
    public long AmountPaidPiastres { get; set; }
    public long ChangeDuePiastres { get; set; }
    public string CheckoutRequestId { get; set; } = default!;
    public string? BusinessDayId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public string Status { get; set; } = default!;
    public bool IsPaid { get; set; }
    public string? ElectronicPaymentMethod { get; set; }
    public string? ElectronicPaymentProvider { get; set; }
    public long? ElectronicPaymentAmountPiastres { get; set; }
    public string? ElectronicPaymentStatus { get; set; }
    public long RefundAmountPiastres { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancelReason { get; set; }
    public string? CancelledBy { get; set; }

    public List<OrderItemEntity> Items { get; set; } = new();
    public List<OrderRefundEntity> Refunds { get; set; } = new();

    /// <summary>EF Core's optimistic concurrency token - guards the rare case of two writers
    /// loading the same order row before either commits (e.g. a refund racing a payment
    /// confirmation on the same order from two devices).</summary>
    public uint Version { get; set; }
}

public class OrderItemEntity
{
    public int Id { get; set; }
    public string OrderId { get; set; } = default!;
    public string ProductId { get; set; } = default!;
    public string? VariantId { get; set; }
    public string ProductName { get; set; } = default!;
    public string? VariantName { get; set; }
    public long UnitPricePiastres { get; set; }
    public long ListUnitPricePiastres { get; set; }
    public int Quantity { get; set; }
    public long TotalPricePiastres { get; set; }
    public bool SendToGrill { get; set; }
    public string? PriceRuleId { get; set; }
    public string? PriceRuleName { get; set; }
    public string? Notes { get; set; }
}

public class OrderRefundEntity
{
    public int Id { get; set; }
    public string OrderId { get; set; } = default!;
    public string OperationId { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public long AmountPiastres { get; set; }
    public string PaymentMethod { get; set; } = default!;
    public string TreasuryMovementId { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = default!;
    public string AuthorizedBy { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public string OriginalShiftId { get; set; } = default!;
    public string OperationalShiftId { get; set; } = default!;
}
