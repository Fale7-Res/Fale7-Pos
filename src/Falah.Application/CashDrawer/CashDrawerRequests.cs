namespace Falah.Application.CashDrawer;

/// <summary>Ported from the 'drawer_open' case of addCashMovement in lib/store.tsx - the ONE case
/// of that command that posts no Treasury movement (opening a drawer moves no money by itself).
/// Cashier-machine-only by architecture (matching the same "Call Center never touches cash-drawer
/// hardware" boundary already documented for this row) - not server-enforced here any more than any
/// other client-scoped UI affordance in this system is (e.g. Call Center is simply never given the
/// shift-open button either), since there is no per-machine identity concept anywhere in this
/// system to enforce it against.</summary>
public sealed record OpenCashDrawerRequest(string RequestId, string ShiftId, string Reason, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record CashDrawerOpenDto(string Id, string ShiftId, string Reason, string RequestedBy, string? ApprovedBy, string Status, string? LastError, DateTimeOffset RequestedAtUtc);

/// <summary>Physical verification is explicitly deferred until real hardware testing - this exists
/// so the moment it happens, there is already a command to report through, exactly like
/// ReportPrintJobResultRequest for the printer side.</summary>
public sealed record ReportCashDrawerResultRequest(string CashDrawerOpenId, bool Succeeded, string? Error, string ReportedByUserId);

/// <summary>Ported from the 'cash_in'/'cash_out' cases of addCashMovement in lib/store.tsx - a
/// cashier manually moving cash between Main Treasury and their own drawer mid-shift (float top-up
/// or skim), distinct from both an owner deposit/withdrawal (Treasury/DepositHandler etc., gated on
/// treasury_manage) and a safe drop (always drawer-&gt;nowhere, gated on safe_drop). A real gap found
/// during the legacy-parity audit: this command did not exist at all despite ShiftReconciliation
/// already reading TreasuryMovements of exactly this type for blind-close expected-cash math.
/// MovementType is "cash_in" (tre-main -&gt; tre-drawer) or "cash_out" (tre-drawer -&gt; tre-main).</summary>
public sealed record CashierCashMovementRequest(
    string RequestId, string ShiftId, string MovementType, long AmountPiastres, string Reason,
    string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record CashierCashMovementResult(string TreasuryMovementId, string MovementType, long AmountPiastres, bool WasReplayed);
