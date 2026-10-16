"""Parent-only, lossless ordinary-audit registry merge; not plugin canonical output."""
from knowledge_store import *
import re
paths=['Sub-Agents/Results/financial-findings.json','Sub-Agents/Results/printing-findings.json',
       'Sub-Agents/Results/ui-findings.json','Deep-Audit/Security/manual-security-findings.json']
baseline=json.loads((WORK/'Deep-Audit/Source-Inventory/verified-baseline-hashes.json').read_text(encoding='utf8'))
hashes={r['path']:r for r in baseline['files']}
legacy=json.loads((WORK/'Deep-Audit/phase-progress-evidence.latest.json').read_text(encoding='utf8'))
independent=set(legacy['independently_corroborated_saved_finding_ids'])|{'SEC-001'}
target=WORK/'Deep-Audit/MASTER_FINDINGS.json'
old=json.loads(target.read_text(encoding='utf8')) if target.exists() else None
records={r['id']:r for r in old['findings']} if old else {}
inputs=[];changed=[]
for rel in paths:
    raw=(WORK/rel).read_bytes();j=json.loads(raw.decode('utf-8-sig'));items=j if isinstance(j,list) else j['findings']
    inputs.append({'path':'Codex Work/'+rel,'sha256':sha256(raw)})
    for item in items:
        fid=item['id'];references=[]
        for path in sorted(set(re.findall(r'(?:src|tests|tools|\.config)/[A-Za-z0-9_./-]+\.(?:cs|xaml|ps1|json|csproj)',json.dumps(item)))):
            if path not in hashes:continue
            r=hashes[path]
            if sha256((ROOT/path).read_bytes())!=r['worktree_sha256']:raise ValueError('Source invalidation '+path)
            references.append({'path':path,'source_git_blob_sha256':r['git_blob_sha256'],'worktree_sha256':r['worktree_sha256']})
        # Normalized fields aid navigation. Lossless source_record keeps the authoritative details.
        rec={'id':fid,'baseline':BASELINE,'severity':item.get('severity','UNKNOWN'),
             'confidence':item.get('confidence','UNKNOWN'),'original_status':item.get('status','UNKNOWN'),
             'title':item.get('title','UNKNOWN'),'source_references':references,
             'independent_verification':{'status':'SAVED_STATIC_CORROBORATION' if fid in independent else 'PENDING_OR_NOT_REQUIRED',
                                        'scope':'SEC-001 mechanism isolated, others static; no full live exploit claimed'},
             'verification_classification':{'source_confirmed':True if fid in independent else 'SEE_SOURCE_RECORD',
                                           'isolated_reproduction':fid=='SEC-001','live_end_to_end_exploit':'NOT_EXECUTED'},
             'origin':{'path':'Codex Work/'+rel,'file_sha256':sha256(raw),'record_sha256':sha256(json.dumps(item,sort_keys=True).encode())},
             'source_record':item,'mandatory_field_gate':'PENDING_INDEPENDENT_SCHEMA_AND_SOURCE_REFERENCE_REVIEW; UNKNOWN fields are not filled by inference',
             'disagreement_history':records.get(fid,{}).get('disagreement_history',[])}
        prev=records.get(fid)
        if prev and prev.get('origin',{}).get('record_sha256')!=rec['origin']['record_sha256']:
            event={'at_utc':now(),'reason':'Original reviewer persisted a new evidence/status revision; predecessor retained',
                   'old_origin':prev['origin'],'old_original_status':prev.get('original_status')}
            rec['disagreement_history'].append(event)
        if prev!=rec:changed.append(fid)
        records[fid]=rec
if old and changed:
    hist=WORK/'Knowledge/History/Master-Findings'/('MASTER_FINDINGS.'+sha256(json.dumps(old,sort_keys=True).encode())[:20]+'.json')
    if not hist.exists():atomic_json(hist,old)
payload={'schema_version':1,'artifact_kind':'Ordinary manual deep-audit registry; NOT Codex Security plugin canonical artifacts',
         'status':'IN_PROGRESS','baseline':BASELINE,'sole_writer':'parent orchestrator',
         'updated_at_utc':now(),'inputs':inputs,'finding_count':len(records),'findings':[records[k] for k in sorted(records)],
         'coverage_limit':'Registry consolidation is not validation or completed security coverage. Original evidence is preserved losslessly.'}
atomic_json(target,payload)
print(json.dumps({'canonical_findings':len(records),'changed_ids':changed,'retired_or_erased_ids':0}))
