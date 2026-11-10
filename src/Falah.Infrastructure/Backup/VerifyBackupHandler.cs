namespace Falah.Infrastructure.Backup;

using Falah.Application.Backup;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Runs SQL Server's own `RESTORE VERIFYONLY` against a backup file - checks the backup
/// set's structure and checksum (since CreateBackupHandler always writes WITH CHECKSUM) without
/// actually restoring anything, so this is always safe to run against a live system regardless of
/// what else is happening. This is the "verification/status" the requirements ask for, done the
/// SQL-Server-native way rather than a hand-rolled checksum like the reference app's own
/// FNV-1a32-over-JSON scheme.</summary>
public sealed class VerifyBackupHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public VerifyBackupHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<BackupRecordDto> HandleAsync(VerifyBackupRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "backup_manage");

        return await RunAsync(new BackupActor(actorUser.Id, actorUser.Name, actorUser.Role), request.BackupRecordId, ct);
    }

    /// <summary>The verification itself, with no permission check in front of it - see
    /// CreateBackupHandler.RunAsync for why the check sits in the operator path above rather than
    /// here. A scheduled backup verifies itself immediately: a .bak nobody has ever read back is a
    /// promise, not a backup.</summary>
    public async Task<BackupRecordDto> RunAsync(BackupActor actor, string backupRecordId, CancellationToken ct = default)
    {
        var entity = await _db.BackupRecords.FirstOrDefaultAsync(b => b.Id == backupRecordId, ct)
            ?? throw new InvalidOperationException("سجل النسخة الاحتياطية غير موجود.");
        if (!File.Exists(entity.FilePath))
            throw new Falah.Domain.Common.DomainException("ملف النسخة الاحتياطية غير موجود على القرص.");

        var now = DateTimeOffset.UtcNow;
        try
        {
            await _db.Database.ExecuteSqlRawAsync("RESTORE VERIFYONLY FROM DISK = {0}", new object[] { entity.FilePath }, ct);
            entity.VerificationStatus = "Valid";
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actor.Id, actor.Name, actor.Role,
                "التحقق من نسخة احتياطية", "backup", $"النسخة صالحة: {entity.FilePath}", "normal", now, ReferenceId: entity.Id));
        }
        catch (Exception ex)
        {
            entity.VerificationStatus = "Invalid";
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actor.Id, actor.Name, actor.Role,
                "فشل التحقق من نسخة احتياطية", "backup", $"{entity.FilePath} - الخطأ: {ex.Message}", "critical", now, ReferenceId: entity.Id));
        }
        entity.VerifiedAtUtc = now;

        await _db.SaveChangesAsync(ct);
        return BackupQueryService.ToDto(entity);
    }
}
