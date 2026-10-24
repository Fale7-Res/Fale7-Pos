# src/Falah.Client.CashierWinUI/Models/CartLine.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `f75937aed2cca8ca1dbbe5034aaccb3afc00efc2efa79d869db0ea89416e17fd`

Worktree SHA-256: `f75937aed2cca8ca1dbbe5034aaccb3afc00efc2efa79d869db0ea89416e17fd`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Observable client cart estimate model.

## Source paths and function/resource analysis

- `LineTotalPiastres` at `src/Falah.Client.CashierWinUI/Models/CartLine.cs:29`: Multiplies long price by integer quantity unchecked; ordinary commands maintain minimum one but model itself imposes no upper/lower quantity validation. Server receives only identities/quantity, so this is presentation robustness not authoritative price tampering.

- `OnQuantityChanged` at `src/Falah.Client.CashierWinUI/Models/CartLine.cs:33`: Notifies line total/display; parent totals rely on VM quantity commands rather than item property subscription. Notes are mutable strings without local length cap; server validation required.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing extreme quantity and external model setter parent-total notification tests.

## Recommendations (not implemented)

Keep server pricing authority; bound legitimate quantity and handle numeric overflow for predictable UI.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
