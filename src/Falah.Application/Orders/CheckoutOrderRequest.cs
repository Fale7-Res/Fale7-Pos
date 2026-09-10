namespace Falah.Application.Orders;

/// <summary>
/// A client sends WHAT was selected, never what it costs - no price, name, or "send to grill" flag
/// travels in this request. The server looks up the product/variant, resolves any requested price
/// rule against what is actually live right now, and freezes the result into the order. See
/// CheckoutOrderHandler for the resolution step.
/// </summary>
public sealed record CheckoutItemRequest(
    string ProductId, string? VariantId, int Quantity, string? RequestedPriceRuleId, string? Notes);

/// <summary>Method is one of "cash" / "card" / "instapay" / "wallet" (never "multi" - a split leg
/// is always one of the four concrete methods).</summary>
public sealed record CheckoutSplitRequest(string Method, long AmountPiastres);

/// <summary>
/// What a client (Cashier app today; any future station tomorrow) sends to create/checkout an
/// order. CheckoutRequestId is minted ONCE per checkout session on the client and must be reused
/// unchanged by a same-task retry or a network retry - it is the whole idempotency contract.
/// </summary>
public sealed record CheckoutOrderRequest(
    string CheckoutRequestId,
    string CashierId,
    string ShiftId,
    string OrderType,
    string PaymentMethod,
    IReadOnlyList<CheckoutItemRequest> Items,
    long DiscountPiastres,
    long DeliveryFeePiastres,
    long CompAmountPiastres,
    string? CompApprovalId,
    string? CompApproverUserId,
    string? CompReason,
    long AmountPaidPiastres,
    IReadOnlyList<CheckoutSplitRequest>? SplitPayments,
    string? WalletProvider,
    string? CustomerName,
    string? CustomerPhone,
    /// <summary>Free-text one-liner from the client, kept for backward source-compat - the actual
    /// persisted DeliveryAddress is always the server-composed one (DeliveryRules.
    /// ComposeDeliveryAddress from the structured parts below), never trusted raw from a client.</summary>
    string? DeliveryAddress,
    string? DeliveryArea,
    string? BusinessDayId,
    string? DeliveryStreet = null,
    string? DeliveryBuilding = null,
    string? DeliveryFloor = null,
    string? DeliveryApartment = null,
    string? DeliveryLandmark = null,
    string? DeliveryNotes = null,
    string? DeliveryServiceId = null,
    string? DeliveryServiceName = null,
    /// <summary>One of PricingRules.CompReasonCodes ("hospitality"/"complaint"/"kitchen_error"/
    /// "staff_meal"/"owner_guest") - required whenever CompAmountPiastres &gt; 0, ported from the
    /// reference app's own compReasonCode/compReasonLabel taxonomy (types.ts) so "how much was
    /// given away, by reason" reporting is possible. CompReason above remains the separate free-text
    /// note.</summary>
    string? CompReasonCode = null,
    /// <summary>Which customer-facing ticket this station prints - "Receipt" for a till, "CallCenter"
    /// for a phoned-in confirmation copy. Null means Receipt. The station is the only thing that
    /// knows which of the two it is, so it is the only thing that can say.</summary>
    string? CustomerPrintRoute = null);

public sealed record CheckoutOrderResult(
    string OrderId,
    string OrderNumber,
    long SequenceNumber,
    long TotalPiastres,
    long AmountPaidPiastres,
    long ChangeDuePiastres,
    string Status,
    bool IsPaid,
    string? ElectronicPaymentStatus,
    bool WasReplayed,
    /// <summary>False when the sale succeeded but its tickets could not be queued. The sale is
    /// never failed for a printing problem - but the operator must still be told, or a ticket that
    /// never reached the kitchen looks exactly like one that did. Reprint from Order Lookup.</summary>
    bool PrintJobsQueued = true);

/// <summary>Thrown when a write hits a genuine, unresolvable idempotency-key collision at the
/// database's own constraint level that is NOT a legitimate replay (no matching Order exists to
/// return instead) - a state that should only ever arise from an operator/integration bug, never
/// from ordinary retries. The Server layer maps this to HTTP 409.</summary>
public sealed class CheckoutConflictException : Exception
{
    public CheckoutConflictException(string message) : base(message) { }
}

public sealed class ShiftNotOpenException : Exception
{
    public ShiftNotOpenException(string message) : base(message) { }
}
