"""Coordinator-only reconciliation; preserves discovery and independent evidence separately."""
from knowledge_store import *
rel='Deep-Audit/Duplication-and-Architecture/delivery-payment-contract-semantic-comparison.json'
challenge_rel='Sub-Agents/Handoffs/delivery-payment-contract-independent-challenge.json'
challenge=json.loads((WORK/challenge_rel).read_text(encoding='utf8'))
for s in challenge['sources']:assert sha256((ROOT/s['path']).read_bytes())==s['worktree_sha256'],s['path']
evidence_path=WORK/rel;data=json.loads(evidence_path.read_text(encoding='utf8'))
atomic_json(WORK/'Knowledge/History/Mirror-Reports'/('SEM004.before-challenge.'+sha256(evidence_path.read_bytes())[:16]+'.json'),data)
data['status']='MANUAL_SEMANTIC_COMPARISON_AND_INDEPENDENT_FINANCIAL_CHALLENGE_COMPLETE_WITH_LIMITS'
data['independent_review']={'artifact':challenge_rel,'sha256':sha256((WORK/challenge_rel).read_bytes()),
 'reviewer':challenge['reviewer'],'source_identity_gate':'12sourcehashesvalid',
 'A':'Static wire-case mismatch corroborated; extends existing FIN019 source reachability, not a new financial finding count.',
 'B':'Prepaid client default mismatch corroborated; server no-reason variance guard intact, editablezero workaround. Retain financial usability/regression followup, no demonstrated doublecollection.'}
data['comparisons'][1]['disposition']='INDEPENDENTLY_SOURCE_CORROBORATED_FIN019_EXTENSION_NO_RENDERED_UI'
data['comparisons'][2]['disposition']='INDEPENDENTLY_SOURCE_CORROBORATED_DEFAULT_MISMATCH_GUARD_INTACT_UI_FOLLOWUP'
atomic_json(evidence_path,data)
parent_path=WORK/'Deep-Audit/Database-and-Finance/parent-financial-findings.json';parent=json.loads(parent_path.read_text(encoding='utf8'))
atomic_json(WORK/'Knowledge/History/parent-financial-findings'/('before-SEM004.'+sha256(parent_path.read_bytes())[:16]+'.json'),parent)
finding=next(f for f in parent['findings'] if f['id']=='FIN-019')
finding.setdefault('related_source_extensions',[]).append({'id':'SEM-MIRROR-004','evidence':rel,'independent_challenge':challenge_rel,
 'disposition':'Delivery board canonical lowercase pending is not recognized by uppercase client predicate; normal UI-source path not limited to hypothetical raw API, still no actual UI/HTTP runtime test.',
 'financial_count_delta':0,'guarded_prepaid_default':'Related independent default/usability mismatch retained in backlog; not evidence of silent server overcollection.'})
finding['sources'].extend(s for s in data['sources'] if not any(t['path']==s['path'] for t in finding['sources']))
parent['at_utc']=now();atomic_json(parent_path,parent)
statuspath=WORK/'Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.json';status=json.loads(statuspath.read_text(encoding='utf8'))
status['latest_semantic_comparison']['status']=data['status'];status['latest_semantic_comparison']['independent_challenge']=challenge_rel
status['latest_semantic_comparison']['new_canonical_findings']=0;status['updated_at_utc']=now();atomic_json(statuspath,status)
p=WORK/'Deep-Audit/CODE_DUPLICATION_AND_ARCHITECTURE.md';text=p.read_text(encoding='utf8')
text+='\nIndependent followup: [original financial specialist challenge](../Sub-Agents/Handoffs/delivery-payment-contract-independent-challenge.json) corroborated the wire-case mismatch as a FIN019 source extension and the prepaid default mismatch with the server variance guard intact. The latter remains a usability/regression followup; no new canonical defect ID or real double-collection claim. Case comparisons inside translated SQL predicates elsewhere are separately provider/collation dependent; the in-memory client string defect must not be generalized to those queries without database collation evidence.\n'
atomic_text(p,text)
backlogpath=WORK/'Conversation-Recovery/PAUSED_AUDIT_BACKLOG.json';backlog=json.loads(backlogpath.read_text(encoding='utf8'))
backlog.setdefault('new_preserved_cross_stream_followups',[]).append({'id':'SEM004-PREPAID-DEFAULT',
 'evidence':rel,'independent_challenge':challenge_rel,'owner_when_resumed':'UI/financial contract followup; no specialist automatically resumed',
 'status':'SOURCE_SUPPORTED_USABILITY_DEFAULT_MISMATCH_GUARD_INTACT; RUNTIME_NOT_EXECUTED',
 'question':'Prepaid DeliveryDto gross10000 but handler collectible0; initialize dialog from authoritative collectible fact, preserving gross display and server variance controls.',
 'does_not_block_other_financial_source_review':True})
atomic_json(backlogpath,backlog)
print(json.dumps({'reconciled':'SEM-MIRROR-004','new_finding_count':0,'independent_source_hashes':len(challenge['sources'])}))
