"""Independent source challenge of first-movement/unit-change admission, no database execution."""
from knowledge_store import *
key='INVENTORY_UNIT_CHANGE_STALE_NO_MOVEMENT_ADMISSION'
c=next(c for c in json.loads((WORK/'Sub-Agents/Results/financial-new-candidates.json').read_text(encoding='utf8'))['candidates'] if c['candidate_key']==key)
refs=[]
for path,ranges in {
 'src/Falah.Infrastructure/Inventory/InventoryItemHandlers.cs':[(74,100)],
 'src/Falah.Domain/Inventory/InventoryRules.cs':[(40,47)],
 'src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs':[(60,100),(165,178),(206,209)],
 'src/Falah.Infrastructure/Persistence/FalahDbContext.cs':[(245,255)],
 'src/Falah.Infrastructure/Persistence/Entities/InventoryEntities.cs':[(24,76)],
 'src/Falah.Infrastructure/Inventory/InventoryQueryService.cs':[(20,34)],
 'src/Falah.Application/Inventory/InventoryItemRequests.cs':[(14,26)],
 'src/Falah.Server/Program.cs':[(909,916)]}.items():
 s=load_symbols(path);assert sha256((ROOT/path).read_bytes())==s['worktree_sha256'],path
 assert all(0<a<=b<=len((ROOT/path).read_text(encoding='utf8').splitlines()) for a,b in ranges)
 refs.append({'path':path,'source_git_blob_sha256':s['git_blob_sha256'],'worktree_sha256':s['worktree_sha256'],
  'line_ranges':ranges,'relevant_symbols':[{'stable_id':m['stable_id'],'signature':m.get('qualifiedSymbol') or m['name'],
    'span':[m['startLine'],m['startColumn'],m['endLine'],m['endColumn']]} for m in s['symbols']
      if m['isCallable'] and any(m['startLine']<=b and m['endLine']>=a for a,b in ranges)]})
relative='Deep-Audit/Database-and-Finance/Independent-Challenges/inventory-unit-first-movement.json'
challenge={'id':'FIN-020','at_utc':now(),'baseline':BASELINE,'reviewer':'/root independent coordinator',
 'originator':'Original /root/financial_integrity','classification':'SOURCE_CORROBORATED_MEDIUM_CONCURRENCY_NOT_RUNTIME',
 'sources':refs,'original_candidate':c,
 'verified_schedule':['Authorized metadata request reads existing UnitA, no movements, passes explicit Domain unit-lock guard, pauses before Save.',
 'Separate authorized legitimate positive credit receipt (paid0) commits first movement, supplier debt and invoice. Receipt transaction is fully finished before metadata Save resumes.',
 'Metadata update writes UnitB without requery/stock admission guard. Item mapping245 has primary key only, entity has no version field; stock FK restricts deletion/existence, not immutable Unit. DTO query joins current item Unit with bare signed movement quantity.'],
 'counterevidence':['Sequential update after preexisting movement correctly rejects; only first-movement admission race described.',
 'Receipt FK/source-account locks do not negate selected schedule because receipt has completed before update Save; credit0 avoids unrelated protected-account risk.',
 'Changing unrelated metadata or keeping same Unit remains legitimate. Valid different unit strings assumed; arbitrary malformed units not needed.',
 'No DB SQL/query plan/triggers/isolation observed; any external deployment serialization/trigger could change runtime outcome. Source defect with unexecuted race, no quantified monetary loss.',
 'Adding metadata optimistic token alone cannot detect inserted movements if posting does not modify item token; both writer families must participate in same invariant.'],
 'root_cause':c['root_cause'],'impact':c['impact'],'recommended_solution':c['correction_design'],
 'required_tests':c['regression_tests'],
 'verification':{'independent_static':True,'isolated_reproduction':'NOT_EXECUTED','live_end_to_end':'NOT_EXECUTED'},
 'deduplication':'Distinct immutable-unit invariant; related stale-admission family but not duplicate negative purchase FIN018 or prep-cycle candidate.',
 'new_callable_count':0}
atomic_json(WORK/relative,challenge)
p=WORK/'Deep-Audit/Database-and-Finance/parent-financial-findings.json';parent=json.loads(p.read_text(encoding='utf8'))
assert not any(f['id']=='FIN-020' for f in parent['findings'])
atomic_json(WORK/'Knowledge/History/parent-financial-findings'/('before-FIN020.'+sha256(p.read_bytes())[:16]+'.json'),parent)
parent['findings'].append({'id':'FIN-020','baseline':BASELINE,'title':'First stock receipt can race unit-change admission and reinterpret movement history',
 'severity':'MEDIUM','confidence':'HIGH_STATIC_SCHEDULE_NO_RUNTIME','status':'INDEPENDENTLY_CORROBORATED_STATIC_FINDING',
 'originator':'Original /root/financial_integrity; challenged by /root','sources':refs,
 'source_evidence':c,'evidence_chain':challenge['verified_schedule'],'root_cause':challenge['root_cause'],
 'impact':challenge['impact'],'counterevidence':challenge['counterevidence'],'recommended_solution':challenge['recommended_solution'],
 'required_tests':challenge['required_tests'],'independent_challenge':relative,'verification':challenge['verification'],
 'deduplication':challenge['deduplication']})
parent['at_utc']=now();atomic_json(p,parent)
nextpath=WORK/'Conversation-Recovery/NEXT_ACTION.md';text=nextpath.read_text(encoding='utf8')
atomic_text(nextpath,text+'\n## Latest independent financial batch — '+now()+'\n\nFIN020 source-corroborated first-receipt/unit-change stale admission; exact8source hashes/IDs/spans `'+relative+'`. Original candidate retained. MEDIUM, SQL schedule NOT executed; no money-loss measurement. Root next merge canonical, reconcile semantic unit/cost comparison and financial coverage quality; specialist continues remaining live queue. No runtime/source/Git changes.\n')
print(json.dumps({'saved':relative,'new_canonical_id':'FIN-020','source_paths':len(refs),'runtime_executed':False}))
