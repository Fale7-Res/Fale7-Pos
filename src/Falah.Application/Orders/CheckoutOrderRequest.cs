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
    string? DeliveryAddress,
    string? DeliveryArea,
    string? BusinessDayId);

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
    bool WasReplayed);

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
