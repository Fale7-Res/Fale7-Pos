# src/Falah.Application/Employees/AttendanceRequests.cs

Employees API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `1e68615e544b3f852296dd0923218c6839a80a1a96beb69d4c56de3c6b550419`. Working SHA-256 `1e68615e544b3f852296dd0923218c6839a80a1a96beb69d4c56de3c6b550419`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-47.

Symbols indexed (index is not a claim every method was reviewed): BiometricCheckInRequest@8, BiometricCheckOutRequest@10, AttendanceRecordDto@12, CorrectAttendanceRequest@25, RecordDailyWageRequest@35, RecordOvertimeRequest@43, RecordExtraShiftRequest@47.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 8: `public sealed record BiometricCheckInRequest(string EmployeeId, string? BiometricEventId, string ActorUserId);`
- Line 10: `public sealed record BiometricCheckOutRequest(string EmployeeId, string? BiometricEventId, string ActorUserId);`
- Line 12: `public sealed record AttendanceRecordDto(`
- Line 25: `public sealed record CorrectAttendanceRequest(`
- Line 35: `public sealed record RecordDailyWageRequest(string EmployeeId, long? CustomWagePiastres, string? ShiftId, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);`
- Line 43: `public sealed record RecordOvertimeRequest(string RequestId, string EmployeeId, double Hours, long? CustomRatePiastres, string? Notes, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);`
- Line 47: `public sealed record RecordExtraShiftRequest(string RequestId, string EmployeeId, int ShiftCount, string? Notes, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);`

Employee and attendance payloads include pay rates, biometrics identifiers, national ID and address; do not expose to every authenticated station. Integer money avoids floating currency, while hours/multipliers need finite/range checks. Payroll status contract accepts arbitrary string; FIN-006 independent trace confirms Paid must route through payment handler. Manual ledger entry intentionally lacks request ID: retry safety is not guaranteed by DTO.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
