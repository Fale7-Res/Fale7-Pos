namespace Falah.Infrastructure.Printing;

using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Printing;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Workflow redesign (owner decision 2026-11-24): the queue's "print kitchen invoice" action, for an order whose kitchen
/// paper never reached the printer (automatic printing switched off, or the paper failed). It is the order's FIRST
/// kitchen paper, not a reprint: the first papers it lacks are queued (CreatePrintJobsHandler only adds routes with no
/// first print) and a first paper that failed is sent again. Once one is printed the order counts as in the kitchen
/// (KitchenTicket) and cancelling it needs a manager. Pressing it again queues nothing new for a route already queued.
/// </summary>
public sealed class SendKitchenInvoiceHandler
{
    private readonly FalahDbContext _db;
    private readonly CreatePrintJobsHandler _createJobs;

    public SendKitchenInvoiceHandler(FalahDbContext db, CreatePrintJobsHandler createJobs)
    {
        _db = db;
        _createJobs = createJobs;
    }

    public async Task<int> HandleAsync(SendKitchenInvoiceRequest request, CancellationToken ct = default)
    {
        InputRules.AssertRequestId(request.RequestId);
        var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var who = OrderMapper.ToActor(actor);
        if (!AuthorizationPolicy.HasPermission(who, "pos") && !AuthorizationPolicy.HasPermission(who, "delivery_manage"))
            throw new DomainException("ليس لديك صلاحية طباعة ورقة المطبخ.");
        if (request.StationRoute is not (nameof(PrintRoute.Receipt) or nameof(PrintRoute.CallCenter)))
            throw new DomainException("مسار طباعة المحطة غير صالح.");

        var order = await _db.Orders.AsNoTracking().Where(o => o.Id == request.OrderId)
            .Select(o => new { o.Id, o.Status, o.OrderType }).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("الطلب غير موجود.");
        if (order.Status == "cancelled") throw new DomainException("الطلب ده اتلغى.");

        var failed = await _db.PrintJobs.Where(j => j.OrderId == order.Id && !j.IsReprint && j.Status == nameof(PrintJobStatus.Failed)
            && j.Route != nameof(PrintRoute.DriverDispatch) && j.DocumentKind != nameof(PrintDocumentKind.PickupReceipt)).ToListAsync(ct);
        foreach (var job in failed) { job.Status = nameof(PrintJobStatus.Pending); job.ClaimedBy = null; job.ClaimedUntilUtc = null; }
        if (failed.Count > 0) await _db.SaveChangesAsync(ct);

        var created = 0;
        try
        {
            var result = await _createJobs.HandleAsync(new CreatePrintJobsRequest(
                $"kitchen-{request.RequestId}", order.Id, request.StationRoute, IsReprint: false, request.ActorUserId, IncludeKitchenRoutes: true), ct);
            created = result.WasReplayed ? 0 : result.Jobs.Count(j => j.Status == nameof(PrintJobStatus.Pending));
        }
        catch (DomainException) { /* every first paper already exists */ }
        return failed.Count + created;
    }
}
