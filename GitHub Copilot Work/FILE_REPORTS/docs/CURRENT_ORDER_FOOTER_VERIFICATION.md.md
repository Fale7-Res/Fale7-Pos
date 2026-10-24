# docs/CURRENT_ORDER_FOOTER_VERIFICATION.md

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Worktree SHA256 `0921364de015ef4dba51c746e44c3401ed7f3a51de9f9aed80c9055ad9a68abc`; Git blob SHA256 `0921364de015ef4dba51c746e44c3401ed7f3a51de9f9aed80c9055ad9a68abc`. Read lines1–41.

Status: full static document/configuration review; functions NOT_APPLICABLE.

## role

Historical fixture-based native footer layout measurements and reproduction instructions.

## reasoning

All41lines read. Describes separate totals rows, footer outside scrolling, compact mode below520DIP and48 historical fixture cases. Carefully distinguishes DIP-equivalent available dimensions from changing Windows scaling and bitmap capture from geometry; minimum51DIP viewport is not proof practical touch usability. Split state coverage expressly excludes the payment dialog. FooterLayoutProbe command changes build output/entrypoint and uses a hardcoded E-drive path; it was not executed in this audit. Artifacts referenced predate this conversation and remain untouched rather than being claimed as our new screenshots.

## relationships

- NativeFooterLayoutProbe source
- CartPanelView and TaxEngine
- Ignored artifacts/footer-layout/results.json
- CashierWinUI conditional probe build

## tests

No probe, window capture, scale change, backend request or physical printer action executed. The claimed48passed cases are historical documentation, not independently rerun here.

## recommendation

Use retained fixture geometry as a starting matrix, then separately approve actual native screen/state/scaling observation and paper-width tests. Validate long cart touch usability, not only nonoverlap.

## risks

Accidental probe build may replace normal entrypoint; fixed output paths/fixture mode must remain isolated. No direct DB/printing in document itself.

## Scope and effects

Inputs are human/operator/build configuration; outputs are instructions/build membership/ignore behavior as described above. No application business logic, XAML, packages, databases, services or printers modified. Runtime/hardware claims remain explicitly unverified.
