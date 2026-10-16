"""Parent-only recovery snapshot. Preserve prior state and never touch application/runtime."""
from knowledge_store import *
import subprocess
REFERENCE='08f1fadc9b31d6cd4a90fe7b0db5086dfd511471'
facts=json.loads((WORK/'Deep-Audit/recovery-after-08f1fad.json').read_text(encoding='utf8'))
coverage=json.loads((WORK/'Knowledge/coverage-index.json').read_text(encoding='utf8'))
def preserve(path):
    path=WORK/path
    if path.exists():
        raw=path.read_bytes();hist=WORK/'Knowledge/History/Recovery'/Path(path.name+'.'+sha256(raw)[:20]+'.json')
        if not hist.exists():atomic_json(hist,{'original_path':str(path.relative_to(WORK)),'original_sha256':sha256(raw),'preservation_reason':'Superseded recovery/progress state, not discarded','content':json.loads(raw.decode('utf-8-sig'))})
def git(*a):return subprocess.check_output(['git',*a],cwd=ROOT).decode().rstrip()
assert git('branch','--show-current')=='main'
assert not git('diff','--name-only',BASELINE,'--','.',':(exclude)Codex Work/**')
delta=git('diff','--name-only',REFERENCE,'--','Codex Work').splitlines()
untracked=git('ls-files','--others','--exclude-standard','--','Codex Work').splitlines()
all_changed=sorted(set(delta+untracked))
preserve('Deep-Audit/audit-progress.json')
old=json.loads((WORK/'Deep-Audit/audit-progress.json').read_text(encoding='utf8'))
old.update(last_updated_utc=now(),current_head=git('rev-parse','HEAD'),file_review_receipts=610,files_without_full_review=80,
           streams=facts['scopes'],last_completed_task='Recovered on-disk work, verified baseline and stable knowledge receipts; publishing safe checkpoint',
           current_interruption='Original agents recovered and holding writes for parent publication; audit incomplete',
           functions_actually_reviewed='743 author-asserted exact stable callable reasoning receipts; 130 flagged for depth challenge. Global independently certified function total UNKNOWN.',
           strict_stable_callable_receipts=743,strict_remaining_indexed_callable_ids=10910,
           source_symbol_quality_challenge='Codex Work/Knowledge/Quality-Reviews/printing-generic-route-reasoning.json',
           interrupted_tasks=['48 financial files','14 UI files','18 parent-owned docs/config files','130 route symbol quality challenges','Other10910 indexed callable IDs without structured receipts','10 reported non-C# functions outside Roslyn map','SEC001 full defensive boundary matrix','Dependency/advisory/SBOM','Visual states and physical limitations','Performance/hardware','Final category synthesis and independent checks'],
           findings='Canonical ordinary audit registry Deep-Audit/MASTER_FINDINGS.json preserves42 original records; final field quality/deduplication not complete. No canonical plugin scan exists.',
           publication={'authorization':'Latest explicit user authorization for safe Codex Work normal commit/push only','state':'VERIFIED_SCOPE_PENDING_FINAL_STAGED_CONTENT_CHECK'})
atomic_json(WORK/'Deep-Audit/audit-progress.json',old)
preserve('Sub-Agents/Agent-Registry/agent-state.json')
registry=json.loads((WORK/'Sub-Agents/Agent-Registry/agent-state.json').read_text(encoding='utf8'))
for a in registry['agents']:
    name=a['name'].split('/')[-1];file_count={'financial_integrity':279,'ui_visual':260,'printing_workflows':71}[name]
    remaining={'financial_integrity':48,'ui_visual':14,'printing_workflows':0}[name]
    a.update(status='INTERRUPTED',last_turn='SAME_ORIGINAL_SESSION_RECOVERED; HOLD_FOR_PARENT_PUBLICATION',saved_complete_files=file_count,remaining_files=remaining,
             exact_callable_receipts=coverage['reviewers'][name]['analyzed_callables'],
             checkpoint='Codex Work/Sub-Agents/Checkpoints/'+name+'.json',
             remaining='Resume own remaining files/symbols; printing also130 reasoning-quality challenges and10 non-C# mapping gaps',
             functions_reviewed='See exact stable receipts; depth not independently certified by schema')
registry['latest_recovery_utc']=now();registry['replacement_agents']=[]
atomic_json(WORK/'Sub-Agents/Agent-Registry/agent-state.json',registry)
atomic_json(WORK/'Conversation-Recovery/Recovery-After-08f1fad.json',{'at_utc':now(),'reference_publication':REFERENCE,'observed_head':git('rev-parse','HEAD'),
    'accessible_evidence':'Actual local files, Git history, agent checkpoints and same original session recovery replies. User reports a hidden response; missing content was not fabricated.',
    'source_baseline':BASELINE,'baseline_hash_mismatches':facts['hash_mismatches'],'all_changed_paths':all_changed,
    'complete_file_receipts':610,'previous_published_file_receipts':564,'new_file_receipts':46,
    'stable_callable_receipts':743,'stable_quality_flagged_ids':coverage['callables_flagged_for_repeated_reasoning'],'independently_certified_function_total':'UNKNOWN',
    'unchanged_finding_record_count':42,'new_persisted_evidence':['Stable source IDs and743 reasoning receipts','21 SEC001 manually traced route/branch cases','10 non-C# manually reasoned function gaps','Canonical lossless registry','Parent130-symbol quality challenge'],
    'agent_unsaved_reasoning':'All original agents report none after bounded recovery. Financial Treasury test legacy ledger/report gap was reconciled before HOLD.',
    'database_runtime_safety':'No DB/migration/seed/session/backup/print operations intentionally executed during recovery. Active runtime data was not probed or copied; byte-for-byte DB state is not asserted.',
    'plugin_state':'Prior launch failed before scan. No fabricated scan ID/result; no bypass attempt or requirement that ordinary review needs Daybreak.'})
handoff=WORK/'Conversation-Recovery/RESUME-HANDOFF.md'
t=handoff.read_text(encoding='utf8')
marker='# CURRENT VERIFIED RECOVERY — 2026-10-16 after 08f1fad\n'
if not t.startswith(marker):
    header=marker+'''\nThis top section supersedes older counts/instructions retained below as historical evidence. HEAD/reference publication08f1fadc9b31d6cd4a90fe7b0db5086dfd511471; source baselinec35816ecae49633674775dd32061c6ee92f94aaf remains hash-identical. Latest user explicitly authorizes normal safe Codex Work commit/push, no application changes or force/history rewrite.\n\nCurrent on-disk receipts:610 files,80 remaining(48financial,14UI,18parent). Knowledge743 exact stable callable author receipts:317financial,38UI,388printing;10910 indexed IDs remain without such receipts. Parent flagged130 route receipts in17 repeated-reasoning groups for deeper validation. Do not call743 independently approved function audits. Legacy507 heading matches stay separate/unpromoted. Ten non-C# functions have saved manual reasoning but no parent stable map yet.\n\nCanonical registry: Deep-Audit/MASTER_FINDINGS.json,42 preserved source finding/candidate records. SEC00121-case matrix saved in Sub-Agents/Results/SEC001-printing-entrypoints.json; full MinimalAPI harness has NOT run, only earlier3 parser/middleware cases. All original agents recovered; no replacement or hidden-context claim. Their files/checkpoints are current; all holding writes for publication. Financial Treasury unsaved-ledger gap was reconciled.\n\nNext: finish safe publication checks and normal commit/push; then release original agents to remaining assignments, require atomic checkpoints, address exact130-symbol quality queue, map10 non-C# functions and continue SEC001 defensive boundary verification without live sessions/DB. Parent retains dependency/SBOM, performance,18 parent files, canonical merges and global validation. Never run initial reset generators.\n\nDetailed recovery: Recovery-After-08f1fad.json. Stable schema/coverage: Knowledge/README.md and Knowledge/coverage-index.json (paths relative to Codex Work). No database or printer operations were performed.\n\n---\n# HISTORICAL HANDOFFS — retained verbatim\n\n'''
    handoff.write_text(header+t,encoding='utf8')
report=WORK/'Deep-Audit/PHASE_PROGRESS_REPORT.md'
t=report.read_text(encoding='utf8')
if not t.startswith('> Latest recovery checkpoint:'):
    notice='''> Latest recovery checkpoint: **610 file reports;80 files remaining**. Persistent stable callable receipts743, with130 awaiting depth-quality challenge; independently certified global function total remainsUNKNOWN. Canonical MASTER_FINDINGS.json now exists with42 preserved records. See [current recovery](../Conversation-Recovery/Recovery-After-08f1fad.json) and [knowledge coverage](../Knowledge/coverage-index.json). The original dated15-phase assessment below is preserved, not silently rewritten.\n\n'''
    report.write_text(notice+t,encoding='utf8')
print(json.dumps({'changed_paths_before_recovery_metadata':len(all_changed),'files':610,'stable_author_receipts':743,'quality_challenged':130,'source_changes':0}))
