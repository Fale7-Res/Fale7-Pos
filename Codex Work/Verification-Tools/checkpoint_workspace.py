"""HISTORICAL INITIAL RECOVERY GENERATOR. Do not rerun: it resets later checkpoints.
Preserved for provenance only; use assess_progress.py for current read-only evidence snapshots.
"""
from pathlib import Path
import hashlib,json,subprocess,collections,datetime
ROOT=Path(__file__).resolve().parents[2]; W=ROOT/'Codex Work'
BASE='c35816ecae49633674775dd32061c6ee92f94aaf'
def git(*args): return subprocess.check_output(['git',*args],cwd=ROOT)
def digest(data):return hashlib.sha256(data).hexdigest()
def save(name,obj):
 p=W/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(obj,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def doc(name,text):
 p=W/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(text.strip()+'\n',encoding='utf-8')
assert git('rev-parse','HEAD').decode().strip()==BASE
assert git('branch','--show-current').decode().strip()=='main'
assert not git('diff','--name-only') and not git('diff','--cached','--name-only')
paths=git('ls-files','-z').decode().rstrip('\0').split('\0')
manifest=json.loads((W/'Workspace-Manifest.json').read_text(encoding='utf-8'))
verified=[]
for item in manifest['entries']:
 if not item.get('destination_path'):continue
 p=ROOT/item['destination_path'];actual=digest(p.read_bytes())
 assert actual==item['destination_sha256'],str(p)
 if item['migration_status']=='EXACT_COPY_VERIFIED':
  assert digest((ROOT/item['original_path']).read_bytes())==actual
 verified.append(str(p.relative_to(ROOT)))
rows=[];agents=[];all_reviewed={}
for name,key in [('financial','records'),('ui','files'),('printing','files')]:
 ledger=json.loads((W/f'Sub-Agents/Results/{name}-ledger.json').read_text(encoding='utf-8'))
 records=ledger[key];counts=collections.Counter(r['status'] for r in records)
 if name=='financial':
  counts['NOT_ANALYZED']=ledger['remaining_count']
 for r in records:
  p=ROOT/r['path'];raw=p.read_bytes();blob=git('show',f'{BASE}:{r["path"]}')
  assert raw.replace(b'\r\n',b'\n')==blob.replace(b'\r\n',b'\n'),r['path']
  expected=r.get('worktree_sha256',r.get('working_sha256',r.get('sha256')))
  assert digest(raw)==expected,r['path']
  all_reviewed[r['path']]=r['status']
 rows.append({'stream':name,'scope':ledger.get('scope_count',len(records)),'statuses':dict(counts)})
 agents.append({'name':{'financial':'/root/financial_integrity','ui':'/root/ui_visual','printing':'/root/printing_workflows'}[name],
 'status':'INTERRUPTED','same_session_recovery': 'SUCCEEDED_CHECKPOINT_RETURNED_HOLD' if name!='printing' else 'RETRY_FAILED_AGENT_THREAD_LIMIT',
 'ledger':f'Codex Work/Sub-Agents/Results/{name}-ledger.json','counts':dict(counts),
 'remaining':'Continue NOT_ANALYZED and PARTIAL records; independent challenge outstanding; no function-completeness claim',
 'dependencies':['verified baseline hashes','parent independent validation'],
 'session_identity_limit':'Canonical collaboration agent name is accessible; no hidden internal session identifier claimed.'})
hashes=[]
for p in paths:
 raw=(ROOT/p).read_bytes();blob=git('show',f'{BASE}:{p}')
 assert raw.replace(b'\r\n',b'\n')==blob.replace(b'\r\n',b'\n'),p
 hashes.append({'path':p,'worktree_sha256':digest(raw),'git_blob_sha256':digest(blob),'comparison':'BYTE_EQUAL' if raw==blob else 'CRLF_LF_EQUIVALENT','review_status':all_reviewed.get(p,'NOT_ANALYZED')})
save('Deep-Audit/Source-Inventory/verified-baseline-hashes.json',{'baseline':BASE,'files':hashes})
save('Sub-Agents/Agent-Registry/agent-state.json',{'baseline':BASE,'agents':agents,'replacement_agents':[],'security_deep_scan':{'status':'NOT_STARTED','scan_id':None}})
save('Deep-Audit/audit-progress.json',{'baseline':BASE,'branch':'main','status':'IN_PROGRESS','tracked_baseline_files':len(paths),'streams':rows,
 'review_receipts':'Recovered agent source-review claims verified against current file hashes; not all independently re-reviewed.',
 'functions_actually_reviewed':'UNKNOWN; precise symbol-to-review reconciliation pending. Indexing does not count.',
 'last_completed_task':'Copy-only consolidation and source/hash validation',
 'interrupted_tasks':['Financial remaining source review','UI remaining source review and unsaved candidate writeups','Server Program.cs remaining handlers','Roslyn BOM correction','Security deep scan','Dependency advisories','Performance and hardware','Independent finding challenges'],
 'inventory_caveat':'Recovered Roslyn checkpoint has a BOM-removal bug. Do not trust its parser failures or declaration count. Corrected inventory pending.',
 'tests':{'domain':'ABORTED_TESTHOST_CONNECTION_TIMEOUT','executed':0,'runtime_financial_tests_during_audit':0},
 'visual_observations':'No expense defect screenshot recovered; source evidence and user description only. DPI matrix NOT_TESTED.',
 'findings':'See recovered financial/printing findings; unsaved UI candidates are not final. No canonical security scan result exists.'})
doc('README.md','''# Fale7C# — persistent Codex workspace

Audit baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`, existing `main`. Audit is **IN PROGRESS**. No application fixes are authorized.

- [Resume handoff](Conversation-Recovery/RESUME-HANDOFF.md)
- [Requirements and decisions](Conversation-Recovery/Requirements-and-Decisions.md)
- [Recovery limits](Conversation-Recovery/Recovery-Limitations.md)
- [Audit progress](Deep-Audit/audit-progress.json)
- [Agent state](Sub-Agents/Agent-Registry/agent-state.json)
- [Relocation manifest](Workspace-Manifest.json)
- [Verified baseline hashes](Deep-Audit/Source-Inventory/verified-baseline-hashes.json)

Recovered reports retain their original bytes and historical paths for provenance. Original `artifacts/` paths still exist at the repository root; resolve them from there, not relative to a report. New navigation uses this workspace. Reports under Recovered-Checkpoint and Recovered-Evidence are historical, not validated current inventories. Runtime script copies ending `.source.txt` are reference material; live helpers remain under ignored `artifacts/local-runtime/`.

No database files, printer images, raw runtime logs, build binaries or credentials belong here. No commit or push is approved. Existing artifacts ignore rule remains unchanged.
''')
doc('Conversation-Recovery/Requirements-and-Decisions.md','''# Requirements and decisions

The latest request supersedes the earlier instruction to defer auditing: perform the complete audit now, with no implementation. Preserve baseline main and all existing source. Do not create branches, rewrite history, commit or push without approval of exact files.

Scope: all 690 original tracked files, individual reports, actual function coverage, security and malicious behavior, direct/transitive dependencies, printing and order lifecycle, all relevant UI states, database/financial integrity, performance/hardware, duplication and architecture, immediate and future recommendations. Evidence must identify source path, symbol, lines, severity, confidence, solution and verification. Windows 7 is informational; prioritize Windows 10 x64 compatible architecture.

The user approved only a separate LocalDB Fale7CSharpDev and Fale7CSharp_Test under artifacts/local-runtime/data during the earlier runtime phase. Preserve it. Never touch POS2016 or other existing databases. This audit authorizes no migrations, seeders, financial writes, schema changes, system changes or physical printing. Additional state-changing tests require explicit approval.

Printing requirements include branch pickup kitchen ticket before payment and final receipt after payment; delivery kitchen/delivery receipts; missing-printer detection, faithful paper preview, Arabic clipping, retries and duplicate printing. The reported expense-category contrast defect needs source and visual evidence distinguished.

The original detailed audit attachment is preserved as Original-Deep-Audit-Request.md. Current recovery requirements are summarized here and in RESUME-HANDOFF.md; inaccessible historical messages are not claimed recovered.
''')
doc('Conversation-Recovery/Progress-and-History.md','''# Verified recovery history

Visible conversation and saved reports describe architecture discovery, Release solution build, isolated LocalDB initialization, real backend and client startup, authenticated API checks and file-only printing. These are historical runtime observations, not fresh audit executions. The existing backend is Falah.Server; no replacement backend is needed. CashierWinUI is the active cashier; CallCenterWinUI is optional. Domain/Application/Infrastructure implement business contracts and persistence. PrintAgent polls server jobs and transports/render tickets.

Earlier runtime work wrote one assistant test sale (EGP10), opened a test business day/shift and requested a reprint. The owner later used the isolated environment. Audit recovery performs no new financial operation. Do not assume later test records belong to the assistant.

Recovered 250 per-file reports: financial 71 complete static files; UI 106 source-reviewed plus 2 partial; printing 70 static plus 1 partial. This is 247 reported full-file source reviews and 3 partial, not a completed audit. Another 440 files remain without completed review, including 18 parent-owned files. Review receipts and current source hashes agree. Independent review and function-level reconciliation remain incomplete.

The Domain test attempt aborted before execution because testhost failed to connect within 90 seconds: executed 0, passed 0, failed test cases 0. This is a failed test infrastructure step, never a passing test run.

Financial and UI original agents returned saved checkpoints. Printing recovery hit an agent thread limit; no duplicate agent was created. All recovered files were copied, never moved. Thirteen artifact-root directories of unverified prior origin were preserved unchanged.
''')
doc('Conversation-Recovery/Recovery-Limitations.md','''# Recovery limitations

Available: current visible conversation, user attachment, saved audit reports/ledgers/tools, Git and file hashes, original collaboration agent names and financial/UI checkpoint replies. Unavailable hidden agent context and missing historical messages have not been reconstructed as facts.

UI finding JSON/summary were not saved before interruption. Candidate descriptions reconstructed from accessible agent replies must be revalidated before final promotion. The expense screenshot is described but no image recovered. No fresh screenshot/DPI matrix or physical paper test exists.

Recovered Roslyn inventories contain a culture-sensitive UTF-8 BOM-removal bug; counts and parser failures are untrusted until regenerated. Semantic probe is not a full MSBuildWorkspace compilation and generated sources were not executed. No full semantic coverage claim is valid.

No completed security plugin scan, vulnerability advisory inventory, exhaustive dependency security verdict or performance hardware measurement was recovered. Historical process IDs may be stale. Full per-function and independent per-file verification remain pending.

Safe report copies were hash-verified. Secret/PII review before publication is a separate pending gate; no publication has been approved.
''')
doc('Conversation-Recovery/RESUME-HANDOFF.md','''# Resume handoff

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`, main. Re-run Git and verified-baseline-hashes checks before relying on findings. Application source unchanged; only Codex Work is newly untracked. Audit only, no application fixes or state-changing tests.

Start with ../Deep-Audit/audit-progress.json and ../Sub-Agents/Agent-Registry/agent-state.json. All original artifacts remain in place. Workspace-Manifest.json records copy provenance, exclusions and SHA-256 hashes. Runtime data stays under ignored artifacts/local-runtime; do not move or read/export its private logs, databases or ticket previews.

Next exact actions:

1. Finish independent workspace privacy/link/Git tracking validation; retain exact copies and provide migrated link navigation. Keep artifacts ignored. No gitignore edit is currently necessary.
2. Correct only the audit helper BOM check to text.Length > 0 && text[0] == '\\uFEFF'. Create working helper under Verification-Tools/RoslynInventory; preserve original broken snapshot. Regenerate inventory with explicit semantic limitations and reconcile review spans against declarations.
3. Resume financial_integrity and ui_visual SAME collaboration sessions with new workspace output paths; do not rerun their initial report generators because they reset progress. Printing session recovery previously failed once with thread limit; bounded retry after availability changes, replacement only if necessary with explicit provenance.
4. Financial remaining: 256 files; UI remaining:166 plus 2 partial; printing: remaining Program.cs ranges. Parent:18 baseline docs/config files, dependencies/security/performance and independent challenges.
5. Revalidate financial FIN-001 through FIN-006 static confirmations; FIN-007 through FIN-009 candidates. Challenge PRT-001 duplicate claiming and PRT-005 printer verification script hazards. Reconstruct unsaved UI candidates from handoff and actual code, never treat as saved findings.
6. Security deep scan NOT STARTED: use the read deep-security-scan skill, advisory Daybreak once then coordinator once, preserve scan ID/claim. Plugin canonical artifacts use plugin storage, not hand-authored JSON impersonation. Complete once only after canonical evidence exists.
7. Keep progress and handoff current. Before commit/push, show exact file list and obtain approval. Do not mark complete until file/function/category and independent verification gates are genuinely met.
''')
doc('Sub-Agents/Handoffs/ui-recovered-candidates.md','''# UI checkpoint candidates — not final findings

Source: accessible original ui_visual recovery reply. Final findings JSON and UI summary were not saved. Recheck actual source and precise evidence before promotion. No screenshot or runtime reproduction claimed.

- UI-001: memory-only pending delegates for cashier financial operations/Treasury recovery.
- UI-002: CashierView expense buttons lines313–340 and CheckoutView payment buttons lines31–42 hardcode WhiteBrush with pale dark-theme TextPrimary; visual verification pending.
- UI-003: pickup kitchen-before-payment stage absent.
- UI-004: ThemeResources.Get dictionary traversal and converter invalidation.
- UI-005: electronic recovery with LastCheckoutResult null may disable confirmation/cancel and editing.
- UI-006: pickup schedule omitted by ResetDelivery.
- UI-007: Call Center asynchronous customer lookup may overwrite newer input.
- UI-009: PrintingViewModel exposes four of six server routes.
- UI-010: keyboard guards may omit operational modals.
- UI-011: ClearResolvedPrint may leave definitively rejected manual reprint on disk.
- UI-012: Call Center receipt reprint defaults IncludeKitchenRoutes true; peer static confirmation, parent challenge pending.
- UI-013: legacy WPF change parsing may mix currency units.
- BackupViewModel Completed versus Succeeded status mismatch requires handler check.

Recover these candidates with symbols, paths, line numbers, confidence, severity, counterarguments and proposed tests. Do not invent missing UI-008 or assume numbering is exhaustive.
''')
save('Build-and-Runtime/domain-test-attempt-summary.json',{'result':'ABORTED','reason':'Testhost connection timeout after 90 seconds','executed':0,'passed':0,'failed_test_cases':0,'infrastructure_step':'FAILED','raw_trx':'Retained only under ignored artifacts/deep-audit-c35816e-20261016/evidence/domain-tests; not copied because machine/user metadata.'})
save('Verification-Tools/consolidation-validation.json',{'timestamp_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'copy_hashes_verified':len(verified),'baseline_files_hash_checked':len(hashes),'tracked_source_changes':[],'gitignore_modified':False,'databases_accessed':False,'publication_privacy_gate':'PENDING','link_gate':'PENDING'})
print(json.dumps({'copies_verified':len(verified),'baseline_files':len(hashes),'streams':rows,'source_modified':False}))
