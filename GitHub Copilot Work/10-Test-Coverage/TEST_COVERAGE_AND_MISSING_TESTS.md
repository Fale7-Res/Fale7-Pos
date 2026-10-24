# Test Coverage, Gap Analysis, and Missing Integration Tests Audit

- **Audit Target:** Existing Test Suites (Unit, Integration, UI) & Unverified Production Behaviors
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit
- **Status:** Complete Test Suite Mapping & Specification of Missing Regression Suites.

---

## 1. Existing Test Inventory

The repository contains three test projects comprising **96 test files** and **2,439 test callables**:
1. **`tests/Falah.Domain.Tests` (557 callables):**
   Pure unit tests covering domain rules: `MoneyTests.cs`, `CheckoutRulesTests.cs`, `DeliveryRulesTests.cs`, `PayrollRulesTests.cs`, `PricingRulesTests.cs`.
2. **`tests/Falah.Server.IntegrationTests` (1,592 callables):**
   ASP.NET Core `WebApplicationFactory` vertical slice integration tests against SQL Server LocalDB.
3. **`tests/Falah.Client.Tests` (290 callables):**
   ViewModel unit tests covering cashier cart interactions and state transitions.

---

## 2. Forensic Test Gaps & Uncovered Critical Paths

While existing unit tests verify pure calculations in isolation, **cross-aggregate transactional workflows are completely untested**:
1. **Mixed-Payment Checkout & Confirmation Gap:** Zero integration tests exist executing a split cash/electronic checkout followed by `ConfirmElectronicPaymentHandler`. This explains why `FIN-001` (100% crash on electronic confirmation due to journal key collision) survived into the baseline undetected!
2. **Unpaid Order Refund Gap:** Zero tests attempt to execute `RefundOrderHandler` against an unpaid COD delivery order or pending electronic order (`FIN-003`).
3. **Concurrent Invoice Settlement Gap:** Zero multi-threaded concurrency tests verify supplier invoice settlements under concurrent requests (`FIN-005`).
4. **Printer Queue Collision Gap:** Zero tests verify multi-workstation kitchen ticket dispatch under concurrent orders (`PRT-001`).

---

## 3. Required Missing Test Specifications

To guarantee system stability during the subsequent fix phase, the following regression tests must be implemented:
1. `MixedPaymentConfirmationIntegrationTests.cs`:
   Assert that split-payment orders can confirm their electronic leg without journal key collision.
2. `UnpaidDeliveryRefundRejectionTests.cs`:
   Assert that calling refund on an order with `IsPaid == false` throws `DomainException`.
3. `OwnerDashboardWasteStockSubtractionTests.cs`:
   Assert that outbound waste movements decrease aggregate stock balance.
4. `ConcurrentBusinessDayOpenTests.cs`:
   Assert that simultaneous business-day open requests result in exactly one active business day.
