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
    /// <summary>Which table a dine-in order belongs to, as typed at the till. Free text on
    /// purpose: restaurants number, letter and nickname their tables however they like, and this
    /// system has never been told which scheme this one uses.</summary>
    public string? TableNumber { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    /// <summary>Composed one-liner (DeliveryRules.ComposeDeliveryAddress), computed server-side from
    /// the structured parts below plus the landmark - never trusted as raw client input. Used by
    /// receipts/search/driver view; the structured parts exist for a driver who needs real fields
    /// to navigate by, not just a sentence.</summary>
    public string? DeliveryAddress { get; set; }
    public string? DeliveryArea { get; set; }
    public string? DeliveryStreet { get; set; }
    public string? DeliveryBuilding { get; set; }
    public string? DeliveryFloor { get; set; }
    public string? DeliveryApartment { get; set; }
    /// <summary>Free-text landmark/notes - the only unstructured part of the address, appended last.</summary>
    public string? DeliveryLandmark { get; set; }
    public string? DeliveryNotes { get; set; }
    /// <summary>Pricing service snapshot (which priced delivery zone/service funded the fee) -
    /// separate from DeliveryArea (the physical neighborhood name). Frozen at checkout time like
    /// every other order snapshot; no referential integrity to a service catalog is enforced here.</summary>
    public string? DeliveryServiceId { get; set; }
    public string? DeliveryServiceName { get; set; }
    public long DeliveryFeePiastres { get; set; }
    public long SubtotalPiastres { get; set; }
    /// <summary>Manual discount applied to the taxable base at checkout time - ported from
    /// order.discount in the reference app. Previously computed and used only transiently (to feed
    /// the tax calc) and then discarded; now frozen here so a report can ever separate it from
    /// offersValue (a live price rule) and compsValue (a PIN-authorized comp) - see SalesRules.</summary>
    public long DiscountPiastres { get; set; }
    public long CompAmountPiastres { get; set; }
    public string? CompReasonCode { get; set; }
    public string? CompAuthorizedBy { get; set; }
    public string? CompApprovalId { get; set; }
    public long TaxAmountPiastres { get; set; }
    public long TaxableBasePiastres { get; set; }
    public int? TaxRateBasisPoints { get; set; }
    public string? TaxMode { get; set; }
    public string? TaxRuleName { get; set; }
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

    /// <summary>The kitchen station this line routed to AT SALE TIME, frozen exactly like ProductName/
    /// UnitPrice - never overwritten by a later change to ProductEntity.PreparationStation, which only
    /// governs future orders.</summary>
    public string? PreparationStation { get; set; }
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
