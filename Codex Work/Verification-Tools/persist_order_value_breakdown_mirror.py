"""Persist a bounded call-site and semantic comparison for OrderValueBreakdown."""
from knowledge_store import *

scope={
 'src/Falah.Domain/Orders/OrderValueBreakdown.cs':[(1,22)],
 'src/Falah.Domain/Orders/CheckoutRules.cs':[(32,39),(88,109)],
 'src/Falah.Domain/Orders/Order.cs':[(60,112)],
}
baseline={f['path']:f for f in json.loads((WORK/'Deep-Audit/Source-Inventory/verified-baseline-hashes.json').read_text(encoding='utf8'))['files']}
refs=[]
for path,ranges in scope.items():
 raw=(ROOT/path).read_bytes();ix=load_symbols(path)
 assert sha256(raw)==ix['worktree_sha256']==baseline[path]['worktree_sha256'],path
 lines=raw.decode('utf-8-sig').splitlines();assert all(1<=a<=b<=len(lines) for a,b in ranges)
 symbols=[{'stable_id':s['stable_id'],'signature':s.get('qualifiedSymbol') or s['name'],'kind':s['kind'],'span':[s['startLine'],s['startColumn'],s['endLine'],s['endColumn']]}
          for s in ix['symbols'] if s['isCallable'] and any(s['startLine']<=b and s['endLine']>=a for a,b in ranges)]
 refs.append({'path':path,'worktree_sha256':sha256(raw),'source_git_blob_sha256':baseline[path]['git_blob_sha256'],'line_ranges':ranges,'relevant_symbols':symbols,
              'excerpts':[{'start':a,'end':b,'text':'\n'.join(lines[a-1:b])} for a,b in ranges]})

# The repository-wide exact name search is recorded as evidence, not treated as proof of
# absence under reflection, generated code, or external consumers.
matches=[]
for base in (ROOT/'src',ROOT/'tests'):
 for p in base.rglob('*.cs'):
  if p.name=='OrderValueBreakdown.cs': continue
  try:
   for n,line in enumerate(p.read_text(encoding='utf-8-sig').splitlines(),1):
    if 'OrderValueBreakdown' in line: matches.append({'path':p.relative_to(ROOT).as_posix(),'line':n,'text':line.strip()})
  except UnicodeDecodeError: pass
assert not matches, matches

rel='Deep-Audit/Duplication-and-Architecture/order-value-breakdown-vs-checkout-semantic-comparison.json'
artifact={'artifact_kind':'manual_semantic_comparison','id':'SEM-MIRROR-046','baseline':BASELINE,'created_at_utc':now(),'reviewer':'/root coordinator',
 'title':'Unreferenced OrderValueBreakdown helper is not equivalent to authoritative checkout totals',
 'scope':'Bounded source/call-site review of the Domain helper against the active checkout plan and order creation; no general dead-code proof.',
 'decision':'Repository text search found no usages of OrderValueBreakdown outside its declaration file in src or tests. The helper computes list value, offers, charged subtotal, comp and collected from line snapshots only; it does not include CheckoutRules discount, tax or delivery fee. Active CheckoutOrderHandler uses CheckoutRules.BuildPlan, and Order.Create consumes the plan. Thus the helper is currently unreferenced in the searched source/tests and is not an active competing financial path. It would be unsafe to treat it as a drop-in authoritative total if a future caller appears. This is classified as a dormant/historical helper with narrower semantics, not a current financial defect.',
 'search_evidence':{'scope':'All current src/**/*.cs and tests/**/*.cs text lines, exact identifier OrderValueBreakdown, excluding declaration file when reporting external uses.','external_occurrences':matches,'limitations':'Text search does not prove absence via reflection, generated code, binary consumers or dynamic loading; no such path was found in the inspected solution structure.'},
 'behavior_comparison':[
  {'implementation':'OrderValueBreakdown.Compute','inputs':'OrderItem list and comp amount','arithmetic':'ListUnitPrice*Quantity; TotalPrice sum as charged subtotal; offer is nonnegative list-minus-charged; comp is clamped nonnegative; collected is charged-minus-comp clamped at zero.','exclusions':'No manual discount, tax, delivery fee, payment method, split payment, deferred electronic/COD state, or persistence side effects.','disposition':'Unreferenced narrower helper; historical parity utility / maintenance risk if reused as a full checkout amount.'},
  {'implementation':'CheckoutRules.BuildPlan / CheckoutOrderHandler','inputs':'Resolved server catalog prices, discount, tax, delivery fee, comp, payment and split inputs','arithmetic':'Computes subtotal, taxable base, tax, payable, cash-on-delivery/electronic deferral, payment validation and paid state. Order.Create receives the plan; handler persists order and related financial/inventory effects.','disposition':'Active authoritative checkout calculation path in current source.'}],
 'recommendation':'No removal or code change in this audit. In future maintenance, either document/rename the helper as a line-value breakdown and test its narrow contract, or remove it only after confirming no external consumers. Do not wire it as a replacement for CheckoutRules; any reuse needs explicit tax/discount/delivery/payment semantics and parity tests.',
 'required_tests':['If retained, pure tests for list value, offer difference, comp cap and no negative collected.', 'If wired by a future feature, equivalence tests against CheckoutRules across tax modes, discounts, fees, comps, COD/electronic/split payments; no such integration exists in current searched call sites.'],
 'runtime':'NOT_EXECUTED; static source and call-site search only','new_financial_callable_receipts':0,'source_references':refs}
atomic_json(WORK/rel,artifact)
regp=WORK/'Deep-Audit/Duplication-and-Architecture/semantic-comparison-registry.json';reg=json.loads(regp.read_text(encoding='utf8'))
assert not any(x['id']==artifact['id'] for x in reg['semantic_groups'])
reg['semantic_groups'].append({'id':artifact['id'],'decision':'Unreferenced narrow line-value helper differs from active checkout total; no current divergence defect.','artifact':rel})
reg['inputs'].append({'path':rel,'sha256':sha256((WORK/rel).read_bytes()),'source_references':len(refs)})
paths,ids,records,ranges=set(),set(),0,0
for item in reg['inputs']:
 doc=json.loads((WORK/item['path']).read_text(encoding='utf8')); rs=doc.get('source_references',doc.get('sources',[]));records+=len(rs)
 for ref in rs:
  paths.add(ref['path']);ranges+=len(ref.get('line_ranges',[]));ids.update(s['stable_id'] for s in ref.get('relevant_symbols',[]))
reg.update(at_utc=now(),referenced_source_paths=len(paths),referenced_callable_context_ids=len(ids),source_reference_records=records,explicit_ranges=ranges)
atomic_json(regp,reg)

report=WORK/'Deep-Audit/CODE_DUPLICATION_AND_ARCHITECTURE.md';old=report.read_text(encoding='utf8')
section='''\n\n## SEM-MIRROR-046 — dormant order-value helper versus checkout calculation\n\n[Source comparison](Duplication-and-Architecture/order-value-breakdown-vs-checkout-semantic-comparison.json) found no exact-name call sites for `OrderValueBreakdown` outside its declaration in current `src` and `tests`. Its value split covers line list/charged/comp values only; the active `CheckoutRules.BuildPlan` additionally applies discount, tax, delivery fee and payment/deferred-state rules, and `CheckoutOrderHandler` persists the resulting plan. The helper is therefore not an active competing total today. Treating it as a full checkout calculation in future work would omit material amounts/states. This is a bounded unreferenced-helper/maintenance observation, not a current financial defect or proof of binary-level dead code. No code changed and no tests ran.\n'''
assert '## SEM-MIRROR-046 — dormant order-value helper versus checkout calculation' not in old
atomic_text(report,old+section)
statusp=WORK/'Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.json';st=json.loads(statusp.read_text(encoding='utf8'));st['at_utc']=now();st['latest_semantic_comparison']={'id':'SEM-MIRROR-046','path':rel,'status':'SAVED_BOUNDED_CALLSITE_AND_BEHAVIOR_COMPARISON'};atomic_json(statusp,st)
nextp=WORK/'Conversation-Recovery/NEXT_ACTION.md';txt=nextp.read_text(encoding='utf8')
atomic_text(nextp,txt+f'''\n\n## Main mirror batch — SEM-MIRROR-046 ({now()})\n\nSaved `{rel}`. Exact-name search in current `src` and `tests` found no call sites outside the declaration. `OrderValueBreakdown` is a narrower dormant line-value helper and differs from active CheckoutRules on discount/tax/delivery/payment; no current defect promoted. No tests/runtime executed and no new financial callable receipt claimed. Financial specialist remains active on separate handoff; its independent review is pending.\n''')
print(json.dumps({'id':artifact['id'],'source_paths':len(refs),'external_calls':len(matches),'registry_groups':len(reg['semantic_groups']),'refs':records,'paths':len(paths),'symbols':len(ids),'ranges':ranges}))
