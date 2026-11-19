namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Backup;
using Falah.Domain.Backup;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Ported for the current SQL Server architecture (native BACKUP DATABASE/RESTORE DATABASE) rather
/// than the reference app's own browser-localStorage JSON export - see
/// Falah.Domain.Backup.BackupRules' own doc comment. CreateBackup and VerifyBackup are exercised
/// fully against the real per-test SQL Server database (safe: BACKUP DATABASE/RESTORE VERIFYONLY
/// never require exclusive access). RestoreBackup's GATES (permission, confirmation phrase, reason,
/// active-operations block) are exercised fully too - each throws before ever reaching the actual
/// RESTORE DATABASE statement, so none of these tests force the shared SQL Server instance into
/// single-user mode. See RestoreBackupHandler's own doc comment for why the actual restore sequence
/// itself is not exercised end-to-end in this automated suite.
/// </summary>
public class BackupVerticalSliceTests : IAsyncLifetime
{
    private static readonly string TestBackupDirectory = @"E:\SqlData\Tests\Backups";

    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _backupAdminId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?> { ["Backup:DefaultDirectory"] = TestBackupDirectory });
        _client = _factory.CreateAuthenticatedClient();
        // Read-only assertions use the owner session; mutation bodies still name their actual actor.
        _factory.StationUserId = await _factory.SeedUserAsync("قارئ بيانات المالك للاختبار", "owner", "all");
        _backupAdminId = await _factory.SeedUserAsync("مسؤول النسخ الاحتياطي", "supervisor", "backup_manage");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Creating_a_backup_requires_backup_manage()
    {
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var response = await _client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(noPermissionUser, TestBackupDirectory));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // Audit 2026-11-18: every backup sat on the disk the database itself is on, so the disk that failed
    // took the backups with it. Backup:CopyToDirectory (a second disk, a USB drive, a share) gets a copy.
    [Fact]
    public async Task A_backup_is_also_copied_to_the_configured_second_place()
    {
        var copyDirectory = Path.Combine(TestBackupDirectory, "copy-" + Guid.NewGuid().ToString("N"));
        using var factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["Backup:DefaultDirectory"] = TestBackupDirectory, ["Backup:CopyToDirectory"] = copyDirectory,
        });
        try
        {
            var client = factory.CreateAuthenticatedClient();
            var adminId = await factory.SeedUserAsync("مسؤول النسخ", "supervisor", "backup_manage");

            var record = await (await client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(adminId, TestBackupDirectory)))
                .Content.ReadFromJsonAsync<BackupRecordDto>();

            Assert.Equal("Succeeded", record!.Status);
            var copy = Path.Combine(copyDirectory, Path.GetFileName(record.FilePath));
            Assert.True(File.Exists(copy), $"expected the second copy at {copy}");
            Assert.Equal(new FileInfo(record.FilePath).Length, new FileInfo(copy).Length);
        }
        finally { try { Directory.Delete(copyDirectory, recursive: true); } catch { } }
    }

    [Fact]
    public async Task A_second_copy_that_cannot_be_written_keeps_the_backup_and_raises_a_critical_audit()
    {
        var notADirectory = Path.Combine(TestBackupDirectory, "not-a-dir-" + Guid.NewGuid().ToString("N") + ".txt");
        Directory.CreateDirectory(TestBackupDirectory);
        File.WriteAllText(notADirectory, "x");
        using var factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["Backup:DefaultDirectory"] = TestBackupDirectory, ["Backup:CopyToDirectory"] = notADirectory,
        });
        try
        {
            var client = factory.CreateAuthenticatedClient();
            var adminId = await factory.SeedUserAsync("مسؤول النسخ", "supervisor", "backup_manage");

            var record = await (await client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(adminId, TestBackupDirectory)))
                .Content.ReadFromJsonAsync<BackupRecordDto>();

            Assert.Equal("Succeeded", record!.Status);
            var audit = await factory.QueryAsync(db => db.AuditEvents.AsNoTracking()
                .Where(e => e.ReferenceId == record.Id && e.Severity == "critical").ToListAsync());
            Assert.Contains(audit, e => e.Action == "فشل نسخ النسخة الاحتياطية إلى المكان الثاني");
        }
        finally { File.Delete(notADirectory); }
    }

    [Fact]
    public async Task Creating_a_backup_against_the_real_database_succeeds_and_writes_a_real_file()
    {
        var response = await _client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(_backupAdminId, TestBackupDirectory));
        response.EnsureSuccessStatusCode();
        var record = await response.Content.ReadFromJsonAsync<BackupRecordDto>();
        Assert.Equal("Succeeded", record!.Status);
        Assert.True(record.FileSizeBytes > 0);
        Assert.True(File.Exists(record.FilePath), $"expected a real backup file at {record.FilePath}");

        var listed = await (await _client.GetAsync("/api/backups")).Content.ReadFromJsonAsync<List<BackupRecordDto>>();
        Assert.Contains(listed!, b => b.Id == record.Id);
    }

    [Fact]
    public async Task Every_backup_gets_a_distinct_timestamped_filename_never_overwriting_an_earlier_one()
    {
        var first = await (await _client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(_backupAdminId, TestBackupDirectory)))
            .Content.ReadFromJsonAsync<BackupRecordDto>();
        await Task.Delay(1100); // filenames are second-resolution timestamps
        var second = await (await _client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(_backupAdminId, TestBackupDirectory)))
            .Content.ReadFromJsonAsync<BackupRecordDto>();

        Assert.NotEqual(first!.FilePath, second!.FilePath);
        Assert.True(File.Exists(first.FilePath));
        Assert.True(File.Exists(second.FilePath));
    }

    [Fact]
    public async Task Verifying_a_real_backup_reports_it_as_valid()
    {
        var backup = await (await _client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(_backupAdminId, TestBackupDirectory)))
            .Content.ReadFromJsonAsync<BackupRecordDto>();

        var response = await _client.PostAsJsonAsync($"/api/backups/{backup!.Id}/verify", new VerifyBackupRequest(backup.Id, _backupAdminId));
        response.EnsureSuccessStatusCode();
        var verified = await response.Content.ReadFromJsonAsync<BackupRecordDto>();
        Assert.Equal("Valid", verified!.VerificationStatus);
        Assert.NotNull(verified.VerifiedAtUtc);
    }

    [Fact]
    public async Task Verifying_requires_backup_manage()
    {
        var backup = await (await _client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(_backupAdminId, TestBackupDirectory)))
            .Content.ReadFromJsonAsync<BackupRecordDto>();
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var response = await _client.PostAsJsonAsync($"/api/backups/{backup!.Id}/verify", new VerifyBackupRequest(backup.Id, noPermissionUser));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- Restore gates (never reach the actual RESTORE DATABASE statement - see class doc) ----

    [Fact]
    public async Task Restoring_without_backup_manage_is_rejected_before_touching_the_database()
    {
        var backup = await (await _client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(_backupAdminId, TestBackupDirectory)))
            .Content.ReadFromJsonAsync<BackupRecordDto>();
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");

        var response = await _client.PostAsJsonAsync($"/api/backups/{backup!.Id}/restore",
            new RestoreBackupRequest(backup.Id, "اختبار", BackupRules.RequiredRestoreConfirmationPhrase, noPermissionUser, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var record = await (await _client.GetAsync($"/api/backups/{backup.Id}")).Content.ReadFromJsonAsync<BackupRecordDto>();
        Assert.Equal("Succeeded", record!.Status); // the original backup record is untouched
    }

    [Fact]
    public async Task Restoring_without_the_exact_confirmation_phrase_is_rejected()
    {
        var backup = await (await _client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(_backupAdminId, TestBackupDirectory)))
            .Content.ReadFromJsonAsync<BackupRecordDto>();

        var response = await _client.PostAsJsonAsync($"/api/backups/{backup!.Id}/restore",
            new RestoreBackupRequest(backup.Id, "اختبار", "please restore this now", _backupAdminId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Restoring_without_a_reason_is_rejected()
    {
        var backup = await (await _client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(_backupAdminId, TestBackupDirectory)))
            .Content.ReadFromJsonAsync<BackupRecordDto>();

        var response = await _client.PostAsJsonAsync($"/api/backups/{backup!.Id}/restore",
            new RestoreBackupRequest(backup.Id, "", BackupRules.RequiredRestoreConfirmationPhrase, _backupAdminId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_blocked_restore_attempt_still_writes_a_critical_audit_record()
    {
        var backup = await (await _client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(_backupAdminId, TestBackupDirectory)))
            .Content.ReadFromJsonAsync<BackupRecordDto>();

        var blocked = await _client.PostAsJsonAsync($"/api/backups/{backup!.Id}/restore",
            new RestoreBackupRequest(backup.Id, "", "wrong-phrase", _backupAdminId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blocked.StatusCode);

        var auditViewer = await _factory.SeedUserAsync("مسؤول التدقيق", "manager", "all");
        var events = await (await _client.GetAsync($"/api/audit/events?actorUserId={auditViewer}&category=backup"))
            .Content.ReadFromJsonAsync<List<Falah.Application.Audit.AuditEventDto>>();
        Assert.Contains(events!, e => e.Severity == "critical" && e.Action.Contains("مرفوضة"));
    }

    private sealed class FixedBackupTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
    }
    [Fact]
    public async Task Two_backups_with_the_exact_same_timestamp_never_share_a_file()
    {
        using var host = _factory.WithWebHostBuilder(b => Microsoft.AspNetCore.TestHost.WebHostBuilderExtensions.ConfigureTestServices(b,
            s => Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<TimeProvider>(s, new FixedBackupTime())));
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.TokenFor(_backupAdminId));
        var records = new List<BackupRecordDto>();
        for (var i = 0; i < 2; i++)
        {
            var response = await client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(_backupAdminId, TestBackupDirectory));
            response.EnsureSuccessStatusCode();
            var record = (await response.Content.ReadFromJsonAsync<BackupRecordDto>())!;
            Assert.Equal("Succeeded", record.Status); records.Add(record);
        }
        Assert.NotEqual(records[0].FilePath, records[1].FilePath);
        Assert.All(records, r => Assert.True(File.Exists(r.FilePath)));
    }


    private async Task RunBackupWorkerStepAsync(string method)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Falah.Infrastructure.Persistence.FalahDbContext>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Backup:Automatic:KeepCount"] = "1", ["Backup:Automatic:MaxAgeHours"] = "20" }).Build();
        var worker = new Falah.Infrastructure.Backup.ScheduledBackupService(_factory.Services.GetRequiredService<IServiceScopeFactory>(), config,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Falah.Infrastructure.Backup.ScheduledBackupService>.Instance);
        var target = typeof(Falah.Infrastructure.Backup.ScheduledBackupService).GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)target.Invoke(worker, method == "PruneAsync" ? new object[] { db, CancellationToken.None } : new object[] { CancellationToken.None })!;
    }
    [Fact]
    public async Task Retention_never_deletes_a_file_still_referenced_by_a_kept_backup()
    {
        var result = await _client.PostAsJsonAsync("/api/backups", new CreateBackupRequest(_backupAdminId, TestBackupDirectory));
        result.EnsureSuccessStatusCode(); var kept = (await result.Content.ReadFromJsonAsync<BackupRecordDto>())!;
        Assert.Equal("Succeeded", kept.Status);
        await _factory.QueryAsync(async db => {
            await db.BackupRecords.Where(b => b.Id == kept.Id).ExecuteUpdateAsync(s => s.SetProperty(b => b.VerificationStatus, "Valid"));
            db.BackupRecords.Add(new Falah.Infrastructure.Persistence.Entities.BackupRecordEntity {
                Id = "legacy-alias", Kind = "Backup", Status = "Succeeded", FilePath = kept.FilePath, FileSizeBytes = kept.FileSizeBytes,
                VerificationStatus = "Valid", RequestedBy = "test", RequestedAtUtc = DateTimeOffset.UtcNow.AddDays(-1), CompletedAtUtc = DateTimeOffset.UtcNow.AddDays(-1) });
            await db.SaveChangesAsync(); return true;
        });
        await RunBackupWorkerStepAsync("PruneAsync");
        Assert.True(File.Exists(kept.FilePath));
        Assert.NotNull(await _factory.QueryAsync(db => db.BackupRecords.Where(b => b.Id == kept.Id).Select(b => b.FileSizeBytes).SingleAsync()));
    }
    [Fact]
    public async Task A_recent_success_record_with_a_missing_file_does_not_delay_the_next_backup()
    {
        var missing = Path.Combine(TestBackupDirectory, "missing-" + Guid.NewGuid().ToString("N") + ".bak");
        await _factory.QueryAsync(async db => {
            db.BackupRecords.Add(new Falah.Infrastructure.Persistence.Entities.BackupRecordEntity {
                Id = "missing-recent", Kind = "Backup", Status = "Succeeded", FilePath = missing, FileSizeBytes = 100,
                VerificationStatus = "Valid", RequestedBy = "test", RequestedAtUtc = DateTimeOffset.UtcNow, CompletedAtUtc = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(); return true;
        });
        await RunBackupWorkerStepAsync("RunCycleAsync");
        var created = await _factory.QueryAsync(db => db.BackupRecords.Where(b => b.Id != "missing-recent" && b.Kind == "Backup").ToListAsync());
        var backup = Assert.Single(created);
        Assert.True(File.Exists(backup.FilePath));
        Assert.Equal("Valid", backup.VerificationStatus);
    }

}
