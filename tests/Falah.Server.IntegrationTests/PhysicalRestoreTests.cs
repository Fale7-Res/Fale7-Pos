namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Backup;
using Falah.Domain.Backup;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>Runs the real RESTORE DATABASE sequence (SINGLE_USER, RESTORE WITH REPLACE, MULTI_USER)
/// end to end - but only against this test's own disposable FalahTest_&lt;guid&gt; database, which every
/// test asserts before anything destructive. ALTER DATABASE ... SINGLE_USER is scoped to that one
/// database; nothing here touches the instance or any other database (DB-0005 / DB-0006, B36).</summary>
public class PhysicalRestoreTests : IAsyncLifetime
{
    private const string TestBackupDirectory = @"E:\SqlData\Tests\Backups";
    private FalahApiFactory _factory = default!;
    private string _ownerId = default!;
    private string _managerId = default!;
    private HttpClient _owner = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["Backup:DefaultDirectory"] = TestBackupDirectory,
            ["Database:MaintenanceDrainTimeoutMilliseconds"] = "10000",
        });
        Assert.StartsWith("FalahTest_", _factory.DatabaseName);
        _ownerId = await _factory.SeedUserAsync("مالك اختبار الاستعادة", "owner", "all");
        _managerId = await _factory.SeedUserAsync("مدير اعتماد الاستعادة", "manager", "all");
        _factory.StationUserId = _ownerId;
        // Sessions live in the database: create them BEFORE the backup so they survive the restore.
        _owner = _factory.CreateClient();
        _owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(_ownerId));
        Assert.NotNull(_factory.TokenFor(_managerId));
    }

    public Task DisposeAsync() { _owner.Dispose(); _factory.Dispose(); return Task.CompletedTask; }

    private async Task<BackupRecordDto> BackupAsync()
    {
        var response = await _owner.PostAsJsonAsync("/api/backups", new CreateBackupRequest(_ownerId, TestBackupDirectory));
        response.EnsureSuccessStatusCode();
        var backup = (await response.Content.ReadFromJsonAsync<BackupRecordDto>())!;
        Assert.Equal("Succeeded", backup.Status);
        Assert.StartsWith(TestBackupDirectory, backup.FilePath, StringComparison.OrdinalIgnoreCase);
        return backup;
    }

    private async Task<HttpResponseMessage> RestoreAsync(BackupRecordDto backup)
    {
        var issued = await _owner.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "backup_manage", "اختبار استعادة"));
        issued.EnsureSuccessStatusCode();
        var approval = (await issued.Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!;
        return await _owner.PostAsJsonAsync($"/api/backups/{backup.Id}/restore", new RestoreBackupRequest(
            backup.Id, "اختبار استعادة على قاعدة اختبار", BackupRules.RequiredRestoreConfirmationPhrase, _ownerId,
            approval.ApprovalId, approval.ApproverUserId));
    }

    private async Task<string> UserAccessAsync()
    {
        var master = new SqlConnectionStringBuilder(await _factory.QueryAsync(db => Task.FromResult(db.Database.GetConnectionString()!)))
            { InitialCatalog = "master" }.ConnectionString;
        await using var connection = new SqlConnection(master);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CONVERT(nvarchar(60), DATABASEPROPERTYEX(@db, 'UserAccess'))";
        command.Parameters.AddWithValue("@db", _factory.DatabaseName);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task<int> UsersNamedAsync(string name)
    {
        SqlConnection.ClearAllPools(); // the restore killed every pooled connection of this database
        return await _factory.QueryAsync(db => db.Users.CountAsync(u => u.Name == name));
    }

    [Fact]
    public async Task A_restore_replaces_the_data_records_itself_and_leaves_the_database_usable()
    {
        var backup = await BackupAsync();
        await _factory.SeedUserAsync("أُضيف بعد النسخة", "cashier", "pos");
        Assert.Equal(1, await UsersNamedAsync("أُضيف بعد النسخة"));

        var response = await RestoreAsync(backup);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var restore = (await response.Content.ReadFromJsonAsync<BackupRecordDto>())!;
        Assert.Equal("Succeeded", restore.Status);
        Assert.Null(restore.ErrorMessage);
        Assert.Equal(0, await UsersNamedAsync("أُضيف بعد النسخة"));   // data really replaced
        Assert.Equal("MULTI_USER", await UserAccessAsync());
        Assert.True(await _factory.QueryAsync(db => db.BackupRecords.AnyAsync(b => b.Id == restore.Id && b.Status == "Succeeded")),
            "the restored database carries the record of its own restore");
        // Ordinary API traffic works again through the maintenance barrier and a recovered pool.
        (await _owner.GetAsync("/api/menu")).EnsureSuccessStatusCode();
        (await _owner.GetAsync("/api/backups")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_restore_waits_for_running_activity_to_finish_and_then_proceeds()
    {
        var backup = await BackupAsync();
        var master = new SqlConnectionStringBuilder(await _factory.QueryAsync(db => Task.FromResult(db.Database.GetConnectionString()!)))
            { InitialCatalog = "master" }.ConnectionString;
        var resource = "FalahMaintenance:" + _factory.DatabaseName.ToUpperInvariant();
        var holder = new SqlConnection(master);
        await holder.OpenAsync();
        await using (var take = holder.CreateCommand())
        {
            take.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@r1, @LockMode='Shared', @LockOwner='Session', @LockTimeout=0; SELECT @r";
            take.Parameters.AddWithValue("@r1", resource);
            Assert.True(Convert.ToInt32(await take.ExecuteScalarAsync()) >= 0);
        }

        var restoring = RestoreAsync(backup);
        await Task.Delay(1500);
        Assert.False(restoring.IsCompleted, "the restore must wait while another request is still using the database");
        await using (var release = holder.CreateCommand())
        {
            release.CommandText = "EXEC sys.sp_releaseapplock @Resource=@r1, @LockOwner='Session'";
            release.Parameters.AddWithValue("@r1", resource);
            await release.ExecuteNonQueryAsync();
        }
        await holder.DisposeAsync();

        var response = await restoring;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Succeeded", (await response.Content.ReadFromJsonAsync<BackupRecordDto>())!.Status);
        Assert.Equal("MULTI_USER", await UserAccessAsync());
        (await _owner.GetAsync("/api/menu")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_restore_from_a_damaged_file_is_reported_failed_and_the_database_stays_usable()
    {
        var backup = await BackupAsync();
        await _factory.SeedUserAsync("باقٍ بعد الفشل", "cashier", "pos");
        await File.WriteAllBytesAsync(backup.FilePath, new byte[64 * 1024]); // this test's own file only

        var response = await RestoreAsync(backup);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var restore = (await response.Content.ReadFromJsonAsync<BackupRecordDto>())!;
        Assert.Equal("Failed", restore.Status);
        Assert.False(string.IsNullOrWhiteSpace(restore.ErrorMessage));
        Assert.Equal("MULTI_USER", await UserAccessAsync());
        Assert.Equal(1, await UsersNamedAsync("باقٍ بعد الفشل"));      // nothing was replaced
        (await _owner.GetAsync("/api/menu")).EnsureSuccessStatusCode();
    }
    // ---- Independent audit 2026-11-23, F-03: the schema a backup was taken at ----

    private Task<string> LatestMigrationAsync() =>
        _factory.QueryAsync(async db => (await db.Database.GetAppliedMigrationsAsync()).OrderBy(m => m, StringComparer.Ordinal).Last());

    [Fact]
    public async Task A_backup_records_the_schema_it_was_taken_at_beside_the_file()
    {
        var backup = await BackupAsync();

        var stamp = Falah.Infrastructure.Backup.BackupSchemaStamp.PathFor(backup.FilePath);
        Assert.True(File.Exists(stamp));
        Assert.Equal(await LatestMigrationAsync(), File.ReadAllText(stamp).Trim());
    }

    [Fact]
    public async Task A_backup_taken_at_another_schema_is_refused_before_anything_is_replaced()
    {
        var backup = await BackupAsync();
        File.WriteAllText(Falah.Infrastructure.Backup.BackupSchemaStamp.PathFor(backup.FilePath), "20200101000000_AnOlderRelease");
        await _factory.SeedUserAsync("باقٍ بعد الرفض", "cashier", "pos");

        var response = await RestoreAsync(backup);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("Update-Database", await response.Content.ReadAsStringAsync());
        Assert.Equal("MULTI_USER", await UserAccessAsync());
        Assert.Equal(1, await UsersNamedAsync("باقٍ بعد الرفض"));      // nothing was replaced
    }

    [Fact]
    public async Task An_unstamped_backup_of_an_older_schema_is_restored_and_says_the_update_must_be_applied()
    {
        // An older release's backup: the newest migration is not in its history, and it has no stamp.
        var latest = await LatestMigrationAsync();
        await _factory.QueryAsync(db => db.Database.ExecuteSqlRawAsync("DELETE FROM [__EFMigrationsHistory] WHERE [MigrationId] = {0}", latest));
        var backup = await BackupAsync();
        File.Delete(Falah.Infrastructure.Backup.BackupSchemaStamp.PathFor(backup.FilePath));

        var response = await RestoreAsync(backup);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var restore = (await response.Content.ReadFromJsonAsync<BackupRecordDto>())!;
        Assert.Equal("Succeeded", restore.Status);
        Assert.Contains(latest, restore.ErrorMessage);
        Assert.Contains("Update-Database", restore.ErrorMessage);
    }
}
