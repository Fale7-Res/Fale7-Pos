# src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `cc623befa8992fab4643823c395240e76cd3966508683e0dd9df2055ced33290`; worktree SHA256 `cc623befa8992fab4643823c395240e76cd3966508683e0dd9df2055ced33290`; LF-equivalent `True`.

Actual role: Legacy WPF cashier menu, cart and order-type base partial; active app is WinUI.

## Function/resource review

- `CashierViewModel(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:23`: Own Cart event recomputes totals; self-owned collection closure does not establish external retention leak. Header/session references retained.

- `RefreshVisibleProducts(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:41`: Blank search filters selected category; search spans all categories culture-insensitive. Tile offer choices constructed from menu snapshot.

- `LoadMenuAsync(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:54`: Loads real API menu and selects first category; failed refresh retains old rows with error. No duplicate-request gate or response generation, so concurrent loads may publish stale result; legacy scope.

- `SelectProduct(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:83`: Multi-variant opens chooser; single/no variant adds line. Does not clear PendingOfferId before ordinary selection; closing chooser/variant commit clears it. Intervening selection could inherit offer if command invoked while chooser open; native overlay must gate.

- `SelectOffer(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:90`: Multi-variant retains chosen offerId; single variant adds immediately. Variant selection does not validate membership in current product; backend authoritative validation needed.

- `SelectVariant(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:99`: Adds current chooser product plus passed variant and pending offer then clears chooser and pending state. Null chooser ignored.

- `CloseVariantPicker(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:108`: Clears both transient product and offer choice.

- `AddToCart(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:86`: Prefers matching offer fixed price then variant price then product base; each click separate CartLine, no merge. Offer chosen by current product list. Prices estimates, not financial authority.

- `RecomputeCartTotals(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:27`: Sums integer line totals, not tax or delivery; GrandTotal explicitly equals subtotal. Collection changes plus quantity commands manually notify computed display.

- `IncreaseQuantity(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:146`: Increments integer without local cap; arithmetic overflow/unbounded quantity is backend/domain concern as well. No active WinUI claim.

- `DecreaseQuantity(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:149`: Decrements above1 otherwise removes. RemoveLine removal also triggers redundant total recompute, benign.

- `AppendNote(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:159`: Null guard, concatenates Arabic comma; free text length unrestricted here, server must enforce.

- `ClearCart(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:166`: Clears lines, totals and current checkout request identity; must be prevented while unknown write by other partial logic.

- `SetOrderType(` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:179`: Assigns raw string without whitelist in this method. SelectCategory assigns selected category and generated hook rebuilds visible tiles.

## Tests and limitations

NOT_EXECUTED. Legacy-specific tests for offer cancellation, variant membership, stale menu loads and checkout identity lifetime; active WinUI behavior reviewed separately.

## Recommendations (not implemented)

Avoid exposing legacy app as equivalent production client. Validate variant/offer context and retain pending keys for ambiguous writes.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
