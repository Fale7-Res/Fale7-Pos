namespace Falah.Infrastructure.Backup;

using Falah.Application.Backup;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class BackupQueryService
{
    private readonly FalahDbContext _db;
    public BackupQueryService(FalahDbContext db) => _db = db;

    public Task<List<BackupRecordDto>> ListAsync(CancellationToken ct = default) =>
        _db.BackupRecords.AsNoTracking().OrderByDescending(b => b.RequestedAtUtc).Select(b => ToDto(b)).ToListAsync(ct);

    public async Task<BackupRecordDto> GetAsync(string id, CancellationToken ct = default) =>
        ToDto(await _db.BackupRecords.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new InvalidOperationException("سجل النسخة الاحتياطية غير موجود."));

    internal static BackupRecordDto ToDto(BackupRecordEntity e) => new(
        e.Id, e.Kind, e.FilePath, e.Status, e.FileSizeBytes, e.VerificationStatus, e.VerifiedAtUtc,
        e.RequestedBy, e.RequestedAtUtc, e.CompletedAtUtc, e.ErrorMessage, e.Reason, e.ApprovedBy);
}
