namespace Falah.Infrastructure.Persistence.Entities;

/// <summary>Ported from PayrollRun in enterprise-types.ts - one run per period (YYYY-MM), status
/// machine ported verbatim from PAYROLL_TRANSITIONS in lib/payroll-domain.mjs.</summary>
public class PayrollRunEntity
{
    public string Id { get; set; } = default!;
    public string Period { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? PaidAtUtc { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? OperationId { get; set; }
    public string? TreasuryMovementId { get; set; }
    public string? PaymentAccountId { get; set; }
    public string? IdempotencyKey { get; set; }
}

/// <summary>Ported from PayrollLine in enterprise-types.ts - one row per employee per run, computed
/// once at CreatePayroll time from that period's own ledger entries (never recomputed later, so a
/// later ledger correction never silently rewrites an already-drafted/paid run).</summary>
public class PayrollLineEntity
{
    public string Id { get; set; } = default!;
    public string PayrollRunId { get; set; } = default!;
    public string EmployeeId { get; set; } = default!;
    public long BasePiastres { get; set; }
    public long OvertimePiastres { get; set; }
    public long ExtraShiftsPiastres { get; set; }
    public long BonusesPiastres { get; set; }
    public long DeductionsPiastres { get; set; }
    public long AdvanceDeductionsPiastres { get; set; }
    public long NetPayablePiastres { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset? PaidAtUtc { get; set; }
    public string? SettlementEntryId { get; set; }
}
