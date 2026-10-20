"""Map owner-supplied external-review summaries, without claiming retrieval of its hidden report."""
from knowledge_store import *
master=json.loads((WORK/'Deep-Audit/MASTER_FINDINGS.json').read_text(encoding='utf8'))
byid={f['id']:f for f in master['findings']}
rows=[
 ('Payroll transition Paid without financial settlement',['FIN-006'],'Existing source-backed finding; no new ID.'),
 ('Payroll pay/cancel concurrency',['FIN-CAND-PAYROLL-001'],'Existing independently source-challenged stale-status schedule; actual SQL race unexecuted.'),
 ('Negative purchase quantity and unit cost',['FIN-018'],'Existing authorized both-negative receipt trace, not arbitrary unauthorized invoice access.'),
 ('Checkout after shift closure',['FIN-015'],'Existing independently source-challenged pretransaction open-shift admission window.'),
 ('Backup restore outcome classification',['FIN-014'],'Existing source trace for replace succeeded then MULTI_USER failure; destructive restore not run.'),
 ('Authorization identity mismatch',['SEC-001'],'Existing independently traced/bounded binding finding. General security work remains paused; external summary is not a fresh exploit proof.'),
 ('Employee data exposure',['SEC-003'],'Existing source-supported conditional disclosure finding, owner policy/deployment qualifications retained; security work paused.'),
 ('Printing verification-script safety',[],'Insufficient original report/script identity supplied. Preserve as unverified paused printing/tool-safety lead; do not run script or restart printing investigation.')]
mapped=[]
for lead,ids,decision in rows:
 refs=[]
 for fid in ids:
  for s in byid[fid]['source_references']:
   assert sha256((ROOT/s['path']).read_bytes())==s['worktree_sha256'],s['path']
   refs.append(s)
 mapped.append({'owner_supplied_summary':lead,'canonical_ids':ids,'decision':decision,
                'current_source_identity_references':refs,'new_source_review_claimed':False})
payload={'at_utc':now(),'baseline':BASELINE,'origin':'Latest owner continuation message, independent GitHub review summary only',
 'retrieval_limit':'Original external full report/hidden reasoning not provided or retrieved. This is deduplication against existing evidence, not independent endorsement of all external assertions.',
 'leads':mapped,'new_finding_count':0,'new_function_receipts':0,'runtime_tests_executed':False}
relative='Conversation-Recovery/EXTERNAL_GITHUB_REVIEW_LEAD_RECONCILIATION.json';atomic_json(WORK/relative,payload)
p=WORK/'Conversation-Recovery/PAUSED_AUDIT_BACKLOG.json';b=json.loads(p.read_text(encoding='utf8'))
b['external_review_lead_reconciliation']=relative
b.setdefault('new_preserved_cross_stream_followups',[]).append({'id':'EXTERNAL-PRINTING-TOOL-SAFETY-UNRESOLVED',
 'owner_when_resumed':'Paused printing/tool-safety review','status':'UNVERIFIED_LEAD_NO_SCRIPT_IDENTIFIED',
 'evidence':relative,'next_action':'Recover exact external review report/script identity and inspect read-only before any execution. No printing reviewer resumed now.'})
atomic_json(p,b)
print(json.dumps({'mapped_leads':len(mapped),'existing_finding_links':sum(len(r['canonical_ids']) for r in mapped),'new_findings':0}))
