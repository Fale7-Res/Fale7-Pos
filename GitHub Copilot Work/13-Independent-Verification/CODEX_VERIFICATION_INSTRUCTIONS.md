# OpenAI Codex Independent Challenge & Verification Instructions

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
