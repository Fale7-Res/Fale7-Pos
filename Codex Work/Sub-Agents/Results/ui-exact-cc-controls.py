import sys
sys.path.insert(0,'Codex Work/Sub-Agents/Results')
from ui_source_receipts import *
facts={
'EmptyStateView':{
(14,5):('Initializes XAML then copies current dependency-property title and message to the named text blocks; initialization has no data request or navigation side effect.','InitializeComponent; TitleBlock.Text; MessageBlock.Text','Check default and preconfigured text after construction.'),
(23,9):('Returns the Title dependency-property value as a string; does not localize or derive empty-state meaning from domain state.','GetValue(TitleProperty)','Binding and null-value behavior needs UI verification.'),
(24,9):('Writes Title through SetValue so the registered property callback updates displayed title; no external model mutation.','SetValue(TitleProperty); OnTitleChanged','Verify later title changes propagate.'),
(29,9):('Reads Message dependency-property string used for descriptive empty-state content; no asynchronous or database operation.','GetValue(MessageProperty)','Check message binding defaults.'),
(30,9):('Writes Message through dependency-property storage and registered callback; caller controls message content.','SetValue(MessageProperty); OnMessageChanged','Verify empty and multiline content.'),
(33,5):('Casts the changed control and assigns the new title string directly to its named text block; default metadata supplies initial text.','TitleProperty callback; TitleBlock.Text','Null values and long translated headings require UI tests.'),
(36,5):('Updates only MessageBlock.Text when Message changes; no automatic retry, service call or permission decision is hidden in the control.','MessageProperty callback; MessageBlock.Text','Long-message wrapping and RTL remain native-unobserved.')},
'ErrorStateView':{
(17,5):('Builds the error-state visual tree and copies current Message into the named block; retry behavior remains an event contract supplied by the owning view.','InitializeComponent; MessageBlock.Text','Default hidden retry state and text need rendering checks.'),
(25,9):('Returns the stored Message string without parsing error types or altering server failure state.','GetValue(MessageProperty)','Binding default/null coverage missing.'),
(26,9):('Stores Message via dependency-property machinery, invoking text update callback on changes.','SetValue(MessageProperty); OnMessageChanged','Exercise successive different server error messages.'),
(31,9):('Reads the boolean RetryCommandVisible property; visibility alone does not establish whether retry is safe or idempotent.','GetValue(RetryCommandVisibleProperty)','Owner must decide permissible retries.'),
(32,9):('Stores retry visibility flag; callback changes button visibility but no command or service invocation is performed here.','SetValue(RetryCommandVisibleProperty)','Verify false-to-true and true-to-false transitions.'),
(35,5):('Copies changed Message value into error text block using string cast; this is presentation-only propagation.','MessageProperty callback; MessageBlock.Text','Null/long error presentation unobserved.'),
(38,5):('Maps boolean new value to Visible or Collapsed for RetryButton, avoiding reserved space when retry is disabled.','RetryCommandVisibleProperty callback; RetryButton.Visibility','Keyboard focus behavior when hiding focused retry not tested.'),
(41,5):('Raises RetryRequested synchronously with this control as sender and empty arguments if a subscriber exists; actual work, exception handling and duplicate prevention belong to subscriber.','RetryRequested?.Invoke','Verify no subscriber, one subscriber, and repeated clicks at owner boundary.')},
'LoadingStateView':{
(11,5):('Initializes loading-state XAML then copies current Message; it owns no task, timeout, cancellation token or network lifetime.','InitializeComponent; MessageBlock.Text','Native progress announcement and layout not observed.'),
(19,9):('Reads Message dependency property; localized loading text is supplied through metadata or owner binding.','GetValue(MessageProperty)','Default loading caption test missing.'),
(20,9):('Stores new loading Message and triggers registered text callback; does not restart or cancel underlying work.','SetValue(MessageProperty)','Verify message changes during existing loading operation.'),
(23,5):('Updates loading MessageBlock.Text directly from new string value; task completion must change parent loading visibility independently.','MessageProperty callback; MessageBlock.Text','Long and null messages need binding/render tests.')},
'ModalHeaderView':{
(19,5):('Initializes modal header and copies current Title to heading; CloseCommand and automation ID are dependency callbacks rather than modal lifecycle management.','InitializeComponent; TitleBlock.Text','Verify initialization with parent bindings.'),
(27,9):('Reads header title string from dependency storage; no fallback localization or semantic heading announcement is added here.','GetValue(TitleProperty)','Accessible heading behavior remains unobserved.'),
(28,9):('Writes title through dependency property and invokes registered label callback when changed.','SetValue(TitleProperty)','Check long Arabic modal titles.'),
(33,9):('Returns nullable ICommand stored for close behavior; command permission and CanExecute semantics remain with supplied command.','GetValue(CloseCommandProperty)','Check null and disabled commands.'),
(34,9):('Stores nullable close command; callback assigns it to button without invoking close or discarding any draft.','SetValue(CloseCommandProperty); OnCloseCommandChanged','Verify replacement command updates button.'),
(41,9):('Reads stable close-button automation ID string; this identifies UI automation target and is not an authorization boundary.','GetValue(CloseAutomationIdProperty)','Unique IDs across simultaneous dialogs require owner checks.'),
(42,9):('Writes automation ID dependency property; change callback updates attached automation property on actual button.','SetValue(CloseAutomationIdProperty)','Verify supplied automation identifier reaches button.'),
(45,5):('Copies new title string to TitleBlock only; it does not resize dialog or alter close state.','TitleProperty callback; TitleBlock.Text','Native wrapping and RTL not verified.'),
(48,5):('Assigns new value as ICommand to CloseButton.Command; invalid/null value results in no command and no direct view model mutation.','CloseCommandProperty callback; CloseButton.Command','Owner cancellation/unsaved draft policy must be tested independently.'),
(51,5):('Applies changed identifier with AutomationProperties.SetAutomationId on close button; automation naming is independent from visible caption.','CloseAutomationIdProperty callback; AutomationProperties.SetAutomationId','Screen reader name and button focus behavior unobserved.')},
'PermissionDeniedStateView':{(8,5):('Only initializes static permission-denied XAML; displaying this view is explanatory UI, while actual authorization must remain enforced by server and owning navigation.','InitializeComponent','Do not infer endpoint permission enforcement from this visual component.')},
'StatusPillView':{
(11,5):('Initializes status pill XAML without explicitly invoking Text callback; initial default background follows XAML/resources until Text changes.','InitializeComponent','Initial and theme-switch visual states require native verification.'),
(18,9):('Reads text dependency property used as display label and converter input; no independent typed status model is stored here.','GetValue(TextProperty)','Verify localized status strings supported by converter.'),
(19,9):('Writes status text and triggers callback when dependency value changes; same-text theme changes do not inherently invoke this setter callback.','SetValue(TextProperty)','Same status across light/dark changes requires regression check UI004.'),
(22,5):('Normalizes new text to empty string, updates Label.Text, then creates StatusToPillBrushConverter and assigns converted Brush to Root.Background. Brush recalculation is tied to text changes, not theme events.','TextProperty callback; StatusToPillBrushConverter.Convert; Root.Background','UI004 source-only theme/resource risk; no rendered-state claim.')},
'SummaryCardView':{
(17,5):('Only initializes summary card XAML; CaptionBlock is explicitly Collapsed in XAML so default empty caption has no reserved visible caption space.','InitializeComponent; SummaryCardView.xaml CaptionBlock.Visibility','Verify nonempty initial binding callback and card layout.'),
(24,9):('Reads preformatted summary Label dependency string without calculating KPI or applying permissions.','GetValue(LabelProperty)','Owner metric meaning and formatting tested elsewhere.'),
(25,9):('Writes label string through dependency storage and callback; no financial aggregation occurs in reusable card.','SetValue(LabelProperty)','Check changing labels and translation lengths.'),
(30,9):('Reads Value string already formatted by owner; this does not convert piastres, currency or counts.','GetValue(ValueProperty)','Source units must be verified at owner view model.'),
(31,9):('Stores preformatted Value string and updates displayed value through callback; no API or data mutation.','SetValue(ValueProperty)','Verify zero, negative, and long monetary values visually.'),
(36,9):('Reads optional Caption string; visibility decision is implemented in property callback rather than getter.','GetValue(CaptionProperty)','Default empty and whitespace semantics need tests.'),
(37,9):('Writes Caption and schedules registered callback through dependency property change; no external effect beyond card visuals.','SetValue(CaptionProperty)','Check clearing a previously populated caption.'),
(40,5):('Assigns changed label string to LabelBlock.Text, with no transformation or generated fallback.','LabelProperty callback; LabelBlock.Text','Null and translated label rendering unobserved.'),
(43,5):('Assigns changed value string to ValueBlock.Text; currency precision and series correctness remain upstream responsibilities.','ValueProperty callback; ValueBlock.Text','Verify monetary text from owner retains exact units.'),
(46,5):('Copies new caption string then collapses CaptionBlock only for null/empty content; whitespace remains Visible because IsNullOrEmpty does not trim.','CaptionProperty callback; CaptionBlock.Visibility','Whitespace caption is minor layout behavior, not confirmed defect; native unobserved.')}
}
for name,f in facts.items():persist_exact(C+'Controls/'+name+'.xaml.cs',f,'Reusable CallCenter '+name+' dependency properties and explicit visual/event callbacks individually reconciled; no native interaction.')
