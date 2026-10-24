import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('GitHub Copilot Work/Imported-Codex-Work/Deep-Audit/MASTER_FINDINGS.json', 'r', encoding='utf-8') as f:
    codex_data = json.load(f)
    codex_map = {f['id']: f for f in codex_data.get('findings', [])}

with open('GitHub Copilot Work/Imported-Codex-Work/Sub-Agents/Results/ui-findings.json', 'r', encoding='utf-8') as f:
    ui_map = {u['id']: u for u in json.load(f).get('findings', [])}

with open('GitHub Copilot Work/Imported-Codex-Work/Sub-Agents/Results/printing-findings.json', 'r', encoding='utf-8') as f:
    prt_map = {p['id']: p for p in json.load(f)}

with open('GitHub Copilot Work/11-Master-Findings/RECONCILED_MASTER_FINDINGS.json', 'r', encoding='utf-8') as f:
    copilot_data = json.load(f)

GENERIC_IMPACT = "Potential data inconsistency or operational failure."
GENERIC_REM = "Modify affected handler to enforce proper invariant and transaction boundaries."
GENERIC_TESTS = ["Unit test", "Integration test with rollback"]
GENERIC_ACCEPT = "Invariant holds under retries and concurrent executions."

for f in copilot_data.get('findings', []):
    fid = f['id']
    c_find = codex_map.get(fid, {})
    src_rec = c_find.get('source_record', {})
    
    # Priority for implementation readiness: top level, or nested in source_record
    imp_read = c_find.get('implementation_readiness') or src_rec.get('implementation_readiness') or {}
    src_ev = src_rec.get('source_evidence') or {}
    
    # Check specialized maps
    ui_item = ui_map.get(fid, {})
    prt_item = prt_map.get(fid, {})
    
    # 1. Root Cause
    cand_rc = (
        ui_item.get('rootCause') or
        prt_item.get('root_cause') or
        imp_read.get('root_cause') or
        src_rec.get('root_cause') or
        src_ev.get('root_cause')
    )
    if cand_rc and (f.get('root_cause') == f.get('title') or not f.get('root_cause')):
        f['root_cause'] = cand_rc
        
    # 2. Impact
    cand_impact = (
        ui_item.get('impact') or
        prt_item.get('existing_behavior') or
        imp_read.get('impact') or
        src_rec.get('existing_behavior') or
        src_rec.get('impact') or
        src_ev.get('impact') or
        src_ev.get('behavior')
    )
    if cand_impact and (f.get('impact') == GENERIC_IMPACT or not f.get('impact')):
        f['impact'] = cand_impact
        
    # 3. Remediation Plan
    cand_rem = (
        ui_item.get('recommendation') or
        prt_item.get('recommendation') or
        imp_read.get('proposed_remediation') or
        imp_read.get('proposed_fix_or_owner_decision') or
        src_rec.get('correction_design') or
        src_rec.get('recommendation') or
        src_ev.get('proposed_fix_or_owner_decision') or
        src_ev.get('recommended_remediation')
    )
    if cand_rem and (f.get('remediation_plan') == GENERIC_REM or not f.get('remediation_plan')):
        f['remediation_plan'] = cand_rem
        
    # 4. Required Tests
    cand_tests = (
        ui_item.get('regressionTests') or
        prt_item.get('tests') or
        imp_read.get('regression_tests') or
        src_rec.get('regression_tests') or
        src_rec.get('tests') or
        src_ev.get('regression_tests') or
        src_ev.get('required_tests')
    )
    if cand_tests and (f.get('required_tests') == GENERIC_TESTS or not f.get('required_tests')):
        if isinstance(cand_tests, list):
            f['required_tests'] = cand_tests
        elif isinstance(cand_tests, str):
            f['required_tests'] = [cand_tests]
            
    # 5. Acceptance Criteria
    cand_acc = (
        imp_read.get('acceptance_criteria') or
        src_rec.get('acceptance_criteria') or
        src_ev.get('acceptance_criteria')
    )
    if cand_acc and (f.get('acceptance_criteria') == GENERIC_ACCEPT or not f.get('acceptance_criteria')):
        if isinstance(cand_acc, list):
            f['acceptance_criteria'] = cand_acc
        elif isinstance(cand_acc, str):
            f['acceptance_criteria'] = [cand_acc]

    # 6. Source line numbers / evidence ranges
    evidence_ranges_map = {}
    for r in (imp_read.get('source_references') or src_rec.get('sources') or []):
        p = r.get('path')
        ranges = r.get('evidence_ranges') or ( [r.get('lines')] if r.get('lines') else [] )
        if p and ranges:
            evidence_ranges_map[p] = ranges
            
    if ui_item.get('evidence'):
        for ev in ui_item['evidence']:
            p = ev.get('path')
            if p and ev.get('startLine') and ev.get('endLine'):
                evidence_ranges_map[p] = [[ev['startLine'], ev['endLine']]]

    for sref in f.get('source_references', []):
        p = sref.get('path')
        if p in evidence_ranges_map and not sref.get('lines'):
            sref['lines'] = evidence_ranges_map[p]

# Special overrides for the 11 verified critical findings:
for f in copilot_data.get('findings', []):
    fid = f['id']
    if fid == 'SEC-001':
        f['root_cause'] = "In AuthenticationMiddleware.cs (lines 86-133), FindActorMismatchAsync uses case-sensitive TryGetProperty(\"actorUserId\", out _) against lowercase string names. An incoming JSON payload with PascalCase 'ActorUserId' silently bypasses the mismatch check. Minimal API model binding in Program.cs (lines 1951-1958) binds case-insensitively into UpdateUserPermissionsRequest.ActorUserId. UpdateUserPermissionsHandler.cs (lines 32-36) queries DB for request.ActorUserId and authorizes the operation using that user's permissions instead of context.ActorId() from the verified bearer session token."
        f['impact'] = "Source-Proven: Complete privilege escalation. Any authenticated user holding a valid session token (even cashier or low-privilege employee) can grant themselves arbitrary permissions (including users_manage, orders_refund, all) by supplying 'ActorUserId': 'usr-admin' in PascalCase on POST /api/users/{id}/permissions. (Prior claim of arbitrary JSON Actor object injection was inaccurate and is refuted by wire DTO definitions)."
        f['remediation_plan'] = "1. In AuthenticationMiddleware.cs, parse request body using case-insensitive property name matching (e.g. EnumerateObject() with StringComparison.OrdinalIgnoreCase).\n2. Refactor Program.cs endpoint and UpdateUserPermissionsHandler to eliminate ActorUserId from wire DTOs and bind authorized caller identity exclusively from context.ActorId() established by token session resolution."
        f['required_tests'] = [
            "Submit POST /api/users/{targetId}/permissions with token for low-privilege cashier and body containing 'ActorUserId': 'usr-admin'; assert HTTP 403 Forbidden.",
            "Verify case variations ('actorUserId', 'ActorUserId', 'ACTORUSERID') all trigger actor mismatch rejection in middleware.",
            "Verify UpdateUserPermissionsHandler authorizes against authenticated session user even if body attempts to provide ActorUserId."
        ]
        f['acceptance_criteria'] = [
            "AuthenticationMiddleware rejects any mismatched actor identity regardless of JSON key casing.",
            "Handlers never read or trust client-supplied actor user ID from request body."
        ]
        f['source_references'] = [
            {"path": "src/Falah.Server/AuthenticationMiddleware.cs", "exists_in_tree": True, "lines": [[86, 133]]},
            {"path": "src/Falah.Server/Program.cs", "exists_in_tree": True, "lines": [[1951, 1958]]},
            {"path": "src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs", "exists_in_tree": True, "lines": [[32, 36]]},
            {"path": "src/Falah.Application/Users/UserManagementRequests.cs", "exists_in_tree": True, "lines": [[1, 20]]}
        ]
        f['copilot_disposition'] = "INDEPENDENTLY_CORROBORATED_AND_CORRECTED"
        f['disposition_rationale'] = "Forensically traced through middleware, JSON parsing, Minimal API model binding, and command handler. Prior Codex claim of arbitrary JSON Actor object injection corrected to case-sensitive TryGetProperty mismatch bypass."
    elif fid == 'SEC-002':
        f['root_cause'] = "FileRetainedRequestStore.cs in CashierWinUI and CallCenterWinUI writes raw in-flight request payloads to %LOCALAPPDATA%\\Falah\\pending-operations\\{station}\\{ownerKey}.json as unencrypted plaintext JSON. User creation, PIN changes, and manager overrides that run while offline or mid-dispatch persist plaintext PINs on disk."
        f['impact'] = "Workstation credential exposure. Anyone with read access to the local machine's user directory (%LOCALAPPDATA%) can read persistent and in-flight employee and manager PINs in plaintext."
        f['remediation_plan'] = "Encrypt retained request payloads using Windows Data Protection API (ProtectedData.Protect with CurrentUser scope) prior to file write. Mask or redact PIN properties from retained request diagnostic envelopes."
        f['required_tests'] = [
            "Execute CreateUser or PIN change request in CashierWinUI while offline; inspect persisted JSON in %LOCALAPPDATA%; assert payload is ciphertext and PIN is unreadable in plaintext."
        ]
        f['copilot_disposition'] = "INDEPENDENTLY_CORROBORATED_AND_CORRECTED"
    elif fid == 'SEC-003':
        f['root_cause'] = "GET /api/employees and GET /api/employees/{employeeId} in Program.cs (lines 685-687) have no permission policy attribute. EmployeeQueryService.cs (lines 81-85) unconditionally projects all database columns (NationalId, DailyWagePiastres, MonthlySalaryPiastres, OvertimeRatePiastres, FingerprintId, Address, Phone) into EmployeeDto."
        f['impact'] = "Internal data exposure. Any authenticated terminal session (including frontline cashiers, waiters, delivery drivers) can query the exact salaries, overtime rates, national IDs, and home addresses of all restaurant staff."
        f['remediation_plan'] = "Introduce PublicEmployeeSummaryDto for operational cashier selection. Restrict GET /api/employees full profile to callers possessing 'employees_manage' or 'payroll_view' permissions."
        f['required_tests'] = [
            "Query GET /api/employees with cashier bearer token; assert response contains only public summary fields and omits salary/national ID.",
            "Query GET /api/employees with manager bearer token; assert full profile is returned."
        ]
        f['copilot_disposition'] = "INDEPENDENTLY_CORROBORATED"
    elif fid == 'FIN-001':
        f['root_cause'] = "CheckoutOrderHandler.cs (line 148) posts the cash tender leg using idempotency key 'sale-{CheckoutRequestId}'. When ConfirmElectronicPaymentHandler.cs (line 75) attempts to post the deferred electronic split leg, it attempts to insert into EfFinancialJournal using the exact same key 'sale-{CheckoutRequestId}', throwing a unique constraint violation."
        f['impact'] = "Split payment completion failure. Cash is accepted and deposited into the drawer, but the electronic leg (InstaPay/Card) can never be confirmed on the server; the order remains stuck in pending payment state."
        f['remediation_plan'] = "Key journal entries by stable leg identity, e.g. 'sale-{CheckoutRequestId}-{TenderType}'. Transition pending electronic leg atomically to confirmed."
        f['copilot_disposition'] = "INDEPENDENTLY_CORROBORATED"
    elif fid == 'FIN-002':
        f['root_cause'] = "Order.cs:CancelElectronicPayment sets order Status to Cancelled without issuing a reversal journal entry for cash tender legs that were already deposited into the till drawer at checkout."
        f['impact'] = "Treasury divergence. Drawer cash remains physically and logically deposited, but order disappears from sales reports, causing shift drawer reconciliation discrepancies."
        f['remediation_plan'] = "When cancelling a mixed tender order, check if any cash leg was deposited and atomically post a reversal journal movement to the drawer."
        f['copilot_disposition'] = "INDEPENDENTLY_CORROBORATED"
    elif fid == 'FIN-003':
        f['root_cause'] = "Order.cs:ApplyRefund and RefundOrderHandler.cs do not verify that the order was actually settled (IsPaid == true) before approving a cash refund."
        f['impact'] = "Cash drain. An operator can issue a cash refund against an unpaid Cash-On-Delivery order or pending InstaPay order, draining drawer funds for money never collected."
        f['remediation_plan'] = "Enforce invariant in Order.ApplyRefund that order must be fully collected (IsPaid == true) and refund amount cannot exceed collected tender."
        f['copilot_disposition'] = "INDEPENDENTLY_CORROBORATED"
    elif fid == 'FIN-005':
        f['root_cause'] = "PayPurchaseHandler.cs (lines 59-74) reads supplier invoice balance without UPDLOCK or serializable isolation, allowing concurrent transactions to each read the remaining balance before either updates it."
        f['impact'] = "Supplier overpayment and invoice balance corruption under concurrent settlement clicks."
        f['remediation_plan'] = "Execute invoice settlement within an explicit Serializable transaction or apply pessimistic locking (UPDLOCK, ROWLOCK) on the purchasing invoice record."
        f['copilot_disposition'] = "INDEPENDENTLY_CORROBORATED"
    elif fid == 'FIN-006':
        f['root_cause'] = "TransitionPayrollHandler.cs permits generic status transition of an approved payroll run directly to 'Paid' without generating corresponding treasury journal disbursements or employee ledger settlement entries."
        f['impact'] = "Payroll status marked Paid without actual money leaving treasury or settling employee balances."
        f['remediation_plan'] = "Disallow transition to Paid via generic TransitionPayrollHandler. Require execution exclusively through PayPayrollHandler which generates atomic treasury and ledger entries."
        f['copilot_disposition'] = "INDEPENDENTLY_CORROBORATED"
    elif fid == 'FIN-013':
        f['root_cause'] = "OwnerDashboardQueryService.cs calculates inventory stock movements by summing QuantityMilliUnits without multiplying by -1 when Direction == Out."
        f['impact'] = "Inventory stock overstatement on owner dashboard. Waste and sales increase stock totals rather than decreasing them."
        f['remediation_plan'] = "Apply proper sign negation: (m.Direction == StockMovementDirection.Out ? -m.QuantityMilliUnits : m.QuantityMilliUnits)."
        f['copilot_disposition'] = "INDEPENDENTLY_CORROBORATED"
    elif fid == 'FIN-016':
        f['root_cause'] = "CheckoutOrderHandler.cs (line 142) accepts request.DeliveryFeePiastres directly from client payload without verifying against DeliveryService.DefaultFee or checking manager authorization."
        f['impact'] = "Delivery fee tampering. A client can supply negative or zero delivery fee to discount an order without triggering manager discount authorization."
        f['remediation_plan'] = "Validate delivery fee server-side against configured delivery rates or require manager override for non-standard delivery fees."
        f['copilot_disposition'] = "INDEPENDENTLY_CORROBORATED"
    elif fid == 'PRT-001':
        f['root_cause'] = "CreatePrintJobsHandler.cs generates job ID using request.RequestId, and PrintJobs table in FalahDbContext lacks a composite unique constraint on (OrderId, Route)."
        f['impact'] = "Duplicate kitchen and receipt tickets printed when multiple stations submit print requests for the same order."
        f['remediation_plan'] = "Add composite unique index on (OrderId, Route, DocumentKind) in PrintJobEntity configuration, or ensure idempotent deduplication at database level."
        f['copilot_disposition'] = "INDEPENDENTLY_CORROBORATED"
    elif fid == 'FLOW-001':
        f['root_cause'] = "Creation, stock consumption and payment checkout share one use case (CheckoutOrderHandler); only Delivery Cash has explicit deferred cash rule. No state machine exists for pay-on-collection pickup orders."
        f['impact'] = "Operational gap: Kitchen cannot be notified to prepare pickup orders before payment has been taken in full, blocking customer phone/advance pickup workflows."
        f['remediation_plan'] = "Design separate pickup fulfilment state and payment state with server-authoritative transitions (two-stage reserve-and-prep -> pay-and-handover)."
        f['copilot_disposition'] = "INDEPENDENTLY_CORROBORATED"

with open('GitHub Copilot Work/11-Master-Findings/RECONCILED_MASTER_FINDINGS.json', 'w', encoding='utf-8') as f:
    json.dump(copilot_data, f, indent=2, ensure_ascii=False)

# Check placeholders remaining
f_list = copilot_data.get('findings', [])
p_impact = sum(1 for f in f_list if f.get('impact') == GENERIC_IMPACT)
p_rem = sum(1 for f in f_list if f.get('remediation_plan') == GENERIC_REM)
p_tests = sum(1 for f in f_list if f.get('required_tests') == GENERIC_TESTS)
print(f"Post-enrichment status across 77 findings:")
print(f"Generic impact remaining: {p_impact}")
print(f"Generic remediation remaining: {p_rem}")
print(f"Generic tests remaining: {p_tests}")
