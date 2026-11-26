namespace Falah.Infrastructure.Orders;

using Falah.Application.Orders;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Owner, 2026-11-25 («ترحيل»): at the end of a shift the queue's screen is cleared for the next one. The clear is one
/// audit row per kind (delivery, pickup) - who and when - and OrderQueueQueryService hides every order of that kind taken
/// up to that moment once its cycle has finished. Nothing is deleted or changed: orders, reports, driver accounts and
/// money stay exactly as they were. Replays by RequestId before anything is validated.
/// </summary>
public sealed class ClearOrderQueueHandler
{
    private readonly FalahDbContext _db;
    private readonly IAuditLog _auditLog;
    private readonly OrderQueueQueryService _queue;

    public ClearOrderQueueHandler(FalahDbContext db, IAuditLog auditLog, OrderQueueQueryService queue)
    {
        _db = db;
        _auditLog = auditLog;
        _queue = queue;
    }

    private static string AuditId(string requestId, string kind) => $"audit-queueclear-{kind}-{requestId}";

    public async Task<ClearOrderQueueResult> HandleAsync(ClearOrderQueueRequest request, CancellationToken ct = default)
    {
        InputRules.AssertRequestId(request.RequestId);
        if (await ReplayAsync(request.RequestId, ct) is { } replay) return replay;

        if (!request.Deliveries && !request.Pickups) throw new DomainException("اختار التوصيل أو الاستلام من الفرع.");
        var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actorPolicy = OrderMapper.ToActor(actor);
        if (!AuthorizationPolicy.HasPermission(actorPolicy, "pos") && !AuthorizationPolicy.HasPermission(actorPolicy, "delivery_manage"))
            throw new DomainException("ليس لديك صلاحية ترحيل الطابور.");

        var kinds = new List<string>();
        if (request.Deliveries) kinds.Add("delivery");
        if (request.Pickups) kinds.Add("pickup");

        var shown = (await _queue.ListAsync(ct)).Where(q => kinds.Contains(q.OrderType)).ToList();
        var removed = shown.Count(OrderQueueQueryService.IsFinished);
        var kept = shown.Count - removed;

        var now = DateTimeOffset.UtcNow;
        foreach (var kind in kinds)
        {
            var label = kind == "delivery" ? "التوصيل" : "الاستلام من الفرع";
            _auditLog.Append(new AuditEvent(
                AuditId(request.RequestId, kind), actor.Id, actor.Name, actor.Role,
                $"ترحيل طابور {label}", OrderQueueQueryService.ClearCategory,
                $"اتشال من الواجهة {removed} طلب خلص، وفضل {kept} لسه شغال (الطلبات ما اتمسحتش)",
                "normal", now, NewValue: $"{removed}|{kept}", OperationId: request.RequestId, ReferenceId: kind));
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            if (await ReplayAsync(request.RequestId, ct) is { } conflictReplay) return conflictReplay;
            throw;
        }
        return new ClearOrderQueueResult(removed, kept, false);
    }

    private async Task<ClearOrderQueueResult?> ReplayAsync(string requestId, CancellationToken ct)
    {
        var saved = await _db.AuditEvents.AsNoTracking()
            .Where(a => a.Category == OrderQueueQueryService.ClearCategory && a.OperationId == requestId)
            .Select(a => a.NewValue).FirstOrDefaultAsync(ct);
        if (saved is null) return null;
        var parts = saved.Split('|');
        return new ClearOrderQueueResult(int.Parse(parts[0]), int.Parse(parts[1]), true);
    }
}
