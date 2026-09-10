namespace Falah.Application.Purchasing;

/// <summary>Ported from addSupplier. Category/Notes are free text, matching the reference app's own
/// PurchasingScreen form exactly (address is not collected there either).</summary>
public sealed record AddSupplierRequest(
    string RequestId, string Name, string? Phone, string? Address, string? Category, string? Notes,
    string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record AddSupplierResult(string SupplierId, bool WasReplayed);

public sealed record SupplierDto(
    string Id, string Name, string Phone, string Address, string Category, bool Active, string? Notes,
    long TotalInvoicedPiastres, long TotalPaidPiastres, long OutstandingBalancePiastres);

public sealed record PurchaseLineRequest(string InventoryItemId, long QuantityMilliUnits, long UnitCostPiastres);

/// <summary>Ported from receivePurchase (PurchaseCommandInput). PaymentMethod is one of
/// cash/instapay/bank/credit - "credit" (آجل) means Paid must be zero, the whole invoice starts life
/// entirely outstanding on the supplier ledger. ActorUserId is both the authorizing actor and the
/// person who physically received the goods - the reference app's own UI never distinguishes these
/// (receivedBy is always the acting cashier/manager's own name), so this system resolves both from
/// the one server-verified actor rather than trusting a separately client-supplied name.</summary>
public sealed record ReceivePurchaseRequest(
    string RequestId, string SupplierId, string? InvoiceNumber, IReadOnlyList<PurchaseLineRequest> Lines,
    long PaidPiastres, string PaymentMethod, string ActorUserId, string? Notes,
    string? ManagerApprovalId, string? ManagerApproverUserId,
    /// <summary>When the invoice is due. Optional, and left null when no term was agreed - the
    /// system never invents one, because an invented due date would produce invented overdue.</summary>
    DateOnly? DueDate = null);

/// <summary>Sets or clears the due date on an invoice already on the books - terms are often agreed
/// after the goods arrive. Deliberately its own small command: it touches no money, so it carries
/// none of the treasury machinery receiving and paying do.</summary>
public sealed record SetPurchaseDueDateRequest(
    string RequestId, string PurchaseId, DateOnly? DueDate, string ActorUserId);

/// <summary>Ported from payPurchase (PurchasePaymentCommandInput) - a later settlement against a
/// purchase already on the books. PaymentMethod excludes "credit": a real payment always moves real
/// money out of a real treasury account.</summary>
public sealed record PayPurchaseRequest(
    string RequestId, string PurchaseId, long AmountPiastres, string PaymentMethod,
    string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record PurchaseLineDto(string InventoryItemId, long QuantityMilliUnits, long UnitCostPiastres, long LineTotalPiastres);

public sealed record PurchasePaymentDto(
    string Id, long AmountPiastres, string PaymentAccountId, string? ApprovedBy, DateTimeOffset CreatedAtUtc);

public sealed record PurchaseDto(
    string Id, string SupplierId, string? InvoiceNumber, IReadOnlyList<PurchaseLineDto> Lines,
    long SubtotalPiastres, long TotalPiastres, long PaidPiastres, long RemainingAmountPiastres,
    string PaymentMethod, string? PaymentAccountId, string Status, string PaymentStatus,
    string ReceivedBy, DateTimeOffset CreatedAtUtc, IReadOnlyList<PurchasePaymentDto> Payments, bool WasReplayed = false,
    string? DueDate = null);

/// <summary>One unpaid invoice, placed in an aging bucket.</summary>
/// <param name="DaysOverdue">Null when the invoice has no due date - not zero. Zero means due but
/// not yet late.</param>
public sealed record PayableDto(
    string PurchaseId, string SupplierId, string SupplierName, string? InvoiceNumber,
    long RemainingPiastres, string ReceivedOnBusinessDate, string? DueDate,
    int AgeInDays, int? DaysOverdue, string Bucket, string BucketLabel);

public sealed record PayablesBucketDto(string Bucket, string Label, int InvoiceCount, long TotalPiastres);

/// <param name="OverduePiastres">Only invoices with an agreed due date that has passed. Invoices
/// without a due date are counted in UnscheduledPiastres instead, never silently as overdue.</param>
public sealed record PayablesAgingDto(
    string AsOfBusinessDate,
    long TotalOutstandingPiastres,
    long OverduePiastres,
    int OverdueInvoiceCount,
    long UnscheduledPiastres,
    int UnscheduledInvoiceCount,
    IReadOnlyList<PayablesBucketDto> Buckets,
    IReadOnlyList<PayableDto> Invoices);

public sealed record SupplierLedgerEntryDto(
    string Id, string SupplierId, string Type, long DebitPiastres, long CreditPiastres,
    string? PurchaseId, string Reason, DateTimeOffset CreatedAtUtc, string CreatedBy);

public sealed class PurchasingConflictException : Exception
{
    public PurchasingConflictException(string message) : base(message) { }
}
