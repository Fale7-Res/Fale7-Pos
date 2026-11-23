namespace Falah.Infrastructure.Persistence;

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Puts every connection EF takes from the pool back at READ COMMITTED before it is used.
///
/// Found on the restaurant laptop 2026-11-23: the user-permission and user-activation commands run SERIALIZABLE
/// transactions (so two administrators cannot both remove the last user manager), and SQL Server keeps a
/// connection's isolation level when the pool hands it to the next request. Whatever ran next on that connection
/// ran SERIALIZABLE: the print agents' claim query failed with HTTP 500 ("READPAST ... only in READ COMMITTED or
/// REPEATABLE READ"), and every other read took range locks nobody asked for. A command that needs another level
/// still sets it itself when it begins its transaction.
/// </summary>
public sealed class IsolationLevelResetInterceptor : DbConnectionInterceptor
{
    private const string Reset = "SET TRANSACTION ISOLATION LEVEL READ COMMITTED;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = connection.CreateCommand();
        command.CommandText = Reset;
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = Reset;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
