namespace Falah.Infrastructure.Persistence;

using Falah.Domain.Common;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Uses an action's existing audit event as an atomic retry receipt. The caller
/// must keep its transaction open through the mutation, audit append and commit.</summary>
internal static class CommandReceipt
{
    public static string Id(string action, string? requestId) => string.IsNullOrWhiteSpace(requestId)
        ? $"audit-{Guid.NewGuid():N}" : $"audit-{action}-{requestId}";

    public static async Task<AuditEventEntity?> FindAsync(FalahDbContext db, string id,
        string referenceId, string actorUserId, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("A receipt requires the command transaction.");
        var receipt = await db.AuditEvents
            .FromSqlRaw("SELECT * FROM AuditEvents WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", id)
            .AsNoTracking().FirstOrDefaultAsync(ct);
        if (receipt is not null && (receipt.ReferenceId != referenceId || receipt.UserId != actorUserId))
            throw new DomainException("مفتاح الطلب مستخدم لعملية أخرى.");
        return receipt;
    }
}
