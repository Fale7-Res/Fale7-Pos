namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Employees;
using Falah.Domain.Payroll;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from correctAttendance in lib/store.tsx - gated on `attendance_manage`. Posts
/// only the DELTA between what the automatic late-penalty/overtime calculation already produced
/// and what the correction implies, as a new 'adjustment' ledger entry - the original automatic
/// entries (if any) are never rewritten or deleted, only reconciled forward.</summary>
public sealed class CorrectAttendanceHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public CorrectAttendanceHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<AttendanceRecordDto> HandleAsync(CorrectAttendanceRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب تصحيح الحضور مطلوب.");
        if (!Enum.TryParse<AttendanceStatus>(request.Status, ignoreCase: true, out _))
            throw new DomainException("حالة الحضور غير صالحة.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "attendance_manage", request.Reason);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "attendance_manage", approval, requireApproval: true);
        var verifiedApproverName = authorization.Approver?.ApproverName ?? actorUser.Name;

        var record = await _db.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == request.RecordId, ct)
            ?? throw new InvalidOperationException("سجل الحضور غير موجود.");
        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == record.EmployeeId, ct);

        var oldLateMinutes = record.LateMinutes;
        var oldOvertimeHours = record.OvertimeHours;

        record.Status = request.Status;
        record.LateMinutes = request.LateMinutes;
        record.WorkHours = request.WorkHours;
        record.OvertimeHours = request.OvertimeHours;
        record.TatbiqCount = request.TatbiqCount;
        record.IsManualCorrection = true;
        record.CorrectedBy = verifiedApproverName;
        record.CorrectionReason = request.Reason;

        if (employee is not null && employee.DailyWagePiastres > 0)
        {
            var settings = await new SettingsQueryService(_db).GetAsync(ct);
            var dailyWage = Money.FromPiastres(employee.DailyWagePiastres);

            var oldPenalty = PayrollRules.CalculateLateDeduction(dailyWage, settings.StandardWorkdayHours, oldLateMinutes,
                settings.LateGraceMinutes, settings.MaxLatePenaltyHours, settings.LatePenaltyMultiplier).Amount;
            var newPenalty = PayrollRules.CalculateLateDeduction(dailyWage, settings.StandardWorkdayHours, request.LateMinutes,
                settings.LateGraceMinutes, settings.MaxLatePenaltyHours, settings.LatePenaltyMultiplier).Amount;
            var penaltyDelta = newPenalty.Piastres - oldPenalty.Piastres;
            if (penaltyDelta != 0)
            {
                var isCredit = penaltyDelta < 0; // a REDUCED penalty credits the employee back
                await EmployeeLedgerPoster.PostIfNewAsync(_db, $"led-correction-penalty-{request.RequestId}", record.EmployeeId, nameof(LedgerEntryType.Adjustment),
                    $"تصحيح خصم تأخير حضور — {request.Reason}",
                    isCredit ? Money.Zero : Money.FromPiastres(Math.Abs(penaltyDelta)),
                    isCredit ? Money.FromPiastres(Math.Abs(penaltyDelta)) : Money.Zero,
                    verifiedApproverName, verifiedApproverName, null, record.Id, null, null, null, ct);
            }

            var oldOvertime = PayrollRules.CalculateOvertimeAmount(dailyWage, settings.StandardWorkdayHours, oldOvertimeHours, settings.OvertimeMultiplier).Amount;
            var newOvertime = PayrollRules.CalculateOvertimeAmount(dailyWage, settings.StandardWorkdayHours, request.OvertimeHours, settings.OvertimeMultiplier).Amount;
            var overtimeDelta = newOvertime.Piastres - oldOvertime.Piastres;
            if (overtimeDelta != 0)
            {
                var isCredit = overtimeDelta > 0; // an INCREASED overtime credits the employee more
                await EmployeeLedgerPoster.PostIfNewAsync(_db, $"led-correction-overtime-{request.RequestId}", record.EmployeeId, nameof(LedgerEntryType.Adjustment),
                    $"تصحيح ساعات إضافية حضور — {request.Reason}",
                    isCredit ? Money.Zero : Money.FromPiastres(Math.Abs(overtimeDelta)),
                    isCredit ? Money.FromPiastres(Math.Abs(overtimeDelta)) : Money.Zero,
                    verifiedApproverName, verifiedApproverName, null, record.Id, null, null, null, ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        return EmployeeQueryService.ToAttendanceDto(record);
    }
}
