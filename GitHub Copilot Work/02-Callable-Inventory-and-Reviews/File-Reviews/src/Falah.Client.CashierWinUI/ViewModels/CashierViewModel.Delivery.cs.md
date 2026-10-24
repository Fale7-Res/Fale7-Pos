# src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `d989056e4c339b114b4586cd3aeadbf1d0ca01352f38c5bb6ba112c58ae42c94`

Worktree SHA-256: `d9464aaaf7664d362a7600e49df95dc7794847563c08b3d55dadc9ac5dfc588e`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Shared customer fields, delivery service lookup/address editor and order reset.

## Source paths and function/resource analysis

- `OnOrderTypeChanged` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs:39`: Notifies order type/totals, clears identity for takeaway, clears pickup schedule only when moving away from pickup. Opens corresponding editor immediately.

- `OnCustomerPhoneChanged` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs:57`: Clears saved-address choice and lookup message; updates pickup summary; does not clear customer name. Phone edit can retain previous name until lookup/explicit edit.

- `LoadDeliveryServicesAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs:76`: One busy guard preserves service by ID across reload. Fetches existing server service fee; failure keeps previous list and sets error.

- `LookupCustomerAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs:96`: Snapshots trimmed phone, discards response if current phone changed. This correct race guard is absent from CallCenter equivalent UI-007. Does not cancel transport; same-phone repeated requests may still reorder.

- `SaveDeliveryDetailsAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs:113`: Requires all mandatory details and selected service; upserts customer before checkout, so saving details writes customer even when order is later cancelled. Uses session backend only; no mock record.

- `ResetDelivery` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs:131`: Clears customer/address/service fields and summary, omits PickupDate/Time (UI-006) and OrderType intentionally persists.

- `SelectDeliveryService` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs:140`: Direct setter without CanEditOperation guard; XAML/input scope needs full keyboard freeze verification.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing order lifecycle reset time isolation; stale-phone guard should have intentional delayed HTTP tests; customer upsert side effect should be documented.

## Recommendations (not implemented)

Clear all order-specific pickup values on resolved new-order reset; consistently enforce CanEditOperation in callable mutation handlers.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
