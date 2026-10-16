namespace Falah.Application.Reports;

/// <summary>Read-only projections over already-persisted transactions. Every figure here is derived
/// at query time from Orders/Shifts/Expenses/TreasuryMovements/Deliveries/StockMovements - nothing
/// is stored as a separately-maintained running total, and no report invents a number that is not
/// traceable back to one of those tables (see ReportingQueryService's own doc comment).</summary>
/// <summary>CashWithDriversPiastres is cash-on-delivery money the restaurant does not hold yet -
/// still with the customer, or in a driver's custody after delivery but before settlement. It is
/// excluded from CollectedSalesPiastres for the same reason PendingElectronicSalesPiastres is, and
/// reported separately so it is visible rather than silently missing.</summary>
public sealed record SalesReportDto(
    int OrderCount, long GrossSalesPiastres, long RefundsPiastres, long NetSalesPiastres, long ListSalesPiastres,
    long OffersValuePiastres, long CompsValuePiastres, long ManualDiscountsPiastres, long PendingElectronicSalesPiastres,
    long CashWithDriversPiastres, long CollectedSalesPiastres, long AverageOrderValuePiastres,
    long CashPiastres, long CardPiastres, long InstaPayPiastres, long WalletPiastres,
    long PickupPiastres, long TakeawayPiastres, long DeliveryPiastres);

public sealed record ExpenseReportDto(long TotalPiastres, IReadOnlyList<Falah.Application.Expenses.ExpenseDto> Expenses);

public sealed record TaxReportDto(long GrossPiastres, long TaxablePiastres, long TaxPiastres, long RefundTaxImpactPiastres, long NetTaxPiastres);

public sealed record RefundActivityDto(
    string OrderId, string? OrderNumber, long AmountPiastres, string PaymentMethod, string Reason,
    string CreatedBy, string AuthorizedBy, DateTimeOffset CreatedAtUtc, string OriginalShiftId, string ProcessingShiftId);

/// <summary>The read-only equivalent of the reference app's reconcileBusinessDay - "where is the
/// money" for one business day, from the SAME persisted movements/orders/shifts every other screen
/// reads. Deliberately excludes purchasing/payroll sub-sections the reference app's version has:
/// neither Suppliers/Purchases nor Payroll exist in this system yet (see IMPLEMENTATION_MATRIX.md).</summary>
public sealed record TreasuryBalancesDto(long MainPiastres, long DrawerPiastres, long BankPiastres, long InstaPayPiastres, long WalletPiastres);

public sealed record BusinessDayReconciliationDto(
    string BusinessDayId, string BusinessDate, SalesReportDto Sales, long ExpensesTotalPiastres,
    TreasuryBalancesDto Treasury, long DeliveryExpectedCustodyPiastres, long DeliverySettledCustodyPiastres,
    int ShiftCount, long ShiftsExpectedCashPiastres, long ShiftsActualCashPiastres, long ShiftsShortagePiastres, long ShiftsOveragePiastres,
    long PurchasingPaidPiastres = 0);

/// <summary>Employee-ledger side of the reference's DailySummaryScreen, summed across every employee
/// for one business day. DailyWage/Overtime are money owed to staff (their Credit side); Advance/
/// Withdrawal are cash already paid out against that balance (their Debit side) - the same asymmetry
/// EmployeesViewModel's own cash-out already surfaces rather than nets away.</summary>
public sealed record EmployeeLedgerTotalsDto(long DailyWagePiastres, long OvertimePiastres, long AdvancePiastres, long WithdrawalPiastres);

/// <summary>Ported from DailySummaryScreen.tsx - the printable end-of-day sheet. Reuses
/// GetBusinessDayReconciliationAsync's own sales/expenses/shift-discrepancy figures rather than
/// re-deriving them, and adds only what that reconciliation doesn't already carry: employee-ledger
/// totals and the names of whoever actually worked a shift that day (for the signature block).</summary>
public sealed record DailySummaryDto(
    string BusinessDayId, string BusinessDate, SalesReportDto Sales, long ExpensesTotalPiastres,
    long ShiftsShortagePiastres, long ShiftsOveragePiastres, EmployeeLedgerTotalsDto EmployeeLedger,
    IReadOnlyList<string> CashierNames);

public sealed record TopSellingItemDto(string ProductName, int Quantity, long TotalPiastres);

/// <summary>One product's sales inside one hour of the Cairo clock. The two questions the reports
/// screen could already answer separately - what sold, and when the day was busy - only cross here:
/// TopSellingItemDto has no time in it and the dashboard's hourly trend has no product in it, so
/// neither could say that the kofta goes at nine and the pastries go at eight.
/// <para>Hour is 0-23 on a Cairo wall clock, the same bucketing the dashboard trend uses. Only
/// hours that actually sold something appear - the view fills the rest with zero, because an
/// absent row and a zero row mean the same thing here and one of them costs nothing to send.</para>
/// </summary>
public sealed record ProductHourlySalesDto(string ProductName, int Hour, int Quantity, long TotalPiastres);

public sealed record CategorySalesDto(string CategoryId, string CategoryName, int Quantity, long TotalPiastres);

/// <summary>Total cost of goods sold for a period, resolved from sold items' CURRENT recipe cost -
/// same "unknown cost is not zero" principle as CostingRules: an item whose recipe has any
/// incomplete-cost ingredient is named in Missing, never silently folded into the total as zero.</summary>
public sealed record FoodCostReportDto(long TotalFoodCostPiastres, int SoldLineCount, int KnownLineCount, IReadOnlyList<string> Missing);

/// <summary>One row per manager-authorized or otherwise noteworthy action, unioned from every
/// table that already records CreatedBy/ApprovedBy on itself - not a second, separately-maintained
/// audit log (see ReportingQueryService's own doc comment).</summary>
public sealed record SignificantActionDto(
    string Category, string Type, long? AmountPiastres, string Actor, string? Approver, string? Reason,
    DateTimeOffset CreatedAtUtc, string ReferenceId);

public sealed record ShiftSummaryDto(
    string ShiftId, string ShiftName, string OpenedByUserId, string Status, long OpeningCashPiastres,
    long? ExpectedCashPiastres, long? ActualCashPiastres, long? CashDifferencePiastres,
    SalesReportDto Sales, long ExpensesTotalPiastres,
    /// <summary>Added for the Shifts history screen, which - unlike the single-shift report this
    /// DTO originally served - has no other way to show "who opened it" / "who closed it" or when.
    /// Trailing and optional so the one existing caller (GetShiftSummaryAsync's own single-shift
    /// report) keeps compiling unchanged.</summary>
    string? OpenedByUserName = null, string? ClosedByUserName = null,
    DateTimeOffset OpenedAtUtc = default, DateTimeOffset? ClosedAtUtc = default,
    string? DifferenceReason = null);

/// <summary>Ported from deliveryReport in lib/delivery-domain.mjs. SettlementCount replaces
/// legacy's own `trips` count - this system's per-order delivery model has no separate "trip"
/// concept (a driver settlement is itself the closest scoped unit here), so counting settlements in
/// range is the honest analog rather than inventing a trip abstraction that doesn't exist.
/// AverageDeliveryMinutes is null (not zero) when no delivery in range has both timestamps, matching
/// legacy's own `durations.length ? ... : undefined`.</summary>
public sealed record DeliveryOutcomesReportDto(
    int DeliveredCount, int FailedCount, int ReturnedCount, long DeliveryFeesPiastres,
    int UnsettledCustodyCount, long UnsettledCustodyExpectedPiastres,
    long? AverageDeliveryMinutes, long SettledCashPiastres, long ShortagesPiastres, long OveragesPiastres, int SettlementCount);
