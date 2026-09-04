namespace Falah.Infrastructure.BusinessDay;

using Falah.Application.BusinessDay;
using Falah.Domain.BusinessDay;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Mirrors OpenShiftHandler's shape exactly. **Real gap found and fixed (2026-09-04)**: this
/// handler had NO permission check at all until now - `openBusinessDay` in the reference app gates on
/// `business_day_open` (`authorizeCommand({actor,permission:'business_day_open'})`), and nothing in
/// this codebase's own history of this handler ever ported that check, discovered while wiring the
/// sibling close/reopen commands (Falah.Infrastructure.ControlCenter) right next to it.</summary>
public sealed class OpenBusinessDayHandler
{
    private readonly FalahDbContext _db;
    private readonly Falah.Domain.Authorization.IApprovalRegistry _approvalRegistry;
    public OpenBusinessDayHandler(FalahDbContext db, Falah.Domain.Authorization.IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<OpenBusinessDayResult> HandleAsync(OpenBusinessDayRequest request, CancellationToken ct = default)
    {
        var existing = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.OpenRequestId == request.RequestId, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.OpenedByUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        Falah.Domain.Authorization.AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, Falah.Infrastructure.Orders.OrderMapper.ToActor(actorUser), "business_day_open");

        var now = DateTimeOffset.UtcNow;
        var activeDayEntities = await _db.BusinessDays.AsNoTracking()
            .Where(d => d.Status == nameof(BusinessDayStatus.Open) || d.Status == nameof(BusinessDayStatus.Closing) || d.Status == nameof(BusinessDayStatus.Reopened))
            .ToListAsync(ct);
        var activeDays = activeDayEntities
            .Select(d => new BusinessDayRecord(d.Id, d.BusinessDate, d.OpenedBy, d.OpenedAtUtc, Enum.Parse<BusinessDayStatus>(d.Status), d.CloseRevision))
            .ToList();

        var businessDate = request.BusinessDate ?? BusinessClock.BusinessDate(now);
        var day = BusinessDayRules.Open($"day-{Guid.NewGuid():N}", businessDate, request.OpenedByUserId, now, activeDays);

        var entity = new BusinessDayEntity
        {
            Id = day.Id, BusinessDate = day.BusinessDate, OpenedBy = day.OpenedBy, OpenedAtUtc = day.OpenedAtUtc,
            Status = nameof(BusinessDayStatus.Open), CloseRevision = 0, OpenRequestId = request.RequestId,
        };

        try
        {
            _db.BusinessDays.Add(entity);
            await _db.SaveChangesAsync(ct);
            return ToResult(entity, wasReplayed: false);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.OpenRequestId == request.RequestId, ct);
            if (replay is not null) return ToResult(replay, wasReplayed: true);
            throw;
        }
    }

    private static OpenBusinessDayResult ToResult(BusinessDayEntity entity, bool wasReplayed) =>
        new(entity.Id, entity.BusinessDate.ToString("yyyy-MM-dd"), entity.Status, wasReplayed);
}
