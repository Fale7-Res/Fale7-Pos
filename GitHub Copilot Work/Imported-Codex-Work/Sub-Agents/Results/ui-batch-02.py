from ui_incremental import *
record(P+'Services/ApiClient.Menu.cs','Menu/category/product/image/tax/price-rule HTTP authoring adapter.',[
('GetAllCategoriesAsync','GET all categories uses SendAsync rather than read-only ReadAsync; failure message incorrectly implies unknown money operation. GetAllProductsAsync same. No automatic resend or writes in these GET wrappers.'),
('CreateCategoryAsync','POST typed CreateCategoryRequest; UpdateCategory PUT encoded category ID; SetCategoryActive POST active; bool commands use SendEmptyAsync so successful empty response accepted. Server validates actor/input; no SQL here.'),
('CreateProductAsync','Typed product POST; UpdateProduct PUT encoded ID; SetProductImage PUT JSON typed request, not file path/process. RemoveProductImage Delete uses encoded product and actor query, no path traversal in client URL construction.'),
('CreateTaxRuleAsync','Create POST, Update PUT encoded tax rule ID. Price rule Save uses POST DTO and decoded authoritative response; no client-side hidden price mutation.')],
'All methods accept and forward cancellation token to HTTP/helper. Current tests should assert no-content success for each bool mutation and read-vs-write diagnostic. Native UI and real API not exercised.',
'Use read helper for read-only GET diagnostics; keep proper ID escaping and server-only financial authority.')
record(P+'Services/ApiClient.People.cs','Employee/delivery-directory/cash-count/restore/dashboard API adapters.',[
('CreateEmployeeAsync','POST employee typed DTO, UpdateEmployee PUT encoded ID; successful DTO deserialized. Manager/employee permissions are server responsibility.'),
('GetAllDeliveryServicesAsync','GET includes inactive entries needed for reactivation. Create service POST, update PUT ID encoded. GET incorrectly uses SendAsync ambiguous-operation message.'),
('GetCashCountDenominationsAsync','GET fixed denomination vocabulary; GetCashCounts GET encoded business day; SaveCashCount POST to business day counts typed DTO. Concrete count uses durable helper, validation in view model/server separate.'),
('RestoreBackupAsync','POST encoded record ID restore with typed reason/phrase/actor approval request. Dangerous real DB operation reachable through existing server; no invocation in audit. DTO status issue UI-014 is consumer behavior, not this transport wrapper.'),
('GetOwnerDashboardAsync','GET one owner snapshot with encoded actor and ISO dates. Uses SendAsync for a read; no server-side date mutation implied. Cancellation forwarded all methods.')],
'Read-only network failure diagnostic tests absent in reviewed tests. EmptyResponse contract not relevant where typed DTO mandated. No restore or DB writes tested.',
'Distinguish read failure text from unresolved writes; avoid client interpreting HTTP success as completed restore.')
record(C+'Services/ApiClient.cs','CallCenter typed HTTP transport and bearer-token lifecycle.',[
('ApiClient','Injected HttpClient supported for fixtures. Default has10-second timeout and localhost:5151 URI; AppSession overrides from FALAH_SERVER_URL. HttpClient retained process-wide without IDisposable; acceptable reuse but owner lifetime unclear.'),
('ProductImageUri','Encodes product and image version; ignores unknown image version by returning null. Bitmap downloads happen outside typed authenticated HTTP (same URL may be intentionally anonymous server; verify authorization server-side).'),
('PingHealthAsync','Response disposed; catch all including cancel returns false, health callers cannot distinguish explicit stop/offline. HTTP health says reachable, not full workflow healthy.'),
('ReadResultAsync','Success requires nonnull deserialized JSON; empty/malformed success interpreted unknown by SendAsync. Failure reads error JSON best-effort, status>=500 flagged unknown. Auth failures fallback invalid PIN text even outside login.'),
('SendAsync','Catches transport/cancellation/JSON/IO failures as unknown, retains status0; sends GET reads through same diagnostic as writes. No automatic retry; callers own idempotency. bool endpoints would reject empty success because no SendEmpty adapter exists here, but currently typed calls predominate.'),
('LoginByPinAsync','Authenticates existing endpoint, installs bearer DefaultRequestHeaders and maps actual returned identity/permissions. No hardcoded account in client; no PIN/token values exposed by this review.'),
('LogoutAsync','Calls server endpoint then clears bearer even on failure; returned HttpResponseMessage not disposed and status ignored. MainViewModel.Lock does not call this function, so implemented revoke is unused in ordinary lock path (parent security lead notified).'),
('GetMenuAsync','Read menu via GET. GetCurrentShift/GetSettings/GetTaxRules/GetActiveDeliveryServices/GetTreasuryBalance GET actual server state; read failure text misclassifies uncertainty as money operation.'),
('CheckoutAsync','POST /api/orders/checkout; Refund/ConfirmElectronic/CancelPending POST named typed endpoints, no client SQL or direct ledger changes. All pass cancellation through.'),
('OpenShiftAsync','POST open; PrepareShiftClose/CloseShift POST real endpoints even though CallCenter UX does not offer these actions. Exposed class method is not public server authorization bypass.'),
('LookupOrderByNumberAsync','GET escaped number; GetOrder encoded path. IssueManagerApproval POST PIN request, AddExpense POST typed expense (methods copied for shared vocabulary, not all active CallCenter UI).'),
('CreatePrintJobsAsync','POST order print-jobs with encoded path and full request; does not override IncludeKitchenRoutes so CallCenter caller omission propagates UI-012. SafeDrop/CashierCashMovement/OpenCashDrawer also typed POST; no native printing here.')],
'CallCenterDurabilityTests fake HTTP handler can validate exact request IDs/body. Need response disposal, login/logout lifecycle, offline read diagnostic, non-JSON success, permission status and cancellation cases. No external endpoint requests executed.',
'Invoke actual logout/revoke on lock if required contract; unify safe read/empty-success semantics and cancellation without hiding unknown writes; restrict UI without treating client methods as authority.')
record(C+'ViewModels/MainViewModel.cs','CallCenter root PIN/intake navigation, polling and close/lock safety.',[
('MainViewModel','Shows login then5second timer plus immediate Ping. No single-flight guard: timeout10seconds can overlap health requests. No recovered operation summary unlike cashier; actual desk restore happens after login.'),
('CheckHealthNowAsync','Late result suppressed only after Stop; overlapping results may reverse apparent online state. No cancellation to outstanding HTTP.'),
('TryClose','Reads session draft unknown/busy gate and InfoBar flag; normal close safety only, power loss relies journal.'),
('Stop','Stops health/header timers, no outstanding initialization cancellation.'),
('Lock','If no unresolved write shows login only; CurrentUser/bearer not cleared and Api.LogoutAsync uncalled. Parent security lead cross-checking intended lock/revoke contract.'),
('OnLoggedIn','async void event handler gates pos permission then sets CurrentView and awaits InitializeAsync. If operator locks while read initialization outstanding, old completion still updates VM/header/session. No login generation captured and thrown initialization exceptions escape async void outside LoginViewModel catch.')],
'Current plain test timer is no-op; cannot validate native overlap/focus. Missing logout endpoint invocation on lock, initialization after lock/user switch and async exception tests.',
'Use cancellable/generation-bound initialization, safe task event bridge and real token revoke lifecycle; keep unknown order close gate.')
record(C+'ViewModels/LoginViewModel.cs','CallCenter PIN login commands and masking.',[
('PressDigit','Caps PIN length6, appends command string, clears error; no IsBusy guard so keypad can alter visual PIN during await though actual sent string already captured. Backspace/Clear likewise alter displayed string.'),
('CanSubmit','Requires minimum4 and !busy with generated CanExecute updates.'),
('SubmitAsync','Real API login sets CurrentUser on success, clears PIN then LoggedIn event. ApiResult offline/5xx/403 converted indiscriminately into invalid PIN; exception branch shows raw ex.Message. Async void event continuation errors not caught after await.')],
'No credentials entered by reviewer. Missing rejected network vs credential UI message and keypad busy-lock tests.',
'Safe actionable error classification, freeze keypad while busy, clear session/token coherently on lock; preserve actual server identity.')
record(P+'ViewModels/AttendanceViewModel.cs','Per-Cairo-date attendance and approved financial correction editor.',[
('AttendanceViewModel','Uses Windows Egypt Standard Time for current date; service backend independent. Loads durable correction through field initializer, but no constructor session pending registration or recovered editor context.'),
('ReloadAsync','Coalescing do/while with requested date snapshot, discards stale date response and loops; clears old records before each fetch. CanBrowse blocks while correction open/busy. Correctly handles read ordering compared to UI-007.'),
('PreviousDay','Date-- with CanBrowse then Load; NextDay analogous. Property date changed only label; methods explicitly load. No cancellation when page abandoned.'),
('OpenCorrection','Requires permission, browse, not loading and no read error. Maps actual known status, count/hour fields, reason blank. Does not guard recovered _correction.HasPending; recovered request can disagree with newly displayed target record.'),
('NormalizeNumber','Arabic/Persian digits and decimal separator. TryHours finite/nonnegative invariant double; TryCount nonnegative int, rejects decimal/sign. Server must enforce max hours/domain consistency.'),
('SaveCorrectionAsync','If retained request exists, skips current editor fields and sends original exact DTO; valid retry semantics but UI may show different CorrectionRecord after recovery. Fresh validates permission/status/numeric/reason then Guid request, delegates real handler.'),
('OnIsBusyChanged','Registers management pending only when busy changes, not at construction when recovered helper already pending; can navigate/lock before any retry unless another path registers. On success closes editor, clears reason, reloads authoritative records to avoid stale overwrite.')],
'AttendanceWorkflowTests need review; missing concrete recovered correction UI target/context and initial pending registration tests. No attendance writes executed.',
'Surface recovered correction target and block incompatible new target; register pending at construction and allow explicit retry without misleading active form.')

# These project files were read in full before interruption, and rechecked as build metadata.
for pp in [P,C]:
 record(pp+('Falah.Client.CashierWinUI.csproj' if pp==P else 'Falah.Client.CallCenterWinUI.csproj'),'Unpackaged WinUI3 x64 build/dependency metadata.',[
 ('TargetFramework','net8.0-windows10.0.19041.0 with TargetPlatformMinVersion17763; UseWinUI/WinExe/x64/win-x64. WindowsAppSDKSelfContained true carries Windows App SDK, not automatically proof .NET runtime self-contained because SelfContained absent here.'),
 ('PackageReference','Pinned WindowsAppSDK1.7.260224002, Windows SDK BuildTools10.0.22621.3233, CommunityToolkit.Mvvm8.3.2. Supply-chain advisory lookup handled parent; no vulnerability safety inferred.'),
 ('EnableMsixTooling','Required PRI build resources while WindowsPackageType None, no MSIX identity. ApplicationManifest app.manifest declares native compatibility/DPI separately.'),
 ('ScreenLayoutProbe','Compile-time property defines SCREEN_LAYOUT_PROBE and linked test native probe; cashier additionally has FOOTER_LAYOUT_PROBE and management probe. Release normal build unaffected unless property explicitly supplied. Probe-only fixture must never be reported real backend UI validation.')],
 'Build not run by this reviewer; parent owns environment/build evidence. Native probe source separate tracked review; headless sources linking does not compile XAML.',
 'Publish explicit supported Windows/x64/.NET requirements and avoid deploying probe configuration; inspect locked transitive assets separately.')
record(W+'Falah.Client.Cashier.csproj','WPF technical-reference executable build metadata.',[
('TargetFramework','net8.0-windows, UseWPF true WinExe; nullable and implicit usings. AnyCPU/default runtime not WinUI x64 explicit. References Application + MVVM8.3.2; no SQL or printing dependency in this project file.')],
'Not source-linked by active client test project; build success says little about active WinUI.',
'Clearly designate reference executable to prevent accidental installation; dependency advisories parent-owned.')
record(T+'Falah.Client.Tests.csproj','Headless source-linked client regression project.',[
('TargetFramework','net8.0 nonnative test project; xunit2.5.3/TestSDK17.8/MVVM8.3.2. NativeFooter/Screen/Management probes explicitly excluded from normal test compilation.'),
('Compile Include','Links cashier services/models/VM plus CallCenter services/models/VM and root/login. ThemeService/Resources/Settings.Appearance excluded, so no coverage of real WinUI theme resource resolution; XAML/views not compiled.'),
('ProjectReference','Only Application project referenced, tests compile copied/link source rather than actual frontend assemblies. Stub Header/DispatcherTimer replace real native dependencies; source business logic tests valid but UI/runtime behavior not proved.')],
'Explicit test-scope caveat required in final report; no tests executed by reviewer.',
'Keep headless regression suite but add native/real HTTP isolated lifecycle evidence. Do not count native probe files as executed unit tests.')
