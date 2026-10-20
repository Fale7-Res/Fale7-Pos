"""Source/identity/evidence quality gates for identified mirror groups; no semantic-score invention."""
from knowledge_store import *
import collections
base=WORK/'Deep-Audit/Duplication-and-Architecture'
ledger=json.loads((base/'clone-review-ledger.json').read_text(encoding='utf8'))
ast=json.loads((base/'ast-duplicate-candidates.json').read_text(encoding='utf8'))
assert len(ledger['groups'])==len(ast['groups'])==218
assert ledger['source_artifact_sha256']==sha256((base/'ast-duplicate-candidates.json').read_bytes())
errors=[];source_cache={};manual=[];generated=[];all_ids=set();batch_count=0
original_ids={'clone-'+g['body_token_sha256'] for g in ast['groups']}
for g in ledger['groups']:
 if g['group_id'] not in original_ids:errors.append({'id':g['group_id'],'error':'Group not in actual AST artifact'})
 if g['status'].startswith('PENDING'):errors.append({'id':g['group_id'],'error':'Pending group'})
 if g['status'].startswith('GENERATED'):generated.append(g)
 else:manual.append(g)
 if len(g['reasoning'])<50:errors.append({'id':g['group_id'],'error':'No substantive context decision'})
 artifact=g.get('evidence_artifact')
 if artifact:
  evidence=json.loads((WORK/artifact).read_text(encoding='utf8'));entry=next((x for x in evidence['comparisons'] if x['group_id']==g['group_id']),None)
  if entry is None:errors.append({'id':g['group_id'],'error':'Missing batch comparison'})
  else:
   for key in ['reasoning','callers_context','financial_security_effects','recommendation','tests']:
    if not entry.get(key):errors.append({'id':g['group_id'],'error':'Missing '+key})
   batch_count+=1
 for o in g['occurrences']:
  p=o['path']
  if p not in source_cache:
   source_cache[p]=load_symbols(p)
   if sha256((ROOT/p).read_bytes())!=source_cache[p]['worktree_sha256']:errors.append({'path':p,'error':'Source changed'})
  s=next((s for s in source_cache[p]['symbols'] if s['stable_id']==o['stable_id']),None)
  if not s or s['startLine']!=o['start_line'] or s['endLine']!=o['end_line']:errors.append({'path':p,'error':'Invalid source symbol/span'})
  if o['source_git_blob_sha256']!=source_cache[p]['git_blob_sha256']:errors.append({'path':p,'error':'Baseline blob identity mismatch'})
  all_ids.add(o['stable_id'])
context_ids={o['stable_id'] for g in manual for o in g['occurrences']}
result={'at_utc':now(),'baseline':BASELINE,'candidate_groups':len(ledger['groups']),'generated_history_accounted':len(generated),'actual_context_decision_groups':len(manual),
 'application_context_groups':sum('TEST' not in g['status'] for g in manual),'test_context_groups':sum('TEST' in g['status'] for g in manual),
 'new_batch_decisions_verified':batch_count,'earlier_decisions_preserved':len(manual)-batch_count,'context_source_files':len({o['path'] for g in manual for o in g['occurrences']}),
 'context_callable_ids':len(context_ids),'all_group_source_files':len(source_cache),'all_group_callable_ids':len(all_ids),
 'status_counts':dict(collections.Counter(g['status'] for g in ledger['groups'])),'errors':errors,
 'gate':'PASS_IDENTIFIED_GROUP_SOURCE_EVIDENCE' if not errors else 'FAIL',
 'scope_limit':'Validates source hashes, original AST identity, saved individual context decisions and evidence links. Does not independently certify every conclusion, execute tests, exhaust renamed semantic clones/dead code or count all generated CLR behavior. Generated migration history is accounted, not recommended for consolidation.'}
atomic_json(base/'mirror-final-quality-gate.json',result)
assert not errors,errors
print(json.dumps(result))
