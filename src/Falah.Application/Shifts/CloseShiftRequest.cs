namespace Falah.Application.Shifts;

/// <summary>Step 1: the cashier enters what they physically counted BEFORE the system reveals what
/// it expected ("blind" close). Read-only - commits nothing, so the cashier can back out.</summary>
public sealed record PrepareShiftCloseRequest(string ShiftId, long ActualCashCountedPiastres);

public sealed record PrepareShiftCloseResult(
    string ShiftId, long ExpectedCashPiastres, long ActualCashPiastres, long DifferencePiastres, bool RequiresManagerApproval);

/// <summary>One cash count typed for a shift's close, in the order they were typed. More than one
/// means the count was changed after the expected cash was shown.</summary>
public sealed record ShiftCashDeclarationDto(int Sequence, long CountedPiastres, string DeclaredBy, DateTimeOffset DeclaredAtUtc);

/// <summary>Step 2: commits the close. CloseRequestId is minted once per "confirm close" button
/// press and must be reused unchanged by a retry - same idempotency contract as
/// CheckoutOrderRequest.CheckoutRequestId. ManagerApprovalId/ManagerApproverUserId are only
/// required when PrepareShiftCloseResult.RequiresManagerApproval was true - same shape as
/// RefundOrderRequest's approval fields.
///
/// Physical handover (owner, 2026-11-08): of the counted cash, RetainedFloatPiastres stays in the drawer for the
/// next cashier, and the rest is physically handed to HandoverReceiverUserId (moved drawer -> main). Left null,
/// everything counted is handed over, which is the behaviour before the split. The receiver is the manager who
/// takes the cash.</summary>
public sealed record CloseShiftRequest(
    string CloseRequestId, string ShiftId, string ClosedByUserId, long ActualCashCountedPiastres, string? Reason,
    string? ManagerApprovalId, string? ManagerApproverUserId,
    long? RetainedFloatPiastres = null, string? HandoverReceiverUserId = null);

public sealed record CloseShiftResult(
    string ShiftId, string Status, long ExpectedCashPiastres, long ActualCashPiastres, long DifferencePiastres,
    bool WasReplayed, long RetainedFloatPiastres = 0, long HandedOverPiastres = 0, string? HandoverReceiverName = null,
    string? HandoverReceiptStatus = null);

/// <summary>P4: the receiver confirms the cash handed over at a close. Either the receiver's own session sends it, or the
/// receiver types a PIN at the till (an approval issued for shifts_manage, ReceiverApprovalId) - choosing a name at the
/// close is not a receipt. ReceivedPiastres is what the receiver counted; a different amount records a dispute (a note
/// is then required) and moves no money. RequestId replays.</summary>
public sealed record AcknowledgeShiftHandoverRequest(
    string RequestId, string ShiftId, string ActorUserId, long ReceivedPiastres, string? Note,
    string? ReceiverApprovalId = null, string? ReceiverApproverUserId = null);

/// <summary>One closed shift's handover and its receipt. Status: "pending", "acknowledged", "disputed", or
/// "not_recorded" for a shift closed before receipts were recorded.</summary>
public sealed record ShiftHandoverReceiptDto(
    string ShiftId, string ShiftName, DateOnly BusinessDate, DateTimeOffset? ClosedAtUtc, string? ClosedBy,
    long HandedOverPiastres, string? ReceiverUserId, string? ReceiverName, string Status, long? ReceivedPiastres,
    long? DifferencePiastres, string? AcknowledgedBy, DateTimeOffset? AcknowledgedAtUtc, string? Note, bool WasReplayed = false);

public sealed class ShiftCloseConflictException : Exception
{
    public ShiftCloseConflictException(string message) : base(message) { }
}
