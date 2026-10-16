# src/Falah.Client.CashierWinUI/MainWindow.xaml

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `52724a0dd90467349ed40676be8bb5cf1a54b7047328d9548b80465689eabfb6`

Worktree SHA-256: `db63a2f50b06c6cdaef327ba9b0dbe9b8884f7ed5af55c187346c8038da3ba20`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.xaml`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: RTL root, persistent header/connectivity/recovery info and content host.

## Source paths and function/resource analysis

- `RootGrid` at `src/Falah.Client.CashierWinUI/MainWindow.xaml:7`: Three-row root leaves content star height after header/banners. Recovery notice grows/shrinks workspace; short-screen modal viability needs observation.

- `RecoveredNotice` at `src/Falah.Client.CashierWinUI/MainWindow.xaml:30`: Uses NullToBool converter and command dismiss, reports journal presence only. Raw owner keys are technical strings rather than localized action names.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Native layout needed at 100/125/150/200% DPI; no screenshot asserted.

## Recommendations (not implemented)

Name recovered operations for operators and offer direct safe recovery navigation; test minimum working content height.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
