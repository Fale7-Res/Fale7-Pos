"""Refresh coordinator recovery pointers without regenerating specialist evidence."""
from knowledge_store import *
import subprocess
live=json.loads((WORK/'Sub-Agents/Results/financial-current-union-progress.json').read_text(encoding='utf8'))
head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
mirror=json.loads((WORK/'Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.json').read_text(encoding='utf8'))
stamp=now()
state={'at_utc':stamp,'baseline':BASELINE,'git_head':head,'publication':'NO_NEW_COMMIT_OR_PUSH',
 'financial_owner':'/root/financial_integrity','recovery':'ORIGINAL_SESSION_RESUMED_AND_DISK_HANDOFF_ACKNOWLEDGED',
 'financial_union':live['original_union_count'],'financial_total':live['original_total'],
 'financial_remaining':live['original_remaining_count'],'getters':live['additive_getters_completed'],
 'generated_contexts':live['generated_contexts_completed'],
 'financial_queue':'Sub-Agents/Results/financial-current-union-progress.json#/remaining_original_symbols',
 'financial_next':'FIN005 active citation correction with history, then missing AddExpenseHandler IDs (specialist owns live checkpoint). Preserve paused printing residue of broad historical327file scope.',
 'mirror_status':mirror['status'],'root_current_task':'Assess material semantic financial mirrors outside completed exact-body candidate inventory; preserve completed218groups.',
 'last_root_completed':'FIN019 independently challenged/merged; FIN005 model citation correction saved; mirror38current links/source gate and690baseline checks passed in prior batch.',
 'root_next':'Trace financial payment/collection representations across server projection, custody and client state; compare new semantic groups only, source evidence before promotion.',
 'paused_agents':['/root/ui_visual','/root/ui_independent_challenge','/root/printing_workflows','/root/performance_reviewer'],
 'remaining_original_audit':'Conversation-Recovery/PAUSED_AUDIT_BACKLOG.json',
 'completion':'FINANCIAL_OPEN; MIRROR_IDENTIFIED_QUEUE_COMPLETE_BROADER_SCOPE_QUALIFIED; ORIGINAL15PHASES_NOT_COMPLETE'}
atomic_json(WORK/'Sub-Agents/Checkpoints/root-current-two-workstreams.json',state)
nextpath=WORK/'Conversation-Recovery/NEXT_ACTION.md'
old=nextpath.read_text(encoding='utf8')
atomic_json(WORK/'Knowledge/History/Recovery-Snapshots'/('NEXT_ACTION.'+sha256(old.encode())[:20]+'.json'),{'original_document':'Conversation-Recovery/NEXT_ACTION.md','original_sha256':sha256(old.encode()),'original_text':old})
prefix=f'''# CURRENT RESUME — {stamp}

Original financial agent resumed and explicitly acknowledged complete persistent handoff, FIN019 challenge/FIN005 correction and exact queue. No replacement. Actual HEAD `{head}`; do not commit/push.

- Financial author union **{live['original_union_count']}/{live['original_total']}**, remaining **{live['original_remaining_count']}**; getter receipts{live['additive_getters_completed']}, generated contexts{live['generated_contexts_completed']}/95. Live queue `Sub-Agents/Results/financial-current-union-progress.json` overrides all historical counts below.
- Financial next: preserve/correct FIN005 irrelevant old model citation; then missing AddExpenseHandler constructor/Handle and subsequent financial queue. Agent owns all finance reviews and report; no full-file duplicate assignment. Paused printing overlap of broad historical scope must remain explicitly accounted.
- Root next: material semantic-mirror assessment beyond completed218ASTgroups; final report already substantive. Inspect actual callers/state/amount semantics, record exact evidence. Source/hash/group validation is not runtime or whole-repository semantic certification.
- Last root completed: FIN019 independent static challenge and57record canonical merge; FIN005 reference correction saved;38canonical mirror links valid,611changedmdlinks valid with declared historical bases;690baseline matches331exact+359newline-only. These checks are prior timestamps, not a claim all later artifacts were revalidated.
- Exact state: `Sub-Agents/Checkpoints/root-current-two-workstreams.json`. All remaining financial IDs live queue; all remaining15phases/19deliverables `Conversation-Recovery/PAUSED_AUDIT_BACKLOG.json`. All UI/printing/general-performance agents paused. No application/DB/runtime/printing/Git mutation.

--- Historical checkpoint retained below; current live pointers override old numbers ---

'''
atomic_text(nextpath,prefix+old)
for rel in ['Conversation-Recovery/COMPLETE_RECOVERY_STATE.md','Conversation-Recovery/RESUME-HANDOFF.md']:
 p=WORK/rel;atomic_text(p,prefix+p.read_text(encoding='utf8'))
p=WORK/'Sub-Agents/Agent-Registry/agent-state.json';registry=json.loads(p.read_text(encoding='utf8'))
registry['latest_two_workstreams_resume']=state
atomic_json(p,registry)
print(json.dumps({'persisted':'root-current-two-workstreams.json','financial_union':live['original_union_count'],'recovery':state['recovery']}))
