namespace Falah.Infrastructure.Printing;

using Falah.Application.Printing;
using Falah.Domain.Common;
using Falah.Domain.Printing;
using Falah.Domain.Financial;
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
        foreach (var route in new[] { "Receipt", "CallCenter", "Charcoal", "Grill", nameof(PrintRoute.DriverDispatch), nameof(PrintRoute.DeliveryOrder) })
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

        // A shift or expense slip describes no order, so it never touches the order composition below.
        if (job.DocumentKind is { } kindName && kindName != nameof(PrintDocumentKind.Order))
        {
            var slip = Enum.Parse<PrintDocumentKind>(kindName) switch
            {
                PrintDocumentKind.ShiftOpen => await ComposeShiftOpenSlipAsync(job, ct),
                PrintDocumentKind.ShiftClose => await ComposeShiftCloseSlipAsync(job, ct),
                PrintDocumentKind.Expense => await ComposeExpenseSlipAsync(job, ct),
                _ => throw new InvalidOperationException($"نوع مستند طباعة غير معروف: {kindName}"),
            };
            return ToDto(MarkIfReprint(slip, job));
        }

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
                PrintRules.ComposeKitchenTicket(route, (await _settings.GetAsync(ct)).RestaurantName, order.Id, order.OrderNumber,
                    orderType, ticketItems, order.CreatedAtUtc, order.RequestedPickupAtUtc),
            PrintRoute.Receipt or PrintRoute.CallCenter => await ComposeReceiptAsync(route, order, orderType, cashier?.Name ?? order.CashierId, ticketItems, ct),
            PrintRoute.DeliveryOrder => await ComposeDeliveryOrderAsync(order, orderType, cashier?.Name ?? order.CashierId, ticketItems, ct),
            PrintRoute.DriverDispatch => await ComposeDriverDispatchAsync(job, order, ct),
            _ => throw new InvalidOperationException($"مسار طباعة غير معروف: {route}"),
        };

        return ToDto(MarkIfReprint(composed, job));
    }

    private static ComposedTicketDto ToDto(ComposedTicket composed) =>
        new(composed.Route.ToString(), composed.OrderId, composed.OrderNumber,
            composed.Lines.Select(TicketElementMapping.ToDto).ToList());

    /// <summary>Paper produced by a reprint says so, on every piece it produces. Applied here rather
    /// than inside each composition so no route can be reproduced silently - and applied at compose
    /// time, so a reprint job that is retried by the agent keeps saying it is a reprint.</summary>
    private static ComposedTicket MarkIfReprint(ComposedTicket composed, PrintJobEntity job) =>
        job.IsReprint ? PrintRules.MarkAsReprint(composed, job.CreatedAtUtc, job.CreatedBy) : composed;

    /// <summary>Shared by both shift slips: who the shift belongs to, and the short code that lets
    /// its opening and closing papers be matched without printing an internal id.</summary>
    private async Task<(ShiftEntity Shift, ShiftSlipIdentity Identity)> LoadShiftIdentityAsync(string shiftId, CancellationToken ct)
    {
        var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shiftId, ct)
            ?? throw new InvalidOperationException("الوردية غير موجودة.");
        var cashier = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == shift.OpenedByUserId, ct);
        var settings = await _settings.GetAsync(ct);
        return (shift, new ShiftSlipIdentity(settings.RestaurantName, settings.SubName, ShiftReference(shift.Id),
            shift.ShiftName, cashier?.Name ?? shift.OpenedByUserId, shift.OpenedAtUtc));
    }

    /// <summary>The last six characters of the shift id, upper-cased: short enough to read out, and
    /// unique enough to match two slips of the same shift against each other.</summary>
    internal static string ShiftReference(string shiftId) =>
        shiftId.Length <= 6 ? shiftId.ToUpperInvariant() : shiftId[^6..].ToUpperInvariant();

    private async Task<ComposedTicket> ComposeShiftOpenSlipAsync(PrintJobEntity job, CancellationToken ct)
    {
        var (shift, identity) = await LoadShiftIdentityAsync(job.OrderId, ct);
        return PrintRules.ComposeShiftOpenTicket(identity, Money.FromPiastres(shift.OpeningCashPiastres));
    }

    /// <summary>
    /// The handover slip states what the close itself persisted - the expected figure, the count and
    /// the difference are read from the shift row, never recomputed here, so a slip reprinted later
    /// cannot disagree with the close it describes. Only the breakdown that explains how the expected
    /// figure was reached is recomputed, from the same reconciliation the close used.
    /// </summary>
    private async Task<ComposedTicket> ComposeShiftCloseSlipAsync(PrintJobEntity job, CancellationToken ct)
    {
        var (shift, identity) = await LoadShiftIdentityAsync(job.OrderId, ct);
        var opening = Money.FromPiastres(shift.OpeningCashPiastres);
        var breakdown = await Falah.Infrastructure.Shifts.ShiftReconciliation.ComputeBreakdownAsync(_db, shift.Id, opening, ct);
        var actual = Money.FromPiastres(shift.ActualCashPiastres ?? 0);
        var expected = Money.FromPiastres(shift.ExpectedCashPiastres ?? breakdown.ExpectedCash.Piastres);
        breakdown = breakdown with
        {
            ExpectedCash = expected,
            ActualCash = actual,
            Difference = Money.FromPiastres(shift.CashDifferencePiastres ?? (actual - expected).Piastres),
        };

        var expenses = (await _db.Expenses.AsNoTracking()
                .Where(e => e.ShiftId == shift.Id && e.PaidFromDrawer)
                .Select(e => new { e.Category, e.AmountPiastres }).ToListAsync(ct))
            .GroupBy(e => e.Category)
            .Select(g => new ExpenseSummaryLine(g.Key, Money.FromPiastres(g.Sum(e => e.AmountPiastres))))
            .OrderByDescending(l => l.Amount.Piastres).ToList();

        return PrintRules.ComposeShiftCloseTicket(identity, shift.ClosedAtUtc ?? job.CreatedAtUtc, breakdown,
            expenses, await LoadCloseWarningsAsync(shift, ct), shift.DifferenceReason);
    }

    /// <summary>What is still unfinished as this shift is handed over. Read-only: the close itself
    /// is never blocked by this - the slip states it so the money is counted with it in view.</summary>
    private async Task<List<string>> LoadCloseWarningsAsync(ShiftEntity shift, CancellationToken ct)
    {
        var warnings = new List<string>();

        var pendingElectronic = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.ShiftId == shift.Id && o.ElectronicPaymentStatus == "pending", ct);
        if (pendingElectronic > 0) warnings.Add($"طلبات بانتظار تأكيد تحويل إلكتروني: {pendingElectronic}");

        var unsettled = await _db.DriverCustodies.AsNoTracking()
            .Where(c => c.Status == "Open").Select(c => c.ExpectedPiastres).ToListAsync(ct);
        if (unsettled.Count > 0)
            warnings.Add($"عهدة سائقين غير مسواة: {unsettled.Count} بقيمة {Money.FromPiastres(unsettled.Sum()).Egp} ج");

        var unpaidOrders = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.ShiftId == shift.Id && !o.IsPaid && o.Status != "cancelled" && o.ElectronicPaymentStatus == null, ct);
        if (unpaidOrders > 0) warnings.Add($"طلبات غير مدفوعة: {unpaidOrders}");

        return warnings;
    }

    private async Task<ComposedTicket> ComposeExpenseSlipAsync(PrintJobEntity job, CancellationToken ct)
    {
        var expense = await _db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == job.OrderId, ct)
            ?? throw new InvalidOperationException("المصروف غير موجود.");
        var createdBy = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == expense.CreatedByUserId, ct);
        var settings = await _settings.GetAsync(ct);
        return PrintRules.ComposeExpenseTicket(new ExpenseSlipInfo(
            settings.RestaurantName, settings.SubName, expense.Id, expense.CreatedAtUtc,
            Money.FromPiastres(expense.AmountPiastres), expense.Category, expense.Description,
            createdBy?.Name ?? expense.CreatedByUserId, DescribePaymentAccount(expense.PaymentAccountId),
            expense.ApprovedBy is { } approver ? $"اعتماد: {approver}" : null));
    }

    /// <summary>The account an expense was paid from, in the words the Treasury screen uses.</summary>
    private static string DescribePaymentAccount(string? accountId) => accountId switch
    {
        TreasuryAccounts.Drawer => "درج الكاشير",
        TreasuryAccounts.Main => "الخزينة الرئيسية",
        TreasuryAccounts.Bank => "البنك",
        TreasuryAccounts.InstaPay => "إنستاباي",
        null or "" => "غير محدد",
        var other => other,
    };

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
            Footer: settings.ReceiptFooter,
            RequestedPickupAtUtc: order.RequestedPickupAtUtc,
            MenuUrl: settings.MenuUrl,
            IsPaid: order.IsPaid);
        var totals = new ReceiptTotals(
            Money.FromPiastres(order.SubtotalPiastres), Money.FromPiastres(order.DiscountPiastres),
            Money.FromPiastres(order.TaxAmountPiastres), order.TaxRuleName, Money.FromPiastres(order.DeliveryFeePiastres),
            Money.FromPiastres(order.CompAmountPiastres), Money.FromPiastres(order.TotalPiastres),
            Money.FromPiastres(order.AmountPaidPiastres), Money.FromPiastres(order.ChangeDuePiastres),
            OrderEnumMapping.ParsePaymentMethod(order.PaymentMethod));
        return PrintRules.ComposeReceiptTicket(route, header, ticketItems, totals);
    }

    /// <summary>The delivery ORDER ticket, composed from the same frozen order snapshot the receipt
    /// reads - plus the customer phone and the two note fields the receipt never printed. Whether
    /// this is cash-on-delivery is decided by CheckoutRules.IsCashOnDelivery, the same function
    /// checkout itself used, never re-expressed here.</summary>
    private async Task<ComposedTicket> ComposeDeliveryOrderAsync(
        OrderEntity order, OrderType orderType, string cashierName, IReadOnlyList<TicketItem> ticketItems, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        var paymentMethod = OrderEnumMapping.ParsePaymentMethod(order.PaymentMethod);
        var header = new ReceiptHeader(
            order.Id, settings.RestaurantName, settings.SubName, settings.Address, order.OrderNumber, order.CreatedAtUtc,
            cashierName, orderType, order.TableNumber, order.CustomerName, order.DeliveryAddress, order.DeliveryArea,
            settings.Phone1, settings.Phone2, settings.ComplaintsPhone,
            ElectronicPaymentPending: order.ElectronicPaymentStatus == "pending",
            Footer: settings.ReceiptFooter,
            RequestedPickupAtUtc: order.RequestedPickupAtUtc);
        var totals = new ReceiptTotals(
            Money.FromPiastres(order.SubtotalPiastres), Money.FromPiastres(order.DiscountPiastres),
            Money.FromPiastres(order.TaxAmountPiastres), order.TaxRuleName, Money.FromPiastres(order.DeliveryFeePiastres),
            Money.FromPiastres(order.CompAmountPiastres), Money.FromPiastres(order.TotalPiastres),
            Money.FromPiastres(order.AmountPaidPiastres), Money.FromPiastres(order.ChangeDuePiastres), paymentMethod);

        var outstanding = Money.FromPiastres(Math.Max(0, order.TotalPiastres - order.AmountPaidPiastres));
        var info = new DeliveryOrderTicketInfo(
            order.CustomerPhone, order.DeliveryNotes, outstanding,
            CheckoutRules.IsCashOnDelivery(orderType, paymentMethod, Money.FromPiastres(order.TotalPiastres)));

        return PrintRules.ComposeDeliveryOrderTicket(header, ticketItems, totals, info);
    }

    /// <summary>Loads the delivery/driver facts the customer-facing receipt never needed and never
    /// carried. isReassignment is detected, not stored: true whenever an EARLIER DriverDispatch job
    /// already exists for this order - the first dispatch is never marked, every one after it is.</summary>
    private async Task<ComposedTicket> ComposeDriverDispatchAsync(PrintJobEntity job, OrderEntity order, CancellationToken ct)
    {
        var delivery = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.OrderId == order.Id, ct)
            ?? throw new InvalidOperationException("طلب الدليفري المرتبط بهذا الطلب غير موجود.");
        var driverName = delivery.DriverNameAtHandover
            ?? (delivery.DriverId is { } driverId ? await _db.Drivers.AsNoTracking().Where(d => d.Id == driverId).Select(d => d.Name).FirstOrDefaultAsync(ct) : null)
            ?? "غير معروف";

        // A queued dispatch belongs to its assignment, not whichever driver owns the
        // delivery when the printer finally drains. Reassignment history retains that name.
        var nextReassignment = await _db.DeliveryReassignments.AsNoTracking()
            .Where(h => h.DeliveryId == delivery.Id && h.ChangedAtUtc > job.CreatedAtUtc)
            .OrderBy(h => h.ChangedAtUtc).FirstOrDefaultAsync(ct);
        if (nextReassignment?.OldDriverName is { } originalDriverName)
            driverName = originalDriverName;

        var earlierJobExists = await _db.PrintJobs.AsNoTracking()
            .AnyAsync(j => j.OrderId == order.Id && j.Route == nameof(PrintRoute.DriverDispatch) && j.CreatedAtUtc < job.CreatedAtUtc, ct);

        // The SAME rule checkout used and the same one the delivery order ticket is given. Derived
        // here rather than re-expressed, so the two delivery tickets for one order can never
        // disagree about whether the driver is collecting money.
        var settings = await _settings.GetAsync(ct);
        var orderType = OrderEnumMapping.ParseOrderType(order.OrderType);
        var paymentMethod = OrderEnumMapping.ParsePaymentMethod(order.PaymentMethod);
        var isCashOnDelivery = CheckoutRules.IsCashOnDelivery(orderType, paymentMethod, Money.FromPiastres(order.TotalPiastres));
        var amountToCollect = Money.FromPiastres(Math.Max(0, order.TotalPiastres - order.AmountPaidPiastres));
        return PrintRules.ComposeDriverDispatchTicket(
            settings.RestaurantName, order.Id, order.OrderNumber, driverName, order.CustomerName, order.CustomerPhone,
            order.DeliveryAddress, order.DeliveryArea, amountToCollect, isCashOnDelivery,
            electronicPaymentPending: order.ElectronicPaymentStatus == "pending",
            order.DeliveryNotes, job.CreatedAtUtc, earlierJobExists);
    }
}
