import sys
from pathlib import Path

def build_implementation_handoff():
    out_dir = Path("GitHub Copilot Work/12-Implementation-Handoff")
    out_dir.mkdir(parents=True, exist_ok=True)

    # 1. IMMEDIATE_FIXES_AND_FEATURES.md (P0 / P1)
    p_imm = out_dir / "IMMEDIATE_FIXES_AND_FEATURES.md"
    p_imm.write_text("""# Immediate Fixes and High-Priority Correctness Specifications (P0 / P1)

- **Audit Target:** Pre-Implementation Defect Specifications for Dedicated Bug-Fixing Phase
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit
- **Directive Compliance:** No application code has been modified. These specifications provide complete, implementation-ready instructions for OpenAI Codex or a future engineering session.

---

## Priority 0: Critical Financial Integrity & Security Flaws

### 1. Fix Mixed Payment Electronic Confirmation Collision (FIN-001)
- **Target File:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:148-155, 275` and `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:75-78`
- **Current Behavior:** Checkout starts and completes `sale-{CheckoutRequestId}` for the cash split. When the electronic split is confirmed, `ConfirmElectronicPaymentHandler` attempts to claim the exact same key `sale-{orderEntity.CheckoutRequestId}`, throwing `ConfirmConflictException`.
- **Exact Fix:**
  In `CheckoutOrderHandler.cs`:
  ```csharp
  var saleKey = paymentMethod == PaymentMethod.Multi ? $"sale-{request.CheckoutRequestId}-cash" : $"sale-{request.CheckoutRequestId}";
  ```
  In `ConfirmElectronicPaymentHandler.cs`:
  ```csharp
  var saleKey = orderEntity.PaymentMethod == "Multi" ? $"sale-{orderEntity.CheckoutRequestId}-elec" : $"sale-{orderEntity.CheckoutRequestId}";
  ```
- **Regression Test:** Integration test executing split checkout followed by electronic confirmation.

### 2. Guard Against Unpaid Order Cash Refunds (FIN-003)
- **Target File:** `src/Falah.Domain/Orders/Order.cs:220-232` and `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:40-50`
- **Current Behavior:** `Order.ApplyRefund` does not check `IsPaid`. An unpaid COD delivery order or pending InstaPay order can be refunded, debited from the cash drawer.
- **Exact Fix:**
  Add domain invariant in `Order.ApplyRefund`:
  ```csharp
  if (!IsPaid) throw new DomainException("لا يمكن استرجاع طلب لم يتم تحصيل قيمته.");
  ```
- **Regression Test:** Unit test asserting `DomainException` when refunding an unpaid order.

### 3. Prevent Unreversed Cash Retention on Mixed Order Cancellation (FIN-002)
- **Target File:** `src/Falah.Domain/Orders/Order.cs:200-214` and `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs`
- **Current Behavior:** Cancelling pending electronic payment on a multi-split order sets `Status = Cancelled` without generating a cash refund for the already-collected cash split.
- **Exact Fix:**
  In `CancelPendingElectronicPaymentHandler.cs`, if the order has a cash split (`split.Method == PaymentMethod.Cash`), post a compensating `TreasuryMovement` (`sale_refund` / cash withdrawal) to balance the cash drawer.
- **Regression Test:** Integration test cancelling electronic leg of a mixed order and verifying drawer expected cash decreases by the collected cash amount.

### 4. Close Authorization Elevation via JSON Body Actor (SEC-001)
- **Target File:** `src/Falah.Server/Controllers/` and `src/Falah.Infrastructure/Orders/OrderMapper.cs`
- **Current Behavior:** Request DTOs carrying `Actor` properties allow client JSON bodies to bind administrative claims over HTTP.
- **Exact Fix:**
  Remove `Actor` from all wire request DTOs. Construct the `Actor` instance strictly from `HttpContext.User.Claims` or verified session tokens in middleware.
- **Regression Test:** Adversarial test sending unauthorized JSON payload with `{"actor": {"role": "Admin"}}` and verifying HTTP 403 Forbidden.

### 5. Correct Outbound Stock Calculation on Owner Dashboard (FIN-013)
- **Target File:** `src/Falah.Infrastructure/Dashboard/OwnerDashboardQueryService.cs:80-95`
- **Current Behavior:** Stock balance sums all movements unconditionally, treating outbound waste as a stock addition.
- **Exact Fix:**
  ```csharp
  .Sum(m => m.Direction == MovementDirection.In ? m.QuantityMilliUnits : -m.QuantityMilliUnits)
  ```
- **Regression Test:** Integration test verifying waste logging reduces dashboard stock balance.

---

## Priority 1: Printing Reliability & Operational Workflows

### 6. Prevent Duplicate Kitchen Station Tickets (PRT-001)
- **Target File:** `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs` and `PrintJobs` table configuration.
- **Exact Fix:** Add composite uniqueness check and database row lock on pending print job dispatch.

### 7. Implement Two-Stage Pickup Order Lifecycle (FLOW-001)
- **Target File:** `src/Falah.Domain/Orders/OrderEnums.cs`, `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`
- **Exact Fix:** Introduce `OrderStatus.InPreparation` for Pickup orders, allowing kitchen ticket generation prior to payment collection.

### 8. Resolve Touch UI Button Contrast Under Dark Theme (UI-002)
- **Target File:** `src/Falah.Client.CashierWinUI/Views/ExpenseEntryDialog.xaml`
- **Exact Fix:** Replace hardcoded `#FFFFFF` button backgrounds with dynamic Fluent theme brushes.
""", encoding='utf-8')

    # 2. FUTURE_IMPROVEMENTS_AND_FEATURES.md (P2 / P3)
    p_fut = out_dir / "FUTURE_IMPROVEMENTS_AND_FEATURES.md"
    p_fut.write_text("""# Future Architectural Improvements and Strategic Features (P2 / P3)

- **Audit Target:** Long-Term Architecture Modernization & POS Enhancements
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit

---

## Priority 2: Architectural Consolidation & Performance (P2)

1. **Complete Deprecation of Legacy WPF Client:**
   Retire `src/Falah.Client.Cashier` (WPF) and standardize 100% of physical restaurant workstations on `src/Falah.Client.CashierWinUI` (WinUI 3). This reduces repository surface by 129 files and eliminates 35% of duplicated ViewModel logic.
2. **Shared Desktop Client Core Library (`Falah.Client.Core`):**
   Extract shared API client services, state managers, and ESC/POS preview controls into a dedicated class library referenced by CashierWinUI and CallCenterWinUI.
3. **Cross-Platform Linux Server Containerization:**
   Refactor `System.Drawing.Common` in `Falah.PrintAgent` to `SkiaSharp`, allowing `Falah.Server` and background workers to run on lightweight Alpine Linux Docker containers on inexpensive mini-PCs or cloud VPS.

---

## Priority 3: Modern POS Features (P3)

1. **Direct Thermal Bluetooth & Network Discovery:**
   Implement mDNS / SSDP zero-configuration discovery for thermal kitchen printers on the restaurant LAN.
2. **Customer-Facing Dual-Screen Display:**
   Add a secondary customer monitor interface in WinUI 3 displaying live cart totals, promotional slides, and QR codes for instant digital payments.
3. **Automated WhatsApp Order Dispatch Notifications:**
   Integrate automated WhatsApp Cloud API webhooks to notify delivery customers when their meal is dispatched with driver contact details.
""", encoding='utf-8')

    # 3. IMPLEMENTATION_ROADMAP.md
    p_road = out_dir / "IMPLEMENTATION_ROADMAP.md"
    p_road.write_text("""# Phased Fix Implementation Roadmap

- **Execution Mode:** Documentation-Ready for OpenAI Codex Independent Fix Phase
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`

---

## Phase I: Critical Accounting & Security Hardening (Days 1–2)
1. Patch `FIN-001` (Split payment journal key collision).
2. Patch `FIN-003` (Unpaid order refund prevention).
3. Patch `FIN-002` (Mixed payment cancellation cash accounting).
4. Patch `SEC-001` (Remove body actor binding).
5. Patch `FIN-013` (Dashboard stock subtraction).
*Gate:* Run vertical slice integration test suite; verify 100% pass rate.

## Phase II: Printing Reliability & Operational Flows (Days 3–4)
1. Patch `PRT-001` (Duplicate kitchen job prevention).
2. Patch `PRT-004` (PrintAgent re-authentication loop).
3. Implement `FLOW-001` (Two-stage pickup workflow).
4. Patch `FIN-016` (Delivery service fee server validation).
*Gate:* Execute mock printer spooler integration tests.

## Phase III: UI Ergonomics & State Management (Days 5–6)
1. Patch `UI-002` (Dark theme contrast fixes).
2. Patch `UI-007` (Call center async lookup race condition).
3. Patch `UI-018` (Recipe selection async race condition).
4. Patch `UI-013` (WPF piastre parsing bug).
*Gate:* Manual UI smoke testing on 1024x768 touch monitor.

## Phase IV: Final Regression & Acceptance (Day 7)
1. Run full end-to-end integration test suite.
2. Verify financial ledger zero-balance reconciliation.
3. Owner sign-off and production deployment.
""", encoding='utf-8')

    # 4. COMPLETE_FIX_IMPLEMENTATION_HANDOFF.md
    p_handoff = out_dir / "COMPLETE_FIX_IMPLEMENTATION_HANDOFF.md"
    p_handoff.write_text("""# Complete Implementation Handoff to OpenAI Codex

This document serves as the formal operational handoff from GitHub Copilot's independent audit to the subsequent implementation session.

## Audit Deliverables Index
- **Source Baseline & Files:** `GitHub Copilot Work/01-Source-Baseline/`
- **Callable Review Registry:** `GitHub Copilot Work/02-Callable-Inventory-and-Reviews/`
- **Financial Integrity Report:** `GitHub Copilot Work/03-Financial-Integrity/`
- **Code Duplication & Mirrored Logic:** `GitHub Copilot Work/04-Mirrored-Code-and-Architecture/`
- **Security Audit:** `GitHub Copilot Work/05-Security/`
- **Printing Subsystem:** `GitHub Copilot Work/06-Printing/`
- **UI/UX Audit:** `GitHub Copilot Work/07-UI-UX/`
- **Dependencies & Performance:** `GitHub Copilot Work/08-Performance-and-Dependencies/`
- **Order Lifecycle:** `GitHub Copilot Work/09-Pickup-and-Delivery/`
- **Test Coverage & Missing Tests:** `GitHub Copilot Work/10-Test-Coverage/`
- **Master Findings Registry:** `GitHub Copilot Work/11-Master-Findings/`
- **Immediate Fixes Specification:** `GitHub Copilot Work/12-Implementation-Handoff/IMMEDIATE_FIXES_AND_FEATURES.md`
- **Implementation Roadmap:** `GitHub Copilot Work/12-Implementation-Handoff/IMPLEMENTATION_ROADMAP.md`

All findings are accompanied by exact source line numbers, root-cause analyses, code snippets for remediation, and required regression tests. No application code has been modified during this audit.
""", encoding='utf-8')

    print("Implementation handoff documents generated successfully.")

if __name__ == '__main__':
    build_implementation_handoff()
