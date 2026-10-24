"""Coordinator-only current checkpoint reconciliation; no Git or runtime mutation."""
from knowledge_store import *
coverage=json.loads((WORK/'Sub-Agents/Results/financial-coordinator-coverage.json').read_text(encoding='utf8'))
remaining=coverage['remaining_original_symbols'];remaining_ids={x['stable_id'] for x in remaining}
team_path=WORK/'Sub-Agents/Agent-Registry/financial-team-state.json';team=json.loads(team_path.read_text(encoding='utf8'))
allocated=set()
for assignment in (WORK/'Sub-Agents/Assignments/Financial-Team').glob('*.json'):
 j=json.loads(assignment.read_text(encoding='utf8'));old=list(j['remaining_original_symbols'])
 j.setdefault('initial_assignment_symbol_ids',[x['stable_id'] for x in old])
 j['remaining_original_symbols']=[x for x in old if x['stable_id'] in remaining_ids]
 j['completed_since_initial_allocation_ids']=sorted(set(j['initial_assignment_symbol_ids'])-remaining_ids)
 j['remaining_original_count']=len(j['remaining_original_symbols']);j['reconciled_at_utc']=now()
 j['actual_execution']='Root fallback sequential source review; no specialist session was created.'
 allocated.update(x['stable_id'] for x in j['remaining_original_symbols']);atomic_json(assignment,j)
 for role in team['roles']:
  if role['role']==j['role']:
   role['remaining_original_count']=j['remaining_original_count'];role['completed_since_initial_allocation_count']=len(j['completed_since_initial_allocation_ids'])
assert allocated==remaining_ids,(len(allocated),len(remaining_ids))
team['latest_coverage']={k:coverage[k] for k in ['at_utc','union_original_receipts','remaining_original_count','reviewer_receipt_counts','supplementary_financial_scope']}
team['latest_root_batch']='DailyStatement/DbContext/clock/backup/dashboard/treasury and employee attendance/payout/profile reasoning saved; current specialist launch results unchanged.'
atomic_json(team_path,team)

lead=json.loads((WORK/'Deep-Audit/Database-and-Finance/reverse-date-range-source-lead.json').read_text(encoding='utf8'))
p=WORK/'Deep-Audit/Database-and-Finance/parent-financial-findings.json';parent=json.loads(p.read_text(encoding='utf8'))
fid='FIN-CAND-REPORT-RANGE-001'
if not any(f['id']==fid for f in parent['findings']):
 parent['findings'].append({'id':fid,'title':lead['title'],'severity':'MEDIUM','confidence':'SOURCE_SUPPORTED_CANDIDATE_WITH_PARTIAL_PURE_COMPONENT_REPRODUCTION','status':'CANDIDATE_NOT_INDEPENDENTLY_VERIFIED','baseline':BASELINE,'originator':'root financial_coordinator','sources':[{'path':r['path'],'source_git_blob_sha256':r['source_git_blob_sha256'],'worktree_sha256':r['worktree_sha256'],'line_ranges':r['ranges'],'symbols':[s['stable_id'] for s in r['relevant_symbols']]} for r in lead['source_references']],'source_evidence':lead,'impact':lead['impact'],'root_cause':'Range ordering handled inconsistently between normalized sales UTC interval and raw cost/date series consumers.','preconditions':lead['preconditions'],'counterevidence':lead['counterevidence'],'recommended_fix':lead['recommended_solution'],'tests_missing':lead['required_tests'],'verification':'8 isolated component assertions passed; raw cost predicate explicitly simulated. No query service/API/DB/DTO execution; no independent reviewer.','evidence_artifact':'Codex Work/Deep-Audit/Database-and-Finance/reverse-date-range-source-lead.json'})
 parent['at_utc']=now();atomic_json(p,parent)

report_path=WORK/'Deep-Audit/Database-and-Finance/DATABASE_AND_FINANCIAL_INTEGRITY.md';report=report_path.read_text(encoding='utf8')
hist=WORK/'Knowledge/History/financial_coordinator'/('financial-report.'+sha256(report.encode())[:16]+'.md')
if not hist.exists():atomic_text(hist,report)
start=report.index('## Findings and candidates');end=report.index('## Latest completed source investigations')
findings=report[start:end]
if fid not in findings:
 findings=findings.replace('Source locations, hashes,', '| FIN-CAND-REPORT-RANGE-001 | MEDIUM | Reversed report dates scope sales and food cost differently; root candidate,8 partial component assertions, independent challenge pending |\n\nSource locations, hashes,')
executed=report[report.index('## Latest completed source investigations'):report.index('## Current financial completion gates')]
total=coverage['union_original_receipts'];missing=coverage['remaining_original_count'];root=coverage['reviewer_receipt_counts']['financial_coordinator']
updated=f'''# Database and financial integrity — ongoing review

Status: **IN_PROGRESS — completion gate NOT SATISFIED**. Updated UTC: {now()}.

Continuing audit, not a complete financial certificate or production readiness claim. Baseline `{BASELINE}`. One owner-authorized **INTERIM** documentation checkpoint pushed and verified: `bc914a4d71a34de6ef6655887ce73140075c17e0`. Subsequent batches are local, uncommitted; no second push. No application source/database/runtime state changes performed.

## Current verified accounting of evidence

- Financial scope327 paths,327 prior file reports; report presence alone does not certify full function depth.
- Original financial callable author-reasoning receipts **{total}/8804**; **{missing} missing**. Root{root}, original specialist4253; exact stable-ID union removes overlap.
- Supplementary getters **77/77**; generated contexts **9/95**. Separate denominators, not total CLR methods. These receipts still require independent semantic quality assessment.
- Exact remaining IDs/signatures/spans: [financial-coordinator-coverage.json](../../Sub-Agents/Results/financial-coordinator-coverage.json). Disjoint updated assignments: [financial-team-state.json](../../Sub-Agents/Agent-Registry/financial-team-state.json).
- Original specialist followup and exactly one replacement attempt both failed thread limit in coordinated pass; no financial specialist created. Root continues, nonfinancial specialists paused.

'''+findings+executed+'''
## Latest additional source evidence

- DailyStatement71 and current DbContext212 original callable receipts;59typed DbSet getter facets separately. All77financial getter facets now receipted, not full generated-behavior completion.
- OwnerDashboard114 callables, clock/audit/pricing/backup scheduler23, treasury safe-drop/transfer/contention20, six financial Domain files54, employee/payroll queries45, payout/poster20, credit/manual23, attendance/correction/profile27, cashout14. Counts describe new coherent root batches, not independent whole-system acceptance.
- [Owner dashboard scope review](owner-dashboard-financial-scope-review.json): current stock versus selected-period flow intentional, FIN013 preserved, date/cost and packaging completeness candidates qualified.
- [Reverse date source chain](reverse-date-range-source-lead.json) and [8component assertions](../../Local-Verification/report-range-pure-results.json): actual linked Domain calls plus explicitly simulated raw-date cost predicate. Query service/API/DB/DTO not executed.
- [Packaging component assertions](../../Local-Verification/packaging-coverage-pure-results.json):4cases,0failures. Actual CostingRules/ProfitabilityRules plus explicit synthetic accumulation; not full report query reproduction.
- [Employee query scope](employee-query-scope-comparison.json): all-time remaining balance versus range flow; active-only staff versus dashboard all ledger obligations. Adjustment credits omission links FIN012, not new duplicate finding.
- [Employee posting concurrency](employee-posting-concurrency-source-notes.json): standalone payout DOES lock employee anchor/range; shared Poster alone does not. [Credit admission source leads](employee-credit-admission-source-leads.json) retain signed wages, manual numeric enum, disclosed no-idempotency contract and cached BalanceAfter concurrency questions.
- [Attendance boundary leads](attendance-financial-boundary-source-leads.json): UTC midnight reconstruction, case-insensitive profile parse/raw string versus exact Daily consumer, historical correction using current rates. Source-supported follow-ups, not independently verified/runtime-confirmed findings.
- [Payout policy comparison](../Duplication-and-Architecture/employee-payout-policy-comparison.json) and [safe-drop/transfer comparison](../Duplication-and-Architecture/treasury-safe-drop-transfer-comparison.json): intentional account/permission/shift/advance differences preserved; no refactor implemented.

## Current financial completion gates

| Gate | Status |
|---|---|
|327 financial scope files|Accounted by prior reports; per-function depth quality remains open|
|Original applicable callable reasoning|OPEN: '''+str(missing)+''' missing original IDs|
|Supplementary behavior|77/77getter receipts;86generated contexts remain; independent depth not certified|
|Financial workflows, transaction/isolation/concurrency/restart|Source chains and risks saved; full closure and safe provider test plans still incomplete|
|Independent challenges|18FIN source findings retain saved corroboration;6root canonical financial candidates still await independent challenge|
|Runtime/hardware|No live SQL/financial records/printing; isolated components only. Provider/concurrency tests NOT_EXECUTED|
|Financial completion certificate|NOT ISSUED; material source-review gaps remain|

## Mirrored-code and paused backlog

Mirrored-code audit **INCOMPLETE**:218AST candidate groups,50application+24test pending;42groups/8paths/111distinct IDs narrow previous contextual comparisons, not whole-file completion. New treasury/employee semantic supplements are additional bounded comparisons and do not automatically reduce AST pending counts. [Dedicated status](../MIRRORED_CODE_AUDIT_STATUS.md).

Entire original15phase/19deliverable backlog and paused agents preserved in [PAUSED_AUDIT_BACKLOG.json](../../Conversation-Recovery/PAUSED_AUDIT_BACKLOG.json). Exact current next action and source queues: [NEXT_ACTION.md](../../Conversation-Recovery/NEXT_ACTION.md). No Printing/UI/general-performance resumption and no further interim Git publication without authorization.
'''
atomic_text(report_path,updated)

mirror=WORK/'Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.json';m=json.loads(mirror.read_text(encoding='utf8'))
for extra in ['treasury-safe-drop-transfer-comparison.json','employee-payout-policy-comparison.json']:
 if extra not in m['financial_mirror_supplements']:m['financial_mirror_supplements'].append(extra)
m['at_utc']=now();m['next']='Continue financially relevant remaining comparisons and verify shared state/calculation policy; added semantic supplements do not automatically certify unreviewed AST groups.';atomic_json(mirror,m)
md=WORK/'Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.md';s=md.read_text(encoding='utf8')
if '## Additional financial semantic comparisons' not in s:
 atomic_text(md,s+'\n## Additional financial semantic comparisons\n\n[Treasury safe-drop/transfer](Duplication-and-Architecture/treasury-safe-drop-transfer-comparison.json) and [employee payout policies](Duplication-and-Architecture/employee-payout-policy-comparison.json) add actual bounded source comparisons. Intentional permission/account/shift/advance and lock-order differences documented. These do not mechanically reduce50application/24test AST pending groups or establish exhaustive semantic completion. No further Git push.\n')
print(json.dumps({'current_financial_receipts':total,'remaining':missing,'allocated_remaining_exactly_once':len(allocated),'canonical_candidate_added':fid,'certificate_issued':False}))
