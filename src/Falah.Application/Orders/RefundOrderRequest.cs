namespace Falah.Application.Orders;

/// <summary>RefundRequestId is minted once per refund attempt (one press of "confirm refund") and
/// must be reused unchanged by a retry - same idempotency contract as CheckoutRequestId.</summary>
public sealed record RefundOrderRequest(
    string RefundRequestId,
    string OrderId,
    long AmountPiastres,
    string Reason,
    string CashierId,
    string? ManagerApprovalId,
    string? ManagerApproverUserId);

public sealed record RefundOrderResult(
    string RefundId,
    string OrderId,
    long AmountPiastres,
    string OrderStatus,
    long RemainingRefundablePiastres,
    string TreasuryMovementId,
    bool WasReplayed);

public sealed class RefundConflictException : Exception
{
    public RefundConflictException(string message) : base(message) { }
}
