namespace Falah.Application.Settings;

/// <summary>Field-for-field port of SystemSettings in types.ts. A single system-wide row - see
/// SettingsEntity's own doc comment. Money-shaped fields (OvertimeHourlyRate/DeliveryFee/
/// CashDiscrepancyThreshold) are piastres here, matching every other amount in this API, unlike
/// the reference app's own plain-EGP-integer fields.</summary>
public sealed record SettingsDto(
    string RestaurantName, string SubName, string Address, string Phone1, string Phone2, string PhoneLandline,
    string ComplaintsPhone, string ReceiptFooter, long OvertimeHourlyRatePiastres, long DeliveryFeePiastres,
    string Currency, long CashDiscrepancyThresholdPiastres, int AutoLockMinutes, string ActiveShiftName,
    string PrinterWidth, bool AutoPrintReceipt, bool AutoPrintPreparationTicket, int NumberOfReceiptCopies,
    bool GrillScreenSoundAlert, int StandardWorkdayHours, int LateGraceMinutes, int MaxLatePenaltyHours,
    double LatePenaltyMultiplier, double OvertimeMultiplier,
    /// <summary>False (the default) sends an order to the kitchen the moment it is taken, even when
    /// it was paid by an electronic transfer nobody has confirmed yet. True holds the preparation
    /// ticket until the transfer is confirmed. The customer's receipt prints either way.</summary>
    bool HoldPreparationUntilPaymentConfirmed = false,
    /// <summary>What the open-shift dialog offers as the float, in piastres. Only a starting value:
    /// the cashier can change it, and what they enter is what the shift stores and reconciles
    /// against. Changing this never touches a shift that is already open or closed.</summary>
    long DefaultOpeningCashPiastres = 100_000,
    /// <summary>Where the menu QR on the customer receipt points. Null means no menu link is
    /// configured, and the receipt then prints no QR at all - it never falls back to a guessed or
    /// placeholder address, because a QR that opens the wrong page is worse than none.</summary>
    string? MenuUrl = null);

/// <summary>Ported from updateSettings in lib/store.tsx: a partial update (every field optional,
/// null = "leave unchanged") gated on `settings_manage` - EXCEPT when the only field being changed
/// is GrillScreenSoundAlert, which needs no permission at all (a per-device sound toggle any user
/// may flip, matching legacy's own `if (!Object.keys(newSettings).every(key =>
/// key === 'grillScreenSoundAlert')) authorizeCommand(...)`).</summary>
public sealed record UpdateSettingsRequest(
    string ActorUserId,
    string? RestaurantName = null, string? SubName = null, string? Address = null, string? Phone1 = null,
    string? Phone2 = null, string? PhoneLandline = null, string? ComplaintsPhone = null, string? ReceiptFooter = null,
    long? OvertimeHourlyRatePiastres = null, long? DeliveryFeePiastres = null, string? Currency = null,
    long? CashDiscrepancyThresholdPiastres = null, int? AutoLockMinutes = null, string? ActiveShiftName = null,
    string? PrinterWidth = null, bool? AutoPrintReceipt = null, bool? AutoPrintPreparationTicket = null,
    int? NumberOfReceiptCopies = null, bool? GrillScreenSoundAlert = null, int? StandardWorkdayHours = null,
    int? LateGraceMinutes = null, int? MaxLatePenaltyHours = null, double? LatePenaltyMultiplier = null,
    double? OvertimeMultiplier = null, bool? HoldPreparationUntilPaymentConfirmed = null,
    /// <summary>Null leaves the menu link unchanged; an empty string clears it.</summary>
    string? MenuUrl = null,
    /// <summary>The float the open-shift dialog starts with, in piastres. Null leaves it unchanged.</summary>
    long? DefaultOpeningCashPiastres = null);
