namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Domain.Employees;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class EmployeeQueryService
{
    private readonly FalahDbContext _db;
    public EmployeeQueryService(FalahDbContext db) => _db = db;

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
