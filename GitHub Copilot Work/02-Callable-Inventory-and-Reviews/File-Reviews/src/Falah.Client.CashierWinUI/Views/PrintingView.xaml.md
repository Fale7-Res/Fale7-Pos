# src/Falah.Client.CashierWinUI/Views/PrintingView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `641baeb22fbefd88a07b53020f928952e499280d7ebf5aec0f63a958a1db40fa`; worktree SHA256 `27d2860676346f2a6c2e7d8f0f30fe9d583174ecc20a52c07353f480cbdc4f57`; LF-equivalent `True`.

Actual role: Read-only logical print queue/status and attention panel.

## Function/resource review

- `HeadlineState` at `src/Falah.Client.CashierWinUI/Views/PrintingView.xaml:39`: Explicitly distinguishes queued software command from actual paper printed. No reprint, retry, test print or physical printer submission command in this view.

- `Queues` at `src/Falah.Client.CashierWinUI/Views/PrintingView.xaml:40`: Horizontal StackPanel with each route min230px plus12gap, no wrap/horizontal ScrollViewer. Four current routes already require956px before card padding; native overflow at narrow workspace likely but unobserved. Six-route VM omission UI009 separate.

- `SelectRouteCommand` at `src/Falah.Client.CashierWinUI/Views/PrintingView.xaml:50`: Route ID passed to root command; button displays pending/state/waiting/last success, combining textual state with color rather than color alone.

- `RouteJobs` at `src/Falah.Client.CashierWinUI/Views/PrintingView.xaml:113`: Order/status/age/error list uses fixed410px columns plus error star alongside fixed380px attention pane. Source layout width pressure severe at small terminal sizes; native measurement needed.

- `HasNoPending` at `src/Falah.Client.CashierWinUI/Views/PrintingView.xaml:163`: Empty queue text says all tickets were pulled, which is narrower than confirmed physical paper success and must be interpreted using banner. Failure/attention list remains independently visible.

- `NeedsAttention` at `src/Falah.Client.CashierWinUI/Views/PrintingView.xaml:61`: Cross-route list shows logical route, age and errors, no automatic recovery action. Counts bound via Count for empty state. Loading/error overlays only selected jobs, route and attention areas may retain stale data on failure.

## Tests and limitations

NOT_EXECUTED. No physical tickets. Need all6 routes,4route narrow width, stale error data, logical delivered versus physical success wording and accessible refresh icon tests. No callable declarations.

## Recommendations (not implemented)

Use adaptive/wrapping route cards and queue widths; preserve explicit queue-versus-paper distinction and inspect PrintAgent independently.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
