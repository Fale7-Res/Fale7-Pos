namespace Falah.Application.Reports;

/// <summary>
/// Everything an owner needs to judge the restaurant, in one read.
///
/// This composes figures the system already produces - it introduces no new financial rule and no
/// new source of truth. Its whole reason to exist is that answering "how are we doing" used to
/// require opening five or six screens and holding the arithmetic in your head.
///
/// Two conventions run through every section:
///
/// A figure that is not known is reported as not known, never as zero. Food cost with missing
/// ingredients, a percentage of no sales, a health check that could not run - each says so.
///
/// Nothing here is a threshold or a judgement. The dashboard reports what is true; whether 34% food
/// cost is good or alarming is the owner's call, and any limit is configured, never assumed.
/// </summary>
public sealed record OwnerDashboardDto(
    string FromBusinessDate,
    string ToBusinessDate,
    int DayCount,
    CashPositionDto Cash,
    LeakageDto Leakage,
    ProfitabilityDto Profitability,
    AttentionDto Attention,
    StockPositionDto Stock,
    ObligationsDto Obligations,
    PeopleDto People,
    SystemHealthDto Health,
    int SignificantActionCount,
    DashboardTrendDto Trend);

/// <summary>Direction, which is what turns a figure into a decision. The comparable period is the
/// same span one week earlier - a Friday judged against a Friday - because yesterday compares two
/// different kinds of trading day and the ratio means nothing.</summary>
public sealed record DashboardTrendDto(
    string ComparedFromBusinessDate,
    string ComparedToBusinessDate,
    ChangeDto NetSales,
    ChangeDto Collected,
    ChangeDto CashWithDrivers,
    ChangeDto Expenses,
    ChangeDto OrderCount,
    /// <summary>"hour" for a single business date, "day" for anything longer.</summary>
    string SeriesGranularity,
    IReadOnlyList<TrendPointDto> Series);

/// <param name="Percent">Null when the earlier figure was zero. Growth from nothing has no ratio,
/// and inventing one would put the loudest number on the screen next to the emptiest fact.</param>
public sealed record ChangeDto(long Current, long Previous, long Delta, double? Percent);

public sealed record TrendPointDto(string Label, long NetSalesPiastres, int OrderCount);

/// <summary>What was sold versus what the restaurant actually holds. The distinction between the two
/// is the point: sales are not cash, and cash a driver is carrying is not the restaurant's yet.</summary>
public sealed record CashPositionDto(
    int OrderCount,
    long GrossSalesPiastres,
    long NetSalesPiastres,
    long CollectedSalesPiastres,
    long PendingElectronicSalesPiastres,
    long CashWithDriversPiastres,
    long AverageOrderValuePiastres,
    long CashPiastres,
    long CardPiastres,
    long InstaPayPiastres,
    long WalletPiastres,
    long PickupPiastres,
    long TakeawayPiastres,
    long DeliveryPiastres,
    TreasuryBalancesDto TreasuryNow,
    /// <summary>Driver custody settled into the restaurant's own treasury during the period. This
    /// is the difference between the four channel figures above and CollectedSalesPiastres: money
    /// that WAS collected, but arrived through a settlement rather than through a sale's own payment
    /// leg. Without this line an owner comparing the two totals would reasonably conclude that one
    /// of them was wrong.</summary>
    long SettledFromDriversPiastres = 0,
    /// <summary>Owner rule (2026-11-08): a driver keeps the delivery fee of each completed delivery out of the cash he
    /// collected, so the cash he hands in is less than the collected orders by those fees (less any fees the drawer
    /// paid out to him). This is the rest of the reconciling line: collected through drivers = settled + retained.</summary>
    long DriverFeesRetainedPiastres = 0);

/// <summary>The money that left without being stolen - discounts, hospitality, refunds, waste. Each
/// is legitimate on its own and dangerous in aggregate, which is why they are shown together.</summary>
public sealed record LeakageDto(
    long OffersValuePiastres,
    long CompsValuePiastres,
    long ManualDiscountsPiastres,
    long RefundsPiastres,
    int RefundCount,
    long WasteValuePiastres,
    int WasteMovementCount,
    long ExpensesPiastres,
    /// <summary>AUD-03: cash the customer did not pay on the period's delivered orders (collected below the amount due,
    /// approved). The sale stays at its price, so this is money the sales figures include and the restaurant never got.</summary>
    long DeliveryCollectionShortfallPiastres = 0,
    int DeliveryCollectionShortfallCount = 0,
    /// <summary>AUD-14: loss movements with no known cost. They are in WasteMovementCount but add nothing to
    /// WasteValuePiastres, so that value is a floor, not the waste - unknown is never shown as zero.</summary>
    int WasteUnknownValueCount = 0,
    /// <summary>Of DeliveryCollectionShortfallPiastres, the part no manager has classified yet: neither a loss nor money
    /// owed until someone decides (CollectionShortfallRules).</summary>
    long DeliveryShortfallUnresolvedPiastres = 0,
    int DeliveryShortfallUnresolvedCount = 0);

/// <param name="IsFoodCostComplete">False when some sold line had no known ingredient cost. Food
/// cost is then understated and profit overstated - the figures stay visible, labelled.</param>
public sealed record ProfitabilityDto(
    long NetSalesPiastres,
    long? KnownFoodCostPiastres,
    long? GrossProfitPiastres,
    double? FoodCostPercent,
    double? GrossProfitPercent,
    long WagesPiastres,
    /// <summary>Null when no wage entry was posted for the period at all. Zero would claim labour
    /// cost nothing, which is a different statement from nobody having recorded any.</summary>
    double? WagePercent,
    long? OperatingResultPiastres,
    double? OperatingPercent,
    int SoldLineCount,
    int KnownCostLineCount,
    bool IsFoodCostComplete,
    IReadOnlyList<string> ItemsWithUnknownCost,
    /// <summary>AUD-02: the driver's pay for the period's completed deliveries - the full delivery fee of each
    /// delivered (not returned) order among the period's sales (owner rule: the driver keeps the whole fee). The fee is
    /// inside net sales, so this is the cost that offsets it; it is deducted in OperatingResultPiastres.</summary>
    long DriverCompensationPiastres = 0,
    /// <summary>Fees of the period's delivery orders not resolved yet (still with the driver or not dispatched). Owed to
    /// a driver only if delivered, so not deducted - but the operating result is not final while this is above zero.</summary>
    long DriverCompensationPendingPiastres = 0,
    int DriverCompensationPendingCount = 0,
    /// <summary>AUD-03: the part of the period's delivery collection shortfalls a manager classified as given up (an
    /// approved discount or a write-off), deducted in OperatingResultPiastres - the sale is in revenue at its full price
    /// while this part of it will never be received. Unresolved and still-owed shortfalls are NOT deducted.</summary>
    long DeliveryCollectionShortfallPiastres = 0,
    /// <summary>Shortfalls nobody has classified: shown beside the result, never inside it - not a verified loss.</summary>
    long DeliveryShortfallUnresolvedPiastres = 0,
    /// <summary>Shortfalls classified as still owed (by the customer or the driver): a receivable, not a loss.</summary>
    long DeliveryShortfallReceivablePiastres = 0);

/// <summary>What is unfinished right now. Every entry here is something a person has to resolve, not
/// a number to read.</summary>
public sealed record AttentionDto(
    int OpenShiftCount,
    int ShiftsWithDiscrepancyCount,
    long ShiftShortagePiastres,
    long ShiftOveragePiastres,
    int UnauthorizedShiftDiscrepancyCount,
    int UnsettledCustodyCount,
    long UnsettledCustodyPiastres,
    int PendingElectronicOrderCount,
    int OpenExceptionCount,
    int CloseBlockerCount,
    bool HasOpenBusinessDay,
    /// <summary>How many business days the currently open one is behind today. Zero when it is
    /// today's, null when none is open. A day left open silently keeps stamping new work onto an
    /// old date - every daily figure, every attendance record and every close blocker attaches to
    /// a day that ended long ago, and nothing previously said so.</summary>
    int? OpenBusinessDayAgeDays = null,
    /// <summary>Ingredients whose cost basis moved by an order of magnitude or more on a received
    /// invoice during the period. Far more often a misplaced decimal than a real price move, and
    /// it silently rewrites every recipe cost and the gross margin - see
    /// PurchasingRules.DescribeUnitCostChange.</summary>
    int UnusualCostChangeCount = 0,
    /// <summary>How much money the pending electronic payments counted above are - each pending
    /// leg's own amount - and how long the oldest has waited. A count says something is unconfirmed;
    /// only the amount and the age say whether it matters today.</summary>
    long PendingElectronicPiastres = 0,
    double? OldestPendingElectronicMinutes = null,
    /// <summary>FIN-0042: delivery orders that were already PAID (card, confirmed transfer) but never
    /// reached the customer (failed or returned) and are not yet fully refunded - money the restaurant
    /// holds that the customer is owed. Visibility only; it does not block the day close.</summary>
    int PaidUndeliveredOrderCount = 0,
    long PaidUndeliveredOwedPiastres = 0);

public sealed record StockPositionDto(
    int TrackedItemCount,
    int LowStockCount,
    IReadOnlyList<LowStockItemDto> LowStockItems);

public sealed record LowStockItemDto(string Id, string Name, string Unit, long BalanceMilliUnits, long MinimumMilliUnits);

/// <summary>What the restaurant owes. Supplier outstanding is derived from the supplier ledger, the
/// same source the per-supplier screen reads, so the total can never disagree with the detail.</summary>
/// <param name="OverduePayablesPiastres">Only invoices whose agreed due date has passed. Invoices
/// with no agreed due date are counted in UnscheduledPayablesPiastres instead - they are neither
/// overdue nor on time, and reporting them as either would be an invention.</param>
public sealed record ObligationsDto(
    long SupplierOutstandingPiastres,
    int SuppliersWithBalanceCount,
    long PurchasesReceivedPiastres,
    long PurchasesPaidPiastres,
    long EmployeeBalancesOwedPiastres,
    int EmployeesWithBalanceCount,
    long OverduePayablesPiastres = 0,
    int OverduePayableCount = 0,
    long UnscheduledPayablesPiastres = 0,
    int UnscheduledPayableCount = 0,
    int OldestOverdueDays = 0);

public sealed record PeopleDto(
    int AttendanceRecordCount,
    int PresentCount,
    int LateCount,
    int AbsentCount,
    int MissingFingerprintCount,
    int TotalLateMinutes,
    long WagesPostedPiastres);

/// <param name="LastBackupAgeHours">Null when no backup has ever completed - which is itself the
/// finding, not a missing value to ignore.</param>
public sealed record SystemHealthDto(
    DateTimeOffset? LastCompletedBackupAtUtc,
    double? LastBackupAgeHours,
    string? LastBackupVerificationStatus,
    int FailedBackupCount,
    IReadOnlyList<PrintRouteHealthDto> PrintRoutes,
    /// <summary>OPS-0006: free space on the disk the backups go to (Backup:DefaultDirectory); null when
    /// that disk cannot be read. Low when under Backup:MinimumFreeMegabytes (default 5 GB) or under
    /// three times the last backup's size, whichever is larger.</summary>
    long? BackupDiskFreeBytes = null,
    bool BackupDiskLow = false);

/// <param name="LastSuccessAtUtc">The last time this route is known to have physically printed.
/// Null means it has never reported a success - a printer nobody has proved works.</param>
public sealed record PrintRouteHealthDto(
    string Route,
    int PendingCount,
    double? OldestPendingAgeMinutes,
    DateTimeOffset? LastSuccessAtUtc,
    DateTimeOffset? LastFailureAtUtc,
    string? LastError,
    int FailedJobCount);
