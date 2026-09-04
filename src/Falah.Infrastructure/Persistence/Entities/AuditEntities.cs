namespace Falah.Infrastructure.Persistence.Entities;

/// <summary>Ported from Falah.Domain.Audit.AuditEvent - the general-purpose, append-only trail for
/// sensitive actions across the whole system (orders, cash, shifts, menu, inventory, delivery,
/// printing), distinct from the narrower ControlAuditEventEntity (business-day close/reopen and
/// user-management actions specifically - see that entity's own doc comment for why the two are kept
/// separate rather than unified).</summary>
public class AuditEventEntity
{
    public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string UserRole { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Details { get; set; } = default!;
    public string Severity { get; set; } = default!;
    public DateTimeOffset TimestampUtc { get; set; }
    public string? ShiftId { get; set; }
    public string? Device { get; set; }
    public string? Reason { get; set; }
    public string? AuthorizedBy { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string? OperationId { get; set; }
    public string? ReferenceId { get; set; }
}
