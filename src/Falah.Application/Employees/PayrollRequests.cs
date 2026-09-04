namespace Falah.Application.Employees;

/// <summary>Ported from createPayroll in lib/enterprise-store.tsx - one run per period (YYYY-MM),
/// idempotent on period (a second create for an already-active period for that same period is a
/// safe no-op, matching legacy's own `if (existing) return`). Lines are computed once here, from
/// that period's own employee ledger entries, and never recomputed later. Gated on
/// `employees_manage` - a real gap closed here (legacy's own createPayroll has no permission check
/// at all), per the explicit requirement that Payroll be "idempotent, auditable, permissioned, and
/// financially transactional."</summary>
public sealed record CreatePayrollRequest(string RequestId, string Period, string ActorUserId);

public sealed record PayrollRunDto(
    string Id, string Period, string Status, string CreatedBy, DateTimeOffset CreatedAtUtc, string? ApprovedBy,
    DateTimeOffset? PaidAtUtc, DateTimeOffset? ReviewedAtUtc, string? ReviewedBy, string? TreasuryMovementId,
    string? PaymentAccountId, bool WasReplayed = false);

public sealed record PayrollLineDto(
    string Id, string PayrollRunId, string EmployeeId, long BasePiastres, long OvertimePiastres,
    long ExtraShiftsPiastres, long BonusesPiastres, long DeductionsPiastres, long AdvanceDeductionsPiastres,
    long NetPayablePiastres, string Status, DateTimeOffset? PaidAtUtc);

/// <summary>Ported from transitionPayroll - draft→calculated→reviewed→approved→paid|cancelled
/// (PAYROLL_TRANSITIONS), with `approved` additionally requiring `payroll_approve` (permission or
/// approval) STACKED on top of the base `employees_manage` every transition requires - the same
/// "base gate + stacked gate for the sensitive step" shape AuthorizeShiftClose already uses.</summary>
public sealed record TransitionPayrollRequest(string PayrollRunId, string Status, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

/// <summary>Ported from payPayroll - gated on `payroll_pay`, requires the run to already be
/// Approved, requires an open business day, checks the paying account's protected balance for the
/// run's total net-payable, posts exactly one Treasury movement for the whole run plus one
/// 'payroll_settlement' ledger debit per employee line (reducing each paid employee's own running
/// balance by what they were just paid). Idempotent on `payroll-payment-{runId}`.</summary>
public sealed record PayPayrollRequest(string PayrollRunId, string PaymentAccountId, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record PayPayrollResult(string TreasuryMovementId, long TotalPaidPiastres, int SettlementCount, bool WasReplayed);
