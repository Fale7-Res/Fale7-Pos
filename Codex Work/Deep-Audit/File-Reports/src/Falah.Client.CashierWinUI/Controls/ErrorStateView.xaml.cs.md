# src/Falah.Client.CashierWinUI/Controls/ErrorStateView.xaml.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `9541e97c6c46392dd01a4e918088f04a0b17fa18a152fded206656076fea8037`; worktree SHA256 `9541e97c6c46392dd01a4e918088f04a0b17fa18a152fded206656076fea8037`; LF-equivalent `True`.

Actual role: cashier error message/retry visibility and event bridge.

## Function/resource review

- `ErrorStateView` at `src/Falah.Client.CashierWinUI/Controls/ErrorStateView.xaml.cs:7`: Initializes default message; retry defaults false/Collapsed in XAML. DP wrappers no data side effects.

- `OnMessageChanged` at `src/Falah.Client.CashierWinUI/Controls/ErrorStateView.xaml.cs:10`: Assigns message text, OnRetryVisibleChanged sets actual Button visibility; callbacks assume components initialized through normal XAML construction.

- `OnRetryClick` at `src/Falah.Client.CashierWinUI/Controls/ErrorStateView.xaml.cs:41`: Raises RetryRequested without running any API itself; subscribers own cancellation/busy guard. No command can-execute check, because event-based design intentional.

## Tests and limitations

NOT_APPLICABLE business arithmetic; need native DP/event subscriber tests at actual call sites.

## Recommendations (not implemented)

Avoid displaying retry button without subscriber; guard double clicks in subscribed command.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
