# src/Falah.Client.CashierWinUI/ViewModels/InventoryViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `f4b10e775cc1d438dd99f57a7e3bb9368fe412822c7cc82f8046685bc8aa95e8`; worktree SHA256 `98aff22180ad51c50fed3c9bc5a3e3a5cffd2f233d026b6bf8f774600d5e6d74`; LF-equivalent `True`.

Actual role: Stock overview/detail/physical count sheets/single count/waste money-sensitive UI.

## Function/resource review

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/InventoryViewModel.cs:98`: Real stock overview+last60movements+selected detail; single busy guard, no cancellation, snapshots values from backend. Unknown value caveat and negative balance warning correctly avoid zero/moral accusation; NameOf/UnitOf linear lookup per60movements O(items*60).

- `LoadDetailAsync` at `src/Falah.Client.CashierWinUI/ViewModels/InventoryViewModel.cs:122`: SelectedItem snapshot row but no generation/identity after await; old detail can publish under new/closed selection and set IsDetailLoading false while next read pending. Only reports_view allowed cost detail, inventory_manage without reports cannot open detail; intentional value gate but can still count/waste.

- `MovementRow` at `src/Falah.Client.CashierWinUI/ViewModels/InventoryViewModel.cs:120`: Displays actual direction/quantity/value/cost provenance/reference/actor; unknown monetary value dash. Formats station-local timestamp instead of Cairo; presentation only.

- `CountLine` at `src/Falah.Client.CashierWinUI/ViewModels/InventoryViewModel.cs:313`: Observed quantity nullable on invalid/blank, variance=typed minus cachedsystem, unchecked long difference/valuation; physical count difference based cached snapshot can age while sales continue. Per-line Changed calls parent counters; no durable row state itself.

- `RebuildCountSheet` at `src/Falah.Client.CashierWinUI/ViewModels/InventoryViewModel.cs:68`: Copies only currently visible typed dictionary then rebuilds from filtered Items. Hiding shelfAforBthenbackA loses typedA because hidden rows no longer saved; contradicts keep figures intent. After Load refresh rebuild also recalculates baselines.

- `SubmitCountSheetAsync` at `src/Falah.Client.CashierWinUI/ViewModels/InventoryViewModel.cs:437`: Direct per-line RecordCountAdjustment with deterministic sheet-item ID, no RetainedRequest or immutable whole-sheet capture; one approval shared all lines, backend token reuse needs handler check. First fail/unknown does not register persistent unknown; after summary Load refresh recomputes differences against latest balances and potentially changes retry payload for same IDs. Cancel/navigation allowed once IsBusy false. Successful lines and failures not atomically submitted, intentional partial batch must be explicit/recoverable.

- `SubmitCountAsync` at `src/Falah.Client.CashierWinUI/ViewModels/InventoryViewModel.cs:544`: Single-item actual amount validates MilliUnits/reason, computes difference from snapshot, real approval, durable typed request; retry branch before CountTarget required. Native recovered modal not restored in constructor so actual retry button reachability unverified.

- `SubmitWasteAsync` at `src/Falah.Client.CashierWinUI/ViewModels/InventoryViewModel.cs:639`: Typed waste positive quantity/reason, known type choice UI, real approval; durable request preserves quantity and ID, server returned nullable loss value accurately worded unknown. Close/open mutation guarded by CanEditAction; session pending initially only IsBusy change.

- `ResolveApprovalAsync` at `src/Falah.Client.CashierWinUI/ViewModels/InventoryViewModel.cs:459`: Single actual approval endpoint, no forged client identity accepted. Business concurrency/transactions server owns. Number overflow risk on extreme inputs not runtime reproduced.

## Tests and limitations

Need filter-away/back count draft preservation; partial sheet commit/lost-response/reload/concurrent-sale immutable retry; count/waste reconstructed native recovery; detail A/B stale read and extreme valuation. No stock writes.

## Recommendations (not implemented)

Durable exact per-line outcome journal/whole sheet coordinator, preserve hidden count inputs by item ID, compare authoritative current balance explicitly, version detail requests and safe bound arithmetic.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
