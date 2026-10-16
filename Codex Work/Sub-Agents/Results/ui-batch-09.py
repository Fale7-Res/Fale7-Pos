from ui_incremental import *
for path,station in [(T+'HeaderViewModel.cs','cashier'),(T+'CallCenterHeaderViewModel.cs','CallCenter')]:
 record(path,station+' headless native-header substitute.',[
 ('HeaderViewModel','Constructor accepts optional production session but ignores it, Stop/Refresh/SetIdentity all no-op. It substitutes not only clock but also native notifications and actual restaurant identity, despite comment only native clock omitted.'),
 ('Refresh','Noop means tests cannot assert actual header user/role/shift/lock notifications or session OperationStateChanged subscription. Stop no timer semantics; SetIdentity no configured name behavior.')],
 'This is test infrastructure, NOT_APPLICABLE standalone domain correctness. Native header paths separately reviewed; no test run here.',
 'State exact substitution scope in test evidence; add native header integration rather than infer tested UX from headless assertions.')
record(W+'Models/ProductTile.cs','WPF immutable product/offer pairing records.',[
('OfferChoice','Pairs actual product DTO+offer DTO by immutable record, view can send correct product offer without ancestor lookup. ProductTile only product/offers; no images/station display helpers unlike active WinUI.'),
('ProductTile','No business calculation, network, authorization, IDisposable or concurrency state; IReadOnlyList does not guarantee underlying list immutable but normal construction passes fresh menu offer list.')],
'NOT_APPLICABLE direct DB/printing, no WPF tests linked current suite.',
'Keep reference scope documented; do not assume active image features in WPF.')
record(P+'ViewModels/ShellPlaceholderViewModel.cs','Fallback title-only not-built module state.',[
('ShellPlaceholderViewModel','Stores supplied moduleTitle via generated observable field. No invented KPIs/data or API call; actual reachability only unknown nav key fallback, ShellViewModel current known catalog maps real modules.')],
'NOT_APPLICABLE financial functions; unsupported-key route/native text layout tests useful.',
'Do not list placeholder existence as proof a known module missing; verify catalog route first.')
record(C+'Services/CashInput.cs','Localized strict EGP to integer piastre parser shared behavior copy.',[
('TryParse','Arabic/Persian digits+Arabic decimal separator normalized to invariant; decimal point only, nonnegative, <=long.Max/100, effective<=2decimals; sets out0before parsing. Avoids double rounding and exponent/negative/group separator ambiguity.'),
('Format','decimal long/100m to two invariant decimals, safe display precision. Exact source copy of active cashier parser aside from namespace, potential future drift if maintained independently.')],
'Missing explicitly asserted all-boundary parser parity between station copies. No live input or money writes.',
'Retain parse parity tests and optional shared pure utility only in authorized refactor.')
for pp,station in [(P,'cashier'),(C,'CallCenter')]:
 record(pp+'Views/AppHeaderView.xaml',station+' persistent identity/shift/clock/user/lock layout.',[
 ('LockButton','Arabic button binds CanLock and actual OnLockClicked; tooltip tells finish financial operation, but busy text fixed even when enabled. Root Lock implementation separate missing logout issue.'),
 ('RestaurantName','64DIP height, star identity group plus Auto clock/user columns. No wrapping/trimming/max width on potentially long restaurant/user/shift name; native narrow/high-scale overlap test needed.'),
 ('IsShiftOpen','Shift dot plus text, accessible state not color-only because ShiftLabel present. Cashier uses ThemeResource card/semantic brushes, CallCenter literalWhite/static palette; no assumption both have same dark mode.')],
'No screenshot supplied. Need long names, RTL Tab order/text scaling and lock can-execute runtime checks.',
'Constrain long identity content and verify operational clock uses intended Cairo zone; preserve textual shift status.')
record(P+'ViewModels/ShellNavItem.cs','Immutable shell page/group navigation metadata.',[
('ShellNavItem','Key,label,optional permission raw catalog string; permission is visibility metadata not server access authority. Null permission always-visible semantics owned ShellVM filter.'),
('ShellNavGroup','IsDirect Count==1, Single index0without guard. Builder only creates nonempty groups from allowed items, so normal invariant valid; public constructing empty group would throw if Single read. No data writes.')],
'Need builder permission/group invariants, no native test in this file.',
'Preserve nonempty group invariant and test catalog entries reachable once.')
record(C+'MainWindow.xaml','CallCenter root header/offline/close-warning/content host.',[
('RootGrid','RTL3rows header/banner/contentstar, static flat station palette. Header hidden login, offline banner collapsed by code-behind; headline says server unavailable rather than fake successful order.'),
('CloseNotice','TwoWay close-warning IsOpen; closing InfoBar only dismisses notice, next TryClose reevaluates actual session gate. No recovered operation notice unlike cashier; desk-specific retry must communicate journal recovery.')],
'No actual native viewport/runtime proof; high-DPI when warning consumes height requires test.',
'Add clear operator recovery explanation and direct safe retry where context restored; no fabricated UI claims.')
record(P+'Views/DeliveryDetailsView.xaml','Cashier structured customer/address/service editor overlay.',[
('IsDeliveryEditorOpen','Themed overlay card with pinned header/footer and scroll middle, MaxWidth720not fixed width. Separate phone/name/address fields preserve actual backend structured contract.'),
('CustomerPhone','Fields lack CanEditOperation IsEnabled bindings, unlike pickup. SaveDeliveryDetailsAsync sets IsLoadingDelivery but UI phone/name/address remain editable during awaited customer upsert, allowing response/summary to correspond earlier request. Pending money operations normally cannot open editor, but direct UI overlap needs native focus check.'),
('SavedAddresses','ComboBox displays only Street (same-street building choices indistinguishable); actual selection copies full address. Service refresh/error/save provided, no mock fee invention.')],
'Need edit-during-save original payload/summary test, same-street saved addresses UI, keyboard focus trap and per-DPI scrolling.',
'Bind edit busy states consistently, display distinguishing address details; no physical printing/backend modifications done.')
for pp,station in [(P,'cashier'),(C,'CallCenter')]:
 record(pp+'Views/CategoryStripView.xaml',station+' live product-search and horizontally scrolling category chips.',[
 ('CategoriesGridView','66DIP horizontal strip ItemsStackPanel, SelectionModeNone/ItemClick, explicit IsSelected in CategoryTile. Min116wide48highchip, max220text ellipsis; source does not expose full-name tooltip for truncated category.'),
 ('Background','Unselected WhiteBrush, foreground TextPrimaryBrush via converters. Cashier dark repeats UI-002 pair defect beyond expense/payment; CallCenter currently static light palette coherent. Font icon + name state color resolution depends converter, no direct ThemeResource selected expressions.'),
 ('SearchQuery','TextBox named semantic Arabic search, updates each keystroke; actual VM refresh O(menu)and no debounce. Cashier named ProductSearchBox enables F2; CallCenter no shortcut focus method.')],
'Need keyboard category activation, selected announcement because native SelectionModeNone, long-title tooltip and dark-state screenshot tests; no visual observed.',
'Theme paired resources and accessible selected state, while keeping actual stored categories intact.')
record(W+'MainWindow.xaml','WPF reference root with implicit VM type templates.',[
('Window.Resources','WPF DataTemplate DataType resolves Login/Cashier into views, unlike active WinUI manual switch. Initial1366x820centered window, no maximize/min-size behavior.'),
('DockPanel','Header visibility uses ancestor root IsLoggedIn after header DataContext replaced; offline banner root IsOnline, CurrentView content remaining area. All static palette; no WinUI live theme.')],
'NOT_APPLICABLE money mutation; WPF native window/resize binding not runtime-tested.',
'Reference-only designation, test correct DPI/window when deployed; no close gate in code-behind.')
record(C+'App.xaml','CallCenter Fluent resources and converter instances.',[
('MergedDictionaries','Fluent XamlControlsResources then Colors/Typography/Controls; all flat static station palette. No ThemeService dictionary in this app; dark-mode assumptions from cashier must not be generalized.'),
('StatusToPillBrushConverter','Registers20converter resources including image/status/count/money/date; registration not proof bindings valid or data safe. Some duplicate low-level utilities across station source maintenance risk.')],
'Parent XML inventory parses; no actual native resource instantiate evidence here.',
'Keep resource keys complete and types actual WinUI; native failure state validation separate from successful C# build.')
for pp,station in [(W,'WPF reference'),(C,'CallCenter')]:
 record(pp+'Models/CartLine.cs',station+' mutable line display/identity state.',[
 ('LineTotalPiastres','Unchecked price*intQuantity estimate, no internal quantity validation; product/variant/offer identities immutable init, notes/quantity observable. Backend DTO sends quantity/identity, no trusting display price.'),
 ('OnQuantityChanged','Updates line total/display, parent grand totals require own VM commands/recompute. Notes no local length bound; server must validate quantity/note length; no DB or printer action.')],
'Need extreme quantity overflow and property/direct setter aggregate notification; active VM tests not automatically WPF evidence.',
'Keep estimated price explicitly nonauthoritative and bounded user input; no code changes.')
record(P+'Views/ShellView.xaml','Role-filtered grouped navigation chrome and page host.',[
('NavBar','Code-generated horizontal StackPanel inside horizontal ScrollViewer, AutomationProperties.Name Arabic.46DIPminimum; overflow accessible via scroll but no static fixed page count. App header sits outside shell.'),
('PageHost','Stretched star content, manual code-behind type mapping. Source comments about past NavigationView crash not current runtime reproduction; no reason to treat removed control as present defect.')],
'Native nav keyboard/RTL scrolling and permission state tested separately; no actual screenshot here.',
'Keep complete catalog/host mapping and user-facing module names, validate page cancellation lifecycle.')
record(P+'Views/ShellPlaceholderView.xaml','Explicit unknown/not-yet-connected module presentation.',[
('ModuleTitle','Shows supplied title and honest development message, no fake business data. Default fallback only; current known catalog mostly maps actual screens, so do not infer all modules placeholder.'),
('PageCardStyle','Fixed420DIP centered card with wrapping explanation, icon and title; can exceed small scaled viewport without parent scroll. Native layout risk, no rendered clipping claim.')],
'NOT_APPLICABLE authorization or database transaction; native unknown-route/high-scale layout needed.',
'Distinguish fallback reachability from missing implementation; responsive width if actual shortscreen measured.')
record(C+'Models/ProductTile.cs','CallCenter product/offer/category display wrappers.',[
('ProductTile','ImageURI optional, HasImage only URI presence not download success; category glyph fallback and min-variant starting price, preparation station Arabic label from real DTO. No best-seller per-tile badge flag unlike cashier.'),
('OfferChoice','Immutable correct product+offer pairing; CategoryTile constructor owns category and generated IsSelected mutable flag, VM synchronizes reference selected. No physical printing merely station label.')],
'Missing image-error fallback and station/parity test; no real image download invoked.',
'Test failed URI download independently from absent image and preserve correct offer product reference.')
