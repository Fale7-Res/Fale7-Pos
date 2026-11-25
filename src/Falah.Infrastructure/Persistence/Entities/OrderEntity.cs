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

    /// <summary>When the customer said they would come and collect a pickup order. Null on every
    /// other type, and null on a pickup nobody named a time for - which is most of them.</summary>
    public DateTimeOffset? RequestedPickupAtUtc { get; set; }

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

    /// <summary>UI-0025 (owner decision 2026-11-22): a pickup registered and sent to the kitchen unpaid,
    /// waiting for its customer to pay at collection. While true it is in no sales figure; collecting it
    /// moves the sale to the moment and the shift that took the money (CreatedAtUtc / ShiftId), keeping
    /// the registration in RegisteredAtUtc / RegisteredShiftId. An unpaid one blocks the day close.</summary>
    public bool AwaitingCollection { get; set; }
    public DateTimeOffset? RegisteredAtUtc { get; set; }
    public string? RegisteredShiftId { get; set; }
    /// <summary>The collect request that paid it - its replay key.</summary>
    public string? CollectionRequestId { get; set; }
    public string? CollectedBy { get; set; }

    /// <summary>Owner decision 2026-11-24 (workflow redesign): the moment a pickup's bag was handed to its customer -
    /// the end of a pickup's life (Status becomes completed and it leaves the active queue; the row is never deleted).
    /// Set by the collection of an unpaid pickup (it is handed over as it is paid) or by the handover of a prepaid
    /// one. Null on every other order type, and on a pickup still waiting at the counter.</summary>
    public DateTimeOffset? HandedOverAtUtc { get; set; }
    public string? HandedOverBy { get; set; }
    /// <summary>The handover request of a prepaid pickup - its replay key (a collected one replays by CollectionRequestId).</summary>
    public string? HandoverRequestId { get; set; }

    /// <summary>INV-0011 / FIN-0044 (owner decision 2026-11-22): when the order's business day closed,
    /// its food cost was frozen - each line's FrozenUnitFoodCostPiastres and this order's packaging -
    /// so a later recipe or purchase-price change never moves a closed day's food cost or profit.
    /// Null = not frozen yet (its day is still open): the food cost is the live estimate.</summary>
    public DateTimeOffset? FoodCostFrozenAtUtc { get; set; }
    /// <summary>The order's packaging cost at the freeze; null with FoodCostFrozenAtUtc set = unknown then.</summary>
    public long? FrozenPackagingCostPiastres { get; set; }
    /// <summary>The packaging named as unpriced at the freeze, when FrozenPackagingCostPiastres is null.</summary>
    public string? FrozenPackagingMissing { get; set; }

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

    /// <summary>INV-0011 / FIN-0044: one unit's food cost frozen at business-day close (see
    /// OrderEntity.FoodCostFrozenAtUtc); null on a frozen order = the cost was unknown then.</summary>
    public long? FrozenUnitFoodCostPiastres { get; set; }
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
