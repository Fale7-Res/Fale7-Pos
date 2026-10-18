# src/Falah.Client.CashierWinUI/Views/InventoryView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `2ae63654d2aa2d8b5d8ba6fbc3571edd5a7f33a66b078268072b4a86bbd0c132`; worktree SHA256 `7827f5daf0c56d8258754883dd4c9df76814175a01390884bc172a3375d4d81a`; LF-equivalent `True`.

Actual role: Stock overview, detailed cost/movement trace and multi-item physical count sheet.

## Function/resource review

- `StockValueCaveat` at `src/Falah.Client.CashierWinUI/Views/InventoryView.xaml:60`: Unknown-cost item count and stock-value caveat explicitly separate unpriced stock from known total. Negativebalance warning distinct from low-stock filter. Top260px search and multiple auto metric columns can leave stockvalue narrow.

- `SelectedItem` at `src/Falah.Client.CashierWinUI/Views/InventoryView.xaml:138`: Browse ListView rather than unvirtualized top ItemsControl, selection2way drives detail. Fixed480 columns plus action and380rail leave name star constrained. Nested names horizontal badge can defeat text wrapping; native unknown.

- `RecentMovements` at `src/Falah.Client.CashierWinUI/Views/InventoryView.xaml:235`: No-selection rail shows item/type/actor/quantity/time; selected detail shows costsource/asof, received/used/lost/count totals and unexplainedvariance. These are evidence-rich real DTO fields, not frontend mock balance.

- `DetailMovements` at `src/Falah.Client.CashierWinUI/Views/InventoryView.xaml:374`: Trace shows type/quantity/time/actor/reference/reason; detail load error overlay only movementarea, upperdetail may retain prior data ifVMfails to clear. Closeicon tooltip exists but no explicit AutomationProperties.Name.

- `CountSheet` at `src/Falah.Client.CashierWinUI/Views/InventoryView.xaml:33`: Physical count description explicitly encourages current search results and shelf-by-shelf filtering. Search remains available during counting. PriorVM filter-rebuild preservation candidate therefore has real user path; this markup alone cannot retain off-filter entered rows.

- `CountedText` at `src/Falah.Client.CashierWinUI/Views/InventoryView.xaml:478`: Countsheet quantity inputs have no CanEditAction/IsBusy disable, unlike singleitem modals. Reason/PIN/cancel similarly alwaysenabled; SubmitCountSheetCommand must prevent editing/abandoning in-flight per-line results. Prior batchcount directAPI/no durable unknown flow candidate not contradicted by UI.

- `CountSheetProgressLabel` at `src/Falah.Client.CashierWinUI/Views/InventoryView.xaml:435`: Per-row Result and sheet progress/value/summary communicate partial writes, no atomic-all-items promise. Error and summary occupy same footer row1 and could overlap ifbothset; VMmustclear consistently.

- `InventoryModalsView` at `src/Falah.Client.CashierWinUI/Views/InventoryView.xaml:536`: Individual count/waste overlays spanall3rows, unlike Shifts row0 issue. Darkwhite-onTextPrimarytoast repeats semanticcontrast; no actual stock adjustment performed.

## Tests and limitations

NOT_EXECUTED. Need filterA→B→A preservation, response loss after per-item commit, edit/cancel during batch, partial success+error footer, detail race and narrow layouts. No callable declarations.

## Recommendations (not implemented)

Retain immutable per-item batch requests and off-filter count drafts, register global unknown outcome, and verify real operator flow with isolated fixtures before source fixes.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
