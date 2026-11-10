namespace Falah.Application.Orders;

/// <summary>Voids a still-pending InstaPay/wallet claim that never actually arrived - the order
/// itself is cancelled (never a "refund", since no money was ever collected). Ported from
/// cancelPendingElectronicPayment in lib/store.tsx. No separate idempotency-request-id: like
/// ConfirmElectronicPaymentRequest, the order's own Status/ElectronicPaymentStatus IS the
/// idempotency marker (cancelling an already-cancelled order is a harmless no-op).</summary>
/// <param name="StockDisposition">Whether the order's food was prepared ("prepared" - its ingredients
/// are recorded as waste) or not ("not_prepared" - they go back into stock). Required: checkout took
/// them out of stock, and only the operator knows which of the two happened.</param>
public sealed record CancelPendingElectronicPaymentRequest(string OrderId, string Reason, string CashierId, string? StockDisposition = null);

public sealed record CancelPendingElectronicPaymentResult(
    string OrderId, string Status, string ElectronicPaymentStatus, bool WasReplayed, long CashToHandBackPiastres = 0);
