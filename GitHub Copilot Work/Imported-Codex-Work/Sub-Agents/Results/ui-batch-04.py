from ui_incremental import *
record(C+'App.xaml.cs','CallCenter WinUI application launch and separate station journal initialization.',[
('App','Initializes resources then FileRetainedRequestStore.DirectoryFor callcenter, before order desk constructor can recover. No error callback, directory failure may abort startup; Save error later silent, see retained store review.'),
('OnLaunched','Normal creates MainWindow and activates. Only explicit SCREEN_LAYOUT_PROBE compile-time build opens isolated native verification fixture, no hidden runtime switch/timed activation. Probe result is not real backend transaction proof.')],
'Application launch not performed by reviewer; parent controls desktop. NOT_APPLICABLE direct DB/printing here.',
'Handle journal startup failure visibly and keep fixture executables separate from real runtime.')
record(C+'MainWindow.xaml.cs','Native CallCenter root window and manual VM-to-view composition.',[
('MainWindow','Attaches close veto to TryClose, cleanup stops root/header timers and root property subscription. LockRequested forwards actual root command; initial view attached before native maximize.'),
('MaximizeOnTargetDisplay','Native maximize avoids DPI physical-pixel resize math. No root theme preference handling unlike cashier; station colors use static brushes.'),
('OnViewModelPropertyChanged','Routes login/online/current view to actual visibility/content. ApplyCurrentView only Login/CallCenter known VM, unknown null; does not retain native UserControl when root locks/unlocks.'),
('ApplyCurrentView','New CallCenterView each unlock but session caches same desk; child CallCenterOrderPanelView Cart.CollectionChanged never detaches, retaining old view via cached VM. Native window shutdown eventually releases process memory, repeated lock leaks until then.')],
'Missing repeated lock/unlock native weak-reference memory and close event wiring test. No window interactions performed.',
'Detach nested long-lived VM events on unload/DataContext change and bind token revoke to root lock lifecycle.')
for pp,station in [(P,'cashier'),(C,'CallCenter')]:
 record(pp+'ViewModels/HeaderViewModel.cs',station+' persistent restaurant/user/shift header and wall clock.',[
 ('HeaderViewModel','Retains shared session, starts30second native DispatcherTimer, immediately initializes date/time. Cashier additionally subscribes session.OperationStateChanged; Stop does not remove that subscription, though normally both live process lifetime.' if pp==P else 'Retains shared session, starts30second native DispatcherTimer, immediately initializes date/time; CallCenter manually Refresh called by operation state code.'),
 ('UpdateClock','Uses DateTime.Now machine-local timezone and ar-EG formatting, unlike explicitly Cairo BusinessClock. Station with different Windows timezone shows differing clock/date even though backend business date Cairo. Legitimate UI timer, no scheduled destructive action.'),
 ('Refresh','Notifies CanLock/shift/name/role from actual session; no backend fetch. Header can show stale shift if session not refreshed externally. UserInitial slices UTF16 first char, possible half-surrogate for emoji names, minor presentation issue.'),
 ('SetIdentity','Stores actual configured restaurant name but initial fallback hardcoded restaurant branding; no display-name project rename implied. Stop only stops clock; no financial state.')],
'Headless tests substitute HeaderViewModel, so actual timer/timezone/formatting/event subscription not validated there.',
'Use explicit restaurant timezone where operational clock must match; dispose session subscription if header lifetime becomes shorter than session; test Unicode initials.')
record(C+'Views/CallCenterOrderPanelView.xaml.cs','CallCenter cart empty-state and note event bridge.',[
('CallCenterOrderPanelView','Every DataContextChanged attaches current vm.Cart.CollectionChanged with no removal of previous, no Unloaded detach or subscription tracker. Session caches VM while MainWindow recreates views after lock, keeping old UserControls alive through event handlers. Unlike cashier CartPanelView correct unsubscribe.'),
('UpdateEmptyCartVisibility','Shows hint iff ViewModel null/cart empty and hides actual scroll; correct empty switch for subscribed current context. Old collection callbacks read new context if reused, unnecessary updates.'),
('OnNoteChipClicked','Requires actual CartLine/Tag and VM; delegates to guarded AppendNoteText. No direct network/printing in code-behind.')],
'Missing lock/unlock repeated-subscription count and garbage-collection weak reference test. Native binding/visual result not observed.',
'Detach prior VM collection on DataContext change/Unloaded; reattach once on Loaded. Source-level memory retention risk, actual long-session growth unmeasured.')
for pp,station,target in [(P,'cashier',212),(C,'CallCenter',190)]:
 record(pp+'Views/ProductCatalogueView.xaml.cs',station+' native product grid responsive cells and product/offer event routing.',[
 ('ProductCatalogueView','Hooks own Grid SizeChanged and own Loaded; local events do not create external retained VM reference. Reapplies after panel realized.'),
 ('ApplyItemWidth',f'Uses target{target}DIP, minimum two columns, divides available width; declared min/max constants unused. At narrow/high-scale width can assign cells below declared minimum or arbitrarily large cells, so source comment claiming min/max constraint not implemented. Actual content clipping needs native screenshot; no runtime proof.'),
 ('OnProductClicked','Typed tile/VM dispatches SelectProduct command; VM enforces frozen state (availability differs across station implementations).'),
 ('OnOfferTapped','Sets e.Handled true before dispatch, correctly prevents bubbling product click clobbering offer variant selection. Because offer surface may be Border Tapped (paired XAML), keyboard accessibility must be checked separately.')],
'Missing native available width edge cases, large text and keyboard/offer activation tests. Unit VM tests do not exercise routed tap propagation.',
'Enforce actual intended column min/max adaptivity and accessible keyboard offer action; retain Handled correctness.')
record(P+'Views/CategoryStripView.xaml.cs','Category item bridge and native search-focus shortcut.',[
('FocusSearch','Programmatic Focus then SelectAll; no check whether focus succeeds/modal active in this method. Caller shortcut predicate currently omits most modals UI-010.'),
('OnCategoryClicked','Dispatches actual CategoryTile through selected category command. ViewModel typed cast permits design-time null; no financial mutation.'),
('CategoryStripView','Initialize only, no external event subscriptions or disposal resources.')],
'Native F2 with each overlay, RTL caret direction and selection tests absent.',
'Enforce focus isolation in caller and guard keyboard behavior through central modal state.')
for pp in [P,C]:
 record(pp+'app.manifest','Native Windows compatibility declaration for unpackaged WinUI.',[
 ('supportedOS','Five compatibility GUID entries, no requestedExecutionLevel, auto-start, service registration, scheduled task, file association or printer configuration changes in this manifest. Minimum SDK/runtime actual project target remains authoritative; GUID support list alone does not prove older OS runs WinUI.'),
 ('assembly','No explicit DPI awareness declaration visible; WindowsAppSDK/runtime defaults may provide behavior, so cannot infer DPI-unaware bug solely from omission. Native multi-monitor DPI check required.')],
'NOT_APPLICABLE C# functions/database/printing. XML parsed inventory separately; no software installation executed.',
'Document actual tested Windows build/architecture and validate DPI behavior; do not add admin privileges unnecessarily.')
