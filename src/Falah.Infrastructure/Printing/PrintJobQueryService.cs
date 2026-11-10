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
    private readonly Falah.Infrastructure.Reports.ReportingQueryService _reporting;
    private readonly Falah.Infrastructure.Delivery.DeliveryQueryService _delivery;
    public PrintJobQueryService(FalahDbContext db, SettingsQueryService settings, Falah.Infrastructure.Reports.ReportingQueryService reporting,
        Falah.Infrastructure.Delivery.DeliveryQueryService delivery)
    {
        _db = db;
        _settings = settings;
        _reporting = reporting;
        _delivery = delivery;
    }

    public async Task<List<PrintJobDto>> ListJobsForOrderAsync(string orderId, CancellationToken ct = default) =>
        (await _db.PrintJobs.AsNoTracking().Where(j => j.OrderId == orderId).OrderBy(j => j.CreatedAtUtc).ToListAsync(ct))
            .Select(CreatePrintJobsHandler.ToDto).ToList();

    /// <summary>
    /// The pending jobs of a route that this agent will print, claimed for <paramref name="lease"/>
    /// in one atomic UPDATE so two agents polling at the same instant can never be handed the same
    /// job. A job is claimable when nobody holds it, its holder's lease has run out (the agent
    /// crashed or lost its printer), or it is this agent's own (restarted under the same identity).
    /// READPAST skips rows another agent's claim is locking right now instead of waiting on them.
    /// </summary>
    public async Task<List<PrintJobDto>> ClaimPendingJobsForRouteAsync(string route, string agentId, TimeSpan lease, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentId) || agentId.Length > 160 || agentId.Contains('|'))
            throw new DomainException("معرّف وكيل الطباعة غير صالح أو أطول من 160 حرفًا.");
        var now = DateTimeOffset.UtcNow;
        var claimId = $"{agentId}|{Guid.NewGuid():N}";
        var claimed = await _db.PrintJobs.FromSqlRaw(
                """
                WITH next_job AS (
                    SELECT TOP (1) * FROM PrintJobs WITH (ROWLOCK, READPAST, UPDLOCK)
                    WHERE Route = {2} AND Status = {3}
                      AND (ClaimedBy IS NULL OR ClaimedUntilUtc IS NULL OR ClaimedUntilUtc < {4}
                           OR LEFT(ClaimedBy, LEN({5}) + 1) = {5} + '|')
                    ORDER BY CreatedAtUtc, Id
                )
                UPDATE next_job SET ClaimedBy = {0}, ClaimedUntilUtc = {1}
                OUTPUT INSERTED.*
                """,
                claimId, now + lease, route, nameof(PrintJobStatus.Pending), now, agentId)
            .AsNoTracking().ToListAsync(ct);
        return claimed.Select(j => CreatePrintJobsHandler.ToDto(j) with { ClaimId = j.ClaimedBy }).ToList();
    }

    /// <summary>Once bytes may leave, a job is never reclaimed automatically. A lost agent/report
    /// leaves Sending visible for inspection, rather than creating a second physical ticket.</summary>
    public async Task<bool> StartSendingAsync(string jobId, string claimId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var count = await _db.PrintJobs.Where(j => j.Id == jobId && j.ClaimedBy == claimId
                && (j.Status == nameof(PrintJobStatus.Sending)
                    || j.Status == nameof(PrintJobStatus.Pending) && j.ClaimedUntilUtc >= now))
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.Status, nameof(PrintJobStatus.Sending)), ct);
        return count == 1;
    }

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
            var pending = forRoute.Where(j => (j.Status == nameof(PrintJobStatus.Pending) || j.Status == nameof(PrintJobStatus.Sending))).ToList();
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
                        || (j.Status == nameof(PrintJobStatus.Sending) && j.ClaimedUntilUtc <= DateTimeOffset.UtcNow)
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
                PrintDocumentKind.DriverStatement => await ComposeDriverStatementAsync(job, ct),
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

    /// <summary>The key a driver-statement print job carries in place of an order id: driver, business
    /// day, payment filter, and summary/detail. Built and parsed only here.</summary>
    public static string DriverStatementKey(string driverId, DateOnly businessDate, string? filter, bool detailed) =>
        $"drv|{driverId}|{businessDate:yyyy-MM-dd}|{(string.IsNullOrWhiteSpace(filter) ? "all" : filter)}|{(detailed ? "d" : "s")}";

    /// <summary>UI-REQ-001: composed at print time from the very query the screen shows.</summary>
    private async Task<ComposedTicket> ComposeDriverStatementAsync(PrintJobEntity job, CancellationToken ct)
    {
        var parts = job.OrderId.Split('|');
        if (parts.Length != 5 || parts[0] != "drv") throw new InvalidOperationException("مفتاح كشف السائق غير صالح.");
        var date = DateOnly.ParseExact(parts[2], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var statement = await _delivery.GetDriverDailyStatementAsync(parts[1], date, parts[3], ct);
        var settings = await _settings.GetAsync(ct);
        return PrintRules.ComposeDriverStatement(settings.RestaurantName, settings.SubName, statement.DriverName, date, statement.PaymentFilter,
            statement.Orders, statement.Totals, parts[4] == "d", job.CreatedBy, job.CreatedAtUtc);
    }

    /// <summary>The closing statement's paper, composed exactly as the print agent receives it (UI-REQ-002 preview).</summary>
    public static ComposedTicketDto ComposeCloseStatementPaper(ShiftCloseStatement statement, bool recomputed) =>
        ToDto(PrintRules.ComposeShiftCloseStatement(statement, recomputed));

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
        var (statement, recomputed) = await LoadCloseStatementAsync(job.OrderId, ct);
        return PrintRules.ComposeShiftCloseStatement(statement, recomputed);
    }

    /// <summary>
    /// UI-REQ-002: the closing statement as the close froze it - the paper, its reprints and the
    /// screen all read this one copy. A shift closed before statements were frozen gets the same
    /// statement rebuilt from today's records around the expected/counted/difference figures the close
    /// persisted, and is flagged as recomputed so nobody mistakes it for the original.
    /// </summary>
    public async Task<(ShiftCloseStatement Statement, bool Recomputed)> LoadCloseStatementAsync(string shiftId, CancellationToken ct)
    {
        var frozen = await Falah.Infrastructure.Shifts.ShiftCloseStatements.LoadFrozenAsync(_db, shiftId, ct);
        if (frozen is not null) return (frozen, false);

        var (shift, identity) = await LoadShiftIdentityAsync(shiftId, ct);
        if (shift.ClosedAtUtc is null) throw new InvalidOperationException("الوردية لم تُغلق بعد.");
        var closer = shift.ClosedByUserId is { } id
            ? await _db.Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.Name).FirstOrDefaultAsync(ct) ?? id
            : identity.CashierName;
        var opening = Money.FromPiastres(shift.OpeningCashPiastres);
        var expected = shift.ExpectedCashPiastres
            ?? (await Falah.Infrastructure.Shifts.ShiftReconciliation.ComputeBreakdownAsync(_db, shift.Id, opening, ct)).ExpectedCash.Piastres;
        var actual = shift.ActualCashPiastres ?? 0;
        var rebuilt = await Falah.Infrastructure.Shifts.ShiftCloseStatements.BuildAsync(_db, _reporting, shift, identity.CashierName, closer,
            shift.DifferenceAuthorizedBy, shift.ClosedAtUtc.Value, expected, actual, shift.CashDifferencePiastres ?? actual - expected,
            shift.DifferenceReason, ct);
        return (rebuilt, true);
    }
    // The open items at handover are frozen with the statement (ShiftCloseStatements.LoadOutstandingAsync).

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
