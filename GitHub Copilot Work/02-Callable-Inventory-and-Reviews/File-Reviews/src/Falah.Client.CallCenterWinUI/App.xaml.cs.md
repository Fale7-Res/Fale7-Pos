# src/Falah.Client.CallCenterWinUI/App.xaml.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `be75bda41587b475e82dde243495617f4f5a911a7ce81043c8e710cf038f283d`; worktree SHA256 `63bd32b07694b98e4d04fe82e84aac3bb5a41069e6269e4bc34438b8ecf72e53`; LF-equivalent `True`.

Actual role: CallCenter WinUI application launch and separate station journal initialization.

## Function/resource review

- `App` at `src/Falah.Client.CallCenterWinUI/App.xaml.cs:5`: Initializes resources then FileRetainedRequestStore.DirectoryFor callcenter, before order desk constructor can recover. No error callback, directory failure may abort startup; Save error later silent, see retained store review.

- `OnLaunched` at `src/Falah.Client.CallCenterWinUI/App.xaml.cs:21`: Normal creates MainWindow and activates. Only explicit SCREEN_LAYOUT_PROBE compile-time build opens isolated native verification fixture, no hidden runtime switch/timed activation. Probe result is not real backend transaction proof.

## Tests and limitations

Application launch not performed by reviewer; parent controls desktop. NOT_APPLICABLE direct DB/printing here.

## Recommendations (not implemented)

Handle journal startup failure visibly and keep fixture executables separate from real runtime.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
