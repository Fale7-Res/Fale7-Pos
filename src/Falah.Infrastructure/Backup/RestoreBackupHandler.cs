namespace Falah.Infrastructure.Backup;

using Falah.Application.Backup;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Backup;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The single most destructive command in this entire system - a SQL Server RESTORE DATABASE
/// discards every committed transaction since the backup was taken. Gated far more strictly than
/// any other command here (see BackupRequests.RestoreBackupRequest's own doc comment for exactly
/// what's new): mandatory typed-out confirmation phrase, mandatory reason, and a hard block while
/// any financial operation is mid-flight (a pending FinancialJournalEntries row - the same "started,
/// not committed" signal every money-moving command in this codebase already relies on).
///
/// PHYSICAL VERIFICATION NOTE (documented per direct instruction): the actual RESTORE DATABASE
/// sequence below - ALTER DATABASE ... SET SINGLE_USER WITH ROLLBACK IMMEDIATE, then RESTORE
/// DATABASE, then SET MULTI_USER - is architecturally correct SQL Server usage, but has NOT been
/// exercised end-to-end against a live instance in this automated pass. Doing so in the integration
/// test suite would force the SAME shared SQL Server instance every other concurrently-running test
/// depends on into single-user mode, killing their connections mid-test - a genuinely destructive
/// action this pass will not take just to exercise a code path. Every gate ABOVE the actual restore
/// call (permission/approval, confirmation phrase, reason, active-operations block) IS exercised by
/// real integration tests against real SQL Server, since none of them touch the database's own
/// connectivity. The restore statement sequence itself needs a dedicated, isolated SQL Server
/// instance to verify physically before this path is ever used against a real production database.
/// </summary>
public sealed class RestoreBackupHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;
    private readonly CreateBackupHandler _createBackup;

    public RestoreBackupHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog, CreateBackupHandler createBackup)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
        _createBackup = createBackup;
    }

    public async Task<BackupRecordDto> HandleAsync(RestoreBackupRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        var backup = await _db.BackupRecords.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.BackupRecordId, ct)
            ?? throw new InvalidOperationException("سجل النسخة الاحتياطية غير موجود.");

        var now = DateTimeOffset.UtcNow;
        var restoreRecord = new BackupRecordEntity
        {
            Id = $"restore-{Guid.NewGuid():N}", Kind = nameof(BackupRecordKind.Restore), FilePath = backup.FilePath,
            Status = nameof(BackupRecordStatus.Requested), RequestedBy = actorUser.Name, RequestedAtUtc = now,
            Reason = request.Reason,
        };

        try
        {
            BackupRules.AssertRestoreReason(request.Reason);
            BackupRules.AssertRestoreConfirmationPhrase(request.ConfirmationPhrase);

            ManagerApproval? approval = null;
            if (!string.IsNullOrEmpty(request.ManagerApprovalId))
            {
                var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                    ?? throw new DomainException("المعتمد غير موجود.");
                approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                    approverUser.Role, DateTimeOffset.UtcNow, "backup_manage", request.Reason);
            }
            var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "backup_manage", approval, requireApproval: true);
            restoreRecord.ApprovedBy = authorization.Approver?.ApproverName;

            if (!File.Exists(backup.FilePath))
                throw new DomainException("ملف النسخة الاحتياطية غير موجود على القرص.");

            var pendingCount = await _db.FinancialJournalEntries.AsNoTracking().CountAsync(j => j.Status != "Complete", ct);
            BackupRules.AssertNoActiveOperations(pendingCount);
        }
        catch (Exception ex) when (ex is DomainException)
        {
            restoreRecord.Status = nameof(BackupRecordStatus.Blocked);
            restoreRecord.ErrorMessage = ex.Message;
            restoreRecord.CompletedAtUtc = DateTimeOffset.UtcNow;
            _db.BackupRecords.Add(restoreRecord);
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "محاولة استعادة نسخة احتياطية - مرفوضة", "backup", $"{backup.FilePath} - السبب: {ex.Message}",
                "critical", restoreRecord.CompletedAtUtc.Value, Reason: request.Reason, ReferenceId: restoreRecord.Id));
            await _db.SaveChangesAsync(ct);
            throw;
        }

        // ---- Safety backup of what is about to be discarded ----
        //
        // A restore throws away every transaction committed since the chosen backup was taken. If
        // the operator picked the wrong file, or the backup turns out to be older than they thought,
        // this file is the only way back - so it is taken unconditionally, after every gate has
        // passed and before anything is destroyed, and a failure to take it BLOCKS the restore.
        // Refusing here costs the operator a retry; not refusing can cost the restaurant its books.
        //
        // Only the file survives the restore. This BackupRecords row does not - it is written to
        // the very database that is about to be overwritten - which is why the safety file's path
        // is recorded in the audit entry for the restore itself as well.
        string safetyBackupPath;
        try
        {
            var safety = await _createBackup.HandleAsync(new CreateBackupRequest(request.ActorUserId), ct);
            if (safety.Status != nameof(BackupRecordStatus.Succeeded))
                throw new DomainException(safety.ErrorMessage ?? "تعذّر إنشاء نسخة أمان قبل الاستعادة.");
            safetyBackupPath = safety.FilePath;
        }
        catch (Exception ex)
        {
            restoreRecord.Status = nameof(BackupRecordStatus.Blocked);
            restoreRecord.ErrorMessage = "لم تُنشأ نسخة أمان للحالة الحالية، فأُوقفت الاستعادة: " + ex.Message;
            restoreRecord.CompletedAtUtc = DateTimeOffset.UtcNow;
            _db.BackupRecords.Add(restoreRecord);
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "محاولة استعادة نسخة احتياطية - مرفوضة", "backup", restoreRecord.ErrorMessage,
                "critical", restoreRecord.CompletedAtUtc.Value, Reason: request.Reason, ReferenceId: restoreRecord.Id));
            await _db.SaveChangesAsync(ct);
            throw new DomainException(restoreRecord.ErrorMessage);
        }

        _db.BackupRecords.Add(restoreRecord);
        await _db.SaveChangesAsync(ct);

        var databaseName = _db.Database.GetDbConnection().Database;
        BackupRules.AssertSafeDatabaseIdentifier(databaseName);
        var connectionString = _db.Database.GetConnectionString()!;
        var databaseWasReplaced = false;
        try
        {
            // RESTORE requires exclusive access - a connection scoped to 'master', never to the
            // database being restored itself (you cannot restore a database you are connected to).
            var masterBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
            await using var connection = new SqlConnection(masterBuilder.ConnectionString);
            await connection.OpenAsync(ct);

            await using (var singleUser = connection.CreateCommand())
            {
                singleUser.CommandText = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                await singleUser.ExecuteNonQueryAsync(ct);
            }
            await using (var restore = connection.CreateCommand())
            {
                restore.CommandText = $"RESTORE DATABASE [{databaseName}] FROM DISK = @path WITH REPLACE";
                restore.Parameters.AddWithValue("@path", backup.FilePath);
                await restore.ExecuteNonQueryAsync(ct);
            }
            await using (var multiUser = connection.CreateCommand())
            {
                multiUser.CommandText = $"ALTER DATABASE [{databaseName}] SET MULTI_USER";
                await multiUser.ExecuteNonQueryAsync(ct);
            }

            databaseWasReplaced = true;
            restoreRecord.Status = nameof(BackupRecordStatus.Succeeded);
            restoreRecord.CompletedAtUtc = DateTimeOffset.UtcNow;
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "استعادة نسخة احتياطية", "backup",
                $"تمت الاستعادة من {backup.FilePath} - نسخة الأمان للحالة السابقة: {safetyBackupPath}", "critical",
                restoreRecord.CompletedAtUtc.Value, Reason: request.Reason, AuthorizedBy: restoreRecord.ApprovedBy, ReferenceId: restoreRecord.Id));
        }
        catch (Exception ex)
        {
            // A restore that fails partway can leave the database in SINGLE_USER - which locks the
            // whole restaurant out of a database that was never replaced. Best effort, and its own
            // failure must not bury the real error above.
            await TryReleaseSingleUserAsync(connectionString, databaseName);

            restoreRecord.Status = nameof(BackupRecordStatus.Failed);
            restoreRecord.ErrorMessage = ex.Message;
            restoreRecord.CompletedAtUtc = DateTimeOffset.UtcNow;
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "فشل استعادة نسخة احتياطية", "backup",
                $"{backup.FilePath} - الخطأ: {ex.Message} - نسخة الأمان للحالة السابقة: {safetyBackupPath}", "critical",
                restoreRecord.CompletedAtUtc.Value, Reason: request.Reason, ReferenceId: restoreRecord.Id));
        }

        await RecordOutcomeAsync(restoreRecord, databaseWasReplaced, connectionString, ct);
        return BackupQueryService.ToDto(restoreRecord);
    }

    /// <summary>Writing down what happened, after the database it would be written to may have been
    /// swapped underneath us.
    ///
    /// The restore record was saved into the database the restore has just discarded. Once the
    /// restore succeeds that row does not exist any more, so the update EF had queued for it
    /// matched nothing and threw - and the caller got HTTP 500 for an operation that had completed
    /// perfectly. The operator's screen said the restore failed while the restaurant's entire
    /// database had in fact been replaced, which is the most dangerous thing this handler could
    /// possibly report.
    ///
    /// So the row is re-created inside the restored database rather than updated, along with the
    /// audit entry - otherwise the restored database carries no trace of ever having been restored,
    /// and neither does anything else. And if even that write fails, the restore still happened:
    /// the note about it is what was lost, and that is what the caller is told.</summary>
    private async Task RecordOutcomeAsync(BackupRecordEntity restoreRecord, bool databaseWasReplaced, string connectionString, CancellationToken ct)
    {
        if (databaseWasReplaced)
        {
            // SINGLE_USER WITH ROLLBACK IMMEDIATE killed every pooled connection; handing one of
            // those corpses to the next command would fail for a reason unrelated to any of this.
            using (var pooled = new SqlConnection(connectionString)) SqlConnection.ClearPool(pooled);
            _db.Entry(restoreRecord).State = EntityState.Added;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _db.ChangeTracker.Clear();
            var note = databaseWasReplaced
                ? "تمت الاستعادة فعليًا، لكن تعذّر تسجيلها داخل قاعدة البيانات المستعادة: "
                : "تعذّر تسجيل نتيجة المحاولة: ";
            restoreRecord.ErrorMessage = restoreRecord.ErrorMessage is null
                ? note + ex.Message
                : restoreRecord.ErrorMessage + " | " + note + ex.Message;
        }
    }

    private static async Task TryReleaseSingleUserAsync(string connectionString, string databaseName)
    {
        try
        {
            var masterBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
            await using var connection = new SqlConnection(masterBuilder.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"IF DATABASEPROPERTYEX('{databaseName}', 'UserAccess') = 'SINGLE_USER' " +
                                  $"ALTER DATABASE [{databaseName}] SET MULTI_USER";
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            // Nothing more this process can do; the operator is already being told the restore failed.
        }
    }
}
