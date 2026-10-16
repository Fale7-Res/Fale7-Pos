# src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.ShiftClose.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `f3e4f93a1a8c5fd1a0f03a27b9ef20fcb111b7b2b30cdbcd016b95cb092778fe`

Worktree SHA-256: `06cd84179290763e76ed175c2d91f6cdddcb2d6ad4b95503bc5f24386a631a82`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Two-step blind counted-cash close with manager discrepancy approval.

## Source paths and function/resource analysis

- `OpenCloseShift` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.ShiftClose.cs:36`: Requires shifts_blind_close, editable state and empty cart. Resets prior count/preview/approval state.

- `PrepareCloseShiftAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.ShiftClose.cs:56`: Requires nonnegative count; server returns expected/actual/difference and manager requirement. Count visible only after prepare, as designed. Prepare can be repeated through returning to count; this is business behavior to confirm, not automatic security finding.

- `ConfirmCloseShiftAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.ShiftClose.cs:92`: Requires preview; freezes preview actual cash; requires reason and manager approval when preview requests it. Server must re-evaluate freshness/threshold if ledger changed after preview.

- `CompleteCloseShiftAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.ShiftClose.cs:100`: Preserves ambiguous in-memory DTO with fixed ID, but loses it on process restart UI-001. On success clears session shift/name and closes overlay; no direct printer calls.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

CloseoutRegressionTests is relevant name but contents not yet fully reviewed in this checkpoint; explicit stale-preview and restart recovery tests required.

## Recommendations (not implemented)

Persist close DTO before API send; distinguish successful financial close from print completion; recheck server expected values atomically.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
