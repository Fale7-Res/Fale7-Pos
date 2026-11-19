# 03 — Prioritized Fix Roadmap

**Ordering principle:**
1. Preserve and prove: nothing can be verified without a green test suite.
2. Stop money loss.
3. Close data exposure.
4. Keep the restaurant operating: printing, stations.
5. Correctness of payroll and inventory.
6. Client reliability.
7. Database constraints and performance.
8. Polish.

Root-cause fixes that close several issues are grouped into one work item.

**Rules for every phase (from CLAUDE.md):**
- Write a failing test first, fix, then run the class and its neighbours.
- Integration tests run only against `FalahTest_<guid>` databases.
- Every migration is additive, reviewed and applied only after a verified backup.
- Business-policy items wait for the owner's or the accountant's decision.

The issue IDs link to self-contained records in [ISSUES/](ISSUES/).

## Phase 0 — Preserve the work and restore proof (before any fix)

| Item | Issues | Depends on | Expected effect | Regression risk | Testing | Acceptance |
|---|---|---|---|---|---|---|
| 0.1 Back up the untracked tree off the laptop: migrations, CLAUDE.md, docs, audits **(owner)** | [DOC-0003](ISSUES/DOC-0003.md), [DB-0001](ISSUES/DB-0001.md), [DOC-0006](ISSUES/DOC-0006.md) | — | No loss of the only copy | None | — | A copy exists off-machine, or a commit exists |
| 0.2 Rebuild; re-run the failing and never-run integration classes; then the full suite (in a scratch copy) | [TEST-0001](ISSUES/TEST-0001.md) | 0.1 | A known baseline | None | The full suite | Green except the expected RED tests |
| 0.3 Write the RED regression tests for the confirmed defects | [TEST-0002](ISSUES/TEST-0002.md) | 0.2 | Every later fix has a failing test | None | [06](06_TEST_AND_ACCEPTANCE_PLAN.md) | Each new test fails for its stated reason |
| 0.4 Re-run the tests of every FIXED issue on the current tree | All 30 FIXED issues (group 5 in [01](01_MASTER_ISSUE_REGISTRY.md)) | 0.2 | Converts static "fixed" into verified | None | Named tests per issue | All green |

## Phase 1 — Stop money loss (critical)

| Item | Issues | Depends on | Expected effect | Regression risk | Testing | Acceptance |
|---|---|---|---|---|---|---|
| 1.1 **Split-payment model.** Leg-level journal keys and confirmation; reverse posted legs on void; custody counts uncollected legs only; reject more than one electronic leg | [FIN-0001](ISSUES/FIN-0001.md), [FIN-0002](ISSUES/FIN-0002.md), [FIN-0003](ISSUES/FIN-0003.md), [FIN-0004](ISSUES/FIN-0004.md); later [FIN-0005](ISSUES/FIN-0005.md), [FIN-0006](ISSUES/FIN-0006.md), [UI-0023](ISSUES/UI-0023.md) | Owner: cash hand-back on void; multi refunds | Split orders complete; drawer and custody correct | Medium: touches checkout, confirm, cancel, delivery | Split tests (T-FIN-01/02), SplitOrderVoidTests, shift-close and treasury classes | All split scenarios reconcile |
| 1.2 Refund only collected money | [FIN-0007](ISSUES/FIN-0007.md) | Owner: COD refund policy | No drawer loss | Low | T-FIN-04 | Uncollected refunds refused |
| 1.3 **Money admission inside the transaction** (shared command scope: transaction → locks → day gate → replay) | [FIN-0010](ISSUES/FIN-0010.md), [FIN-0011](ISSUES/FIN-0011.md), [FIN-0012](ISSUES/FIN-0012.md), [FIN-0013](ISSUES/FIN-0013.md), [FIN-0023](ISSUES/FIN-0023.md), [ARCH-0003](ISSUES/ARCH-0003.md); unblocks [FIN-0014](ISSUES/FIN-0014.md), [RPT-0002](ISSUES/RPT-0002.md) | 0.2 | No sale in a closed shift, no overdraw, no overpay, no post-close money | Medium (lock ordering, deadlocks) | T-CONC-01/02/03; deadlock watch ([DB-0010](ISSUES/DB-0010.md)) | Concurrency tests green |
| 1.4 In-lock replay for refunds; replay before approval consumption for ledger credits | [FIN-0025](ISSUES/FIN-0025.md), [PAYR-0014](ISSUES/PAYR-0014.md) | — | Successful operations are never reported as refused | Low | T-FIN-06, PAYR replay test | Retries return replays |
| 1.5 Remove the generic Approved → Paid transition | [PAYR-0001](ISSUES/PAYR-0001.md) | — | Paid only when actually paid | Low | Transition test | Refused |
| 1.6 DST-safe business clock (**deadline 2027-04-30**) | [FIN-0028](ISSUES/FIN-0028.md) | — | No crash on the DST date | Low | T-TIME-01/02 | Tests green |

## Phase 2 — Data exposure and identity

| Item | Issues | Depends on | Expected effect | Regression risk | Testing | Acceptance |
|---|---|---|---|---|---|---|
| 2.1 Endpoint-enumeration test, then gate every sensitive read | [ARCH-0001](ISSUES/ARCH-0001.md), [SEC-0001](ISSUES/SEC-0001.md), [SEC-0002](ISSUES/SEC-0002.md), [SEC-0003](ISSUES/SEC-0003.md), [SEC-0004](ISSUES/SEC-0004.md), [SEC-0005](ISSUES/SEC-0005.md), [INV-0008](ISSUES/INV-0008.md), [CUS-0001](ISSUES/CUS-0001.md) | Owner: permission names | Least privilege | Medium: client screens may lose data, so trace callers first | Negative tests per route | The enumeration test lists none |
| 2.2 No PINs on disk (DPAPI store); PIN check without a token swap | [SEC-0008](ISSUES/SEC-0008.md), [SEC-0009](ISSUES/SEC-0009.md) | — | No credential exposure or identity confusion | Low | Client tests | No PIN in files; bearer unchanged |
| 2.3 PIN integrity: reactivation check, serialised choice, role normalisation | [SEC-0012](ISSUES/SEC-0012.md), [SEC-0010](ISSUES/SEC-0010.md), [SEC-0014](ISSUES/SEC-0014.md) | — | Unique PINs; canonical roles | Low | T-SEC-01 | Tests green |
| 2.4 Operator: rotate demo PINs, create print-agent accounts, enable HTTPS on site **(owner)** | [SEC-0022](ISSUES/SEC-0022.md), [SEC-0023](ISSUES/SEC-0023.md), [SEC-0024](ISSUES/SEC-0024.md), [SEC-0006](ISSUES/SEC-0006.md) | On-site | No published credentials; encrypted LAN | — | `docs/LAN_DEPLOYMENT.md` acceptance | Security endpoint clean; HTTPS in use |

## Phase 3 — Operational continuity (printing, stations, backup)

| Item | Issues | Depends on | Expected effect | Regression risk | Testing | Acceptance |
|---|---|---|---|---|---|---|
| 3.1 Self-starting, logged print agents (login retry, service registration, file log, journal probe in `%ProgramData%`) | [PRT-0012](ISSUES/PRT-0012.md), [PRT-0011](ISSUES/PRT-0011.md), [PRT-0010](ISSUES/PRT-0010.md) | — | Printing resumes after a power cut | Low | T-PRT-03/05; power-cut drill | First ticket prints with no manual action |
| 3.2 Print ownership: bounded or renewed claims; holder-checked reports; unique first-print jobs; Call Center reprint without kitchen routes | [PRT-0003](ISSUES/PRT-0003.md), [PRT-0002](ISSUES/PRT-0002.md), [PRT-0001](ISSUES/PRT-0001.md), [PRT-0014](ISSUES/PRT-0014.md) | Migration rules | No duplicate or overwritten tickets | Medium | T-PRT-01/02 | Each ticket once |
| 3.3 Drawer kick priority; on-site drawer test | [HW-0001](ISSUES/HW-0001.md), [HW-0002](ISSUES/HW-0002.md) | 3.1 | Drawer opens promptly | Low | T-HW-01, H-05 | On-site pass |
| 3.4 Deterministic errors are definitive; discard for management kept requests; retire sold sizes safely | [API-0003](ISSUES/API-0003.md), [MENU-0002](ISSUES/MENU-0002.md), [UI-0003](ISSUES/UI-0003.md) | — | No station lock-outs | Low | Client and API tests | No permanent pending operations |
| 3.5 Safe printer verification tool | [PRT-0007](ISSUES/PRT-0007.md) | — | No deleted real tickets | Low | — | Tool matches only its own job |
| 3.6 Backup and restore correctness | [DB-0004](ISSUES/DB-0004.md), [DB-0005](ISSUES/DB-0005.md), [DB-0006](ISSUES/DB-0006.md), [UI-0012](ISSUES/UI-0012.md), [OPS-0002](ISSUES/OPS-0002.md) | — | Reliable recovery | Medium | T-DB-09; restore rehearsal on a test database | Rehearsal passes |
| 3.7 Migration review and forward-only rule **(owner)** | [DB-0001](ISSUES/DB-0001.md), [DB-0002](ISSUES/DB-0002.md), [DB-0007](ISSUES/DB-0007.md) | 0.1 | Safe deployment | — | Rehearsal on a restored copy | Recorded review |

## Phase 4 — Payroll, inventory and input correctness

| Item | Issues | Depends on | Notes |
|---|---|---|---|
| 4.1 Signed-input validation at the API boundary | [FIN-0015](ISSUES/FIN-0015.md), [FIN-0016](ISSUES/FIN-0016.md), [MENU-0001](ISSUES/MENU-0001.md), [INV-0009](ISSUES/INV-0009.md), [PAYR-0010](ISSUES/PAYR-0010.md), [FIN-0019](ISSUES/FIN-0019.md) | — | One validator closes six issues |
| 4.2 Canonical enum persistence | [FIN-0031](ISSUES/FIN-0031.md), [PAYR-0011](ISSUES/PAYR-0011.md), [PAYR-0012](ISSUES/PAYR-0012.md) | — | — |
| 4.3 Payroll model | [PAYR-0002](ISSUES/PAYR-0002.md), [PAYR-0003](ISSUES/PAYR-0003.md), [PAYR-0005](ISSUES/PAYR-0005.md), [PAYR-0006](ISSUES/PAYR-0006.md), [PAYR-0008](ISSUES/PAYR-0008.md), [PAYR-0009](ISSUES/PAYR-0009.md), [PAYR-0013](ISSUES/PAYR-0013.md), [PAYR-0017](ISSUES/PAYR-0017.md) | Owner: [PAYR-0004](ISSUES/PAYR-0004.md), [PAYR-0007](ISSUES/PAYR-0007.md), [PAYR-0019](ISSUES/PAYR-0019.md) | Time basis (PAYR-0008/0009) together with FIN-0028 |
| 4.4 Inventory count sheet sends counted totals | [INV-0007](ISSUES/INV-0007.md) | — | — |
| 4.5 Inventory concurrency and rounding | [INV-0001](ISSUES/INV-0001.md), [INV-0002](ISSUES/INV-0002.md), [INV-0003](ISSUES/INV-0003.md), [INV-0004](ISSUES/INV-0004.md), [INV-0012](ISSUES/INV-0012.md), [UI-0016](ISSUES/UI-0016.md) | — | — |
| 4.6 Financial controls | [FIN-0020](ISSUES/FIN-0020.md), [FIN-0042](ISSUES/FIN-0042.md), [FIN-0030](ISSUES/FIN-0030.md) | Owner and accountant | — |

## Phase 5 — Client reliability and accessibility

| Item | Issues |
|---|---|
| 5.1 Durable retry everywhere | [UI-0001](ISSUES/UI-0001.md), [UI-0014](ISSUES/UI-0014.md), [UI-0010](ISSUES/UI-0010.md), [UI-0006](ISSUES/UI-0006.md) |
| 5.2 Async identity guards | [UI-0008](ISSUES/UI-0008.md), [UI-0015](ISSUES/UI-0015.md), [UI-0017](ISSUES/UI-0017.md) |
| 5.3 State bugs | [UI-0007](ISSUES/UI-0007.md), [UI-0009](ISSUES/UI-0009.md), [UI-0013](ISSUES/UI-0013.md), [UI-0018](ISSUES/UI-0018.md), [PRT-0015](ISSUES/PRT-0015.md), [PRT-0016](ISSUES/PRT-0016.md) |
| 5.4 Contrast and labels | [UI-0004](ISSUES/UI-0004.md), [UI-0019](ISSUES/UI-0019.md), [UI-0020](ISSUES/UI-0020.md), [UI-0021](ISSUES/UI-0021.md), [UI-0022](ISSUES/UI-0022.md) |
| 5.5 Desktop verification (UI5) | [UI-0027](ISSUES/UI-0027.md), [RPT-0003](ISSUES/RPT-0003.md), [UI-0005](ISSUES/UI-0005.md) |

## Phase 6 — Database constraints and performance
- **Constraints:** [FIN-0021](ISSUES/FIN-0021.md), [FIN-0022](ISSUES/FIN-0022.md).
- **Indexes and aggregates:** [PERF-0001](ISSUES/PERF-0001.md), [PERF-0002](ISSUES/PERF-0002.md), [PERF-0003](ISSUES/PERF-0003.md), [PERF-0004](ISSUES/PERF-0004.md), [PERF-0005](ISSUES/PERF-0005.md), [PERF-0006](ISSUES/PERF-0006.md).
- **Timeouts:** [API-0004](ISSUES/API-0004.md).
- **Reports:** [RPT-0001](ISSUES/RPT-0001.md), [RPT-0002](ISSUES/RPT-0002.md).
- **Process:** every index is an additive migration (deployment rules). Measure on a seeded `FalahTest_*` database first.

## Phase 7 — Low priority and polish
All remaining issues in group 4 of [01](01_MASTER_ISSUE_REGISTRY.md): API contracts, architecture clean-up, legacy WPF client, documentation corrections.

## Unverified risks
Resolve each with the verification listed in [05](05_UNVERIFIED_RISKS.md) before deciding whether a fix is needed.

## Owner and accountant decisions blocking fixes

| Decision | Issues |
|---|---|
| Split-order void cash hand-back; multi-payment refunds | FIN-0002, FIN-0005 |
| Refund of uncollected COD orders | FIN-0007 |
| Late-confirmation day attribution | FIN-0014 |
| VAT on comped orders; VAT-registration assumption | FIN-0030, FIN-0029 |
| Business-day count re-count approval (FIN5-D) | FIN-0020 |
| Payroll proration (FIN2-D); advance/settlement netting; rounding rule; payroll month basis | PAYR-0007, PAYR-0004, PAYR-0019, PAYR-0008 |
| Permission names for gated reads; which roles may read customers | SEC-0001, SEC-0002, CUS-0001 |
| Approval transaction binding (SEC3-D); login model (SEC1-D) | SEC-0017, SEC-0020 |
| Automatic retry of failed prints; held-ticket auto-release; two-stage pickup | PRT-0013, PRT-0016, UI-0025 |
| Historical food-cost basis; customer erasure (legal) | INV-0011, CUS-0002 |
