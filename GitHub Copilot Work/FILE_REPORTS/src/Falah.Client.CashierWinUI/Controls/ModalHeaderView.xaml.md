# src/Falah.Client.CashierWinUI/Controls/ModalHeaderView.xaml

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `5fe2da6cca5138ae1c50eca5ebd0d9fd80164704ecd0e242bdf7f4c00f1e4c59`

Worktree SHA-256: `5fe2da6cca5138ae1c50eca5ebd0d9fd80164704ecd0e242bdf7f4c00f1e4c59`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.xaml`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: RTL reusable title/close header.

## Source paths and function/resource analysis

- `TitleBlock` at `src/Falah.Client.CashierWinUI/Controls/ModalHeaderView.xaml:20`: Star/auto columns reserve close width, wraps title to max two lines; may trim longer title but avoids collision. Close glyph has Arabic AutomationProperties.Name and 44 DIP minimum from shared style.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

NOT_APPLICABLE business rules; native title length/screen reader focus required.

## Recommendations (not implemented)

Retain named close button and verify long title+RTL hit targets.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
