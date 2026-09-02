namespace Falah.Infrastructure.Persistence;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// Allocates the next order sequence number as a single atomic UPDATE ... OUTPUT statement - there
/// is no separate read-then-write step for a concurrent request from the other device to race
/// into. SQL Server takes an exclusive row lock for the UPDATE's duration, so a second writer's
/// UPDATE against the same row simply waits for the first to commit or roll back and then sees the
/// already-incremented value; the OUTPUT clause hands back the freshly-incremented value from the
/// exact same atomic step (SQL Server has no RETURNING clause - OUTPUT INSERTED.* is its equivalent).
/// MUST be called from inside the same transaction as the Order insert it numbers, so a rolled-back
/// checkout does not "burn" a sequence number that no order ever ends up using.
/// </summary>
public static class OrderSequenceService
{
    public static async Task<long> AllocateNextAsync(FalahDbContext db, CancellationToken ct = default)
    {
        var connection = (SqlConnection)db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = (SqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "UPDATE OrderSequence SET LastAllocated = LastAllocated + 1 OUTPUT INSERTED.LastAllocated WHERE Id = 1;";
        var result = await command.ExecuteScalarAsync(ct);
        if (result is null)
            throw new InvalidOperationException("جدول تسلسل أرقام الطلبات غير مهيأ - يجب إدخال الصف الابتدائي عند إنشاء القاعدة.");
        return (long)result;
    }
}
