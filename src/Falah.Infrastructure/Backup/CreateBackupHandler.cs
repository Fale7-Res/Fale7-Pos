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

    public CreateBackupHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog, IConfiguration configuration)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
        _configuration = configuration;
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
        var now = DateTimeOffset.UtcNow;
        var fileName = $"falah-{databaseName}-{now:yyyyMMdd-HHmmss}.bak";
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
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actor.Id, actor.Name, actor.Role,
                "إنشاء نسخة احتياطية لقاعدة البيانات", "backup", $"تم حفظ النسخة في {filePath} ({entity.FileSizeBytes} بايت)",
                "normal", entity.CompletedAtUtc.Value, ReferenceId: entity.Id));
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
}
