# 01 — Master Issue Registry

One row per canonical issue, rebuilt from the issue files in `ISSUES/` (the source of truth). Grouped by remediation group, then severity, then ID. Remediation progress: [REMEDIATION/PROGRESS.md](REMEDIATION/PROGRESS.md).


## 1. Critical — fix before production

| ID | Title | Category | Severity | Priority | Implementation Status | Verification Level | Confidence | Issue File |
|---|---|---|---|---|---|---|---|---|
| DB-0001 | Eight untracked, unreviewed migrations (two rewrite data) are auto-applied at startup | Database / deployment | High | P0 | PARTIALLY_FIXED | STATIC_CODE_REVIEW | High | [ISSUES/DB-0001.md](ISSUES/DB-0001.md) |
| PRT-0012 | Print agents exit if the server is not up and have no service/restart tooling | Printing / operations | High | P0 | PARTIALLY_FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PRT-0012.md](ISSUES/PRT-0012.md) |
| SEC-0006 | Server uses plain HTTP by default; PINs and session tokens travel in clear on the LAN | Security / transport | High | P0 | PARTIALLY_FIXED | CODE_TEST_VERIFIED_ONSITE_PENDING | High | [ISSUES/SEC-0006.md](ISSUES/SEC-0006.md) |
| DOC-0003 | CLAUDE.md, 11 operational docs, all audit folders and 8 migrations exist only in the working tree | Documentation / preservation | Medium | P0 | PARTIALLY_FIXED | STATIC_CODE_REVIEW | High | [ISSUES/DOC-0003.md](ISSUES/DOC-0003.md) |

## 2. High priority

| ID | Title | Category | Severity | Priority | Implementation Status | Verification Level | Confidence | Issue File |
|---|---|---|---|---|---|---|---|---|
| DB-0005 | Restore does not block writers; only counts pending journal rows | Database / backup | High | P1 | PARTIALLY_FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/DB-0005.md](ISSUES/DB-0005.md) |
| PAYR-0004 | Standalone employee settlements and payroll runs can both pay the same earnings | Payroll / policy | High | P1 | NOT_FIXED_YET | STATIC_CODE_REVIEW | Medium | [ISSUES/PAYR-0004.md](ISSUES/PAYR-0004.md) |
| SEC-0022 | Demo users (published PINs) seeded into any empty database | Security / credentials | High | P1 | PARTIALLY_FIXED | STATIC_CODE_REVIEW | High | [ISSUES/SEC-0022.md](ISSUES/SEC-0022.md) |
| SEC-0023 | Print agent ran as the demo manager; print queue endpoints open to any session | Security / least privilege | High | P1 | PARTIALLY_FIXED | STATIC_CODE_REVIEW | High | [ISSUES/SEC-0023.md](ISSUES/SEC-0023.md) |
| SEC-0024 | Exposed demo manager PIN must be rotated on every real database | Security / credentials | High | P1 | PARTIALLY_FIXED | STATIC_CODE_REVIEW | High | [ISSUES/SEC-0024.md](ISSUES/SEC-0024.md) |
| ARCH-0001 | 197 routes in one Program.cs with four hand-written authorization idioms | Architecture | Medium | P1 | PARTIALLY_FIXED | STATIC_CODE_REVIEW | High | [ISSUES/ARCH-0001.md](ISSUES/ARCH-0001.md) |
| ARCH-0003 | Money-command invariants are composed by hand in each handler | Architecture | Medium | P1 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/ARCH-0003.md](ISSUES/ARCH-0003.md) |
| HW-0002 | Physical cash drawer kick through the print agent — implemented, never run | Hardware | Medium | P1 | PARTIALLY_FIXED | NOT_VERIFIED | Medium | [ISSUES/HW-0002.md](ISSUES/HW-0002.md) |
| TEST-0002 | Confirmed money/time defects have no regression tests | Testing | Medium | P1 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/TEST-0002.md](ISSUES/TEST-0002.md) |

## 3. Medium priority

| ID | Title | Category | Severity | Priority | Implementation Status | Verification Level | Confidence | Issue File |
|---|---|---|---|---|---|---|---|---|
| UI-0025 | Two-stage pickup (prepare first, pay at collection) is not supported | Workflow / requirement | High | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/UI-0025.md](ISSUES/UI-0025.md) |
| API-0004 | Fixed 10-second client timeout versus growing full-history server work | API / availability | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | Medium | [ISSUES/API-0004.md](ISSUES/API-0004.md) |
| DOC-0002 | Backlog statuses lag the code | Documentation | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/DOC-0002.md](ISSUES/DOC-0002.md) |
| FIN-0005 | Split (multi-payment) orders cannot be refunded | Financial / refunds | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/FIN-0005.md](ISSUES/FIN-0005.md) |
| FIN-0020 | Business-day cash count is compared against a typed "expected" figure, not the system's expected cash | Financial / controls | Medium | P2 | PARTIALLY_FIXED | STATIC_CODE_REVIEW | Medium | [ISSUES/FIN-0020.md](ISSUES/FIN-0020.md) |
| FIN-0030 | Tax computed before comp is subtracted (VAT on comped orders) | Financial / tax policy | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | Medium | [ISSUES/FIN-0030.md](ISSUES/FIN-0030.md) |
| FIN-0042 | Failed delivery of an already-paid order: refund requirement not surfaced | Financial / delivery | Medium | P2 | PARTIALLY_FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/FIN-0042.md](ISSUES/FIN-0042.md) |
| FIN-0051 | A cash-on-delivery order that has not been dispatched cannot be cancelled or voided | Financial / delivery workflow | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | Medium | [ISSUES/FIN-0051.md](ISSUES/FIN-0051.md) |
| OPS-0002 | Backups stay on the same machine; off-machine copy is manual | Operations / backup | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/OPS-0002.md](ISSUES/OPS-0002.md) |
| OPS-0005 | End-to-end operating scenarios A–F as integration tests | Testing / operations | Medium | P2 | PARTIALLY_FIXED | NOT_VERIFIED | High | [ISSUES/OPS-0005.md](ISSUES/OPS-0005.md) |
| PAYR-0005 | Attendance corrections revalue old effects at today's wage and settings | Payroll | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PAYR-0005.md](ISSUES/PAYR-0005.md) |
| PAYR-0006 | Salary catch-up accrual uses the current salary for past days | Payroll | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | Medium | [ISSUES/PAYR-0006.md](ISSUES/PAYR-0006.md) |
| PAYR-0007 | Part-month payroll pays the full profile salary while accrual covers employed days only | Payroll / policy | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PAYR-0007.md](ISSUES/PAYR-0007.md) |
| PAYR-0017 | Monthly salary accrual and its dating in reports | Payroll | Medium | P2 | PARTIALLY_FIXED | STATIC_CODE_REVIEW | Medium | [ISSUES/PAYR-0017.md](ISSUES/PAYR-0017.md) |
| PERF-0001 | Treasury balance re-sums all history under UPDLOCK/HOLDLOCK with no account index | Performance / database | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PERF-0001.md](ISSUES/PERF-0001.md) |
| PERF-0003 | Business-day close evaluator loads all shifts, custodies, journal rows and movements | Performance | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PERF-0003.md](ISSUES/PERF-0003.md) |
| PRT-0004 | "Succeeded" means the transport accepted bytes; paper-out and cover-open are invisible; uncertain outcomes look like failures | Printing / hardware | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PRT-0004.md](ISSUES/PRT-0004.md) |
| PRT-0005 | Spooler submission is synchronous and ignores cancellation | Printing | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PRT-0005.md](ISSUES/PRT-0005.md) |
| PRT-0013 | Failed print jobs are never retried automatically | Printing / recovery | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PRT-0013.md](ISSUES/PRT-0013.md) |
| PRT-0016 | Held kitchen ticket is not released automatically when the transfer is confirmed | Printing / workflow | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PRT-0016.md](ISSUES/PRT-0016.md) |
| RPT-0003 | Owner financial screens (cash flow, pending electronic, voids, refunds, payroll accruals) built but not run on the desktop | Reporting / UI verification | Medium | P2 | PARTIALLY_FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/RPT-0003.md](ISSUES/RPT-0003.md) |
| UI-0027 | Reconciliation screen, period filters and a desktop UI automation run are missing | UI / features and verification | Medium | P2 | NOT_FIXED_YET | NOT_VERIFIED | High | [ISSUES/UI-0027.md](ISSUES/UI-0027.md) |

## 4. Low priority / polish

| ID | Title | Category | Severity | Priority | Implementation Status | Verification Level | Confidence | Issue File |
|---|---|---|---|---|---|---|---|---|
| DOC-0004 | Prior-tool audit evidence is not reliable without re-verification | Documentation / audit quality | Medium | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/DOC-0004.md](ISSUES/DOC-0004.md) |
| PERF-0004 | Supplier list loads the entire supplier ledger | Performance | Medium | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PERF-0004.md](ISSUES/PERF-0004.md) |
| PERF-0006 | Employees screen makes one balance request per employee and empties the list on the first failure | Performance / UI | Medium | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PERF-0006.md](ISSUES/PERF-0006.md) |
| API-0001 | InvalidOperationException mapped to 404 across ~99 endpoints | API / contracts | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/API-0001.md](ISSUES/API-0001.md) |
| API-0002 | actorUserId repeated in query strings on reads | API / design | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | Medium | [ISSUES/API-0002.md](ISSUES/API-0002.md) |
| ARCH-0002 | Two parallel WinUI client code bases plus a legacy WPF client | Architecture | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/ARCH-0002.md](ISSUES/ARCH-0002.md) |
| ARCH-0004 | Treasury account ids hard-coded in clients | Code quality | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/ARCH-0004.md](ISSUES/ARCH-0004.md) |
| CUS-0002 | No way to erase or anonymise a customer record | Privacy / feature | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/CUS-0002.md](ISSUES/CUS-0002.md) |
| DB-0003 | Queries rely on a case-insensitive database collation | Database / portability | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | Medium | [ISSUES/DB-0003.md](ISSUES/DB-0003.md) |
| DB-0009 | Financial journal states Failed and RecoveryRequired are never used | Database / dead state | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/DB-0009.md](ISSUES/DB-0009.md) |
| DOC-0001 | Documentation places the exposed demo PIN in the wrong file | Documentation | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/DOC-0001.md](ISSUES/DOC-0001.md) |
| FIN-0044 | Business-day P&L missing from the close snapshot | Reporting / feature | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/FIN-0044.md](ISSUES/FIN-0044.md) |
| INV-0011 | Historical food-cost reports use today's recipes and costs | Reporting / costing | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | Medium | [ISSUES/INV-0011.md](ISSUES/INV-0011.md) |
| OPS-0006 | No disk-space monitoring for database or backups | Operations / monitoring | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/OPS-0006.md](ISSUES/OPS-0006.md) |
| PAYR-0015 | PayrollRules.OutstandingAdvance compares lowercase types and is never called | Payroll / latent | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PAYR-0015.md](ISSUES/PAYR-0015.md) |
| PAYR-0019 | Payroll multipliers are doubles rounded with banker's rounding | Payroll / arithmetic | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PAYR-0019.md](ISSUES/PAYR-0019.md) |
| PERF-0002 | ControlAuditEvents queried by Action with only a time index | Performance / database | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PERF-0002.md](ISSUES/PERF-0002.md) |
| PRT-0008 | Saved printer width and receipt copy count do not affect printing | Printing / settings | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/PRT-0008.md](ISSUES/PRT-0008.md) |
| RPT-0001 | Product/food-cost reports neither normalise reversed ranges nor bound missing ranges | Reporting | Low | P3 | PARTIALLY_FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/RPT-0001.md](ISSUES/RPT-0001.md) |
| SEC-0007 | Sessions have only a 16-hour idle lifetime, no absolute lifetime | Security / sessions | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/SEC-0007.md](ISSUES/SEC-0007.md) |
| SEC-0011 | PIN uniqueness check runs PBKDF2 over every active user | Security / performance | Low | P3 | PARTIALLY_FIXED | STATIC_CODE_REVIEW | High | [ISSUES/SEC-0011.md](ISSUES/SEC-0011.md) |
| UI-0011 | Legacy WPF client: change display parses EGP as piastres; cash boundary and rounding divergences | UI / legacy client | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | Medium | [ISSUES/UI-0011.md](ISSUES/UI-0011.md) |
| UI-0021 | Disabled text at 2.3–2.6:1 without an explanation of why controls are disabled | UI / usability | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/UI-0021.md](ISSUES/UI-0021.md) |
| UI-0024 | Legacy WPF refund submit creates a new request id per attempt | UI / legacy client | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/UI-0024.md](ISSUES/UI-0024.md) |
| UI-0026 | Client caches permissions until re-login | UI / authorization UX | Low | P3 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [ISSUES/UI-0026.md](ISSUES/UI-0026.md) |

## 5. Fixed — needs re-test on the current tree

| ID | Title | Category | Severity | Priority | Implementation Status | Verification Level | Confidence | Issue File |
|---|---|---|---|---|---|---|---|---|
| FIN-0008 | Historical cash refund was omitted from the current shift's expected cash | Financial / shifts | High | P3 | FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/FIN-0008.md](ISSUES/FIN-0008.md) |
| FIN-0032 | Refunds were dated by the original sale rather than the refund event | Financial / periods | High | P3 | FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/FIN-0032.md](ISSUES/FIN-0032.md) |
| FIN-0034 | A sale could happen with no business day open (zero-float shift) | Financial / controls | High | P3 | FIXED | EXISTING_TEST_EVIDENCE | High | [ISSUES/FIN-0034.md](ISSUES/FIN-0034.md) |
| FIN-0036 | Failed/returned delivery stayed a sale with unresolved custody | Financial / delivery | High | P3 | FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/FIN-0036.md](ISSUES/FIN-0036.md) |
| SEC-0015 | Shift close impersonation via closedByUserId | Security / identity | High | P3 | FIXED | EXISTING_TEST_EVIDENCE | High | [ISSUES/SEC-0015.md](ISSUES/SEC-0015.md) |
| SEC-0016 | Case-insensitive / duplicate actor-field smuggling | Security / identity | High | P3 | FIXED | EXISTING_TEST_EVIDENCE | High | [ISSUES/SEC-0016.md](ISSUES/SEC-0016.md) |
| SEC-0017 | Manager approvals never expired and were not bound to the requesting session | Security / approvals | High | P3 | FIXED | STATIC_CODE_REVIEW | High | [ISSUES/SEC-0017.md](ISSUES/SEC-0017.md) |
| SEC-0019 | Approval-PIN guessing at /api/approvals/issue | Security / brute force | High | P3 | FIXED | EXISTING_TEST_EVIDENCE | High | [ISSUES/SEC-0019.md](ISSUES/SEC-0019.md) |
| SEC-0020 | PIN-uniqueness answers allowed enumerating other users' PINs | Security / oracle | High | P3 | FIXED | EXISTING_TEST_EVIDENCE | High | [ISSUES/SEC-0020.md](ISSUES/SEC-0020.md) |
| SEC-0021 | Login-by-PIN brute force (no failure accounting, IPv6 rotation, unbounded PBKDF2 work) | Security / brute force | High | P3 | FIXED | EXISTING_TEST_EVIDENCE | High | [ISSUES/SEC-0021.md](ISSUES/SEC-0021.md) |
| UI-0002 | Cashier checkout/refund/close/expense/safe-drop/cash-movement/drawer requests were memory-only | UI / reliability | High | P3 | FIXED | EXISTING_TEST_EVIDENCE | High | [ISSUES/UI-0002.md](ISSUES/UI-0002.md) |
| ARCH-0005 | Authentication middleware was registered before the exception handler | Architecture | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | High | [ISSUES/ARCH-0005.md](ISSUES/ARCH-0005.md) |
| DB-0008 | Scheduled backup worker ran inside test hosts | Database / testing | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | High | [ISSUES/DB-0008.md](ISSUES/DB-0008.md) |
| FIN-0009 | A pending split order subtracted the whole order from collected sales | Reporting / financial | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/FIN-0009.md](ISSUES/FIN-0009.md) |
| FIN-0029 | Profit computed on VAT-inclusive sales | Financial / reporting | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/FIN-0029.md](ISSUES/FIN-0029.md) |
| FIN-0033 | Dashboard leakage mixed refund amount and refund count bases | Reporting | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/FIN-0033.md](ISSUES/FIN-0033.md) |
| FIN-0035 | Shift handover/reconciliation movements carried no business day | Financial / periods | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/FIN-0035.md](ISSUES/FIN-0035.md) |
| FIN-0037 | Cancelled pending electronic order kept its stock deduction | Inventory / financial | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/FIN-0037.md](ISSUES/FIN-0037.md) |
| FIN-0038 | Calendar-day and business-day reports disagreed across midnight | Reporting / time | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/FIN-0038.md](ISSUES/FIN-0038.md) |
| FIN-0039 | Settled-from-drivers sum ignored movement status | Reporting | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/FIN-0039.md](ISSUES/FIN-0039.md) |
| FIN-0041 | Free-text expense categories (ingredient purchases double counted) | Financial / data quality | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/FIN-0041.md](ISSUES/FIN-0041.md) |
| INV-0006 | Owner dashboard summed stock without direction | Inventory / reporting | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | High | [ISSUES/INV-0006.md](ISSUES/INV-0006.md) |
| OPS-0004 | Lifecycle trace and manual hardware checklist | Operations / documentation | Medium | P3 | FIXED | STATIC_CODE_REVIEW | High | [ISSUES/OPS-0004.md](ISSUES/OPS-0004.md) |
| PAYR-0018 | Wage cost ignored deductions | Payroll / reporting | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | Medium | [ISSUES/PAYR-0018.md](ISSUES/PAYR-0018.md) |
| PRT-0006 | Print agent never re-authenticated after session loss | Printing | Medium | P3 | FIXED | EXISTING_TEST_EVIDENCE | High | [ISSUES/PRT-0006.md](ISSUES/PRT-0006.md) |
| RPT-0004 | Metric definitions for every displayed figure | Reporting / documentation | Medium | P3 | FIXED | STATIC_CODE_REVIEW | High | [ISSUES/RPT-0004.md](ISSUES/RPT-0004.md) |
| SEC-0026 | prepare-close, treasury balance and close-snapshot reads lacked permission checks | Security / authorization | Medium | P3 | FIXED | STATIC_CODE_REVIEW | High | [ISSUES/SEC-0026.md](ISSUES/SEC-0026.md) |
| UI-0004 | Dark theme: unselected expense/payment buttons use literal white background with pale text (1.22:1) | UI / accessibility | Medium | P2 | FIXED | NATIVE_UI_AND_CONTRAST_VERIFIED | High | [ISSUES/UI-0004.md](ISSUES/UI-0004.md) |
| UI-0019 | Muted caption/table-header text below WCAG AA on grey surfaces (4.34–4.48:1) | UI / accessibility | Medium | P2 | FIXED | NATIVE_UI_AND_CONTRAST_VERIFIED | High | [ISSUES/UI-0019.md](ISSUES/UI-0019.md) |
| UI-0020 | Warning and success text colours fail WCAG AA in the Light theme (3.07–3.58:1) | UI / accessibility | Medium | P2 | FIXED | NATIVE_UI_AND_CONTRAST_VERIFIED | High | [ISSUES/UI-0020.md](ISSUES/UI-0020.md) |
| DOC-0006 | Project working notes (CLAUDE.md) and project skills | Documentation | Low | P3 | FIXED | STATIC_CODE_REVIEW | High | [ISSUES/DOC-0006.md](ISSUES/DOC-0006.md) |
| FIN-0040 | Card refund of a closed-shift order with no open shift failed with a misleading 409 | Financial / refunds | Low | P3 | FIXED | EXISTING_TEST_EVIDENCE | High | [ISSUES/FIN-0040.md](ISSUES/FIN-0040.md) |
| SEC-0025 | CORS allowed any origin | Security / browser | Low | P3 | FIXED | STATIC_CODE_REVIEW | High | [ISSUES/SEC-0025.md](ISSUES/SEC-0025.md) |
| UI-0005 | Converter/imperative brushes may not follow live theme changes | UI / theming | Low | P3 | FIXED | NATIVE_UI_AND_CONTRAST_VERIFIED | Low | [ISSUES/UI-0005.md](ISSUES/UI-0005.md) |

## 6. Unverified risks (cannot yet be confirmed)

| ID | Title | Category | Severity | Priority | Implementation Status | Verification Level | Confidence | Issue File |
|---|---|---|---|---|---|---|---|---|
| UI-0006 | Recovered electronic confirm/cancel may have no reachable retry context at the till | UI / recovery | High | P1 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/UI-0006.md](ISSUES/UI-0006.md) |
| DB-0010 | Deadlock behaviour of concurrent tills is unknown | Database / concurrency | Medium | P2 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/DB-0010.md](ISSUES/DB-0010.md) |
| FIN-0045 | Financial approval provenance may retain an unvalidated presenter or drop a verified approver | Financial / audit | Medium | P2 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/FIN-0045.md](ISSUES/FIN-0045.md) |
| FIN-0046 | Unknown packaging cost does not mark profit as incomplete | Reporting / costing | Medium | P3 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/FIN-0046.md](ISSUES/FIN-0046.md) |
| OPS-0001 | Dependency vulnerability status unknown; .NET 8 support ends around November 2026 | Operations / dependencies | Medium | P2 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/OPS-0001.md](ISSUES/OPS-0001.md) |
| OPS-0003 | Production host may be a laptop that sleeps; write-cache durability unknown | Operations / hosting | Medium | P2 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/OPS-0003.md](ISSUES/OPS-0003.md) |
| INV-0013 | Recipe ↔ product association has two sources of truth (candidates: stale pointer after retarget, selling-price resolution mismatch) | Inventory / data model | Low | P3 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/INV-0013.md](ISSUES/INV-0013.md) |
| MENU-0003 | Price change between building the cart and checkout yields an unexplained refusal or unexpected total | Menu / UX | Low | P3 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/MENU-0003.md](ISSUES/MENU-0003.md) |
| PRT-0009 | Raster rendering of very long tickets may use large bitmaps | Printing / performance | Low | P3 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/PRT-0009.md](ISSUES/PRT-0009.md) |
| PRT-0018 | Ticket layout, price and encoding rendering candidates (prior-tool) | Printing / hardware | Low | P3 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/PRT-0018.md](ISSUES/PRT-0018.md) |
| RPT-0002 | Multi-query reports and the frozen close snapshot are not read from one consistent snapshot | Reporting / data consistency | Low | P3 | UNVERIFIED | NOT_VERIFIED | Medium | [ISSUES/RPT-0002.md](ISSUES/RPT-0002.md) |
| RPT-0005 | Owner dashboard candidates: reopened-day omission and derived open-exception count (prior-tool) | Reporting | Low | P3 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/RPT-0005.md](ISSUES/RPT-0005.md) |
| SEC-0030 | Reprint authorization and reprint voucher handling (prior-tool candidates) | Security / printing | Low | P3 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/SEC-0030.md](ISSUES/SEC-0030.md) |
| TEST-0003 | Test-only in-memory doubles and test context candidates (prior-tool) | Testing | Low | P3 | UNVERIFIED | NOT_VERIFIED | Low | [ISSUES/TEST-0003.md](ISSUES/TEST-0003.md) |

## 7. Not a defect / superseded (kept for traceability)

| ID | Title | Category | Severity | Priority | Implementation Status | Verification Level | Confidence | Issue File |
|---|---|---|---|---|---|---|---|---|
| PAYR-0016 | Claimed: employee cash advances are not written to the employee ledger | Payroll | High | P3 | NOT_A_DEFECT | STATIC_CODE_REVIEW | High | [ISSUES/PAYR-0016.md](ISSUES/PAYR-0016.md) |
| SEC-0028 | Claimed: role-casing bypass in PinHasher (Copilot executive summary) | Security | High | P3 | NOT_A_DEFECT | STATIC_CODE_REVIEW | High | [ISSUES/SEC-0028.md](ISSUES/SEC-0028.md) |
| SEC-0029 | Claimed: authentication middleware lets non-/api paths bypass authentication | Security | High | P3 | NOT_A_DEFECT | STATIC_CODE_REVIEW | High | [ISSUES/SEC-0029.md](ISSUES/SEC-0029.md) |
| FIN-0043 | Month/year/YTD/custom totals not proven consistent with sums of days | Reporting / verification | Medium | P2 | NOT_A_DEFECT | CURRENT_TEST_VERIFIED | Low | [ISSUES/FIN-0043.md](ISSUES/FIN-0043.md) |
| INV-0005 | Claimed: refunds and cancellations never restock inventory | Inventory | Medium | P3 | NOT_A_DEFECT | STATIC_CODE_REVIEW | High | [ISSUES/INV-0005.md](ISSUES/INV-0005.md) |
| SEC-0018 | Claimed: approval double-spending race | Security / approvals | Medium | P3 | NOT_A_DEFECT | STATIC_CODE_REVIEW | High | [ISSUES/SEC-0018.md](ISSUES/SEC-0018.md) |
| DOC-0005 | Prior-tool identifiers that are process artifacts or numbering gaps, not defects | Documentation / traceability | Low | P3 | NOT_A_DEFECT | STATIC_CODE_REVIEW | Medium | [ISSUES/DOC-0005.md](ISSUES/DOC-0005.md) |
| FIN-0024 | Delivery report field "expected unsettled custody" sums collected amounts | Reporting | Low | P3 | NOT_A_DEFECT | CURRENT_TEST_VERIFIED | Medium | [ISSUES/FIN-0024.md](ISSUES/FIN-0024.md) |
| FIN-0050 | CalculateExpectedDrawerCash signature lacks explicit refunds/safe-drop parameters | Code quality | Low | P3 | NOT_A_DEFECT | STATIC_CODE_REVIEW | High | [ISSUES/FIN-0050.md](ISSUES/FIN-0050.md) |
| SEC-0013 | Claimed: deactivating a user leaves their sessions valid | Security / sessions | Low | P3 | NOT_A_DEFECT | STATIC_CODE_REVIEW | High | [ISSUES/SEC-0013.md](ISSUES/SEC-0013.md) |
| SEC-0031 | PIN hash cryptography parameters (prior-tool candidate) | Security / crypto | Low | P3 | NOT_A_DEFECT | STATIC_CODE_REVIEW | Medium | [ISSUES/SEC-0031.md](ISSUES/SEC-0031.md) |

## 8. Fixed and verified by current tests

| ID | Title | Category | Severity | Priority | Implementation Status | Verification Level | Confidence | Issue File |
|---|---|---|---|---|---|---|---|---|
| API-0003 | Deterministic server errors (5xx) are treated as "outcome unknown", so kept requests can stay pending forever | API / client reliability | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/API-0003.md](ISSUES/API-0003.md) |
| FIN-0001 | Electronic leg of a split (cash + InstaPay/Wallet) order can never be confirmed | Financial / payments | High | P0 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0001.md](ISSUES/FIN-0001.md) |
| FIN-0002 | Cancelling the pending electronic leg of a split order leaves its cash leg posted in the drawer | Financial / payments | High | P0 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0002.md](ISSUES/FIN-0002.md) |
| FIN-0003 | Driver custody for a split order with a pending leg includes the cash already collected at the till | Financial / delivery | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0003.md](ISSUES/FIN-0003.md) |
| FIN-0004 | A split with two electronic methods (InstaPay + Wallet) silently loses the second electronic amount | Financial / payments | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0004.md](ISSUES/FIN-0004.md) |
| FIN-0007 | Refunds can pay out money that was never collected (unpaid COD, unconfirmed InstaPay) | Financial / refunds | High | P0 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0007.md](ISSUES/FIN-0007.md) |
| FIN-0010 | A sale can commit into a shift that has just closed and handed over | Financial / concurrency | High | P0 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0010.md](ISSUES/FIN-0010.md) |
| FIN-0011 | Shift-open float and purchase receipt check account balances before their transaction | Financial / concurrency | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0011.md](ISSUES/FIN-0011.md) |
| FIN-0012 | Concurrent payments of one supplier invoice can overpay it | Financial / purchasing | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0012.md](ISSUES/FIN-0012.md) |
| FIN-0013 | Money commands admitted before a business-day close can still post after it | Financial / concurrency | High | P1 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/FIN-0013.md](ISSUES/FIN-0013.md) |
| FIN-0028 | Business-date range computation throws on Cairo's DST start date | Time / reliability | High | P0 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0028.md](ISSUES/FIN-0028.md) |
| MENU-0002 | Saving a product without a size that was ever sold fails with 500 forever and locks the station | Menu / reliability | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/MENU-0002.md](ISSUES/MENU-0002.md) |
| PAYR-0001 | A payroll run can be marked Paid through the generic status transition without paying anyone | Payroll | High | P0 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PAYR-0001.md](ISSUES/PAYR-0001.md) |
| PAYR-0002 | Attendance-correction credits never reach payroll | Payroll | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PAYR-0002.md](ISSUES/PAYR-0002.md) |
| PAYR-0003 | Concurrent pay and cancel of one payroll run can both act | Payroll / concurrency | High | P1 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/PAYR-0003.md](ISSUES/PAYR-0003.md) |
| PRT-0007 | Printer verification tool deletes real queued restaurant tickets | Printing / tooling | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PRT-0007.md](ISSUES/PRT-0007.md) |
| PRT-0014 | Call Center customer-copy reprint also reprints kitchen tickets | Printing / UI | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PRT-0014.md](ISSUES/PRT-0014.md) |
| SEC-0001 | Employee national ID, phone, address and salary are readable by any logged-in session | Security / privacy | High | P0 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0001.md](ISSUES/SEC-0001.md) |
| SEC-0009 | PIN-change "proof" login replaces the shared session token | Security / sessions | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0009.md](ISSUES/SEC-0009.md) |
| SEC-0035 | A PIN choice that hit another account was no longer recorded (collision audit rolled back) | Security / audit | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0035.md](ISSUES/SEC-0035.md) |
| TEST-0001 | Integration suite was red at its last run (19/760) and several new test classes never ran | Testing | High | P0 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/TEST-0001.md](ISSUES/TEST-0001.md) |
| UI-0001 | Treasury and management Shift screens lose retry identity (new id per attempt, memory-only) | UI / reliability | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0001.md](ISSUES/UI-0001.md) |
| UI-0008 | A late Call Center customer lookup can overwrite the form for a different phone | UI / async race | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0008.md](ISSUES/UI-0008.md) |
| UI-0012 | A successful restore is reported as "not completed" | UI / backup | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0012.md](ISSUES/UI-0012.md) |
| UI-0014 | Recovered payroll transition/payment cannot reach its retry command | UI / recovery | High | P1 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/UI-0014.md](ISSUES/UI-0014.md) |
| UI-0015 | Late recipe-detail response can replace another selected recipe before editing | UI / async race | High | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0015.md](ISSUES/UI-0015.md) |
| UI-0017 | Late product-image completion re-targets the editor to the previous product | UI / async race | High | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0017.md](ISSUES/UI-0017.md) |
| UI-0028 | WinUI payroll screen compares lowercase statuses, so it cannot advance or pay a real run | UI / payroll | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0028.md](ISSUES/UI-0028.md) |
| UI-0032 | The delivery board and the assign dialog told the driver to collect money from orders that were already paid | UI / delivery money | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0032.md](ISSUES/UI-0032.md) |
| CUS-0001 | Customer names, phones and addresses are readable and editable by any logged-in session | Security / privacy | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/CUS-0001.md](ISSUES/CUS-0001.md) |
| DB-0002 | RenameDineInOrdersToPickup.Down rewrites every pickup order | Database / migrations | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/DB-0002.md](ISSUES/DB-0002.md) |
| DB-0004 | Backup file names collide within one second; retention can then delete the newest backup | Database / backup | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/DB-0004.md](ISSUES/DB-0004.md) |
| DB-0006 | Successful restore recorded as failed if SET MULTI_USER then fails | Database / backup | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/DB-0006.md](ISSUES/DB-0006.md) |
| DB-0007 | Server migrates the database automatically on every start | Database / deployment | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/DB-0007.md](ISSUES/DB-0007.md) |
| DB-0011 | A missing or unreachable database was reported as "server in maintenance" instead of the handled error | Database / reliability | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/DB-0011.md](ISSUES/DB-0011.md) |
| FIN-0006 | Void report classifies every non-COD void as "electronic_unconfirmed" | Reporting / financial | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0006.md](ISSUES/FIN-0006.md) |
| FIN-0014 | A late electronic-payment confirmation posts into the original (possibly closed) business day | Financial / periods | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/FIN-0014.md](ISSUES/FIN-0014.md) |
| FIN-0015 | Checkout accepts negative delivery fee, discount and comp amounts | Financial / input validation | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0015.md](ISSUES/FIN-0015.md) |
| FIN-0016 | Delivery-service fee can be stored negative | Financial / input validation | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0016.md](ISSUES/FIN-0016.md) |
| FIN-0019 | Cash denomination count accepts arbitrary denominations and unchecked multiplication | Financial / input validation | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0019.md](ISSUES/FIN-0019.md) |
| FIN-0021 | Business-day close revision is computed as Count+1 without a unique constraint | Database / concurrency | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/FIN-0021.md](ISSUES/FIN-0021.md) |
| FIN-0022 | Two concurrent business-day opens can create two active days | Database / concurrency | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0022.md](ISSUES/FIN-0022.md) |
| FIN-0025 | Same-id retry racing its own full refund is reported as refused (422) though the refund succeeded | Financial / idempotency | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0025.md](ISSUES/FIN-0025.md) |
| FIN-0031 | Tax mode accepted as any parseable enum (e.g. "1") and stored raw; checkout treats it as Exclusive | Financial / input normalisation | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0031.md](ISSUES/FIN-0031.md) |
| HW-0001 | Cash-drawer kicks wait behind the print backlog and expire after 30 seconds | Hardware | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/HW-0001.md](ISSUES/HW-0001.md) |
| INV-0001 | Inventory unit change can race the first stock movement | Inventory / concurrency | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/INV-0001.md](ISSUES/INV-0001.md) |
| INV-0007 | Inventory count sheet posts variance against the balance loaded when the sheet was opened | Inventory / stale state | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/INV-0007.md](ISSUES/INV-0007.md) |
| INV-0009 | Purchase receipt lines accept negative quantity and unit cost | Inventory / input validation | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/INV-0009.md](ISSUES/INV-0009.md) |
| MENU-0001 | Server accepts negative product-variant prices | Menu / input validation | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/MENU-0001.md](ISSUES/MENU-0001.md) |
| PAYR-0008 | Payroll selects a period's ledger entries by the UTC calendar month | Payroll / time | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PAYR-0008.md](ISSUES/PAYR-0008.md) |
| PAYR-0009 | Attendance stores UTC "HH:mm", rebuilds check-in on the checkout's UTC date, and shows UTC times | Payroll / time | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PAYR-0009.md](ISSUES/PAYR-0009.md) |
| PAYR-0010 | Manual daily-wage and overtime commands accept negative custom wage/rate | Payroll / input validation | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PAYR-0010.md](ISSUES/PAYR-0010.md) |
| PAYR-0012 | Compensation type accepted case-insensitively but stored raw; payroll compares exact names | Payroll / input normalisation | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PAYR-0012.md](ISSUES/PAYR-0012.md) |
| PAYR-0013 | Employee ledger BalanceAfter snapshots can be stale under concurrent writers | Payroll / concurrency | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/PAYR-0013.md](ISSUES/PAYR-0013.md) |
| PAYR-0014 | Retry of a committed daily-wage/overtime/extra-shift is refused because the approval was spent before the replay check | Payroll / idempotency | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PAYR-0014.md](ISSUES/PAYR-0014.md) |
| PERF-0005 | Inventory list loads every stock movement | Performance | Medium | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PERF-0005.md](ISSUES/PERF-0005.md) |
| PRT-0001 | Duplicate kitchen jobs can be created for one order and route | Printing | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PRT-0001.md](ISSUES/PRT-0001.md) |
| PRT-0002 | A late print result from an agent that lost its claim overwrites the job state | Printing / concurrency | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PRT-0002.md](ISSUES/PRT-0002.md) |
| PRT-0003 | The agent leases the whole backlog for a fixed 120 s with no renewal; a slow drain is re-claimed and printed again | Printing / distributed ownership | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/PRT-0003.md](ISSUES/PRT-0003.md) |
| PRT-0010 | An unwritable print-attempt journal silently stalls the route forever | Printing / deployment | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/PRT-0010.md](ISSUES/PRT-0010.md) |
| PRT-0015 | Printer status screen lists 4 of 6 routes | Printing / UI | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PRT-0015.md](ISSUES/PRT-0015.md) |
| SEC-0002 | Payroll runs, backups, purchases and supplier ledgers are readable by any session | Security / authorization | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0002.md](ISSUES/SEC-0002.md) |
| SEC-0003 | /api/users exposes every user's role and full permission list | Security / authorization | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0003.md](ISSUES/SEC-0003.md) |
| SEC-0004 | Print ticket content (customer name, delivery address) and pending queue readable by any session | Security / privacy | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0004.md](ISSUES/SEC-0004.md) |
| SEC-0008 | PINs and customer data in pending requests are stored as plain JSON on till disks | Security / data at rest | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0008.md](ISSUES/SEC-0008.md) |
| SEC-0010 | PIN uniqueness check-then-act race | Security / concurrency | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/SEC-0010.md](ISSUES/SEC-0010.md) |
| SEC-0012 | Reactivating a user does not check PIN uniqueness | Security / identity | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0012.md](ISSUES/SEC-0012.md) |
| SEC-0014 | Free-text roles compared case-sensitively bypass the single-owner rule, owner protection and cashier expense scoping | Security / authorization | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0014.md](ISSUES/SEC-0014.md) |
| SEC-0033 | Customer name, phone and address are readable by any session through order and delivery reads | Security / customer data | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0033.md](ISSUES/SEC-0033.md) |
| SEC-0034 | Driver custody balances and statistics are readable by any session | Security / financial reads | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0034.md](ISSUES/SEC-0034.md) |
| UI-0003 | Refused retries of kept requests: discard exists for the cashier only, and one server path misreports success as refusal | UI / recovery | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0003.md](ISSUES/UI-0003.md) |
| UI-0007 | A new pickup order inherits the previous order's pickup time | UI / state | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0007.md](ISSUES/UI-0007.md) |
| UI-0009 | Keyboard shortcuts act behind most open modals | UI / state | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/UI-0009.md](ISSUES/UI-0009.md) |
| UI-0010 | Discarded Call Center reprint returns after restart | UI / recovery | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/UI-0010.md](ISSUES/UI-0010.md) |
| UI-0013 | Call Center order panels leak through the cart subscription | UI / memory | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0013.md](ISSUES/UI-0013.md) |
| UI-0016 | Recipe yield conversion truncates fractional pieces-per-unit | UI / input | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0016.md](ISSUES/UI-0016.md) |
| UI-0018 | Recipe ingredient picker shows 60 items with no search box | UI / usability | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0018.md](ISSUES/UI-0018.md) |
| UI-0031 | The WinUI delivery board never flags an unconfirmed electronic transfer | UI / delivery | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0031.md](ISSUES/UI-0031.md) |
| UI-0033 | The Call Center had no dark theme; on a dark Windows it mixed dark system controls with fixed light panels | UI / theming | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0033.md](ISSUES/UI-0033.md) |
| UI-0035 | Shift opening/closing dialogs hide their primary action at short heights or with long errors | UI / layout and usability | Medium | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0035.md](ISSUES/UI-0035.md) |
| UI-0036 | Offer pills on product tiles could not be used by keyboard or UI Automation and covered the tile text | UI / interaction and accessibility | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0036.md](ISSUES/UI-0036.md) |
| UI-0037 | Interactive controls exposed no accessible name (icon buttons, product/category tiles, unlabelled inputs) | UI / accessibility | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0037.md](ISSUES/UI-0037.md) |
| UI-0040 | Order lookup rejected the number printed on the receipt ("#0128", leading zeros, Arabic-Indic digits) | Functional / POS lookup | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0040.md](ISSUES/UI-0040.md) |
| UI-0041 | Pickup editor showed the date 1/1/1926 and time 0:00 when no pickup time was chosen | UI / data display | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0041.md](ISSUES/UI-0041.md) |
| UI-0043 | Call Center order lookup showed only area and street of the delivery address | UI / data display | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0043.md](ISSUES/UI-0043.md) |
| UI-0044 | Call Center "إلغاء الطلب" discarded a phone order and the caller's details on one tap | UI / destructive action | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0044.md](ISSUES/UI-0044.md) |
| ARCH-0006 | EfFinancialJournal.Find throws NotSupportedException (interface contract) | Code quality | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/ARCH-0006.md](ISSUES/ARCH-0006.md) |
| FIN-0023 | Safe drops and expenses can be attributed to a closed shift | Financial / shifts | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0023.md](ISSUES/FIN-0023.md) |
| FIN-0026 | Money arithmetic overflow edge cases (Abs on long.MinValue; unchecked sums) | Financial / arithmetic | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/FIN-0026.md](ISSUES/FIN-0026.md) |
| INV-0002 | Concurrent opposite prep-recipe edges can create a costing cycle | Inventory / concurrency | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/INV-0002.md](ISSUES/INV-0002.md) |
| INV-0003 | Yield conversion rounds per portion before multiplying by quantity | Inventory / costing | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/INV-0003.md](ISSUES/INV-0003.md) |
| INV-0004 | Unknown loss value counted as zero in the loss summary | Inventory / reporting | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/INV-0004.md](ISSUES/INV-0004.md) |
| INV-0008 | Purchase unit costs are readable by any session through the stock-movement and inventory-list endpoints, bypassing the cost gate | Security / authorization | Low | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/INV-0008.md](ISSUES/INV-0008.md) |
| INV-0012 | Inventory item detail ignores the date range for its movement rows | Inventory / reporting | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/INV-0012.md](ISSUES/INV-0012.md) |
| PAYR-0011 | Manual ledger accepts undefined numeric entry types | Payroll / input normalisation | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PAYR-0011.md](ISSUES/PAYR-0011.md) |
| PAYR-0020 | Concurrent same-id ledger-credit requests (daily wage, overtime, extra shift) can return 500 | Payroll / concurrency | Low | P2 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/PAYR-0020.md](ISSUES/PAYR-0020.md) |
| PRT-0011 | Print agent keeps no persistent log | Printing / observability | Low | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PRT-0011.md](ISSUES/PRT-0011.md) |
| PRT-0017 | Shift close slip reprint recomputes from live data | Printing / reporting | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/PRT-0017.md](ISSUES/PRT-0017.md) |
| SEC-0005 | Settings (including cash discrepancy threshold) and business-day list readable by any session | Security / authorization | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0005.md](ISSUES/SEC-0005.md) |
| SEC-0032 | A login that fails with a server error or cancellation is counted as a wrong PIN | Security / login throttling | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/SEC-0032.md](ISSUES/SEC-0032.md) |
| UI-0022 | Call Center shows internal payment codes ("cash", "instapay") in the Arabic UI | UI / localisation | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | Medium | [ISSUES/UI-0022.md](ISSUES/UI-0022.md) |
| UI-0023 | Call Center labels a cancelled unpaid order "collect on delivery" | UI / financial display | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0023.md](ISSUES/UI-0023.md) |
| UI-0029 | Pickup-time getter throws for a 00:00–00:59 pickup on Cairo's DST start date | UI / time | Low | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0029.md](ISSUES/UI-0029.md) |
| UI-0030 | The WinUI till never offers a refund for a settled cash-on-delivery order | UI / refunds | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0030.md](ISSUES/UI-0030.md) |
| UI-0034 | Cart lines ran to both panel edges (Call Center and Cashier POS): ItemsControl Padding is ignored | UI / layout | Low | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0034.md](ISSUES/UI-0034.md) |
| UI-0038 | Modal dialogs did not close with Escape | UI / keyboard | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0038.md](ISSUES/UI-0038.md) |
| UI-0039 | Toasts and the header monogram used literal colours that were unreadable in the Dark theme | UI / theming | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0039.md](ISSUES/UI-0039.md) |
| UI-0042 | Kitchen-note chips added their note again on every tap ("حار، حار" on the kitchen ticket) | UI / data entry | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0042.md](ISSUES/UI-0042.md) |
| UI-0045 | Shifts history reprint buttons gave no feedback, so a failed reprint was silent | UI / feedback | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0045.md](ISSUES/UI-0045.md) |
| UI-0046 | Control Center checklist rows did not expose done / not done to assistive technology | UI / accessibility | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0046.md](ISSUES/UI-0046.md) |
| UI-0047 | Menu screen availability buttons all had the same accessible name | UI / accessibility | Low | P3 | FIXED | CURRENT_TEST_VERIFIED | High | [ISSUES/UI-0047.md](ISSUES/UI-0047.md) |

## 9. Whole-project review 2026-11-17

Findings from the whole-project code review. The evidence, the rejected findings and about thirty
minor items recorded for later (SEC-0038…0040, FIN-0055…0060, PAYR-0022…0024, INV-0015/0016,
RPT-0001…0005, UI-0053…0062, OPS-0008/0009) are in
[REVIEW_20261117_WHOLE_PROJECT.md](REVIEW_20261117_WHOLE_PROJECT.md). These rows are not yet counted
in the totals below.

| ID | Title | Category | Severity | Priority | Implementation Status | Verification Level | Confidence | Issue File |
|---|---|---|---|---|---|---|---|---|
| SEC-0036 | A users_manage holder could reset a more privileged account's PIN and log in as it | Security / users | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| FIN-0053 | Treasury transfer through the drawer bypassed the shift's expected cash (false shortage, double debit) | Financial / treasury | High | P1 | FIXED | CURRENT_TEST_VERIFIED | High | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| FIN-0052 | Cash in/out could post to a shift closing at the same moment | Financial / concurrency | Medium | P2 | FIXED | STATIC_CODE_REVIEW | Medium | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| FIN-0054 | Fully comped InstaPay/wallet order waited for a 0 ج transfer | Financial / payments | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| INV-0014 | Invoice with one ingredient on two lines could not be received | Purchasing | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| OPS-0007 | Settings accepted negative fees/rates, zero copies and zero workday hours | Settings / validation | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| PRT-0019 | A failing cash-drawer step stopped every receipt on the station | Printing / agent | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| UI-0048 | Call center froze on a kept checkout the server refused on retry | UI / durability | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| UI-0049 | Recovered treasury correction / supplier credit were invisible and blocking | UI / durability | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| UI-0050 | Recovered transfer confirm/cancel froze the till with no control | UI / durability | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| UI-0051 | Cash count read Arabic digits as 0 | UI / input | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| UI-0052 | Menu photos never showed (image control sends no token); photo GET made public - owner may reverse | UI / menu | Medium | P2 | FIXED | CURRENT_TEST_VERIFIED | Medium | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| TEST-0003 | Read-gate coverage test did not know the report-export routes | Testing | Low | P2 | FIXED | CURRENT_TEST_VERIFIED | High | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| SEC-0037 | Attendance check-in/out needs no permission and has an optional request id (owner question) | Security / payroll | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | High | [review](REVIEW_20261117_WHOLE_PROJECT.md) |
| PAYR-0021 | Dashboard wage cost excludes attendance-correction adjustments that payroll counts (accountant question) | Payroll / reports | Medium | P2 | NOT_FIXED_YET | STATIC_CODE_REVIEW | Medium | [review](REVIEW_20261117_WHOLE_PROJECT.md) |

## Totals

Canonical issues: **230**

| Implementation status | Count |
|---|---|
| FIXED | 145 |
| NOT_A_DEFECT | 11 |
| NOT_FIXED_YET | 43 |
| PARTIALLY_FIXED | 17 |
| UNVERIFIED | 14 |

| Verification level | Count |
|---|---|
| CODE_TEST_VERIFIED_ONSITE_PENDING | 1 |
| CURRENT_TEST_VERIFIED | 117 |
| EXISTING_TEST_EVIDENCE | 25 |
| NATIVE_UI_AND_CONTRAST_VERIFIED | 4 |
| NOT_VERIFIED | 17 |
| STATIC_CODE_REVIEW | 66 |

| Severity | Count |
|---|---|
| High | 53 |
| Low | 64 |
| Medium | 113 |

| Remediation group | Count |
|---|---|
| 1. Critical — fix before production | 4 |
| 2. High priority | 9 |
| 3. Medium priority | 22 |
| 4. Low priority / polish | 25 |
| 5. Fixed — needs re-test on the current tree | 34 |
| 6. Unverified risks (cannot yet be confirmed) | 14 |
| 7. Not a defect / superseded (kept for traceability) | 11 |
| 8. Fixed and verified by current tests | 111 |
