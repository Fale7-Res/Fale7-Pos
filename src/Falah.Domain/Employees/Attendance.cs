namespace Falah.Domain.Employees;

/// <summary>Ported from AttendanceStatus in types.ts.</summary>
public enum AttendanceStatus { Present, Late, Absent, Leave, MissingFingerprint }

/// <summary>Ported from AttendanceRecord in types.ts - one row per (employee, date), created on
/// first check-in and updated in place by check-out/correction, never duplicated for the same day.</summary>
public sealed record AttendanceRecord(
    string Id, string EmployeeId, string EmployeeName, DateOnly Date, string? CheckIn, string? CheckOut,
    string ShiftName, AttendanceStatus Status, int LateMinutes, double WorkHours, double OvertimeHours,
    int TatbiqCount, bool IsManualCorrection, string? CorrectedBy, string? CorrectionReason,
    string? LastCheckInEventId, string? LastCheckOutEventId, string? Source);
