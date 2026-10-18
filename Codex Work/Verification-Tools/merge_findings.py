"""Parent-only, lossless ordinary-audit registry merge; not plugin canonical output."""
from knowledge_store import *
import re
paths=['Sub-Agents/Results/financial-findings.json','Sub-Agents/Results/printing-findings.json',
       'Sub-Agents/Results/ui-findings.json','Deep-Audit/Security/manual-security-findings.json']
parent_finance=WORK/'Deep-Audit/Database-and-Finance/parent-financial-findings.json'
if parent_finance.exists():paths.append('Deep-Audit/Database-and-Finance/parent-financial-findings.json')
baseline=json.loads((WORK/'Deep-Audit/Source-Inventory/verified-baseline-hashes.json').read_text(encoding='utf8'))
hashes={r['path']:r for r in baseline['files']}
legacy=json.loads((WORK/'Deep-Audit/phase-progress-evidence.latest.json').read_text(encoding='utf8'))
independent=set(legacy['independently_corroborated_saved_finding_ids'])|{'SEC-001'}
independent_details={}
negative_challenge=WORK/'Deep-Audit/Database-and-Finance/negative-fee-parent-source-challenge.json'
negative_result=WORK/'Local-Verification/negative-fee-pure-results.json'
if negative_challenge.exists() and negative_result.exists():
    negative=json.loads(negative_result.read_text(encoding='utf8'))
    if negative.get('assertions')==7 and negative.get('failures')==0:
        independent.add('FIN-016')
        independent_details['FIN-016']={'artifact':str(negative_challenge.relative_to(WORK)).replace('\\','/'),
            'disposition':'Independent source trace plus7 pure Domain arithmetic assertions',
            'qualifications':'Separate net10 audit executable; original Domain source unchanged. No HTTP/user/DB/stock/ledger operation executed.'}
checkout_challenge=WORK/'Deep-Audit/Database-and-Finance/Independent-Challenges/checkout-after-close.json'
if checkout_challenge.exists():
    checkout=json.loads(checkout_challenge.read_text(encoding='utf8'))
    if checkout['disposition']=='SURVIVES_INDEPENDENT_SOURCE_CHALLENGE_TOCTOU':
        independent.add('FIN-015')
        independent_details['FIN-015']={'artifact':str(checkout_challenge.relative_to(WORK)).replace('\\','/'),
            'disposition':checkout['disposition'],'qualifications':checkout['counterevidence']}
flow_challenge=WORK/'Deep-Audit/Printing/FLOW001-parent-independent-challenge.json'
if flow_challenge.exists():
    flow=json.loads(flow_challenge.read_text(encoding='utf8'))
    if flow['disposition']=='SURVIVES_BOUNDED_REQUIREMENT_GAP_CHALLENGE':
        independent.add('FLOW-001')
        independent_details['FLOW-001']={'artifact':str(flow_challenge.relative_to(WORK)).replace('\\','/'),
            'disposition':flow['disposition'],'qualifications':flow['counterevidence'],
            'classification':'Owner-requested workflow gap; overlapping UI003, not two distinct security defects'}
finance_challenge=WORK/'Deep-Audit/Database-and-Finance/financial-parent-challenges.json'
if finance_challenge.exists():
    for challenged in json.loads(finance_challenge.read_text(encoding='utf8'))['findings']:
        if challenged['disposition'].startswith('SURVIVES_INDEPENDENT_STATIC_CHALLENGE'):
            independent.add(challenged['id'])
            independent_details[challenged['id']]={'artifact':str(finance_challenge.relative_to(WORK)).replace('\\','/'),
                'disposition':challenged['disposition'],'qualifications':challenged.get('counterevidence','')}
restore_challenge=WORK/'Sub-Agents/Handoffs/financial-restore-outcome-challenge.json'
if restore_challenge.exists():
    restored=json.loads(restore_challenge.read_text(encoding='utf8'))
    if restored.get('classification','').startswith('INDEPENDENT_STATIC_CHALLENGE_CONFIRMS'):
        independent.add('FIN-014')
        independent_details['FIN-014']={'artifact':str(restore_challenge.relative_to(WORK)).replace('\\','/'),
            'disposition':restored['classification'],'qualifications':restored['counterevidence']}
ui_challenge_gate=WORK/'Knowledge/Quality-Reviews/ui-independent-challenge-validation.json'
if ui_challenge_gate.exists():
    validated=json.loads(ui_challenge_gate.read_text(encoding='utf8'))
    if not validated['errors']:
        for r in validated['reports']:
            artifact=WORK/r['artifact']
            if sha256(artifact.read_bytes())!=r['artifact_sha256']:raise ValueError('Stale independent UI gate '+r['id'])
            if r['disposition'].startswith(('SURVIVES_SOURCE_CHALLENGE','SURVIVES_BOUNDED_ACTIVE_CLIENT_REQUIREMENT_GAP','REPORTABLE_SOURCE_RETENTION_BEHAVIOR_DEPLOYMENT_EXPOSURE_UNVERIFIED')):
                independent.add(r['id'])
                details=json.loads(artifact.read_text(encoding='utf8'))
                independent_details[r['id']]={'artifact':r['artifact'],'disposition':r['disposition'],
                    'severity_qualification':r['severity_qualification'],'qualifications':details['counterevidence']}
sec003_challenge=WORK/'Deep-Audit/Security/SEC003-independent-challenge.json'
if sec003_challenge.exists():
    challenge=json.loads(sec003_challenge.read_text(encoding='utf8'))
    if challenge.get('disposition')=='SURVIVES_SOURCE_CHALLENGE_WITH_POLICY_AND_DEPLOYMENT_QUALIFICATIONS':
        independent.add('SEC-003')
for fid, relative in [('FIN-017','shift-close-attribution.json'),('FIN-018','negative-purchase-lines.json')]:
    challenge_path=WORK/'Deep-Audit/Database-and-Finance/Independent-Challenges'/relative
    if challenge_path.exists():
        challenge=json.loads(challenge_path.read_text(encoding='utf8'))
        if challenge.get('classification','').startswith('SOURCE_CORROBORATED_MEDIUM'):
            independent.add(fid)
            independent_details[fid]={'artifact':str(challenge_path.relative_to(WORK)).replace('\\','/'), 'disposition':challenge['classification'], 'qualifications':challenge['counterevidence']}

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
                                        'scope':'SEC-001 binding, FIN-016 fee arithmetic and FIN-CAND-CASHCOUNT-001 count arithmetic isolated only; other mechanisms static; SEC-003 HIGH conditional on private HR data/restricted users, exact owner policy/deployment unverified; no full live exploit claimed'},
             'verification_classification':{'source_confirmed':True if fid in independent else 'SEE_SOURCE_RECORD',
                                           'isolated_reproduction':fid in ('SEC-001','FIN-016','FIN-CAND-CASHCOUNT-001'),'live_end_to_end_exploit':'NOT_EXECUTED'},
             'origin':{'path':'Codex Work/'+rel,'file_sha256':sha256(raw),'record_sha256':sha256(json.dumps(item,sort_keys=True).encode())},
             'source_record':item,'mandatory_field_gate':'PENDING_INDEPENDENT_SCHEMA_AND_SOURCE_REFERENCE_REVIEW; UNKNOWN fields are not filled by inference',
             'disagreement_history':records.get(fid,{}).get('disagreement_history',[])}
        if fid in independent_details:rec['independent_verification']['saved_challenge']=independent_details[fid]
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
