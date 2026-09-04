namespace Falah.Infrastructure.Persistence.Entities;

public class SupplierEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string Category { get; set; } = default!;
    public bool Active { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Ported from Purchase in the reference app: a received supplier invoice, its lines, and
/// its own running payment state. Total/Paid/RemainingAmount are kept as their own columns (not
/// derived on every read) because the reference app itself persists them this way and every command
/// that changes them (receivePurchase, payPurchase) recomputes them from the ledger at the moment of
/// the write - the ledger (SupplierLedgerEntryEntity) remains the true source of "how much is
/// outstanding," these are a convenient, always-in-sync cache of it.</summary>
public class PurchaseEntity
{
    public string Id { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public string SupplierId { get; set; } = default!;
    public string? InvoiceNumber { get; set; }
    public long SubtotalPiastres { get; set; }
    public long TotalPiastres { get; set; }
    public long PaidPiastres { get; set; }
    public long RemainingAmountPiastres { get; set; }
    /// <summary>One of cash/instapay/bank/credit - "credit" (آجل) means nothing was paid at receipt
    /// time; the invoice is carried entirely on the supplier ledger until a later payment.</summary>
    public string PaymentMethod { get; set; } = default!;
    /// <summary>The real treasury account the INITIAL payment (if any) was drawn from - null when
    /// PaymentMethod is credit or Paid is zero. A purchase can later accumulate further payments
    /// (see PurchasePaymentEntity) against different accounts; this column only records the first.</summary>
    public string? PaymentAccountId { get; set; }
    /// <summary>draft/received/partially_paid/paid/cancelled - mirrors PaymentStatus except a
    /// fully-unpaid invoice reads "received", not "unpaid" (there is no distinct draft state used by
    /// this system's checkout-free receiving flow).</summary>
    public string Status { get; set; } = default!;
    public string PaymentStatus { get; set; } = default!;
    public string ReceivedBy { get; set; } = default!;
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = default!;
    public string? OperationId { get; set; }
    /// <summary>The treasury movement for the INITIAL payment at receipt time only - null when
    /// nothing was paid then. Later payments each carry their own movement id on their own
    /// PurchasePaymentEntity row instead.</summary>
    public string? TreasuryMovementId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public List<PurchaseLineEntity> Lines { get; set; } = new();
    public List<PurchasePaymentEntity> Payments { get; set; } = new();
}

public class PurchaseLineEntity
{
    public int Id { get; set; }
    public string PurchaseId { get; set; } = default!;
    public string InventoryItemId { get; set; } = default!;
    public long QuantityMilliUnits { get; set; }
    public long UnitCostPiastres { get; set; }
    public long LineTotalPiastres { get; set; }
}

/// <summary>One payment against a purchase invoice - the initial payment made at receipt time (if
/// any) gets a row here too, exactly like every later payPurchase call, so a purchase's full payment
/// history is always readable from one place.</summary>
public class PurchasePaymentEntity
{
    public string Id { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public string PurchaseId { get; set; } = default!;
    public string SupplierId { get; set; } = default!;
    public long AmountPiastres { get; set; }
    public string PaymentAccountId { get; set; } = default!;
    public string TreasuryMovementId { get; set; } = default!;
    public string OperationId { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public string? ApprovedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Ported from SupplierLedgerEntry - the supplier's own running account, debited by every
/// invoice received and credited by every payment made against it. SupplierBalance/outstanding are
/// always derived from this table (PurchasingRules.SupplierOutstandingFromLedger), never stored.</summary>
public class SupplierLedgerEntryEntity
{
    public string Id { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public string SupplierId { get; set; } = default!;
    /// <summary>purchase_invoice (debit) or supplier_payment (credit) - the only two types this
    /// system posts; supplier_return/adjustment exist in the reference app's type union but have no
    /// command that produces them yet (no supplier-return flow in this pass).</summary>
    public string Type { get; set; } = default!;
    public long DebitPiastres { get; set; }
    public long CreditPiastres { get; set; }
    public string? PurchaseId { get; set; }
    public string? ReferenceId { get; set; }
    public string Reason { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public string? OperationId { get; set; }
    public string? TreasuryMovementId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
