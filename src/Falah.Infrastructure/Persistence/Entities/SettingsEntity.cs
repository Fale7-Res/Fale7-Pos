namespace Falah.Infrastructure.Persistence.Entities;

/// <summary>Ported from SystemSettings in types.ts / updateSettings in lib/store.tsx - a single
/// system-wide settings row (Id is always the fixed constant SettingsEntity.SingletonId, matching
/// the reference app's own single `settings` object, never a per-user or per-device table). A real
/// gap found during the legacy-parity audit: no Settings feature existed at all in this port -
/// restaurant identity was hardcoded in both the WinUI header and the receipt-composition query,
/// the shift discrepancy threshold was a deploy-time config value, and auto-print/printer-width/
/// auto-lock/attendance-policy fields had nowhere to live. Attendance-policy fields
/// (StandardWorkdayHours..OvertimeMultiplier) and OvertimeHourlyRatePiastres are stored now (cheap,
/// no behavior yet) so the future Attendance/Payroll slice does not need a second migration - they
/// are inert until that slice reads them.</summary>
public class SettingsEntity
{
    public const string SingletonId = "settings";

    public string Id { get; set; } = SingletonId;
    public string RestaurantName { get; set; } = default!;
    public string SubName { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string Phone1 { get; set; } = default!;
    public string Phone2 { get; set; } = default!;
    public string PhoneLandline { get; set; } = default!;
    public string ComplaintsPhone { get; set; } = default!;
    public string ReceiptFooter { get; set; } = default!;
    public long OvertimeHourlyRatePiastres { get; set; }
    public long DeliveryFeePiastres { get; set; }
    public string Currency { get; set; } = default!;
    public long CashDiscrepancyThresholdPiastres { get; set; }
    public int AutoLockMinutes { get; set; }
    public string ActiveShiftName { get; set; } = default!;
    public string PrinterWidth { get; set; } = default!;
    public bool AutoPrintReceipt { get; set; }
    public bool AutoPrintPreparationTicket { get; set; }
    public int NumberOfReceiptCopies { get; set; }
    public bool GrillScreenSoundAlert { get; set; }
    public int StandardWorkdayHours { get; set; }
    public int LateGraceMinutes { get; set; }
    public int MaxLatePenaltyHours { get; set; }
    public double LatePenaltyMultiplier { get; set; }
    public double OvertimeMultiplier { get; set; }
}

/// <summary>Ported from CashHandover in enterprise-types.ts / createHandover in
/// lib/enterprise-store.tsx - a receipt-book record of "who physically handed money to whom,"
/// created only after a Treasury transfer (TreasuryScreen.tsx's own only call site), never for
/// every movement type. A real gap found during the legacy-parity audit; substantially the same
/// facts already land in the general AuditEvent log, but this is the dedicated, queryable ledger
/// legacy itself kept separate. Deduped on TreasuryMovementId exactly like legacy's own
/// `cashHandovers.some(item => item.treasuryMovementId === input.treasuryMovementId)` check.</summary>
public class CashHandoverEntity
{
    public string Id { get; set; } = default!;
    public string FromAccountId { get; set; } = default!;
    public string ToAccountId { get; set; } = default!;
    public long AmountPiastres { get; set; }
    public string HandedBy { get; set; } = default!;
    public string ReceivedBy { get; set; } = default!;
    public string? ShiftId { get; set; }
    public string? Notes { get; set; }
    public string? TreasuryMovementId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
