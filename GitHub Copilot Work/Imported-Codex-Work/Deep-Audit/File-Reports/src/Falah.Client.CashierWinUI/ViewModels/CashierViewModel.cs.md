# src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `adc1659f1ba8076b0ec32f59e2773df69471d50d7200079097470a0a063cc77c`

Worktree SHA-256: `ec9eeab0b553ea327798f673e9c077892d38fc2676492b183cca1678622488ac`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Menu, categories/offers/variants, cart estimates and order type.

## Source paths and function/resource analysis

- `CashierViewModel` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:23`: Binds session/header and cart collection callback; no recovery of primary checkout/expense/etc. Constructor itself has no database side effects.

- `BuildBestSellers` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:69`: Joins server-ranked IDs to actual menu, omits missing IDs/empty shortcut. Category remains outside real categories to avoid duplicate product search.

- `RefreshVisibleProducts` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:52`: Rebuilds full tiles and product-to-icon dictionary per keystroke; no debounce or virtualization decision here. O(menu + result) work on UI thread can be noticeable with large catalogues.

- `LoadMenuAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:107`: Fetches menu then tax rules sequentially, replaces categories and selects best-sellers/first. No busy guard/epoch/cancellation; concurrent reloads may race, and existing cart unit prices are not refreshed. Server remains pricing authority but stale estimate/tender can disagree.

- `SelectProduct` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:161`: Checks CanEditOperation; unlike CallCenter version lacks explicit IsAvailable check at this method. Actual catalogue disabled state and server validation are separate controls, so no unauthorized sale claim made here.

- `SelectOffer` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:169`: Checks editable and handles variant choice; PendingOffer persists until variant resolved/closed. AddToCart derives estimate from fixed offer or variant/base price.

- `RecomputeCartTotals` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:33`: Recomputes sum/item count and all derived estimates; quantity commands trigger this explicitly. No collection-item subscriptions for arbitrary external quantity changes.

- `GrandTotalPiastres` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:240`: Uses domain TaxEngine on subtotal less discount, adds delivery and subtracts comp, clamps at zero. Domain/server must validate discount/comp relationships independently.

- `IncreaseQuantity` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:264`: Unbounded integer increment and unchecked unitprice*quantity can overflow in extreme programmatic use; normal POS use not reproduced. Decrease removes at one; RemoveLine and AppendNote obey edit guard.

- `ClearCart` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:287`: Clears lines and discount/comp/split; resets shared delivery but leaves pickup schedule UI-006; resets checkout ID only for genuinely new cart.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

CashierCorrectnessTests relevant but not fully inspected here. Missing large-catalogue UI timing, concurrent reload ordering, concrete crash recovery and successive pickup schedule cases.

## Recommendations (not implemented)

Guard/cancel overlapping menu reads; bind estimates honestly and recover exact write identity; keep optional schedule lifecycle consistent.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
