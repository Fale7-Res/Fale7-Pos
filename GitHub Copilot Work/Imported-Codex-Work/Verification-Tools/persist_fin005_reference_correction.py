"""Evidence quality correction, preserving the original financial finding verbatim."""
from knowledge_store import *
findings=json.loads((WORK/'Sub-Agents/Results/financial-findings.json').read_text(encoding='utf8'))
items=findings if isinstance(findings,list) else findings['findings']
old=next(f for f in items if f['id']=='FIN-005')
refs=[]
for path,spans in {
 'src/Falah.Infrastructure/Persistence/FalahDbContext.cs':[(277,305)],
 'src/Falah.Infrastructure/Persistence/Entities/PurchasingEntities.cs':[(20,60),(75,89),(93,111)],
 'src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs':[(36,44),(59,81),(95,115)]}.items():
 s=load_symbols(path);assert sha256((ROOT/path).read_bytes())==s['worktree_sha256']
 lines=(ROOT/path).read_text(encoding='utf8').splitlines()
 refs.append({'path':path,'worktree_sha256':s['worktree_sha256'],'source_git_blob_sha256':s['git_blob_sha256'],
  'line_ranges':spans,'excerpts':[{'start':a,'end':b,'source_excerpt':'\n'.join(lines[a-1:b])} for a,b in spans],
  'symbol_contexts':[{'stable_id':m['stable_id'],'signature':m.get('qualifiedSymbol') or m['name'],
                    'span':[m['startLine'],m['startColumn'],m['endLine'],m['endColumn']]}
    for m in s['symbols'] if m['isCallable'] and any(m['startLine']<=b and m['endLine']>=a for a,b in spans)]})
artifact={'at_utc':now(),'id':'FIN-005','baseline':BASELINE,'reviewer':'/root coordinator',
 'disposition':'FINDING_SURVIVES_BUT_LEGACY_MODEL_CITATION_IRRELEVANT_AND_REQUIRES_CORRECTION',
 'original_finding_preserved':old,'source_references':refs,
 'citation_error':'Original finding FalahDbContext.cs309-328 quotes real source but covers Customer/Delivery mappings, not Purchase. It cannot support absence of Purchase concurrency guard. Correct Purchase/Payment/SupplierLedger mappings277-305 and actual entity20-60 inspected.',
 'reasoning':'Purchase configuration has ID and unique receive idempotency, relationships and payment/ledger individual-key uniqueness. PurchaseEntity has ordinary Paid/Remaining fields and no Version/concurrency annotation. Model search IsConcurrencyToken/RowVersion is present on Order/Shift/Approval/Delivery, not Purchase. PayPurchase reads tracked invoice and supplier outstanding before transaction; later account locking refreshes account movements, not invoice/outstanding. Distinct valid request IDs and sufficient source cash permit two stale invoice admissions; independent repeated-request identity protection does not bind aggregate paid<=invoice invariant.',
 'counterevidence':'Existing account lock protects protected account from aggregate overdraft. Purchase paid cache is not source of supplier balance. No claim ordinary same-request replay duplicates; both commands require legitimate purchasing authority. SQL provider lock/deadlock execution was not performed.',
 'runtime':'NOT_EXECUTED; no DB/API/financial records or application code changed',
 'recommended_action':'Financial owner correct active citation while preserving historical finding and this correction. Do not silently edit original source or claim a new defect count.',
 'new_function_coverage_claimed':0}
target=WORK/'Deep-Audit/Database-and-Finance/Independent-Challenges/FIN005-model-reference-correction.json'
atomic_json(target,artifact)
print(json.dumps({'saved':target.relative_to(WORK).as_posix(),'finding_count_delta':0}))
