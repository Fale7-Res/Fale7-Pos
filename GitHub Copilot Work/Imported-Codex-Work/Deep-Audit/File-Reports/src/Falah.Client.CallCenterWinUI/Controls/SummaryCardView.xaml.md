# src/Falah.Client.CallCenterWinUI/Controls/SummaryCardView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `899712123fecfef6ade0eb50d7e40ece68b9c7a264dfb3dc6d52babc67ccb0a5`; worktree SHA256 `899712123fecfef6ade0eb50d7e40ece68b9c7a264dfb3dc6d52babc67ccb0a5`; LF-equivalent `True`.

Actual role: CallCenter KPI label/value/optional caption card.

## Function/resource review

- `SummaryCardStyle` at `src/Falah.Client.CallCenterWinUI/Controls/SummaryCardView.xaml:12`: Card border and stacked label/metric/caption. Metric/source is supplied by callers, not invented here. Caption starts Collapsed; no financial arithmetic.

- `ValueBlock` at `src/Falah.Client.CallCenterWinUI/Controls/SummaryCardView.xaml:15`: Large MetricValueText suitable glance reading but no TextWrapping/trim, so long formatted currency may exceed fixed parent cell. Native long-number/DPI check needed; no rendered claim.

## Tests and limitations

NOT_APPLICABLE ledger/authorization; need unknown-vs-zero and long localized value display cases at parent screen.

## Recommendations (not implemented)

Preserve unknown/null display contracts, avoid treating missing value as zero in caller.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
