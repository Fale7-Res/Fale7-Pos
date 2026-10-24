# src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.Statistics.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `c733c76299e43f14180ad5498feacf350f9b5ce5cffd5d0ecfc1400664dacd20`; worktree SHA256 `c733c76299e43f14180ad5498feacf350f9b5ce5cffd5d0ecfc1400664dacd20`; LF-equivalent `True`.

Actual role: Driver performance period/date query and display.

## Function/resource review

- `ResolveStatsRange` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.Statistics.cs:75`: BusinessClock.Today Cairo, last7/month/customdates sorted, translates inclusive selected dates into exact UTC start/end using BusinessClock. API escapes offsets, no accidental+tospace.

- `LoadDriverStatisticsAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.Statistics.cs:44`: Version on each request, checks before publish/finally; good stale period response guard. Need base CloseAccount to increment version to invalidate closed-driver response (not yet reviewed); no cancellation network.

- `AverageDeliveryMinutesDisplay` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.Statistics.cs:50`: Nullunknown dash, does not invent instant0; explicit last order/settlement Cairo formatting. HasStatistics based DTO presence, no financial calculation.

- `ReloadStatisticsForCurrentDriver` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.Statistics.cs:55`: Picker changes and period set trigger actual selecteddriver query; possible burst queries but only latest version published. No database writes/settlement here.

## Tests and limitations

Need close/account switch invalidates version and exact Cairo DST dateboundaries; no endpoint requests executed.

## Recommendations (not implemented)

Keep version guard and add task cancellation; validate base lifecycle clears stale statistics on close.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
