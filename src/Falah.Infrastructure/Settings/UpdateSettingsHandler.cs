namespace Falah.Infrastructure.Settings;

using Falah.Application.Settings;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from updateSettings in lib/store.tsx: a partial merge onto the singleton row
/// (created on first write, from SettingsQueryService.Defaults, if none exists yet), gated on
/// `settings_manage` unless the request touches nothing but GrillScreenSoundAlert. Audited exactly
/// like legacy's own `logAuditEvent` call (`category=settings`, `severity=warning`).</summary>
public sealed class UpdateSettingsHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public UpdateSettingsHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<SettingsDto> HandleAsync(UpdateSettingsRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");

        var onlyGrillSound = request.RestaurantName is null && request.SubName is null && request.Address is null
            && request.Phone1 is null && request.Phone2 is null && request.PhoneLandline is null
            && request.ComplaintsPhone is null && request.ReceiptFooter is null && request.OvertimeHourlyRatePiastres is null
            && request.DeliveryFeePiastres is null && request.Currency is null && request.CashDiscrepancyThresholdPiastres is null
            && request.AutoLockMinutes is null && request.ActiveShiftName is null && request.PrinterWidth is null
            && request.AutoPrintReceipt is null && request.AutoPrintPreparationTicket is null && request.NumberOfReceiptCopies is null
            && request.StandardWorkdayHours is null && request.LateGraceMinutes is null && request.MaxLatePenaltyHours is null
            && request.LatePenaltyMultiplier is null && request.OvertimeMultiplier is null
            && request.HoldPreparationUntilPaymentConfirmed is null;
        if (!onlyGrillSound)
            AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "settings_manage");

        var entity = await _db.Settings.FirstOrDefaultAsync(s => s.Id == SettingsEntity.SingletonId, ct);
        if (entity is null)
        {
            var d = SettingsQueryService.Defaults;
            entity = new SettingsEntity
            {
                RestaurantName = d.RestaurantName, SubName = d.SubName, Address = d.Address, Phone1 = d.Phone1,
                Phone2 = d.Phone2, PhoneLandline = d.PhoneLandline, ComplaintsPhone = d.ComplaintsPhone,
                ReceiptFooter = d.ReceiptFooter, OvertimeHourlyRatePiastres = d.OvertimeHourlyRatePiastres,
                DeliveryFeePiastres = d.DeliveryFeePiastres, Currency = d.Currency,
                CashDiscrepancyThresholdPiastres = d.CashDiscrepancyThresholdPiastres, AutoLockMinutes = d.AutoLockMinutes,
                ActiveShiftName = d.ActiveShiftName, PrinterWidth = d.PrinterWidth, AutoPrintReceipt = d.AutoPrintReceipt,
                AutoPrintPreparationTicket = d.AutoPrintPreparationTicket, NumberOfReceiptCopies = d.NumberOfReceiptCopies,
                GrillScreenSoundAlert = d.GrillScreenSoundAlert, StandardWorkdayHours = d.StandardWorkdayHours,
                LateGraceMinutes = d.LateGraceMinutes, MaxLatePenaltyHours = d.MaxLatePenaltyHours,
                LatePenaltyMultiplier = d.LatePenaltyMultiplier, OvertimeMultiplier = d.OvertimeMultiplier,
                HoldPreparationUntilPaymentConfirmed = d.HoldPreparationUntilPaymentConfirmed,
            };
            _db.Settings.Add(entity);
        }

        if (request.RestaurantName is not null) entity.RestaurantName = request.RestaurantName;
        if (request.SubName is not null) entity.SubName = request.SubName;
        if (request.Address is not null) entity.Address = request.Address;
        if (request.Phone1 is not null) entity.Phone1 = request.Phone1;
        if (request.Phone2 is not null) entity.Phone2 = request.Phone2;
        if (request.PhoneLandline is not null) entity.PhoneLandline = request.PhoneLandline;
        if (request.ComplaintsPhone is not null) entity.ComplaintsPhone = request.ComplaintsPhone;
        if (request.ReceiptFooter is not null) entity.ReceiptFooter = request.ReceiptFooter;
        if (request.OvertimeHourlyRatePiastres is not null) entity.OvertimeHourlyRatePiastres = request.OvertimeHourlyRatePiastres.Value;
        if (request.DeliveryFeePiastres is not null) entity.DeliveryFeePiastres = request.DeliveryFeePiastres.Value;
        if (request.Currency is not null) entity.Currency = request.Currency;
        if (request.CashDiscrepancyThresholdPiastres is not null) entity.CashDiscrepancyThresholdPiastres = request.CashDiscrepancyThresholdPiastres.Value;
        if (request.AutoLockMinutes is not null) entity.AutoLockMinutes = request.AutoLockMinutes.Value;
        if (request.ActiveShiftName is not null) entity.ActiveShiftName = request.ActiveShiftName;
        if (request.PrinterWidth is not null) entity.PrinterWidth = request.PrinterWidth;
        if (request.AutoPrintReceipt is not null) entity.AutoPrintReceipt = request.AutoPrintReceipt.Value;
        if (request.AutoPrintPreparationTicket is not null) entity.AutoPrintPreparationTicket = request.AutoPrintPreparationTicket.Value;
        if (request.NumberOfReceiptCopies is not null) entity.NumberOfReceiptCopies = request.NumberOfReceiptCopies.Value;
        if (request.GrillScreenSoundAlert is not null) entity.GrillScreenSoundAlert = request.GrillScreenSoundAlert.Value;
        if (request.StandardWorkdayHours is not null) entity.StandardWorkdayHours = request.StandardWorkdayHours.Value;
        if (request.LateGraceMinutes is not null) entity.LateGraceMinutes = request.LateGraceMinutes.Value;
        if (request.MaxLatePenaltyHours is not null) entity.MaxLatePenaltyHours = request.MaxLatePenaltyHours.Value;
        if (request.LatePenaltyMultiplier is not null) entity.LatePenaltyMultiplier = request.LatePenaltyMultiplier.Value;
        if (request.OvertimeMultiplier is not null) entity.OvertimeMultiplier = request.OvertimeMultiplier.Value;
        if (request.HoldPreparationUntilPaymentConfirmed is not null)
            entity.HoldPreparationUntilPaymentConfirmed = request.HoldPreparationUntilPaymentConfirmed.Value;

        _auditLog.Append(new AuditEvent(
            $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
            "تحديث إعدادات النظام وقواعد التشغيل", "settings",
            $"تم تحديث الإعدادات (سعر الساعة الإضافية: {entity.OvertimeHourlyRatePiastres} قرش)",
            "warning", DateTimeOffset.UtcNow));

        await _db.SaveChangesAsync(ct);
        return SettingsQueryService.ToDto(entity);
    }
}
