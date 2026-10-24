# src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `dece8eb51b3c8db5f1767395d8992eff8756afe3ee4c2c0b57c83b813ab7e8a1`

Worktree SHA-256: `dece8eb51b3c8db5f1767395d8992eff8756afe3ee4c2c0b57c83b813ab7e8a1`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: PIN-only login screen.

## Source paths and function/resource analysis

- `PressDigit` at `src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:35`: Limits string length to six but accepts arbitrary parameter string from command caller; ordinary keypad supplies digits. Does not block during IsBusy, so display PIN can change while request awaits; request string already captured.

- `CanSubmit` at `src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:51`: Requires 4+ chars and !IsBusy; generated notifications on PIN/busy update button.

- `SubmitAsync` at `src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:54`: Real login endpoint returns UserSummaryDto, sets shared session then event. All ApiResult failures are replaced by incorrect PIN message, including offline/network and 5xx, obscuring runtime diagnosis; catch exposes raw exception message. PIN cleared after success/failure but failed connection result misclassified.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing typed network failure UI test; login screens not automated and no credentials included in reports.

## Recommendations (not implemented)

Distinguish invalid credentials from server connectivity using safe ApiResult error/status; freeze keypad while sending and clear sensitive state on lock.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
