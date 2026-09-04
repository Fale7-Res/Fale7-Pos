namespace Falah.Application.Shifts;

/// <summary>Step 1: the cashier enters what they physically counted BEFORE the system reveals what
/// it expected ("blind" close). Read-only - commits nothing, so the cashier can back out.</summary>
public sealed record PrepareShiftCloseRequest(string ShiftId, long ActualCashCountedPiastres);

public sealed record PrepareShiftCloseResult(
    string ShiftId, long ExpectedCashPiastres, long ActualCashPiastres, long DifferencePiastres, bool RequiresManagerApproval);

/// <summary>Step 2: commits the close. CloseRequestId is minted once per "confirm close" button
/// press and must be reused unchanged by a retry - same idempotency contract as
/// CheckoutOrderRequest.CheckoutRequestId. ManagerApprovalId/ManagerApproverUserId are only
/// required when PrepareShiftCloseResult.RequiresManagerApproval was true - same shape as
/// RefundOrderRequest's approval fields.</summary>
public sealed record CloseShiftRequest(
    string CloseRequestId, string ShiftId, string ClosedByUserId, long ActualCashCountedPiastres, string? Reason,
    string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record CloseShiftResult(
    string ShiftId, string Status, long ExpectedCashPiastres, long ActualCashPiastres, long DifferencePiastres,
    bool WasReplayed);

public sealed class ShiftCloseConflictException : Exception
{
    public ShiftCloseConflictException(string message) : base(message) { }
}
