# tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `3019f6a0484cce30ba52d22fc784451075fd7bb985af7375b91d4a1ff57af04d`; worktree SHA256 `3019f6a0484cce30ba52d22fc784451075fd7bb985af7375b91d4a1ff57af04d`; LF-equivalent `True`.

Actual role: Isolated opt-in footer geometry and screenshot probe using real views with fake API.

## Function/resource review

- `Start(Window window)` at `tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:24`: Replaces window content with white host, subscribes Loaded and activates native window. This must run in isolated opt-in build, not current operator window. Constructs no real user session here.

- `ParenthesizedLambdaExpression` at `tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:UNKNOWN`: Once-only closure sets started before awaiting RunAsync, preventing repeatedLoaded duplicate probe runs. Async event exception propagation is native context; RunAsync catches its main body exceptions.

- `RunAsync(Window window, Grid host)` at `tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:33`: Loops6 nominal size/scale conditions and8 cart states with fake GET menu/tax. Real CartPanelVM populated with empty/small60line/highquantity/discount/comp/split variants. Measures RTL label/amount gutter, row separation, CTA/total gap, footer viewport and scroll independence. Captures PNG only3 states, records capture errors separately, writes results/error then closes. Scaling adjusts DIP geometry and capture raster but does not change OS DPI. Does not test delivery row, busy/pending or theme states.

- `Bounds(string name)` at `tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:70`: Finds named FrameworkElement and transforms its actual rectangle into host coordinates, allowing footer/cart comparisons across scroll. Missing name/null throws to enclosing catch; does not measure actual glyph bounds or contrast.

- `SendAsync(HttpRequestMessage request, CancellationToken ct)` at `tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:130`: Rejects every nonGET and unknown path; returns only synthetic empty menu and configured14percent inclusive/exclusive tax. Strict route fixture prevents accidental network/write. Cancellation token ignored, no physical printing.

## Tests and limitations

SOURCE_REVIEW_ONLY. All5 callable spans explicitly reviewed, not executed. Source comment saying no live API is corroborated by strict fixture; native output availability not claimed.

## Recommendations (not implemented)

Interpret nominal DPI sizing and raw result records conservatively. Add absent delivery/pending/theme states only under authorized verification.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
