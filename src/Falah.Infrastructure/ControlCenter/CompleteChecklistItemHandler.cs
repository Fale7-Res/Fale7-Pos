namespace Falah.Infrastructure.ControlCenter;

using Falah.Application.BusinessDay;
using Falah.Domain.Authorization;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from completeChecklistItem - a plain idempotent-by-nature toggle (no RequestId,
/// same as the reference app: completing the same item twice just re-upserts one row), unlike every
/// financial command in this codebase which needs an explicit idempotency key.</summary>
public sealed class CompleteChecklistItemHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public CompleteChecklistItemHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task HandleAsync(CompleteChecklistItemRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "business_day_close");

        var item = await _db.ChecklistItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.ItemId, ct)
            ?? throw new InvalidOperationException("بند القائمة غير موجود.");
        var day = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.BusinessDayId, ct)
            ?? throw new InvalidOperationException("يوم العمل غير موجود.");

        var now = DateTimeOffset.UtcNow;
        var existing = await _db.ChecklistCompletions.FirstOrDefaultAsync(c => c.BusinessDayId == day.Id && c.ItemId == item.Id, ct);
        if (existing is null)
        {
            _db.ChecklistCompletions.Add(new ChecklistCompletionEntity
            {
                BusinessDayId = day.Id, ItemId = item.Id, Completed = request.Completed,
                CompletedAtUtc = request.Completed ? now : null, CompletedBy = request.Completed ? actorUser.Name : null,
            });
        }
        else
        {
            existing.Completed = request.Completed;
            existing.CompletedAtUtc = request.Completed ? now : null;
            existing.CompletedBy = request.Completed ? actorUser.Name : null;
        }
        await _db.SaveChangesAsync(ct);
    }
}
