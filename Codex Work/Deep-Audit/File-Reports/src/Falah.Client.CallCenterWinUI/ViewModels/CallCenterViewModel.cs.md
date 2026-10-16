# src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `6f96cfc61418e8146cf90abfcf8803f97a97ee211e4d918618f3c0f8f867fd06`

Worktree SHA-256: `75ca06ba893a7b302d51a828fe590119039f6fd1bab1a33f4e9702e9dba4339c`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Phone/delivery catalogue/cart and initialization.

## Source paths and function/resource analysis

- `CallCenterViewModel` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:19`: Captures immutable operator owner and restores journal before any new order; cart collection recalculates local estimates.

- `InitializeAsync` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:41`: Sequential actual shift, settings identity, menu/taxes and service loading. Shift availability distinct from menu failure; uses backend only.

- `RefreshVisibleProducts` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:85`: Rebuilds tiles and icon index per search keystroke, same O(menu+matches) UI-thread cost as cashier. No best-seller duplicate products in real categories.

- `LoadMenuAsync` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:54`: Loads real menu and enabled tax rule then sets best sellers; no epoch/cancellation. Existing cart prices are still snapshot estimates.

- `SelectProduct` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:185`: Checks editable and product availability; AddToCart repeats this guard. SelectOffer opens picker without upfront guard but actual add guarded; potential modal state mutation while frozen, not financial bypass.

- `GrandTotalPiastres` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:256`: Shared domain TaxEngine plus actual selected delivery fee, clamps zero. Submit sends identities/amount and server recalculates authority.

- `ClearOrder` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:299`: Clears cart/form/key/result only if editable. Does not cancel outstanding customer lookup UI-007; direct lookup callback can repopulate new form.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing concrete clear-during-lookup/native catalogue large-data cases.

## Recommendations (not implemented)

Reuse correct phone-generation race protection; separate loading failure from no shift; bound repeated UI-thread rebuilding.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
