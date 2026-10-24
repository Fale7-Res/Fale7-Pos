"""Persist MANUALLY supplied clone comparison decisions, validate source IDs/hashes, reconcile queue."""
from knowledge_store import *
import collections,sys
batch=json.loads((WORK/sys.argv[1]).read_text(encoding='utf8'))
ledger_path=WORK/'Deep-Audit/Duplication-and-Architecture/clone-review-ledger.json'
ledger=json.loads(ledger_path.read_text(encoding='utf8'));history=WORK/'Knowledge/History/Mirror-Ledger'/('clone-review-ledger.'+sha256(ledger_path.read_bytes())[:20]+'.json')
if not history.exists():atomic_json(history,ledger)
written=[]
for note in batch['comparisons']:
 matches=[g for g in ledger['groups'] if g['group_id'].startswith('clone-'+note['group_prefix'])];assert len(matches)==1,note['group_prefix']
 g=matches[0]
 if not g['status'].startswith('PENDING_'):
  assert g.get('evidence_artifact')==batch['evidence_output'],'Already reviewed group: preserve prior evidence'
  continue
 refs=[]
 for o in g['occurrences']:
  p=o['path'];source=load_symbols(p);assert sha256((ROOT/p).read_bytes())==source['worktree_sha256']
  s=next(s for s in source['symbols'] if s['stable_id']==o['stable_id'])
  assert s['startLine']==o['start_line'] and s['endLine']==o['end_line']
  parents=[x for x in source['symbols'] if x['kind'] in ('MethodDeclaration','ConstructorDeclaration','LocalFunctionStatement') and x['startLine']<=s['startLine'] and x['endLine']>=s['endLine'] and x['stable_id']!=s['stable_id']]
  parent=min(parents,key=lambda x:x['endLine']-x['startLine']) if parents else None
  refs.append({**o,'worktree_sha256':source['worktree_sha256'],'signature':s.get('qualifiedSymbol') or s['name'],'symbol_span':[s['startLine'],s['startColumn'],s['endLine'],s['endColumn']],
   'enclosing_callable':None if parent is None else {'stable_id':parent['stable_id'],'signature':parent.get('qualifiedSymbol') or parent['name'],'lines':[parent['startLine'],parent['endLine']]}})
 for required in ['reasoning','callers_context','financial_security_effects','recommendation','tests']:
  assert note.get(required),required
 g.update(status='MANUALLY_COMPARED_TEST_CONTEXT' if 'TEST' in g['status'] else 'MANUALLY_COMPARED_APPLICATION_CONTEXT',reasoning=note['reasoning'],reviewer='root mirrored-code coordinator',reviewed_at_utc=now(),evidence_artifact=batch['evidence_output'],independent_whole_method_correctness_claim=False)
 written.append({**note,'group_id':g['group_id'],'source_references':refs,'runtime_executed':False,'review_scope':'Actual bodies and described caller/context compared; not full-file/function certification or all runtime callers.'})
if written:
 ledger['at_utc']=now();ledger['status_counts']=dict(collections.Counter(g['status'] for g in ledger['groups']));ledger['next_action']='Continue exact pending clone groups; no completed group restarted. Financial specialist owns financial function queue.'
 atomic_json(ledger_path,ledger)
 atomic_json(WORK/batch['evidence_output'],{'at_utc':now(),'baseline':BASELINE,'reviewer':'root mirrored-code coordinator','batch':batch['name'],'comparisons':written,'limits':'Source comparison, not runtime acceptance, financial function receipt increment, or exhaustive semantic duplication proof. No code/DB/printing/Git changes.'})
status_path=WORK/'Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.json';status=json.loads(status_path.read_text(encoding='utf8'))
manual=[g for g in ledger['groups'] if g['status'].startswith(('MANUALLY_','SOURCE_CHALLENGED_'))]
status.update(at_utc=now(),status='INCOMPLETE',status_counts=ledger['status_counts'],remaining_groups=[g for g in ledger['groups'] if g['status'].startswith('PENDING_')],manually_triaged_or_context_challenged_groups=len(manual),distinct_source_paths_in_those_groups=len({o['path'] for g in manual for o in g['occurrences']}),distinct_callable_ids_in_those_groups=len({o['stable_id'] for g in manual for o in g['occurrences']}),source_paths=sorted({o['path'] for g in manual for o in g['occurrences']}),callable_ids=sorted({o['stable_id'] for g in manual for o in g['occurrences']}))
status['pending_application_groups']=sum(g['status']=='PENDING_MANUAL_APPLICATION_CONTEXT' for g in ledger['groups']);status['pending_test_groups']=sum(g['status']=='PENDING_MANUAL_TEST_FIXTURE_CONTEXT' for g in ledger['groups'])
status.setdefault('comparison_batch_artifacts',[])
if batch['evidence_output'] not in status['comparison_batch_artifacts']:status['comparison_batch_artifacts'].append(batch['evidence_output'])
atomic_json(status_path,status)
md=WORK/'Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.md'
old=md.read_text(encoding='utf8');hist=WORK/'Knowledge/History/Mirror-Ledger'/('mirror-status.'+sha256(old.encode())[:20]+'.json')
if not hist.exists():atomic_json(hist,{'original_path':str(md.relative_to(WORK)).replace('\\','/'),'verbatim_text':old})
summary=f'''# Mirrored / duplicated code audit status

**INCOMPLETE.** Updated UTC: {now()}.

Original Phase11 includes semantic duplication, repeated financial/authorization rules, divergent clients/server, dead code, wrappers, coupling and multiple truth sources. AST candidate accounting does not exhaust that scope.

AST inventory561C#files, bodies>=20tokens,218candidate groups. Identifiers/literals preserved; generated history102groups accounted intentional history, not fresh independent semantic approval.

Actual context/manual group decisions **{len(manual)}**; distinct source paths **{status['distinct_source_paths_in_those_groups']}**, distinct callable IDs **{status['distinct_callable_ids_in_those_groups']}**. These are comparison occurrences, not full-file coverage or additional financial function-review receipts. Some groups are nested/overlapping syntax units.

Pending application groups **{status['pending_application_groups']}**, pending test groups **{status['pending_test_groups']}**. Exact paths, stable IDs, source hashes/spans and pending queue: [status JSON](MIRRORED_CODE_AUDIT_STATUS.json), [clone ledger](Duplication-and-Architecture/clone-review-ledger.json).

## Comparison evidence

Prior42groups preserved:13typed server adapters,6retained request-journal helpers,1currency input helper,22client API bodies. [API evidence](Duplication-and-Architecture/client-api-financial-mirror-review.json), [financial semantic comparison](Duplication-and-Architecture/financial-mirror-comparison-20261018.json), [day authority comparison](Duplication-and-Architecture/business-day-authorization-mirrors.json).

Additional semantic supplements: [Treasury policy](Duplication-and-Architecture/treasury-safe-drop-transfer-comparison.json), [employee payout policy](Duplication-and-Architecture/employee-payout-policy-comparison.json). They do not automatically subtract AST groups; each actual group decision has separate evidence below.

'''
for artifact in status['comparison_batch_artifacts']:
 summary+='- ['+Path(artifact).stem+']('+str(Path(artifact).relative_to('Deep-Audit')).replace('\\','/')+')\n'
summary+='\nNo application refactor, DB/runtime/physical printing or new Git publication. Financial primary owner original /root/financial_integrity; root owns these comparisons. No UI/printing/general-performance specialist resumed. Final CODE_DUPLICATION_AND_ARCHITECTURE.md is not yet certified complete. Short bodies, renamed semantic equivalents and inactive compilation branches remain inventory limitations; all material identified divergences must retain evidence.\n'
atomic_text(md,summary)
checkpoint={'at_utc':now(),'owner':'root mirrored-code coordinator','active_task':'Compare exact remaining application/test clone groups; financial source queue owned original specialist',
 'last_saved_batch':batch['evidence_output'],'last_completed_group':written[-1]['group_id'] if written else None,
 'manual_groups':len(manual),'pending_application':status['pending_application_groups'],'pending_tests':status['pending_test_groups'],
 'exact_remaining_queue':'Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.json','next_group':status['remaining_groups'][0] if status['remaining_groups'] else None,
 'financial_owner':'/root/financial_integrity ORIGINAL successfully resumed, complete handoff acknowledged',
 'financial_latest_queue':'Sub-Agents/Results/financial-current-union-progress.json; original handoff snapshot financial-coordinator-coverage.json',
 'paused_specialists':'All UI/printing/general performance; no new unrelated tasks',
 'all_other_audit_backlog':'Conversation-Recovery/PAUSED_AUDIT_BACKLOG.json','publication':'NO_NEW_COMMIT_OR_PUSH','runtime':'SOURCE_ONLY_NO_APP_OR_DB_CHANGES'}
atomic_json(WORK/'Sub-Agents/Checkpoints/root-mirrored-code.json',checkpoint)
next_path=WORK/'Conversation-Recovery/NEXT_ACTION.md';previous=next_path.read_text(encoding='utf8');marker='\n## Current two-workstream snapshot\n'
if marker in previous:previous=previous.split(marker,1)[0]
atomic_text(next_path,previous+marker+'\nUpdated UTC: '+now()+'\n\nOriginal financial agent resumed and full handoff acknowledged; financial primary owns remaining source and independent challenges. Its live own union: Sub-Agents/Results/financial-current-union-progress.json. Root no full-financial assignment duplication.\n\nRoot last completed batch: '+batch['evidence_output']+f'. Manual/context group decisions{len(manual)}, application pending{status["pending_application_groups"]}, test pending{status["pending_test_groups"]}. Full exact queue and every source occurrence in Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.json. Next operation inspect actual next pending implementation/callers, persist next comparison batch, then substantive canonical duplication report. No false AST=allsemantic completion.\n\nFinancial payroll/day HIGH candidate independent source challenges saved by original reviewer, root merged qualified corroboration; no SQL test. All paused specialists and full15phase/19deliverable backlog preserved. No new Git publication.\n')
print(json.dumps({'new_groups':len(written),'manual_groups':len(manual),'pending_application':status['pending_application_groups'],'pending_tests':status['pending_test_groups']}))
