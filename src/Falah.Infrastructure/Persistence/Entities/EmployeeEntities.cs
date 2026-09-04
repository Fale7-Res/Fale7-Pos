namespace Falah.Infrastructure.Persistence.Entities;

/// <summary>Ported from Employee in types.ts - see Falah.Domain.Employees.Employee's own doc
/// comment for why no Pin field is carried here.</summary>
public class EmployeeEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string EmployeeNumber { get; set; } = default!;
    public string JobTitle { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public DateOnly HireDate { get; set; }
    public string CompensationType { get; set; } = default!;
    public long DailyWagePiastres { get; set; }
    public long ShiftValuePiastres { get; set; }
    public long? MonthlySalaryPiastres { get; set; }
    public string? UserId { get; set; }
    public bool CanDeliver { get; set; }
    public string Role { get; set; } = default!;
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public string? Address { get; set; }
    public string? NationalId { get; set; }
    public string? Department { get; set; }
    public string? EmploymentArrangement { get; set; }
    public string? FingerprintId { get; set; }
    public string? LifecycleStatus { get; set; }
    public long? OvertimeRatePiastres { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Ported from EmployeeLedgerEntry in types.ts - append-only, no update/delete path
/// anywhere (matching the reference app, which only ever appends to this array too).</summary>
public class EmployeeLedgerEntryEntity
{
    public string Id { get; set; } = default!;
    public string EmployeeId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Description { get; set; } = default!;
    public long DebitPiastres { get; set; }
    public long CreditPiastres { get; set; }
    public long BalanceAfterPiastres { get; set; }
    public string RecordedBy { get; set; } = default!;
    public string? AuthorizedBy { get; set; }
    public string? ShiftId { get; set; }
    public string? ReferenceId { get; set; }
    public double? Hours { get; set; }
    public long? RateSnapshotPiastres { get; set; }
    public long? ValueSnapshotPiastres { get; set; }
    public string? PayrollRunId { get; set; }
    public string? TreasuryMovementId { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Ported from AttendanceRecord in types.ts - one row per (EmployeeId, Date), enforced by
/// a unique index (matching the reference app's own array `.find(a => a.employeeId===... &&
/// a.date===...)` upsert pattern).</summary>
public class AttendanceRecordEntity
{
    public string Id { get; set; } = default!;
    public string EmployeeId { get; set; } = default!;
    public string EmployeeName { get; set; } = default!;
    public DateOnly Date { get; set; }
    public string? CheckIn { get; set; }
    public string? CheckOut { get; set; }
    public string ShiftName { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int LateMinutes { get; set; }
    public double WorkHours { get; set; }
    public double OvertimeHours { get; set; }
    public int TatbiqCount { get; set; }
    public bool IsManualCorrection { get; set; }
    public string? CorrectedBy { get; set; }
    public string? CorrectionReason { get; set; }
    public string? LastCheckInEventId { get; set; }
    public string? LastCheckOutEventId { get; set; }
    public string? Source { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
