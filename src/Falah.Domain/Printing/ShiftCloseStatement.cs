namespace Falah.Domain.Printing;

/// <summary>
/// UI-REQ-002: everything the shop needs to know when a shift is handed over, frozen at the moment
/// the close commits. The close stores it (as JSON on the close's own audit event, in the same
/// transaction), and the screen, the printed statement and every later reprint read that frozen copy -
/// so a reprint next week states exactly what was handed over, not what the data says today
/// (a pending InstaPay confirmed after the close, for instance, would otherwise move between lines).
///
/// Amounts are integer piastres. Every sales figure is the shared definition
/// (SalesRules.ComputeSalesReport, docs/METRIC_DEFINITIONS.md) for the shift's own sales; the cash
/// figures are the drawer reconciliation the close itself used (ShiftReconciliation) and the
/// expected/counted/difference values the close persisted.
/// </summary>
public sealed record ShiftCloseStatement(
    string Kind,
    int SchemaVersion,
    string RestaurantName,
    string? SubName,
    string ShiftId,
    string ShiftReference,
    string ShiftName,
    DateOnly BusinessDate,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset ClosedAtUtc,
    string CashierName,
    string ClosedByName,
    string? ApprovedByName,
    ShiftStatementSales Sales,
    IReadOnlyList<ShiftStatementExpense> Expenses,
    ShiftStatementCash Cash,
    string? DifferenceReason,
    IReadOnlyList<string> Outstanding)
{
    public const string KindName = "shift-close-statement";
    public const int CurrentSchemaVersion = 1;

    /// <summary>Drawer-paid expenses reduce the cash the cashier hands over; expenses paid from
    /// another account are listed for completeness and do not enter the reconciliation.</summary>
    public long DrawerExpensesPiastres => Expenses.Where(e => e.PaidFromDrawer).Sum(e => e.AmountPiastres);
    public long OtherAccountExpensesPiastres => Expenses.Where(e => !e.PaidFromDrawer).Sum(e => e.AmountPiastres);
}

/// <summary>The shift's own sales, under the shared report definitions.</summary>
public sealed record ShiftStatementSales(
    int OrderCount,
    long ListSalesPiastres,
    long OffersPiastres,
    long CompsPiastres,
    long ManualDiscountsPiastres,
    long GrossSalesPiastres,
    long RefundsPiastres,
    long NetSalesPiastres,
    long CashPiastres,
    long CardPiastres,
    long InstaPayPiastres,
    long WalletPiastres,
    long PendingElectronicPiastres,
    long CashWithDriversPiastres,
    long CollectedPiastres,
    int CancelledOrderCount);

/// <summary>One expense recorded against the shift, as entered.</summary>
public sealed record ShiftStatementExpense(
    string Category,
    string Description,
    long AmountPiastres,
    bool PaidFromDrawer,
    string PaymentAccountName,
    string CreatedByName,
    DateTimeOffset CreatedAtUtc);

/// <summary>The drawer: how the expected figure was reached, what was counted, and what left the
/// drawer for the main treasury at the close.</summary>
public sealed record ShiftStatementCash(
    long OpeningPiastres,
    long CashSalesPiastres,
    long RefundsPiastres,
    long CashInPiastres,
    long DrawerExpensesPiastres,
    long WithdrawalsPiastres,
    long SafeDropsPiastres,
    long ExpectedPiastres,
    long CountedPiastres,
    long DifferencePiastres,
    long HandedOverPiastres,
    string HandoverDestinationName);
