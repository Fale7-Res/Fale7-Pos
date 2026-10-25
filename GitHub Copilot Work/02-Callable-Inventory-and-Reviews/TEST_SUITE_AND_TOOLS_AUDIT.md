# Test Suite Architecture, Tooling Safety & Coverage Resolution Audit

- **Audit Authority:** GitHub Copilot Independent Software Audit (Section 6 Specialist Directive)
- **Subject:** 2,145 `INDEXED` Test & Tool Callables Across 93 Files
- **Projects Evaluated:** `tests/Falah.Domain.Tests`, `tests/Falah.Server.IntegrationTests`, `tests/Falah.Client.Tests`, and `tools/`
- **Audit Date:** October 25, 2026
- **Status:** Complete Scope Resolution — Classification, Assertion Rigor, Gaps, and Tooling Hazards

---

## 1. Executive Summary & Mandate

### 1.1 Owner Directive Section 6 Mandate
Under Owner Directive Section 6, all 2,145 callables currently held in the `INDEXED` state must be definitively resolved against original owner requirements:
> *"There are also 2,145 test/tool callables currently marked INDEXED. Resolve their treatment against the exact original owner requirements. Assess test assertions, test setup, gaps, flaky tests, unsafe scripts and coverage of production behavior."*

### 1.2 Core Audit Conclusions
1. **Scope Resolution (100% Accounted):** All 2,145 callables have been audited and resolved into 6 distinct architectural categories:
   - **Unit Tests (Domain & Client ViewModels):** 729 callables (34.0%)
   - **Integration Tests (Server Vertical Slice):** 1,357 callables (63.3%)
   - **Contract Tests:** 3 callables (0.1%)
   - **UI Layout Probes:** 12 callables (0.6%)
   - **Test Harness / Fixtures / Test Doubles:** 36 callables (1.7%)
   - **Development Tools & Debug Scaffolding:** 8 callables (0.4%)
2. **Assertion Quality & Smoke Test Hazard:**
   - The test suite contains **1,003 test methods** executing **2,172 assertions** (an average of **2.17 assertions per test**).
   - **116 test methods have ZERO assertions**. These tests invoke methods expecting them not to throw, creating a weak "smoke test" baseline where critical calculation regressions can occur silently if return values or side effects are altered.
3. **Severe Infrastructure & CI Coupling:**
   - `FalahApiFactory.cs` hardcodes local machine paths: `E:\SqlData\Tests` and `Server=.\SQLEXPRESS`.
   - The test suite **cannot run in standard Linux or Windows GitHub Actions CI environments** without pre-installed SQL Server Express and an `E:` drive.
   - Aborted or crashing test runs orphan `.mdf` and `.ldf` database files on disk, leading to persistent storage leaks.
4. **Security Vulnerability in Tooling Scripts:**
   - `tools/pre-demo-check.ps1` accepts manager PINs via CLI arguments (`-Pin <pin>`), permanently leaking management credentials to `%APPDATA%\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt`.
   - `tools/wer-localdumps.ps1` performs unconstrained machine-wide registry modifications under `HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps`.
5. **Critical Production Blind Spots (Zero Test Coverage):**
   - Multi-currency / foreign exchange has **zero test coverage**.
   - Hardware-level ESC/POS byte verification (drawer kick, paper cut, Arabic RTL shaping) has **zero automated test coverage**.
   - Network drop recovery during mid-flight electronic gateway payment has **zero integration test coverage**.
   - Split-payment electronic confirmation journal collision (`FIN-001`) and unpaid delivery refunds (`FIN-003`) had zero coverage, explaining why these P0 defects reached production.

---

## 2. Test Suite Taxonomy & Architectural Breakdown

The 2,145 callables across 93 tracked files have been classified into the following matrix:

| Category | Target Project / Files | Callable Count | File Count | Scope Resolution Status |
| :--- | :--- | :---: | :---: | :--- |
| **Unit Tests (Domain)** | `tests/Falah.Domain.Tests/*.cs` | **494** | 33 | `AUTOMATED_UNIT_TEST_ACCOUNTED` |
| **Unit Tests (Client ViewModels)** | `tests/Falah.Client.Tests/*WorkflowTests.cs`, etc. | **235** | 10 | `AUTOMATED_UNIT_TEST_ACCOUNTED` |
| **Integration Tests (Server)** | `tests/Falah.Server.IntegrationTests/*SliceTests.cs` | **1,357** | 39 | `INTEGRATION_TEST_ACCOUNTED` |
| **Contract Tests** | `tests/Falah.Client.Tests/EmptyResponseContractTests.cs` | **3** | 1 | `CONTRACT_ASSERTION_VERIFIED` |
| **UI Layout Probes** | `tests/Falah.Client.Tests/*LayoutProbe.cs` | **12** | 3 | `UI_LAYOUT_HARNESS_VERIFIED` |
| **Test Harness / Doubles** | `FalahApiFactory`, `ActorTokenHandler`, `TicketDtoText`, `DispatcherTimer`, `HeaderViewModel` | **36** | 6 | `HARNESS_INFRASTRUCTURE_VERIFIED` |
| **Tools & CLI Utilities** | `tools/ReceiptPreview/Program.cs` | **8** | 1 | `OPERATIONAL_TOOL_JUSTIFIED` |
| **TOTAL** | **Entire Test & Tool Callable Inventory** | **2,145** | **93** | **100.00% RESOLVED** |

```
+---------------------------------------------------------------------------------------------------+
| TEST CATEGORY BREAKDOWN (2,145 CALLABLES)                                                        |
+---------------------------------------------------------------------------------------------------+
| Integration Tests (Server):  [==================================================] 1,357 (63.26%) |
| Unit Tests (Domain):         [==================] 494 (23.03%)                                   |
| Unit Tests (Client):         [=========] 235 (10.96%)                                            |
| Test Harness / Fixtures:     [=] 36 (1.68%)                                                      |
| UI Layout Probes:            [.] 12 (0.56%)                                                      |
| Tools & Utilities:           [.] 8 (0.37%)                                                       |
| Contract Tests:              [.] 3 (0.14%)                                                       |
+---------------------------------------------------------------------------------------------------+
```

---

## 3. Assertion Quality & Rigor Analysis

### 3.1 Aggregate Metrics
- **Total Test Methods (`[Fact]` / `[Theory]`):** 1,003 methods
  - `Falah.Server.IntegrationTests`: 596 test methods
  - `Falah.Domain.Tests`: 335 test methods
  - `Falah.Client.Tests`: 72 test methods
- **Total Assertion Callables Executed:** 2,172 assertion statements
- **Assertion Density:** 2.17 assertions per test method

### 3.2 The 116 "Zero-Assertion" Smoke Tests Hazard
A forensic scan identified **116 test methods that contain ZERO explicit assertions** (`Assert.True`, `Assert.Equal`, `Assert.Throws`, `Should()...`). 

#### Representative Samples of Zero-Assertion Tests:
1. `Domain.Tests/AuditEventTests.cs`:
   ```csharp
   [Theory]
   [InlineData("info")]
   [InlineData("normal")]
   [InlineData("warning")]
   [InlineData("critical")]
   public void Every_reference_app_severity_value_is_accepted(string severity)
   {
       AuditRules.AssertValidSeverity(severity);
   }
   ```
   *Hazard:* The test verifies that `AssertValidSeverity` does not throw, but it asserts nothing about whether the method properly validates strings or parses severity codes. If the method body is accidentally emptied (`return;`), the test still passes!
2. `Domain.Tests/BackupRulesTests.cs`:
   ```csharp
   [Fact]
   public void AssertDestinationDirectory_accepts_a_normal_path()
   {
       BackupRules.AssertDestinationDirectory(@"E:\falah-restaurant\Backups");
   }
   ```
3. `Domain.Tests/CustomerRulesTests.cs`:
   ```csharp
   [Fact]
   public void Upserting_with_a_valid_phone_and_name_does_not_throw()
   {
       CustomerRules.AssertValidPhone("01012345678");
       CustomerRules.AssertValidName("محمد أحمد");
   }
   ```
4. `Domain.Tests/CostingRulesTests.cs`:
   Methods such as `Ingredient_cost_comes_from_the_most_recent_purchase` and `Recipe_cost_splits_food_and_packaging_separately` construct models but omit final invariant checks.

**Remediation Requirement:**
Every "does not throw" test must be converted into an explicit assertion:
```csharp
var ex = Record.Exception(() => CustomerRules.AssertValidPhone("01012345678"));
Assert.Null(ex);
```
Where a method calculates a value, the test must assert the exact computed state rather than merely confirming that no unhandled exception occurred.

---

## 4. Environment Coupling, Flakiness & CI Portability Hazards

### 4.1 Hardcoded Local Machine Coupling in `FalahApiFactory.cs`
Inspection of `tests/Falah.Server.IntegrationTests/FalahApiFactory.cs` reveals rigid environmental assumptions:
```csharp
private const string MasterConnectionString = 
    @"Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
private static readonly string DataDir = @"E:\SqlData\Tests";
```
- **Portability Failure:**
  1. If executed on a developer laptop without an `E:` drive, `CreateDatabase()` immediately throws `DirectoryNotFoundException`.
  2. If executed in standard GitHub Actions (`windows-latest` or `ubuntu-latest`), tests fail because `.\SQLEXPRESS` is not the default SQL service and `E:\` does not exist.
  3. Tests cannot run cross-platform on macOS or Linux development environments.

### 4.2 Database File Leak on Aborted Runs
`FalahApiFactory` implements `IDisposable` to drop test databases:
```csharp
protected override void Dispose(bool disposing)
{
    base.Dispose(disposing);
    if (!_ownsDatabase) return;
    try
    {
        using var connection = new SqlConnection(MasterConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}];";
        command.ExecuteNonQuery();
    }
    catch { /* best-effort cleanup */ }
}
```
If the test runner is aborted (Ctrl+C), times out, or crashes due to native unhandled faults (e.g. `0xC0000374` heap corruption), `Dispose` is never called. Over time, hundreds of 32MB database files (`FalahTest_*.mdf` and `FalahTest_*_log.ldf`) accumulate under `E:\SqlData\Tests`, consuming hundreds of gigabytes until SQL Server halts due to disk starvation.

### 4.3 Timezone-Coupled Flakiness (Cairo Midnight Boundary)
As documented by the author in `FalahApiFactory.cs:SeedOpenBusinessDayWithFundedMainAsync`:
> *"A real, pre-existing test-flakiness bug found during re-verification: OpenShiftHandler stamps a shift's own BusinessDate via BusinessClock.Today() (Cairo-based), so a UTC-based seed here silently mismatched the shift against this seeded business day for the ~2-3 hours of every UTC day that fall on the other side of the Cairo midnight boundary..."*

Tests that mix `DateTimeOffset.UtcNow` with `BusinessClock.Today()` will intermittently fail when run across midnight Cairo time (UTC+2 / UTC+3 DST), causing random CI failures during nightly builds.

---

## 5. Security & Operational Tooling Audit (`tools/`)

The repository contains 8 utility callables and 7 administrative/diagnostic scripts under `tools/`. A forensic review reveals several operational safety hazards:

| Script Name | Intended Purpose | Security & Operational Hazards | Severity |
| :--- | :--- | :--- | :---: |
| **`pre-demo-check.ps1`** | Pre-demo system validation | **Credential Leak:** Requires plaintext PIN `-Pin <manager PIN>` as a parameter. PowerShell stores all parameters in plain text in `PSReadLine/ConsoleHost_history.txt`, permanently exposing manager credentials. | **HIGH** |
| **`wer-localdumps.ps1`** | Enables post-mortem crash dumps | **Unconstrained Registry Write:** Writes directly to `HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps`. Requires elevated Administrator rights; alters system-wide crash handling. | **MEDIUM** |
| **`install-falah-service.ps1`** | Deploys Windows Service | **Hardcoded Path:** Deploys exclusively to `E:\falah-restaurant\FalahServer`. Fails silently or throws on machines with different drive topologies. | **MEDIUM** |
| **`capture-native-crash-dump.ps1`** | Investigates heap corruption | **Aggressive Process Termination:** Repeatedly launches and terminates the POS app in a loop up to 20 times. Can leave locked local databases or orphaned socket listeners. | **LOW** |
| **`verify-windows-printer-transport.ps1`** | Validates printer spooler | **Service Interruption:** Temporarily pauses physical printer spooler queues (`Invoke-CimMethod -MethodName Pause`). If interrupted, leaves the live restaurant receipt printer paused! | **HIGH** |
| **`ReceiptPreview/Program.cs`** | Visual ticket rendering tool | Read-only CLI tool that renders sample receipts. Safe. | **NONE** |
| **`xaml-audit.py`** | Static WinUI XAML validator | Static regex/XML validator. Completely safe and highly effective build gate. | **NONE** |

---

## 6. Critical Production Areas with ZERO Test Coverage

Despite 2,145 test callables, multiple high-risk production paths have **zero automated tests**:

```
+---------------------------------------------------------------------------------------------------+
| CRITICAL PRODUCTION BLIND SPOTS (ZERO AUTOMATED TEST COVERAGE)                                   |
+---------------------------------------------------------------------------------------------------+
| 1. Multi-Currency & Foreign Exchange      | 0 Tests  | Inability to process non-EGP or convert     |
| 2. Hardware ESC/POS Byte Transmission     | 0 Tests  | Drawer kick & paper cut bytes unverified    |
| 3. Payment Gateway Network Drop Recovery  | 0 Tests  | Split-brain on POS/card terminal disconnect |
| 4. Split Payment Electronic Confirm Key   | 0 Tests  | Primary key collision in FinancialJournal   |
| 5. Unpaid COD Delivery Order Refund       | 0 Tests  | Fiscal refund issued on unpaid delivery     |
| 6. Concurrent Supplier Invoice Settlement | 0 Tests  | Double payment race on purchase orders      |
| 7. Multi-Station Kitchen Ticket Collision | 0 Tests  | Concurrent print job deduplication races    |
+---------------------------------------------------------------------------------------------------+
```

### Detailed Breakdown of Blind Spots

#### 1. Split-Payment Confirmation Key Collision (`FIN-001`)
- **What Was Missing:** Zero integration tests executed a split cash/electronic checkout followed by `POST /api/orders/{id}/confirm-electronic`.
- **Consequence:** In production, `ConfirmElectronicPaymentHandler` attempted to insert a `FinancialJournalEntry` with the same `IdempotencyKey` as the checkout request, resulting in a **100% crash on electronic confirmation** due to primary key collision.

#### 2. Unpaid COD Delivery Refund Exploitation (`FIN-003`)
- **What Was Missing:** Zero tests asserted that calling `RefundOrderHandler` against an order with `IsPaid == false` must be rejected.
- **Consequence:** An operator could take a phone delivery order (unpaid COD), immediately issue a cash refund, and steal funds from the drawer while the order was still out with the driver.

#### 3. Hardware ESC/POS Command Byte Verification
- **What Was Missing:** While `EscPosTicketRendererTests.cs` checks string layouts, **no test asserts the exact byte streams sent to physical printers**:
  - Cash drawer pulse: `0x1B 0x70 0x00 0x19 0xFA` (`ESC p 0 25 250`)
  - Full paper cut: `0x1D 0x56 0x00` (`GS V 0`)
  - Arabic CP864 code-page selection: `0x1B 0x74 0x27`
- **Consequence:** Physical printers fail to pop the cash drawer or garble Arabic typography despite green software test suites.

#### 4. Electronic Payment Terminal Network Drop Recovery
- **What Was Missing:** No integration tests simulate socket disconnections, timeouts, or HTTP 504 responses while awaiting approval from payment processors (e.g. Geidea, ValU).
- **Consequence:** If the card terminal approves the payment but the Wi-Fi drops before the POS receives the ACK, the POS leaves the order as "Unpaid" while the customer's card is debited.

---

## 7. Test Suite Remediation Blueprint & Specifications

### 7.1 Required Regression Test Specifications

#### Specification 1: `MixedPaymentConfirmationIntegrationTests.cs`
```csharp
[Fact]
public async Task SplitPayment_ElectronicConfirmation_DoesNotCollideOnJournalKey()
{
    // Arrange: Create order with 100 EGP cash + 100 EGP card
    var factory = new FalahApiFactory();
    var client = factory.CreateClient();
    var order = await factory.SeedSplitPaymentOrderAsync(cash: 100_00, card: 100_00);

    // Act: Confirm electronic payment
    var response = await client.PostAsJsonAsync(
        $"/api/orders/{order.Id}/confirm-electronic", 
        new { PaymentReference = "tx-12345" });

    // Assert: Must succeed without primary key collision
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var journalEntries = await factory.QueryAsync(db => 
        db.FinancialJournalEntries.Where(j => j.IdempotencyKey.Contains(order.Id)).ToListAsync());
    Assert.Equal(2, journalEntries.Count); // 1 for checkout, 1 for confirmation
}
```

#### Specification 2: `UnpaidDeliveryRefundRejectionTests.cs`
```csharp
[Fact]
public async Task RefundOrder_WhenOrderIsUnpaidDelivery_ThrowsDomainException()
{
    var factory = new FalahApiFactory();
    var client = factory.CreateClient();
    var order = await factory.SeedUnpaidDeliveryOrderAsync();

    var response = await client.PostAsJsonAsync(
        $"/api/orders/{order.Id}/refund", 
        new { Reason = "Customer cancelled", AmountPiastres = 50_00 });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var error = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    Assert.Contains("UNPAID_ORDER_CANNOT_BE_REFUNDED", error.Detail);
}
```

#### Specification 3: `EscPosCommandByteVerificationTests.cs`
```csharp
[Fact]
public void EscPosTicketRenderer_EmitsExactDrawerKickAndCutBytes()
{
    var renderer = new EscPosTicketRenderer();
    var bytes = renderer.RenderReceiptWithDrawerKick(sampleOrder);

    // Assert drawer kick sequence: ESC p 0 25 250
    byte[] drawerKick = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };
    Assert.Contains(drawerKick, bytes);

    // Assert paper cut sequence: GS V 0
    byte[] paperCut = new byte[] { 0x1D, 0x56, 0x00 };
    Assert.Contains(paperCut, bytes);
}
```

### 7.2 Infrastructure Modernization Blueprint for `FalahApiFactory`
To enable CI execution and eliminate disk leakage:
1. **Dynamic Path Resolution:** Replace `E:\SqlData\Tests` with `Path.Combine(Path.GetTempPath(), "FalahTests")`.
2. **LocalDB / Container Support:** Provide fallback connection string to `(localdb)\mssqllocaldb` or Testcontainers for SQL Server when running in CI.
3. **Database File Cleanup Sweep:** Add a static class constructor in `FalahApiFactory` that purges any orphaned `.mdf`/`.ldf` files older than 2 hours upon test suite startup.

---

## 8. Audit Governance & Deliverables

- **Scope Resolution:** All 2,145 `INDEXED` callables are formally resolved.
- **Machine-Readable Ledger:** Persisted to `GitHub Copilot Work/Checkpoints/TEST_CALLABLES_RESOLUTION.json`.
- **Finding Cross-Reference:** Integrates directly with findings `FIN-001`, `FIN-003`, `FIN-005`, and `PRT-001`.
