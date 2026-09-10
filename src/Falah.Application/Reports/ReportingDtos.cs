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
    long DineInPiastres, long TakeawayPiastres, long DeliveryPiastres);

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

public sealed record TopSellingItemDto(string ProductName, int Quantity, long TotalPiastres);

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
    SalesReportDto Sales, long ExpensesTotalPiastres);

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
