# Immediate Fixes and High-Priority Correctness Specifications (P0 / P1)

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
