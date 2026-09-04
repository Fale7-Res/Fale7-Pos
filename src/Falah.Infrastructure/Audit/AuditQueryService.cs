namespace Falah.Infrastructure.Audit;

using Falah.Application.Audit;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Read-only projections over the general audit trail. Ported from the reference app's own
/// Control Center review screen filters (by category/severity/actor/date range) - gated on
/// `audit_view`/`control_center` at the endpoint, matching the reference app's own `hasPermission`
/// checks on that screen.</summary>
public sealed class AuditQueryService
{
    private readonly FalahDbContext _db;
    public AuditQueryService(FalahDbContext db) => _db = db;

    public async Task<List<AuditEventDto>> ListAsync(
        string? category, string? severity, string? userId, DateTimeOffset? from, DateTimeOffset? to,
        int limit = 200, CancellationToken ct = default)
    {
        var query = _db.AuditEvents.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(category)) query = query.Where(e => e.Category == category);
        if (!string.IsNullOrEmpty(severity)) query = query.Where(e => e.Severity == severity);
        if (!string.IsNullOrEmpty(userId)) query = query.Where(e => e.UserId == userId);
        if (from is not null) query = query.Where(e => e.TimestampUtc >= from);
        if (to is not null) query = query.Where(e => e.TimestampUtc <= to);

        var events = await query.OrderByDescending(e => e.TimestampUtc).Take(limit).ToListAsync(ct);
        return events.Select(ToDto).ToList();
    }

    private static AuditEventDto ToDto(AuditEventEntity e) => new(
        e.Id, e.UserId, e.UserName, e.UserRole, e.Action, e.Category, e.Details, e.Severity, e.TimestampUtc,
        e.ShiftId, e.Device, e.Reason, e.AuthorizedBy, e.PreviousValue, e.NewValue, e.OperationId, e.ReferenceId);
}
