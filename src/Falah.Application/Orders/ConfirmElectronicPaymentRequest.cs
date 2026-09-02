namespace Falah.Application.Orders;

/// <summary>No separate idempotency-request-id is needed here, unlike checkout/refund: confirming
/// an order's pending electronic payment is inherently a single meaningful action per order (there
/// is no legitimate case for "confirm the same order's payment twice as separate business
/// events"), so the order's own ElectronicPaymentStatus is the idempotency marker.</summary>
public sealed record ConfirmElectronicPaymentRequest(string OrderId, string CashierId);

public sealed record ConfirmElectronicPaymentResult(
    string OrderId, string ElectronicPaymentStatus, bool IsPaid, string? TreasuryMovementId, bool WasReplayed);

public sealed class ConfirmConflictException : Exception
{
    public ConfirmConflictException(string message) : base(message) { }
}
