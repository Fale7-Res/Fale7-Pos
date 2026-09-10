namespace Falah.Infrastructure.Reports;

using Falah.Application.Reports;
using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Domain.Employees;
using Falah.Domain.Financial;
using Falah.Domain.Inventory;
using Falah.Domain.Orders;
using Falah.Domain.Printing;
using Falah.Domain.Reports;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Assembles the owner dashboard from the same rows every other screen reads.
///
/// It deliberately owns no financial rule of its own: sales come from ReportingQueryService, food
/// cost from the costing pipeline, supplier balances from the supplier ledger, and so on. If a
/// figure here ever disagreed with the screen it came from, that would be a defect, not a difference
/// of method - so nothing is recomputed a second way for convenience.
///
/// Treasury balances and outstanding obligations are deliberately NOT scoped to the reporting
/// period. What is in the drawer is what is in the drawer; asking "how much cash do I have" and
/// getting last Tuesday's answer would be worse than useless.
/// </summary>
public sealed class OwnerDashboardQueryService
{
    private readonly FalahDbContext _db;
    private readonly ReportingQueryService _reports;

    public OwnerDashboardQueryService(FalahDbContext db, ReportingQueryService reports)
    {
        _db = db;
        _reports = reports;
    }

    public async Task<OwnerDashboardDto> GetAsync(BusinessDateRange range, CancellationToken ct = default)
    {
        var sales = await _reports.GetSalesReportAsync(range, null, ct);
        var expenses = await _reports.GetExpenseReportAsync(range, null, ct);
        var refunds = await _reports.GetRefundActivityAsync(range, null, ct);
        var foodCost = await _reports.GetFoodCostReportAsync(range.From, range.To, ct);
        var significantActions = await _reports.GetSignificantActionsAsync(range.From, range.To, ct);

        var (start, end) = range.ToUtcRange();

        var cash = new CashPositionDto(
            sales.OrderCount, sales.GrossSalesPiastres, sales.NetSalesPiastres,
            sales.CollectedSalesPiastres, sales.PendingElectronicSalesPiastres, sales.CashWithDriversPiastres,
            sales.AverageOrderValuePiastres, sales.CashPiastres, sales.CardPiastres,
            sales.InstaPayPiastres, sales.WalletPiastres,
            sales.DineInPiastres, sales.TakeawayPiastres, sales.DeliveryPiastres,
            await CurrentTreasuryAsync(ct));

        var leakage = await LoadLeakageAsync(range, sales, expenses.TotalPiastres, refunds.Count, start, end, ct);
        var wages = await LoadWagesPostedAsync(start, end, ct);
        var profitability = BuildProfitability(sales, foodCost, expenses.TotalPiastres, wages.Total, wages.Count);

        return new OwnerDashboardDto(
            range.From.ToString("yyyy-MM-dd"), range.To.ToString("yyyy-MM-dd"), range.DayCount,
            cash, leakage, profitability,
            await LoadAttentionAsync(range, ct),
            await LoadStockAsync(ct),
            await LoadObligationsAsync(start, end, ct),
            await LoadPeopleAsync(range, wages.Total, ct),
            await LoadHealthAsync(ct),
            significantActions.Count,
            await LoadTrendAsync(range, sales, expenses.TotalPiastres, ct));
    }

    /// <summary>The same figures for the comparable earlier period, plus the series a sparkline is
    /// drawn from. The comparable period comes from TrendRules, not from here: what counts as
    /// comparable is a rule about restaurants, not a detail of this query.</summary>
    private async Task<DashboardTrendDto> LoadTrendAsync(
        BusinessDateRange range, SalesReportDto sales, long expensesTotal, CancellationToken ct)
    {
        var previousRange = TrendRules.ComparablePeriod(range);

        // Reusing the same report methods keeps the comparison honest - the earlier figure is
        // computed exactly the way the current one was, including the cash-basis treatment of
        // money still sitting with a driver.
        var previousSales = await _reports.GetSalesReportAsync(previousRange, null, ct);
        var previousExpenses = await _reports.GetExpenseReportAsync(previousRange, null, ct);

        static ChangeDto Change(long current, long previous)
        {
            var c = TrendRules.Compare(current, previous);
            return new ChangeDto(c.CurrentPiastres, c.PreviousPiastres, c.DeltaPiastres, c.Percent);
        }

        var (granularity, series) = range.IsSingleDay
            ? ("hour", await LoadHourlySeriesAsync(range, ct))
            : ("day", await LoadDailySeriesAsync(range, ct));

        return new DashboardTrendDto(
            previousRange.From.ToString("yyyy-MM-dd"),
            previousRange.To.ToString("yyyy-MM-dd"),
            Change(sales.NetSalesPiastres, previousSales.NetSalesPiastres),
            Change(sales.CollectedSalesPiastres, previousSales.CollectedSalesPiastres),
            Change(sales.CashWithDriversPiastres, previousSales.CashWithDriversPiastres),
            Change(expensesTotal, previousExpenses.TotalPiastres),
            Change(sales.OrderCount, previousSales.OrderCount),
            granularity,
            series);
    }

    /// <summary>Net per order is total less refunds - the same subtraction SalesRules makes for the
    /// period total, so the points on the curve add up to the headline figure above it.</summary>
    private async Task<List<TrendPointDto>> LoadDailySeriesAsync(BusinessDateRange range, CancellationToken ct)
    {
        var (start, end) = range.ToUtcRange();
        var rows = await _db.Orders.AsNoTracking()
            .Where(o => o.Status != nameof(OrderStatus.Cancelled) && o.CreatedAtUtc >= start && o.CreatedAtUtc < end)
            .Select(o => new { o.CreatedAtUtc, o.TotalPiastres, o.RefundAmountPiastres })
            .ToListAsync(ct);

        var byDate = rows
            .GroupBy(r => BusinessClock.BusinessDate(r.CreatedAtUtc))
            .ToDictionary(g => g.Key, g => (Net: g.Sum(r => r.TotalPiastres - r.RefundAmountPiastres), Orders: g.Count()));

        // Every day in the span gets a point, including the ones with no trade at all - a gap in a
        // curve reads as missing data, while a zero reads as a closed day, which is the truth.
        return range.Days()
            .Select(day =>
            {
                var found = byDate.TryGetValue(day, out var v) ? v : (Net: 0L, Orders: 0);
                return new TrendPointDto(day.ToString("MM-dd"), found.Net, found.Orders);
            })
            .ToList();
    }

    private async Task<List<TrendPointDto>> LoadHourlySeriesAsync(BusinessDateRange range, CancellationToken ct)
    {
        var (start, end) = range.ToUtcRange();
        var rows = await _db.Orders.AsNoTracking()
            .Where(o => o.Status != nameof(OrderStatus.Cancelled) && o.CreatedAtUtc >= start && o.CreatedAtUtc < end)
            .Select(o => new { o.CreatedAtUtc, o.TotalPiastres, o.RefundAmountPiastres })
            .ToListAsync(ct);

        // Bucketed by the hour a Cairo clock on the wall would show, not by UTC.
        var byHour = rows
            .GroupBy(r => TimeZoneInfo.ConvertTime(r.CreatedAtUtc, BusinessClock.CairoTimeZone).Hour)
            .ToDictionary(g => g.Key, g => (Net: g.Sum(r => r.TotalPiastres - r.RefundAmountPiastres), Orders: g.Count()));

        return Enumerable.Range(0, 24)
            .Select(hour =>
            {
                var found = byHour.TryGetValue(hour, out var v) ? v : (Net: 0L, Orders: 0);
                return new TrendPointDto($"{hour:00}", found.Net, found.Orders);
            })
            .ToList();
    }

    /// <summary>Current balances across every posted movement - not the period's, because the
    /// question this answers is "what do I hold right now".</summary>
    private async Task<TreasuryBalancesDto> CurrentTreasuryAsync(CancellationToken ct)
    {
        var movements = await _db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Status == nameof(MovementStatus.Posted))
            .Select(m => new { m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres })
            .ToListAsync(ct);
        long Balance(string account) => movements.Sum(m =>
            (m.DestinationAccountId == account ? m.AmountPiastres : 0) -
            (m.SourceAccountId == account ? m.AmountPiastres : 0));
        return new TreasuryBalancesDto(Balance(TreasuryAccounts.Main), Balance(TreasuryAccounts.Drawer),
            Balance(TreasuryAccounts.Bank), Balance(TreasuryAccounts.InstaPay), Balance(TreasuryAccounts.Wallet));
    }

    private async Task<LeakageDto> LoadLeakageAsync(
        BusinessDateRange range, SalesReportDto sales, long expensesTotal, int refundCount,
        DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var lossTypes = new[] { nameof(StockMovementType.Waste), nameof(StockMovementType.Spoilage), nameof(StockMovementType.Breakage) };
        var losses = await _db.StockMovements.AsNoTracking()
            .Where(m => lossTypes.Contains(m.Type) && m.CreatedAtUtc >= start && m.CreatedAtUtc < end)
            .Select(m => m.LossValuePiastres).ToListAsync(ct);

        return new LeakageDto(
            sales.OffersValuePiastres, sales.CompsValuePiastres, sales.ManualDiscountsPiastres,
            sales.RefundsPiastres, refundCount,
            losses.Sum(v => v ?? 0), losses.Count, expensesTotal);
    }

    /// <summary>Wages actually posted to employee ledgers in the period - daily wages, salaries,
    /// overtime and extra shifts. Advances and withdrawals are excluded: they move money the
    /// employee had already earned, so counting them as wage cost would double-count it.</summary>
    /// <summary>The total AND how many entries produced it. The count is what lets the wage ratio
    /// say "unknown" instead of "0%" for a period where nobody posted any labour at all.</summary>
    private async Task<(long Total, int Count)> LoadWagesPostedAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var wageTypes = new[]
        {
            nameof(LedgerEntryType.DailyWage), nameof(LedgerEntryType.Salary),
            nameof(LedgerEntryType.Overtime), nameof(LedgerEntryType.ExtraShift),
            nameof(LedgerEntryType.Bonus), nameof(LedgerEntryType.OtherEarning),
        };
        var rows = await _db.EmployeeLedgerEntries.AsNoTracking()
            .Where(e => wageTypes.Contains(e.Type) && e.CreatedAtUtc >= start && e.CreatedAtUtc < end)
            .Select(e => e.CreditPiastres).ToListAsync(ct);
        return (rows.Sum(), rows.Count);
    }

    private static ProfitabilityDto BuildProfitability(
        SalesReportDto sales, FoodCostReportDto foodCost, long expenses, long wages, int wagePostingCount)
    {
        var netSales = Money.FromPiastres(sales.NetSalesPiastres);
        var knownFoodCost = Money.FromPiastres(foodCost.TotalFoodCostPiastres);

        var gross = ProfitabilityRules.ComputeGrossProfit(
            netSales, knownFoodCost, foodCost.SoldLineCount, foodCost.KnownLineCount);
        var operating = ProfitabilityRules.ComputeOperatingMargin(
            netSales, knownFoodCost, Money.FromPiastres(expenses), Money.FromPiastres(wages),
            foodCost.SoldLineCount, foodCost.KnownLineCount, wagePostingCount);

        return new ProfitabilityDto(
            gross.NetSales.Piastres, gross.KnownFoodCost?.Piastres, gross.GrossProfit?.Piastres,
            gross.FoodCostPercent, gross.GrossProfitPercent,
            wages, operating.WagePercent,
            operating.OperatingResult?.Piastres, operating.OperatingPercent,
            gross.SoldLineCount, gross.KnownLineCount, gross.IsComplete,
            foodCost.Missing);
    }

    private async Task<AttentionDto> LoadAttentionAsync(BusinessDateRange range, CancellationToken ct)
    {
        var openDay = await _db.BusinessDays.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Status == nameof(Falah.Domain.BusinessDay.BusinessDayStatus.Open), ct);
        // Measured against the real business date now, not against the reporting range - a day left
        // open is stale relative to today no matter which period the owner happens to be reading.
        int? openDayAge = openDay is null ? null
            : Math.Max(0, BusinessClock.Today().DayNumber - openDay.BusinessDate.DayNumber);

        var (costStart, costEnd) = range.ToUtcRange();
        var unusualCostChanges = await _db.AuditEvents.AsNoTracking()
            .CountAsync(e => e.Category == "inventory_cost" && e.Severity == "warning"
                && e.TimestampUtc >= costStart && e.TimestampUtc < costEnd, ct);

        var threshold = await Falah.Infrastructure.Shifts.PrepareShiftCloseHandler.ReadDiscrepancyThresholdAsync(_db, ct);

        var shifts = await _db.Shifts.AsNoTracking()
            .Where(s => s.BusinessDate >= range.From && s.BusinessDate <= range.To)
            .Select(s => new { s.Status, s.CashDifferencePiastres, s.DifferenceAuthorizedBy })
            .ToListAsync(ct);

        var withDifference = shifts.Where(s => s.CashDifferencePiastres != null && s.CashDifferencePiastres != 0).ToList();

        var custodies = await _db.DriverCustodies.AsNoTracking()
            .Where(c => c.Status != nameof(CustodyStatus.Settled))
            .Select(c => c.ExpectedPiastres).ToListAsync(ct);

        var pendingElectronic = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.ElectronicPaymentStatus == "Pending", ct);

        var openExceptions = await _db.ControlExceptionStates.AsNoTracking()
            .CountAsync(e => e.Status == "Open", ct);

        var day = await _db.BusinessDays.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Status == nameof(Falah.Domain.BusinessDay.BusinessDayStatus.Open), ct);

        var blockerCount = 0;
        if (day is not null)
        {
            var blockers = await Falah.Infrastructure.ControlCenter.BusinessDayCloseEvaluator.EvaluateAsync(_db, day, ct);
            blockerCount = blockers.Count;
        }

        return new AttentionDto(
            OpenShiftCount: shifts.Count(s => s.Status != "Closed"),
            ShiftsWithDiscrepancyCount: withDifference.Count,
            ShiftShortagePiastres: withDifference.Where(s => s.CashDifferencePiastres < 0).Sum(s => -s.CashDifferencePiastres!.Value),
            ShiftOveragePiastres: withDifference.Where(s => s.CashDifferencePiastres > 0).Sum(s => s.CashDifferencePiastres!.Value),
            // A difference beyond the configured threshold that nobody authorised is the one a
            // manager has to answer for - it is also what blocks the business day from closing.
            UnauthorizedShiftDiscrepancyCount: withDifference.Count(s => s.DifferenceAuthorizedBy == null
                && (s.CashDifferencePiastres > threshold || s.CashDifferencePiastres < -threshold)),
            UnsettledCustodyCount: custodies.Count,
            UnsettledCustodyPiastres: custodies.Sum(),
            PendingElectronicOrderCount: pendingElectronic,
            OpenExceptionCount: openExceptions,
            CloseBlockerCount: blockerCount,
            HasOpenBusinessDay: day is not null,
            OpenBusinessDayAgeDays: openDayAge,
            UnusualCostChangeCount: unusualCostChanges);
    }

    private async Task<StockPositionDto> LoadStockAsync(CancellationToken ct)
    {
        var items = await _db.InventoryItems.AsNoTracking().Where(i => i.Active)
            .Select(i => new { i.Id, i.Name, i.Unit, i.MinimumMilliUnits }).ToListAsync(ct);
        var balances = await _db.StockMovements.AsNoTracking()
            .GroupBy(m => m.InventoryItemId)
            .Select(g => new { InventoryItemId = g.Key, Balance = g.Sum(m => m.QuantityMilliUnits) })
            .ToDictionaryAsync(x => x.InventoryItemId, x => x.Balance, ct);

        var low = items
            .Select(i => new { i.Id, i.Name, i.Unit, i.MinimumMilliUnits, Balance = balances.GetValueOrDefault(i.Id) })
            // Same test the inventory screen uses, so the count here and the list there agree.
            .Where(i => i.Balance <= i.MinimumMilliUnits && i.MinimumMilliUnits > 0)
            .OrderBy(i => i.Balance - i.MinimumMilliUnits)
            .Select(i => new LowStockItemDto(i.Id, i.Name, i.Unit, i.Balance, i.MinimumMilliUnits))
            .ToList();

        return new StockPositionDto(items.Count, low.Count, low);
    }

    private async Task<ObligationsDto> LoadObligationsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        // The supplier ledger is the source of truth for what is owed - the same one the per-supplier
        // screen reads - so this total can never drift from the detail behind it.
        var supplierRows = await _db.SupplierLedgerEntries.AsNoTracking()
            .GroupBy(e => e.SupplierId)
            .Select(g => new { SupplierId = g.Key, Outstanding = g.Sum(e => e.DebitPiastres - e.CreditPiastres) })
            .ToListAsync(ct);
        var owing = supplierRows.Where(r => r.Outstanding > 0).ToList();

        var purchases = await _db.Purchases.AsNoTracking()
            .Where(p => p.CreatedAtUtc >= start && p.CreatedAtUtc < end)
            .Select(p => new { p.TotalPiastres, p.PaidPiastres }).ToListAsync(ct);

        var employeeRows = await _db.EmployeeLedgerEntries.AsNoTracking()
            .GroupBy(e => e.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Balance = g.Sum(e => e.CreditPiastres - e.DebitPiastres) })
            .ToListAsync(ct);
        var owedEmployees = employeeRows.Where(r => r.Balance > 0).ToList();

        // Aging is deliberately NOT period-scoped: an invoice that fell overdue last month is still
        // overdue while the owner looks at this week.
        var asOf = BusinessClock.Today();
        var openInvoices = await _db.Purchases.AsNoTracking()
            .Where(p => p.RemainingAmountPiastres > 0 && p.Status != "cancelled")
            .Select(p => new { p.RemainingAmountPiastres, p.DueDate }).ToListAsync(ct);

        var overdueInvoices = openInvoices
            .Select(p => new { p.RemainingAmountPiastres, Days = Falah.Domain.Purchasing.PayablesAging.DaysOverdue(p.DueDate, asOf) })
            .Where(p => p.Days is > 0).ToList();
        var unscheduledInvoices = openInvoices.Where(p => p.DueDate is null).ToList();

        return new ObligationsDto(
            owing.Sum(r => r.Outstanding), owing.Count,
            purchases.Sum(p => p.TotalPiastres), purchases.Sum(p => p.PaidPiastres),
            owedEmployees.Sum(r => r.Balance), owedEmployees.Count,
            overdueInvoices.Sum(p => p.RemainingAmountPiastres), overdueInvoices.Count,
            unscheduledInvoices.Sum(p => p.RemainingAmountPiastres), unscheduledInvoices.Count,
            overdueInvoices.Count == 0 ? 0 : overdueInvoices.Max(p => p.Days!.Value));
    }

    private async Task<PeopleDto> LoadPeopleAsync(BusinessDateRange range, long wages, CancellationToken ct)
    {
        var records = await _db.AttendanceRecords.AsNoTracking()
            .Where(a => a.Date >= range.From && a.Date <= range.To)
            .Select(a => new { a.Status, a.LateMinutes }).ToListAsync(ct);

        return new PeopleDto(
            records.Count,
            records.Count(a => a.Status == nameof(AttendanceStatus.Present)),
            records.Count(a => a.Status == nameof(AttendanceStatus.Late)),
            records.Count(a => a.Status == nameof(AttendanceStatus.Absent)),
            records.Count(a => a.Status == nameof(AttendanceStatus.MissingFingerprint)),
            records.Sum(a => a.LateMinutes),
            wages);
    }

    private async Task<SystemHealthDto> LoadHealthAsync(CancellationToken ct)
    {
        var backups = await _db.BackupRecords.AsNoTracking()
            .Where(b => b.Kind == nameof(Persistence.Entities.BackupRecordKind.Backup))
            .Select(b => new { b.Status, b.CompletedAtUtc, b.VerificationStatus })
            .ToListAsync(ct);

        var lastCompleted = backups
            .Where(b => b.Status == nameof(Persistence.Entities.BackupRecordStatus.Succeeded) && b.CompletedAtUtc != null)
            .OrderByDescending(b => b.CompletedAtUtc)
            .FirstOrDefault();

        var routes = new List<PrintRouteHealthDto>();
        var now = DateTimeOffset.UtcNow;
        foreach (var route in Enum.GetNames<PrintRoute>())
        {
            var jobs = await _db.PrintJobs.AsNoTracking().Where(j => j.Route == route)
                .Select(j => new { j.Id, j.Status, j.CreatedAtUtc }).ToListAsync(ct);
            var pending = jobs.Where(j => j.Status == nameof(PrintJobStatus.Pending)).ToList();
            var jobIds = jobs.Select(j => j.Id).ToList();

            var attempts = await _db.PrintJobAttempts.AsNoTracking()
                .Where(a => jobIds.Contains(a.PrintJobId))
                .Select(a => new { a.Succeeded, a.AttemptedAtUtc, a.Error })
                .ToListAsync(ct);

            var lastSuccess = attempts.Where(a => a.Succeeded).OrderByDescending(a => a.AttemptedAtUtc).FirstOrDefault();
            var lastFailure = attempts.Where(a => !a.Succeeded).OrderByDescending(a => a.AttemptedAtUtc).FirstOrDefault();

            routes.Add(new PrintRouteHealthDto(
                route,
                pending.Count,
                pending.Count > 0 ? Math.Round((now - pending.Min(p => p.CreatedAtUtc)).TotalMinutes, 1) : null,
                lastSuccess?.AttemptedAtUtc,
                lastFailure?.AttemptedAtUtc,
                lastFailure?.Error,
                jobs.Count(j => j.Status == nameof(PrintJobStatus.Failed))));
        }

        return new SystemHealthDto(
            lastCompleted?.CompletedAtUtc,
            lastCompleted?.CompletedAtUtc is { } at ? Math.Round((now - at).TotalHours, 1) : null,
            lastCompleted?.VerificationStatus,
            backups.Count(b => b.Status == nameof(Persistence.Entities.BackupRecordStatus.Failed)),
            routes);
    }
}
