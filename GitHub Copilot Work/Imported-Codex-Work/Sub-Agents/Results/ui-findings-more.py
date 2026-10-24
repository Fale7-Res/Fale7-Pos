from pathlib import Path
import json
root=Path.cwd(); p=root/'Codex Work/Sub-Agents/Results/ui-findings.json';data=json.loads(p.read_text(encoding='utf-8-sig'))
P='src/Falah.Client.CashierWinUI/';C='src/Falah.Client.CallCenterWinUI/'
def ev(path,needle,reason,width=1):
 lines=(root/path).read_text(encoding='utf-8-sig').splitlines(); start=next(i+1 for i,l in enumerate(lines) if needle in l)
 return {'path':path,'startLine':start,'endLine':min(len(lines),start+width-1),'explanation':reason}
def add(id,title,severity,evidence,trigger,cause,impact,counter,tests,rec,status='SOURCE_CONFIRMED_STATIC_REASONING',confidence='HIGH'):
 if any(f['id']==id for f in data['findings']):return
 data['findings'].append({'id':id,'title':title,'severity':severity,'status':status,'confidence':confidence,'evidence':evidence,'trigger':trigger,'rootCause':cause,'impact':impact,'counterarguments':counter,'regressionTests':tests,'recommendation':rec,'runtimeReproduction':'NOT_EXECUTED','screenshotEvidence':'NOT_OBSERVED','independentChallenge':'Parent/peer challenge requested; not yet incorporated','baseline':data['baseline']})
add('UI-015','CallCenter lock/unlock retains old order-panel visual trees through cart subscription','MEDIUM',[
 ev(C+'Views/CallCenterOrderPanelView.xaml.cs','DataContextChanged +=','Adds vm.Cart.CollectionChanged, no previous/unloaded detach.',6),
 ev(C+'MainWindow.xaml.cs','CallCenterViewModel vm => new CallCenterView','New view for same retained draft on unlock.'),
 ev(C+'Services/AppSession.cs','private readonly Dictionary','Session caches drafts across locks.',1)],
 'Repeatedly lock/unlock same operator while CallCenter process remains open.',
 'Long-lived cached VM Cart event holds every prior UserControl handler; subscriptions never removed on unload or DataContext replacement.',
 'Memory/native visual resource growth and repeated invisible callbacks over long shift; actual memory growth not measured.',
 ['Process exit releases memory.','UserControl own local events are not the issue; external cached Cart event is strong root.','Cashier CartPanel correctly unsubscribes and is not affected.'],
 ['Native repeated lock/unlock, weak-reference old panel and force test GC; only current panel should remain.','DataContext A->B->A event subscription counts exactly1 and no callbacks on detached view.'],
 'Track subscribed VM, detach on change/unload, reattach once Loaded.')
add('UI-016','Management shift actions abandon unknown outcomes and generate a new request identity per retry','HIGH',[
 ev(P+'ViewModels/ShiftsViewModel.Open.cs','RequestId: $"shift-open-','Fresh Guid built each ConfirmOpenShift invocation.',4),
 ev(P+'ViewModels/ShiftsViewModel.Open.cs','private void CloseOpenShiftModal()','Unconditional modal close while request possible.'),
 ev(P+'ViewModels/ShiftsViewModel.Close.cs','CloseRequestId: $"shift-close-','Fresh close ID every attempted confirmation.',4),
 ev(P+'ViewModels/ShiftsViewModel.cs','public partial class ShiftsViewModel','Whole3partial class lacks pending-operation/session registration; anchored declaration for scope.')],
 'Management opens/closes shift, backend response lost/5xx, user retries or navigates/locks while write in flight.',
 'Separate management presentation did not adopt cashier immutable pending/outcomeUnknown protocol; only own busy booleans, no session registration or retained key.',
 'Cannot safely reconcile original shift operation; new attempt may be rejected by single-open/already-closed server guards or mislead state, and user can abandon in-flight money context. Actual duplicate server shift transaction not claimed.',
 ['Backend constraints/idempotent status checks can limit duplicate financial writes, but cannot reconstruct original exact request using newly minted key.','Primary cashier same-process pending flow is better; this is management-specific.'],
 ['Fake commit-lost-response, retry asserts identical typed DTO/key and immutable counted/opening cash.','Root TryClose/Lock/Shell.Navigate blocked while management open/close busy or unresolved.','First definite rejection permits correction/new attempt, unknown requires same attempt reconciliation.'],
 'Share durable operation coordination between both shift presentations and register global pending before dispatch.')
add('UI-017','Recovered payroll transition or payment cannot reach its retry command','HIGH',[
 ev(P+'ViewModels/PayrollViewModel.cs','private readonly RetainedRequest<TransitionPayrollRequest>','Transition and pay recover before constructor, without SelectedRun/context.',2),
 ev(P+'ViewModels/PayrollViewModel.cs','if (!CanEditAction || IsPayOpen) return;','Load refuses when retained request exists, leaves SelectedRun null.'),
 ev(P+'ViewModels/PayrollViewModel.cs','public bool CanAdvance =>','Requires NextStatus from transient selected run.'),
 ev(P+'ViewModels/PayrollViewModel.cs','if (IsBusy || _createRequest.HasPending || _transitionRequest.HasPending || !CanPay || !IsPayOpen)','Pay retry refuses before inspecting recovered DTO unless transient IsPayOpen.')],
 'Restart after unknown payroll transition/pay and reopen Payroll module.',
 'Durable DTO recovery is not accompanied by run selection/pay-dialog reconstruction; all routes to restore context blocked by same pending state.',
 'Retained financial operation stranded; ordinary page cannot reconcile original status/payment. Unlike create-run retained branch, no standalone retry in this VM.',
 ['Same-process retry has current context and generally works.','Calling private APIs externally/erasing journal is not implemented UI recovery.','Server payment idempotency remains separate; no duplicate payout claimed.'],
 ['Fake retained transition/pay DTO, construct fresh VM/session, assert explicit retry available without selecting new run/account.','Read original target by stored identity and show original intended action; exact DTO resends; no new payment until resolved.'],
 'Recover run/visible action from stored request or expose generic pending-action verification independent of transient selection.')
add('UI-018','Late recipe-detail response can replace another recipe selection before editing','HIGH',[
 ev(P+'ViewModels/RecipesViewModel.cs','partial void OnSelectedRecipeChanged','Each selection launches LoadDetailAsync without cancellation/epoch.',7),
 ev(P+'ViewModels/RecipesViewModel.cs','public async Task LoadDetailAsync()','Snapshots row but no identity check after await; assigns Detail directly.',24),
 ev(P+'ViewModels/RecipesViewModel.cs','public void StartEdit()','Copies currently published Detail, not validated against selected row.',10),
 ev(P+'ViewModels/RecipesViewModel.cs','new SaveRecipeRequest(','Save target read from Detail RecipeId, so pane/selection mismatch can update wrong recipe.',3)],
 'Select A then B while A detail response slower; A arrives last, then operator edits believing selected B.',
 'No version/selected-ID validation around asynchronous recipe detail publishing.',
 'Wrong ingredient list/recipe identity can be edited and saved, affecting future inventory consumption/costing. Server authorizes a valid recipe update but cannot infer operator intended different selected row. Runtime write not tested.',
 ['Attentive operator may notice name/ingredient mismatch.','If native selection disabled during detail load in actual XAML some route may be mitigated; source VM direct selection has no guard, native test pending.'],
 ['Fake delayed A/B reverse completion asserts Detail.RecipeId equals SelectedRecipe key and StartEdit uses B.','Close/change selection while reading must invalidate old response/finally flags.'],
 'Use monotonic detail generation and validate again after every await; freeze target identity across edit/save.',confidence='MEDIUM')
add('UI-019','Recipe yield conversion silently truncates fractional pieces-per-base-unit','MEDIUM',[
 ev(P+'ViewModels/RecipesViewModel.cs','if (!MilliUnits.TryParse(line.YieldRatioText','Accepts3decimal fractional input.',8),
 ev(P+'ViewModels/RecipesViewModel.cs','yieldRatio = ratioMilli / 1000;','Integer division truncates1.5to1and only rejects<=0.',7)],
 'Enter yield ratio1.5or2.9pieces per base unit in recipe editor.',
 'MilliUnits parser accepts fractional unit then conversion to integer floor without checking remainder.',
 'Stored recipe ratio differs from typed value and changes ingredient usage/cost calculation on future sales. No real recipe updated.',
 ['Backend expects integer and may reject other invalid values, but receives already-truncated valid integer.','Values<1truncate0and are rejected; fractional>1is problematic.'],
 ['Enter1.5,2.999Arabic١٫٥; must reject with explicit integer requirement before HTTP, not send1/2.','Whole numbers1,2and textual2.000 accepted exactly, enforce overflow bound.'],
 'Validate integer divisibility before division or use dedicated localized positive-integer parser.')
add('UI-020','PIN-change retry proof replaces shared bearer despite preserving visible user identity','HIGH',[
 ev(P+'ViewModels/UsersViewModel.Actions.cs','var proof = await _session.Api.LoginByPinAsync(original.NewPin);','Uses session-mutating login for proof during retained self-change retry.',3),
 ev(P+'Services/ApiClient.cs','_sessionToken = result.Value.Token;','Successful login replaces shared bearer token.',4),
 ev(P+'ViewModels/UsersViewModel.Actions.cs','new("users.change-pin")','Journal key station-global, no user ownership suffix.')],
 'Self PIN change response unknown, retry uses new-PIN proof; especially restart/recovery under another authorized operator or proof PIN matches different user.',
 'LoginByPinAsync has bearer installation side effect, but proof caller assumes read-only credential test and does not reconcile session.CurrentUser.',
 'HTTP authenticated identity may diverge from UI/current actor; subsequent requests can fail or run under unintended principal depending server actor enforcement. Parent security reviewer owns final exploitability/severity validation; no real login performed.',
 ['Same original user successful proof leaves same principal, mainly rotates token.','Server bound actor/session checks can reject mismatched operations and prevent privilege gain.','On wrong-user successful proof, header already replaced even if caller declines proof then retries original change.'],
 ['Fake login proof changes token identity tootheruser, assert root session and bearer remain original after failed proof.','Recovered self-PIN request owned by another actor must be visibly routed/blocked; no session switch as side effect.'],
 'Use isolated non-session-mutating proof client/endpoint and explicit owner context; coordinate with security report to avoid duplicate final finding.',status='SECURITY_COORDINATOR_CHALLENGE_REQUIRED')
p.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');print('UI findings/candidates:',len(data['findings']))
