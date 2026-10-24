# src/Falah.Client.CashierWinUI/ViewModels/DailySummaryViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `b4951f4250aece845c172b95f03ff3e8c4b7f96cb41d53827bb9b158f96cd3e3`; worktree SHA256 `b4951f4250aece845c172b95f03ff3e8c4b7f96cb41d53827bb9b158f96cd3e3`; LF-equivalent `True`.

Actual role: Actual selected/latest business-day printable-style daily summary data.

## Function/resource review

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DailySummaryViewModel.cs:64`: Settings optional then current day; any current failure falls back history, not only404. Transient current error may show latest historical day while user expects today; summary displays actual BusinessDate so explicit date helps. Empty history correct no day; summary failure LoadError.

- `FormatArabicLongDate` at `src/Falah.Client.CashierWinUI/ViewModels/DailySummaryViewModel.cs:41`: Invariant ISO input parsed DateOnly, ar-EG long date; parse failure preserves raw rather than fabricate date.

- `NetShiftDifferencePiastres` at `src/Falah.Client.CashierWinUI/ViewModels/DailySummaryViewModel.cs:58`: Overage-shortage, advances+withdrawals grouped true server amounts; nullSummary0but view HasSummary gates actual sheet. No direct printing command; source explicitly management print pipeline absent, cannot claim printable/exported report working.

## Tests and limitations

Need current non404failure fallback message test and actual date label, no-day/unknown vszero. No print jobs created.

## Recommendations (not implemented)

Distinguish current-day lookup failure from genuinely closed/no day before history fallback; report no management paper pipeline honestly.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
