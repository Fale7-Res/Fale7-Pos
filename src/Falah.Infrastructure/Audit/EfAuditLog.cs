namespace Falah.Infrastructure.Audit;

using Falah.Domain.Audit;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;

/// <summary>Adds to the SAME DbContext/change tracker the calling handler already has open, never
/// calling SaveChangesAsync itself - the audit row is persisted together with the business action it
/// describes, in the same transaction, so a rolled-back operation can never leave an orphaned audit
/// entry behind (and a committed one can never lose its own audit trail to a separate, un-transacted
/// write).</summary>
public sealed class EfAuditLog : IAuditLog
{
    private readonly FalahDbContext _db;
    public EfAuditLog(FalahDbContext db) => _db = db;

    public void Append(AuditEvent auditEvent)
    {
        AuditRules.AssertValidSeverity(auditEvent.Severity);
        _db.AuditEvents.Add(new AuditEventEntity
        {
            Id = auditEvent.Id, UserId = auditEvent.UserId, UserName = auditEvent.UserName, UserRole = auditEvent.UserRole,
            Action = auditEvent.Action, Category = auditEvent.Category, Details = auditEvent.Details, Severity = auditEvent.Severity,
            TimestampUtc = auditEvent.TimestampUtc, ShiftId = auditEvent.ShiftId, Device = auditEvent.Device, Reason = auditEvent.Reason,
            AuthorizedBy = auditEvent.AuthorizedBy, PreviousValue = auditEvent.PreviousValue, NewValue = auditEvent.NewValue,
            OperationId = auditEvent.OperationId, ReferenceId = auditEvent.ReferenceId,
        });
    }
}
