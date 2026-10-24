"""Manual semantic payment-contract comparison outside exact-body clone inventory."""
from knowledge_store import *
scope={
 'src/Falah.Domain/Orders/OrderPaymentQueries.cs':[(26,55)],
 'src/Falah.Infrastructure/Orders/OrderEnumMapping.cs':[(58,65)],
 'src/Falah.Infrastructure/Orders/OrderMapper.cs':[(118,122)],
 'src/Falah.Infrastructure/Delivery/DeliveryFactsComposer.cs':[(14,29)],
 'src/Falah.Infrastructure/Delivery/DeliveryQueryService.cs':[(40,47),(58,92)],
 'src/Falah.Domain/Delivery/DeliveryRules.cs':[(9,21),(83,89)],
 'src/Falah.Application/Delivery/DeliveryRequests.cs':[(71,76)],
 'src/Falah.Client.CashierWinUI/Services/ApiClient.cs':[(58,65),(631,632)],
 'src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs':[(66,77),(134,134),(174,198),(208,215),(221,242),(297,307),(319,356)],
 'src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml':[(119,140)],
 'src/Falah.Client.CashierWinUI/Views/DeliveryModalsView.xaml':[(76,79),(103,107)],
 'src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs':[(94,110)]}
baseline={f['path']:f for f in json.loads((WORK/'Deep-Audit/Source-Inventory/verified-baseline-hashes.json').read_text(encoding='utf8'))['files']}
refs=[]
for path,spans in scope.items():
 raw=(ROOT/path).read_bytes();assert sha256(raw)==baseline[path]['worktree_sha256'],path
 lines=raw.decode('utf-8-sig').splitlines();assert all(0<a<=b<=len(lines) for a,b in spans),path
 symbols=[]
 if path.endswith('.cs'):
  s=load_symbols(path)
  symbols=[{'stable_id':m['stable_id'],'signature':m.get('qualifiedSymbol') or m['name'],
           'span':[m['startLine'],m['startColumn'],m['endLine'],m['endColumn']]}
    for m in s['symbols'] if m['isCallable'] and any(m['startLine']<=b and m['endLine']>=a for a,b in spans)]
 refs.append({'path':path,'worktree_sha256':sha256(raw),'source_git_blob_sha256':baseline[path]['git_blob_sha256'],
              'line_ranges':spans,'relevant_symbols':symbols,
              'excerpts':[{'start':a,'end':b,'text':'\n'.join(lines[a-1:b])} for a,b in spans]})
record={'id':'SEM-MIRROR-004','at_utc':now(),'baseline':BASELINE,'reviewer':'/root coordinator',
 'status':'MANUAL_SOURCE_CONTRACT_COMPARISON_COMPLETED_FINANCIAL_FOLLOWUP_PENDING',
 'title':'Delivery payment truth diverges between per-leg totals, DTO amount, wire state and client outcome default',
 'scope':'Financial amount/state contract only; not restarting UI visual or printing audit. New semantic group outside218exact-body denominator.',
 'sources':refs,
 'comparisons':[
  {'key':'PER_LEG_VS_WHOLE_ORDER',
   'behavior':'OrderPaymentQueries.PaymentMethodTotals counts non-electronic split legs immediately even IsPaidfalse; Composer passes entire Total-refund and IsPaidfalse to DeliveryRules. FIN019 independent challenge already established overstated custody for pending mixed delivery.',
   'disposition':'RELATED_EXISTING_FIN019_NOT_NEW_FINDING',
   'counterevidence':'Reports may intentionally use different row scopes, but same specific pending-mixed order has actual immediate leg already posted; this is not merely period scope.'},
  {'key':'WIRE_STATUS_CASE',
   'behavior':'OrderEnumMapping writes Pending as lowercase pending. OrderMapper stores it, DeliveryQueryService returns unchanged string, ApiClient.ReadResultAsync deserializes DTO without case conversion. DeliveryViewModel checks uppercase Pending in PaymentStateLabel and IsPaymentUnconfirmed. For canonical lowercase pending/IsPaidfalse row, source resolves label to collect-on-delivery and warningfalse.',
   'disposition':'SOURCE_SUPPORTED_FINANCIAL_PRESENTATION_CANDIDATE_PENDING_INDEPENDENT_CHALLENGE',
   'related':'FIN019; exact existing IDs searched, no new canonical ID allocated here',
   'entry_points':'GET dispatch board -> BoardRow properties -> DeliveryView bindings119-124; MarkReady/OpenAssign commands use permission/busy only and XAML visibility uses delivery status, no pending check.',
   'counterevidence':'Historical noncanonical uppercase row would display warning; this does not correct ordinary mapper-produced lowercase values. No native UI or authenticated HTTP exercised.'},
  {'key':'GROSS_AMOUNT_VS_ACTUAL_COLLECTION',
   'behavior':'DeliveryDto.AmountToCollectPiastres is Composer Total-refund even when PaymentAlreadySettledtrue. Client OpenOutcome always pre-fills that amount. Server result separately derives zero responsibility for prepaid nonCash; unchanged positive form with blank reason is rejected by AssertCollectionAmount. This is an actionable disagreement between client default and server invariant; zero can be entered manually.',
   'disposition':'SOURCE_SUPPORTED_FINANCIAL_CLIENT_DEFAULT_CANDIDATE_PENDING_INDEPENDENT_CHALLENGE',
   'example':'Fully paid Card delivery total10000, no refund, IsPaidtrue: DTO10000, dialog defaults10000, server expected0; default confirmation requires variance reason. With a user-supplied reason, server can record positive collected on a prepaid delivery, but no real duplicate collection or settlement was executed.',
   'counterevidence':'Server validation blocks silent no-reason overcollection. Client field is editable. Normal COD expects full amount and is unaffected. Gross order value is a useful separate field and need not be deleted.'}],
 'recommendations':['Use typed/centralized wire state contract or explicit canonical parse; do not fix only label while leaving authority rules.',
  'Separate gross order value, confirmed paid amount and actual collectibility in DTO/domain contract; have client initialize from authoritative responsibility, with explicit pending-payment freeze or authorized conversion policy.',
  'Preserve server-side reason/approval/idempotency/locking and distinct refund allocation semantics. Audit proposes only; no fix implemented.'],
 'required_tests':['Pure canonical pending/confirmed/cancelled/null BoardRow state mappings, including exact serialized lowercase strings.',
  'Prepaid card/confirmed electronic, all-immediate Multi, CashCOD and partially pending Multi: API projection, client default and handler responsibility agree; zero prepaid outcome succeeds without invented variance.',
  'Authenticated isolated pending dispatch policy tests with legitimate actors and no physical printing, only after owner authorization for stateful environment.'],
 'runtime':'NOT_EXECUTED; source/manual semantic comparison only','new_financial_callable_receipts':0,
 'independent_review':'Pending financial specialist; no claim root independently corroborated its own newly discovered candidates.'}
relative='Deep-Audit/Duplication-and-Architecture/delivery-payment-contract-semantic-comparison.json'
atomic_json(WORK/relative,record)
p=WORK/'Deep-Audit/CODE_DUPLICATION_AND_ARCHITECTURE.md';old=p.read_text(encoding='utf8')
section='''
## Additional semantic contract comparison — delivery payment state and amount

[SEM-MIRROR-004 source comparison](Duplication-and-Architecture/delivery-payment-contract-semantic-comparison.json) traces per-leg money, delivery DTO, wire strings, client default and server result validation. It is outside the218exact-body groups, not another whole-file audit or additional financial callable count.

- Per-leg immediate collection versus whole-order custody is already FIN019; no duplicate finding ID.
- Canonical server electronic status is lowercase `pending`; the dispatch client tests uppercase `Pending`. Source predicts the pending warning is missed and the label falls back to collection on delivery. No native observation claimed.
- A prepaid non-Cash delivery DTO still carries gross net-of-refund amount; client outcome defaults to that amount while server expects zero. Unchanged positive input with no reason is rejected; manually entering zero is a workaround. A reason permits positive collection, but actual double collection was not tested.

The two new source-supported financial presentation/default candidates await the Financial Integrity specialist's independent challenge and deduplication. They are not promoted to confirmed runtime defects. Preserve the gross amount as a separately named fact, unify the payment-state wire contract, and derive actual collection/defaults from authoritative per-leg responsibility. No UI visual or printing specialist was resumed.
'''
assert '## Additional semantic contract comparison — delivery payment state and amount' not in old
atomic_json(WORK/'Knowledge/History/Mirror-Reports'/('CODE_DUPLICATION.'+sha256(old.encode())[:20]+'.json'),{'original_document':'Deep-Audit/CODE_DUPLICATION_AND_ARCHITECTURE.md','original_text':old})
atomic_text(p,old+section)
statuspath=WORK/'Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.json';status=json.loads(statuspath.read_text(encoding='utf8'))
status['latest_semantic_comparison']={'id':'SEM-MIRROR-004','path':relative,'status':record['status'],'new_financial_candidates':2,'independent_challenge':'PENDING'}
status['updated_at_utc']=now();atomic_json(statuspath,status)
nextpath=WORK/'Conversation-Recovery/NEXT_ACTION.md';t=nextpath.read_text(encoding='utf8')
atomic_text(nextpath,t+'\n## Latest semantic batch — '+now()+'\n\nSEM-MIRROR-004 saved `'+relative+'`: canonical lowercase pending versus client uppercase Pending, plus prepaid gross DTO amount versus zero actual server responsibility/default. Two new bounded financial contract candidates, independent specialist challenge pending; related FIN019 not duplicated. No runtime/new callable counts. Next root reconcile independent challenge, current mirror links and financial evidence quality. All paused backlog/ownership unchanged.\n')
print(json.dumps({'saved':relative,'source_paths':len(refs),'semantic_group':'SEM-MIRROR-004','new_canonical_findings':0,'runtime':False}))
