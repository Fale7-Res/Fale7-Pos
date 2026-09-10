namespace Falah.Infrastructure.Settings;

using Falah.Application.Settings;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Read side of the Settings singleton. Legacy's own INITIAL_SETTINGS (mock-data.ts)
/// defaults are used when no row has ever been saved yet, so a fresh deployment behaves exactly
/// like a fresh legacy install rather than exposing zeros/blanks.</summary>
public sealed class SettingsQueryService
{
    private readonly FalahDbContext _db;
    public SettingsQueryService(FalahDbContext db) => _db = db;

    public static readonly SettingsDto Defaults = new(
        RestaurantName: "فالح أبو العنبه", SubName: "مطاعم فالح أبو العنبه - 24 ساعة",
        Address: "6 أكتوبر - الحي السابع - سنتر الأردنية", Phone1: "01144741115", Phone2: "01000602832",
        PhoneLandline: "0238361323", ComplaintsPhone: "01112595678",
        ReceiptFooter: "شكراً لزيارتكم مطاعم فالح أبو العنبه. نسعد بخدمتكم على مدار 24 ساعة",
        OvertimeHourlyRatePiastres: 2500, DeliveryFeePiastres: 2000, Currency: "ج.م",
        CashDiscrepancyThresholdPiastres: 5000, AutoLockMinutes: 10, ActiveShiftName: "وردية صباحية",
        PrinterWidth: "80mm", AutoPrintReceipt: true, AutoPrintPreparationTicket: true, NumberOfReceiptCopies: 1,
        GrillScreenSoundAlert: true, StandardWorkdayHours: 10, LateGraceMinutes: 15, MaxLatePenaltyHours: 2,
        LatePenaltyMultiplier: 2.0, OvertimeMultiplier: 1.5,
        // What this system has always done. Changing the default would change how every restaurant
        // running it behaves, without anyone asking for it.
        HoldPreparationUntilPaymentConfirmed: false);

    public async Task<SettingsDto> GetAsync(CancellationToken ct = default)
    {
        var e = await _db.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == SettingsEntity.SingletonId, ct);
        return e is null ? Defaults : ToDto(e);
    }

    internal static SettingsDto ToDto(SettingsEntity e) => new(
        e.RestaurantName, e.SubName, e.Address, e.Phone1, e.Phone2, e.PhoneLandline, e.ComplaintsPhone, e.ReceiptFooter,
        e.OvertimeHourlyRatePiastres, e.DeliveryFeePiastres, e.Currency, e.CashDiscrepancyThresholdPiastres,
        e.AutoLockMinutes, e.ActiveShiftName, e.PrinterWidth, e.AutoPrintReceipt, e.AutoPrintPreparationTicket,
        e.NumberOfReceiptCopies, e.GrillScreenSoundAlert, e.StandardWorkdayHours, e.LateGraceMinutes,
        e.MaxLatePenaltyHours, e.LatePenaltyMultiplier, e.OvertimeMultiplier, e.HoldPreparationUntilPaymentConfirmed);
}
