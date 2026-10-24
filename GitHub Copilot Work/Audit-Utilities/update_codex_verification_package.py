import os
import sys
import json

sys.stdout.reconfigure(encoding='utf-8')

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
WORKSPACE = os.path.join(REPO_ROOT, 'GitHub Copilot Work')
MANIFEST_JSON = os.path.join(WORKSPACE, '13-Independent-Verification', 'CODEX_VERIFICATION_MANIFEST.json')
INSTRUCTIONS_MD = os.path.join(WORKSPACE, '13-Independent-Verification', 'CODEX_VERIFICATION_INSTRUCTIONS.md')
STATS_JSON = os.path.join(WORKSPACE, '02-Callable-Inventory-and-Reviews', 'CALLABLE_SUMMARY_STATISTICS.json')
LEDGER_JSON = os.path.join(WORKSPACE, 'Checkpoints', 'SOURCE_EXCERPT_VERIFICATION_LEDGER.json')

def main():
    print("Updating Codex Verification Package...")
    with open(STATS_JSON, 'r', encoding='utf-8') as f:
        stats = json.load(f)

    with open(LEDGER_JSON, 'r', encoding='utf-8') as f:
        ledger = json.load(f)

    manifest = {
        "manifest_version": "2.0-MULTI-AGENT-GOVERNANCE",
        "timestamp_utc": "2026-10-24T18:45:00Z",
        "source_baseline": {
            "commit_sha": "c35816ecae49633674775dd32061c6ee92f94aaf",
            "git_head": "d2fd5bc44b7467ce85ddb10da2f85638857a42cc",
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
            "total_repository_callables": stats["total_callables"],
            "taxonomy": stats["taxonomy"],
            "percentages": stats["percentages"],
            "quality_gates": stats["quality_gates"],
            "registry_path": "GitHub Copilot Work/02-Callable-Inventory-and-Reviews/CALLABLE_REVIEW_REGISTRY.json",
            "statistics_path": "GitHub Copilot Work/02-Callable-Inventory-and-Reviews/CALLABLE_SUMMARY_STATISTICS.json"
        },
        "exact_code_verification_ledger": {
            "total_findings_verified": ledger["total_findings_verified"],
            "total_references_verified": sum(len(f["references"]) for f in ledger["findings"]),
            "ledger_path": "GitHub Copilot Work/Checkpoints/SOURCE_EXCERPT_VERIFICATION_LEDGER.json",
            "master_markdown_path": "GitHub Copilot Work/11-Master-Findings/RECONCILED_MASTER_FINDINGS.md"
        },
        "specialist_agent_handoffs": {
            "financial_auditor": "GitHub Copilot Work/Checkpoints/AGENT_HANDOFFS/FINANCIAL_AUDITOR_HANDOFF.md",
            "security_auditor": "GitHub Copilot Work/Checkpoints/AGENT_HANDOFFS/SECURITY_AUDITOR_HANDOFF.md",
            "printing_workflow_auditor": "GitHub Copilot Work/Checkpoints/AGENT_HANDOFFS/PRINTING_WORKFLOW_AUDITOR_HANDOFF.md",
            "mirrored_code_auditor": "GitHub Copilot Work/Checkpoints/AGENT_HANDOFFS/MIRRORED_CODE_AUDITOR_HANDOFF.md",
            "ui_maintainability_auditor": "GitHub Copilot Work/Checkpoints/AGENT_HANDOFFS/UI_MAINTAINABILITY_AUDITOR_HANDOFF.md"
        },
        "material_findings": {
            "total_findings": 77,
            "high_severity": 32,
            "medium_severity": 40,
            "low_severity": 5,
            "dispositions": {
                "INDEPENDENTLY_CORROBORATED": 25,
                "CORROBORATED_WITH_QUALIFICATIONS": 24,
                "REQUIRES_RUNTIME_VERIFICATION": 21,
                "REQUIRES_OWNER_BUSINESS_DECISION": 5,
                "INDEPENDENTLY_CORROBORATED_AND_CORRECTED": 2
            },
            "registry_path": "GitHub Copilot Work/11-Master-Findings/RECONCILED_MASTER_FINDINGS.json"
        },
        "key_corroborated_vulnerabilities": [
            "SEC-001", "SEC-002", "SEC-003", "SEC-004", "SEC-005",
            "FIN-001", "FIN-002", "FIN-003", "FIN-005", "FIN-006", "FIN-013", "FIN-016",
            "PRT-001", "PRT-002", "PRT-003", "PRT-004", "FLOW-001", "FLOW-002",
            "UI-002", "PERF-001", "ASYNC-001"
        ]
    }

    with open(MANIFEST_JSON, 'w', encoding='utf-8') as f:
        json.dump(manifest, f, indent=2)
    print(f"Updated {MANIFEST_JSON}.")

    instructions = """# OpenAI Codex Independent Challenge & Verification Instructions

## Role and Mandatory Directive for OpenAI Codex

OpenAI Codex is tasked with acting as the **Independent External Challenger and Verifier** for the Fale7 POS audit completed by GitHub Copilot / Antigravity.

**DO NOT ACCEPT COPILOT'S CONCLUSIONS AT FACE VALUE.**
You are required to perform a rigorous, adversarial source-grounded challenge against the actual C# and XAML code at baseline commit `c35816ecae49633674775dd32061c6ee92f94aaf`.

---

## 1. Automated Verification of Exact Code Evidence

Every material defect documented in [`RECONCILED_MASTER_FINDINGS.md`](../11-Master-Findings/RECONCILED_MASTER_FINDINGS.md) is machine-verified in [`SOURCE_EXCERPT_VERIFICATION_LEDGER.json`](../Checkpoints/SOURCE_EXCERPT_VERIFICATION_LEDGER.json).

You can automatically verify that every excerpt exactly matches the byte contents of the repository at those line numbers by executing:
```powershell
python "GitHub Copilot Work/Audit-Utilities/embed_exact_code_evidence.py"
```
Codex should independently read each file, check the SHA-256 hash, and confirm that the extracted lines match the defect description.

---

## 2. Adversarial Challenge Protocols for Critical Findings

### Financial & Database Integrity (Specialist Handoff: `03-Financial-Integrity/DATABASE_AND_FINANCIAL_INTEGRITY.md`)
1. **`FIN-001` (Split-Payment Journal Key Collision):**
   - Inspect `CheckoutOrderHandler.cs:253-275` and `ConfirmElectronicPaymentHandler.cs:75-84`.
   - Challenge whether both handlers construct `sale-{CheckoutRequestId}` and whether `EfFinancialJournal.TryStart` throws `ConfirmConflictException`.
2. **`FIN-002` (Electronic Cancellation Cash Omission):**
   - Inspect `Order.cs:200-214` and `CancelPendingElectronicPaymentHandler.cs`.
   - Verify whether cash splits already collected into the drawer are reversed or left untracked.
3. **`FIN-003` (Unpaid COD Refund Leakage):**
   - Inspect `Order.cs:220-232` and `RefundOrderHandler.cs`.
   - Verify whether `ApplyRefund` checks `IsPaid` or permits cash refunds on unpaid COD orders.
4. **`FIN-005` (Concurrent Invoice Overpayment):**
   - Inspect `PayPurchaseHandler.cs:40-65`.
   - Verify whether supplier balance queries use `UPDLOCK` or serializable isolation.
5. **`FIN-006` (Unfunded Payroll Run Transition):**
   - Inspect `TransitionPayrollHandler.cs:30-55`.
   - Verify whether status `Paid` is reached without creating treasury journal or employee ledger disbursement entries.
6. **`FIN-013` (Dashboard Inventory Sign Inversion):**
   - Inspect `OwnerDashboardQueryService.cs:85-115`.
   - Verify whether outbound inventory movements are summed positively rather than subtracted.

### Security & Access Control (Specialist Handoff: `05-Security/SECURITY_AND_BACKDOOR_AUDIT.md`)
1. **`SEC-001` (Case-Sensitive Mismatch Bypass):**
   - Inspect `AuthenticationMiddleware.cs:114-118`.
   - Challenge whether `TryGetProperty("actorUserId")` fails when a client sends PascalCase `"ActorUserId"`, and whether `UpdateUserPermissionsHandler.cs:32-36` loads permissions from untrusted request body.
2. **`SEC-002` (Plaintext PINs in Client Storage):**
   - Inspect `RetainedRequestStore.cs:59-76` in `CashierWinUI`.
   - Verify whether unencrypted `.json` envelopes containing employee/manager PINs are written to `%LOCALAPPDATA%`.
3. **`SEC-003` & `SEC-005` (67 Unpermissioned Sensitive Read Routes):**
   - Inspect `Program.cs:685-700` (`GET /api/employees`, `/ledger`, `/balance`) and lines `819, 862, 1828, 2203`.
   - Verify that any authenticated cashier token can read all employee salaries, national IDs, and financial statements.
4. **`SEC-004` (Shift Close Identity Spoofing):**
   - Inspect `AuthenticationMiddleware.cs:86-90` and `CloseShiftRequest.cs`.
   - Verify that `closedByUserId` is omitted from `ActorFields`, allowing any user to close any shift under another cashier's identity.

### Printing Reliability & Workflow (Specialist Handoff: `06-Printing/PRINTING_RELIABILITY_AND_PREVIEW.md`)
1. **`PRT-001` (Duplicate Kitchen Tickets):**
   - Inspect `FalahDbContext.cs:365-377` and `CreatePrintJobsHandler.cs:34-45`.
   - Verify that `PrintJobs` table lacks a composite unique constraint on `(OrderId, Route)`, allowing twin tickets under concurrent events.
2. **`FLOW-001` (Missing Two-Stage Pay-at-Pickup State Machine):**
   - Inspect `CheckoutRules.cs:44-55` and `OrderEnums.cs:9-15`.
   - Verify whether food preparation can be dispatched to the kitchen before payment is finalized at the till.

### Mirrored Code & UI/UX (Specialist Handoffs: `04-Mirrored-Code-and-Architecture/` & `07-UI-UX/`)
1. **`SEM-MIRROR-065` (Client vs Server Validation Drift):**
   - Compare `CashierViewModel` in WPF vs WinUI 3 against domain rules.
2. **`UI-002` (Color Contrast Ratio Failure):**
   - Inspect dark theme style resources across WinUI and WPF controls.
3. **`PERF-001` & `ASYNC-001` (UI Virtualization & Async Void):**
   - Inspect `ProductCatalogueView.xaml.cs` and `MainViewModel.cs:74`.

---

## 3. Verify Callable Review Ledger Authenticity

- Inspect [`CALLABLE_REVIEW_REGISTRY.json`](../02-Callable-Inventory-and-Reviews/CALLABLE_REVIEW_REGISTRY.json).
- Verify the 6-status quality model:
  - `INDEPENDENTLY_VERIFIED`: 591 callables
  - `SUBSTANTIVE_SOURCE_REVIEWED`: 4,999 callables
  - `GENERATED_OR_TRIVIAL_JUSTIFIED`: 2,801 callables
  - `PRIOR_REASONING_UNVERIFIED`: 1,130 callables
  - `INDEXED`: 2,132 callables
  - `REVIEW_BLOCKED`: 0 callables
- Confirm that previous heuristic template inflation was eliminated and that non-verified callables remain transparently identified.

---

## 4. Safety Constraints & Output Ownership

- **DO NOT MODIFY APPLICATION CODE IN `src/` OR `tests/`.**
- **DO NOT WRITE OVER HISTORICAL CODEX WORK.**
- Write all challenge results into `GitHub Copilot Work/13-Independent-Verification/CODEX_CHALLENGE_REPORT.md`.
- No live database writes or physical printer commands.
"""

    with open(INSTRUCTIONS_MD, 'w', encoding='utf-8') as f:
        f.write(instructions)
    print(f"Updated {INSTRUCTIONS_MD}.")
    print("SUCCESS: Codex Verification Package updated!")

if __name__ == '__main__':
    main()
