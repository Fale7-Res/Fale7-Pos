namespace Falah.Application.Orders;

/// <summary>Voids a still-pending InstaPay/wallet claim that never actually arrived - the order
/// itself is cancelled (never a "refund", since no money was ever collected). Ported from
/// cancelPendingElectronicPayment in lib/store.tsx. No separate idempotency-request-id: like
/// ConfirmElectronicPaymentRequest, the order's own Status/ElectronicPaymentStatus IS the
/// idempotency marker (cancelling an already-cancelled order is a harmless no-op).</summary>
public sealed record CancelPendingElectronicPaymentRequest(string OrderId, string Reason, string CashierId);

public sealed record CancelPendingElectronicPaymentResult(
    string OrderId, string Status, string ElectronicPaymentStatus, bool WasReplayed);
