# src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `dabfa47fa864b50d565951350f19e9a7735b9da5e5ed077fe22a427cb742a384`

Worktree SHA-256: `dabfa47fa864b50d565951350f19e9a7735b9da5e5ed077fe22a427cb742a384`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Phone-first customer resolution and delivery form/address/service selection.

## Source paths and function/resource analysis

- `OnCustomerPhoneChanged` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs:35`: Auto starts direct async lookup at length>=11, no debounce, cancellation, epoch or operation guard. Changing phone below11 only resets ID/addresses, leaves name/address fields.

- `LookupCustomerCoreAsync` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs:49`: Sets _lastLookedUpPhone then awaits current-phone lookup; unconditionally assigns response to current form, with no request-phone equality check. Old response A can overwrite customer B or freshly cleared/new order UI-007. Failed lookup treats timeout/403 as new customer, hides transport error.

- `ResetResolvedCustomer` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs:41`: Clears ID/saved-address/note but leaves name and address, so edits can show mixed old/new identity until operator correction.

- `SaveDeliveryDetails` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs:121`: Validates mandatory fields locally then closes; unlike Cashier counterpart does not upsert immediately. Actual write occurs after successful checkout.

- `ApplySavedAddress` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs:103`: Copies structured address and optionally matches service ID. Does not verify address belongs current resolved CustomerId at invocation; stale list race is primary root.

- `LoadDeliveryServicesAsync` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs:204`: Loads real service list; preserves error but no read busy guard/epoch/selected service rebind. Selecting service notifies estimated totals.

- `ResetCustomerAndDelivery` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs:217`: Clears form and lookup key, but outstanding lookup not cancelled/version-invalidated, enabling post-clear repopulation UI-007.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing deterministic delayed lookup A/B and lookup-then-ClearOrder tests; no customer PII or DB writes used for audit.

## Recommendations (not implemented)

Snapshot normalized requested phone plus generation, discard stale completions, cancel pending lookup on clear/change, distinguish 404 from connectivity failure.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
