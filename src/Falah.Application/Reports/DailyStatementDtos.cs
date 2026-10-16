namespace Falah.Application.Reports;

using Falah.Application.Treasury;

/// <summary>
/// "كشف اليوم" - the owner's single "what happened today" statement. Every field here is assembled
/// from data that already exists and is already computed elsewhere (ReportingQueryService,
/// OwnerDashboardQueryService, ControlCenterQueryService, AuditQueryService, PrintJobQueryService,
/// BackupQueryService, PurchasingQueryService, InventoryQueryService, EmployeeQueryService) - this
/// DTO introduces no new financial rule and no new source of truth. Where a confirmed gap required a
/// small amount of new aggregation (the full sold-product list, the day-scoped employee/treasury/
/// audit views), that logic lives next to the query it extends, not here.
///
/// A field that cannot be answered from real data is null/empty and the day-level UX says so
/// explicitly - nothing here is invented to fill a gap (see DailyStatementQueryService's own doc
/// comment for the two items this build still cannot answer: completed-order void/cancel, and
/// per-item unexplained stock variance rolled up to a single day-level number).
/// </summary>
public sealed record DailyStatementDto(
    string BusinessDate,
    string? BusinessDayId,
    string? BusinessDayStatus,
    SalesReportDto Sales,
    TaxReportDto Tax,
    IReadOnlyList<SoldProductLineDto> SoldProducts,
    IReadOnlyList<ProductHourlySalesDto> ProductHourly,
    ExpenseReportDto Expenses,
    IReadOnlyList<DailyExpenseLineDto> ExpenseLines,
    IReadOnlyList<ShiftSummaryDto> Shifts,
    ShiftDayTotalsDto ShiftTotals,
    IReadOnlyList<TreasuryMovementDto> TreasuryMovements,
    TreasuryBalancesDto TreasuryBalancesNow,
    IReadOnlyList<DailyEmployeeRowDto> Employees,
    DailyInventoryDto Inventory,
    DeliveryOutcomesReportDto Delivery,
    IReadOnlyList<RefundActivityDto> Refunds,
    IReadOnlyList<DailyAuditEntryDto> AuditTrail,
    DailyPrintingDto Printing,
    DailyBackupDto Backup);

/// <summary>Every product sold on the day, not just the top N - the complete list Section 2 of the
/// statement asks for. Grouped by product NAME, matching GetTopSellingItemsAsync/
/// GetProductHourlySalesAsync/GetCategorySalesAsync's own existing grouping key, so this list, the
/// hourly matrix, and the category totals can never disagree about what one "product" is.</summary>
public sealed record SoldProductLineDto(
    string ProductName, string? CategoryId, string? CategoryName,
    int Quantity, int OrderCount, long TotalPiastres, int PeakHour);

/// <summary>One expense, in full - Section 4 asks for the itemized list ExpenseReportDto.Expenses
/// already carries but that the Reports screen fetches and discards. EnteredByName/ApprovedByName
/// are resolved from Users here because ExpenseDto itself only carries CreatedByUserId (an id, not a
/// display name) and the already-name-valued ApprovedBy.</summary>
public sealed record DailyExpenseLineDto(
    string Id, DateTimeOffset CreatedAtUtc, string Category, string Description, long AmountPiastres,
    string EnteredByName, string? ApprovedByName, string PaymentAccountId, string? ShiftId);

/// <summary>Day totals across every shift, shown ALONGSIDE the per-shift rows (Shifts above) - never
/// instead of them. Section 5 is explicit: "Do NOT hide shift differences inside one net number
/// only," so this is an addition to the per-shift list, not a replacement for it.</summary>
public sealed record ShiftDayTotalsDto(
    int ShiftCount, long OpeningCashPiastres, long ExpectedCashPiastres, long ActualCashPiastres,
    long ShortagePiastres, long OveragePiastres);

/// <summary>One employee's day - attendance plus every ledger movement type posted for them today,
/// each reported only where real data exists. RunningBalancePiastres is the same all-time
/// EmployeeRules.CalculateBalance figure the Employees screen already shows (not a day-scoped
/// balance - a running balance has no "today" version by definition), included here so the owner
/// does not have to open a second screen to see it. BonusPiastres/DeductionPiastres are zero for
/// every employee today unless RecordManualLedgerEntryHandler was actually called - this build adds
/// no UI path for that (still true as of this statement; see the final report), so these fields
/// report real zeros, not placeholders.</summary>
public sealed record DailyEmployeeRowDto(
    string EmployeeId, string EmployeeName, bool HasAttendanceRecord, string? AttendanceStatus,
    string? CheckIn, string? CheckOut, double WorkHours, int LateMinutes, double OvertimeHours,
    long DailyWageAccruedPiastres, long OvertimeAccruedPiastres, long BonusPiastres, long DeductionPiastres,
    long AdvancePaidTodayPiastres, long WithdrawalPaidTodayPiastres, long PayrollSettlementPaidTodayPiastres,
    long RunningBalancePiastres, long SettlementPaidTodayPiastres = 0)
{
    public long TotalPaidTodayPiastres => AdvancePaidTodayPiastres + WithdrawalPaidTodayPiastres
        + PayrollSettlementPaidTodayPiastres + SettlementPaidTodayPiastres;
}

/// <summary>Section 8. Movement totals are milli-units (the system's own stock unit), consistent with
/// every other inventory screen. Purchasing/consumption/waste figures are day-scoped; LowStockCount/
/// NegativeStockCount/ItemsWithoutCostCount are deliberately current-moment, not day-scoped - a stock
/// balance has no historical version in this system (same convention OwnerDashboardQueryService's
/// own Stock section already uses), so "how low was stock AT the end of that day" cannot be answered
/// and is not claimed here.</summary>
public sealed record DailyInventoryDto(
    long PurchaseReceiptMilliUnits, long ManualReceiptMilliUnits, long SaleConsumptionMilliUnits,
    long PackagingConsumptionMilliUnits, long WasteMilliUnits, long SpoilageMilliUnits, long BreakageMilliUnits,
    long LossValuePiastres, long CountAdjustmentNetMilliUnits, long PurchasesReceivedValuePiastres,
    int LowStockCount, int NegativeStockCount, int ItemsWithoutCostCount);

/// <summary>One row from either audit source, unified for display only - nothing is migrated,
/// merged, or deleted in the underlying tables (Section 10's explicit instruction). Source names
/// which table the row actually came from, so a discrepancy can still be traced to its real origin.</summary>
public sealed record DailyAuditEntryDto(
    string Source, string Id, string Action, string Actor, string? Category, string? Details,
    string? Severity, long? AmountPiastres, string? Reason, string? ApprovedBy, DateTimeOffset CreatedAtUtc);

/// <summary>Reuses PrintJobQueryService.GetOverviewAsync's own PrintRouteStatusDto rather than
/// inventing a second per-route shape - this statement's printing section is that same overview,
/// with ReprintCount (from today's audit trail - reprints are audited, see DailyAuditEntryDto) added
/// alongside it.</summary>
public sealed record DailyPrintingDto(
    int PendingCount, int FailedCount, int StuckCount, int ReprintCount,
    IReadOnlyList<Falah.Application.Printing.PrintRouteStatusDto> Routes);

public sealed record DailyBackupDto(
    DateTimeOffset? LastVerifiedBackupAtUtc, double? LastBackupAgeHours, string? LastVerificationStatus,
    int BackupsToday, int FailedBackupsToday);
