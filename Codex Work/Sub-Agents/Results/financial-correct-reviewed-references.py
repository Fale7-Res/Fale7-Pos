import sys,json
from pathlib import Path
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()));import knowledge_store as k
p=k.WORK/'Sub-Agents/Handoffs/reverse-date-range-independent-challenge.json';d=json.loads(p.read_text(encoding='utf8'));old=json.loads(json.dumps(d));d['evidence']=[x.replace('DayCount74','DayCount73') for x in d['evidence']]
if d!=old:
 k.atomic_json(k.WORK/'Sub-Agents/Handoffs/History'/('reverse-date-range.'+k.sha256(json.dumps(old,sort_keys=True).encode())[:16]+'.json'),old);d['updated_at_utc']=k.now();k.atomic_json(p,d)
for path in ['src/Falah.Infrastructure/Backup/CreateBackupHandler.cs','src/Falah.Infrastructure/Backup/VerifyBackupHandler.cs']:
 p=k.WORK/'Knowledge/Reviews/financial_integrity'/(path+'.json');d=json.loads(p.read_text(encoding='utf8'))
 for s in d['symbols']:
  if path.endswith('CreateBackupHandler.cs') and s['signature'].split('.')[-1].startswith('RunAsync'):
   s['verified_callers_callees']='Directory49-51; filename56; Requested Save65; native SQL77-79; catch90-99; finalSave101; DTO102.'
  if path.endswith('VerifyBackupHandler.cs') and 'RunAsync(' in s['signature']:
   s['database']='Record read44; native VERIFYONLY52; verification/audit updates and final Save67.'
   s['verified_callers_callees']='File46-47; native SQL52; catch58-64; VerifiedAt65; Save67; ToDto68.'
 k.save_review('financial_integrity',path,d)
path='src/Falah.Infrastructure/Persistence/Entities/PayrollEntities.cs';p=k.WORK/'Knowledge/Reviews/financial_integrity'/(path+'.json');d=json.loads(p.read_text(encoding='utf8'));d['coverage_classification']='SECONDARY_FIELD_SPECIFIC_MANUAL_REVIEW_OF_COORDINATOR_COVERED_ACCESSORS_NOT_NEW_UNION_PROGRESS';d['selection_correction']='Original reviewer accidentally selected52alreadycoveredIDs; genuinefieldreasoningretained as secondary evidence. No union completion increment.';k.save_review('financial_integrity',path,d)
p=k.WORK/'Sub-Agents/Results/financial-new-candidates.json';d=json.loads(p.read_text(encoding='utf8'))
for x in d['candidates']:
 if x['candidate_key']=='SHIFT_CLOSE_APPROVER_ATTRIBUTION_FROM_UNVALIDATED_PRESENTATION':
  x.update(independent_pending=False,status='INDEPENDENTLY_CORROBORATED_PARENT_CANONICAL',canonical_id='FIN017',qualification='Domain Shift.Close requires nonblank authority/reason above threshold; finding narrowed to unvalidated optional presenter attribution, not null-authority close bypass.')
 if x['candidate_key']=='PURCHASE_RECEIPT_ACCEPTS_NEGATIVE_QUANTITY_AND_UNIT_COST':x.update(independent_pending=False,status='INDEPENDENTLY_CORROBORATED_PARENT_CANONICAL',canonical_id='FIN018',qualification='Authorized purchasing actor/day/supplier/item required; source-only double-negative receipt, no SQL write reproduction.')
k.atomic_json(p,d)
cp=k.WORK/'Sub-Agents/Checkpoints/financial_integrity.json';d=json.loads(cp.read_text(encoding='utf8'));d['independent_pending']=[];d['selection_correction']='PayrollEntities52 duplicate root-covered accessors retained as explicit secondary review; union unchanged.';d['next_action']='Select exact unresolved union IDs only: CompleteChecklistItem/SetExceptionStatus/ControlCenterQuery or other current financial queue. Check both shards before each new file; Printing work paused. CashierCashMovement stale parent admission extension requires canonical disposition, not newfindingcount.';d['updated_at_utc']=k.now();k.atomic_json(cp,d)
print('Corrected references/history, reconciled FIN017/018 status and recorded secondary-accessor classification')
