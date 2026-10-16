from ui_incremental import *
record(P+'ViewModels/ShellViewModel.cs','Permission-filtered management catalog/grouping and actual page factories.',[
('ShellViewModel','Filters real page candidates by permission, payroll OR approve/pay, shifts OR reports, treasury OR manage. Constructs no unauthorized module before filter. Constructor selects first allowed entry; no page if empty.'),
('BuildGroups','Plan groups related allowed items; skips empty groups, tracks placed keys, unplaced items appended more. Avoids accidental unreachable future page because grouping forgotten.'),
('AllCandidates','Actual module map includes cashier/orders/delivery/customers/reports/daily statement/summary/shifts/inventory/recipes/waste/purchasing/suppliers/employees/attendance/payroll/menu/users/audit/controlcenter/backup/settings/printing. Gate can be broader or narrower than endpoint functionality; e.g offers-only user cannot see menu entry gated menu_manage, evaluate intended role.'),
('Navigate','Refuses global unresolved operation and item not already allowed NavItems. Constructs new page VM and real load task per navigation; does not reuse management VM, so unknown not globally registered at construction can be abandoned. Ordinary unsaved forms not registered, navigation discards draft without prompt.'),
('CreateOrders','New Orders VM, fire-and-forget Load; corresponding simple factories forMenu,Settings,Backup,ControlCenter,Printing,Users,Audit,Employees,Attendance,Payroll,Shifts,Inventory,Reports,DailySummary,DailyStatement,Delivery all same instantiate+load pattern. Only cashier reused through session draft.'),
('CreateOwnerDashboard','New owner dashboard receives NavigateByKey callback; target nav lookup applies same permissions. CreateCustomers has no eager query until phone entered; Purchasing/suppliers route same VM, role-limited functions in page.'),
('CreateCashier','Reuses per-user cached VM, fire-and-forget initialize. User lock can replace shell while read loads pending; no page disposal/cancellation scope here.')],
'Manual flow review, not all factory/accessor callable declarations marked audited by conservative map. Missing per-permission capability matrix and navigation-discard/read cancellation tests.',
'Tie page/task lifetime to navigation, register recovered pending before allowing departure, expose dirty-form confirmation only for actual unsaved work; server remains auth authority.')
record(P+'Views/ShellView.xaml.cs','Code-generated grouped native nav controls and VM-to-page mapping.',[
('AttachViewModel','Detaches previous VM property subscription, sets new model, builds bar/content/selected styling. No Unloaded unsubscribe; shell VM not retained in session normally, so no definite external event-root leak proved solely here.'),
('CreateNavButton','Actual Arabic automation name set. Single group clicks Navigate; multi creates native MenuFlyout item per allowed page. Captured item per foreach current C# semantics safe, not closure-all-last bug.'),
('ApplyCurrentPage','Explicit cases every current known page model including placeholder; no implicit WinUI WPF-type template assumption. Each change creates new native page; cached cashier VM events must detach in child.'),
('ApplySelectedItem','Imperatively assigns app.Resources brush each selection, no theme-change listener. Selected nav stays previous Brush after live theme switch if selection unchanged, related UI-004. TextOnDark stable across themes partly mitigates; accent differs.'),
('BuildNavBar','Clears old buttons/list and re-adds per group; no process/DB/printer operation, no hidden unfiltered nav. OnPropertyChanged only page/selected, no collection changed subscription assumes permissions fixed per login.')],
'Native focus/selected contrast and live theme UI test absent; source correctness not screenshot proof.',
'Use theme resource expressions or ActualThemeChanged refresh and appropriate unload cleanup; preserve explicit permission-safe group creation.')
record(W+'ViewModels/CashierViewModel.Shift.cs','Older WPF shift open flow.',[
('OpenShiftAsync','Current culture decimal nonnegative parsed, rounds to piastres with overflow not bounded. No explicit busy guard/permission check inside method; generated async command usual concurrency helps native command invocation but direct calls/shared cross-state not coordinated. Fresh Guid per attempt, unknown result not retained; same safety problems as management UI-016 but reference client.'),
('OpenShiftRequest','After success session real ID/name from response, header refresh/menu load. Time-based label machine-local hour, no malicious scheduled behavior.')],
'WPF source not in current headless client tests; backend authorization/constraints independent.',
'Prevent obsolete reference deployment or update only approved future maintenance with active-client safety parity.')
record(W+'ViewModels/MainViewModel.cs','WPF login/cashier switch and online polling.',[
('MainViewModel','Constructs fixed session/header, shows login,5secDispatcherTimer and immediate ping; no Stop/dispose and overlapping10sectimeout polls possible. No native window close safe gate.'),
('Lock','Unconditionally creates new login, abandoning current cashier VM/cart/pending. ShowCashier creates fresh VM, session shift reused but drafts not; lacks active WinUI per-user retained draft.'),
('ShowCashier','No server active shift discovery after fresh process, session ShiftId initiallynull; cannot automatically find existing server shift through this flow. No backend calls except menu if local session shift already set.')],
'WPF reference not runtime-tested; no hidden persistence beyond normal DispatcherTimer.',
'Document unsupported/reference scope, avoid using this executable as real POS; active architecture WinUI backend remains existing.')
record(W+'ViewModels/HeaderViewModel.cs','WPF reference fixed branding/user/shift and local clock.',[
('HeaderViewModel','30sectimer updates machine-local clock ar-EG; no Stop, registered handler owns header process lifetime. RestaurantName hardcoded, no live settings identity.'),
('UpdateClock','Formats actual DateTime.Now date/time. Legitimate display timer, no destructive/time activation.'),
('Refresh','Notifies actual session role/name/shift; no CanLock or global unknown-operation reflection. No SQL/printing itself.')],
'NOT_APPLICABLE money transaction tests; actual WPF timer/branding not covered by WinUI header stubs.',
'Reference-only documentation; production clock must align business timezone and actual restaurant settings.')
record(W+'ViewModels/LoginViewModel.cs','WPF PIN-only reference login.',[
('PressDigit','Caps six chars, appends command parameter, clears error. Backspace/Clear simple; no busy guard on keypad.'),
('SubmitAsync','Real HTTP login reads user summary, clears PIN and updates session/event. Fail result always invalid PIN, catches raw exception message. ApiClient does not install bearer token, so success screen transition alone cannot prove authorized subsequent data loads.')],
'No credential entry performed. Current client suite compiles WinUI Login classes, not this WPF.',
'If maintained, use actual token contract and safe error classification; do not assume login screen transition means POS connectivity.')
record(W+'Services/ApiClient.cs','Legacy WPF typed HTTP adapter without session-token contract.',[
('ApiClient','Fixed default localhost5151with10sectimeout; no injection constructor, environment setting or bearer token storage. Login deserializes summary shape but ignores server Token; later protected requests lack Authorization and likely401under actual middleware (parent server corroboration).'),
('PingHealthAsync','Catchall false, response not disposed. All other wrappers also leave HttpResponseMessage undisposed; process-lifetime HttpClient reused.'),
('ReadResultAsync','2xx reads JSON and returns true even nullValue; failures best-effort error/status generic. No transport outcomeUnknown classification or exception normalization; caller exceptions can escape async commands.'),
('LoginByPinAsync','POST actual PIN request but no token install. Menu GET/OpenShift/Checkout/Refund/ConfirmElectronic POST, Lookup escaped query/GetOrder encoded ID all real endpoints. No direct DB/printer.'),
('CheckoutAsync','No automatic retry; caller builds mutable same-ID request with no frozen payload. All cancellation forwarded if caller supplies, VMs mostly default.')],
'No legacy live runtime or server request executed. Need authenticated menu checkout contract if reference kept supported; not active WinUI issue.',
'Mark reference unsupported until token contract updated; dispose response resources and use authoritative active API result semantics in approved maintenance.')
record(W+'Converters/Converters.cs','WPF presentation-only converters and note command pairing.',[
('NullToCollapsedConverter','Null/whitespace collapses; Bool and inverse use truepattern, ConvertBack most throws because one-way. Unknown types often render absent rather than parser error; relevant to bindings.'),
('BoolToLoginLabelConverter','Uses dictionary login/checkout/confirm busy/idle with fallback login, avoids parameter exception. No localization resource system; static Arabic strings.'),
('NoteAppendPairConverter','Packages values[0]/[1]with no Length check, potential IndexOutOfRange if malformed MultiBinding, actual paired XAML expected2. EqualsConverter guards length==2correctly.'),
('PiastresToEgpConverter','Long scaled100m, null/unknown fallback0.00misrepresents missing monetary data aszero. This is older UI contract; nullable truth-preserving active converters separate.'),
('OnlineStatusBrushConverter','Literal green/red status plus text converter; CashOnlyVisible/ZeroToVisible/InverseBool maps straightforward. No parsing of user code, URI/process or printing.')],
'No WPF converter tests current suite, need null/unknown/malformed array/currentculture cases. No rendered contrast measured.',
'Show unknown distinct fromzero and guard note argument count; maintain explicit one-way bindings.')
record(W+'ViewModels/CashierViewModel.OrderLookup.cs','WPF order lookup and refund reference flow.',[
('LookupOrderAsync','Trims number, sets loading, reads actual order, prefill remaining total. No overlap epoch/guard, late response can target another number. Close always allowed during pending read/refund.'),
('SubmitRefundAsync','Positive currentculture amount/reason, rounds to piastres. Fresh Guid each retry no frozen payload/outcomeUnknown gate, no pre-check remaining/paid/multi or manager approval fallback; backend must reject unauthorized/oversized. Command CanRefund depends selection/busy but no generated NotifyCanExecuteChangedFor fields, button may remain stale.'),
('TranslateStatus','Exact known lowercase Arabic state labels, unknown raw preserved. After success sets message then LookupOrderAsync immediately clears RefundSuccessMessage, so user success banner disappears while refresh starts. Display bug only; server outcome independent.')],
'Reference not active and not source-linked client tests. No real refund performed.',
'Retire accidental reference use or add actual active-client safeguards under future approved implementation; preserve history actual read.')
