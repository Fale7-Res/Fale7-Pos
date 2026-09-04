namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Employees;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from recordDailyWage in lib/store.tsx (the manual/administrative invocation -
/// see BiometricCheckInHandler for the automatic one). Gated on `employees_ledger`, matching the
/// same permission its sibling advance/withdrawal/manual-entry commands already use - a real gap
/// closed here, since the reference app's own recordDailyWage has no permission check at all.
///
/// A real bug found during re-verification: a duplicate same-day/same-shift call here must THROW
/// (assertUniqueDailyWage's own message, "...استخدم تطبيق لوردية إضافية") rather than silently
/// no-op - legacy only swallows that throw at the ONE call site inside biometricCheckIn (a
/// try/catch specifically there, store.tsx:2072), because a retried/duplicate biometric event is
/// expected and harmless, but a manager manually re-triggering this command is not the same case
/// and deserves the real error telling them to use "extra shift" instead.</summary>
public sealed class RecordDailyWageHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public RecordDailyWageHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<EmployeeLedgerEntryDto> HandleAsync(RecordDailyWageRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "employees_ledger", "تسجيل يومية");
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "employees_ledger", approval, requireApproval: true);

        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new InvalidOperationException("الموظف غير موجود.");

        var wage = Money.FromPiastres(request.CustomWagePiastres ?? employee.DailyWagePiastres);
        var date = Falah.Domain.Common.BusinessClock.Today();
        var id = $"led-daily-wage-{request.EmployeeId}-{date:yyyy-MM-dd}-{request.ShiftId ?? "no-shift"}";

        var entity = await EmployeeLedgerPoster.PostIfNewAsync(_db, id, request.EmployeeId, nameof(LedgerEntryType.DailyWage),
            $"تسجيل يومية عمل ({wage.Piastres} قرش)", Money.Zero, wage, actorUser.Name, authorization.Approver?.ApproverName,
            request.ShiftId, null, null, null, wage.Piastres, ct);
        if (entity is null)
            throw new DomainException("تم تسجيل اليومية العادية لهذا الموظف في نفس اليوم والوردية بالفعل. استخدم تطبيق لوردية إضافية.");
        await _db.SaveChangesAsync(ct);
        return EmployeeQueryService.ToLedgerDto(entity);
    }
}

/// <summary>Ported from recordOvertime - gated on `employees_ledger` (see RecordDailyWageHandler's
/// own doc comment for why, matching legacy's un-gated original).</summary>
public sealed class RecordOvertimeHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public RecordOvertimeHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<EmployeeLedgerEntryDto> HandleAsync(RecordOvertimeRequest request, CancellationToken ct = default)
    {
        if (request.Hours <= 0) throw new DomainException("عدد الساعات الإضافية يجب أن يكون أكبر من صفر.");
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "employees_ledger", "ساعات إضافية");
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "employees_ledger", approval, requireApproval: true);

        var settings = await new Falah.Infrastructure.Settings.SettingsQueryService(_db).GetAsync(ct);
        var rate = Money.FromPiastres(request.CustomRatePiastres ?? settings.OvertimeHourlyRatePiastres);
        var totalAmount = Money.FromPiastres((long)Math.Round(rate.Piastres * request.Hours));

        var id = $"led-overtime-{request.RequestId}";
        var entity = await EmployeeLedgerPoster.PostIfNewAsync(_db, id, request.EmployeeId, nameof(LedgerEntryType.Overtime),
            $"ساعات إضافية ({request.Hours} ساعة × {rate.Piastres} قرش/ساعة = {totalAmount.Piastres} قرش)" + (request.Notes is null ? "" : $" - {request.Notes}"),
            Money.Zero, totalAmount, actorUser.Name, authorization.Approver?.ApproverName, null, null, request.Hours, rate.Piastres, null, ct);
        if (entity is null)
        {
            // A genuine retry of the same RequestId is a safe replay, not an error - unlike daily
            // wage, legacy names no "you already recorded overtime" business rule for this command.
            var replay = await _db.EmployeeLedgerEntries.AsNoTracking().FirstAsync(e => e.Id == id, ct);
            return EmployeeQueryService.ToLedgerDto(replay);
        }
        await _db.SaveChangesAsync(ct);
        return EmployeeQueryService.ToLedgerDto(entity);
    }
}

/// <summary>Ported from recordExtraShift - gated on `employees_ledger` (see
/// RecordDailyWageHandler's own doc comment).</summary>
public sealed class RecordExtraShiftHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public RecordExtraShiftHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<EmployeeLedgerEntryDto> HandleAsync(RecordExtraShiftRequest request, CancellationToken ct = default)
    {
        if (request.ShiftCount <= 0) throw new DomainException("عدد الشيفتات يجب أن يكون أكبر من صفر.");
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "employees_ledger", "تطبيق وردية إضافية");
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "employees_ledger", approval, requireApproval: true);

        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new InvalidOperationException("الموظف غير موجود.");
        var shiftValue = Money.FromPiastres(employee.ShiftValuePiastres > 0 ? employee.ShiftValuePiastres : employee.DailyWagePiastres);
        var totalAmount = shiftValue * request.ShiftCount;

        var id = $"led-extra-shift-{request.RequestId}";
        var entity = await EmployeeLedgerPoster.PostIfNewAsync(_db, id, request.EmployeeId, nameof(LedgerEntryType.ExtraShift),
            $"تطبيق وردية إضافية ({request.ShiftCount} شيفت × {shiftValue.Piastres} قرش = {totalAmount.Piastres} قرش)" + (request.Notes is null ? "" : $" - {request.Notes}"),
            Money.Zero, totalAmount, actorUser.Name, authorization.Approver?.ApproverName, null, null, null, null, shiftValue.Piastres, ct);
        if (entity is null)
        {
            // Same idempotent-replay reasoning as RecordOvertimeHandler above.
            var replay = await _db.EmployeeLedgerEntries.AsNoTracking().FirstAsync(e => e.Id == id, ct);
            return EmployeeQueryService.ToLedgerDto(replay);
        }
        await _db.SaveChangesAsync(ct);
        return EmployeeQueryService.ToLedgerDto(entity);
    }
}
