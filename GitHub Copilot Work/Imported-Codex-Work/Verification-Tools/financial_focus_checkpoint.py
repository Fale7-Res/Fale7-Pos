"""Reconcile active financial-only queue without rewriting original agent evidence."""
import json
import subprocess
from knowledge_store import *

original = json.loads((WORK/'Sub-Agents/Results/financial-symbol-remaining.json').read_text(encoding='utf8'))
scope = set(original['scope_paths'])
done = set()
counts = {}
for owner in ('financial_integrity', 'financial_coordinator'):
    ids = set()
    for file in (WORK/'Knowledge/Reviews'/owner).rglob('*.json'):
        review = json.loads(file.read_text(encoding='utf8'))
        if review['path'] in scope:
            ids.update(r['stable_id'] for r in review['symbols'] if r.get('status') == 'ANALYZED')
    counts[owner] = len(ids)
    done.update(ids)
symbols = []
for path in sorted(scope):
    file = WORK/'Knowledge/Source-Symbols'/Path(path+'.json')
    if not file.exists():
        continue
    source = json.loads(file.read_text(encoding='utf8'))
    if sha256((ROOT/path).read_bytes()) != source['worktree_sha256']:
        raise ValueError('Source invalidation: '+path)
    for symbol in source['symbols']:
        if symbol['isCallable']:
            symbols.append({'path': path, 'stable_id': symbol['stable_id'], 'kind': symbol['kind'],
                            'signature': symbol.get('qualifiedSymbol') or symbol['name'],
                            'span': [symbol['startLine'], symbol['startColumn'], symbol['endLine'], symbol['endColumn']]})
remaining = [symbol for symbol in symbols if symbol['stable_id'] not in done]
facets = {'getter_receipts': 0, 'generated_behavior_receipts': 0}
for file in (WORK/'Knowledge/Reviews-CallableFacets').rglob('*.json'):
    review = json.loads(file.read_text(encoding='utf8'))
    if review.get('reviewer') not in ('financial_integrity','financial_coordinator'):
        continue
    for values in review.values():
        if isinstance(values, list):
            facets['getter_receipts'] += sum(isinstance(r, dict) and r.get('status') == 'ANALYZED' for r in values)
            facets['generated_behavior_receipts'] += sum(isinstance(r, dict) and r.get('status') == 'BEHAVIOR_ANALYZED' for r in values)
outside = subprocess.check_output(['git', 'diff', '--name-only', '--', '.', ':!Codex Work'], text=True).splitlines()
snapshot = {
    'at_utc': now(), 'baseline': BASELINE, 'scope_file_paths': sorted(scope), 'file_scope_count': len(scope),
    'original_callable_denominator': len(symbols), 'reviewer_receipt_counts': counts,
    'union_original_receipts': len(done), 'remaining_original_count': len(remaining),
    'remaining_original_symbols': remaining,
    'supplementary_financial_scope': {'getter_total': 77, 'generated_behavior_total': 95, **facets},
    'git_head': subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip(),
    'outside_workspace_tracked_diff': outside,
    'limits': 'Identity/source and author-receipt accounting, not independent certification of all function reasoning. No live financial tests. Original specialist ledgers retained unchanged.'
}
atomic_json(WORK/'Sub-Agents/Results/financial-coordinator-coverage.json', snapshot)
backlog_path = WORK/'Conversation-Recovery/PAUSED_AUDIT_BACKLOG.json'
backlog = json.loads(backlog_path.read_text(encoding='utf8'))
backlog['latest_financial_checkpoint'] = {
    'at_utc': snapshot['at_utc'], 'reference': 'Codex Work/Sub-Agents/Results/financial-coordinator-coverage.json',
    'union_original_receipts': len(done), 'remaining_original_count': len(remaining),
    'candidates_pending_independent_challenge': [
        'Deep-Audit/Database-and-Finance/payroll-status-race-candidate.json',
        'Deep-Audit/Database-and-Finance/cash-denomination-boundary-lead.json',
        'Deep-Audit/Database-and-Finance/prep-graph-concurrency-candidate.json',
        'Deep-Audit/Database-and-Finance/packaging-completeness-candidate.json',
        'Deep-Audit/Database-and-Finance/business-day-admission-candidate.json'
    ],
    'last_financial_session_retry': 'Original session followup failed: agent thread limit reached. Previous replacement attempt also failed; no replacement exists.',
    'execution': 'Root performing financial source review; nonfinancial specialists paused; no repeat launches while last observed limit active.'
}
atomic_json(backlog_path, backlog)
latest_review=max((json.loads(f.read_text(encoding='utf8')) for f in (WORK/'Knowledge/Reviews/financial_coordinator').rglob('*.json')), key=lambda r:r['updated_at_utc'])
checkpoint = {
    'at_utc': now(), 'mode': 'FINANCIAL_ONLY_ROOT_CONTINUATION_SPECIALIST_UNAVAILABLE',
    'last_completed_file': latest_review['path'],
    'last_completed_function': latest_review['symbols'][-1]['signature'],
    'last_completed_stable_id': latest_review['symbols'][-1]['stable_id'],
    'last_completed_operation': 'Clock/audit/pricing/scheduler23 saved and read back;77/77 supplemental getters saved. Reverse-date report chain qualified source lead persisted; no live runtime actions.',
    'current_partial_file': None, 'current_partial_function': None,
    'next_exact_operation': 'DailyStatement71, DbContext212 and clock/audit/pricing/scheduler23 additional original receipts saved; all77 supplemental getters receipted. Reverse-date source lead saved, next trace all cost range callers and financial report scope boundaries; five canonical candidates independently pending.',
    'remaining_original_count': len(remaining), 'exact_remaining_reference': 'Sub-Agents/Results/financial-coordinator-coverage.json',
    'supplementary_remaining': {'getters': 77-facets['getter_receipts'], 'generated_behavior_contexts': 95-facets['generated_behavior_receipts']},
    'remaining_full_audit': 'Conversation-Recovery/PAUSED_AUDIT_BACKLOG.json preserves all15phases/19deliverables; nonfinancial execution paused.',
    'blocked': ['Original specialist and replacement attempt thread-limit failure; no independent reviewer currently available.', 'Financial function review remains materially incomplete; no completion certificate justified.'],
    'publication': 'ONE_INTERIM_PUSH_VERIFIED_bc914a4d71a34de6ef6655887ce73140075c17e0_NO_FURTHER_PUBLICATION', 'source_or_runtime_writes': False
}
atomic_json(WORK/'Sub-Agents/Checkpoints/financial_coordinator.json', checkpoint)
next_path = WORK/'Conversation-Recovery/NEXT_ACTION.md'
previous = next_path.read_text(encoding='utf8')
marker = '\n## Current reconciled financial snapshot\n'
if marker in previous:
    previous = previous.split(marker, 1)[0]
atomic_text(next_path, previous + marker +
    '\nUpdated UTC: '+snapshot['at_utc']+'\n\n'+
    '- Active executor: root financial source continuation; no specialist available. All nonfinancial agents paused.\n'+
    '- Last file: '+latest_review['path']+'; last saved function '+latest_review['symbols'][-1]['signature']+'. No partial function claim.\n'+
    f'- Union original financial receipts: {len(done)}/{len(symbols)}; {len(remaining)} still lack receipts. Author assertions are not independent certification.\n'+
    f'- Root receipts: {counts["financial_coordinator"]}; overlaps retained and unioned, never blindly added.\n'+
    f'- Additive getters {facets["getter_receipts"]}/77; generated-context reasoning {facets["generated_behavior_receipts"]}/95 (not CLR method count).\n'+
    '- Exact remaining IDs/signatures/spans: Sub-Agents/Results/financial-coordinator-coverage.json. All other phases/queues preserved in PAUSED_AUDIT_BACKLOG.json.\n'+
    '- FIN017/018 independently source-corroborated; payroll status race and cash-count boundary candidate remain independently unchallenged.\n'+
    '- Next: reversed-date report source lead and remaining financial report/domain functions. One coordinated original/replacement attempt failed thread limit; no further launches this pass. All77 financial supplemental getters now have source reasoning, independently unchallenged.\n'+
    '- Financial completion and independent quality gates remain OPEN. Do not ask to resume Printing or publish.\n')
assert not outside, 'Unexpected tracked changes outside Codex Work: publication remains forbidden'
print(json.dumps({key: value for key, value in snapshot.items()
                  if key not in ('scope_file_paths', 'remaining_original_symbols')}))
