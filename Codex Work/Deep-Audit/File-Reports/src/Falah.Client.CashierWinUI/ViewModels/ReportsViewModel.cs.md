# src/Falah.Client.CashierWinUI/ViewModels/ReportsViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `ebf6a9f85068ce2a86d389e35c3f08646e0e4b11a44f699e2d8f09ab9a989dab`; worktree SHA256 `17626c2a401c42d2abfd3ee72d3ce305180871f1dd24ba0f495691de6ef6daf1`; LF-equivalent `True`.

Actual role: Read-only multi-endpoint management report compositor.

## Function/resource review

- `ReportsViewModel(` at `src/Falah.Client.CashierWinUI/ViewModels/ReportsViewModel.cs:21`: Initial date is Cairo; period defaults today without triggering generated setter. Specific-day bridge uses UTC offset only as calendar carrier.

- `OnSelectedPeriodChanged(` at `src/Falah.Client.CashierWinUI/ViewModels/ReportsViewModel.cs:59`: Notifies range/day picker and launches reload unless ShowDay suppresses duplicate. Rapid changes coalesce via reload flag rather than cancelling network reads.

- `ShowDay(` at `src/Falah.Client.CashierWinUI/ViewModels/ReportsViewModel.cs:72`: Switches to day with try/finally suppression, updates date offset and label then starts read. PreviousDay/NextDay both delegate here.

- `ResolveRange(` at `src/Falah.Client.CashierWinUI/ViewModels/ReportsViewModel.cs:90`: Inclusive ranges cover today/yesterday/7/30/month or selected date using shared BusinessClock; no money calculation.

- `OnFoodCostChanged(` at `src/Falah.Client.CashierWinUI/ViewModels/ReportsViewModel.cs:169`: Recomputes incomplete count and current recipe/current ingredient cost disclosure. Unknown line costs stay distinct from zero.

- `BuildProductHours(` at `src/Falah.Client.CashierWinUI/ViewModels/ReportsViewModel.cs:201`: Sorts active hours, groups by product name, sums quantity, picks earliest tied peak and caps 15 rows. Dictionary requires one row per product/hour; corroborated ReportingQueryService groups exactly that key, rejecting duplicate-row candidate for current backend. Display dots denote zero, not absent cost.

- `LoadAsync(` at `src/Falah.Client.CashierWinUI/ViewModels/ReportsViewModel.cs:64`: Sequentially loads sales, expense, top, hourly, categories, delivery, tax, refunds, food cost, actions. Failure keeps partial old/new fields but HasData requires no error, hiding successful data region. finally executes requested reload while retaining loading cover. No stale-range early discard, but cover remains through queued reload. Session user asserted nonnull; no cancellation. No financial writes.

- `BusinessDateOffset` at `src/Falah.Client.CashierWinUI/ViewModels/ReportsViewModel.cs:83`: Setter calls ShowDay only for changed calendar day. BusinessDateLabel notified by generated hook; current range not updated by direct BusinessDate setter outside bridge.

## Tests and limitations

NOT_EXECUTED. Test rapid date changes, every intermediate endpoint failure, tied peaks and empty/missing costs. Product/hour duplicate exception is not established with actual backend grouping.

## Recommendations (not implemented)

Retain explicit unknown-cost basis. Consider cancellation/session generation and atomic report snapshot to simplify failure handling.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
