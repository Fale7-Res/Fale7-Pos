"""Validate exact persisted symbol coverage without promoting inventory/legacy labels."""
from knowledge_store import *
from collections import Counter,defaultdict
identities={};byfile={};errors=[]
for p in sorted((WORK/'Knowledge/Source-Symbols').rglob('*.json')):
    j=json.loads(p.read_text(encoding='utf8'));path=j['path']
    if sha256((ROOT/path).read_bytes())!=j['worktree_sha256']:errors.append({'path':path,'error':'SOURCE_HASH_CHANGED'})
    byfile[path]=j
    for s in j['symbols']:
        expected=stable_symbol_id(path,j['git_blob_sha256'],s)
        if s['stable_id']!=expected or expected in identities:errors.append({'path':path,'error':'UNSTABLE_OR_DUPLICATE_ID'})
        identities[s['stable_id']]=(path,s)
required=('reasoning','inputs','outputs','side_effects','trust_boundaries','database','printing_ui','performance','duplicates_conflicts','issues_hypotheses','tests_missing','verified_callers_callees')
counts={};receipts=[];quality=[];all_callables=set()
for folder in sorted((WORK/'Knowledge/Reviews').iterdir()):
    if not folder.is_dir():continue
    analyzed=set();callables=set();files=0;reasoning=defaultdict(list)
    for p in sorted(folder.rglob('*.json')):
        j=json.loads(p.read_text(encoding='utf8'));files+=1;path=j['path'];seen=set()
        if path not in byfile or j['worktree_sha256']!=byfile[path]['worktree_sha256']:errors.append({'path':str(p),'error':'REVIEW_SOURCE_HASH_MISMATCH'})
        for s in j.get('symbols',[]):
            sid=s['stable_id']
            if sid not in identities or identities[sid][0]!=path or sid in seen:errors.append({'path':str(p),'error':'INVALID_OR_DUPLICATE_REVIEW_ID','id':sid});continue
            seen.add(sid)
            if s.get('status')!='ANALYZED':continue
            missing=[k for k in required if k not in s]
            if missing or not isinstance(s['reasoning'],str) or len(s['reasoning'].strip())<50:
                errors.append({'path':str(p),'error':'INCOMPLETE_REASONING_SCHEMA','id':sid,'missing':missing});continue
            analyzed.add(sid);reasoning[s['reasoning']].append(sid)
            if identities[sid][1]['isCallable']:callables.add(sid)
            receipts.append({'stable_id':sid,'path':path,'reviewer':folder.name,'record':str(p.relative_to(WORK)).replace('\\','/'),'isCallable':identities[sid][1]['isCallable']})
    for text,ids in reasoning.items():
        if len(ids)>3:quality.append({'reviewer':folder.name,'issue':'REPEATED_REASONING_REQUIRES_QUALITY_CHECK','stable_ids':ids,'count':len(ids)})
    counts[folder.name]={'review_shards':files,'analyzed_symbols':len(analyzed),'analyzed_callables':len(callables)}
    all_callables.update(callables)
flagged={sid for q in quality for sid in q['stable_ids']}
out={'schema_version':1,'baseline':BASELINE,'at_utc':now(),'source_identity_records':len(byfile),'indexed_declarations':len(identities),
     'indexed_callables':sum(x[1]['isCallable'] for x in identities.values()),'reviewers':counts,
     'unique_analyzed_callable_receipts':len(all_callables),'indexed_callables_without_stable_reasoning_receipt':sum(x[1]['isCallable'] for x in identities.values())-len(all_callables),
     'review_depth_certified_count':'UNKNOWN; schema and identity validation cannot certify reasoning depth',
     'callables_flagged_for_repeated_reasoning':len(flagged),
     'meaning':'Exact identity/schema/source consistency verified; these are author-asserted analysis receipts, not independently approved deep function reviews. Legacy heading matches excluded. Repeated reasoning is an open quality gate, not automatic falsification.',
     'errors':errors,'quality_review_queue':quality,'receipts':receipts}
atomic_json(WORK/'Knowledge/coverage-index.json',out)
print(json.dumps({k:v for k,v in out.items() if k not in ('receipts','quality_review_queue')},ensure_ascii=False))
if errors:raise SystemExit(1)
