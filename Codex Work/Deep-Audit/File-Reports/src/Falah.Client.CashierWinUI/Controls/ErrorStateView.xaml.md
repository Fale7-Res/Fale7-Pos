# src/Falah.Client.CashierWinUI/Controls/ErrorStateView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `8a81caa9248040c5133d0e461135208d777a22110be9d973987b8906380dadd5`; worktree SHA256 `c9f9121ff797745e49ab9697c4793e528c99a0c130882ff52095bef71e0443dc`; LF-equivalent `True`.

Actual role: cashier reusable API-read failure and optional retry.

## Function/resource review

- `RetryButton` at `src/Falah.Client.CashierWinUI/Controls/ErrorStateView.xaml:18`: Arabic Retry button starts Collapsed and raises click code-behind event, not automatically bound ICommand. A call site must set RetryCommandVisible and subscribe RetryRequested for action.

- `MessageBlock` at `src/Falah.Client.CashierWinUI/Controls/ErrorStateView.xaml:16`: Wrapping message plus fixed Arabic title and ThemeResource danger icon. Native screen-reader announcement/live-region absent in source; not a claim inaccessible until tested.40DIP padding in MaxWidth380 reduces available text width.

## Tests and limitations

Missing native error-retry handler reachability and screen-reader live announcement checks. NOT_APPLICABLE direct financial writes.

## Recommendations (not implemented)

Ensure each visible retry is wired and safely retries only read/request with stable identity; expose actionable safe messages.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
