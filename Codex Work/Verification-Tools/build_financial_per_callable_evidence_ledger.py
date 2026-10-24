#!/usr/bin/env python3
"""Build a read-only per-callable evidence-class ledger for the financial source scope.
This maps existing source indexes and review receipts; it does NOT certify reasoning quality.
"""
import hashlib, json, os, tempfile
from collections import Counter, defaultdict
from pathlib import Path
ROOT=Path('.')
WORK=ROOT/'Codex Work'
BASELINE='c35816ecae49633674775dd32061c6ee92f94aaf'
scope=json.loads((WORK/'Sub-Agents/Results/financial-symbol-remaining.json').read_text(encoding='utf-8'))
indexed=set(scope['analyzed_ids'])
resq=json.loads((WORK/'Sub-Agents/Results/financial-residual-213-reconciliation-queue.json').read_text(encoding='utf-8'))
residual={x['stable_id']:x for x in resq['items']}
if len(residual)!=213: raise SystemExit(f'unexpected residual count {len(residual)}')
continuation_path=WORK/'Sub-Agents/Handoffs/financial-integrity-continuation-receipt-manifest-reconciliation.json'
continuation_manifest=json.loads(continuation_path.read_text(encoding='utf-8'))
continuation={x['stable_id']:x for x in continuation_manifest['receipt_ids']}
if len(continuation)!=989: raise SystemExit(f'unexpected continuation manifest count {len(continuation)}')

universe={}; hash_errors=[]; missing_indexes=[]
for source_path in scope['scope_paths']:
    ix_path=WORK/'Knowledge/Source-Symbols'/Path(source_path+'.json')
    if not ix_path.is_file(): missing_indexes.append(source_path); continue
    ix=json.loads(ix_path.read_text(encoding='utf-8'))
    if ix.get('baseline')!=BASELINE or ix.get('path')!=source_path: raise SystemExit(f'index identity mismatch: {source_path}')
    source=ROOT/source_path
    if not source.is_file(): hash_errors.append({'path':source_path,'error':'missing source'}); continue
    digest=hashlib.sha256(source.read_bytes()).hexdigest()
    if digest!=ix.get('worktree_sha256'): hash_errors.append({'path':source_path,'expected':ix.get('worktree_sha256'),'actual':digest})
    for sym in ix['symbols']:
        if not sym.get('isCallable'): continue
        sid=sym['stable_id']
        if sid in universe: raise SystemExit(f'duplicate stable callable ID in source universe: {sid}')
        universe[sid]={'stable_id':sid,'path':source_path,'signature':sym.get('qualifiedSymbol') or sym.get('name') or sym.get('id'),'kind':sym.get('kind'),'span':[sym.get('startLine'),sym.get('startColumn'),sym.get('endLine'),sym.get('endColumn')],'source_git_blob_sha256':ix.get('git_blob_sha256'),'source_worktree_sha256':digest}

receipt_records=defaultdict(list); receipt_files=0
for root in [WORK/'Knowledge/Reviews/financial_integrity',WORK/'Knowledge/Reviews/financial_coordinator']:
    if not root.exists(): continue
    for rp in root.rglob('*.json'):
        try: d=json.loads(rp.read_text(encoding='utf-8'))
        except Exception as e: raise SystemExit(f'invalid receipt {rp}: {e}')
        receipt_files+=1
        if d.get('baseline')!=BASELINE: continue
        for s in d.get('symbols',[]):
            sid=s.get('stable_id')
            if sid: receipt_records[sid].append({'receipt_path':str(rp).replace('\\','/'),'reviewer':d.get('reviewer'),'file_review_status':d.get('file_review_status'),'status':s.get('status'),'reasoning_nonempty':bool(str(s.get('reasoning','')).strip()),'source_git_blob_sha256':d.get('source_git_blob_sha256'),'worktree_sha256':d.get('worktree_sha256'),'span':s.get('span')})

outside=sorted(set(receipt_records)-set(universe))
ids=set(universe)
missing_authority=ids-set(receipt_records)
if missing_indexes or hash_errors: raise SystemExit(json.dumps({'missing_indexes':missing_indexes,'hash_errors':hash_errors},indent=2))
if len(scope['scope_paths'])!=327: raise SystemExit(f'scope paths expected 327, got {len(scope["scope_paths"])}')
if len(universe)!=8804: raise SystemExit(f'callable universe expected 8804, got {len(universe)}')
if len(set(receipt_records)&ids)!=7602: raise SystemExit(f'author receipt union changed: {len(set(receipt_records)&ids)}')
if len(indexed)!=4253 or len(indexed-set(receipt_records)): raise SystemExit('indexed 4253 crosswalk no longer matches current receipt IDs')
if len(missing_authority)!=1202: raise SystemExit(f'authority pending changed: {len(missing_authority)}')
if not set(residual).issubset(missing_authority): raise SystemExit('residual queue is not contained in missing authority IDs')
if not set(continuation).issubset(missing_authority): raise SystemExit('continuation manifest is not contained in missing authority IDs')
if set(continuation)&set(residual): raise SystemExit('continuation and residual IDs overlap')
if set(continuation)|set(residual)!=missing_authority: raise SystemExit('continuation + residual sets do not partition authority pending IDs')

entries=[]; statuses=Counter(); receipt_reasoning=Counter()
for sid in sorted(universe):
    row=universe[sid]; rr=receipt_records.get(sid,[])
    if rr:
        row['evidence_class']='AUTHOR_RECEIPT_PRESENT_REASONING_QUALITY_UNGRADED'
        row['receipt_records']=rr
        row['in_2026_10_17_indexed_analyzed_ids']=sid in indexed
        row['reasoning_text_nonempty']=any(x['reasoning_nonempty'] for x in rr)
        row['substantive_analysis_status']='NOT_INDEPENDENTLY_GRADED_BY_THIS_LEDGER'
    elif sid in residual:
        row['evidence_class']='RESIDUAL_213_DISPOSITION_PRESENT_DEPTH_NOT_CERTIFIED'
        row['residual_disposition_record']=residual[sid]
        row['substantive_analysis_status']='DISPOSITION_ONLY_OR_TARGETED_REVIEW; QUALITY_GATE_OPEN'
    elif sid in continuation:
        row['evidence_class']='CONTINUATION_HANDOFF_IDENTITY_LINKED_DEPTH_UNGRADED'
        row['continuation_evidence_pointer']=continuation[sid]
        row['substantive_analysis_status']='HANDOFF_MEMBERSHIP_LINKED; PER_ID_REASONING_DEPTH_AND_ACCURACY_UNGRADED'
    else:
        row['evidence_class']='NO_CURRENT_PER_ID_REVIEW_EVIDENCE_MAPPED'
        row['substantive_analysis_status']='UNKNOWN; NO_EXACT_PER_ID_CONTINUATION_OR_AUTHOR_RECEIPT_EVIDENCE'
    statuses[row['evidence_class']]+=1
    if rr: receipt_reasoning['nonempty' if row['reasoning_text_nonempty'] else 'empty']+=1
    entries.append(row)

out={'schema_version':1,'artifact_kind':'financial_per_callable_evidence_class_ledger','generated_by':'Verification-Tools/build_financial_per_callable_evidence_ledger.py','baseline':BASELINE,'generated_note':'Read-only identity/evidence mapping. This is not a source audit and does not upgrade any callable to substantively reviewed.','scope':{'files':len(scope['scope_paths']),'callable_ids':len(universe),'source_index_baseline':BASELINE},'current_receipts':{'receipt_files_scanned':receipt_files,'unique_callable_ids_in_author_roots':len(set(receipt_records)&ids),'indexed_4253_identity_matches':len(indexed),'indexed_ids_with_receipt':len(indexed & set(receipt_records)),'reasoning_text_nonempty_counts_for_union':dict(receipt_reasoning),'out_of_scope_receipt_ids':len(outside)},'continuation_manifest':{'path':'Codex Work/Sub-Agents/Handoffs/financial-integrity-continuation-receipt-manifest-reconciliation.json','unique_ids':len(continuation),'manifest_sha256':hashlib.sha256(continuation_path.read_bytes()).hexdigest(),'identity_join':'PASS; 989 exact IDs join the authority-pending set with handoff path and line pointers'},'evidence_class_counts':dict(statuses),'historical_crosswalk':{'manual_category_total':4102,'indexed_reasoning_receipt_ids':4253,'difference':151,'per_id_category_mapping':'UNAVAILABLE; do not infer category membership'},'remaining_reconciliation':{'continuation_ids_identity_linked_depth_ungraded':len(continuation),'residual_213_ids':len(residual),'current_author_receipt_union_missing_ids':len(missing_authority),'genuine_substantive_review_count':'UNKNOWN','exact_pending_substantive_queue':'UNKNOWN'},'quality_limits':['Nonempty reasoning text is not a depth or accuracy judgment.','Receipt identity/status is not independent verification.','Continuation handoff membership is identity linkage, not uniform reasoning depth.','Residual dispositions are not automatically substantive function reviews.','Historical 4,102 method-category totals have no per-ID mapping.','No runtime tests, database activity, source modification, or Git publication was performed.'],'entries':entries}
path=WORK/'Deep-Audit/Database-and-Finance/financial-per-callable-evidence-class-ledger.json'
raw=json.dumps(out,ensure_ascii=False,indent=2)+'\n'
fd,tmp=tempfile.mkstemp(prefix=path.name+'.',suffix='.tmp',dir=path.parent)
with os.fdopen(fd,'w',encoding='utf-8',newline='\n') as f: f.write(raw); f.flush(); os.fsync(f.fileno())
os.replace(tmp,path)
print(json.dumps({'output':str(path),'files':len(scope['scope_paths']),'callables':len(universe),'receipts':len(set(receipt_records)&ids),'indexed_4253':len(indexed),'evidence_class_counts':dict(statuses),'hash_errors':len(hash_errors),'outside_receipt_ids':len(outside)},ensure_ascii=False))

