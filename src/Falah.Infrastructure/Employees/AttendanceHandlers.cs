namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Domain.Common;
using Falah.Domain.Employees;
using Falah.Domain.Payroll;
using Falah.Domain.Shifts;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from biometricCheckIn in lib/store.tsx. Deliberately has NO permission check
/// (see BiometricCheckInRequest's own doc comment) - operating the biometric device/simulator IS
/// the authorization. Internally posts a daily-wage credit (compensationType='daily' only, via the
/// EmployeeLedgerPoster helper directly, NOT through the employees_ledger-gated RecordDailyWageHandler
/// - an automatic system consequence of clocking in is not the same authorization class as a
/// manager manually crediting a wage) and, when late beyond grace, an automatic late-penalty
/// deduction - both deduped by their own deterministic ids so a retried/duplicate biometric event
/// is always a safe no-op.</summary>
public sealed class BiometricCheckInHandler
{
    private readonly FalahDbContext _db;
    public BiometricCheckInHandler(FalahDbContext db) => _db = db;

    public async Task<AttendanceRecordDto?> HandleAsync(BiometricCheckInRequest request, CancellationToken ct = default)
    {
        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);
        if (employee is null) return null;

        var now = DateTimeOffset.UtcNow;
        var date = BusinessClock.Today();
        var timeStr = now.ToString("HH:mm");
        var settings = await new SettingsQueryService(_db).GetAsync(ct);

        var activeShift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == nameof(ShiftStatus.Open), ct);
        var lateMinutes = 0;
        if (activeShift is not null)
            lateMinutes = Math.Max(0, (int)Math.Round((now - activeShift.OpenedAtUtc).TotalMinutes));
        var status = lateMinutes > settings.LateGraceMinutes ? AttendanceStatus.Late : AttendanceStatus.Present;

        var existing = await _db.AttendanceRecords.FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.Date == date, ct);
        AttendanceRecordEntity entity;
        if (existing is not null)
        {
            existing.CheckIn = timeStr;
            existing.Status = status.ToString();
            existing.LateMinutes = lateMinutes;
            existing.LastCheckInEventId = request.BiometricEventId;
            existing.Source = "biometric";
            entity = existing;
        }
        else
        {
            entity = new AttendanceRecordEntity
            {
                Id = $"att-{Guid.NewGuid():N}", EmployeeId = request.EmployeeId, EmployeeName = employee.Name, Date = date,
                CheckIn = timeStr, ShiftName = activeShift?.ShiftName ?? "وردية صباحية", Status = status.ToString(),
                LateMinutes = lateMinutes, WorkHours = 0, OvertimeHours = 0, TatbiqCount = 0, IsManualCorrection = false,
                LastCheckInEventId = request.BiometricEventId, Source = "biometric", CreatedAtUtc = now,
            };
            _db.AttendanceRecords.Add(entity);
        }

        // Daily wage entitlement - created exactly once per employee/date/shift (see
        // RecordDailyWageHandler's own deterministic id scheme, reused here verbatim).
        if (employee.CompensationType == nameof(CompensationType.Daily))
        {
            var wage = Money.FromPiastres(employee.DailyWagePiastres);
            var wageId = $"led-daily-wage-{request.EmployeeId}-{date:yyyy-MM-dd}-{activeShift?.Id ?? "no-shift"}";
            await EmployeeLedgerPoster.PostIfNewAsync(_db, wageId, request.EmployeeId, nameof(LedgerEntryType.DailyWage),
                $"تسجيل يومية عمل ({wage.Piastres} قرش)", Money.Zero, wage, employee.Name, null, activeShift?.Id, null, null, null, wage.Piastres, ct);
        }

        // Automatic late penalty - keyed by employee+date so a duplicate biometric event never
        // creates a second deduction.
        if (lateMinutes > settings.LateGraceMinutes && employee.DailyWagePiastres > 0)
        {
            var penalty = PayrollRules.CalculateLateDeduction(Money.FromPiastres(employee.DailyWagePiastres),
                settings.StandardWorkdayHours, lateMinutes, settings.LateGraceMinutes, settings.MaxLatePenaltyHours, settings.LatePenaltyMultiplier);
            if (penalty.Amount.IsPositive)
            {
                var penaltyId = $"led-late-penalty-{request.EmployeeId}-{date:yyyy-MM-dd}";
                await EmployeeLedgerPoster.PostIfNewAsync(_db, penaltyId, request.EmployeeId, nameof(LedgerEntryType.Deduction),
                    $"خصم تأخير تلقائي ({lateMinutes} دقيقة ← {penalty.Hours} ساعة جزائية × {penalty.Rate.Piastres} قرش/ساعة × {Math.Round(settings.LatePenaltyMultiplier * 100)}%)",
                    penalty.Amount, Money.Zero, employee.Name, null, activeShift?.Id, "biometric-late-penalty", penalty.Hours, penalty.Rate.Piastres, null, ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        return EmployeeQueryService.ToAttendanceDto(entity);
    }
}

/// <summary>Ported from biometricCheckOut - throws if there is no check-in record for today
/// (a real gap the reference app itself fixed: previously proceeded silently and misrepresented an
/// uncheckedin day as a full standard workday).</summary>
public sealed class BiometricCheckOutHandler
{
    private readonly FalahDbContext _db;
    public BiometricCheckOutHandler(FalahDbContext db) => _db = db;

    public async Task<AttendanceRecordDto> HandleAsync(BiometricCheckOutRequest request, CancellationToken ct = default)
    {
        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new InvalidOperationException("الموظف غير موجود.");

        var now = DateTimeOffset.UtcNow;
        var date = BusinessClock.Today();
        var timeStr = now.ToString("HH:mm");
        var settings = await new SettingsQueryService(_db).GetAsync(ct);

        var record = await _db.AttendanceRecords.FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.Date == date, ct)
            ?? throw new DomainException("لا يوجد تسجيل حضور لهذا الموظف اليوم - لا يمكن تسجيل انصراف بدون حضور.");
        var activeShiftId = await _db.Shifts.AsNoTracking().Where(s => s.Status == nameof(ShiftStatus.Open)).Select(s => s.Id).FirstOrDefaultAsync(ct);

        double workHours = 0, overtimeHours = 0;
        if (!string.IsNullOrEmpty(record.CheckIn) && TimeOnly.TryParse(record.CheckIn, out var checkInTime))
        {
            var checkInAt = now.Date.Add(checkInTime.ToTimeSpan());
            var workedMinutes = Math.Max(0, (now.UtcDateTime - checkInAt).TotalMinutes);
            workHours = Math.Round(workedMinutes / 60.0, 2);
            overtimeHours = Math.Max(0, Math.Round(workHours - settings.StandardWorkdayHours, 2));
        }

        record.CheckOut = timeStr;
        record.WorkHours = workHours;
        record.OvertimeHours = overtimeHours;
        record.LastCheckOutEventId = request.BiometricEventId;

        if (overtimeHours > 0 && employee.DailyWagePiastres > 0)
        {
            var overtimeResult = PayrollRules.CalculateOvertimeAmount(Money.FromPiastres(employee.DailyWagePiastres),
                settings.StandardWorkdayHours, overtimeHours, settings.OvertimeMultiplier);
            if (overtimeResult.Amount.IsPositive)
            {
                var overtimeId = $"led-overtime-auto-{request.EmployeeId}-{date:yyyy-MM-dd}";
                await EmployeeLedgerPoster.PostIfNewAsync(_db, overtimeId, request.EmployeeId, nameof(LedgerEntryType.Overtime),
                    $"ساعات إضافية تلقائية من البصمة ({overtimeHours} ساعة × {overtimeResult.Rate.Piastres} قرش/ساعة × {Math.Round(settings.OvertimeMultiplier * 100)}%)",
                    Money.Zero, overtimeResult.Amount, employee.Name, null, activeShiftId, null, overtimeHours, overtimeResult.Rate.Piastres, null, ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        return EmployeeQueryService.ToAttendanceDto(record);
    }
}
