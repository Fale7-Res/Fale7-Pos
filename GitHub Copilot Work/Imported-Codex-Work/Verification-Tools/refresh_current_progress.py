"""Persist reconciled progress; existence/count checks never mark audit phases complete."""
import json
from knowledge_store import WORK, BASELINE, atomic_json, atomic_text, now, sha256


def main():
    p = json.loads((WORK / 'Deep-Audit/Progress-20261017-Current.json').read_text(encoding='utf8'))
    coverage = json.loads((WORK / 'Knowledge/coverage-index.json').read_text(encoding='utf8'))
    master = json.loads((WORK / 'Deep-Audit/MASTER_FINDINGS.json').read_text(encoding='utf8'))
    phases = [
        (1, 'Verified source of truth', '690 baseline identities, normalized hashes and Roslyn syntax inventory verified; semantic probes remain partial.', 'Source-Inventory/Current/FUNCTION_INDEX.json', 'Full semantic/build-generated correspondence and final architecture consistency gate.'),
        (2, 'Per-file and per-function review', 'All690 file-pass reports now present; exact callable reasoning remains incomplete and includes explicitly classified source-equivalence reuse.', 'File-Reports/', 'Every remaining stable callable and independent report-depth quality.'),
        (3, 'Adversarial security', 'SEC001 binding mechanism isolated; SEC002/003 source challenges; backup policy counterevidence saved. Plugin scan never launched successfully.', 'Security/SECURITY_AND_BACKDOOR_AUDIT.md', 'SQL boundary review active; native/process/network/timer/permission completeness and canonical qualification.'),
        (4, 'Dependency security', '210 component versions joined to all advertised saved NuGet pages;59 parser comparisons and61 static test manifests independently rechecked;210 nuspec metadata read.', 'Dependency-Security/DEPENDENCY_SECURITY_AND_SBOM.md', 'License content/provenance/signatures, final deployment/runtime/native servicing and compatibility.'),
        (5, 'Pickup workflow', 'Positive cash pickup requires payment in current rules; independent bounded requirement-gap challenge saved, overlapping UI003.', 'Printing/PICKUP_AND_DELIVERY_WORKFLOW_AUDIT.md', 'Finish global lifecycle synthesis and shared requirement grouping; no real order test.'),
        (6, 'Delivery workflow', 'Saved checkout/dispatch/COD/settlement and ticket traces exist.', 'Printing/PICKUP_AND_DELIVERY_WORKFLOW_AUDIT.md', 'Reconcile financial/state findings and final delivery workflow tests/limits.'),
        (7, 'Printing and preview', '71 file passes,388 exact author callable receipts;130 route challenges with explicit corrections; unsafe verification-script risk documented.', 'Printing/PRINTING_RELIABILITY_AND_PREVIEW.md', 'Final rendering/preview/Arabic/retry synthesis and explicit physical/native validation plan.'),
        (8, 'Visual UI and accessibility', '274 static file passes;11 HIGH UI findings independently challenged with counterevidence; exact callable review still active.', 'UI-UX/Independent-Challenges/README.md', 'Remaining UI callable analysis, final thematic report and unexecuted native/DPI/contrast state plan.'),
        (9, 'Database and finance', '327 file passes;FIN001-014 retain static source challenges, including new post-restore outcome defect.', 'Database-and-Finance/financial-parent-challenges.json', 'Remaining exact financial functions, model-context quality, synthesis and transaction/concurrency test plans.'),
        (10, 'Performance and hardware', 'Focused12-record/36-symbol source challenge saved; provisional hardware envelopes explicitly uncertified.', 'Performance/PERFORMANCE_AND_MINIMUM_HARDWARE.md', 'Integrate remaining source findings; runtime benchmark/capacity measurements NOT EXECUTED.'),
        (11, 'Duplication and architecture', 'Known shared/duplicated client journal and lifecycle observations retained in file reports.', '../Knowledge/coverage-index.json', 'Comprehensive duplicate-function clustering/manual challenge and final architecture report.'),
        (12, 'Recommendations', 'Finding-local proposed fixes/tests exist; no fixes implemented.', 'MASTER_FINDINGS.json', 'Prioritized immediate/near-term/future feature synthesis, tradeoffs and implementation roadmap.'),
        (13, 'Evidence and independence', 'Canonical source challenges retain conditional severities and rejected subclaims; no live exploit claim.', 'MASTER_FINDINGS.json', 'Final canonical mandatory-field, evidence hash/symbol/span and duplicate-group reconciliation.'),
        (14, 'Coverage and quality gates', '690 identities/report receipts verified; stable callable ledger distinguishes reused evidence and depth uncertainty.', '../Knowledge/coverage-index.json', 'Missing callable reasoning and final JSON/link/privacy/source-identity gates.'),
        (15, 'Final deliverables', 'Existing substantive drafts and supporting evidence preserved; presence is not final acceptance.', 'PHASE_PROGRESS_REPORT.md', 'Complete and reconcile all19 deliverables, then final-only normal main commit/push.')]
    phase_rows = [dict(phase=n, name=name, status='IN_PROGRESS', completed_subtasks=done,
                       evidence=evidence, remaining=remaining,
                       phase_specific_file_function_count='UNKNOWN_NO_PHASE_TAGGED_EXHAUSTIVE_LEDGER')
                  for n, name, done, evidence, remaining in phases]
    target = WORK / 'Deep-Audit/audit-progress.json'
    old = json.loads(target.read_text(encoding='utf8'))
    history = WORK / 'Knowledge/History/Progress' / ('audit-progress.' + sha256(target.read_bytes())[:20] + '.json')
    if not history.exists():
        atomic_json(history, old)
    old.update(at_utc=now(), baseline=BASELINE, status='IN_PROGRESS', streams=p['scopes'],
               file_pass_receipts=p['full_file_review_receipts'], missing_file_reports=p['files_without_full_review'],
               functions_actually_reviewed=f"{coverage['unique_analyzed_callable_receipts']} exact author callable receipts; independent depth UNKNOWN",
               callable_receipt_method_counts=coverage.get('callable_author_receipt_method_counts', {}),
               exact_remaining_callable_count=coverage['indexed_callables_without_stable_reasoning_receipt'],
               canonical_records=len(master['findings']), phase_assessments=phase_rows,
               last_completed_task='690 file reports reconciled; performance/dependency parent quality and qualified High source challenges persisted.',
               interrupted_tasks=['Exact callable queue in Conversation-Recovery/remaining-audit-queue.json',
                                  'SQL-boundary source review active; security category synthesis incomplete',
                                  'License/provenance/runtime-deployment dependency follow-up',
                                  'Duplication/architecture and all19 final deliverables/quality gates'],
               publication_gate='FINAL_ONLY; no partial commit or push',
               authoritative_queue='Codex Work/Conversation-Recovery/remaining-audit-queue.json')
    atomic_json(target, old)
    lines = ['# Reconciled progress — ' + p['at_utc'], '',
             '**Audit remains IN PROGRESS. All690 file reports exist; exhaustive function reasoning and final quality gates do not yet pass.**', '',
             f"Local main `{p['current_head']}`. Remote was read-only verified at the same SHA on2026-10-17; no commit/push performed. Original source baseline `{BASELINE}`.", '',
             f"File-pass receipts: **{p['full_file_review_receipts']}/690**, missing reports:{p['files_without_full_review']}. Source hash mismatches:{len(p['hash_mismatches'])}; receipt consistency errors:{len(p['source_review_receipt_errors'])}. Report presence does not independently certify every function.", '',
             f"Exact C# author callable receipts: **{coverage['unique_analyzed_callable_receipts']}/11653**; **{coverage['indexed_callables_without_stable_reasoning_receipt']} missing**. Independent depth total remains UNKNOWN.17 additional non-C# AST identities/receipts have a separate denominator. Legacy507 name/heading records are not extra reviewed functions.", '',
             'Author receipt methods: `' + json.dumps(coverage.get('callable_author_receipt_method_counts', {})) + '`.', '',
             f"Canonical registry has{len(master['findings'])} historical finding records, not{len(master['findings'])} independently runtime-reproduced distinct defects. FLOW001/UI003 overlap one owner workflow requirement; qualifications and grouping still need final reconciliation. No live application/DB exploit reproduced.", '',
             '## All15 original phases', '',
             '|Phase|Actually saved work|Remaining|Evidence|', '|---|---|---|---|']
    for row in phase_rows:
        lines.append(f"|{row['phase']} — {row['name']} (IN_PROGRESS)|{row['completed_subtasks']}|{row['remaining']}|[saved evidence]({row['evidence']})|")
    lines += ['', 'Per-phase source/function totals remain UNKNOWN: current ledgers identify ownership and exact symbols, not a nonoverlapping phase allocation. No percentages estimated.', '',
              '## Required deliverable presence — not acceptance', '', '|Deliverable|Saved paths/state|', '|---|---|']
    for d in p['deliverables']:
        lines.append('|'+d['name']+'|'+('; '.join(d['paths']) if d['paths'] else 'MISSING exact final deliverable')+'|')
    lines += ['|FILE_REPORTS/|690 reports under File-Reports/; final depth acceptance remains open|', '',
              'Domain377 existing-binary tests and SEC00110 isolated binding assertions are separate limited evidence. No DB-backed integration test, native visual session, physical print, benchmark or software/schema change was executed for this progress refresh.', '',
              '## Historical progress follows', '']
    report = WORK / 'Deep-Audit/PHASE_PROGRESS_REPORT.md'
    text = report.read_text(encoding='utf8')
    atomic_text(report, '\n'.join(lines) + '\n' + text)
    print(json.dumps({'file_reports': p['individual_report_files'], 'remaining_callables': coverage['indexed_callables_without_stable_reasoning_receipt'],
                      'phase_final_completion_claims': 0, 'history_preserved': True}))


if __name__ == '__main__':
    main()
