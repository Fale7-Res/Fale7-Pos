"""
GOVERNANCE CORRECTION SCRIPT: Revoke Unsupported INDEPENDENTLY_VERIFIED Promotions
===================================================================================
Run date: 2026-10-25
Owner Directive: Per /goal instruction, the 3,039 INDEPENDENTLY_VERIFIED callables
were promoted via file-path membership in l2_verified_paths (strict_callable_acceptance_gate.py
lines 245-260), not by per-callable independent semantic analysis.

This script:
1. Demotes all INDEPENDENTLY_VERIFIED callables that were granted status solely via
   file-path-based bulk promotion to SUBSTANTIVE_SOURCE_REVIEWED (preserving Markdown evidence).
2. Retains the small set of callables that were individually and explicitly verified
   by Codex independent challenge (codex_verified_ids hardcoded in the acceptance gate).
3. Preserves all existing Markdown evidence and adds an audit-history record of the correction.
4. Does NOT delete any existing QA decisions or Markdown content.
5. Rebuilds accurate summary statistics.

NOTE: This is a MECHANICAL STATUS CORRECTION, not a new source analysis.
Real semantic QA must be completed by specialist sub-agents afterward.
"""

import json
import os
import hashlib
from datetime import datetime, timezone

now_iso = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

# These are the ONLY callables that had genuine per-callable independent Codex challenge
# (as recorded in the hardcoded codex_verified_ids set in strict_callable_acceptance_gate.py)
CODEX_INDIVIDUALLY_VERIFIED = {
    "src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:74:5:MethodDeclaration",
    "src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:66:5:MethodDeclaration",
    "src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:59:5:MethodDeclaration",
    "src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:317:5:MethodDeclaration",
    "src/Falah.Domain/Payroll/PayrollRules.cs:36:5:MethodDeclaration",
    "src/Falah.Domain/Payroll/PayrollRules.cs:44:5:MethodDeclaration",
    "src/Falah.Domain/Payroll/PayrollRules.cs:55:5:MethodDeclaration",
    "src/Falah.Domain/Payroll/PayrollRules.cs:66:5:MethodDeclaration",
    "src/Falah.Domain/Payroll/PayrollRules.cs:83:5:MethodDeclaration",
    "src/Falah.Domain/Payroll/PayrollRules.cs:116:5:MethodDeclaration",
    "src/Falah.Infrastructure/CashDrawer/CashDrawerHandlers.cs:31:5:MethodDeclaration",
    "src/Falah.Infrastructure/CashDrawer/CashDrawerHandlers.cs:96:5:MethodDeclaration",
    "src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs:40:5:MethodDeclaration",
    "src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:34:5:MethodDeclaration",
    "src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:29:5:MethodDeclaration",
    "src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:29:5:MethodDeclaration",
    "src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:36:5:MethodDeclaration",
    "src/Falah.Infrastructure/Delivery/DeliveryServiceHandlers.cs:26:5:MethodDeclaration",
    "src/Falah.Infrastructure/Delivery/DeliveryServiceHandlers.cs:89:5:MethodDeclaration",
    "src/Falah.Infrastructure/Delivery/ReassignDeliveryHandler.cs:34:5:MethodDeclaration",
    "src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs:45:5:MethodDeclaration",
    "src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:36:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/AttendanceHandlers.cs:26:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/AttendanceHandlers.cs:110:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs:30:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs:29:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs:26:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs:79:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:61:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs:46:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:39:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs:33:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs:77:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs:125:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/RecordEmployeeCashOutHandler.cs:38:5:MethodDeclaration",
    "src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs:28:5:MethodDeclaration",
    "src/Falah.Infrastructure/Inventory/InventoryItemHandlers.cs:25:5:MethodDeclaration",
    "src/Falah.Infrastructure/Inventory/InventoryItemHandlers.cs:73:5:MethodDeclaration",
    "src/Falah.Infrastructure/Inventory/SaveOrderPackagingRuleHandler.cs:25:5:MethodDeclaration",
    "src/Falah.Infrastructure/Inventory/SavePrepRecipeHandler.cs:25:5:MethodDeclaration",
    "src/Falah.Infrastructure/Inventory/SaveRecipeHandler.cs:31:5:MethodDeclaration",
    "src/Falah.Infrastructure/Inventory/SetEstimatedCostHandler.cs:40:5:MethodDeclaration",
    "src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs:32:5:MethodDeclaration",
    "src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs:106:5:MethodDeclaration",
    "src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs:31:5:MethodDeclaration",
    "src/Falah.Infrastructure/Purchasing/SetPurchaseDueDateHandler.cs:34:5:MethodDeclaration",
    "src/Falah.Infrastructure/Reports/DailyStatementQueryService.cs:60:5:MethodDeclaration",
    "src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:111:5:MethodDeclaration",
    "src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:134:5:MethodDeclaration",
    "src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:158:5:MethodDeclaration",
    "src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:171:5:MethodDeclaration",
    "src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:191:5:MethodDeclaration",
    "src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:205:5:MethodDeclaration",
    "src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:297:5:MethodDeclaration",
    "src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:317:5:MethodDeclaration",
    "src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:358:5:MethodDeclaration",
    "src/Falah.Infrastructure/Settings/UpdateSettingsHandler.cs:45:5:MethodDeclaration",
    "src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs:47:5:MethodDeclaration",
    "src/Falah.Infrastructure/Shifts/PrepareShiftCloseHandler.cs:32:5:MethodDeclaration",
    "src/Falah.Infrastructure/Treasury/DepositHandler.cs:31:5:MethodDeclaration",
    "src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:33:5:MethodDeclaration",
    "src/Falah.PrintAgent/PrintServerClient.cs:33:5:MethodDeclaration",
    "src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:29:5:MethodDeclaration",
    "src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:37:5:MethodDeclaration",
    "src/Falah.Server/AuthenticationMiddleware.cs:91:5:MethodDeclaration",
}

def main():
    reg_path = "GitHub Copilot Work/02-Callable-Inventory-and-Reviews/CALLABLE_REVIEW_REGISTRY.json"
    stats_path = "GitHub Copilot Work/02-Callable-Inventory-and-Reviews/CALLABLE_SUMMARY_STATISTICS.json"
    coverage_path = "GitHub Copilot Work/Checkpoints/CALLABLE_COVERAGE.json"
    root_coverage_path = "GitHub Copilot Work/AUDIT_COVERAGE.json"
    
    print(f"Loading registry...")
    with open(reg_path, "r", encoding="utf-8") as f:
        reg = json.load(f)
    
    total = len(reg["records"])
    revoked = 0
    retained_iv = 0
    
    for r in reg["records"]:
        cid = r["id"]
        cur_status = r.get("status")
        
        if cur_status != "INDEPENDENTLY_VERIFIED":
            continue
        
        # Check if this callable has genuine per-callable Codex challenge
        if cid in CODEX_INDIVIDUALLY_VERIFIED:
            retained_iv += 1
            r["evidence_level"] = "CODEX_INDIVIDUALLY_CHALLENGED"
            continue
        
        # All others were promoted via file-path bulk promotion - REVOKE
        r["prior_status_before_revocation"] = "INDEPENDENTLY_VERIFIED"
        r["status"] = "SUBSTANTIVE_SOURCE_REVIEWED"
        r["evidence_level"] = "SPECIALIST_SOURCE_AUDITED_FILE_LEVEL_QA_ONLY"
        r.setdefault("audit_history", []).append({
            "action": "BULK_IV_REVOCATION",
            "previous_status": "INDEPENDENTLY_VERIFIED",
            "new_status": "SUBSTANTIVE_SOURCE_REVIEWED",
            "reason": (
                "Revoked: INDEPENDENTLY_VERIFIED was granted by strict_callable_acceptance_gate.py "
                "via file-path membership in l2_verified_paths (wave QA scripts). "
                "This is a file-level batch approval, NOT a per-callable independent semantic challenge. "
                "Callable retains Markdown evidence and SUBSTANTIVE_SOURCE_REVIEWED status. "
                "Real QA L1/L2 per-callable semantic review remains required for re-elevation."
            ),
            "timestamp_utc": now_iso,
            "corrected_by": "revoke_unsupported_iv_promotions.py",
            "authorized_by": "Owner /goal directive 2026-10-25",
        })
        # Clear the flawed acceptance_gate record 
        r["acceptance_gate"] = {
            "verdict": "REVOKED_BULK_FILE_PATH_PROMOTION",
            "revoked_at": now_iso,
            "original_verdict": "ACCEPTED_INDEPENDENTLY_VERIFIED",
            "reason": "File-path-based bulk promotion via wave QA scripts does not satisfy per-callable independent semantic challenge requirement.",
        }
        revoked += 1
    
    # Rebuild statistics
    taxonomy = {}
    for r in reg["records"]:
        st = r.get("status", "UNKNOWN")
        taxonomy[st] = taxonomy.get(st, 0) + 1
    
    total_callables = len(reg["records"])
    
    # Save registry
    with open(reg_path, "w", encoding="utf-8") as f:
        json.dump(reg, f, indent=2)
    print(f"Registry updated. Revoked: {revoked}, Retained genuine IV: {retained_iv}")
    
    # Update stats
    stats = {
        "timestamp_utc": now_iso,
        "governance_correction": "BULK_IV_REVOCATION_APPLIED",
        "correction_details": f"Revoked {revoked} bulk file-path IV promotions. Retained {retained_iv} genuinely individually Codex-challenged callables as INDEPENDENTLY_VERIFIED.",
        "total_callables": total_callables,
        "source_scope_counts": {
            "production_src": sum(1 for r in reg["records"] if not r["path"].startswith("tests/") and not r["path"].startswith("tools/")),
            "tests": sum(1 for r in reg["records"] if r["path"].startswith("tests/")),
            "tools": sum(1 for r in reg["records"] if r["path"].startswith("tools/")),
        },
        "taxonomy": taxonomy,
        "percentages": {k: round(v / total_callables * 100, 2) for k, v in taxonomy.items()},
        "acceptance_gate": {
            "retained_individually_codex_verified": retained_iv,
            "revoked_bulk_file_path_iv": revoked,
            "accepted_substantive_source_reviewed": taxonomy.get("SUBSTANTIVE_SOURCE_REVIEWED", 0),
            "preserved_generated_or_trivial": taxonomy.get("GENERATED_OR_TRIVIAL_JUSTIFIED", 0),
            "preserved_indexed_tests_tools": taxonomy.get("INDEXED", 0),
            "prior_reasoning_unverified": taxonomy.get("PRIOR_REASONING_UNVERIFIED", 0),
            "total_requiring_real_per_callable_qa": taxonomy.get("SUBSTANTIVE_SOURCE_REVIEWED", 0),
        },
    }
    
    with open(stats_path, "w", encoding="utf-8") as f:
        json.dump(stats, f, indent=2)
    
    # Update coverage checkpoints
    coverage = {
        "timestamp_utc": now_iso,
        "total_callables": total_callables,
        "taxonomy": taxonomy,
        "percentages": stats["percentages"],
        "governance_note": "BULK_IV_REVOCATION_APPLIED — Real per-callable QA L1/L2 required for INDEPENDENTLY_VERIFIED re-elevation.",
    }
    for cp in [coverage_path, root_coverage_path]:
        with open(cp, "w", encoding="utf-8") as f:
            json.dump(coverage, f, indent=2)
    
    print("Updated stats, coverage checkpoints.")
    print(f"\nCorrected taxonomy:")
    for k, v in sorted(taxonomy.items()):
        pct = v / total_callables * 100
        print(f"  {k:35s}: {v:5d} ({pct:.2f}%)")
    
    print(f"\nTotal callables: {total_callables}")
    print(f"Callables requiring real per-callable QA: {taxonomy.get('SUBSTANTIVE_SOURCE_REVIEWED', 0)}")
    print(f"Genuinely individually Codex-verified (retained): {retained_iv}")

if __name__ == "__main__":
    main()
