# Unexecuted Tests, Hardware Dependencies, and Audit Limitations

- **Audit Target:** Fale7 POS Ecosystem (ASP.NET Core Server, WPF Client, WinUI 3 Cashier, WinUI 3 CallCenter, Printing Subsystem)
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Mode:** Pure Static Code & Forensic Repository Analysis (Documentation Only — Zero Runtime Invocations)
- **Document Purpose:** Formal record of operational, hardware, concurrency, and visual tests that could NOT be executed during static analysis, mapping their risks and required runtime validation suites.

---

## 1. Physical Hardware & ESC/POS Printing Output Verification

| Dimension | Specification & Required Environment | Validated Findings | Risk Severity If Skipped |
| :--- | :--- | :--- | :--- |
| **Physical Printer Output** | Requires live ESC/POS thermal receipt printers (80mm & 58mm Xprinter, Epson TM-T20/TM-T88, Sunmi) connected over USB and Ethernet (TCP 9100). | `PRT-001`, `PRT-002`, `PRT-003`, `PRT-005`, `PRT-006` | **CRITICAL:** Garbled Arabic receipts, missing item descriptions, paper cut failure, or endless paper feed blocking physical restaurant order routing. |

### Technical Details of Limitation:
Static AST inspection confirms that ESC/POS byte sequence generation (`GS v 0` for raster graphics and `ESC @` for hardware initialization) matches standard command structures in `Transports.cs` and `DocumentPrintJobs.cs`. However, physical dot density, thermal printhead burn time, font substitution, codepage switching (`CP864` vs `Windows-1256`), and bidirectional Arabic word shaping on hardware character generators cannot be verified without physical printouts.

---

## 2. SQL Server High-Concurrency & Race Condition Reproduction

| Dimension | Specification & Required Environment | Validated Findings | Risk Severity If Skipped |
| :--- | :--- | :--- | :--- |
| **Concurrent Load & Deadlock Analysis** | Requires Microsoft SQL Server 2019/2022 instance running under NBomber or Locust multi-client load simulating 50+ concurrent POS terminals. | `FIN-001`, `FIN-005`, `FIN-009`, `FIN-015`, `SEC-007`, `SEC-009`, `FIN-CAND-BUSINESS-DAY-OPEN-RACE-001`, `FIN-CAND-PAYROLL-001` | **CRITICAL:** Silent financial double-spending, negative ledger balances, corrupted shift closing totals, and primary key/idempotency collisions causing till freezes during restaurant peak rush hours. |

### Technical Details of Limitation:
The audit confirmed through static source tracing that:
1. `FIN-001`: In split tender checkouts, `DefersAllMoney` returns `false`, forcing `CheckoutOrderHandler` to burn `sale-{CheckoutRequestId}` in `EfFinancialJournal`, locking out downstream `ConfirmElectronicPaymentHandler`.
2. `FIN-005`: Two concurrent payments against the same purchase invoice can both pass outstanding balance checks before either commits.
3. `SEC-007`: Mitigated by `b.Property(a => a.Consumed).IsConcurrencyToken()` in `FalahDbContext.cs`, but concurrent requests trigger `DbUpdateConcurrencyException` instead of clean domain messages.
4. `SEC-009`: In `PinUniquenessChecker.cs`, in-memory PBKDF2 verification across active users runs without table or row lock, allowing concurrent creations with identical PINs.

---

## 3. WinUI 3 Desktop Visual Rendering & XAML Threading Verification

| Dimension | Specification & Required Environment | Validated Findings | Risk Severity If Skipped |
| :--- | :--- | :--- | :--- |
| **WinUI 3 Desktop Visual Tree & Threading** | Requires Windows 10/11 x64 execution environment with active display driver, WindowsAppSDK 1.7 runtime, and GPU acceleration. | `UI-001`, `UI-004`, `UI-005`, `UI-010`, `UI-014`, `UI-015`, `ASYNC-001`, `PERF-001` | **HIGH:** Hard crash of desktop client on unhandled background task exceptions (crashing till without error dialog), memory leaks on screen switches, and unreadable or clipped order grids. |

### Technical Details of Limitation:
Static inspection revealed multiple `async void` command handlers (e.g., `MainViewModel.OnLoggedIn()`) and complex converter hierarchies in `Converters.cs`. Without launching the Win32/XAML visual dispatcher, it is impossible to verify whether unobserved exceptions terminate the process, whether layout virtualization (`VirtualizingStackPanel`) prevents UI stutter during 500-item catalog scrolls, or whether modal dialogs properly clip long Arabic text.

---

## 4. Arabic RTL Layout, Typography, & High-Contrast Visual Tests

| Dimension | Specification & Required Environment | Validated Findings | Risk Severity If Skipped |
| :--- | :--- | :--- | :--- |
| **Arabic RTL & Theme Contrast** | Requires physical touchscreen till monitors under varying ambient restaurant lighting, switching between Windows Dark and Light themes. | `UI-002`, `UI-004`, `UI-013` | **MEDIUM:** Unselected expense and payment buttons combine literal `#FFFFFF` white backgrounds with light-gray text (`UI-002`), rendering text invisible to cashiers in dark theme. |

---

## 5. Production Data Migration & Volume Scalability Benchmarks

| Dimension | Specification & Required Environment | Validated Findings | Risk Severity If Skipped |
| :--- | :--- | :--- | :--- |
| **Large-Volume Migration & In-Memory Grouping** | Requires production database restore containing 3+ years of restaurant operational data (500,000+ stock movements, 100,000+ orders). | `INV-PERF-001`, `FIN-007`, `FIN-014`, `UI-014` | **HIGH:** `ListInventoryItemsAsync` loads the entire `StockMovements` table into memory to group in LINQ-to-Objects (`INV-PERF-001`). At 500k rows, this will exhaust RAM and freeze the till. |

---

## 6. Multi-Station Network Partition & Offline Recovery Tests

| Dimension | Specification & Required Environment | Validated Findings | Risk Severity If Skipped |
| :--- | :--- | :--- | :--- |
| **Network Partition & Retained Requests** | Requires network fault injection (packet drop, HTTP 504 timeouts, Wi-Fi drops) while orders, refunds, and PIN changes are in flight. | `SEC-002`, `SEC-008`, `UI-001`, `UI-005`, `UI-016` | **HIGH:** Offline retained request store writes unencrypted PINs in `%LOCALAPPDATA%` (`SEC-002`), while replaying a PIN change overwrites the active till session token with the target user's identity (`SEC-008`). |

---

## 7. Comprehensive Unexecuted Test Matrix

| Test Suite ID | Target Component | Finding(s) Validated | Required Environment & Tooling | Risk Level |
| :--- | :--- | :--- | :--- | :--- |
| **UT-PRT-01** | `DocumentPrintJobs.cs`, `Transports.cs` | `PRT-001`, `PRT-002`, `PRT-006` | Physical 80mm/58mm ESC/POS printer; ESC/POS protocol analyzer. | High |
| **UT-CONC-01** | `CheckoutOrderHandler.cs`, `EfFinancialJournal.cs` | `FIN-001`, `FIN-MULTI-ELEC-001` | SQL Server 2022; k6 concurrent split checkout load script. | Critical |
| **UT-CONC-02** | `PayPurchaseHandler.cs`, `PurchasingQueryService.cs` | `FIN-005` | SQL Server 2022; concurrent invoice payment runner. | High |
| **UT-CONC-03** | `EfApprovalRegistry.cs`, `FalahDbContext.cs` | `SEC-007` | SQL Server 2022; parallel approval consumption stress test. | Medium |
| **UT-CONC-04** | `PinUniquenessChecker.cs`, `CreateUserHandler.cs` | `SEC-009`, `SEC-010` | 50 concurrent client workers submitting colliding PINs. | High |
| **UT-UI-01** | `ExpenseViews.xaml`, `OrdersView.xaml` | `UI-002`, `UI-004` | Windows 11 desktop; dynamic theme switching script; contrast meter. | Medium |
| **UT-UI-02** | `UsersViewModel.Actions.cs`, `ApiClient.cs` | `SEC-008` | WinUI 3 Cashier client; Wireshark; offline proxy emulator. | High |
| **UT-VOL-01** | `InventoryQueryService.cs` | `INV-PERF-001` | SQL Server with 500k rows; memory profiler (dotMemory). | High |
| **UT-VOL-02** | `ReportingQueryService.cs` | `FIN-CAND-REPORT-RANGE-001` | SQL Server with 100k orders across leap years and day transitions. | Medium |

---
