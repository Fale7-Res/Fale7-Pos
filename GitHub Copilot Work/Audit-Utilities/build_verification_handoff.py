import json
import sys
import time
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')

def build_verification_handoff():
    out_dir = Path("GitHub Copilot Work/13-Independent-Verification")
    out_dir.mkdir(parents=True, exist_ok=True)

    manifest_data = {
        "manifest_version": "1.0",
        "timestamp_utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "source_baseline": {
            "commit_sha": "c35816ecae49633674775dd32061c6ee92f94aaf",
            "git_head": "9beff52a6c9c6c4f7244cbd46dc449da91745582",
            "total_tracked_files": 690,
            "working_tree_matches_baseline": True
        },
        "original_audit_scope": {
            "phases_count": 15,
            "deliverables_count": 19,
            "all_phases_covered": True,
            "all_deliverables_prepared": True
        },
        "callable_inventory_and_evidence": {
            "total_repository_callables": 11653,
            "production_callables_src": 9205,
            "test_callables_tests": 2439,
            "tools_callables": 9,
            "substantive_reviews_completed": 8852,
            "generated_or_trivial_with_justification": 2801,
            "unaccounted_callables": 0,
            "registry_path": "GitHub Copilot Work/02-Callable-Inventory-and-Reviews/CALLABLE_REVIEW_REGISTRY.json"
        },
        "material_findings": {
            "total_findings": 77,
            "high_severity": 32,
            "medium_severity": 40,
            "low_severity": 5,
            "dispositions": {
                "INDEPENDENTLY_CORROBORATED": 27,
                "CORROBORATED_WITH_QUALIFICATIONS": 24,
                "REQUIRES_RUNTIME_VERIFICATION": 21,
                "REQUIRES_OWNER_BUSINESS_DECISION": 5
            },
            "registry_path": "GitHub Copilot Work/11-Master-Findings/RECONCILED_MASTER_FINDINGS.json"
        },
        "mirrored_code_and_architecture": {
            "ast_clone_groups_dispositioned": 218,
            "semantic_comparisons_recorded": 71,
            "reconciled_contradiction": "Latest comparison is definitively SEM-MIRROR-071 (resolved from historical 066 record)",
            "registry_path": "GitHub Copilot Work/04-Mirrored-Code-and-Architecture/CODE_DUPLICATION_AND_ARCHITECTURE.md"
        },
        "financial_workflow_coverage": {
            "workflows_analyzed": 16,
            "invariants_evaluated": 41,
            "critical_flaws_proven": ["FIN-001", "FIN-002", "FIN-003", "FIN-005", "FIN-006", "FIN-013"],
            "report_path": "GitHub Copilot Work/03-Financial-Integrity/DATABASE_AND_FINANCIAL_INTEGRITY.md"
        },
        "printing_subsystem_coverage": {
            "routes_analyzed": 6,
            "critical_flaws_proven": ["PRT-001", "PRT-002", "PRT-003", "PRT-004"],
            "report_path": "GitHub Copilot Work/06-Printing/PRINTING_RELIABILITY_AND_PREVIEW.md"
        },
        "outstanding_runtime_verification": {
            "sql_server_concurrency": "Multi-threaded race conditions in business day open and supplier settlements require active LocalDB execution.",
            "physical_printing": "Xprinter XP-D200N hardware verification of 5-feed cut distance and bitmap rasterization requires physical device connection.",
            "status": "EXPLICITLY_MARKED_UNEXECUTED_DUE_TO_SAFETY_POLICY"
        },
        "files_requiring_independent_codex_challenge": [
            "src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs",
            "src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs",
            "src/Falah.Domain/Orders/Order.cs",
            "src/Falah.Infrastructure/Orders/RefundOrderHandler.cs",
            "src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs",
            "src/Falah.Infrastructure/Dashboard/OwnerDashboardQueryService.cs",
            "src/Falah.Server/Controllers/",
            "src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs",
            "src/Falah.PrintAgent/Workers/PrintPollingWorker.cs"
        ]
    }

    with open(out_dir / "CODEX_VERIFICATION_MANIFEST.json", 'w', encoding='utf-8') as f:
        json.dump(manifest_data, f, indent=2)

    instructions_md = """# OpenAI Codex Independent Challenge & Verification Instructions

## Role and Mandatory Directive for OpenAI Codex

OpenAI Codex is tasked with acting as the **Independent External Challenger and Verifier** for the Fale7 POS audit completed by GitHub Copilot.

**DO NOT ACCEPT COPILOT'S CONCLUSIONS AT FACE VALUE.**
You are required to perform a rigorous, adversarial source-grounded challenge against the actual C# and XAML code at commit `c35816ecae49633674775dd32061c6ee92f94aaf`.

---

## Verification Protocols for Codex

### 1. Challenge Material High-Severity Findings
Independently inspect the source code files for each P0 finding:
1. **FIN-001 (Split-Payment Journal Key Collision):**
   - Inspect `CheckoutOrderHandler.cs:148-155` and `ConfirmElectronicPaymentHandler.cs:75-78`.
   - Verify whether both handlers construct `sale-{CheckoutRequestId}` and whether `EfFinancialJournal.TryStart` prevents confirmation.
2. **FIN-003 (Unpaid Order Refund Leakage):**
   - Inspect `Order.cs:220-232` and `RefundOrderHandler.cs`.
   - Confirm whether `ApplyRefund` checks `IsPaid` or permits cash refunds on unpaid COD orders.
3. **FIN-002 (Mixed Order Cancellation Cash Omission):**
   - Inspect `Order.cs:200-214` and `CancelPendingElectronicPaymentHandler.cs`.
   - Verify whether cash splits already deposited into the cash drawer are reversed or left stranded.
4. **SEC-001 (JSON Actor Binding Privilege Escalation):**
   - Inspect controller endpoint bindings and `OrderMapper.ToActor`.
   - Challenge whether an attacker can supply an `Actor` block in JSON to escalate permissions.
5. **PRT-001 (Duplicate Kitchen Print Jobs):**
   - Inspect `OrderAutoPrintService.cs` and `PrintJobs` table constraints.
   - Challenge whether two concurrent checkout/update events can submit twin kitchen preparation tickets.

### 2. Verify Callable Ledger Authenticity
- Inspect `GitHub Copilot Work/02-Callable-Inventory-and-Reviews/CALLABLE_REVIEW_REGISTRY.json`.
- Sample at least 50 random callables across Domain, Application, Infrastructure, Server, and Clients.
- Confirm that the line spans, parameter types, and behavioral descriptions accurately reflect the code.

### 3. Safety Constraints
- **DO NOT MODIFY APPLICATION SOURCE CODE.**
- **DO NOT WRITE TO CODEX WORK.** Write verification logs under `Codex Work` only if specifically authorized by the owner, or create a challenge report in `GitHub Copilot Work/13-Independent-Verification/CODEX_CHALLENGE_REPORT.md`.
- No live database writes or physical printing operations.
"""
    with open(out_dir / "CODEX_VERIFICATION_INSTRUCTIONS.md", 'w', encoding='utf-8') as f:
        f.write(instructions_md)

    print("Saved CODEX_VERIFICATION_MANIFEST.json and CODEX_VERIFICATION_INSTRUCTIONS.md.")

if __name__ == '__main__':
    build_verification_handoff()
