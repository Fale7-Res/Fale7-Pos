# src/Falah.Client.CashierWinUI/Views/PurchasingModalsView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `9218d624f52e7fa51cc54e29e2131fa071ad983d41023885077bcf69dde6bbb9`; worktree SHA256 `b9018817d7b00c831bc050ee8bf4696b183711e1a3b3da2a03f251a3a0ca508f`; LF-equivalent `True`.

Actual role: Supplier creation, purchase receipt/payment and due-date editing overlays.

## Function/resource review

- `IsSupplierOpen` at `src/Falah.Client.CashierWinUI/Views/PurchasingModalsView.xaml:10`: Supplier name/phone/category/notes two-way and CanEditAction disabled during unresolved write; conditional manager approval.440max640 entire card scrolls, static save label relies command pending branch.

- `ReceiveLines` at `src/Falah.Client.CashierWinUI/Views/PurchasingModalsView.xaml:65`: Receipt640max720 uses header, star scroll and large autofooter. Each row binds root inventory choices and enabled gate,110quantity/120unitcost columns plus remove44. Delete explicit accessible name; suspicious-cost note per row.

- `SetReceivePaymentMethodCommand` at `src/Falah.Client.CashierWinUI/Views/PurchasingModalsView.xaml:117`: Cash/instapay/bank/credit separate commands. Credit hides paid-now textbox and states no treasury move; DTO must actually force zero rather than retaining hidden prior amount (VM independently reviewed).

- `ReceiveDueDateText` at `src/Falah.Client.CashierWinUI/Views/PurchasingModalsView.xaml:148`: Optional due date deliberately blank rather than fabricated default; note clarifies unspecified not overdue. Numeric/date validation belongs VM, not placeholder.

- `CostShiftAcknowledged` at `src/Falah.Client.CashierWinUI/Views/PurchasingModalsView.xaml:172`: Tenfold cost warning plus explicit acknowledgement; no separate modal. Large footer includes methods/paid/date/approval/error/warning/button; at small height star item list may collapse, especially expanded warning. Native layout not measured.

- `SubmitPayCommand` at `src/Falah.Client.CashierWinUI/Views/PurchasingModalsView.xaml:228`: Invoice payment shows remaining amount and actual paymentmethod, conditional managerPIN, locks amount. No transaction executed.440max620 scroll retains action reachable if focus/viewport correct.

- `SubmitDueDateCommand` at `src/Falah.Client.CashierWinUI/Views/PurchasingModalsView.xaml:253`: Separate existing invoice due-date420 centered stack, blank removes due date explicitly. No scroll/maxheight, long error may overflow. All close/submit commands require retained outcome guarding in VM.

## Tests and limitations

NOT_EXECUTED. No supplier/stock/treasury records written. Need credit→cash transition, due-date clear, suspicious-cost footer dimensions, same-process exact retry versus fresh-process context recovery and native focus. No callable declarations.

## Recommendations (not implemented)

Constrain oversized receipt footer into scrollable layout with pinned minimum action; preserve immutable write and optional due-date semantics.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
