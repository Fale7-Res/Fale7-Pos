"""Read-only semantic evidence integrity gate; not manual reasoning certification."""
from knowledge_store import *
registry_path=WORK/'Deep-Audit/Duplication-and-Architecture/semantic-comparison-registry.json'
registry=json.loads(registry_path.read_text(encoding='utf8'))
errors=[];paths=set();ids=set();checked=0;range_count=0
for item in registry['inputs']:
    raw=(WORK/item['path']).read_bytes()
    if sha256(raw)!=item['sha256']:errors.append({'artifact':item['path'],'error':'Artifact digest mismatch'})
    artifact=json.loads(raw)
    for ref in artifact.get('source_references',artifact.get('sources',[])):
        p=ROOT/ref['path'];source=p.read_bytes();lines=source.decode('utf-8-sig').splitlines();paths.add(ref['path']);checked+=1
        if sha256(source)!=ref['worktree_sha256']:errors.append({'path':ref['path'],'error':'Source hash mismatch'})
        index=json.loads((WORK/'Knowledge/Source-Symbols'/(ref['path']+'.json')).read_text(encoding='utf8'))
        by_id={s['stable_id']:s for s in index['symbols']}
        if ref.get('source_git_blob_sha256')!=index['git_blob_sha256']:errors.append({'path':ref['path'],'error':'Baseline identity mismatch'})
        for symbol in ref.get('relevant_symbols',[]):
            sid=symbol['stable_id'];ids.add(sid);original=by_id.get(sid)
            if original is None:errors.append({'path':ref['path'],'id':sid,'error':'Missing original stable symbol'});continue
            expected=[original['startLine'],original['startColumn'],original['endLine'],original['endColumn']]
            if symbol['span']!=expected:errors.append({'path':ref['path'],'id':sid,'error':'Symbol span mismatch'})
        for lo,hi in ref.get('line_ranges',[]):
            range_count+=1
            if not(1<=lo<=hi<=len(lines)):errors.append({'path':ref['path'],'range':[lo,hi],'error':'Invalid range'})
if len(paths)!=registry['referenced_source_paths'] or len(ids)!=registry['referenced_callable_context_ids']:errors.append({'error':'Registry union count mismatch'})
result={'at_utc':now(),'baseline':BASELINE,'semantic_groups':len(registry['semantic_groups']),'artifact_inputs':len(registry['inputs']),'source_reference_records':checked,'distinct_source_paths':len(paths),'distinct_referenced_symbol_ids':len(ids),'explicit_ranges_checked':range_count,'errors':errors,'gate':'PASS_SOURCE_IDENTITY_AND_SYMBOL_REFERENCES' if not errors else 'FAIL','limits':'Mechanical saved evidence identity only. Not independent semantic proof, runtime verification, exhaustive semantic-clone detection, or new financial callable review. Indexed source map statuses never promoted.'}
atomic_json(WORK/'Deep-Audit/Duplication-and-Architecture/semantic-evidence-quality-gate.json',result)
print(json.dumps(result))
if errors:raise SystemExit(1)
