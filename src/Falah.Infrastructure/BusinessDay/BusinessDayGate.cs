namespace Falah.Infrastructure.BusinessDay;

using Falah.Domain.BusinessDay;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from requireOpenBusinessDay in the reference app's lib/enterprise-store.tsx:
/// every Treasury cash-lifecycle command (deposit, withdrawal, transfer, safe drop) requires an
/// active business day to exist first. Returns the day's id so callers can stamp it onto the
/// movement they post, exactly like the reference app threads day.id through.</summary>
public static class BusinessDayGate
{
    public static async Task<string> RequireOpenAsync(FalahDbContext db, CancellationToken ct) =>
        await TryGetActiveAsync(db, ct) ?? throw new DomainException("لا يوجد يوم عمل مفتوح - يجب فتح يوم العمل أولًا.");

    /// <summary>Ported from checkout()'s own `businessDays.find(day => day.status !== 'closed')?.id`
    /// in lib/store.tsx - a plain sale, unlike a Treasury cash-lifecycle command, does NOT require
    /// an active business day to exist; it just opportunistically stamps the current one's id when
    /// there is one, and proceeds with null otherwise. Never returns from a client-supplied id -
    /// callers (CheckoutOrderHandler) must resolve this server-side, the same "server-authoritative"
    /// treatment every other snapshot field on Order already gets.</summary>
    public static async Task<string?> TryGetActiveAsync(FalahDbContext db, CancellationToken ct)
    {
        var day = await db.BusinessDays.AsNoTracking()
            .Where(d => d.Status == nameof(BusinessDayStatus.Open) || d.Status == nameof(BusinessDayStatus.Closing) || d.Status == nameof(BusinessDayStatus.Reopened))
            .FirstOrDefaultAsync(ct);
        return day?.Id;
    }
}
