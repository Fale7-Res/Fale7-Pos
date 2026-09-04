namespace Falah.Domain.Employees;

using Falah.Domain.Common;

/// <summary>Ported from CompensationType in types.ts - the required pay-basis every employee has,
/// distinct from the optional, looser EmploymentArrangement taxonomy below.</summary>
public enum CompensationType { Daily, Monthly, Hourly }

/// <summary>Ported from the optional `employmentType` field on Employee in types.ts - a looser,
/// separate classification from CompensationType (both exist simultaneously in the reference app;
/// this is not a duplicate invented here).</summary>
public enum EmploymentArrangement { Daily, Monthly, PartTime, Contract }

/// <summary>Ported from the optional `employmentStatus` field on Employee in types.ts.</summary>
public enum EmployeeLifecycleStatus { Active, Inactive, Terminated, Archived }

/// <summary>Ported from EmployeeLedgerEntry's own `type: LedgerEntryType` in types.ts.</summary>
public enum LedgerEntryType
{
    DailyWage, Salary, Advance, Withdrawal, Overtime, ExtraShift, Deduction, Bonus,
    OtherEarning, PayrollSettlement, Adjustment, Settlement,
}

/// <summary>Ported from Employee in types.ts. A real gap found during the legacy-parity audit: this
/// is genuine, real functional data (roster + financial identity) with no C# equivalent at all
/// before this pass - not a mockup like Device Management. Deliberately does NOT carry a `Pin`
/// field: every real command this system actually ports (biometricCheckIn/biometricCheckOut) takes
/// `employeeId` directly, never a PIN lookup - `Employee.pin`/`setEmployeePin` in the reference app
/// has no evidenced consumer among the commands this system's own Attendance slice needs, so
/// inventing a second (redundant, since only Users ever authenticates) hashed-PIN mechanism here
/// would be exactly the kind of unevidenced policy invention this pass was told not to do.
/// UserId (the optional linked login account) is carried as a plain reference for reporting/lookup
/// purposes - no evidenced command in the reference app ever actually sets it to a non-null value
/// (addEmployee always creates with userId=undefined, updateEmployee explicitly preserves the
/// existing value rather than accepting a new one), so this port does not invent a "link employee to
/// user account" command either.</summary>
public sealed record Employee(
    string Id, string Name, string EmployeeNumber, string JobTitle, string Phone, DateOnly HireDate,
    CompensationType CompensationType, Money DailyWage, Money ShiftValue, Money? MonthlySalary,
    string? UserId, bool CanDeliver, string Role, bool IsActive, string? Notes, string? Address,
    string? NationalId, string? Department, EmploymentArrangement? EmploymentArrangement,
    string? FingerprintId, EmployeeLifecycleStatus? LifecycleStatus, Money? OvertimeRate);

/// <summary>Ported from EmployeeLedgerEntry in types.ts - the append-only financial history behind
/// Employee.calculateEmployeeBalance. Debit (سلفة/سحب/خصم) and Credit (يومية/مرتب/تطبيق/إضافي/مكافأة)
/// are kept as two separate non-negative amounts on the same row (never a single signed amount) -
/// matching the reference app's own shape, which is itself what makes a ledger row readable as a
/// classic debit/credit accounting entry.</summary>
public sealed record EmployeeLedgerEntry(
    string Id, string EmployeeId, LedgerEntryType Type, string Description, Money Debit, Money Credit,
    Money BalanceAfter, string RecordedBy, string? AuthorizedBy, string? ShiftId, string? ReferenceId,
    double? Hours, long? RateSnapshotPiastres, long? ValueSnapshotPiastres, string? PayrollRunId, DateTimeOffset CreatedAtUtc);
