namespace Falah.Infrastructure.Printing;

using Falah.Application.Printing;
using Falah.Domain.Audit;
using Falah.Domain.Common;
using Falah.Domain.Orders;
using Falah.Domain.Printing;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Creates one PrintJob per ticket an order needs: at most one customer-facing job (Receipt or
/// CallCenter, whichever the calling station explicitly asked for) plus one job per kitchen station
/// the order's own items actually touch (never requested - always derived from
/// PrintRules.DetermineKitchenRoutes, reading each item's own FROZEN OrderItem.PreparationStation -
/// the station that was authoritative at sale time, never the product's current live configuration,
/// so a later menu change can never alter how an already-committed order routes or reprints). Reads
/// the order but never writes to it, never touches Payment/Inventory/Treasury - printing is completely
/// decoupled from financial truth by construction, not by convention. Idempotent: job ids are
/// deterministic from (RequestId, Route), so retrying the exact same request cannot create a second
/// set of tickets for the same submission; a genuine reprint mints a NEW RequestId and is marked
/// IsReprint, producing a fresh, independent set of jobs without touching the order at all.
/// </summary>
public sealed class CreatePrintJobsHandler
{
    private readonly FalahDbContext _db;
    private readonly IAuditLog _auditLog;
    public CreatePrintJobsHandler(FalahDbContext db, IAuditLog auditLog) { _db = db; _auditLog = auditLog; }

    public async Task<CreatePrintJobsResult> HandleAsync(CreatePrintJobsRequest request, CancellationToken ct = default)
    {
        var existing = await _db.PrintJobs.AsNoTracking()
            .Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
        if (existing.Count > 0) return new CreatePrintJobsResult(existing.Select(ToDto).ToList(), WasReplayed: true);

        PrintRoute? customerRoute = null;
        if (!string.IsNullOrEmpty(request.CustomerRoute))
        {
            if (!Enum.TryParse<PrintRoute>(request.CustomerRoute, out var parsed) || parsed is not (PrintRoute.Receipt or PrintRoute.CallCenter))
                throw new DomainException("مسار الطباعة المطلوب للعميل غير صالح.");
            customerRoute = parsed;
        }

        var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new InvalidOperationException("الطلب غير موجود.");
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");

        IReadOnlyCollection<PrintRoute> kitchenRoutes = request.IncludeKitchenRoutes
            ? PrintRules.DetermineKitchenRoutes(order.Items.Select(i => i.PreparationStation))
            : Array.Empty<PrintRoute>();

        var routes = new List<PrintRoute>(kitchenRoutes);
        if (customerRoute is { } cr) routes.Insert(0, cr);
        if (routes.Count == 0) throw new DomainException("لا توجد محطة طباعة مطلوبة لهذا الطلب.");

        var now = DateTimeOffset.UtcNow;
        var jobs = routes.Select(route => new PrintJobEntity
        {
            Id = $"printjob-{request.RequestId}-{route}", CreateRequestId = request.RequestId, OrderId = order.Id, OrderNumber = order.OrderNumber,
            Route = route.ToString(), Status = nameof(PrintJobStatus.Pending), AttemptCount = 0,
            IsReprint = request.IsReprint, CreatedBy = actorUser.Name, CreatedAtUtc = now,
        }).ToList();

        try
        {
            _db.PrintJobs.AddRange(jobs);
            // Only a genuine REPRINT is audited here - a routine first print of a fresh order is
            // not a sensitive event (PrintJobEntity/PrintJobAttemptEntity already carry its own full
            // attempt history); reproducing an already-committed order's tickets later is the
            // noteworthy action worth surfacing in the general trail too.
            if (request.IsReprint)
            {
                _auditLog.Append(new AuditEvent(
                    $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                    "إعادة طباعة طلب", "order", $"إعادة طباعة الطلب {order.OrderNumber} - المسارات: {string.Join(", ", routes)}",
                    "normal", now, ReferenceId: order.Id));
            }
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.PrintJobs.AsNoTracking().Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
            if (replay.Count > 0) return new CreatePrintJobsResult(replay.Select(ToDto).ToList(), WasReplayed: true);
            throw new PrintingConflictException("تعذر إنشاء طلبات الطباعة بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }

        return new CreatePrintJobsResult(jobs.Select(ToDto).ToList(), WasReplayed: false);
    }

    internal static PrintJobDto ToDto(PrintJobEntity j) => new(
        j.Id, j.OrderId, j.OrderNumber, j.Route, j.Status, j.AttemptCount, j.LastAttemptAtUtc, j.LastError, j.IsReprint, j.CreatedAtUtc);
}
