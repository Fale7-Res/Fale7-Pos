namespace Falah.Application.Shifts;

/// <summary>RequestId is minted once per "open shift" button press and must be reused unchanged by
/// a retry - same idempotency contract as CheckoutOrderRequest.CheckoutRequestId.</summary>
public sealed record OpenShiftRequest(string RequestId, string ShiftName, string OpenedByUserId, long OpeningCashPiastres);

public sealed record OpenShiftResult(string ShiftId, string ShiftName, string Status, long OpeningCashPiastres, bool WasReplayed);

/// <summary>The one currently open shift, if any - a station with no shift-open UI of its own (Call
/// Center) reads this to discover which shift id to attach its checkout requests to, matching the
/// reference app's own single system-wide active-shift model (activeShift in lib/store.tsx).</summary>
public sealed record CurrentShiftDto(string ShiftId, string ShiftName, string OpenedByUserId, long OpeningCashPiastres, DateTimeOffset OpenedAtUtc,
    /// <summary>Who has the shift open, in plain text - the Shifts screen shows this unconditionally
    /// (not gated behind reports_view, unlike the sales figures) so anyone looking at an open shift
    /// can tell whose it is. Trailing and optional so the one other consumer (Call Center, which only
    /// needs the shift id to attach its own checkout requests to) keeps compiling unchanged.</summary>
    string? OpenedByUserName = null);

/// <summary>Ported from openNewShift's own guard: `if (activeShift) throw new Error('توجد وردية
/// مفتوحة بالفعل...')`. There is only ever one active shift system-wide - a second open attempt
/// must close the first one first, never quietly proceed alongside it.</summary>
public sealed class ShiftAlreadyOpenException : Exception
{
    public ShiftAlreadyOpenException(string message) : base(message) { }
}
