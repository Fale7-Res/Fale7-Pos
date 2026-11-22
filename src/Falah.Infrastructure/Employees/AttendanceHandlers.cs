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

/// <summary>Who may record a check-in or check-out, and that it carries a request id (SEC-0037, owner
/// decision 2026-11-22). Recording attendance posts money - a daily wage, a late penalty, overtime -
/// so it needs attendance_manage, as correcting it does; a biometric device would log in as an account
/// holding that permission. The request id is what makes a retried event replay instead of post.</summary>
internal static class AttendanceRecording
{
    public static async Task AuthorizeAsync(FalahDbContext db, string actorUserId, string? requestId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestId)) throw new DomainException("رقم العملية مطلوب لتسجيل الحضور أو الانصراف.");
        var actor = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorUserId, ct)
            ?? throw new DomainException("ليس لديك صلاحية تسجيل الحضور والانصراف.");
        if (!Falah.Domain.Authorization.AuthorizationPolicy.HasPermission(Falah.Infrastructure.Orders.OrderMapper.ToActor(actor), "attendance_manage"))
            throw new DomainException("ليس لديك صلاحية تسجيل الحضور والانصراف.");
    }
}

/// <summary>Ported from biometricCheckIn in lib/store.tsx. Needs attendance_manage and a request id
/// (<see cref="AttendanceRecording"/>; until 2026-11-22 it had no permission check, as the legacy app
/// treated operating the biometric device as the authorization). Internally posts a daily-wage credit (compensationType='daily' only, via the
/// EmployeeLedgerPoster helper directly, NOT through the employees_ledger-gated RecordDailyWageHandler
/// - an automatic system consequence of clocking in is not the same authorization class as a
/// manager manually crediting a wage) and, when late beyond grace, an automatic late-penalty
/// deduction - both deduped by their own deterministic ids so a retried/duplicate biometric event
/// is always a safe no-op.</summary>
public sealed class BiometricCheckInHandler
{
    private readonly FalahDbContext _db;
    private readonly TimeProvider _time;
    public BiometricCheckInHandler(FalahDbContext db, TimeProvider? timeProvider = null) { _db = db; _time = timeProvider ?? TimeProvider.System; }

    public async Task<AttendanceRecordDto?> HandleAsync(BiometricCheckInRequest request, CancellationToken ct = default)
    {
        await AttendanceRecording.AuthorizeAsync(_db, request.ActorUserId, request.BiometricEventId, ct);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var employee = await _db.Employees.FromSqlRaw("SELECT * FROM Employees WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.EmployeeId).AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);
        if (employee is null) return null;

        if (!string.IsNullOrWhiteSpace(request.BiometricEventId) &&
            await _db.AttendanceRecords.AsNoTracking().FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.LastCheckInEventId == request.BiometricEventId, ct) is { } replay)
            return EmployeeQueryService.ToAttendanceDto(replay);
        var now = _time.GetUtcNow();
        var date = BusinessClock.BusinessDate(now);
        var timeStr = now.ToString("HH:mm");
        var settings = await new SettingsQueryService(_db).GetAsync(ct);

        var activeShift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == nameof(ShiftStatus.Open), ct);

        // Lateness is measured from when the open shift began - which is only a meaningful baseline
        // while that shift belongs to TODAY'S business date. A shift left open across days is not a
        // schedule anyone was late for: it produced "متأخر 6377 دقيقة" for someone clocking in
        // normally, and an automatic wage deduction followed it. With no usable baseline the system
        // records no lateness rather than a number it cannot stand behind - the same reason an
        // unknown cost is never reported as zero.
        var lateMinutes = 0;
        if (activeShift is not null && activeShift.BusinessDate == date)
            lateMinutes = Math.Max(0, (int)Math.Round((now - activeShift.OpenedAtUtc).TotalMinutes));
        var status = lateMinutes > settings.LateGraceMinutes ? AttendanceStatus.Late : AttendanceStatus.Present;

        var existing = await _db.AttendanceRecords.FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.Date == date, ct);
        AttendanceRecordEntity entity;
        if (existing is not null && !string.IsNullOrEmpty(existing.CheckIn)) return EmployeeQueryService.ToAttendanceDto(existing);
        entity = existing ?? new AttendanceRecordEntity
        {
            Id = $"att-{Guid.NewGuid():N}", EmployeeId = request.EmployeeId, EmployeeName = employee.Name, Date = date,
            ShiftName = activeShift?.ShiftName ?? "وردية صباحية", WorkHours = 0, OvertimeHours = 0,
            TatbiqCount = 0, IsManualCorrection = false, CreatedAtUtc = now,
        };
        entity.CheckIn = timeStr; entity.CheckInAtUtc = now; entity.Status = status.ToString();
        entity.LateMinutes = lateMinutes; entity.LastCheckInEventId = request.BiometricEventId; entity.Source = "biometric";
        if (existing is null) _db.AttendanceRecords.Add(entity);

        // Daily wage entitlement - created exactly once per employee/date/shift (see
        // RecordDailyWageHandler's own deterministic id scheme, reused here verbatim).
        if (Enum.TryParse<CompensationType>(employee.CompensationType, true, out var savedType) && savedType == CompensationType.Daily)
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
        await transaction.CommitAsync(ct);
        return EmployeeQueryService.ToAttendanceDto(entity);
    }
}

/// <summary>Ported from biometricCheckOut - throws if there is no check-in record for today
/// (a real gap the reference app itself fixed: previously proceeded silently and misrepresented an
/// uncheckedin day as a full standard workday).</summary>
public sealed class BiometricCheckOutHandler
{
    private readonly FalahDbContext _db;
    private readonly TimeProvider _time;
    public BiometricCheckOutHandler(FalahDbContext db, TimeProvider? timeProvider = null) { _db = db; _time = timeProvider ?? TimeProvider.System; }

    public async Task<AttendanceRecordDto> HandleAsync(BiometricCheckOutRequest request, CancellationToken ct = default)
    {
        await AttendanceRecording.AuthorizeAsync(_db, request.ActorUserId, request.BiometricEventId, ct);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var employee = await _db.Employees.FromSqlRaw("SELECT * FROM Employees WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.EmployeeId).AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new InvalidOperationException("الموظف غير موجود.");

        var now = _time.GetUtcNow();
        var date = BusinessClock.BusinessDate(now);
        var timeStr = now.ToString("HH:mm");
        var settings = await new SettingsQueryService(_db).GetAsync(ct);

        if (!string.IsNullOrWhiteSpace(request.BiometricEventId) &&
            await _db.AttendanceRecords.AsNoTracking().FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.LastCheckOutEventId == request.BiometricEventId, ct) is { } replay)
            return EmployeeQueryService.ToAttendanceDto(replay);
        var yesterday = date.AddDays(-1);
        var record = await _db.AttendanceRecords.Where(a => a.EmployeeId == request.EmployeeId && a.Date >= yesterday && a.Date <= date && a.CheckIn != null)
            .OrderBy(a => a.CheckOut != null).ThenByDescending(a => a.Date).FirstOrDefaultAsync(ct)
            ?? throw new DomainException("لا يوجد تسجيل حضور مفتوح لهذا الموظف اليوم أو أمس.");
        if (!string.IsNullOrEmpty(record.CheckOut)) return EmployeeQueryService.ToAttendanceDto(record);
        if (record.IsManualCorrection) throw new DomainException("سجل الحضور مصحح يدويًا؛ راجع المسؤول لتصحيح الانصراف.");
        var checkInAt = AttendanceInstants.CheckIn(record)
            ?? throw new DomainException("وقت الحضور غير مكتمل؛ يلزم تصحيح السجل يدويًا.");
        if (now < checkInAt) throw new DomainException("وقت الانصراف يسبق وقت الحضور.");
        var activeShiftId = await _db.Shifts.AsNoTracking().Where(s => s.Status == nameof(ShiftStatus.Open)).Select(s => s.Id).FirstOrDefaultAsync(ct);
        var workHours = Math.Round((now - checkInAt).TotalHours, 2);
        var overtimeHours = Math.Max(0, Math.Round(workHours - settings.StandardWorkdayHours, 2));
        date = record.Date;
        record.CheckInAtUtc ??= checkInAt;
        record.CheckOut = timeStr; record.CheckOutAtUtc = now;
        record.WorkHours = workHours; record.OvertimeHours = overtimeHours;
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
        await transaction.CommitAsync(ct);
        return EmployeeQueryService.ToAttendanceDto(record);
    }
}
