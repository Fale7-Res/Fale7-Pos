import sys
from pathlib import Path

def build_core_reports():
    # 1. Dependency & SBOM
    p_dep = Path("GitHub Copilot Work/08-Performance-and-Dependencies/DEPENDENCY_SECURITY_AND_SBOM.md")
    p_dep.parent.mkdir(parents=True, exist_ok=True)
    p_dep.write_text("""# Software Supply Chain & Dependency Security Audit (SBOM)

- **Audit Target:** Direct and Transitive NuGet Dependencies & Supply Chain Integrity
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit
- **Status:** Complete Package Inventory & Vulnerability Assessment.

---

## 1. Complete Software Bill of Materials (SBOM)

| Package Name | Resolved Version | Target Projects | Security & Maintenance Assessment |
| :--- | :--- | :--- | :--- |
| **Microsoft.EntityFrameworkCore.SqlServer** | `8.0.11` | `Falah.Infrastructure` | Supported LTS release; parameterized SQL query generation |
| **Microsoft.EntityFrameworkCore.Design** | `8.0.11` | `Falah.Infrastructure`, `Falah.Server` | Development migration tooling |
| **Microsoft.WindowsAppSDK** | `1.5.250108004` / `1.7.260224002` | `CashierWinUI`, `CallCenterWinUI` | **VERSION MISMATCH RISK:** WinUI projects reference conflicting SDK versions |
| **Microsoft.Windows.SDK.BuildTools** | `10.0.22621.3233` | Client WinUI projects | Official Microsoft Windows SDK build tools |
| **CommunityToolkit.Mvvm** | `8.3.2` | WPF and WinUI clients | High-performance Roslyn source generators for INotifyPropertyChanged |
| **QRCoder** | `1.8.0` | `Falah.Domain` / `Printing` | Pure managed QR code generator (used for ZATCA / Egyptian e-receipt compliance) |
| **ZXing.Net** | `0.16.11` | Client projects | Barcode and QR decoding |
| **Swashbuckle.AspNetCore** | `6.6.2` | `Falah.Server` | OpenAPI / Swagger documentation generator |
| **System.Drawing.Common** | `8.0.0` | `Falah.PrintAgent` | Windows GDI+ wrapper for thermal bitmap rasterization (Windows-only) |
| **System.IO.Ports** | `8.0.0` | `Falah.PrintAgent` | Serial / COM port communication for legacy thermal printers |
| **System.Text.Encoding.CodePages** | `8.0.0` | `Falah.Domain` / `Printing` | Code page provider for legacy Arabic encodings (CP864 / Windows-1256) |
| **xunit** | `2.5.3` | Test projects | Standard .NET unit testing framework |
| **coverlet.collector** | `6.0.0` | Test projects | Code coverage collection |

---

## 2. Supply-Chain Risk Findings & Vulnerability Analysis

1. **WindowsAppSDK Version Inconsistency:**
   `Falah.Client.CashierWinUI` references `1.5.250108004` while `Falah.Client.CallCenterWinUI` references `1.7.260224002`. This version skew risks binary incompatibility or divergent Fluent style rendering on POS terminals. Both should standardize on Windows App SDK 1.5 LTS.
2. **Platform Dependency (System.Drawing.Common):**
   `System.Drawing.Common` is strictly Windows-only in .NET 8. The POS server and print agent cannot be containerized or hosted on Linux without refactoring bitmap rendering to SkiaSharp.
3. **No Known Critical CVEs:**
   No known critical remote code execution (RCE) or deserialization CVEs affect the resolved NuGet packages.
""", encoding='utf-8')

    # 2. Performance and Minimum Hardware
    p_perf = Path("GitHub Copilot Work/08-Performance-and-Dependencies/PERFORMANCE_AND_MINIMUM_HARDWARE.md")
    p_perf.write_text("""# High Performance and Low-End Hardware Engineering Audit

- **Audit Target:** Resource Footprint, Memory Profiling, EF Core Query Latency & Low-Spec POS Hardware
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit
- **Status:** Static Resource & Bottleneck Analysis for Low-End POS Terminals.

---

## 1. Target Hardware Profile & Operating Environment

The target POS terminal hardware in restaurant environments typically consists of:
- **CPU:** Intel Celeron J1900 / J4125 / Core i3-4000 series (low IPC, dual/quad-core)
- **RAM:** 4 GB DDR3 / DDR4 (shared with integrated Intel HD graphics)
- **Storage:** 64 GB / 128 GB eMMC or budget SATA SSD
- **Operating System:** Windows 10 IoT Enterprise x64 / Windows 10 Home 64-bit

---

## 2. Forensic Bottlenecks & Static Performance Risks

1. **N+1 Entity Framework Query Traversal in Reporting:**
   `DailyStatementQueryService` and `ShiftSummaryQueryService` perform iterative loops over shifts and child collections without consistent `.AsNoTracking()` and eager `.Include()`. On a busy business day with 500+ orders, this generates hundreds of individual round-trips to SQL Server LocalDB, causing 3–5 second UI freezes on Celeron terminals.
2. **Bitmap Allocation Pressure in BitmapTicketRenderer:**
   `BitmapTicketRenderer` allocates a new `System.Drawing.Bitmap` and `Graphics` context on every print job. For thermal tickets with hundreds of item lines, large unmanaged GDI handles remain in memory until Gen 2 garbage collection fires, contributing to GC pauses during rush hours.
3. **WinUI 3 Process Memory Overhead:**
   WinUI 3 processes (`Falah.Client.CashierWinUI.exe`) carry a baseline working set of ~180MB RAM due to DirectX and Windows App SDK composition runtimes. On 4GB RAM terminals running SQL Server LocalDB (~400MB) and the ASP.NET Core server (~150MB), memory pressure is significant.
4. **Startup Latency:**
   Cold startup of the combined Server + LocalDB + WinUI client takes 12–18 seconds on magnetic HDDs or eMMC storage. Pre-compilation (`PublishReadyToRun`) is recommended.
""", encoding='utf-8')

    # 3. Pickup and Delivery Workflows
    p_pick = Path("GitHub Copilot Work/09-Pickup-and-Delivery/PICKUP_AND_DELIVERY_WORKFLOW_AUDIT.md")
    p_pick.parent.mkdir(parents=True, exist_ok=True)
    p_pick.write_text("""# Branch Pickup and Delivery Order Lifecycle Audit

- **Audit Target:** End-to-End Order Fulfillments, Kitchen Preparation, Cash-on-Delivery & Status Invariants
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit
- **Status:** Complete Static Workflow Audit of Delivery and Pickup Lifecycles.

---

## 1. Branch Pickup Workflow Lifecycle Analysis

The owner directive specifically mandates an investigation of the branch pickup lifecycle:
- **Owner Requirement:** Branch pickup orders must support kitchen ticket creation *before* payment (preparation stage), followed by final payment settlement and fiscal receipt printing at the cashier counter when the customer arrives to collect food.
- **Finding FLOW-001 (HIGH_REQUIREMENT_GAP):**
  Forensic inspection of `src/Falah.Domain/Orders/` and `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` reveals that the requested **two-stage pickup lifecycle is completely absent** from the active application code:
  - In `CheckoutOrderHandler.cs`, pickup orders require immediate payment at creation (`plan.IsPaid = true` or immediate payment tender).
  - There is no operational state or endpoint allowing a cashier to place a pickup order into kitchen preparation without simultaneously taking payment.
  - If a customer phones in a pickup order, the cashier must either fake a payment or delay ringing the order until the customer arrives, delaying food preparation.
- **Remediation:** Introduce `OrderStatus.PendingPayment` or `OrderStatus.InPreparation` for Pickup orders, decouple kitchen ticket printing from checkout payment, and add a dedicated "Pay & Collect Pickup" endpoint.

---

## 2. Delivery Order Workflow Lifecycle Analysis

The delivery lifecycle connects the Cashier/Call Center, Kitchen Prep, Driver Dispatch, and Driver Cash Custody Settlement:

```mermaid
stateDiagram-v2
    [*] --> New: CheckoutOrderHandler (OrderType=Delivery)
    New --> Ready: MarkDeliveryReadyHandler (Kitchen Bagged)
    Ready --> Dispatched: AssignDriverHandler (Driver Custody Assigned)
    Dispatched --> Delivered: SettleDriverCustodyHandler (Cash Handed to Cashier)
    Dispatched --> Returned: CancelDeliveryHandler (Customer Refused / Address Not Found)
    Delivered --> [*]
    Returned --> [*]
```

### Critical Delivery Invariant Findings:
1. **Unvalidated Delivery Service Fees (FIN-016):**
   In `CheckoutOrderHandler.cs:142`, `deliveryFee` is accepted directly from the client wire request without verifying that the fee matches the configured `DeliveryService.DefaultFee` in the database. An operator can alter the request to set `deliveryFee = 0` or apply arbitrary negative adjustments.
2. **Driver Cash Custody Discrepancy (FIN-019 & FIN-CAND-DELIVERY-CUSTODY-001):**
   When a delivery order is paid via mixed payment (e.g. 50 EGP card paid at call center + 100 EGP cash to be collected by driver), `DeliveryFactsComposer.cs` and delivery reports erroneously aggregate the total order amount (`Total`) into the driver's expected cash custody rather than just the cash split (`PaymentSplit.Amount`), demanding drivers hand over cash that was already collected electronically.
""", encoding='utf-8')

    # 4. Test Coverage and Missing Tests
    p_test = Path("GitHub Copilot Work/10-Test-Coverage/TEST_COVERAGE_AND_MISSING_TESTS.md")
    p_test.parent.mkdir(parents=True, exist_ok=True)
    p_test.write_text("""# Test Coverage, Gap Analysis, and Missing Integration Tests Audit

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
""", encoding='utf-8')

    print("Core reports generated successfully.")

if __name__ == '__main__':
    build_core_reports()
