"""Persist source-backed UI findings; documentation only, no application invocation."""
from pathlib import Path
import json, math
root=Path.cwd(); out=root/'Codex Work/Sub-Agents/Results'
P='src/Falah.Client.CashierWinUI/'; C='src/Falah.Client.CallCenterWinUI/'
findings=[]
def finding(id,title,severity,status,evidence,trigger,root_cause,impact,counterarguments,tests,recommendation,confidence='HIGH'):
 findings.append(dict(id=id,title=title,severity=severity,status=status,confidence=confidence,evidence=[{'path':p,'startLine':a,'endLine':b,'explanation':e} for p,a,b,e in evidence],trigger=trigger,rootCause=root_cause,impact=impact,counterarguments=counterarguments,regressionTests=tests,recommendation=recommendation,runtimeReproduction='NOT_EXECUTED',screenshotEvidence='NOT_OBSERVED',independentChallenge='Pending parent incorporation; prior printing peer source corroboration reported for UI-001/UI-005',baseline='c35816ecae49633674775dd32061c6ee92f94aaf'))
finding('UI-001','Primary cashier and treasury money requests lose original retry identity on process loss','HIGH','SOURCE_CONFIRMED_RECOVERY_GAP',[
 (P+'ViewModels/CashierViewModel.Checkout.cs',401,426,'Exact checkout retained only in private field; no journal save/recover.'),
 (P+'ViewModels/CashierViewModel.Expense.cs',104,124,'Drawer expense private pending field and rotating ID.'),
 (P+'ViewModels/CashierViewModel.Refund.cs',16,18,'Refund request and identity stored only in memory.'),
 (P+'ViewModels/TreasuryViewModel.cs',162,164,'Treasury deposit/withdraw/transfer retained as in-memory delegate.'),
 (P+'Services/RetainedRequest.cs',29,56,'Durable helper exists but these money operations do not use it.')],
 'Server may commit request, response is lost, cashier process is terminated or power is lost before outcome resolved; operator reconstructs same operation after restart.',
 'Normal close/edit gates and same-process fixed keys cannot preserve immutable submitted DTO/key across forced termination. Primary financial partials bypass existing durable helper.',
 'No reliable station-owned recovery question for original checkout/refund/expense/treasury action; manually repeating with new key can create duplicate real operations. Actual duplicate ledger writes NOT tested.',
 ['Server idempotency prevents repeat of SAME request ID, but restarted UI mints a new ID.','Normal close gate is present; it cannot intercept power loss.','CallCenter checkout already persists operator-scoped DTO, so finding does not apply to its normal write path.'],
 ['Simulate committed-but-lost-response with fake transport, reconstruct actual cashier VM/session with store, assert exact request ID/payload recovered before any new action.','Repeat for refund, expense, safe drop, drawer transfer, shift open/close and treasury operations; assert server ledger unaffected on replay in isolated integration fixture.'],
 'Persist exact versioned typed financial request and owner before dispatch; expose recovery by original identity, block replacement until outcome known; no fixes performed.')
finding('UI-002','Unselected expense and payment buttons combine literal white with dark-theme pale text','MEDIUM','SOURCE_CONFIRMED_THEME_PAIR_DEFECT',[
 (P+'Views/CashierView.xaml',313,340,'All seven expense category buttons use WhiteBrush background when unselected and TextPrimaryBrush foreground.'),
 (P+'Views/CheckoutView.xaml',31,42,'Four payment buttons repeat same unselected pair.'),
 (P+'Styles/Colors.xaml',240,240,'Dark TextPrimaryColor #E6E9EE; precise line automatically verified in source snippets accompanying per-file report.'),
 (P+'Services/ThemeResources.cs',18,35,'Converter brush resolution depends on current root/fallback resources; live resolution and theme transition separate UI-004.')],
 'Resolve these unselected-state bindings in Dark with TextPrimaryBrush=#E6E9EE.',
 'Instance background bindings override correctly-themed SecondaryButtonStyle/PaymentMethodButtonStyle with WhiteBrush; text continues themed.',
 'Near-white labels on white surface, computed source-token contrast ~1.22:1, makes unselected choices hard to distinguish. User visual symptom consistent; reviewer has no actual screenshot and does not claim rendered pixel measurement.',
 ['Light theme has dark TextPrimary and does not reproduce this token-pair defect.','Exact live brush lookup may be affected by merged resource fallback (UI-004).','Do not globally recolor WhiteBrush; genuine paper/on-accent uses require literal white.'],
 ['Render all expense categories and all four payment methods selected/unselected in Light/Dark/System, unchanged and after live toggle; assert resolved text/background luminance and readable native screenshot.','Check disabled/hover/pressed states separately, preserving stored category values.'],
 'Use coherent semantic card/neutral background with themed primary text for unselected state, direct ThemeResource/visual states for live updates; selected accent uses suitable contrast foreground.')
finding('UI-003','Requested pickup kitchen-before-payment stage is absent from active client workflow','HIGH','SOURCE_CONFIRMED_CLIENT_WORKFLOW_GAP',[
 (P+'Views/CartPanelView.xaml.cs',39,42,'Primary button label always payment in both density modes.'),
 (P+'ViewModels/CashierViewModel.Checkout.cs',223,246,'OpenCheckout validates pickup identity then opens tender; no preparation persistence.'),
 (P+'ViewModels/CashierViewModel.Checkout.cs',337,383,'Checkout request combines payment and order creation, customer route Receipt.'),
 (P+'ViewModels/CashierViewModel.Pickup.cs',78,107,'Pickup editor saves only customer/schedule fields, not kitchen stage/order ID.')],
 'Operator selects pickup and wants kitchen preparation before collecting final payment.',
 'Pickup is a detail variant of common checkout, not separate persisted preparation/awaiting-payment state with stage-aware primary command.',
 'Client cannot perform owner-requested Stage A kitchen ticket then later Stage B checkout on same persisted unpaid pickup. Server/Domain end-to-end corroboration required before claiming global architecture absence.',
 ['Electronic pending and cash-on-delivery defer money for other semantics, but they do not expose intended pickup Stage A.','Reprint existing paid order is not initial prepayment preparation.','This is requirement gap, not evidence hidden deletion or fraudulent accounting.'],
 ['Create pickup preparation without payment in isolated backend test; stable ID and no payment movement; initial kitchen exactly once.','Resume same order, collect final money, receipt once; operational pending list hides terminal paid/completed while history remains.','Concurrent stage clicks and ambiguous print/payment responses retain stable identities.'],
 'Design explicit persisted pickup preparation/payment lifecycle using existing backend/domain capabilities where possible; implementation requires separate authorization.')
finding('UI-004','Imperative/converter brush results do not track live per-window theme reliably','MEDIUM','SOURCE_CONFIRMED_WIRING_RISK_RUNTIME_UNVERIFIED',[
 (P+'Services/ThemeResources.cs',18,35,'Only direct app.ThemeDictionaries checked; actual Colors dictionary is merged.'),
 (P+'App.xaml',8,16,'Colors.xaml merged below Application resource dictionary.'),
 (P+'MainWindow.xaml.cs',61,62,'Theme change only assigns root RequestedTheme; no converter input invalidation.'),
 (P+'Controls/StatusPillView.xaml.cs',22,30,'Imperative status background recomputed only on Text DP change.')],
 'Switch root RequestedTheme Light/Dark while selected category/payment/status bound input remains unchanged, or app theme differs from system theme.',
 'Converter returns concrete Brush object; unlike ThemeResource expression, no dependency informs it theme changed. Explicit dictionary lookup also does not traverse merged dictionaries.',
 'Selected/unselected/status colors can remain from previous or system theme, inconsistent with other surface/text resources. Exact WinUI fallback lookup result not runtime-proved.',
 ['WinUI resource fallback may resolve some merged keys correctly for application/system theme.','Recreating/rebinding views may refresh some converters, masking defect.','Root ThemeResource expressions themselves are live; issue limited to returned/imperatively assigned brushes.'],
 ['Use real WinUI root RequestedTheme opposite system theme, compare ThemeResources.Get colors to descendant ThemeResource references.','Toggle repeatedly without changing Text/ExpenseCategory/PaymentMethod; compare same realized objects and brushes.'],
 'Prefer theme-bound visual states/resources; explicitly observe ActualThemeChanged and traverse actual dictionary topology if imperative lookup retained.',confidence='MEDIUM')
finding('UI-005','Recovered electronic confirmation/cancellation has no reachable cashier retry context','HIGH','SOURCE_CONFIRMED_RECOVERY_DEAD_END',[
 (P+'ViewModels/CashierViewModel.Checkout.cs',113,125,'Durable requests recovered by field initializers; availability requires transient LastCheckoutResult electronic pending; command returns if null.'),
 (P+'ViewModels/CashierViewModel.Checkout.cs',149,150,'Cancellation also requires absent result.'),
 (P+'ViewModels/CashierViewModel.State.cs',22,25,'Either durable pending disables CanEditOperation.'),
 (P+'ViewModels/CashierViewModel.Operations.cs',262,271,'OpenOrderLookup guarded by CanEditOperation; cannot reconstruct result through normal UI.'),
 (P+'Views/CashierView.xaml',164,205,'Confirmation/cancel controls live inside HasCheckoutResult overlay, hidden when result null.')],
 'Electronic confirm/cancel request persisted, outcome unresolved, process restarts, user opens cashier.',
 'Retained DTO loads but transient LastCheckoutResult does not; all retry reachability assumes that result. Main session sees unresolved op and blocks lock/navigation/normal close.',
 'Cashier can become frozen after restart even though retained original order ID exists. Correct production reconciliation must not simply delete journal. Potential cross-user context confusion because keys are station-global.',
 ['Normal same-process ambiguous retry keeps result and works.','Manual externally restoring result or modifying journal is not an implemented recovery flow.','Opening different management page before cashier construction can temporarily avoid gate, not resolve financial outcome.'],
 ['Seed fake store with actual ConfirmElectronicPaymentRequest/CancelPendingElectronicPaymentRequest, create fresh AppSession/current user/Cashier VM, assert reachable retry command and UI visible; retry exact order identity.','Confirm no new checkout, result discard or owner change until original outcome resolved; test another operator sees appropriate recovery ownership.'],
 'Recover operation order context from persisted request or present a standalone pending-action recovery panel independent of transient checkout result.')
finding('UI-006','A new pickup inherits previous order optional collection schedule','MEDIUM','SOURCE_CONFIRMED_STATE_LIFECYCLE_DEFECT',[
 (P+'ViewModels/CashierViewModel.Pickup.cs',45,49,'Schedule clear helper resets both fields.'),
 (P+'ViewModels/CashierViewModel.Delivery.cs',48,52,'Only changing order type away from pickup invokes schedule clear.'),
 (P+'ViewModels/CashierViewModel.Delivery.cs',131,138,'ResetDelivery clears customer/address/service only, omits pickup date/time.'),
 (P+'ViewModels/CashierViewModel.Refund.cs',132,139,'Normal dismiss checkout resets delivery; leaves same order type pickup.'),
 (P+'ViewModels/CashierViewModel.Checkout.cs',364,364,'Next pickup request includes surviving RequestedPickupAtUtc.')],
 'Finish scheduled pickup, dismiss result/start next order while order type remains pickup, enter new customer without changing pickers.',
 'Order reset named ResetDelivery also owns shared pickup customer fields but omits pickup-specific schedule.',
 'New customer order carries previous collection appointment or later fails past-time validation, unless operator notices and clears/changes type.',
 ['Switching away from pickup clears schedule, so only consecutive same-type lifecycle affected.','Schedule is displayed in summary, allowing attentive user correction.'],
 ['Complete scheduled pickup using fake API, dismiss result, add next pickup; date/time and RequestedPickupAtUtc must be null until explicitly selected.','Repeat ClearCart/StartNewOrder and historic pending-order review to ensure intentional preserved live draft schedule is not erased.'],
 'Reset optional schedule with resolved new-order lifecycle and explicit cart cancellation; keep historic order review separate.')
finding('UI-007','Late CallCenter customer lookup can overwrite a different phone/new order','HIGH','SOURCE_CONFIRMED_ASYNC_RACE',[
 (C+'ViewModels/CallCenterViewModel.Customer.cs',35,43,'Phone changes start lookups fire-and-forget without generation.'),
 (C+'ViewModels/CallCenterViewModel.Customer.cs',51,77,'Awaits phone lookup then unconditionally assigns name/ID/saved addresses.'),
 (C+'ViewModels/CallCenterViewModel.Customer.cs',217,231,'Clear/reset invalidates no pending lookup task/generation.'),
 (C+'ViewModels/CallCenterViewModel.Checkout.cs',217,220,'Successful checkout upserts captured phone/name/address, persisting inconsistent pairing if operator accepts stale form.'),
 (P+'ViewModels/CashierViewModel.Delivery.cs',96,102,'Cashier counterpart correctly snapshots phone and discards changed-phone response.')],
 'Phone A lookup slow; user replaces with phone B or clears order, B responds first, A responds later.',
 'No requested-phone equality/epoch check after await; async lookup completion mutates latest mutable form regardless of source identity.',
 'Wrong customer name/address offered under another phone; operator may dispatch wrong destination and upsert mismatched customer data. No real PII or DB writes used to reproduce.',
 ['Operator may notice mismatched details and correct them; not guaranteed silent harm.','Same phone concurrent requests usually equivalent; important trigger is different current phone or cleared/new order.','Checkout request is immutable once built, so late response after capture does not rewrite already-sent DTO, but can corrupt form before capture.'],
 ['Deterministic fake HTTP delayed A/B in reverse completion order; assert final name/addresses match B.','Begin lookup then ClearOrder, release response; new form stays blank.','Lookup failure 404 vs timeout must show correct status; changing phone should invalidate old customer/address selections.'],
 'Snapshot normalized phone and monotonic form generation; discard stale responses/cancel on change/reset; clear incompatible resolved fields safely.')
finding('UI-009','Printer status UI exposes only four of six implemented routes','MEDIUM','SOURCE_CONFIRMED_UI_ROUTE_OMISSION_BACKEND_CROSSCHECK_PENDING',[
 (P+'ViewModels/PrintingViewModel.cs',118,124,'Queue/filter Routes contains Receipt, CallCenter, Charcoal,Grill only.'),
 (P+'ViewModels/PrintingViewModel.cs',77,86,'Job labels already recognize DeliveryOrder and DriverDispatch.')],
 'Delivery/dispatch routes have pending or failed jobs and operator opens Printing page.',
 'Hardcoded status-route list lags shared print route vocabulary.',
 'No dedicated queue summary/filter for two routes; attention aggregate may still surface failed jobs. Not total invisibility claim.',
 ['Server overview NeedsAttention may include omitted routes globally, so some failures remain visible.','Actual route usage cross-check delegated to printing reviewer.'],
 ['Provide overview with six routes and each pending/failed/stuck; assert all selectable and counts true.'],
 'Derive displayed route options from complete shared contract plus localized labels.')
finding('UI-010','Workspace shortcut guard omits most open operational modals','MEDIUM','SOURCE_CONFIRMED_GUARD_OMISSION_NATIVE_TRIGGER_UNVERIFIED',[
 (P+'Views/CashierView.xaml.cs',27,29,'Interactive predicate omits expense/refund/delivery/pickup/shift/lookup/open operation dialogs.'),
 (P+'Views/CashierView.xaml.cs',31,54,'F2/F3/F12 act on underlying workspace when predicate passes.'),
 (P+'Views/CashierView.xaml',43,48,'Accelerators registered at workspace while custom modal grids are sibling overlays.')],
 'Native accelerator routing reaches workspace while expense or other omitted modal open; no money command in flight yet.',
 'Custom overlays do not centralize modal state and shortcut predicate only checks variant/payment/result.',
 'Potential underlying focus change, extra flyout or checkout sheet while another form open, risking confused data entry. Runtime keyboard reachability/focus path not executed.',
 ['WinUI accelerator scope/focus routing may prevent some combinations; source predicate omission alone not proof key delivered.','Busy/unresolved write guard still prevents many changes during actual sends.'],
 ['Actual WinUI keyboard F2/F3/F12 + Tab/ShiftTab/Escape for every modal, assert no background focus/action and only one modal active.'],
 'Centralize IsAnyModalOpen plus accessible focus isolation and apply to all shortcuts.',confidence='MEDIUM')
finding('UI-011','Discarded definitively rejected CallCenter reprint survives in journal','MEDIUM','SOURCE_CONFIRMED_JOURNAL_CLEAR_OMISSION',[
 (C+'ViewModels/CallCenterViewModel.Checkout.cs',359,378,'Every manual print request persisted; definite failure retained with _hasUnresolvedPrint false.'),
 (C+'ViewModels/CallCenterViewModel.Checkout.cs',384,392,'ClearResolvedPrint nulls memory but never clears PrintOwnerKey journal.'),
 (C+'ViewModels/CallCenterViewModel.Checkout.cs',86,91,'Next launch restores file and marks _hasUnresolvedPrint true regardless of original definite rejection.')],
 'Manual reprint receives definite first rejection, operator clears lookup/new order, restarts later.',
 'Safe discard memory path omits corresponding persistent store clear.',
 'Old rejected request reappears as ambiguous unresolved, can block fresh operation or queue an old reprint on retry when permissions/preconditions change. Does not prove duplicate sale.',
 ['Known successful print clears file correctly.','Unknown print intentionally cannot be cleared; recommendation applies only safe definite rejection discard.'],
 ['Persist definite rejected print, clear through actual UI VM, reconstruct operator desk and assert no pending print/file.','Ambiguous failure must continue surviving same clear attempt.'],
 'Clear disk atomically when explicitly discarding known-rejected print; preserve unknown state separately.')
finding('UI-012','CallCenter customer-copy reprint requests kitchen preparation routes again','HIGH','SOURCE_CONFIRMED_CLIENT_FLAG_BACKEND_PEER_CORROBORATED',[
 (C+'ViewModels/CallCenterViewModel.Checkout.cs',337,343,'Customer follow-up reprint uses DTO default IncludeKitchenRoutes.'),
 ('src/Falah.Application/Printing/PrintingRequests.cs',12,17,'IncludeKitchenRoutes defaults true.'),
 (P+'ViewModels/CashierViewModel.Operations.cs',316,322,'Cashier explicitly false and comments warn duplicate kitchen ticket means cook twice.')],
 'CallCenter operator looks up existing delivery order and requests customer confirmation copy reprint, with kitchen-routed products.',
 'CallCenter omitted explicit false flag added to corresponding cashier reprint path.',
 'Potential duplicate charcoal/grill preparation work and food waste, in addition to requested customer copy. Backend route suppression cross-checked by printing peer as parent reports; parent should attach final handler evidence.',
 ['A deliberate full kitchen reprint may be legitimate if explicitly requested, but current action is customer follow-up copy.','Settings may globally disable preparation tickets; then duplicate prep not emitted.','Print jobs can remain queued/failed; no claim actual printer ran.'],
 ['CallCenter reprint fake API asserts IncludeKitchenRoutes false.','Isolated print-job handler fixture with grill/charcoal items asserts customer copy only for follow-up; explicit kitchen reprint separate labeled action.'],
 'Send false for customer-only reprint; expose explicit separately confirmed kitchen reprint only if requirement exists.')
finding('UI-013','Legacy WPF change display parses EGP tender as integer piastres','MEDIUM','SOURCE_CONFIRMED_LEGACY_CLIENT_DEFECT',[
 ('src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs',29,37,'ChangeDue subtracts total piastres from unscaled long text.'),
 ('src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs',44,46,'Tender initialized as two-decimal EGP string.'),
 ('src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs',93,95,'Actual send correctly parses decimal then *100, exposing display/send disagreement.')],
 'Run older WPF cashier and tender 100 EGP for 90 EGP order.',
 'Display parses 100 as100 piastres or fails for100.00 while authoritative send converts to10000.',
 'Displayed change zero instead of10 EGP; server-recorded tender may still correct. Does not affect active WinUI CashInput change implementation.',
 ['WPF is technical-reference client, not currently active production executable.','Backend recomputes change; screen can still mislead operator before/after tender.'],
 ['WPF ChangeDue test 90 EGP total with100 and100.00 tender both returns1000 piastres; Arabic/current-culture formats reviewed separately.'],
 'If WPF remains supported, use same tested CashInput units and notifications; otherwise prevent accidental deployment.')
finding('UI-014','Successful restore is reported incomplete because UI expects Completed instead of Succeeded','HIGH','SOURCE_CONFIRMED_CROSS_LAYER_STATUS_MISMATCH',[
 (P+'ViewModels/BackupViewModel.cs',262,282,'CompleteRestore success comparison expects Completed.'),
 ('src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs',168,168,'Actual successful restore record status Succeeded.'),
 (P+'ViewModels/BackupViewModel.cs',84,93,'Same VM LastRestoreLine recognizes Succeeded, inconsistent local vocabulary.')],
 'An authorized real restore succeeds and returns its success record.',
 'UI compares result status to wrong literal Completed; server stores enum Succeeded.',
 'UI tells operator restore did not complete after database already replaced, leaves restore target shown and can invite repeating dangerous operation. No restore executed by reviewer.',
 ['History reload can show success through LastRestoreLine, partially exposing correct outcome.','Retained helper clears successful HTTP result so this is not indefinite unknown-state recovery; repeated fresh restore still dangerous.'],
 ['Fake API returns successful DTO Status Succeeded; assert success note, target closed and no false RestoreError.','Blocked/Failed records must remain reported non-success; no production DB restore needed for client contract test.'],
 'Use shared enum/contract success vocabulary and avoid treating generic HTTP success as outcome proof; maintain strict restore controls.')

# Correct exact palette anchor rather than retain a guessed historical line.
palette=(root/(P+'Styles/Colors.xaml')).read_text(encoding='utf-8-sig').splitlines()
for f in findings:
 for ev in f['evidence']:
  if ev['path']==P+'Styles/Colors.xaml':
   ev['startLine']=ev['endLine']=next(i+1 for i,l in enumerate(palette) if '#E6E9EE' in l)
for f in findings:
 for ev in f['evidence']:
  n=len((root/ev['path']).read_text(encoding='utf-8-sig').splitlines())
  if not 1<=ev['startLine']<=ev['endLine']<=n: raise ValueError((f['id'],ev,n))
(out/'ui-findings.json').write_text(json.dumps({'baseline':'c35816ecae49633674775dd32061c6ee92f94aaf','scope':'UI client static review; no screenshot/runtime claims','findings':findings,'rejectedCandidates':[{'candidate':'All order reprints inherently duplicate kitchen','reason':'Cashier reprint explicitly IncludeKitchenRoutes false; defect specific to CallCenter omission.'},{'candidate':'Any DateTime.Now/timer constitutes hidden time bomb','reason':'Observed timers update health/clock and shift naming, legitimate purposes; no malicious trigger proved.'},{'candidate':'Every pending-request test proves app durability','reason':'Helper tests do not prove adoption or reachable recovery in concrete screens.'}]},ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Saved {len(findings)} source-backed findings/candidates. No application code executed.')
