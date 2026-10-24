# src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `d7cd5a42556d62c04a0f2f5d70b0585736d29e61391307f077245b7cb1039ad2`

Worktree SHA-256: `ff3fefda7d248cfafe4d7881c5b0f620ea8f76339ac521fbfee8c951d4a3f41e`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Real delivery checkout, operator-scoped durable checkout/reprint and history lookup.

## Source paths and function/resource analysis

- `RecoverPendingOperations` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:77`: Restores checkout ID/DTO per OwnerUserId; recovery retry does not require cart reconstruction. Print request restores _hasUnresolvedPrint and UI retry state, unlike cashier electronic dead end.

- `SubmitOrderAsync` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:99`: Busy/result/print guards; pending checkout retries exact payload before validating blank recovery form. Fresh order validates cart/customer/service/address and rechecks actual active shift. Cash COD sends zero collected, electronic sends claim pending; CustomerPrintRoute CallCenter.

- `SendCheckoutAsync` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:110`: Persists request before send, retains across unknown/rejection after unknown, clears on known first rejection/success. Success fire-and-forget upserts captured request customer details; result handling failure ignored. Journal Save errors swallowed by store can defeat promised durability.

- `LookupOrderAsync` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:312`: CanEditOperation includes lookup busy; trims lookup number, clears prior result, exposes error. Pickup time displayed Cairo; payment breakdown raw method tokens rather than Arabic labels.

- `ReprintLookedUpOrderAsync` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:337`: Creates CreatePrintJobsRequest without IncludeKitchenRoutes false unlike cashier receipt reprint; default flag must be cross-checked in shared DTO/handler. Potential duplicate kitchen work if default true.

- `SendPrintAsync` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:340`: Stores exact request, handles unknown flag and pending replay; definite first failure retains DTO for retry. ClearResolvedPrint clears memory but not store, so stale definite-rejected print can reappear as unresolved after restart (UI-011).

- `StartNewOrder` at `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:238`: Calls ClearResolvedPrint and clears result/error only when edit permitted. ClearOrderLookup likewise clears memory, still journal stale risk.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

CallCenterDurabilityTests relevant, contents not fully reviewed yet. Missing rejected manual print followed by cancel/new-order/restart case; concrete lookup race in Customer partial separate UI-007.

## Recommendations (not implemented)

Pass explicit IncludeKitchenRoutes false for customer-only reprints if DTO default queues kitchen; clear journal when safely discarding definitively rejected print; maintain per-operator durable request scope.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
