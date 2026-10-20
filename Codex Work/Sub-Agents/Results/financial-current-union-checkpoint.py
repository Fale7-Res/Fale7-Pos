"""Own audit checkpoint only; preserves coordinator global ledger and all review shards."""
import sys,json
from pathlib import Path
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()));import knowledge_store as k
coverage=json.loads((k.WORK/'Sub-Agents/Results/financial-coordinator-coverage.json').read_text(encoding='utf8'))
remaining=[];counts={};union=set();total=0;own_receipts=[]
for path in coverage['scope_file_paths']:
 source=k.load_symbols(path);valid={x['stable_id'] for x in source['symbols'] if x['isCallable']};total+=len(valid)
 for reviewer in ('financial_integrity','financial_coordinator'):
  p=k.WORK/'Knowledge/Reviews'/reviewer/(path+'.json')
  if not p.exists():continue
  r=json.loads(p.read_text(encoding='utf8'));assert r['worktree_sha256']==source['worktree_sha256'];ids={x['stable_id'] for x in r.get('symbols',[]) if x.get('status')=='ANALYZED'}&valid
  counts[reviewer]=counts.get(reviewer,0)+len(ids);union.update(ids)
  if reviewer=="financial_integrity" and ids:own_receipts.append(r)
 for s in source['symbols']:
  if s['isCallable'] and s['stable_id'] not in union:remaining.append(dict(path=path,stable_id=s['stable_id'],kind=s['kind'],signature=s['qualifiedSymbol'],span=[s['startLine'],s['startColumn'],s['endLine'],s['endColumn']]))
getter_ids=set();context_ids=set()
for folder in (k.WORK/'Knowledge/Reviews-CallableFacets').iterdir():
 for p in folder.rglob('*.json'):
  r=json.loads(p.read_text(encoding='utf8'))
  if r['path'] not in coverage['scope_file_paths']:continue
  getter_ids.update(x['stable_id'] for x in r.get('facets',[]) if x.get('status')=='ANALYZED');context_ids.update(x['stable_id'] for x in r.get('generated_behavior_inputs',[]) if x.get('status')=='BEHAVIOR_ANALYZED')
printing=[x for x in remaining if '/Printing/' in x['path']]
priority=next((x for x in remaining if x['path'].startswith('src/Falah.Infrastructure/') and '/Printing/' not in x['path']),None)
result=dict(baseline=k.BASELINE,updated_at_utc=k.now(),scope='327 source files; author receipts, not independent final quality certification',original_total=total,reviewer_counts=counts,original_union_count=len(union),original_remaining_count=len(remaining),remaining_original_symbols=remaining,additive_getters_completed=len(getter_ids),generated_contexts_completed=len(context_ids),parent_global_coverage='Sub-Agents/Results/financial-coordinator-coverage.json retained unchanged',paused_printing_direct_directory_count=len(printing),paused_printing_scope_note='Direct /Printing/ path classification only; related test/helper residues are not automatically classified. Every ID remains in original8804 denominator.',next_nonprinting_infrastructure_symbol=priority)
k.atomic_json(k.WORK/'Sub-Agents/Results/financial-current-union-progress.json',result)
cp=k.WORK/'Sub-Agents/Checkpoints/financial_integrity.json';d=json.loads(cp.read_text(encoding='utf8'));d['original_reviewer_callable_count']=counts.get('financial_integrity',0);d['last_verified_union_original_receipts']=len(union);d['union_remaining_original_count']=len(remaining);d['remaining_function_ids_reference']='Sub-Agents/Results/financial-current-union-progress.json#/remaining_original_symbols';d['additive_getters_completed']=len(getter_ids);d['generated_contexts_completed']=len(context_ids);d['paused_printing_direct_directory_count']=len(printing);d['next_action']='Next uncovered non-Printing infrastructure symbol '+priority['path']+' '+priority['stable_id']+'; remaining generated contexts follow. Original total8804 retains paused Printing and related residue.' if priority else 'Review remaining finance-owned tests/generated contexts and preserve paused Printing residue inside original denominator.';d['updated_at_utc']=k.now()
if own_receipts:
 latest=max(own_receipts,key=lambda x:x.get('updated_at_utc',''))
 completed=[x for x in own_receipts if not x.get('remaining_callable_ids')]
 if completed:
  done=max(completed,key=lambda x:x.get('updated_at_utc',''));d['last_completed_file']=done['path'];d['last_completed_symbol']=[x['stable_id'] for x in done['symbols'] if x.get('status')=='ANALYZED'][-1]
 d['partial_file']=latest['path'] if latest.get('remaining_callable_ids') else None
 d['partial_symbol']=latest['remaining_callable_ids'][0] if latest.get('remaining_callable_ids') else None
 d['last_receipt_update_basis']='Latest validated own receipt updated_at_utc; metadata pointer, no new review promotion.'
d['remaining_symbol_file_count']=len({x['path'] for x in remaining});d['last_disposition']='Canonical37financial records include requirements question and unverified concurrent-commit snapshot outcome; eleven own lead dispositions saved; refund overlapping acknowledgement LOW/provider unexecuted; FIN028 delegated committed retry recovery qualified. FIN027 report gate source-corroborated with authentication countercontrol. FIN005/FIN016 citation histories preserved. No financial runtime tests.'

# Preserve chosen within-file continuation separately from global first-uncovered ordering.
active_path=d.get('active_assignment_path') or (latest['path'] if own_receipts else None)
active_next=next((x for x in remaining if x['path']==active_path),None)
if active_next is None and own_receipts:
 active_path=latest['path'];active_next=next((x for x in remaining if x['path']==active_path),None)
d['active_assignment_path']=active_path if active_next else None
d['current_partial_file']=active_next['path'] if active_next else None
d['current_partial_symbol']=active_next['stable_id'] if active_next else None
d['current_partial_signature']=active_next['signature'] if active_next else None
d['next_owned_operation']=('Continue actual chosen file '+active_next['path']+' at '+active_next['signature']+' '+active_next['stable_id']+'; select IDs against both reviewer shards before save.') if active_next else d['next_action']
d['global_first_uncovered_symbol_reference']='Sub-Agents/Results/financial-current-union-progress.json#/next_nonprinting_infrastructure_symbol'

k.atomic_json(cp,d)
print('Original receipts',counts,'UNION',len(union),'remaining',len(remaining),'getters',len(getter_ids),'contexts',len(context_ids))
