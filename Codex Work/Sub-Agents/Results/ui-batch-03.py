from ui_incremental import *
# Both station copies below were independently read in full; paired notes explicitly record
# their resource lookup difference. This is not a blind scan or generated function audit.
for pp,station in [(P,'cashier'),(C,'CallCenter')]:
 lookup='ThemeResource' if pp==P else 'StaticResource'
 record(pp+'Controls/EmptyStateView.xaml',station+' empty-list message layout.',[
 ('TitleBlock','Centered shared SectionTitleText title and MutedText wrapping message in MaxWidth360/40DIP padding stack; decorative icon. No list visibility condition owned here: caller must bind count/loading/error correctly.'),
 ('Foreground',f'{lookup} TextMutedBrush for icon; fonts/styles resolve station dictionaries. Title itself has no wrap, so long caller-supplied title may overflow narrow/high-scale viewport; native measurement not observed.')],
 'NOT_APPLICABLE financial functions/transactions/direct printing. Native empty vs loading/error mutual exclusion and Arabic text-scale tests required.',
 'Confirm parent visibility binds collection Count and excludes loading/error; add long title wrap if actual measurement shows clipping.')
 record(pp+'Controls/EmptyStateView.xaml.cs',station+' empty-state title/message dependency-property bridge.',[
 ('EmptyStateView','Initialize components and copy default Title/Message, ensuring default title shown even no DP callback. Properties wrap GetValue/SetValue; no subscriptions/lifetime resources.'),
 ('OnTitleChanged','Copies supplied string to TitleBlock. OnMessageChanged copies MessageBlock; no HTML evaluation, SQL, server or printer calls. Null casts can assign null to native Text; contract should remain string empty.')],
 'Native DP default/change/null tests absent from plain client suite; no business-unit test applies.',
 'Keep message plain text and defaults explicit; native accessibility names/live announcements need observation.')
 record(pp+'Controls/ErrorStateView.xaml',station+' reusable API-read failure and optional retry.',[
 ('RetryButton','Arabic Retry button starts Collapsed and raises click code-behind event, not automatically bound ICommand. A call site must set RetryCommandVisible and subscribe RetryRequested for action.'),
 ('MessageBlock',f'Wrapping message plus fixed Arabic title and {lookup} danger icon. Native screen-reader announcement/live-region absent in source; not a claim inaccessible until tested.40DIP padding in MaxWidth380 reduces available text width.')],
 'Missing native error-retry handler reachability and screen-reader live announcement checks. NOT_APPLICABLE direct financial writes.',
 'Ensure each visible retry is wired and safely retries only read/request with stable identity; expose actionable safe messages.')
 record(pp+'Controls/ErrorStateView.xaml.cs',station+' error message/retry visibility and event bridge.',[
 ('ErrorStateView','Initializes default message; retry defaults false/Collapsed in XAML. DP wrappers no data side effects.'),
 ('OnMessageChanged','Assigns message text, OnRetryVisibleChanged sets actual Button visibility; callbacks assume components initialized through normal XAML construction.'),
 ('OnRetryClick','Raises RetryRequested without running any API itself; subscribers own cancellation/busy guard. No command can-execute check, because event-based design intentional.')],
 'NOT_APPLICABLE business arithmetic; need native DP/event subscriber tests at actual call sites.',
 'Avoid displaying retry button without subscriber; guard double clicks in subscribed command.')
 record(pp+'Controls/LoadingStateView.xaml',station+' generic loading spinner/message layout.',[
 ('ProgressRing','IsActive true while control realized, caller visibility/removal owns duration; no actual operation tracking here.'),
 ('MessageBlock','Centered default loading text with MutedText, no wrap.40DIP padding; long localized status on narrow viewport may clip. Spinner alone does not expose cancel or prove liveness.')],
 'NOT_APPLICABLE direct financial logic; native loading persistence/stall/announcement tests required.',
 'Callers need timeout/error/retry semantics; validate long messages and assistive announcement.')
 record(pp+'Controls/LoadingStateView.xaml.cs',station+' loading message dependency property.',[
 ('LoadingStateView','Sets default text after InitializeComponent. Message Get/Set delegates DP; OnMessageChanged copies to native TextBlock. No timers, cancellation, network or printer action in this class.')],
 'NOT_APPLICABLE business-function tests; native property binding is relevant.',
 'Keep backend operation status in owner VM; do not use spinner presence as operational verification.')
 record(pp+'Controls/SummaryCardView.xaml',station+' KPI label/value/optional caption card.',[
 ('SummaryCardStyle','Card border and stacked label/metric/caption. Metric/source is supplied by callers, not invented here. Caption starts Collapsed; no financial arithmetic.'),
 ('ValueBlock','Large MetricValueText suitable glance reading but no TextWrapping/trim, so long formatted currency may exceed fixed parent cell. Native long-number/DPI check needed; no rendered claim.')],
 'NOT_APPLICABLE ledger/authorization; need unknown-vs-zero and long localized value display cases at parent screen.',
 'Preserve unknown/null display contracts, avoid treating missing value as zero in caller.')
 record(pp+'Controls/SummaryCardView.xaml.cs',station+' label/value/caption dependency property bridge.',[
 ('SummaryCardView','Constructor only initializes components; default empty label/value already blank native text. Get/Set wrappers use native DP.'),
 ('OnLabelChanged','Copies Label to block; OnValueChanged copies Value, no numeric recomputation.'),
 ('OnCaptionChanged','Copies text and collapses null/empty, shows whitespace caption because IsNullOrEmpty not IsNullOrWhiteSpace. Cosmetic layout gap possible; no data loss.')],
 'Native DP default/updates/empty caption tests; NOT_APPLICABLE business transactions or printing.',
 'Prefer whitespace-aware absence if caller contract permits; verify caption and metric accessibility ordering.')
 record(pp+'Controls/PermissionDeniedStateView.xaml',station+' static permission-denied presentation.',[
 ('MessageBlock','Arabic role-denied explanation, title and lock icon in wrapping centered layout. Comment says per-screen guard but this control alone enforces nothing: actual visibility and server auth must be verified separately.'),
 ('Foreground',f'Icon uses {lookup} WarningBrush; muted text and title from station typography. No retry, privilege escalation or PIN request in denied panel.')],
 'NOT_APPLICABLE authorization decisions; verify blocked screen never renders unauthorized data behind overlay, and backend rejects independently.',
 'Do not equate denied panel registration with enforced authorization; ensure explanatory accessible state.')
record(C+'Controls/ModalHeaderView.xaml','CallCenter RTL title/close modal header.',[
('TitleBlock','Star/Auto columns separate wrapping max2line title from close target. Right alignment with RTL; close glyph explicit Arabic AutomationProperties.Name. Uses shared44DIP close style.'),
('CloseButton','Button command assigned code-behind dependency property; no dismiss on background click or Escape/focus trap in header itself. Parent controls full modal semantics.')],
'Native long Arabic title/close focus tests required; NOT_APPLICABLE financial authorization.',
'Keep explicit close name, verify keyboard focus and owner cancellation safety.')
record(C+'Controls/ModalHeaderView.xaml.cs','CallCenter title/close command/automation-ID dependency properties.',[
('ModalHeaderView','Initialize default title; Get/Set properties directly wrap DPs. No long-lived event subscriptions.'),
('OnTitleChanged','Updates actual title. OnCloseCommandChanged assigns ICommand so native control CanExecute behavior applies.'),
('OnCloseAutomationIdChanged','Sets actual close-button AutomationId. Default empty, per-overlay IDs can be supplied; no credentials placed here.')],
'Native DP ordering and command CanExecute required; no direct data/printer actions.',
'Use stable IDs for real UI probes while preserving pending-operation guarded close commands.')
record(C+'Controls/StatusPillView.xaml','CallCenter raw status chip layout.',[
('Label','StatusPillText text within rounded border; color/state assigned OnTextChanged, no inline state map. No screen-reader status description beyond supplied text. Station palette is flat static rather than cashier Light/Dark resources.')],
'Native raw/localized status and contrast tests absent; NOT_APPLICABLE financial state transitions.',
'Ensure text remains meaningful independent of color, and state mapping receives expected raw vocabulary.')
record(C+'Controls/StatusPillView.xaml.cs','CallCenter status dependency property/brush mapping.',[
('OnTextChanged','Updates raw text then instantiates converter and casts returned brush to native Brush. No null/missing-resource defense; dictionary contract must hold. Only text change recomputes; static CallCenter palette not live theme service.'),
('StatusPillView','Initialize only; empty default text no background until actual change. No mutable business status or backend operation.')],
'Native missing resource/new status conversion tests needed; not current headless project.',
'Use complete explicit status vocabulary with safe fallback and coherent contrast.')
record(P+'Controls/SparklineView.xaml','Owner-dashboard trend line/area/endpoint native drawing.',[
('AreaPath','Opacity0.14 fill under LinePath stroke1.8 and endpoint5DIP. No axis/time labels, intended decorative summary; precise data remains KPI/table responsibility.'),
('FlatLabel','Dash for absent/flat series conflates zero/constant nonzero/missing unless accompanying card distinguishes. Native geometry mirrored with inherited RTL should be checked for chronological order.')],
'No native screenshot/accessibility evidence; NOT_APPLICABLE money mutation. Need flat,missing,rising,falling,mixed-sign data.',
'Expose accessible trend description and preserve a table/value alternative; verify RTL time direction.')
record(P+'Controls/SparklineView.xaml.cs','Simple trend path geometry from supplied long series.',[
('SparklineView','Renders on Loaded/SizeChanged; DP callbacks on Points/LineBrush rerender. Points typed object only accepts IReadOnlyList<long>, otherwise silently empty; mutations to existing list not observed automatically.'),
('Render','Reads dimensions, early returns<=1; computes min/max, dash if<2or flat. Padding3 assumes width/height>6, so tiny>1controls can yield negative usable area. max-min and value-min subtract long before double cast, can overflow with extreme mixed-sign data; normal sales bounds not reproved.'),
('BuildLine','Starts first point then segments, caller guarantees>=2; allocates new geometry per resize/data change O(n).'),
('BuildArea','Starts baseline, appends points then closes last baseline; no disposal needed managed WinUI geometry. Last endpoint margin positioned relative Left; RTL/negative values need native check.')],
'No charts library or network. Missing actual geometry invariants for short dimensions/extreme values/list mutation/RTL. Real source not invoked.',
'Clamp drawing region, convert before subtraction if arbitrary long values supported, provide immutable list updates/collection subscriptions.')
