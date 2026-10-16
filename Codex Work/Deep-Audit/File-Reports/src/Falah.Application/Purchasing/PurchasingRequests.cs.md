# src/Falah.Application/Purchasing/PurchasingRequests.cs

Purchasing API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `e3cdc2188250bc49000e47a82ec855cf35c80a805cddfa6b004e9a9ac0ebd12d`. Working SHA-256 `df5e41e3bdf5ffa195d24c3802d58d24795eecce197195f3e87ec308d208567b`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-85.

Symbols indexed (index is not a claim every method was reviewed): AddSupplierRequest@5, AddSupplierResult@9, SupplierDto@11, PurchaseLineRequest@15, ReceivePurchaseRequest@23, SetPurchaseDueDateRequest@34, PayPurchaseRequest@40, PurchaseLineDto@44, PurchasePaymentDto@46, PurchaseDto@49, PayableDto@59, PayablesBucketDto@64, PayablesAgingDto@68, SupplierLedgerEntryDto@78, PurchasingConflictException@82.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 5: `public sealed record AddSupplierRequest(`
- Line 9: `public sealed record AddSupplierResult(string SupplierId, bool WasReplayed);`
- Line 11: `public sealed record SupplierDto(`
- Line 15: `public sealed record PurchaseLineRequest(string InventoryItemId, long QuantityMilliUnits, long UnitCostPiastres);`
- Line 23: `public sealed record ReceivePurchaseRequest(`
- Line 34: `public sealed record SetPurchaseDueDateRequest(`
- Line 40: `public sealed record PayPurchaseRequest(`
- Line 44: `public sealed record PurchaseLineDto(string InventoryItemId, long QuantityMilliUnits, long UnitCostPiastres, long LineTotalPiastres);`
- Line 46: `public sealed record PurchasePaymentDto(`
- Line 49: `public sealed record PurchaseDto(`
- Line 59: `public sealed record PayableDto(`
- Line 64: `public sealed record PayablesBucketDto(string Bucket, string Label, int InvoiceCount, long TotalPiastres);`
- Line 68: `public sealed record PayablesAgingDto(`
- Line 78: `public sealed record SupplierLedgerEntryDto(`
- Line 84: `public PurchasingConflictException(string message) : base(message) { }`

Receiving, later payment and due-date editing use separate request contracts. Piastre costs and milli-unit quantities need checked arithmetic in domain. Invoice total, supplier obligations and treasury posting must be atomic in handlers; DTO snapshots are not independently authoritative.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
