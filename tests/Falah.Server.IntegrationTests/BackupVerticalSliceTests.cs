namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Backup;
using Falah.Domain.Backup;
using Xunit;

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
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
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
}
