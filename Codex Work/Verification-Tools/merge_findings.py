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
for fid, relative in [('FIN-017','shift-close-attribution.json'),('FIN-018','negative-purchase-lines.json'),('FIN-020','inventory-unit-first-movement.json')]:
    challenge_path=WORK/'Deep-Audit/Database-and-Finance/Independent-Challenges'/relative
    if challenge_path.exists():
        challenge=json.loads(challenge_path.read_text(encoding='utf8'))
        if challenge.get('classification','').startswith('SOURCE_CORROBORATED_MEDIUM'):
            independent.add(fid)
            independent_details[fid]={'artifact':str(challenge_path.relative_to(WORK)).replace('\\','/'), 'disposition':challenge['classification'], 'qualifications':challenge['counterevidence']}

for fid, rel in [
    ('FIN-CAND-PAYROLL-001','Sub-Agents/Handoffs/payroll-terminal-state-independent-challenge.json'),
    ('FIN-CAND-CLOSEDAY-ADMISSION-001','Sub-Agents/Handoffs/business-day-admission-independent-challenge.json'),
    ('FIN-CAND-CASHCOUNT-001','Sub-Agents/Handoffs/cash-denomination-independent-challenge.json'),
    ('FIN-CAND-PREPGRAPH-001','Sub-Agents/Handoffs/prep-graph-independent-challenge.json'),
    ('FIN-CAND-PACKAGING-COVERAGE-001','Sub-Agents/Handoffs/packaging-completeness-independent-challenge.json'),
    ('FIN-CAND-REPORT-RANGE-001','Sub-Agents/Handoffs/reverse-date-range-independent-challenge.json')]:
    challenge_path=WORK/rel
    if challenge_path.exists():
        challenge=json.loads(challenge_path.read_text(encoding='utf8'))
        if challenge.get('status')=='INDEPENDENT_STATIC_CORROBORATION_WITH_QUALIFICATIONS':
            for ref in challenge['sources']:
                if sha256((ROOT/ref['path']).read_bytes())!=ref['worktree_sha256']:
                    raise ValueError('Independent financial challenge source invalidation: '+ref['path'])
            independent.add(fid)
            independent_details[fid]={'artifact':rel,'disposition':challenge['status'],
                'reviewer':challenge['reviewer'],'qualifications':challenge.get('rejected_or_qualified_claims',challenge.get('counterevidence',[])),
                'runtime':'No SQL/HTTP/DB/concurrency reproduction executed'}

delivery_challenge=WORK/'Deep-Audit/Database-and-Finance/Independent-Challenges/mixed-pending-delivery-custody.json'
if delivery_challenge.exists():
    challenge=json.loads(delivery_challenge.read_text(encoding='utf8'))
    if challenge.get('classification')=='SURVIVES_INDEPENDENT_STATIC_CHALLENGE_WITH_COLLECTION_POLICY_QUALIFICATIONS':
        for ref in challenge['sources']:
            if sha256((ROOT/ref['path']).read_bytes())!=ref['worktree_sha256']:
                raise ValueError('FIN019 independent source invalidation: '+ref['path'])
        independent.add('FIN-019')
        independent_details['FIN-019']={'artifact':str(delivery_challenge.relative_to(WORK)).replace('\\','/'),
            'disposition':challenge['classification'],'reviewer':challenge['reviewer'],
            'qualifications':challenge['counterevidence']}
tax_challenge=WORK/'Sub-Agents/Handoffs/tax-mode-independent-challenge.json'
if tax_challenge.exists():
    tax=json.loads(tax_challenge.read_text(encoding='utf8'))
    if tax.get('status')=='INDEPENDENT_STATIC_CORROBORATION_QUALIFIED':
        for ref in tax['source_evidence']:
            if sha256((ROOT/ref['path']).read_bytes())!=ref['worktree_sha256']:
                raise ValueError('FIN021 source invalidation: '+ref['path'])
        independent.add('FIN-021')
        independent_details['FIN-021']={'artifact':str(tax_challenge.relative_to(WORK)).replace('\\','/'),
            'disposition':tax['status'],'reviewer':tax['reviewer'],'qualifications':tax['attempted_counterexamples']+tax['limitations']}
compensation_challenge=WORK/'Sub-Agents/Handoffs/compensation-mode-independent-challenge.json'
if compensation_challenge.exists():
    compensation=json.loads(compensation_challenge.read_text(encoding='utf8'))
    if compensation.get('status')=='INDEPENDENT_STATIC_CORROBORATION_QUALIFIED':
        for ref in compensation['source_evidence']:
            if sha256((ROOT/ref['path']).read_bytes())!=ref['worktree_sha256']:
                raise ValueError('FIN022 source invalidation: '+ref['path'])
        independent.add('FIN-022')
        independent_details['FIN-022']={'artifact':str(compensation_challenge.relative_to(WORK)).replace('\\','/'),
            'disposition':compensation['status'],'reviewer':compensation['reviewer'],
            'qualifications':compensation['counterevidence']+compensation['limitations']}
attendance_challenge=WORK/'Deep-Audit/Database-and-Finance/Independent-Challenges/attendance-utc-reconstruction.json'
if attendance_challenge.exists():
    attendance=json.loads(attendance_challenge.read_text(encoding='utf8'))
    if attendance.get('classification')=='SURVIVES_INDEPENDENT_STATIC_CHALLENGE_WITH_DATE_AND_ENTITLEMENT_QUALIFICATIONS':
        for ref in attendance['sources']:
            if sha256((ROOT/ref['path']).read_bytes())!=ref['worktree_sha256']:
                raise ValueError('FIN023 source invalidation: '+ref['path'])
        independent.add('FIN-023')
        independent_details['FIN-023']={'artifact':str(attendance_challenge.relative_to(WORK)).replace('\\','/'),
            'disposition':attendance['classification'],'reviewer':attendance['reviewer'],
            'qualifications':attendance['counterevidence'],'runtime':'NOT_EXECUTED'}
correction_basis_challenge=WORK/'Deep-Audit/Database-and-Finance/Independent-Challenges/attendance-correction-rate-basis.json'
if correction_basis_challenge.exists():
    basis=json.loads(correction_basis_challenge.read_text(encoding='utf8'))
    if basis.get('classification')=='INDEPENDENT_STATIC_CORROBORATION_POLICY_QUALIFIED_RECONCILIATION_RISK':
        for ref in basis['sources']:
            if sha256((ROOT/ref['path']).read_bytes())!=ref['worktree_sha256']:
                raise ValueError('Correction-basis source invalidation: '+ref['path'])
        independent.add('FIN-CAND-CORRECTION-BASIS-001')
        independent_details['FIN-CAND-CORRECTION-BASIS-001']={'artifact':str(correction_basis_challenge.relative_to(WORK)).replace('\\','/'),
            'disposition':basis['classification'],'reviewer':basis['reviewer'],
            'qualifications':basis['counterevidence'],'owner_policy_required':basis['owner_policy_required'],'runtime':'NOT_EXECUTED'}
employee_credit_challenge=WORK/'Deep-Audit/Database-and-Finance/Independent-Challenges/employee-credit-admission-and-cache.json'
if employee_credit_challenge.exists():
    credits=json.loads(employee_credit_challenge.read_text(encoding='utf8'))
    if credits.get('classification')=='INDEPENDENT_STATIC_CHALLENGE_WITH_PROVIDER_ACCEPTANCE_AND_IMPACT_LIMITS' and credits.get('schema_counterevidence'):
        for ref in credits['sources']:
            if sha256((ROOT/ref['path']).read_bytes())!=ref['worktree_sha256']:
                raise ValueError('Employee credit source invalidation: '+ref['path'])
        for fid in ['FIN-024','FIN-025','FIN-026']:
            independent.add(fid)
            independent_details[fid]={'artifact':str(employee_credit_challenge.relative_to(WORK)).replace('\\','/'),
                'disposition':credits['classification'],'reviewer':'financial_integrity disposition plus root challenge; original root lead preserved',
                'qualifications':credits['counterevidence']+credits['schema_counterevidence']['limitations'],'runtime':'NOT_EXECUTED'}
legacy_provenance_path=WORK/'Deep-Audit/Database-and-Finance/financial-legacy-independent-provenance.json'
if legacy_provenance_path.exists():
    provenance=json.loads(legacy_provenance_path.read_text(encoding='utf8'))
    for record in provenance['records']:
        raw=(WORK/record['artifact']).read_bytes()
        if sha256(raw)!=record['artifact_sha256']:raise ValueError('Legacy challenge artifact changed: '+record['id'])
        lines=raw.decode('utf-8-sig').splitlines()
        lo,hi=record['report_line_span']
        if not (1<=lo<=hi<=len(lines)) or record['saved_evidence']!='\n'.join(lines[lo-1:hi]):raise ValueError('Legacy challenge citation mismatch: '+record['id'])
        independent.add(record['id'])
        independent_details[record['id']]={'artifact':record['artifact'],'artifact_sha256':record['artifact_sha256'],
            'report_line_span':record['report_line_span'],'reviewer':record['reviewer'],
            'disposition':record['status'],'qualifications':'Original bounded static/counterevidence conclusions only, no new source coverage or runtime reproduction; precise provenance in financial-legacy-independent-provenance.json'}
snapshot_contract_path=WORK/'Sub-Agents/Handoffs/financial-snapshot-contract-independent-challenge.json'
if snapshot_contract_path.exists():
    snapshots=json.loads(snapshot_contract_path.read_text(encoding='utf8'))
    for ref in snapshots['sources']:
        if sha256((ROOT/ref['path']).read_bytes())!=ref['worktree_sha256']:raise ValueError('Snapshot challenge source invalidation: '+ref['path'])
    for fid,key in [('FIN-027','SNAPSHOT_REPORT_PERMISSION_BOUNDARY'),('FIN-CAND-CLOSE-SNAPSHOT-REVISION-001','SNAPSHOT_AUTHORITATIVE_REVISION_CONCURRENCY')]:
        disposition=next(c for c in snapshots['contracts'] if c['key']==key)
        independent.add(fid)
        independent_details[fid]={'artifact':str(snapshot_contract_path.relative_to(WORK)).replace('\\','/'),
            'reviewer':snapshots['reviewer'],'disposition':disposition['disposition'],
            'qualifications':disposition['counterevidence'],'runtime':'NOT_EXECUTED; revision duplicate-commit outcome unverified'}
employee_replay_challenge=WORK/'Sub-Agents/Handoffs/financial-employee-credit-approval-replay-independent-challenge.json'
if employee_replay_challenge.exists():
    replay=json.loads(employee_replay_challenge.read_text(encoding='utf8'))
    if replay.get('status')=='INDEPENDENT_STATIC_CORROBORATION_WITH_PRECONDITIONS':
        for ref in replay['sources']:
            if sha256((ROOT/ref['path']).read_bytes())!=ref['worktree_sha256']:raise ValueError('FIN028 source invalidation: '+ref['path'])
        independent.add('FIN-028')
        independent_details['FIN-028']={'artifact':str(employee_replay_challenge.relative_to(WORK)).replace('\\','/'),
            'reviewer':replay['reviewer'],'disposition':replay['status'],
            'qualifications':replay['counterevidence'],'runtime':'NOT_EXECUTED; no duplicate-credit or permanent-data-loss claim'}
driver_provenance_challenge=WORK/'Sub-Agents/Handoffs/financial-driver-variance-approver-independent-challenge.json'
if driver_provenance_challenge.exists():
    driver=json.loads(driver_provenance_challenge.read_text(encoding='utf8'))
    if driver.get('status')=='INDEPENDENT_STATIC_CORROBORATION_ATTRIBUTION_ONLY':
        for ref in driver['sources']:
            if sha256((ROOT/ref['path']).read_bytes())!=ref['worktree_sha256']:raise ValueError('FIN017 driver variant source invalidation: '+ref['path'])
        independent.add('FIN-017')
        independent_details.setdefault('FIN-017',{})['related_variant_challenge']={'artifact':str(driver_provenance_challenge.relative_to(WORK)).replace('\\','/'),
            'reviewer':driver['reviewer'],'disposition':driver['status'],'qualifications':driver['counterevidence'],'runtime':'NOT_EXECUTED; attribution only, strict permission remains enforced'}
refund_replay_challenge=WORK/'Sub-Agents/Handoffs/financial-refund-locked-replay-independent-challenge.json'
if refund_replay_challenge.exists():
    refund=json.loads(refund_replay_challenge.read_text(encoding='utf8'))
    if refund.get('status')=='INDEPENDENT_STATIC_CORROBORATION_ACKNOWLEDGEMENT_ONLY':
        for ref in refund['sources']:
            if sha256((ROOT/ref['path']).read_bytes())!=ref['worktree_sha256']:raise ValueError('Refund replay source invalidation: '+ref['path'])
        independent.add('FIN-CAND-REFUND-REPLAY-001')
        independent_details['FIN-CAND-REFUND-REPLAY-001']={'artifact':str(refund_replay_challenge.relative_to(WORK)).replace('\\','/'),
            'reviewer':refund['reviewer'],'disposition':refund['status'],'qualifications':refund['counterevidence'],'runtime':'NOT_EXECUTED; LOW acknowledgement only; sequential retry recovers'}
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
                                           'isolated_reproduction':fid in ('SEC-001','FIN-016','FIN-CAND-CASHCOUNT-001','FIN-CAND-PACKAGING-COVERAGE-001','FIN-CAND-REPORT-RANGE-001'),
                                           'isolated_scope':'PARTIAL_DOMAIN_COMPONENTS_PLUS_SYNTHETIC_REPORT_BOUNDARY' if fid in ('FIN-CAND-PACKAGING-COVERAGE-001','FIN-CAND-REPORT-RANGE-001') else 'See source record; no full live exploit or financial acceptance inferred',
                                           'live_end_to_end_exploit':'NOT_EXECUTED'},
             'origin':{'path':'Codex Work/'+rel,'file_sha256':sha256(raw),'record_sha256':sha256(json.dumps(item,sort_keys=True).encode())},
             'source_record':item,'mandatory_field_gate':'PENDING_INDEPENDENT_SCHEMA_AND_SOURCE_REFERENCE_REVIEW; UNKNOWN fields are not filled by inference',
             'disagreement_history':records.get(fid,{}).get('disagreement_history',[])}
        if item.get('finding_classification')=='SOURCE_INVARIANT_GAP_WITH_UNVERIFIED_CONCURRENT_COMMIT_OUTCOME':
            rec['finding_classification']=item['finding_classification']
            rec['verification_classification']['source_confirmed']='SOURCE_ADMISSION_GAP_ONLY; DUPLICATE_AUTHORITATIVE_COMMIT_NOT_CONFIRMED'
            rec['verification_classification']['provider_schedule']='NOT_EXECUTED'
        if item.get('finding_classification')=='SOURCE_REPLAY_ADMISSION_GAP_WITH_UNVERIFIED_CONCURRENT_RESPONSE':
            rec['finding_classification']=item['finding_classification']
            rec['verification_classification']['source_confirmed']='SOURCE_ORDERING_GAP_ONLY; CONCURRENT_RESPONSE_NOT_REPRODUCED'
            rec['verification_classification']['provider_schedule']='NOT_EXECUTED'
        if item.get('finding_classification')=='REQUIREMENTS_QUESTION_WITH_SOURCE_SUPPORTED_COUNTEREXAMPLE':
            rec['finding_classification']='REQUIREMENTS_QUESTION_WITH_SOURCE_SUPPORTED_COUNTEREXAMPLE'
            rec['verification_classification']['source_confirmed']='SOURCE_BEHAVIOR_ONLY; OWNER_VALUATION_POLICY_NOT_CONFIRMED'
            rec['verification_classification']['owner_policy_required']=item['owner_policy_required']
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
