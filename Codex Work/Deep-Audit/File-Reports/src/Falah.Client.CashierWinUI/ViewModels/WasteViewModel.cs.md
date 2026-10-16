# src/Falah.Client.CashierWinUI/ViewModels/WasteViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `328fe06abd957b62551b342bb112b16248e5f289feece90b233b09b4706aa069`; worktree SHA256 `328fe06abd957b62551b342bb112b16248e5f289feece90b233b09b4706aa069`; LF-equivalent `True`.

Actual role: Per-day waste/spoilage/breakage summary and movement list.

## Function/resource review

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/WasteViewModel.cs:53`: Summary requested with BusinessDate, but movements fetch last200global and filter only Type, never businessdate. Thus date-specific totals paired with arbitrary all-date movement list; relevant rows can be omitted due global cap. Source-confirmed misleading report, backend endpoint date query not passed.

- `PreviousDay` at `src/Falah.Client.CashierWinUI/ViewModels/WasteViewModel.cs:89`: Changes date then calls Load; NextDay same. If existing read busy, LoadAsync returns and no queued reload/version. Slow old-date response assigned under new date, source stale-read defect.

- `HasNoLoss` at `src/Falah.Client.CashierWinUI/ViewModels/WasteViewModel.cs:42`: NullSummarytreated0only when noerror/notloading. ByType actual server values, labels preserve3loss classes and unknownvalue handling in XAML not this VM. Constructor Cairo current date correct.

## Tests and limitations

Need delayed date flip and multi-date globalmovement fixture: all displayed rows must match selected Cairo business range and full relevant result. No waste writes (read-only screen).

## Recommendations (not implemented)

Query loss movements by exact businessday/range with paging, coalesce/version date changes like Audit/Attendance. Do not infer displayed list explains total until verified.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
