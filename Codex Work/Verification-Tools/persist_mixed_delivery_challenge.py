"""Coordinator's bounded independent source challenge; no application execution."""
from knowledge_store import *

key='MIXED_PENDING_DELIVERY_CUSTODY_COUNTS_ALREADY_COLLECTED_LEG'
candidate=next(c for c in json.loads((WORK/'Sub-Agents/Results/financial-new-candidates.json').read_text(encoding='utf8'))['candidates'] if c['candidate_key']==key)
ranges={
 'src/Falah.Domain/Orders/CheckoutRules.cs':[(44,78),(88,119)],
 'src/Falah.Domain/Financial/FinancialRules.cs':[(14,25)],
 'src/Falah.Domain/Orders/Order.cs':[(100,109)],
 'src/Falah.Domain/Orders/ElectronicPayment.cs':[(6,10)],
 'src/Falah.Domain/Financial/TreasuryMovement.cs':[(32,62)],
 'src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs':[(80,90),(144,176),(223,229),(253,275)],
 'src/Falah.Infrastructure/Delivery/DeliveryFactsComposer.cs':[(14,29)],
 'src/Falah.Domain/Delivery/DeliveryRules.cs':[(9,21),(32,37),(83,89)],
 'src/Falah.Domain/Delivery/DeliveryStatus.cs':[(13,31)],
 'src/Falah.Infrastructure/Delivery/MarkDeliveryReadyHandler.cs':[(26,62)],
 'src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs':[(36,137)],
 'src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs':[(45,144)],
 'src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs':[(35,187)],
 'src/Falah.Server/Program.cs':[(251,251),(1224,1250),(1286,1314)],
 'src/Falah.Server/AuthenticationMiddleware.cs':[(29,84)]}
sources=[]
for path,spans in ranges.items():
    s=load_symbols(path)
    assert sha256((ROOT/path).read_bytes())==s['worktree_sha256'],path
    lines=(ROOT/path).read_text(encoding='utf8').splitlines()
    assert all(0<a<=b<=len(lines) for a,b in spans),path
    symbols=[{'stable_id':m['stable_id'],'signature':m.get('qualifiedSymbol') or m['name'],
              'span':[m['startLine'],m['startColumn'],m['endLine'],m['endColumn']]}
             for m in s['symbols'] if m['isCallable'] and any(m['startLine']<=b and m['endLine']>=a for a,b in spans)]
    sources.append({'path':path,'source_git_blob_sha256':s['git_blob_sha256'],
                    'worktree_sha256':s['worktree_sha256'],'line_ranges':spans,'relevant_symbols':symbols,
                    'coverage_qualification':'Context references, not new whole-file/function coverage receipts.'})
challenge={
 'id':'FIN-019','candidate_key':key,'baseline':BASELINE,'at_utc':now(),
 'reviewer':'/root coordinator; independent from original /root/financial_integrity discovery',
 'classification':'SURVIVES_INDEPENDENT_STATIC_CHALLENGE_WITH_COLLECTION_POLICY_QUALIFICATIONS',
 'severity':'HIGH','confidence':'HIGH_SOURCE_TRACE_NOT_DATABASE_OR_HTTP_REPRODUCED',
 'title':'Pending mixed delivery includes already collected payment leg in driver custody',
 'sources':sources,'original_candidate':candidate,
 'verified_chain':[
  'Valid positive unique Cash5000/InstaPay5000 splits satisfy exact allocation for10000 total; tender10000 satisfies BuildPlan. Multi electronic leg is Pending, IsPaidfalse, DefersAllMoneyfalse. Order.Create stores those plan flags. Checkout persists delivery New and actual immediate Cash5000 movement to Drawer, skips electronic leg.',
  'MarkReady loads delivery only and validates delivery status plus authorized actor/approval, not payment. Assign loads order, composes facts, creates custody; transition map permits New->Ready->Assigned->OutForDelivery. Middleware requires valid session and checks actor identity but does not impose order payment policy; delivery endpoints delegate these handlers with route ID replacement only.',
  'Composer uses IsPaidfalse plus full Total-refund10000, ignores collected split5000. DeliveryCashResponsibility for nonCash Multi/false returns10000. Assignment persists Expected10000. Thus wrong responsibility is source-supported even before any result/settlement.',
  'RecordResult derives same10000 and defaults omitted collection to10000. Explicit5000 requires variance reason; custody difference is5000-10000. No treasury movement or order paid/electronic transition occurs in this handler.',
  'Settlement locks full delivery and custody rowsets, requires resolved deliveries, calculates expectation from Collected, and posts actual positive remit to Main. Defaulted10000/actual5000 causes shortage approval; actually collecting/remitting10000 could add it to earlier5000, but this is conditional on real human money collection and is NOT a runtime-confirmed duplicate payment.'
 ],
 'preconditions':['Legitimate authorized pos and delivery_manage actors (or valid required approvals), open shift, valid server-priced delivery/customer/address/service, active driver, new idempotency keys; no actor impersonation needed.',
                  'Pending mixed order is dispatched through existing authenticated API; ordinary shipped UI may constrain this path. No claim every user can trigger it through normal UI.'],
 'counterevidence':[
  'All-immediate Multi has IsPaidtrue and zero driver custody; pure Cash COD has no checkout collection. These controls do not exhibit the duplicated immediate leg.',
  'ElectronicPayment XML comment explicitly describes freeze until confirmation, but comments are not enforcement. Source route and handlers inspected do not implement that payment guard.',
  'Do NOT treat residual5000 as necessarily cash owed to driver: it remains an electronic claim under current contract. Block dispatch until resolved, or explicitly design authorized payment conversion; silently changing pending electronic to COD would introduce a new policy.',
  'Strong existing delivery/custody locks and idempotency retained; this finding concerns wrong monetary basis, not missing same-driver settlement locking.',
  'No API call, SQL acceptance, customer collection, business data access, print job or runtime state change executed. Static chain only.'
 ],
 'deduplication':{'related':['FIN-001','FIN-010'],
   'decision':'Distinct financial consequence/entry points: custody Expected and result collection basis; FIN001 confirmation-key conflict and FIN010 reporting omission are related whole-order-state problems, not duplicate custody findings. Cross-reference, do not erase predecessors.'},
 'recommended_solution':'Enforce explicit pending-payment operational freeze at server dispatch boundary and derive any supported collectible balance from per-leg posted/confirmed/refunded facts. Define cash-conversion policy explicitly, consistently across assign/result/settlement, without weakening authorization or locking.',
 'required_tests':candidate['regression_tests']+['Authenticated isolated integration with legitimate actors verifies pending-mixed dispatch rejects atomically OR uses explicitly approved conversion policy; assert original immediate payment retained once, no new custody/job on rejection. No current execution claimed.'],
 'verification':{'source_confirmed':True,'independent_static':True,'isolated_reproduction':'NOT_EXECUTED','live_end_to_end':'NOT_EXECUTED'},
 'next_action':'Financial owner reconcile workflow/report and missing tests; coordinator alone merge canonical registry. Do not launch printing reviewer.'}
relative='Deep-Audit/Database-and-Finance/Independent-Challenges/mixed-pending-delivery-custody.json'
atomic_json(WORK/relative,challenge)
target=WORK/'Deep-Audit/Database-and-Finance/parent-financial-findings.json'
parent=json.loads(target.read_text(encoding='utf8'))
assert not any(f['id']=='FIN-019' for f in parent['findings']),'Already persisted; inspect before regenerating.'
atomic_json(WORK/'Knowledge/History/parent-financial-findings'/('before-FIN019.'+sha256(target.read_bytes())[:20]+'.json'),parent)
parent['findings'].append({'id':'FIN-019','title':challenge['title'],'severity':'HIGH',
 'confidence':challenge['confidence'],'status':'INDEPENDENTLY_CORROBORATED_STATIC_FINDING','baseline':BASELINE,
 'originator':'Original /root/financial_integrity; independently challenged by /root',
 'sources':sources,'evidence_chain':challenge['verified_chain'],'preconditions':challenge['preconditions'],
 'counterevidence':challenge['counterevidence'],'impact':candidate['impact'],
 'root_cause':candidate['root_cause'],'deduplication':challenge['deduplication'],
 'recommended_solution':challenge['recommended_solution'],'required_tests':challenge['required_tests'],
 'independent_challenge':relative,'source_evidence':candidate,'verification':challenge['verification']})
parent['at_utc']=now();atomic_json(target,parent)
checkpoint=WORK/'Sub-Agents/Checkpoints/root-mirrored-code.json'
cp=json.loads(checkpoint.read_text(encoding='utf8'));cp.update({'at_utc':now(),
 'active_task':'Independent financial finding challenge and final mirrored report/source-link validation; identified mirror queue complete',
 'last_successfully_completed_task':'FIN019 independent source chain challenge saved with authenticated-entry preconditions, policy and no-runtime qualifications',
 'current_file_function':None,'next_unfinished_operation':'Merge FIN019 canonical finding with independent evidence; refresh current report link validation',
 'new_evidence':relative,'new_financial_function_coverage_claimed':0})
atomic_json(checkpoint,cp)
nextpath=WORK/'Conversation-Recovery/NEXT_ACTION.md'
t=nextpath.read_text(encoding='utf8')
t+='\n## Latest coordinator verified batch — '+now()+'\n\nFIN-019 independently source-corroborated (pending mixed delivery overstates driver custody by already collected leg). Evidence `'+relative+'`; original specialist discovery retained verbatim. No runtime/DB/HTTP reproduction. Parent registry saved; next operation canonical merge and report link validation. Financial primary owner unchanged; live remaining queue remains `Sub-Agents/Results/financial-current-union-progress.json`. Mirrored queue application0/test0; entire paused backlog preserved above. No commit/push.\n'
atomic_text(nextpath,t)
print(json.dumps({'saved':relative,'sources':len(sources),'new_finding':'FIN-019','runtime_executed':False,'new_callable_count':0}))
