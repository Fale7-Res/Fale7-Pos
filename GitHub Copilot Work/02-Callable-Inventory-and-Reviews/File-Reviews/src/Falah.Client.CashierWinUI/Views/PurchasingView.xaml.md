# src/Falah.Client.CashierWinUI/Views/PurchasingView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `2d10e37764373122269320bc697876bd1d1d7705b5f0b39b1761d0eb1feff4e9`; worktree SHA256 `2a99b6deed643babf93ac4d39e883db2ac73ec2c9d297c04296c99c242e3c2c9`; LF-equivalent `True`.

Actual role: Supplier balances, payable aging and invoice settlement navigation.

## Function/resource review

- `Suppliers` at `src/Falah.Client.CashierWinUI/Views/PurchasingView.xaml:7`: 380px supplier pane with names/outstanding server amount and receive command. No supplier select needed for invoices list; no search/paging in markup. Current supplier balance not recomputed in view.

- `PayableBuckets` at `src/Falah.Client.CashierWinUI/Views/PurchasingView.xaml:100`: Horizontal nonwrapping aging buckets plus worst-overdue rows, explicitly separates unspecified due date from overdue. Entire summary autoheight can push invoice table below usable viewport; no outer summary scroll.

- `Purchases` at `src/Falah.Client.CashierWinUI/Views/PurchasingView.xaml:86`: Fixed620px invoice columns plus action beside380px supplier pane; width pressure at1366/largeDPI. Vertical-only ItemsControl eager rows, large amount fields do not wrap.

- `OpenDueDateCommand` at `src/Falah.Client.CashierWinUI/Views/PurchasingView.xaml:205`: Date button shows actual date or explicit no-date text and CanManage gate. Surrendering date has dedicated modal, not accidental blank formatting.

- `OpenPayCommand` at `src/Falah.Client.CashierWinUI/Views/PurchasingView.xaml:209`: Pay visible positive remaining; model must authorize and retain actual payment identity. Loading/error overlay only invoice list, stale supplier/aging snapshot can remain during refresh.

- `ActionNote` at `src/Falah.Client.CashierWinUI/Views/PurchasingView.xaml:236`: White toast on themed TextPrimary repeats dark contrast root. PurchasingModals declared last inherit sameVM and overlay page. No actual supplier/stock/treasury write executed.

## Tests and limitations

NOT_EXECUTED. Need aged buckets plus long invoice list under small viewport, null due date, pending payment restart, partial read failure and dark feedback. No callable declarations.

## Recommendations (not implemented)

Adapt columns/aging layout and show snapshot freshness. Preserve unspecified date versus real overdue semantics.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
