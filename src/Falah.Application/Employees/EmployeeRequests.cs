namespace Falah.Application.Employees;

/// <summary>Ported from Omit&lt;Employee, 'id'|'pin'|'userId'&gt; in addEmployee - a new roster
/// record never carries a login credential or account link (see Falah.Domain.Employees.Employee's
/// own doc comment for why no Pin field exists in this port at all).</summary>
public sealed record CreateEmployeeRequest(
    string RequestId, string Name, string EmployeeNumber, string JobTitle, string Phone, DateOnly HireDate,
    string CompensationType, long DailyWagePiastres, long ShiftValuePiastres, long? MonthlySalaryPiastres,
    bool CanDeliver, string Role, string? Notes, string? Address, string? NationalId, string? Department,
    string? EmploymentArrangement, string? FingerprintId, string? LifecycleStatus, long? OvertimeRatePiastres,
    string ActorUserId);

/// <summary>Ported from updateEmployee - a full profile replace (name/jobTitle/phone/compensation/
/// etc.), matching the reference app's own "pass the whole object" shape. UserId is intentionally
/// absent from this request (see Employee's own doc comment: no evidenced command ever changes it).</summary>
public sealed record UpdateEmployeeRequest(
    string EmployeeId, string Name, string EmployeeNumber, string JobTitle, string Phone, DateOnly HireDate,
    string CompensationType, long DailyWagePiastres, long ShiftValuePiastres, long? MonthlySalaryPiastres,
    bool CanDeliver, string Role, bool IsActive, string? Notes, string? Address, string? NationalId,
    string? Department, string? EmploymentArrangement, string? FingerprintId, string? LifecycleStatus,
    long? OvertimeRatePiastres, string ActorUserId);

public sealed record EmployeeDto(
    string Id, string Name, string EmployeeNumber, string JobTitle, string Phone, DateOnly HireDate,
    string CompensationType, long DailyWagePiastres, long ShiftValuePiastres, long? MonthlySalaryPiastres,
    string? UserId, bool CanDeliver, string Role, bool IsActive, string? Notes, string? Address,
    string? NationalId, string? Department, string? EmploymentArrangement, string? FingerprintId,
    string? LifecycleStatus, long? OvertimeRatePiastres);

public sealed record EmployeeLedgerEntryDto(
    string Id, string EmployeeId, string Type, string Description, long DebitPiastres, long CreditPiastres,
    long BalanceAfterPiastres, string RecordedBy, string? AuthorizedBy, string? ShiftId, string? ReferenceId,
    double? Hours, long? RateSnapshotPiastres, long? ValueSnapshotPiastres, string? PayrollRunId, DateTimeOffset CreatedAtUtc);

/// <summary>Ported from recordManualLedgerEntry - a real, un-gated command in the reference app
/// (no authorizeCommand call in that function at all); a real gap closed here, matching this
/// codebase's established pattern of fixing an un-gated write rather than reproducing it (see
/// PurchasingScreen.tsx's own addSupplier fix for the precedent this follows). Gated on
/// `employees_ledger`, the same permission its sibling advance/withdrawal commands already use.
/// No RequestId/idempotency key: matches legacy's own metadata.id-or-random shape, but a manual
/// entry (unlike an automated biometric posting) has no natural dedup key of its own - a caller
/// wanting idempotency should use RecordAdvance/RecordWithdrawal instead, which do have one.</summary>
public sealed record RecordManualLedgerEntryRequest(
    string EmployeeId, string Type, long AmountPiastres, bool IsCredit, string Description,
    string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

/// <summary>Ported from postEmployeeCashOut (recordAdvance/recordWithdrawal share this one
/// implementation in lib/store.tsx) - always sourced from tre-drawer, requires an open shift and
/// business day, gated on `employees_ledger`. Kind is "advance" or "withdrawal"; withdrawal alone
/// is additionally bounded by the employee's current ledger balance (AssertWithdrawalWithinBalance).</summary>
public sealed record RecordEmployeeCashOutRequest(
    string RequestId, string EmployeeId, string Kind, long AmountPiastres, string Reason,
    string ShiftId, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record EmployeeCashOutResult(string LedgerEntryId, long BalanceAfterPiastres, string? TreasuryMovementId, bool WasReplayed);
