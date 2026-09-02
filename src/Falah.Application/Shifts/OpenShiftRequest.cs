namespace Falah.Application.Shifts;

/// <summary>RequestId is minted once per "open shift" button press and must be reused unchanged by
/// a retry - same idempotency contract as CheckoutOrderRequest.CheckoutRequestId.</summary>
public sealed record OpenShiftRequest(string RequestId, string ShiftName, string OpenedByUserId, long OpeningCashPiastres);

public sealed record OpenShiftResult(string ShiftId, string ShiftName, string Status, long OpeningCashPiastres, bool WasReplayed);
