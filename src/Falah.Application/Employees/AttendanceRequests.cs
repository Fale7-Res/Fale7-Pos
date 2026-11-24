namespace Falah.Application.Employees;

/// <summary>Ported from biometricCheckIn in lib/store.tsx - hardware-agnostic (a real fingerprint
/// device can call this the same way a simulator does). BiometricEventId is the request id: required,
/// and a retry with the same id replays. The actor needs attendance_manage (SEC-0037, owner decision
/// 2026-11-22); a device logs in as an account holding it.</summary>
public sealed record BiometricCheckInRequest(string EmployeeId, string? BiometricEventId, string ActorUserId);

public sealed record BiometricCheckOutRequest(string EmployeeId, string? BiometricEventId, string ActorUserId);

public sealed record AttendanceRecordDto(
    string Id, string EmployeeId, string EmployeeName, DateOnly Date, string? CheckIn, string? CheckOut,
    string ShiftName, string Status, int LateMinutes, double WorkHours, double OvertimeHours, int TatbiqCount,
    bool IsManualCorrection, string? CorrectedBy, string? CorrectionReason, string? Source);

/// <summary>Ported from correctAttendance - gated on `attendance_manage`, reconciles the delta
/// between what the automatic late-penalty/overtime calculation already posted and what the
/// correction implies, as a new 'adjustment' ledger entry (the original automatic entries are never
/// rewritten or deleted).</summary>
/// <summary>RequestId is minted once per "save correction" button press and must be reused
/// unchanged by a retry - a real idempotency gap found during re-verification: the handler
/// originally minted a fresh random id for each posted ledger adjustment on every call, so a
/// network retry or double-click could double-post the same correction's financial delta.</summary>
public sealed record CorrectAttendanceRequest(
    string RequestId, string RecordId, string Status, int LateMinutes, double WorkHours, double OvertimeHours, int TatbiqCount,
    string Reason, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

/// <summary>Ported from recordDailyWage - assertUniqueDailyWage's own deterministic id
/// (`led-daily-wage-{employeeId}-{date}-{shiftId|no-shift}`) is the actual idempotency mechanism,
/// exactly as legacy documents it: a retried/duplicate call for the same employee/date/shift is a
/// safe no-op, never a duplicate wage. Gated on `employees_ledger` for direct/manual invocation -
/// see BiometricCheckInHandler's own doc comment for why the AUTOMATIC posting from a check-in does
/// not go through this same authorized command.</summary>
public sealed record RecordDailyWageRequest(string EmployeeId, long? CustomWagePiastres, string? ShiftId, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId, string? RequestId = null);

/// <summary>RequestId is minted once per submit and reused unchanged by a retry. A real idempotency
/// gap found during re-verification: legacy's own recordOvertime never gives
/// recordManualLedgerEntry a metadata.id either, so a double-click genuinely double-posts overtime
/// pay there too - but this session's standing pattern is to close every such un-protected
/// financial write found rather than reproduce it (the same treatment already given to
/// recordManualLedgerEntry's own missing permission gate), so a real key is required here instead.</summary>
public sealed record RecordOvertimeRequest(string RequestId, string EmployeeId, double Hours, long? CustomRatePiastres, string? Notes, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

/// <summary>See RecordOvertimeRequest's own doc comment - recordExtraShift has the identical gap
/// in legacy, closed the same way here.</summary>
public sealed record RecordExtraShiftRequest(string RequestId, string EmployeeId, int ShiftCount, string? Notes, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);
