# src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Shift.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `942d8dbf033357f7186def2e56f3871b14b273b7ca559738d1456cde829215e2`

Worktree SHA-256: `fe92b9e4d8ea353aa57fb2a70c4a7948a5821376a246c055c6b7282d72929ac0`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Cashier shift opening.

## Source paths and function/resource analysis

- `OpenShiftAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Shift.cs:15`: Requires permission, not initializing/busy, and nonnegative money. Shift label derives host local DateTime.Now hour rather than explicit Cairo clock; cosmetic mismatch possible on differently configured workstation.

- `CompleteOpenShiftAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Shift.cs:18`: Memory-only immutable pending request UI-001; on success updates session ID/name and loads menu. A menu load failure is distinct from successful shift opening and should remain recoverable.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing concrete process-restart opening test; local timezone edge coverage not established.

## Recommendations (not implemented)

Persist open request and use explicit business timezone for suggested shift label.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
