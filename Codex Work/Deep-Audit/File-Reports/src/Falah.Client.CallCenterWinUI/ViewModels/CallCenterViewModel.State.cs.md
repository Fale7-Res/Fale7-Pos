# src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.State.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `d19dc93f79fad66bcf8d817971d1e0b7712b5b75fa98ce6f6386324a76647897`

Worktree SHA-256: `d19dc93f79fad66bcf8d817971d1e0b7712b5b75fa98ce6f6386324a76647897`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: CallCenter busy/edit and owner state.

## Source paths and function/resource analysis

- `IsOperationBusy` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.State.cs:21`: Includes checkout/reprint/order lookup, excludes customer lookup and delivery service fetch; late customer response can mutate frozen checkout form UI-007.

- `CanEditOperation` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.State.cs:25`: Blocks unresolved checkout/print, appropriate protection against abandoning unknown order. This field gates visible controls but direct property/async completion writes can bypass.

- `RefreshOperationState` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.State.cs:27`: Raises own busy and edit state plus SubmitOrder CanExecute, refreshes header. OwnerUserId initialized from login and used in journal keys.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing customer async completion during checkout state and native focus tests.

## Recommendations (not implemented)

Apply owner/version gates to background completion paths; keep print retry reachable when journal restored.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
