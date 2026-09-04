namespace Falah.Domain.Audit;

using Falah.Domain.Common;

/// <summary>An immutable record of one sensitive action - who did it, when, under whose
/// authorization. Never carries a PIN or password, only ever the actor's identity and role. There is
/// deliberately no update or delete operation anywhere near this type: the only legitimate way an
/// audit trail grows is by appending. Field set ported from the reference app's own AuditEvent
/// (lib/types.ts) - Category is deliberately NOT restricted to the reference app's fixed union
/// ('order'|'cash'|'shift'|'employee'|'menu'|'auth'|'settings'|'attendance'|'inventory'), since this
/// system has real feature areas the reference app's category enum predates (delivery, business_day,
/// purchasing, printing) - a plain string lets a new category be introduced without a Domain change,
/// same latitude ProductEntity.Category already gets. Severity IS validated (AuditRules.AssertValidSeverity)
/// since the reference app's own four-value AuditSeverity union is still exactly what this system's
/// evidence supports.</summary>
public sealed record AuditEvent(
    string Id,
    string UserId,
    string UserName,
    string UserRole,
    string Action,
    string Category,
    string Details,
    string Severity,
    DateTimeOffset TimestampUtc,
    string? ShiftId = null,
    string? Device = null,
    string? Reason = null,
    string? AuthorizedBy = null,
    string? PreviousValue = null,
    string? NewValue = null,
    string? OperationId = null,
    string? ReferenceId = null);

/// <summary>Append-only by construction: the interface exposes no way to modify or remove an event
/// once written, and deliberately no way to read them back either - reading is a separate query
/// concern (Falah.Infrastructure.Audit.AuditQueryService), kept off this write-side abstraction so a
/// handler that only needs to log something never accidentally depends on the whole audit trail.</summary>
public interface IAuditLog
{
    void Append(AuditEvent auditEvent);
}

/// <summary>Reference implementation for tests - the real system wires an EF Core-backed log
/// (Falah.Infrastructure.Audit.EfAuditLog) that appends to the SAME DbContext/transaction as the
/// business action it is logging, so a rolled-back operation never leaves an orphaned audit row
/// behind.</summary>
public sealed class InMemoryAuditLog : IAuditLog
{
    private readonly List<AuditEvent> _events = new();
    public void Append(AuditEvent auditEvent) => _events.Add(auditEvent);
    public IReadOnlyList<AuditEvent> Events => _events.AsReadOnly();
}

public static class AuditRules
{
    private static readonly HashSet<string> ValidSeverities = new() { "info", "normal", "warning", "critical" };

    public static void AssertValidSeverity(string severity)
    {
        if (!ValidSeverities.Contains(severity))
            throw new DomainException($"درجة خطورة غير معروفة: {severity}.");
    }
}
