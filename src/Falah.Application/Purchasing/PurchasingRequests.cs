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
    string? ManagerApprovalId, string? ManagerApproverUserId);

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
    string ReceivedBy, DateTimeOffset CreatedAtUtc, IReadOnlyList<PurchasePaymentDto> Payments, bool WasReplayed = false);

public sealed record SupplierLedgerEntryDto(
    string Id, string SupplierId, string Type, long DebitPiastres, long CreditPiastres,
    string? PurchaseId, string Reason, DateTimeOffset CreatedAtUtc, string CreatedBy);

public sealed class PurchasingConflictException : Exception
{
    public PurchasingConflictException(string message) : base(message) { }
}
