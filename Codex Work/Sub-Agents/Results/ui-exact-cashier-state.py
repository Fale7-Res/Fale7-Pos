from ui_persistent import *
path=P+'ViewModels/CashierViewModel.State.cs';source=load_symbols(path)
facts={
9:'Checkout processing change recomputes global edit/payment guards through RefreshOperationState. Clearing busy does not alone permit editing while _pendingCheckout remains, preserving same-process uncertain checkout lock.',
10:'Opening-shift busy change updates edit availability and header lock state. An unresolved _pendingOpenShift still blocks after busy clears; this callback does not persist intent or prove backend shift state.',
11:'Closing-shift busy change updates operation guard and header. Remaining _pendingCloseShift prevents abandonment, but recovery after process restart depends on separate persistence source paths.',
12:'Expense submission flag updates edit/payment guards and header. Pending expense DTO remains an independent blocking condition, avoiding false unlock merely because network await ended.',
13:'Safe-drop submission flag fans out operation guard updates. Callback neither clears request nor changes drawer balance, so source callers must update DTO/outcome before relying on notifications.',
14:'Cash-movement submission flag recalculates edit gates. Actual permission/actor/amount and request retention are elsewhere; this callback provides no extra authorization itself.',
15:'Cash-drawer submission flag updates guard/header but does not send a printer command here. Physical drawer outcome and unknown retained intent require separate handler review.',
16:'Reprint flag updates edit guards/header; a retained _pendingReprint keeps edit false after request ends. This does not establish tickets printed, only local operation-state gating.',
17:'Electronic-confirm busy callback refreshes CanConfirmPendingPayment and edit guards. Retained confirm can remain while transient LastCheckoutResult is absent, so notification does not repair UI-005 recovery reachability.',
18:'Electronic-cancel busy callback refreshes CanCancelPendingPayment and edit gates. A recovered request still requires a reachable command, and callback alone cannot supply missing checkout result identity UI-005.',
31:'Notifies wallet/COD/tender computed properties when payment presentation changes. IsCashOnDelivery depends on total/order type too, so callers NotifyTotals and other partial notifications must keep dependencies complete.',
36:'Changing primary payment method refreshes wallet/COD/tender labels and enabled state. It does not itself reset amount paid, fees or payment DTO; totals and checkout logic own those transitions.',
37:'Changing split method1 raises only UsesWallet; CanEnterTender depends on split-enabled rather than individual method. Sum validation and wallet account selection occur in other partials.',
38:'Changing split method2 raises only UsesWallet, allowing wallet fields to appear for either leg. It does not validate duplicate split methods or amount sum itself.',
39:'Split-enabled changes refresh wallet/COD/tender presentation. Stored SplitMethod values remain retained locally; enabling split disables ordinary cash tender via CanEnterTender expression.',
67:'Notifies edit/tender/electronic action availability then checkout commands and header. It omits direct IsOperationBusy property notification despite expression changing; any view binding to that aggregate can stay stale unless another source emits it. No native stale state asserted.',
74:'Notifies totals, change and tax displays, refreshes payment/checkout availability, then autofills tender only for open checkout and noncash/COD when editable. Tax display uses discounted subtotal without CompAmount here; parity with authoritative checkout must be independently checked. Programmatic auto-tender avoids changing pending request.',
86:'Serializes initialization using IsInitializing. Fetches current shift, accepts404 as none, updates session/header, optionally settings identity/default opening cash, then loads menu only when shift open. No current-user/screen generation guard after awaits; backend/API errors outside result wrappers propagate through finally. Root fire-and-forget caller may not observe them.',
113:'Opens clear-cart confirmation only when operation editable and cart nonempty. It does not clear immediately, protecting pending requests and accidental action; no cancellation of in-flight reads occurs.',
114:'Dismisses clear-cart confirmation unconditionally. It does not mutate cart or unresolved financial state, so dismissing while a new pending command exists cannot itself discard that command.',
115:'Rechecks CanEditOperation before clearing cart, so pending state arising after confirmation opens blocks destructive local clear. On blocked attempt dialog remains open; success calls shared ClearCart and hides dialog.'
}
reviews=[]
for line,reason in facts.items():
 s,=[s for s in source['symbols'] if s.get('isCallable') and (s['startLine'],s['startColumn'])==(line,5)]
 links=('Observable callback -> RefreshOperationState' if line<=18 else {
 31:'OnPaymentMethodChanged; OnIsSplitPaymentEnabledChanged; NotifyTotals -> NotifyPaymentPresentation -> OnPropertyChanged',
 36:'OnPaymentMethodChanged -> NotifyPaymentPresentation',
 37:'OnSplitMethod1Changed -> OnPropertyChanged(UsesWallet)',
 38:'OnSplitMethod2Changed -> OnPropertyChanged(UsesWallet)',
 39:'OnIsSplitPaymentEnabledChanged -> NotifyPaymentPresentation',
 67:'RefreshOperationState -> OnPropertyChanged; NotifyCheckoutAvailability; Header.Refresh',
 74:'NotifyTotals -> OnPropertyChanged; NotifyPaymentPresentation; NotifyCheckoutAvailability; CashInput.Format',
 86:'MainViewModel.CreateCashier -> InitializeAsync -> Api.GetCurrentShiftAsync/GetSettingsAsync; Header.Refresh/SetIdentity; LoadMenuAsync; CashInput.Format',
 113:'RequestClearCartCommand -> CanEditOperation/Cart.Count -> IsClearCartOpen setter',
 114:'CancelClearCartCommand -> IsClearCartOpen setter',
 115:'ConfirmClearCartCommand -> CanEditOperation -> ClearCart -> IsClearCartOpen setter'
 }[line])
 reviews.append(dict(stable_id=s['stable_id'],status='ANALYZED',exact_signature=s['qualifiedSymbol'],exact_span=[line,5,s['endLine'],s['endColumn']],reasoning=reason,inputs='Changed observable property or command; session/API results for InitializeAsync',outputs='Property/command/header events; initialization cache changes or clear-cart dialog state as exact body describes',side_effects='Source-only inspection, no API/financial/cart mutation executed.',trust_boundaries='UI affordance/pending memory versus server-authorized immutable financial request; root shared session can outlive screen.',database='No SQL direct; initialization calls read endpoints only in this file.',printing_ui='Drawer/reprint flags are guards, not proof of physical action; native UI NOT_OBSERVED.',performance='Initialization busy guard avoids duplicate same-VM overlap; no cancellation and sequential reads.',duplicates_conflicts='UI-001/005 related recovery hypotheses preserved; no legacy function-heading promotion.',issues_hypotheses='Check dependency notifications and stale async ownership described in reasoning; parent challenge required before canonical promotion.',tests_missing='Exact callback notifications, unknown-outcome lock, delayed initialization/session changes, and clear confirmation while pending. NOT_EXECUTED.',verified_callers_callees=links))
assert len(reviews)==sum(s.get('isCallable',False) for s in source['symbols'])
shard=save_review('ui_visual',path,dict(file_review_status='ANALYZED_STATIC_EXACT_RECONCILIATION',file_reasoning='Complete source read with all21 exact callable bodies and dependent expression properties. Native financial workflows not run; existing report preserved.',legacy_report_preserved='Codex Work/Deep-Audit/File-Reports/'+path+'.md',symbols=reviews,native_runtime='NOT_EXECUTED'))
cp=ROOT/'Codex Work/Sub-Agents/Checkpoints/ui_visual.json';c=json.loads(cp.read_text(encoding='utf8'));c.update(updated_at_utc=now(),last_completed_file=path,last_completed_stable_id=reviews[-1]['stable_id'],current_partial_file=None,current_partial_symbol=None,latest_review_shard=str(shard.relative_to(ROOT)).replace('\\','/'));atomic_json(cp,c)
print('21 exact callable receipts')
