import sys
sys.path.insert(0,'Codex Work/Sub-Agents/Results')
from ui_facet_receipts import *
def r(a,b,c):return(a,b,c)
for prefix,o in [(P,0),(C,-1)]:
 persist_facets(prefix+'ViewModels/HeaderViewModel.cs',{
(32+o,5):r('Returns cached restaurant name, initially fallback and updated SetIdentity after settings read. No network or dynamic settings reload on getter.','_restaurantName; SetIdentity','Fallback versus configured name UI not observed.'),
(34+o,5):r('Returns fixed Arabic logo letter for header identity; not derived from restaurant rename. No IO or generated business behavior.','literal Arabic letter','Branding localization only, no functionality claim.'),
(47+o,5):r('Reads cached current-user name or empty; no login/token validation. Header Refresh explicitly notifies on session transitions.','AppSession.CurrentUser; Refresh','UI020 cached identity can diverge token if proof mutates bearer; parent challenge already owns.'),
(48+o,5):r('Returns first UTF16 code unit of user name when nonempty, not first Unicode grapheme; Arabic base letter normal, supplementary emoji/combining initial can render partial.','UserName[..1]','Low-impact avatar glyph edge; native not observed.'),
(49+o,5):r('Maps four exact role strings to Arabic, unknown role shown raw or empty without user. No role-based permission derivation or server authorization.','CurrentUser.Role switch','Case/vocabulary compatibility test missing.'),
(58+o,5):r('Allows lock only when shared session reports no unresolved operations. Session OperationStateChanged -> Header.Refresh supplies dependent notification; cannot prevent OS kill.','AppSession.HasUnresolvedOperation; Refresh','Coverage depends owner registration/draft materialization, not journal existence alone.'),
(59+o,5):r('Shift-open display based on cached ShiftId nonnull, not server liveness or business-day state. Empty string would count open but normal server IDs nonempty.','AppSession.ShiftId','Read failures/refresh ownership reviewed separately.'),
(60+o,5):r('If cached ID nonnull uses cached name or generic open label; otherwise closed. No timestamp/business-day conversion or server query.','ShiftId; ShiftName','Header can stale until explicit session refresh; native not observed.')},{
(36+o,41):r('Observable TimeText set by UpdateClock every30seconds from DateTime.Now formatted ar-EG; generated setter only notifies text, no timezone conversion.','UpdateClock; DispatcherTimer.Tick','Machine-local clock differs Cairo reporting helpers; no actual machine mismatch asserted.'),
(37+o,41):r('Observable DateText is local calendar weekday/date string from same DateTime.Now, not operational business date. Ordinary generated notification no midnight job.','UpdateClock','Overnight business day can differ wall-date by design; label semantics need operational clarity.')})
for prefix in [P,C,W]:
 active=prefix!=W;shift=2 if active else 0
 getters={(28,5):r('Builds a fresh string of bullet characters matching raw PIN.Length, hiding content visually while exposing length. Does not encrypt or clear original process string.','Pin.Length; OnPinChanged','No actual PIN accessed; screenreader masking/native view behavior separate.')}
 if active:getters[(29,5)]=r('Submit label changes busy/idle wording based on IsBusy, with authored callback notifying on transition. It is only progress wording, not authenticated state.','OnIsBusyChanged; IsBusy','Native button label not observed.')
 contexts={
(19,20):r('Observable Pin includes NotifyCanExecuteChangedFor SubmitCommand and invokes OnPinChanged to refresh masked string. Generated equality avoids same-string callbacks; no numeric validation or securestring generated.','OnPinChanged; SubmitCommand.CanExecute','PIN text exists memory until cleared; no secret values recorded.'),
(22,21):r('Observable ErrorMessage notifies login error binding; raw connection exception text may be assigned by authored submit catch. Setter does not classify auth/network itself.','SubmitAsync','No live error or credential logged.'),
(26,18):r('Observable IsBusy includes SubmitCommand CanExecute invalidation'+(' and authored SubmitLabel notification' if active else '')+'. This disables submit while task pending, but digit/backspace/clear bodies have no busy check.','CanSubmit; SubmitAsync'+('; OnIsBusyChanged' if active else ''),'UI still must bind keypad enabled state if edit lock desired.'),
(32+shift,5):r('RelayCommand typed string PressDigitCommand appends provided parameter after currentlength<6; no CanExecute metadata, one-digit enforcement or busy guard generated.','PressDigit','Normal keypad supplies single digit, direct invalid parameter/server validation separate.'),
(40+shift,5):r('RelayCommand BackspaceCommand removes finalcharacter if nonempty; no CanExecute predicate or request cancellation.','Backspace','Local text editing does not change already constructed login DTO.'),
(46+shift,5):r('RelayCommand ClearCommand clears PIN only; no logout/session revoke and no busy gate.','Clear','Input clear is distinct from root lock or identity reset.'),
(51+shift,5):r('Async SubmitCommand explicitly uses CanSubmit predicate requiring min4/nonbusy. Pin and IsBusy setters invalidate it; body authenticates and raises LoggedIn, but command generation does not persist/revoke tokens or add stronger PIN rules.','CanSubmit; SubmitAsync; LoggedIn','No actual authentication executed; active transport bearer mutation separately reviewed, legacy lacks token protocol.')}
 persist_facets(prefix+'ViewModels/LoginViewModel.cs',getters,contexts)
for prefix,offset in [(P,0),(C,0),(W,-1)]:
 contexts={
(18+offset,42):r('Observable CurrentView stores root navigation VM object; active MainWindow manually maps types to real views, legacy uses WPF templates. No generated viewfactory or data load in setter.','ShowLogin; ShowMain/ShowCashier; MainWindow','Swapping view does not cancel old async work automatically.'),
(19+offset,39):r('Observable IsOnline initially true before first health check; later probe updates, only transport health status. Active stopped guard avoids poststop update, legacy lacks it.','CheckHealthNowAsync; health timer','Health success does not prove SQL/schema/auth/menu ready.'),
(20+offset,39):r('Observable IsLoggedIn toggles header/login state based on successful client flow, not fresh bearer verification each getter. Root ShowLogin changes it without clearing all cached identity fields.','ShowLogin; ShowMain/OnLoggedIn/ShowCashier','Server remains authoritative; root auth/session semantics parent-owned.')}
 if prefix!=W:contexts[(21,39)]=r('Observable IsCloseBlocked set TryClose from session pending guard for close-warning visibility. Field does not itself cancel AppWindow event; MainWindow event wiring handles that.','TryClose; MainWindow closing handler','Forced process termination and unregistered owner recovery not covered.')
 if prefix==P:
  contexts[(43,42)]=r('Observable RecoveredOperationsNotice initialized once from journal metadata list, can be dismissed independently. It neither resends requests nor registers every uninstantiated operation globally.','DescribeRecoveredOperations; DismissRecoveredNotice','Notice disappearance not proof pending outcome resolved; recovery reachability challenge separate.')
  contexts[(56,5)]=r('DismissRecoveredNoticeCommand clears banner only, leaving journal and pending requests untouched. No CanExecute or global unresolved guard generated.','DismissRecoveredNotice','Dismissal does not confirm server outcome; operator may need persistent route back.')
  lockline=81
 elif prefix==C:lockline=56
 else:lockline=40
 contexts[(lockline,5)]=r(('LockCommand wraps authored session-unresolved guard before ShowLogin, preserving in-process pending workspace.' if prefix!=W else 'Legacy LockCommand unconditionally swaps to login; no pending-outcome guard or draft retention.')+' No generated auth revoke or session reset is implied.','Lock; ShowLogin','Existing parent auth and recovery analysis applies; no UI/session action performed.')
 persist_facets(prefix+'ViewModels/MainViewModel.cs',{},contexts)
persist_facets(W+'ViewModels/HeaderViewModel.cs',{
(28,5):r('Returns hardcoded restaurant name regardless server settings; legacy header has no SetIdentity method unlike active client.','literal name','Source limitation, no rename performed.'),
(29,5):r('Returns fixed Arabic logo letter with no state or IO; decorative identity only.','literal logo letter','Native glyph rendering unobserved.'),
(42,5):r('Reads cached session user name elseempty; no bearer validation in legacy API/session.','CurrentUser.Name; Refresh','Legacy runtime auth compatibility unverified.'),
(43,5):r('Maps exact owner/manager/cashier/supervisor role strings to Arabic, unknown raw or empty. No effective-permission calculation.','CurrentUser.Role','Server authorization external.'),
(52,5):r('Shift-open state means cached ID nonnull only; startup empty session does not discover current server shift in getter.','ShiftId','Header can be misleading under stale session, no native observation.'),
(53,5):r('Uses cached shift name or genericopen when ID present, elseclosed. Explicit Refresh required after mutation.','ShiftId; ShiftName; Refresh','No authoritative shift check or time conversion.')},{
(31,41):r('Observable TimeText fed local DateTime.Now ar-EG clock timer. Setter generates notification only, not timezone or polling logic.','UpdateClock','Native/timezone state not observed.'),
(32,41):r('Observable DateText fed machine-local calendar date every30seconds; not server businessday date.','UpdateClock','Overnight reporting should distinguish wall-date versus operationalday.')})
