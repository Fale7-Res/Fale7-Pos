namespace Falah.Infrastructure.Persistence;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// Allocates the next order sequence number as a single atomic UPDATE ... RETURNING statement -
/// there is no separate read-then-write step for a concurrent request from the other device to
/// race into. SQLite serializes writers at the file level, so this statement either fully
/// completes before any other writer's UPDATE on the same row starts, or waits for it; the
/// RETURNING clause hands back the freshly-incremented value from the exact same atomic step.
/// MUST be called from inside the same transaction as the Order insert it numbers, so a rolled-back
/// checkout does not "burn" a sequence number that no order ever ends up using.
/// </summary>
public static class OrderSequenceService
{
    public static async Task<long> AllocateNextAsync(FalahDbContext db, CancellationToken ct = default)
    {
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "UPDATE OrderSequence SET LastAllocated = LastAllocated + 1 WHERE Id = 1 RETURNING LastAllocated;";
        var result = await command.ExecuteScalarAsync(ct);
        if (result is null)
            throw new InvalidOperationException("جدول تسلسل أرقام الطلبات غير مهيأ - يجب إدخال الصف الابتدائي عند إنشاء القاعدة.");
        return (long)result;
    }
}
