# src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `33b471f584fbd6eb5621807b28feaea76c127863f24f69ef399a016bb52bc14e`; worktree SHA256 `f3757fffbad1e70c6b4669f50d50f696c0043aa305821a7a540330704594a21f`; LF-equivalent `True`.

Actual role: Supplier directory, invoice receiving/payment/due dates and payable aging.

## Function/resource review

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:49`: Real suppliers,purchases,active stock,optional overview currentcost then aging sequential. No request cancellation; errors can leave earlier sections updated and later stale. CanAddSupplier OR purchasing/suppliers permission; server final auth.

- `LoadPayablesAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:86`: Failure silently retains old Aging/Payables or empty panel while other lists valid. Unlike comments safety wish, operator cannot distinguish unavailable aging from clean/no late invoices; source risk needs UI report unavailable state.

- `SubmitDueDateAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:167`: Invariant yyyy-MM-dd or blank null, fresh Guid every submit; no durable outcome identity. Idempotent value setter, less duplicate-money risk, but can abandon uncertainty and overwrite newer date if retry stale.

- `SubmitSupplierAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:229`: Mandatory name, real supplier approval if neither permission; typed durable request ID captured once, success reload. Pending request can retry without dialog but native recovery state IsSupplierOpen false not hydrated.

- `PurchaseLineEntry` at `src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:281`: Selected item lookup prior unit cost; change text notifies10xwarning. Multiplying longprevious/typed by10unchecked can overflow extreme valid money values and suppress warning. Display help actual source cost, not invented.

- `SubmitReceiveAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:438`: Pending exact DTO first, fresh lines parse positive milli quantity/unitcost, skips item-null rows even if user typed amount, requires nonempty. Suspicious cost acknowledgement not reset when line changes after acknowledged, so later big change may remain implicitly acknowledged. Valid optional due date; credit sends0paid. Approval captured server, immutable ReceivePurchaseRequest sent once per modal and retained before network.

- `SubmitPayAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:556`: Pending exact typedPayPurchase first; positive amount, no local cap vs shown remaining (server must enforce). Stable per-modal ID and actual chosen method/approval; result success amount from request not server DTO, contract expected same paid amount. Server multi-row ledger/treasury transaction separate.

- `CompleteReceiveAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:444`: Authoritative total in note; closes and reloads. Supplier/receive/pay all3helpers durable but initial session pending only busy change and dialogs not reconstructed, a broad recovery UI gap requiring native button visibility check. All PIN fields remain until next open rather than clear on close.

## Tests and limitations

Need real-VM reconstructed supplier/receive/pay retry UI, cost-warning acknowledgment reset, empty-item typed row warning, stale aging error, partial read refresh and payment cap contracts. No supplier/invoice/payment records written.

## Recommendations (not implemented)

Make stale aging explicitly unavailable, reset acknowledgement on changed suspicious input, hydrate original pending action visible and protected; retain backend atomic receiving/settlement.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
