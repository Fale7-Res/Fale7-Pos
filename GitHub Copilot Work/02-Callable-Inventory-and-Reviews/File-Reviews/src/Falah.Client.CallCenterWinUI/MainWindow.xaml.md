# src/Falah.Client.CallCenterWinUI/MainWindow.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `cc79b0e1ccf707cb445068b0119fdfd1f21b9afa0fffbbb859d827a9fcf1d50a`; worktree SHA256 `bf3e09fd35fdc9c451d2298a0c8949598d83051b3b2e04294f8cea8eab585385`; LF-equivalent `True`.

Actual role: CallCenter root header/offline/close-warning/content host.

## Function/resource review

- `RootGrid` at `src/Falah.Client.CallCenterWinUI/MainWindow.xaml:7`: RTL3rows header/banner/contentstar, static flat station palette. Header hidden login, offline banner collapsed by code-behind; headline says server unavailable rather than fake successful order.

- `CloseNotice` at `src/Falah.Client.CallCenterWinUI/MainWindow.xaml:21`: TwoWay close-warning IsOpen; closing InfoBar only dismisses notice, next TryClose reevaluates actual session gate. No recovered operation notice unlike cashier; desk-specific retry must communicate journal recovery.

## Tests and limitations

No actual native viewport/runtime proof; high-DPI when warning consumes height requires test.

## Recommendations (not implemented)

Add clear operator recovery explanation and direct safe retry where context restored; no fabricated UI claims.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
