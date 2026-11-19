namespace Falah.Infrastructure.Persistence;

using Microsoft.Data.SqlClient;

public sealed class DatabaseMaintenanceBusyException : Exception
{
    public DatabaseMaintenanceBusyException() : base("الخادم في وضع صيانة أو توجد عمليات جارية؛ لم تبدأ العملية. أعد المحاولة بعد انتهائها.") { }
}

/// <summary>A master-database session lock shared by API requests and background work across
/// server processes. Restore starts with Update (compatible with Shared, one restorer only),
/// authenticates, then upgrades to Exclusive while existing activity drains.</summary>
public sealed class DatabaseActivityScope : IAsyncDisposable
{
    private readonly string _masterConnection;
    private readonly string _database;
    private readonly string _resource;
    private readonly int _drainTimeout;
    private SqlConnection? _connection;
    private int _references;
    private bool _restoreIntent;
    private bool _exclusive;

    public DatabaseActivityScope(string connectionString, int drainTimeoutMilliseconds = 5000)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        _database = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(_database)) throw new ArgumentException("Database name is required for maintenance locking.");
        _resource = "FalahMaintenance:" + _database.ToUpperInvariant();
        builder.InitialCatalog = "master";
        _masterConnection = builder.ConnectionString;
        _drainTimeout = Math.Clamp(drainTimeoutMilliseconds, 0, 60000);
    }

    public async Task EnterRequestAsync(bool restoreIntent, CancellationToken ct = default)
    {
        if (_references > 0) return;
        await AcquireAsync(restoreIntent ? "Update" : "Shared", 0, ct);
        _restoreIntent = restoreIntent;
        if (!restoreIntent)
        {
            // If recovery could not return the database to MULTI_USER, keep ordinary callers
            // out of its single connection slot. The master lock itself never uses that slot.
            await using var check = _connection!.CreateCommand();
            check.CommandText = "SELECT CONVERT(nvarchar(60),DATABASEPROPERTYEX(@database,'UserAccess'))";
            check.Parameters.AddWithValue("@database", _database);
            // Only a database actually held in SINGLE_USER/RESTRICTED_USER is maintenance. NULL means
            // the database is missing or unreachable: that is a server failure, and it must reach the
            // exception handler (500 with a reference), not be reported as "maintenance" (2026-11-07).
            if (await check.ExecuteScalarAsync(ct) is string access && access != "MULTI_USER")
                throw new DatabaseMaintenanceBusyException();
        }
    }

    public async Task EnterRestoreAsync(CancellationToken ct = default)
    {
        if (_exclusive) return;
        if (_references == 0) await EnterRequestAsync(true, ct);
        if (!_restoreIntent) throw new InvalidOperationException("Restore needs an Update intent lease before authentication.");
        await AcquireAsync("Exclusive", _drainTimeout, ct);
        _exclusive = true;
    }

    private async Task AcquireAsync(string mode, int timeout, CancellationToken ct)
    {
        if (_connection is null)
        {
            _connection = new SqlConnection(_masterConnection);
            await _connection.OpenAsync(ct);
        }
        await using var command = _connection.CreateCommand();
        command.CommandTimeout = Math.Max(5, timeout / 1000 + 5);
        command.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@resource, @LockMode=@mode, @LockOwner='Session', @LockTimeout=@timeout; SELECT @r";
        command.Parameters.AddWithValue("@resource", _resource);
        command.Parameters.AddWithValue("@mode", mode);
        command.Parameters.AddWithValue("@timeout", timeout);
        if (Convert.ToInt32(await command.ExecuteScalarAsync(ct)) < 0) throw new DatabaseMaintenanceBusyException();
        _references++;
    }

    public async ValueTask DisposeAsync()
    {
        var connection = _connection;
        _connection = null;
        if (connection is null) return;
        try
        {
            // Upgrading increments the SQL application's lock reference count too.
            while (_references-- > 0)
            {
                await using var command = connection.CreateCommand();
                command.CommandTimeout = 5;
                command.CommandText = "EXEC sys.sp_releaseapplock @Resource=@resource, @LockOwner='Session'";
                command.Parameters.AddWithValue("@resource", _resource);
                await command.ExecuteNonQueryAsync(CancellationToken.None);
            }
        }
        catch { SqlConnection.ClearPool(connection); }
        finally
        {
            _references = 0; _exclusive = false; _restoreIntent = false;
            await connection.DisposeAsync();
        }
    }
}
