namespace Falah.Infrastructure.Persistence.Entities;

public enum CashDrawerCommandStatus { Requested, Succeeded, Failed }

/// <summary>Ported from addCashMovement's 'drawer_open' case in the reference app's lib/store.tsx -
/// the ONE case of that command that posts NO treasury movement at all (only cash_in/cash_out do;
/// drawer_open's own `financial` stays undefined), because opening the drawer moves no money by
/// itself - it is a hardware action, audited ('فتح درج النقدية', severity 'warning' - exactly this
/// system's own choice too) but never a financial fact. Kept as its own dedicated command/entity
/// (not folded into Treasury) for the same reason printing is kept out of Order: "successful
/// [command] != successful [side effect]" - a drawer that fails to physically open must never be
/// able to touch what was already sold or collected, and nothing about a sale should ever depend on
/// this succeeding. Status defaults to Requested and is meant to be updated once real hardware
/// integration exists (ReportCashDrawerResultHandler) - physical verification is explicitly deferred
/// until then, matching the same "unknown pending physical verification" posture EscPosTicketRenderer
/// already carries for Arabic encoding.</summary>
public class CashDrawerOpenEntity
{
    public string Id { get; set; } = default!;
    public string ShiftId { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public string RequestedBy { get; set; } = default!;
    public string? ApprovedBy { get; set; }
    public string Status { get; set; } = default!;
    public string? LastError { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset? ReportedAtUtc { get; set; }
}
