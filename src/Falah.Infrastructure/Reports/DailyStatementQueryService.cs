namespace Falah.Infrastructure.Reports;

using Falah.Application.Printing;
using Falah.Application.Reports;
using Falah.Application.Treasury;
using Falah.Domain.Common;
using Falah.Domain.Employees;
using Falah.Domain.Financial;
using Falah.Domain.Inventory;
using Falah.Infrastructure.ControlCenter;
using Falah.Infrastructure.Audit;
using Falah.Infrastructure.Backup;
using Falah.Infrastructure.Employees;
using Falah.Infrastructure.Inventory;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Printing;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// "كشف اليوم" - assembles ReportingQueryService/InventoryQueryService/AuditQueryService/
/// ControlCenterQueryService/PrintJobQueryService/BackupQueryService/EmployeeQueryService's own
/// already-computed figures into one owner-facing daily statement. Introduces no new financial rule
/// and no new source of truth - every figure here is traceable back to the exact query that already
/// backs some other screen; the few methods this slice added (GetSoldProductsAsync,
/// GetShiftsForBusinessDateAsync, ControlCenterQueryService's day-scoped audit overload) live next
/// to the reports they extend, not here, so this class stays pure composition.
///
/// Two things this build still cannot answer, by design, not by oversight: a completed order cannot
/// currently be voided/cancelled in this system (no handler exists), so no "void" figure appears
/// anywhere in this statement; and stock variance (theoretical vs recorded usage) is computed only
/// per ingredient, on demand, never as a single day-level total - rolling it up would average away
/// the one thing that makes it useful (which specific ingredient drifted).
/// </summary>
public sealed class DailyStatementQueryService
{
    private readonly FalahDbContext _db;
    private readonly ReportingQueryService _reports;
    private readonly InventoryQueryService _inventory;
    private readonly ControlCenterQueryService _controlCenter;
    private readonly AuditQueryService _audit;
    private readonly PrintJobQueryService _printJobs;
    private readonly BackupQueryService _backups;
    private readonly EmployeeQueryService _employees;

    public DailyStatementQueryService(
        FalahDbContext db, ReportingQueryService reports, InventoryQueryService inventory,
        ControlCenterQueryService controlCenter, AuditQueryService audit, PrintJobQueryService printJobs,
        BackupQueryService backups, EmployeeQueryService employees)
    {
        _db = db;
        _reports = reports;
        _inventory = inventory;
        _controlCenter = controlCenter;
        _audit = audit;
        _printJobs = printJobs;
        _backups = backups;
        _employees = employees;
    }

    public async Task<DailyStatementDto> GetAsync(DateOnly businessDate, CancellationToken ct = default)
    {
        var range = BusinessDateRange.SingleDay(businessDate);
        var (start, end) = range.ToUtcRange();

        var day = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.BusinessDate == businessDate, ct);

        var sales = await _reports.GetSalesReportAsync(range, null, ct);
        var tax = await _reports.GetTaxReportAsync(range, null, ct);
        var soldProducts = await _reports.GetSoldProductsAsync(businessDate, businessDate, ct);
        var productHourly = await _reports.GetProductHourlySalesAsync(businessDate, businessDate, ct);
        var expenses = await _reports.GetExpenseReportAsync(range, null, ct);
        var expenseLines = await BuildExpenseLinesAsync(start, end, ct);
        var shifts = await _reports.GetShiftsForBusinessDateAsync(businessDate, ct);
        var shiftTotals = BuildShiftTotals(shifts);
        var treasuryMovements = await LoadTreasuryMovementsAsync(start, end, ct);
        var treasuryBalancesNow = await LoadCurrentTreasuryBalancesAsync(ct);
        var employeeRows = await BuildEmployeeRowsAsync(businessDate, start, end, ct);
        var inventory = await BuildInventoryAsync(businessDate, start, end, ct);
        var delivery = await _reports.GetDeliveryOutcomesReportAsync(businessDate, businessDate, ct);
        var refunds = await _reports.GetRefundActivityAsync(range, null, ct);
        var auditTrail = await BuildAuditTrailAsync(start, end, ct);
        var printing = await BuildPrintingAsync(start, end, ct);
        var backup = await BuildBackupAsync(start, end, ct);

        return new DailyStatementDto(
            businessDate.ToString("yyyy-MM-dd"), day?.Id, day?.Status,
            sales, tax, soldProducts, productHourly, expenses, expenseLines,
            shifts, shiftTotals, treasuryMovements, treasuryBalancesNow,
            employeeRows, inventory, delivery, refunds, auditTrail, printing, backup);
    }

    private async Task<List<DailyExpenseLineDto>> BuildExpenseLinesAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var rows = await _db.Expenses.AsNoTracking()
            .Where(e => e.CreatedAtUtc >= start && e.CreatedAtUtc < end)
            .OrderByDescending(e => e.CreatedAtUtc).ToListAsync(ct);
        var userIds = rows.Select(e => e.CreatedByUserId).Distinct();
        var userNames = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        return rows.Select(e => new DailyExpenseLineDto(
            e.Id, e.CreatedAtUtc, e.Category, e.Description, e.AmountPiastres,
            userNames.GetValueOrDefault(e.CreatedByUserId, e.CreatedByUserId), e.ApprovedBy, e.PaymentAccountId, e.ShiftId)).ToList();
    }

    private static ShiftDayTotalsDto BuildShiftTotals(IReadOnlyList<ShiftSummaryDto> shifts) => new(
        shifts.Count,
        shifts.Sum(s => s.OpeningCashPiastres),
        shifts.Sum(s => s.ExpectedCashPiastres ?? 0),
        shifts.Sum(s => s.ActualCashPiastres ?? 0),
        shifts.Where(s => (s.CashDifferencePiastres ?? 0) < 0).Sum(s => Math.Abs(s.CashDifferencePiastres ?? 0)),
        shifts.Where(s => (s.CashDifferencePiastres ?? 0) > 0).Sum(s => s.CashDifferencePiastres ?? 0));

    /// <summary>Movements are day-scoped by CreatedAtUtc directly, not by BusinessDayId - not every
    /// treasury movement is guaranteed to carry one (a plain deposit/withdrawal/transfer has no
    /// order/business-day context of its own), so this reads the one field every movement always
    /// has, exactly like /api/treasury/movements already does for its own optional day filter.</summary>
    private async Task<List<TreasuryMovementDto>> LoadTreasuryMovementsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var rows = await _db.TreasuryMovements.AsNoTracking()
            .Where(m => m.CreatedAtUtc >= start && m.CreatedAtUtc < end)
            .OrderByDescending(m => m.CreatedAtUtc).ToListAsync(ct);

        // Same readable-reference resolution the treasury ledger endpoint does: one batched lookup
        // so an order-backed movement names the order by its NUMBER, not its internal id.
        var orderIds = rows.Where(m => m.ReferenceType == "order" && !string.IsNullOrEmpty(m.ReferenceId))
            .Select(m => m.ReferenceId).Distinct().ToList();
        var orderNumbers = orderIds.Count == 0
            ? new Dictionary<string, string>()
            : await _db.Orders.AsNoTracking().Where(o => orderIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.OrderNumber, ct);

        return rows.Select(m => new TreasuryMovementDto(
            m.Id, m.Type, m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres, m.ReferenceType,
            m.ReferenceId, m.ShiftId, m.Reason, m.CreatedBy, m.ApprovedBy, m.CreatedAtUtc,
            orderNumbers.GetValueOrDefault(m.ReferenceId))).ToList();
    }

    /// <summary>Deliberately ALL-TIME, not day-scoped - "what is in the account right now," matching
    /// OwnerDashboardQueryService's own explicit design choice for treasury balances (see that
    /// class's doc comment): a balance is a running total, not something a business day resets.</summary>
    private async Task<TreasuryBalancesDto> LoadCurrentTreasuryBalancesAsync(CancellationToken ct)
    {
        var movements = await _db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Status == nameof(MovementStatus.Posted))
            .Select(m => new { m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres }).ToListAsync(ct);
        long Balance(string accountId) => movements.Sum(m =>
            (m.DestinationAccountId == accountId ? m.AmountPiastres : 0) - (m.SourceAccountId == accountId ? m.AmountPiastres : 0));
        return new TreasuryBalancesDto(Balance(TreasuryAccounts.Main), Balance(TreasuryAccounts.Drawer),
            Balance(TreasuryAccounts.Bank), Balance(TreasuryAccounts.InstaPay), Balance(TreasuryAccounts.Wallet));
    }

    /// <summary>One row per employee, attendance plus every ledger type actually posted for them
    /// today - BonusPiastres/DeductionPiastres read the real ledger, so they report a genuine zero
    /// (not a placeholder) for as long as RecordManualLedgerEntryHandler has no calling UI.</summary>
    private async Task<List<DailyEmployeeRowDto>> BuildEmployeeRowsAsync(
        DateOnly businessDate, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var employees = await _employees.ListAsync(ct);
        var attendance = await _db.AttendanceRecords.AsNoTracking()
            .Where(a => a.Date == businessDate).ToListAsync(ct);
        var attendanceByEmployee = attendance.ToDictionary(a => a.EmployeeId);

        var ledgerRows = await _db.EmployeeLedgerEntries.AsNoTracking()
            .Where(l => l.CreatedAtUtc >= start && l.CreatedAtUtc < end).ToListAsync(ct);
        var ledgerByEmployee = ledgerRows.GroupBy(l => l.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<DailyEmployeeRowDto>(employees.Count);
        foreach (var employee in employees)
        {
            attendanceByEmployee.TryGetValue(employee.Id, out var attendanceRow);
            var entries = ledgerByEmployee.GetValueOrDefault(employee.Id) ?? new();
            long SumCredit(string type) => entries.Where(l => l.Type == type).Sum(l => l.CreditPiastres);
            long SumDebit(string type) => entries.Where(l => l.Type == type).Sum(l => l.DebitPiastres);
            var balance = await _employees.GetBalanceAsync(employee.Id, ct);

            rows.Add(new DailyEmployeeRowDto(
                employee.Id, employee.Name, attendanceRow is not null, attendanceRow?.Status,
                attendanceRow?.CheckIn, attendanceRow?.CheckOut, attendanceRow?.WorkHours ?? 0,
                attendanceRow?.LateMinutes ?? 0, attendanceRow?.OvertimeHours ?? 0,
                SumCredit(nameof(LedgerEntryType.DailyWage)), SumCredit(nameof(LedgerEntryType.Overtime)),
                SumCredit(nameof(LedgerEntryType.Bonus)), SumDebit(nameof(LedgerEntryType.Deduction)),
                SumDebit(nameof(LedgerEntryType.Advance)), SumDebit(nameof(LedgerEntryType.Withdrawal)),
                SumDebit(nameof(LedgerEntryType.PayrollSettlement)), balance,
                SumDebit(nameof(LedgerEntryType.Settlement))));
        }
        return rows;
    }

    /// <summary>Purchases/consumption/waste for the day, plus current (not day-scoped, see
    /// DailyInventoryDto's own doc comment) low-stock/negative-stock/uncosted-item counts from the
    /// same overview the Inventory screen itself reads.</summary>
    private async Task<DailyInventoryDto> BuildInventoryAsync(DateOnly businessDate, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var movements = await _db.StockMovements.AsNoTracking()
            .Where(m => m.CreatedAtUtc >= start && m.CreatedAtUtc < end)
            .Select(m => new { m.InventoryItemId, m.Type, m.Direction, m.QuantityMilliUnits }).ToListAsync(ct);
        var packagingIds = (await _db.InventoryItems.AsNoTracking().Where(i => i.IsPackaging).Select(i => i.Id).ToListAsync(ct)).ToHashSet();

        long SumQuantity(StockMovementType type, bool? isPackaging = null) => movements
            .Where(m => m.Type == type.ToString() && (isPackaging == null || packagingIds.Contains(m.InventoryItemId) == isPackaging))
            .Sum(m => m.QuantityMilliUnits);
        long NetAdjustment() => movements.Where(m => m.Type == nameof(StockMovementType.CountAdjustment))
            .Sum(m => m.Direction == nameof(StockDirection.In) ? m.QuantityMilliUnits : -m.QuantityMilliUnits);

        var loss = await _inventory.GetLossSummaryAsync(businessDate, ct);
        var purchasesValue = await _db.Purchases.AsNoTracking()
            .Where(p => p.CreatedAtUtc >= start && p.CreatedAtUtc < end)
            .SumAsync(p => (long?)p.TotalPiastres, ct) ?? 0;
        var overview = await _inventory.GetInventoryOverviewAsync(ct);

        return new DailyInventoryDto(
            SumQuantity(StockMovementType.PurchaseReceipt), SumQuantity(StockMovementType.ManualReceipt),
            SumQuantity(StockMovementType.Usage, isPackaging: false), SumQuantity(StockMovementType.Usage, isPackaging: true),
            SumQuantity(StockMovementType.Waste), SumQuantity(StockMovementType.Spoilage), SumQuantity(StockMovementType.Breakage),
            loss.TotalLossValuePiastres, NetAdjustment(), purchasesValue,
            overview.LowStockCount, overview.NegativeBalanceCount, overview.ItemsWithoutCostCount);
    }

    /// <summary>Unions Falah.Domain.Audit.AuditEvent (order/cash/shift/inventory/menu/employee/
    /// attendance/backup/settings/purchasing/inventory_cost) with ControlAuditEventEntity
    /// (permission/user changes, business-day open/close/reopen, cash-count finalization, PIN
    /// login failures) for display ONLY - neither table is migrated, merged, or written to by this
    /// query. Source tells you exactly which table a row actually lives in.</summary>
    private async Task<List<DailyAuditEntryDto>> BuildAuditTrailAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var generalEvents = await _audit.ListAsync(null, null, null, start, end, limit: 500, ct: ct);
        var controlEvents = await _controlCenter.ListAuditEventsAsync(limit: 500, from: start, to: end, ct: ct);

        var rows = new List<DailyAuditEntryDto>(generalEvents.Count + controlEvents.Count);
        rows.AddRange(generalEvents.Select(e => new DailyAuditEntryDto(
            "audit_event", e.Id, e.Action, e.UserName, e.Category, e.Details, e.Severity,
            null, e.Reason, e.AuthorizedBy, e.TimestampUtc)));
        rows.AddRange(controlEvents.Select(e => new DailyAuditEntryDto(
            "control_audit_event", e.Id, e.Action, e.Actor, null, e.EntityId, null,
            e.AmountPiastres, e.Reason, e.ApprovedBy, e.CreatedAtUtc)));

        return rows.OrderByDescending(r => r.CreatedAtUtc).ToList();
    }

    private async Task<DailyPrintingDto> BuildPrintingAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var overview = await _printJobs.GetOverviewAsync(ct);
        var reprintCount = await _db.AuditEvents.AsNoTracking()
            .CountAsync(e => e.Category == "order" && e.Action == "إعادة طباعة طلب" && e.TimestampUtc >= start && e.TimestampUtc < end, ct);
        return new DailyPrintingDto(overview.TotalPendingCount, overview.TotalFailedCount, overview.TotalStuckCount, reprintCount, overview.Routes);
    }

    private async Task<DailyBackupDto> BuildBackupAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var all = await _backups.ListAsync(ct);
        var backups = all.Where(b => b.Kind == "Backup").ToList();
        var lastVerified = backups.Where(b => b.Status == "Succeeded" && b.VerificationStatus == "Valid" && b.FileSizeBytes != null)
            .OrderByDescending(b => b.CompletedAtUtc).FirstOrDefault();
        double? ageHours = lastVerified?.CompletedAtUtc is { } completedAt
            ? (DateTimeOffset.UtcNow - completedAt).TotalHours : null;
        var today = backups.Where(b => b.RequestedAtUtc >= start && b.RequestedAtUtc < end).ToList();

        return new DailyBackupDto(
            lastVerified?.CompletedAtUtc, ageHours, lastVerified?.VerificationStatus,
            today.Count, today.Count(b => b.Status == "Failed"));
    }
}
