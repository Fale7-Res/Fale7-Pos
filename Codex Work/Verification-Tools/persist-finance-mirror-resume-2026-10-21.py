"""Reconcile the 2026-10-21 finance-agent handoffs and SEM-MIRROR-047 checkpoint."""
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / "Codex Work"
now = datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z")

def load(rel: str):
    return json.loads((WORK / rel).read_text(encoding="utf-8"))

def save(rel: str, value):
    p = WORK / rel
    tmp = p.with_suffix(p.suffix + ".tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    tmp.replace(p)

def append_once(rel: str, marker: str, text: str):
    p = WORK / rel
    old = p.read_text(encoding="utf-8")
    if marker not in old:
        p.write_text(old.rstrip() + "\n\n" + text.strip() + "\n", encoding="utf-8")

# Canonical finance report: cite agent handoffs and preserve conditional/test limits.
finance_note = """
## Supplemental report-callable reviews and delivery custody metric challenge — 2026-10-21

The resumed continuation specialist saved source reviews for both `GetTaxReportAsync` overloads and both `GetSalesReportAsync` overloads: [tax review](../../Sub-Agents/Handoffs/financial-tax-report-substantive-review-2026-10-21.json) and [sales review](../../Sub-Agents/Handoffs/financial-sales-report-substantive-review-2026-10-21.json). Tax aggregation uses checkout-time frozen tax snapshots; refund tax impact is a proportional estimate. The earlier migration's zero default/no historical backfill remains conditional on whether pre-migration taxed rows exist, which was not queried. Sales reporting distinguishes cash-basis collected sales from drawer/payment-channel totals; this review found no new defect. No tests were executed.

The specialist independently corroborated the [delivery-outcomes challenge](../../Sub-Agents/Handoffs/financial-negative-variant-and-delivery-outcomes-independent-challenge-2026-10-21.json): `UnsettledCustodyExpectedPiastres` is populated from the sum of `CollectedPiastres`, although custody creation stores the expected amount and starts collected at zero. Before delivery this can report zero in an explicitly expected-valued DTO; after a delivery variance, expected and collected remain distinct. The checked-in report test covers only successful exact settlement, where the values coincide. Preserve this as `FIN-CAND-DELIVERY-CUSTODY-EXPECTED-001`, a source-confirmed metric-to-field mismatch with report-contract impact not runtime-verified. A future correction should aggregate `ExpectedPiastres` for this DTO field and test assigned/unsettled, exact delivery, short collection, and over-collection. Whether the metric is point-in-time/global or date-filtered remains a separate report-contract question.

The current financial receipt union remains **7,602/8,804 structural membership only**; 1,202 callable IDs are outside that union, while later residual dispositions and specialist handoffs are supplemental and are not evidence of a fully reconciled substantive count. Overall genuine function-level coverage remains **UNKNOWN**, and the 327-file/financial completion gate remains open. The specialist finished only the bounded assigned handoffs; no source, tests, database, or runtime was changed.
"""
append_once("Deep-Audit/Database-and-Finance/DATABASE_AND_FINANCIAL_INTEGRITY.md", "## Supplemental report-callable reviews and delivery custody metric challenge — 2026-10-21", finance_note)

# Current mirror status derived from the validated semantic registry and gate.
registry = load("Deep-Audit/Duplication-and-Architecture/semantic-comparison-registry.json")
gate = load("Deep-Audit/Duplication-and-Architecture/semantic-evidence-quality-gate.json")
links = load("Deep-Audit/Duplication-and-Architecture/final-report-links-validation.json")
leads = load("Deep-Audit/Duplication-and-Architecture/remaining-semantic-search-leads-20261021-continued.json")
referenced = {r["path"] for i in registry["inputs"] for r in load(i["path"]).get("source_references", load(i["path"]).get("sources", []))}
remaining_leads = len({x["path"] for x in leads.get("remaining_leads", [])} - referenced)
status_path = "Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.json"
status = load(status_path)
counts = {
    "semantic_groups": gate["semantic_groups"], "artifact_inputs": gate["artifact_inputs"],
    "source_reference_records": gate["source_reference_records"], "distinct_source_paths": gate["distinct_source_paths"],
    "stable_callable_context_ids": gate["distinct_referenced_symbol_ids"], "explicit_line_ranges": gate["explicit_ranges_checked"]
}
status["at_utc"] = now
status["bounded_semantic_comparisons"] = counts["semantic_groups"]
status["semantic_groups_current"] = counts["semantic_groups"]
status["semantic_artifacts_current"] = counts["artifact_inputs"]
status["semantic_source_reference_records_current"] = counts["source_reference_records"]
status["semantic_paths_current"] = counts["distinct_source_paths"]
status["semantic_symbol_ids_current"] = counts["stable_callable_context_ids"]
status["semantic_ranges_current"] = counts["explicit_line_ranges"]
status["latest_semantic_group"] = "SEM-MIRROR-047 Call Center vs Cashier WinUI checkout recovery"
status["latest_semantic_artifact"] = "Deep-Audit/Duplication-and-Architecture/cashier-callcenter-checkout-recovery-semantic-comparison.json"
status["latest_semantic_disposition"] = "Call Center durably persists/restores its checkout DTO; Cashier same-process retry is retained in memory only. Related to existing UI-001; no duplicate finding created; duplicate-intent risk is conditional, not runtime-proven."
status["latest_mechanical_gate"] = f"PASS: semantic {counts['semantic_groups']}/{counts['artifact_inputs']}/{counts['source_reference_records']}/{counts['distinct_source_paths']}/{counts['stable_callable_context_ids']}/{counts['explicit_line_ranges']}; mirror links {links['checked_links']}/{len(links.get('broken_links', []))} broken; financial evidence 45 findings/141 paths/0 errors/0 missing ranges; src/tests baseline diff clean"
status["latest_semantic_evidence_gate"] = f"PASS_SOURCE_IDENTITY_AND_SYMBOL_REFERENCES: {counts['semantic_groups']} groups, {counts['artifact_inputs']} inputs, {counts['source_reference_records']} refs, {counts['distinct_source_paths']} paths, {counts['stable_callable_context_ids']} symbols, {counts['explicit_line_ranges']} ranges"
status["completion_gate"] = "OPEN — finite AST candidate queue dispositioned and bounded semantic comparisons continue; renamed semantic equivalence/dead-code/reachability remains non-exhaustive."
status["current_reconciliation"] = {
    "as_of_utc": now, "identified_ast_candidate_groups": status.get("candidate_group_total", 218),
    "generated_history_accounted": status.get("status_counts", {}).get("GENERATED_HISTORY_REPETITION_ACCOUNTED_NOT_REFACTOR_DEBT", 0),
    "remaining_application_groups": status.get("pending_application_groups", 0), "remaining_test_groups": status.get("pending_test_groups", 0),
    "bounded_semantic_groups": counts["semantic_groups"], "semantic_registry_artifact_inputs": counts["artifact_inputs"],
    "source_reference_records": counts["source_reference_records"], "distinct_source_paths": counts["distinct_source_paths"],
    "stable_callable_context_ids": counts["stable_callable_context_ids"], "explicit_line_ranges": counts["explicit_line_ranges"],
    "semantic_gate": gate["gate"], "report_link_gate": f"{links['checked_links']} links / {len(links.get('broken_links', []))} broken",
    "remaining_keyword_lead_paths_without_semantic_artifact_ref": remaining_leads,
    "remaining_keyword_lead_manifest": "Duplication-and-Architecture/remaining-semantic-search-leads-20261021-continued.json",
    "status": "INCOMPLETE — finite clone candidates dispositioned; bounded semantic set documented; keyword hits are leads only; full semantic/dead-code/reachability scope remains open."
}
save(status_path, status)
append_once("Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.md", "## Current continuation state —", f"""
## Current continuation state — {now}

The current semantic-comparison registry contains {counts['semantic_groups']} bounded comparison groups in {counts['artifact_inputs']} artifacts, with {counts['source_reference_records']} source-reference records across {counts['distinct_source_paths']} paths, {counts['stable_callable_context_ids']} stable symbol IDs, and {counts['explicit_line_ranges']} line ranges. Mechanical identity/symbol/range validation passed; {links['checked_links']} checked report links have zero broken. `SEM-MIRROR-047` compares restart recovery of checkout idempotency across Call Center and Cashier WinUI and supplements existing `UI-001` without creating a duplicate finding. The finite 218 exact-body candidate inventory is accounted for, while renamed semantic equivalence, full dead-code/reachability and runtime behavior remain outside proven completeness.
""")

# Agent registry: bounded continuation completed, entire financial gate remains open.
agent_path = "Sub-Agents/Agent-Registry/agent-state.json"
agents = load(agent_path)
replacement = next(x for x in agents["replacement_agents"] if x["name"] == "/root/financial_integrity_continuation")
handoffs = [
    "Codex Work/Sub-Agents/Handoffs/financial-negative-variant-and-delivery-outcomes-independent-challenge-2026-10-21.json",
    "Codex Work/Sub-Agents/Handoffs/financial-tax-report-substantive-review-2026-10-21.json",
    "Codex Work/Sub-Agents/Handoffs/financial-sales-report-substantive-review-2026-10-21.json"
]
replacement["status"] = "COMPLETED_BOUNDED_REPORT_REVIEWS; GLOBAL_FINANCIAL_AUDIT_OPEN"
replacement["execution_control"] = "COMPLETED_ASSIGNED_HANDOFFS_SESSION_IDLE"
replacement["latest_handoffs"] = list(dict.fromkeys(replacement.get("latest_handoffs", []) + handoffs))
replacement["latest_reporting_batch"] = handoffs[-1]
replacement["latest_reporting_batch_status"] = "Both SalesReport overloads source-reviewed; cash-basis vs drawer/payment channel distinctions; no new defect; no tests."
replacement["latest_independent_challenge"] = handoffs[0]
replacement["next_operation"] = "Global substantive financial queue remains UNKNOWN; continue only after evidence-class reconciliation identifies a safe non-duplicate callable."
replacement["global_financial_completion"] = False
replacement["completion_limit"] = "Only bounded report/challenge handoffs completed this session; 7,602/8,804 receipt union is structural membership and overall genuine substantive count remains UNKNOWN."
replacement["updated_at_utc"] = now
agents["updated_at_utc"] = now
agents["active_specialist"] = "/root/financial_integrity_continuation completed bounded handoffs; no active execution. Original /root/financial_integrity unavailable."
agents["latest_user_request_execution"] = {
    "at_utc": now, "finance_agent": "Existing /root/financial_integrity_continuation resumed; completed independent variant-price/delivery metric challenge and substantive TaxReport and SalesReport handoffs. Agent is now completed/idle for these bounded tasks; no replacement created.",
    "finance_handoffs": handoffs, "finance_structural_union": "7,602/8,804 membership only; 1,202 outside union; genuine semantic coverage UNKNOWN; 327-file completion gate OPEN.",
    "finance_next": "Unknown until evidence-class reconciliation defines a genuinely unreviewed callable; do not choose from receipt absence alone.",
    "coordinator": "SEM-MIRROR-047 Call Center vs Cashier WinUI checkout restart-recovery comparison; related to existing UI-001; no duplicate finding.",
    "mirror_quality": status["latest_mechanical_gate"], "other_specialists": "PAUSED", "publication": "NO COMMIT/PUSH", "source_runtime": "No application/tests/database/runtime changes; no tests run."
}
save(agent_path, agents)

# Workstream checkpoints.
root_path = "Sub-Agents/Checkpoints/root-current-two-workstreams.json"
root = load(root_path)
root.update({"at_utc": now, "publication": "NO_COMMIT_NO_PUSH", "source_safety": "src/tests diff from baseline clean; audit documents only"})
root["coordinator"] = {
    "owner": "root", "workstream": "Mirrored/Duplicated Code", "last_completed": "SEM-MIRROR-047 persisted; semantic evidence and report-link gates pass.",
    "current_file_or_function": "Cashier vs Call Center WinUI checkout restart-recovery path; completed bounded comparison.",
    "next": "Continue a distinct source-driven semantic mirror group; do not claim Phase 11 exhaustive semantic/dead-code completion.",
    "bounded_semantic": counts, "quality_gates": {"semantic_evidence": status["latest_semantic_evidence_gate"], "report_links": f"PASS {links['checked_links']} links / 0 broken", "financial_metadata": "PASS 45 findings / 141 paths / 0 errors / 0 missing ranges", "source_tests": "BASELINE CLEAN"},
    "latest_artifacts": ["Codex Work/Deep-Audit/Duplication-and-Architecture/cashier-callcenter-checkout-recovery-semantic-comparison.json", "Codex Work/Deep-Audit/CODE_DUPLICATION_AND_ARCHITECTURE.md"]
}
root["financial"] = {
    "owner": "/root/financial_integrity_continuation", "agent_status": "COMPLETED_BOUNDED_HANDOFFS; global financial work incomplete",
    "last_completed": handoffs, "receipt_union": "7,602/8,804 structural membership only", "genuine_substantive_coverage": "UNKNOWN",
    "327_file_completion_gate": "OPEN", "next": "Reconcile stable-ID evidence classes before selecting new callable; prevent duplicate review.",
    "findings": ["FIN-CAND-NEGATIVE-VARIANT-PRICE-001 independently corroborated conditionally", "FIN-CAND-DELIVERY-CUSTODY-EXPECTED-001 metric-field mismatch independently corroborated; report impact not runtime tested"],
    "tests": "NOT RUN"
}
save(root_path, root)

mirror_path = "Sub-Agents/Checkpoints/root-mirrored-code.json"
mirror = load(mirror_path)
mirror["at_utc"] = now
mirror["last_completed"] = "SEM-MIRROR-047: Call Center persists and restores checkout request; Cashier checkout only retains in-memory pending DTO despite configuring store. Cross-reference UI-001, no duplicate finding."
mirror["latest_artifact"] = "Codex Work/Deep-Audit/Duplication-and-Architecture/cashier-callcenter-checkout-recovery-semantic-comparison.json"
mirror["coverage"] = counts
mirror["quality_gate"] = status["latest_semantic_evidence_gate"]
mirror["next_action"] = "Continue bounded semantic scan beyond exact-body candidate inventory; preserve limits and avoid duplicate financial callable credit."
save(mirror_path, mirror)

progress_path = "Deep-Audit/audit-progress.json"
progress = load(progress_path)
progress["latest_financial_mirror_continuation"] = {
    "at_utc": now,
    "financial_agent": "/root/financial_integrity_continuation completed bounded handoffs; global gate open",
    "financial_handoffs": handoffs,
    "financial_receipt_union": "7602/8804 structural membership; genuine substantive coverage unknown",
    "finance_candidate_challenge": "FIN-CAND-DELIVERY-CUSTODY-EXPECTED-001 independently corroborated as DTO Expected populated from Collected; not runtime verified",
    "mirror_group": "SEM-MIRROR-047",
    "mirror_artifact": "Deep-Audit/Duplication-and-Architecture/cashier-callcenter-checkout-recovery-semantic-comparison.json",
    "mirror_current_counts": counts,
    "mirror_gates": {"semantic_evidence": gate["gate"], "report_links_checked": links["checked_links"], "broken_links": len(links.get("broken_links", []))},
    "source_tests": "CLEAN against approved baseline",
    "other_specialists": "PAUSED",
    "publication": "NO COMMIT/PUSH",
    "tests_runtime_database": "NOT EXECUTED"
}
save(progress_path, progress)

# Durable owner-facing resume records.
next_note = f"""
## Superseding resumed-batch checkpoint — {now}

- Finance agent `/root/financial_integrity_continuation` was available and its same existing identity completed three bounded handoffs: `financial-negative-variant-and-delivery-outcomes-independent-challenge-2026-10-21.json`, `financial-tax-report-substantive-review-2026-10-21.json`, and `financial-sales-report-substantive-review-2026-10-21.json`. The agent is now completed/idle for that assignment. The original `/root/financial_integrity` remains unavailable. No additional agent or replacement was created.
- New specialist evidence independently corroborates `FIN-CAND-DELIVERY-CUSTODY-EXPECTED-001`: an Expected-valued DTO sums CollectedPiastres; existing test only observes exact successful settlement. Candidate remains source-confirmed mismatch with runtime report impact untested. It also corroborates the negative variant candidate with privileged-menu and policy qualifications.
- Tax report handoff reviewed both overloads: frozen checkout snapshots, proportional refund-tax impact is an estimate, conditional legacy no-backfill concern remains absent data inspection. Sales report handoff reviewed both overloads: collected cash basis differs by design from drawer/payment-channel totals; no new defect.
- Main coordinator completed `SEM-MIRROR-047`, a bounded cross-station comparison: Call Center persists/restores immutable checkout DTO; Cashier checkout retains same-process only. This supplements existing `UI-001`, no duplicate canonical finding. Artifact `Deep-Audit/Duplication-and-Architecture/cashier-callcenter-checkout-recovery-semantic-comparison.json`; final report section appended.
- Current gates: semantic evidence {counts['semantic_groups']}/{counts['artifact_inputs']}/{counts['source_reference_records']}/{counts['distinct_source_paths']}/{counts['stable_callable_context_ids']}/{counts['explicit_line_ranges']} PASS; mirror links {links['checked_links']}/0 broken; financial metadata PASS 45 findings/141 paths/0 missing ranges; src/tests diff from baseline CLEAN.
- Financial coverage remains structural union 7,602/8,804 only; genuine substantive global count UNKNOWN, all financial completion gates OPEN. No tests run. No source, test, database, runtime, or production changes. No commit/push.
- Next coordinator operation: continue a distinct bounded semantic comparison from source; exact financial callable next is UNKNOWN pending evidence-class crosswalk, not inferable from receipt-only gaps. All other specialist agents remain paused.
"""
append_once("Conversation-Recovery/NEXT_ACTION.md", "## Superseding resumed-batch checkpoint —", next_note)
append_once("Conversation-Recovery/FINANCIAL_MIRROR_JOINT_RECONCILIATION.md", "## Resumed batch reconciliation —", f"""
## Resumed batch reconciliation — {now}

Communication was direct through the existing agent session `/root/financial_integrity_continuation`; the agent completed its bounded challenge and two report-callable handoffs and is currently completed/idle. The original `/root/financial_integrity` remains inaccessible. Work was not sent to a replacement.

Financial specialist conclusions: the negative variant-price candidate is independently source-corroborated with authorization and owner zero-price-policy conditions; the delivery-outcomes DTO labelled Expected uses collected amounts, independently corroborated against assignment and settlement paths; both tax-report overloads use frozen checkout snapshots, with a conditional legacy migration issue; both sales-report overloads intentionally distinguish collected sales from drawer/payment-channel figures. Exact reports are linked in NEXT_ACTION.md and the agent registry. No tests ran.

Coordinator completed SEM-MIRROR-047. Current bounded semantic registry: {counts['semantic_groups']} groups / {counts['artifact_inputs']} evidence inputs / {counts['source_reference_records']} source refs / {counts['distinct_source_paths']} paths / {counts['stable_callable_context_ids']} symbol IDs / {counts['explicit_line_ranges']} ranges. Mechanical identity and line/symbol validation passed; {links['checked_links']} report links have zero broken. This is not exhaustive semantic-equivalence proof. SEM-MIRROR-047 supplements existing UI-001; no duplicate finding ID created.

Reconciled outcome: both assigned workers made durable bounded progress, but neither global workstream can be certified complete. Finance substantive callable total remains unknown and 327-file scope completion is not certified; the mirrored-code Phase 11 semantic/dead-code/reachability scope remains open. All other specialists remain paused; no publication.
""")

print(json.dumps({"at_utc": now, "mirror": counts, "remaining_keyword_leads_without_semantic_reference": remaining_leads, "finance_agent": replacement["status"], "financial_gate": "OPEN", "mirror_gate": "OPEN"}, ensure_ascii=False))
