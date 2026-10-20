"""Independent coordinator receipt accounting, not semantic depth certification."""
from knowledge_store import *
from collections import Counter
live_path=WORK/'Sub-Agents/Results/financial-current-union-progress.json'
start=json.loads(live_path.read_text(encoding='utf8'))
ledger=json.loads((WORK/'Sub-Agents/Results/financial-ledger.json').read_text(encoding='utf8'))
required=['reasoning','inputs','outputs','side_effects','trust_boundaries','database','printing_ui','performance','duplicates_conflicts','issues_hypotheses','tests_missing','verified_callers_callees']
errors=[];universe={};receipts={};reviewer_ids={};perfile=[];review_artifacts=[]
for record in ledger['records']:
    p=record['path'];index=json.loads((WORK/'Knowledge/Source-Symbols'/(p+'.json')).read_text(encoding='utf8'))
    current=sha256((ROOT/p).read_bytes())
    if current!=index['worktree_sha256']:errors.append({'path':p,'error':'Current source identity mismatch'})
    original={s['stable_id']:s for s in index['symbols'] if s['isCallable']}
    universe.update(original);file_reviewed=set()
    for reviewer in ['financial_integrity','financial_coordinator']:
        rp=WORK/'Knowledge/Reviews'/reviewer/(p+'.json')
        if not rp.exists():continue
        raw=rp.read_bytes();review=json.loads(raw)
        review_artifacts.append({'path':str(rp.relative_to(WORK)).replace('\\','/'),'sha256':sha256(raw)})
        if review['worktree_sha256']!=current:errors.append({'path':p,'reviewer':reviewer,'error':'Stale receipt source hash'})
        for item in review.get('symbols',[]):
            if item.get('status')!='ANALYZED':continue
            sid=item['stable_id'];symbol=original.get(sid)
            if symbol is None:errors.append({'path':p,'id':sid,'error':'Unknown or noncallable analyzed ID'});continue
            expected=[symbol['startLine'],symbol['startColumn'],symbol['endLine'],symbol['endColumn']]
            if item['span']!=expected:errors.append({'path':p,'id':sid,'error':'Receipt span mismatch'})
            def present(f):
                value=item.get(f)
                if isinstance(value,str):return bool(value.strip())
                if f=='verified_callers_callees' and isinstance(value,list):
                    return bool(value) and all(isinstance(v,str) and v.strip() for v in value)
                return False
            empty=[f for f in required if not present(f)]
            if empty:errors.append({'path':p,'id':sid,'error':'Missing reasoning fields','fields':empty})
            reviewer_ids.setdefault(reviewer,set()).add(sid);receipts.setdefault(sid,[]).append(reviewer);file_reviewed.add(sid)
    perfile.append({'path':p,'indexed_original_callables':len(original),'analyzed_author_receipts':len(file_reviewed),'remaining':len(original.keys()-file_reviewed),'limitation':'Counts are structural author receipts, not independently verified semantic depth or runtime coverage'})
finish=json.loads(live_path.read_text(encoding='utf8'));remaining=set(universe)-set(receipts)
concurrent=start['updated_at_utc']!=finish['updated_at_utc']
queue_ids={s['stable_id'] for s in finish['remaining_original_symbols']}
if not concurrent and remaining!=queue_ids:errors.append({'error':'Live remaining queue disagrees with independent receipt recomputation','only_recalculated':len(remaining-queue_ids),'only_live':len(queue_ids-remaining)})
counts={k:len(v) for k,v in reviewer_ids.items()}
result={'at_utc':now(),'baseline':BASELINE,'scope_files':len(perfile),'source_index_original_callables':len(universe),'author_receipt_union':len(receipts),'remaining_original_callables':len(remaining),'reviewer_counts':counts,'overlap_ids':sum(len(v)>1 for v in receipts.values()),'files_with_all_original_callable_receipts':sum(x['remaining']==0 for x in perfile),'files_with_nonzero_original_scope_and_all_receipts':sum(x['remaining']==0 and x['indexed_original_callables']>0 for x in perfile),'files_still_missing_receipts':sum(x['remaining']>0 for x in perfile),'zero_callable_files':sum(x['indexed_original_callables']==0 for x in perfile),'live_snapshot_before':start['updated_at_utc'],'live_snapshot_after':finish['updated_at_utc'],'concurrent_writer_observed':concurrent,'per_file':perfile,'receipt_artifacts':review_artifacts,'errors':errors,'gate':'PASS_SOURCE_SYMBOL_RECEIPT_STRUCTURE_ONLY' if not errors else 'FAIL','limitations':['Nonempty generic reasoning fields do not establish genuine semantic analysis; manual quality checks remain required.','All327 prior reports and structural author receipt counts are not final financial completion.','77getter/35generated-context separate ledgers excluded from8804 denominator; pausedPrinting161 remains inside originaldenominator.','No source/DB/runtime changes or test execution.'] }
atomic_json(WORK/'Knowledge/Quality-Reviews/financial-independent-receipt-accounting.json',result)
print(json.dumps({k:v for k,v in result.items() if k not in ['per_file','receipt_artifacts']}))
if errors:raise SystemExit(1)
