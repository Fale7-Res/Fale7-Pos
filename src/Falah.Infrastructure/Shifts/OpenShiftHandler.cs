namespace Falah.Infrastructure.Shifts;

using Falah.Application.Shifts;
using Falah.Domain.Common;
using Falah.Domain.Shifts;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Minimal shift-open orchestration - just enough for the Checkout vertical slice to
/// have a real, persisted, open shift to reference. Blind-close, discrepancy approval, and any
/// "only one active shift" constraint are deliberately out of scope for this slice; see
/// docs/PROGRESS.md.</summary>
public sealed class OpenShiftHandler
{
    private readonly FalahDbContext _db;
    public OpenShiftHandler(FalahDbContext db) => _db = db;

    public async Task<OpenShiftResult> HandleAsync(OpenShiftRequest request, CancellationToken ct = default)
    {
        var existing = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.OpenRequestId == request.RequestId, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        var now = DateTimeOffset.UtcNow;
        var shift = Shift.Open($"shift-{Guid.NewGuid():N}", request.ShiftName, request.OpenedByUserId,
            Money.FromPiastres(request.OpeningCashPiastres), now);

        var entity = new ShiftEntity
        {
            Id = shift.Id,
            ShiftName = shift.ShiftName,
            OpenedByUserId = shift.OpenedByUserId,
            OpeningCashPiastres = shift.OpeningCash.Piastres,
            Status = nameof(ShiftStatus.Open),
            BusinessDate = shift.BusinessDate,
            OpenedAtUtc = shift.OpenedAtUtc,
            OpenRequestId = request.RequestId,
        };

        try
        {
            _db.Shifts.Add(entity);
            await _db.SaveChangesAsync(ct);
            return ToResult(entity, wasReplayed: false);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.OpenRequestId == request.RequestId, ct);
            if (replay is not null) return ToResult(replay, wasReplayed: true);
            throw;
        }
    }

    private static OpenShiftResult ToResult(ShiftEntity entity, bool wasReplayed) =>
        new(entity.Id, entity.ShiftName, entity.Status, entity.OpeningCashPiastres, wasReplayed);
}
