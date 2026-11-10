namespace Falah.Infrastructure.BusinessDay;

using Falah.Domain.BusinessDay;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Resolve the active accounting day from the server. Financial writers call this inside
/// their transaction, after FinancialAdmission, so a concurrent close cannot invalidate the check.</summary>
public static class BusinessDayGate
{
    public static async Task<string> RequireOpenAsync(FalahDbContext db, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("An open business day must be checked inside the financial transaction.");
        return await TryGetActiveAsync(db, ct)
            ?? throw new DomainException("لا يوجد يوم عمل مفتوح - يجب فتح يوم العمل أولًا.");
    }

    /// <summary>
    /// The accounting day an AUTHORIZED shift opening runs under: the active day if there is one, otherwise a day
    /// for the server's business date, created inside the caller's transaction (owner-approved handover,
    /// 2026-11-09). A cashier therefore starts their own shift without a manager login, and gains no
    /// day-management permission: standalone open/close/reopen keep their own permissions.
    ///
    /// - Creation is serialized by an exclusive app lock taken AFTER the caller's FinancialAdmission. The order is
    ///   always admission, then creation lock, so it cannot deadlock. A concurrent opener waits, then re-reads and
    ///   reuses the committed day.
    /// - A date whose day is already Closed is refused. A closed period is never reopened or duplicated here.
    /// - The day's request id is derived from the shift's request id. If the opening rolls back, nothing remains.
    ///   If it commits, a retry of the same request replays the shift and never reaches this code.
    /// </summary>
    public static async Task<string> ResolveForShiftOpenAsync(
        FalahDbContext db, string openedByUserId, string dayRequestId, DateTimeOffset nowUtc, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A shift's business day must be resolved inside the financial transaction.");
        if (await TryGetActiveAsync(db, ct) is { } active) return active;

        var result = new Microsoft.Data.SqlClient.SqlParameter("@creationLock", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
        await db.Database.ExecuteSqlRawAsync(
            "EXEC @creationLock = sys.sp_getapplock @Resource=N'FalahBusinessDayCreation', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=15000",
            new object[] { result }, ct);
        if (result.Value is not int code || code < 0)
            throw new DomainException("تعذر بدء يوم العمل بسبب عملية متزامنة. أعد المحاولة.");
        if (await TryGetActiveAsync(db, ct) is { } openedMeanwhile) return openedMeanwhile;

        var businessDate = BusinessClock.BusinessDate(nowUtc);
        if (await db.BusinessDays.AsNoTracking().AnyAsync(d => d.BusinessDate == businessDate && d.Status == nameof(BusinessDayStatus.Closed), ct))
            throw new DomainException($"يوم العمل {businessDate:yyyy-MM-dd} مُغلق بالفعل. لا يمكن فتح وردية عليه إلا بعد إعادة فتحه أو بدء يوم جديد بواسطة المدير.");

        var day = BusinessDayRules.Open($"day-{Guid.NewGuid():N}", businessDate, openedByUserId, nowUtc, Array.Empty<BusinessDayRecord>());
        db.BusinessDays.Add(new Persistence.Entities.BusinessDayEntity
        {
            Id = day.Id, BusinessDate = day.BusinessDate, OpenedBy = day.OpenedBy, OpenedAtUtc = day.OpenedAtUtc,
            Status = nameof(BusinessDayStatus.Open), CloseRevision = 0, OpenRequestId = dayRequestId,
        });
        return day.Id;
    }

    /// <summary>Optional lookup for reads and legacy shift close. A pre-business-day shift must
    /// remain closable even when no active day exists; new money commands use RequireOpenAsync.</summary>
    public static async Task<string?> TryGetActiveAsync(FalahDbContext db, CancellationToken ct)
    {
        var day = await db.BusinessDays.AsNoTracking()
            .Where(d => d.Status == nameof(BusinessDayStatus.Open) || d.Status == nameof(BusinessDayStatus.Closing) || d.Status == nameof(BusinessDayStatus.Reopened))
            .FirstOrDefaultAsync(ct);
        return day?.Id;
    }
}
