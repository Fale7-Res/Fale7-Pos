from ui_incremental import *
for prefix,label in [(P,'Cashier'),(C,'CallCenter')]:
 record(prefix+'Views/LoginView.xaml',label+' PIN touch login presentation.',[
 ('FlowDirection="RightToLeft"','Arabic surface RTL but nested numeric keypad deliberately LTR; digits1..9 row order preserves phone layout.'),
 ('PinDisplay','Visible masked VM display, empty placeholder and error display; this markup does not store or reveal raw PIN. No native PasswordBox input here; physical keyboard handling lives elsewhere.'),
 ('PressDigitCommand','Twelve buttons delegate digit, clear and backspace. Backspace glyph lacks explicit AutomationProperties.Name; accessible meaning needs native verification. All actions rely VM busy gates, no keypad IsEnabled busy binding.'),
 ('SubmitLabel','Primary submit caption and command bound; no username because API maps PIN to account. Footer describes personal permissions.'),
 ('Width="440"','Fixed width440 plus40px padding; no ScrollViewer. Small display or large accessibility scale may clip login; native DPI observation NOT_EXECUTED.'),
 ('CardBorderBrush','Cashier uses ThemeResource brushes; CallCenter uses static and literal white/light backgrounds. This loop records observed difference, not a claim of identical theming.')
 ],'NOT_EXECUTED. Native keyboard/assistive tech, busy double taps, display masking and text scaling tests required. No actual authentication automated.','Give keypad glyph explicit accessible name; verify high contrast and overflow. Keep auth security review with coordinator.')
record(W+'ViewModels/CashierViewModel.cs','Legacy WPF cashier menu, cart and order-type base partial; active app is WinUI.',[
 ('CashierViewModel(','Own Cart event recomputes totals; self-owned collection closure does not establish external retention leak. Header/session references retained.'),
 ('RefreshVisibleProducts(','Blank search filters selected category; search spans all categories culture-insensitive. Tile offer choices constructed from menu snapshot.'),
 ('LoadMenuAsync(','Loads real API menu and selects first category; failed refresh retains old rows with error. No duplicate-request gate or response generation, so concurrent loads may publish stale result; legacy scope.'),
 ('SelectProduct(','Multi-variant opens chooser; single/no variant adds line. Does not clear PendingOfferId before ordinary selection; closing chooser/variant commit clears it. Intervening selection could inherit offer if command invoked while chooser open; native overlay must gate.'),
 ('SelectOffer(','Multi-variant retains chosen offerId; single variant adds immediately. Variant selection does not validate membership in current product; backend authoritative validation needed.'),
 ('SelectVariant(','Adds current chooser product plus passed variant and pending offer then clears chooser and pending state. Null chooser ignored.'),
 ('CloseVariantPicker(','Clears both transient product and offer choice.'),
 ('AddToCart(','Prefers matching offer fixed price then variant price then product base; each click separate CartLine, no merge. Offer chosen by current product list. Prices estimates, not financial authority.'),
 ('RecomputeCartTotals(','Sums integer line totals, not tax or delivery; GrandTotal explicitly equals subtotal. Collection changes plus quantity commands manually notify computed display.'),
 ('IncreaseQuantity(','Increments integer without local cap; arithmetic overflow/unbounded quantity is backend/domain concern as well. No active WinUI claim.'),
 ('DecreaseQuantity(','Decrements above1 otherwise removes. RemoveLine removal also triggers redundant total recompute, benign.'),
 ('AppendNote(','Null guard, concatenates Arabic comma; free text length unrestricted here, server must enforce.'),
 ('ClearCart(','Clears lines, totals and current checkout request identity; must be prevented while unknown write by other partial logic.'),
 ('SetOrderType(','Assigns raw string without whitelist in this method. SelectCategory assigns selected category and generated hook rebuilds visible tiles.')
 ],'NOT_EXECUTED. Legacy-specific tests for offer cancellation, variant membership, stale menu loads and checkout identity lifetime; active WinUI behavior reviewed separately.','Avoid exposing legacy app as equivalent production client. Validate variant/offer context and retain pending keys for ambiguous writes.')
