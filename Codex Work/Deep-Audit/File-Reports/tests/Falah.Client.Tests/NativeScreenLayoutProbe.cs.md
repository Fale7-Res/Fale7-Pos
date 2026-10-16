# tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `5c4cb491ead1d03eafbfd0ca7d9a28c98790209f52adcd25392558cc96bb8946`; worktree SHA256 `5c4cb491ead1d03eafbfd0ca7d9a28c98790209f52adcd25392558cc96bb8946`; LF-equivalent `True`.

Actual role: Opt-in native view-construction probe, not business workflow test.

## Function/resource review

- `Start(` at `tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs:11`: Replaces window content with white host; reflection selects non-placeholder Views UserControls, constructs without session/DataContext at6 nominal size/scale combinations, resizes actual window and waits25ms. Writes progress/results then closes. Scope cannot establish populated modal/binding/auth/API correctness or pixel contrast. Must not run against active user draft.

- `CountVisuals(` at `tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs:50`: Recursively counts current visual descendants; count alone not overflow/accessibility/contrast assertion. Loaded callback guarded once; per-view exceptions become report data; finally output/close may itself throw. Management probe gated CASHIER_LAYOUT_PROBE.

## Tests and limitations

SOURCE_REVIEW_ONLY, NOT_EXECUTED. Simulated DIP sizes are not OS DPI changes; errors captured need consuming verifier. No screenshots implied.

## Recommendations (not implemented)

Use isolated process and populated states for layout; assert geometry/theme/automation independently. Do not count construction as full usability evidence.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
