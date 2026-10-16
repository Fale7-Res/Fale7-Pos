namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Domain.Common;
using Falah.Domain.Employees;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class EmployeeQueryService
{
    private readonly FalahDbContext _db;
    public EmployeeQueryService(FalahDbContext db) => _db = db;

    /// <summary>Payroll/employee financial reporting "by today / last 7 days / last 10 days / month
    /// / custom range" - the report the final business rule asks for now that payment is not tied to
    /// a payroll period. Six buckets, one row per employee, covering all twelve LedgerEntryType
    /// values with no overlap: Earnings (DailyWage/Salary/Overtime/ExtraShift credit), Additions
    /// (Bonus/OtherEarning credit), Deductions (Deduction/Adjustment debit), Advances (Advance debit,
    /// kept separate per the explicit "do not merge advance with normal payment" rule), Payments
    /// (Withdrawal/Settlement/PayrollSettlement debit - every way money already earned actually
    /// leaves, whichever flow posted it). RemainingBalancePiastres is deliberately the all-time
    /// running balance (EmployeeRules.CalculateBalance), not range-scoped - a running balance has no
    /// "as of the range" version, same convention DailyStatementQueryService's own employee rows use.</summary>
    public async Task<List<EmployeeFinancialReportLineDto>> GetFinancialReportAsync(
        DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        DateTimeOffset? start = null, end = null;
        if (from is { } f) start = BusinessClock.UtcRangeForBusinessDate(f).Start;
        if (to is { } t) end = BusinessClock.UtcRangeForBusinessDate(t).End;

        var employees = await _db.Employees.AsNoTracking().Where(e => e.IsActive).ToListAsync(ct);
        var allEntries = await _db.EmployeeLedgerEntries.AsNoTracking()
            .Select(e => new { e.EmployeeId, e.Type, e.CreditPiastres, e.DebitPiastres, e.CreatedAtUtc })
            .ToListAsync(ct);

        var earningsTypes = new[] { nameof(LedgerEntryType.DailyWage), nameof(LedgerEntryType.Salary), nameof(LedgerEntryType.Overtime), nameof(LedgerEntryType.ExtraShift) };
        var additionTypes = new[] { nameof(LedgerEntryType.Bonus), nameof(LedgerEntryType.OtherEarning) };
        var deductionTypes = new[] { nameof(LedgerEntryType.Deduction), nameof(LedgerEntryType.Adjustment) };
        var paymentTypes = new[] { nameof(LedgerEntryType.Withdrawal), nameof(LedgerEntryType.Settlement), nameof(LedgerEntryType.PayrollSettlement) };

        var result = new List<EmployeeFinancialReportLineDto>(employees.Count);
        foreach (var employee in employees)
        {
            var allForEmployee = allEntries.Where(e => e.EmployeeId == employee.Id).ToList();
            var inRange = allForEmployee.Where(e =>
                (start is null || e.CreatedAtUtc >= start) && (end is null || e.CreatedAtUtc < end)).ToList();

            long SumCredit(IEnumerable<string> types) => inRange.Where(e => types.Contains(e.Type)).Sum(e => e.CreditPiastres);
            long SumDebit(IEnumerable<string> types) => inRange.Where(e => types.Contains(e.Type)).Sum(e => e.DebitPiastres);
            var balance = allForEmployee.Sum(e => e.CreditPiastres) - allForEmployee.Sum(e => e.DebitPiastres);

            result.Add(new EmployeeFinancialReportLineDto(
                employee.Id, employee.Name, SumCredit(earningsTypes), SumCredit(additionTypes),
                SumDebit(deductionTypes), SumDebit(new[] { nameof(LedgerEntryType.Advance) }),
                SumDebit(paymentTypes), balance));
        }
        return result.OrderBy(r => r.EmployeeName).ToList();
    }

    public Task<List<EmployeeDto>> ListAsync(CancellationToken ct = default) =>
        _db.Employees.AsNoTracking().Select(e => ToDto(e)).ToListAsync(ct);

    public async Task<EmployeeDto> GetAsync(string employeeId, CancellationToken ct = default) =>
        ToDto(await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId, ct)
            ?? throw new InvalidOperationException("الموظف غير موجود."));

    public Task<List<EmployeeLedgerEntryDto>> ListLedgerEntriesAsync(string employeeId, CancellationToken ct = default) =>
        _db.EmployeeLedgerEntries.AsNoTracking().Where(e => e.EmployeeId == employeeId)
            .OrderByDescending(e => e.CreatedAtUtc).Select(e => ToLedgerDto(e)).ToListAsync(ct);

    /// <summary>Ported verbatim from calculateEmployeeBalance: sum(credit) - sum(debit) across
    /// every ledger entry for this employee.</summary>
    public async Task<long> GetBalanceAsync(string employeeId, CancellationToken ct = default)
    {
        var entries = await _db.EmployeeLedgerEntries.AsNoTracking().Where(e => e.EmployeeId == employeeId)
            .Select(e => new { e.DebitPiastres, e.CreditPiastres }).ToListAsync(ct);
        return entries.Sum(e => e.CreditPiastres) - entries.Sum(e => e.DebitPiastres);
    }

    internal static EmployeeDto ToDto(EmployeeEntity e) => new(
        e.Id, e.Name, e.EmployeeNumber, e.JobTitle, e.Phone, e.HireDate, e.CompensationType,
        e.DailyWagePiastres, e.ShiftValuePiastres, e.MonthlySalaryPiastres, e.UserId, e.CanDeliver,
        e.Role, e.IsActive, e.Notes, e.Address, e.NationalId, e.Department, e.EmploymentArrangement,
        e.FingerprintId, e.LifecycleStatus, e.OvertimeRatePiastres);

    internal static EmployeeLedgerEntryDto ToLedgerDto(EmployeeLedgerEntryEntity e) => new(
        e.Id, e.EmployeeId, e.Type, e.Description, e.DebitPiastres, e.CreditPiastres, e.BalanceAfterPiastres,
        e.RecordedBy, e.AuthorizedBy, e.ShiftId, e.ReferenceId, e.Hours, e.RateSnapshotPiastres,
        e.ValueSnapshotPiastres, e.PayrollRunId, e.CreatedAtUtc);

    public Task<List<AttendanceRecordDto>> ListAttendanceAsync(string? employeeId, DateOnly? date, CancellationToken ct = default)
    {
        var query = _db.AttendanceRecords.AsNoTracking().AsQueryable();
        if (employeeId is not null) query = query.Where(a => a.EmployeeId == employeeId);
        if (date is not null) query = query.Where(a => a.Date == date);
        return query.OrderByDescending(a => a.Date).Select(a => ToAttendanceDto(a)).ToListAsync(ct);
    }

    internal static AttendanceRecordDto ToAttendanceDto(AttendanceRecordEntity a) => new(
        a.Id, a.EmployeeId, a.EmployeeName, a.Date, a.CheckIn, a.CheckOut, a.ShiftName, a.Status, a.LateMinutes,
        a.WorkHours, a.OvertimeHours, a.TatbiqCount, a.IsManualCorrection, a.CorrectedBy, a.CorrectionReason, a.Source);
}
