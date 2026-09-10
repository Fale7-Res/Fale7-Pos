namespace Falah.Infrastructure.Printing;

using Falah.Application.Printing;
using Falah.Domain.Common;
using Falah.Domain.Printing;
using Falah.Domain.Orders;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;

/// <summary>Read-only projections over PrintJob/PrintJobAttempt - "target printer/station, order
/// number, attempt time, success/failure, retry/reprint count, last error" is exactly what these
/// expose. ComposeTicketContentAsync is the one place job tracking meets ticket content: it composes
/// (via the same pure Domain functions PrintRules already tests) what the job's own route/order
/// actually needs to print - a job never stores its own rendered content, so this is recomposed on
/// every read/reprint from the order's own FROZEN item data (ProductName, UnitPrice,
/// PreparationStation), never from the product's current live configuration.</summary>
public sealed class PrintJobQueryService
{
    private readonly FalahDbContext _db;
    private readonly SettingsQueryService _settings;
    public PrintJobQueryService(FalahDbContext db, SettingsQueryService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<List<PrintJobDto>> ListJobsForOrderAsync(string orderId, CancellationToken ct = default) =>
        (await _db.PrintJobs.AsNoTracking().Where(j => j.OrderId == orderId).OrderBy(j => j.CreatedAtUtc).ToListAsync(ct))
            .Select(CreatePrintJobsHandler.ToDto).ToList();

    /// <summary>What a station's own print agent polls: "give me my pending work." Ordered oldest
    /// first so a backlog drains in submission order.</summary>
    public async Task<List<PrintJobDto>> ListPendingJobsForRouteAsync(string route, CancellationToken ct = default) =>
        (await _db.PrintJobs.AsNoTracking()
            .Where(j => j.Route == route && j.Status == nameof(PrintJobStatus.Pending))
            .OrderBy(j => j.CreatedAtUtc).ToListAsync(ct))
            .Select(CreatePrintJobsHandler.ToDto).ToList();

    /// <summary>How long a Pending job has to sit before it means the agent is not draining the
    /// queue rather than that the job is new. Not a policy about printing - just long enough that
    /// no healthy station would still be holding it.</summary>
    private static readonly TimeSpan StuckAfter = TimeSpan.FromMinutes(5);

    /// <summary>The whole printing picture in one read - see PrintingOverviewDto.</summary>
    public async Task<PrintingOverviewDto> GetOverviewAsync(CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-2);
        var jobs = await _db.PrintJobs.AsNoTracking()
            .Where(j => j.Status != nameof(PrintJobStatus.Succeeded) || j.LastAttemptAtUtc >= since)
            .ToListAsync(ct);

        var stuckBefore = DateTimeOffset.UtcNow - StuckAfter;
        var routes = new List<PrintRouteStatusDto>();
        foreach (var route in new[] { "Receipt", "CallCenter", "Charcoal", "Grill" })
        {
            var forRoute = jobs.Where(j => j.Route == route).ToList();
            var pending = forRoute.Where(j => j.Status == nameof(PrintJobStatus.Pending)).ToList();
            routes.Add(new PrintRouteStatusDto(
                route,
                pending.Count,
                forRoute.Count(j => j.Status == nameof(PrintJobStatus.Failed)),
                pending.Count(j => j.CreatedAtUtc <= stuckBefore),
                pending.Count == 0 ? null : pending.Min(j => j.CreatedAtUtc),
                forRoute.Where(j => j.Status == nameof(PrintJobStatus.Succeeded))
                    .Select(j => j.LastAttemptAtUtc).DefaultIfEmpty(null).Max()));
        }

        // Failures first, then whatever has been waiting longest: that is the order somebody
        // working through this screen would pick them up in anyway.
        var attention = jobs
            .Where(j => j.Status == nameof(PrintJobStatus.Failed)
                        || (j.Status == nameof(PrintJobStatus.Pending) && j.CreatedAtUtc <= stuckBefore))
            .OrderBy(j => j.Status == nameof(PrintJobStatus.Failed) ? 0 : 1)
            .ThenBy(j => j.CreatedAtUtc)
            .Take(60)
            .Select(CreatePrintJobsHandler.ToDto)
            .ToList();

        return new PrintingOverviewDto(
            routes, attention,
            routes.Sum(r => r.PendingCount), routes.Sum(r => r.FailedCount), routes.Sum(r => r.StuckCount));
    }

    public async Task<PrintJobDetailsDto> GetJobDetailsAsync(string jobId, CancellationToken ct = default)
    {
        var job = await _db.PrintJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new InvalidOperationException("طلب الطباعة غير موجود.");
        var attempts = await _db.PrintJobAttempts.AsNoTracking().Where(a => a.PrintJobId == jobId)
            .OrderBy(a => a.AttemptedAtUtc)
            .Select(a => new PrintJobAttemptDto(a.AttemptedAtUtc, a.Succeeded, a.Error, a.ReportedBy))
            .ToListAsync(ct);
        return new PrintJobDetailsDto(CreatePrintJobsHandler.ToDto(job), attempts);
    }

    public async Task<ComposedTicketDto> ComposeTicketContentAsync(string jobId, CancellationToken ct = default)
    {
        var job = await _db.PrintJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new InvalidOperationException("طلب الطباعة غير موجود.");
        var route = Enum.Parse<PrintRoute>(job.Route);

        var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == job.OrderId, ct)
            ?? throw new InvalidOperationException("الطلب المرتبط بطلب الطباعة غير موجود.");
        var cashier = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == order.CashierId, ct);

        var ticketItems = order.Items.Select(i => new TicketItem(
            i.ProductName, i.VariantName, i.Quantity, Money.FromPiastres(i.UnitPricePiastres), Money.FromPiastres(i.TotalPricePiastres),
            i.Notes, i.PriceRuleName, i.ListUnitPricePiastres > 0 ? Money.FromPiastres(i.ListUnitPricePiastres) : null,
            i.PreparationStation)).ToList();

        var orderType = OrderEnumMapping.ParseOrderType(order.OrderType);
        var composed = route switch
        {
            PrintRoute.Charcoal or PrintRoute.Grill =>
                PrintRules.ComposeKitchenTicket(route, order.Id, order.OrderNumber, orderType, null, ticketItems, order.CreatedAtUtc),
            PrintRoute.Receipt or PrintRoute.CallCenter => await ComposeReceiptAsync(route, order, orderType, cashier?.Name ?? order.CashierId, ticketItems, ct),
            _ => throw new InvalidOperationException($"مسار طباعة غير معروف: {route}"),
        };

        return new ComposedTicketDto(composed.Route.ToString(), composed.OrderId, composed.OrderNumber,
            composed.Lines.Select(l => new TicketLineDto(l.Text, l.Bold, l.Centered, l.LargeFont)).ToList());
    }

    /// <summary>Restaurant identity, phones, and footer now come from the real Settings singleton
    /// (see SettingsEntity's own doc comment) rather than the hardcoded placeholders this query
    /// used before the legacy-parity audit closed that gap. The "⚠ غير مدفوع" pending-electronic
    /// banner is resolved from the order's own frozen ElectronicPaymentStatus, never recomputed
    /// from live data.</summary>
    private async Task<ComposedTicket> ComposeReceiptAsync(
        PrintRoute route, OrderEntity order, OrderType orderType, string cashierName,
        IReadOnlyList<TicketItem> ticketItems, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        var header = new ReceiptHeader(
            order.Id, settings.RestaurantName, settings.SubName, settings.Address, order.OrderNumber, order.CreatedAtUtc,
            cashierName, orderType, order.TableNumber, order.CustomerName, order.DeliveryAddress, order.DeliveryArea,
            settings.Phone1, settings.Phone2, settings.ComplaintsPhone,
            ElectronicPaymentPending: order.ElectronicPaymentStatus == "pending",
            Footer: settings.ReceiptFooter);
        var totals = new ReceiptTotals(
            Money.FromPiastres(order.SubtotalPiastres), Money.FromPiastres(order.DiscountPiastres),
            Money.FromPiastres(order.TaxAmountPiastres), order.TaxRuleName, Money.FromPiastres(order.DeliveryFeePiastres),
            Money.FromPiastres(order.CompAmountPiastres), Money.FromPiastres(order.TotalPiastres),
            Money.FromPiastres(order.AmountPaidPiastres), Money.FromPiastres(order.ChangeDuePiastres),
            OrderEnumMapping.ParsePaymentMethod(order.PaymentMethod));
        return PrintRules.ComposeReceiptTicket(route, header, ticketItems, totals);
    }
}
