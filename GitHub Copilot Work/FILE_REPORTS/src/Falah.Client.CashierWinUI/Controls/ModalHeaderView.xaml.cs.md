# src/Falah.Client.CashierWinUI/Controls/ModalHeaderView.xaml.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `77396fd63e05f090a77d2d8239c7049bbbdb5526dc8f549f1b05aacf7ddd2dd2`

Worktree SHA-256: `77396fd63e05f090a77d2d8239c7049bbbdb5526dc8f549f1b05aacf7ddd2dd2`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Dependency properties bridging modal title/close command/automation ID.

## Source paths and function/resource analysis

- `ModalHeaderView` at `src/Falah.Client.CashierWinUI/Controls/ModalHeaderView.xaml.cs:8`: Initializes title; static DP callbacks update actual controls. Title callback assumes nonnull string; public property contract defaults empty. Close command property nullable intentionally.

- `OnCloseAutomationIdChanged` at `src/Falah.Client.CashierWinUI/Controls/ModalHeaderView.xaml.cs:17`: Sets stable per-dialog close AutomationId through WinUI automation API, useful for actual UI tests; no direct privileged action.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Native DP timing/null and command CanExecute propagation tests absent from headless project.

## Recommendations (not implemented)

Keep default initialized state and avoid credentials in automation names.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
