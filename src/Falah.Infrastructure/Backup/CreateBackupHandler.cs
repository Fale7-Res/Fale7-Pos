namespace Falah.Infrastructure.Backup;

using Falah.Application.Backup;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Backup;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

/// <summary>Native SQL Server backup (`BACKUP DATABASE ... WITH COPY_ONLY, CHECKSUM, FORMAT`) - see
/// BackupRules' own doc comment for why this replaces the reference app's browser-localStorage JSON
/// export rather than porting it verbatim. COPY_ONLY so an ad-hoc, operator-triggered backup never
/// disturbs any separately-scheduled differential/log backup chain the DBA may already have running
/// against this same database. A fresh, timestamped filename is minted every time (never a caller-
/// supplied name), so a backup can never silently overwrite an earlier one.</summary>
public sealed class CreateBackupHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _time;

    public CreateBackupHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog, IConfiguration configuration, TimeProvider? time = null)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
        _configuration = configuration;
        _time = time ?? TimeProvider.System;
    }

    public async Task<BackupRecordDto> HandleAsync(CreateBackupRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "backup_manage");

        return await RunAsync(new BackupActor(actorUser.Id, actorUser.Name, actorUser.Role), request.DestinationDirectory, ct);
    }

    /// <summary>The backup itself, with no permission check in front of it - every caller that can
    /// reach this has already established who is asking. <see cref="HandleAsync"/> is the operator
    /// path and authorizes first; the nightly schedule is the other caller and has no user to
    /// authorize, which is exactly why the check lives above this rather than inside it.</summary>
    public async Task<BackupRecordDto> RunAsync(BackupActor actor, string? destinationDirectory, CancellationToken ct = default)
    {
        var directory = destinationDirectory ?? _configuration["Backup:DefaultDirectory"] ?? "E:\\falah-restaurant\\Backups";
        BackupRules.AssertDestinationDirectory(directory);
        Directory.CreateDirectory(directory);

        var databaseName = _db.Database.GetDbConnection().Database;
        BackupRules.AssertSafeDatabaseIdentifier(databaseName);
        var now = _time.GetUtcNow();
        var fileName = $"falah-{databaseName}-{now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.bak";
        var filePath = Path.Combine(directory, fileName);

        var entity = new BackupRecordEntity
        {
            Id = $"backup-{Guid.NewGuid():N}", Kind = nameof(BackupRecordKind.Backup), FilePath = filePath,
            Status = nameof(BackupRecordStatus.Requested), RequestedBy = actor.Name, RequestedAtUtc = now,
        };
        _db.BackupRecords.Add(entity);
        await _db.SaveChangesAsync(ct);

        try
        {
            // BACKUP DATABASE cannot run inside a user transaction - this is a standalone statement
            // against the live database, deliberately not wrapped in _db.Database.BeginTransaction.
            // databaseName is validated as a plain identifier above (never attacker-controlled to
            // begin with - it comes from this app's own connection string) - EF1002 is a correct
            // general warning about interpolating into SQL, but the actual value/path arguments
            // below ARE passed as real parameters ({0}/{1}), only the identifier is interpolated,
            // which SQL Server gives no other way to express.
#pragma warning disable EF1002
            await _db.Database.ExecuteSqlRawAsync(
                $"BACKUP DATABASE [{databaseName}] TO DISK = {{0}} WITH COPY_ONLY, CHECKSUM, FORMAT, NAME = {{1}}",
                new object[] { filePath, $"Falah backup {now:O}" }, ct);
#pragma warning restore EF1002

            entity.FileSizeBytes = File.Exists(filePath) ? new FileInfo(filePath).Length : null;
            entity.Status = nameof(BackupRecordStatus.Succeeded);
            entity.CompletedAtUtc = DateTimeOffset.UtcNow;
            // Audit 2026-11-23, F-03: the schema this backup was taken at, beside it, so a restore can refuse a
            // backup this build cannot run on. A stamp that cannot be written leaves the backup standing.
            if (!BackupSchemaStamp.TryWrite(filePath, await BackupSchemaStamp.AppliedVersionAsync(_db, ct)))
                _auditLog.Append(new AuditEvent(
                    $"audit-{Guid.NewGuid():N}", actor.Id, actor.Name, actor.Role,
                    "تعذر تسجيل إصدار قاعدة البيانات بجانب النسخة الاحتياطية", "backup",
                    $"{BackupSchemaStamp.PathFor(filePath)} - النسخة نفسها سليمة، لكن الاستعادة لن تعرف إصدارها مسبقًا.",
                    "warning", DateTimeOffset.UtcNow, ReferenceId: entity.Id));
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actor.Id, actor.Name, actor.Role,
                "إنشاء نسخة احتياطية لقاعدة البيانات", "backup", $"تم حفظ النسخة في {filePath} ({entity.FileSizeBytes} بايت)",
                "normal", entity.CompletedAtUtc.Value, ReferenceId: entity.Id));
            CopyToSecondPlace(actor, entity, filePath, fileName);
        }
        catch (Exception ex)
        {
            entity.Status = nameof(BackupRecordStatus.Failed);
            entity.ErrorMessage = ex.Message;
            entity.CompletedAtUtc = DateTimeOffset.UtcNow;
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actor.Id, actor.Name, actor.Role,
                "فشل إنشاء نسخة احتياطية لقاعدة البيانات", "backup", $"{filePath} - الخطأ: {ex.Message}",
                "critical", entity.CompletedAtUtc.Value, ReferenceId: entity.Id));
        }

        await _db.SaveChangesAsync(ct);
        return BackupQueryService.ToDto(entity);
    }

    /// <summary>
    /// Audit 2026-11-18: every backup was kept on the disk the database is on, so a failed disk, a
    /// theft or ransomware took the database and all sixty backups together. When
    /// <c>Backup:CopyToDirectory</c> is set (a second disk, a USB drive, a network share) each good
    /// backup is copied there too. A copy that fails leaves the backup itself standing and is written
    /// to the audit trail as critical, so the owner hears about it rather than discovering it on the day
    /// it was needed. Unset, nothing changes.
    /// </summary>
    private void CopyToSecondPlace(BackupActor actor, BackupRecordEntity entity, string filePath, string fileName)
    {
        if (_configuration["Backup:CopyToDirectory"] is not { Length: > 0 } copyDirectory) return;
        var copyPath = Path.Combine(copyDirectory, fileName);
        try
        {
            Directory.CreateDirectory(copyDirectory);
            File.Copy(filePath, copyPath, overwrite: false);
            if (File.Exists(BackupSchemaStamp.PathFor(filePath)))
                File.Copy(BackupSchemaStamp.PathFor(filePath), BackupSchemaStamp.PathFor(copyPath), overwrite: false);
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actor.Id, actor.Name, actor.Role,
                "نسخ النسخة الاحتياطية إلى المكان الثاني", "backup", $"تم نسخها إلى {copyPath}",
                "normal", DateTimeOffset.UtcNow, ReferenceId: entity.Id));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actor.Id, actor.Name, actor.Role,
                "فشل نسخ النسخة الاحتياطية إلى المكان الثاني", "backup",
                $"النسخة محفوظة في {filePath} لكن نسخها إلى {copyPath} فشل - الخطأ: {ex.Message}",
                "critical", DateTimeOffset.UtcNow, ReferenceId: entity.Id));
        }
    }
}
