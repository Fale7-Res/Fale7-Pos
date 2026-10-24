# src/Falah.Client.CashierWinUI/Views/CashierOperationsModalsView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `19cc59cf773d433025fd8c4084d275f3d10841d0d626cc37e43dd7a3f101d1bf`; worktree SHA256 `f9c24f4c4d429ebc5c51f0a871b831c3d8eaf5d977a72b0926f3ce5016a4d5d7`; LF-equivalent `True`.

Actual role: Cashier safe drop, drawer cash movements, drawer log, balance and order lookup overlays.

## Function/resource review

- `IsSafeDropOpen` at `src/Falah.Client.CashierWinUI/Views/CashierOperationsModalsView.xaml:18`: Amount/reason/manager fields two-way immediate, disabled by shared CanEditOperation; manager PIN PasswordBox appears only approval required. Submit relies command gating, footer not independent busy field.

- `IsCashMovementOpen` at `src/Falah.Client.CashierWinUI/Views/CashierOperationsModalsView.xaml:42`: Cash in/out shares title and retained VM fields; reason and amount explicit automation IDs. Overlay itself no focus trap.

- `IsCashDrawerOpen` at `src/Falah.Client.CashierWinUI/Views/CashierOperationsModalsView.xaml:65`: Text explicitly says records a drawer request rather than confirming physical drawer opened. No direct device call in XAML.

- `IsCashCountOpen` at `src/Falah.Client.CashierWinUI/Views/CashierOperationsModalsView.xaml:87`: Read-only server ledger balance hides while loading, then displays amount even if error also present; check VM null/reset so previous balance cannot be mistaken for current. No reconciliation write here.

- `IsOrderLookupOpen` at `src/Falah.Client.CashierWinUI/Views/CashierOperationsModalsView.xaml:105`: Number/search binds VM; summary gives order status, piastres, actual split payment, table and pickup time. Items cap220 inside outer ScrollViewer, potential clipping with many lines.

- `LookupHasPendingPayment` at `src/Falah.Client.CashierWinUI/Views/CashierOperationsModalsView.xaml:178`: Pending review/refund/reprint depend separate flags and commands; reprint is physical request pathway, not print preview. No printer-free full receipt rendering present in this view.

- `CloseAutomationId` at `src/Falah.Client.CashierWinUI/Views/CashierOperationsModalsView.xaml:21`: Header close commands vary per overlay and must preserve unresolved operations in VM. Shared CanEditOperation freezes input but native keyboard accelerator isolation is not provided here.

## Tests and limitations

NOT_EXECUTED. Test pending locks, command retries, null balance/error display, many-line lookup, screen reader names of placeholder-only fields and native modal focus.

## Recommendations (not implemented)

Keep financial pending journal concerns UI001 distinct from markup. Add explicit accessible field names/focus containment and a separate receipt preview if desired later.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
