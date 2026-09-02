namespace Falah.Domain.Audit;

/// <summary>An immutable record of one sensitive action - who did it, when, under whose
/// authorization. Never carries a PIN or password, only ever the actor's identity and role.
/// There is deliberately no update or delete operation anywhere near this type: the only
/// legitimate way an audit trail grows is by appending.</summary>
public sealed record AuditEvent(
    string Id,
    string UserId,
    string UserName,
    string UserRole,
    string Action,
    string Category,
    string Severity,
    DateTimeOffset TimestampUtc,
    string? AuthorizedBy = null,
    string? PreviousValue = null,
    string? NewValue = null);

/// <summary>Append-only by construction: the interface exposes no way to modify or remove an event
/// once written. Production wires an EF Core-backed log; InMemoryAuditLog below is a reference
/// implementation for tests.</summary>
public interface IAuditLog
{
    void Append(AuditEvent auditEvent);
    IReadOnlyList<AuditEvent> All { get; }
}

public sealed class InMemoryAuditLog : IAuditLog
{
    private readonly List<AuditEvent> _events = new();
    public void Append(AuditEvent auditEvent) => _events.Add(auditEvent);
    public IReadOnlyList<AuditEvent> All => _events.AsReadOnly();
}
