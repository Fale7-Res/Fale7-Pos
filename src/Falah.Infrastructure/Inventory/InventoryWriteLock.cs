namespace Falah.Infrastructure.Inventory;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

/// <summary>Short stock postings, physical counts, unit changes and recipe edits share a database
/// barrier. Financial callers acquire treasury locks first, then this barrier; stock-only callers
/// never acquire treasury locks while holding it. This avoids account/stock lock inversion.</summary>
internal static class InventoryWriteLock
{
    internal static async Task AcquireAsync(FalahDbContext db, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("An inventory transaction is required.");
        var result = new SqlParameter("@stockLockResult", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await db.Database.ExecuteSqlRawAsync("EXEC @stockLockResult=sys.sp_getapplock @Resource=N'FalahStockPosting', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=15000", new object[] { result }, ct);
        if (result.Value is not int code || code < 0) throw new DomainException("توجد حركة مخزون أخرى قيد التنفيذ؛ أعد المحاولة بنفس الطلب.");
    }
}
