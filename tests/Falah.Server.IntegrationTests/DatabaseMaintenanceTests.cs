namespace Falah.Server.IntegrationTests;
using System.Net;
using System.Net.Http.Json;
using Falah.Application.Backup;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class DatabaseMaintenanceTests
{
    private sealed class HeldLease(SqlConnection connection, string resource) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await using var command = connection.CreateCommand(); command.CommandText = "EXEC sys.sp_releaseapplock @Resource=@resource, @LockOwner='Session'";
            command.Parameters.AddWithValue("@resource", resource); await command.ExecuteNonQueryAsync(); await connection.DisposeAsync();
        }
    }
    private static async Task<HeldLease> HoldAsync(FalahApiFactory factory, string mode)
    {
        Assert.StartsWith("FalahTest_", factory.DatabaseName);
        var original = await factory.QueryAsync(db=>Task.FromResult(db.Database.GetConnectionString()!));
        var connection = new SqlConnection(new SqlConnectionStringBuilder(original) { InitialCatalog = "master" }.ConnectionString);
        await connection.OpenAsync(); var resource = "FalahMaintenance:" + factory.DatabaseName.ToUpperInvariant();
        await using var command = connection.CreateCommand();
        command.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@resource, @LockMode=@mode, @LockOwner='Session', @LockTimeout=0; SELECT @r";
        command.Parameters.AddWithValue("@resource",resource); command.Parameters.AddWithValue("@mode",mode);
        Assert.True(Convert.ToInt32(await command.ExecuteScalarAsync()) >= 0);
        return new HeldLease(connection,resource);
    }
    [Fact]
    public async Task An_exclusive_restore_lease_refuses_API_access_until_released()
    {
        using var factory = new FalahApiFactory(); using var client = factory.CreateAuthenticatedClient();
        (await client.GetAsync("/api/menu")).EnsureSuccessStatusCode();
        await using (var held = await HoldAsync(factory,"Exclusive"))
        {
            var response = await client.GetAsync("/api/menu");
            Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
        }
        (await client.GetAsync("/api/menu")).EnsureSuccessStatusCode();
    }
    [Fact]
    public async Task Restore_cannot_begin_while_another_database_activity_lease_is_active()
    {
        using var factory = new FalahApiFactory(configOverrides:new Dictionary<string,string?> { ["Database:MaintenanceDrainTimeoutMilliseconds"]="200" });
        var actor = await factory.SeedUserAsync("مالك الاختبار", "owner", "all"); factory.StationUserId=actor;
        using var client = factory.CreateAuthenticatedClient(); (await client.GetAsync("/api/menu")).EnsureSuccessStatusCode();
        await using var held=await HoldAsync(factory,"Shared");
        // Invalid/missing target deliberately guarantees the RED run never performs a RESTORE.
        var response = await client.PostAsJsonAsync("/api/backups/missing/restore", new RestoreBackupRequest("missing","اختبار","invalid",actor,null,null));
        Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
    }
}
