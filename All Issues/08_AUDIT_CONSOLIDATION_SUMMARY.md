# 08 — Audit Consolidation Summary

> Documentation cleanup (2026-11-08): imported audits and older reports are preserved in [the verified archive](../docs/archive/documentation-20261108.zip). Original historical paths below identify archive entries; use [the archive guide](../docs/archive/README.md) and its manifest to locate them. Current source, canonical issues and implementation evidence remain outside the archive.


**Prepared:** 2026-11-05. Read-only: no source, configuration, documentation, database or git state was changed.

## 1. Sources reviewed

| Source | Content |
|---|---|
| `AUDIT_REPORTS/` | 14 files (first audit) |
| `INDEPENDENT_AUDIT/` | 14 files (historical verification of 193 items plus new findings) |
| `PRODUCTION_RESILIENCE_AUDIT/` | 19 files (failure modes, 65 scenarios, 19 interleavings) |
| `reports from another tool/` | 9 094 files (Codex and Copilot). The 100-finding register was read in full. All finding IDs (161 distinct) were extracted, and the 61 not in the register were mapped individually. |
| `docs/` | `REMEDIATION_PLAN.md` backlog and Decisions; `REMEDIATION_PROGRESS.md` test records; deployment, LAN, print-agent and operations docs |
| `CLAUDE.md` and `.claude/skills` | Rules used to judge correctness |
| Source TODO/FIXME scan | 1 hit, a comment about a *resolved* TODO (`tests/Falah.Domain.Tests/OrderCancelElectronicPaymentTests.cs:9`); no open TODO/FIXME in `src` or `tests` |
| Git | Branch, HEAD and untracked state; history of `src/Falah.PrintAgent/appsettings.json` |
| `AGENTS.md` | Does not exist |

## 2. Consolidation results

| Measure | Value |
|---|---|
| Historical identifiers found | **417** (mapped: 417; unmapped: 0) |
| Relationship of historical IDs | 202 primary · 131 duplicate/merged · 54 scenario → issue · 30 scenario with no issue (safeguard holds) |
| Canonical issues | **203**, each with one file in `ISSUES/` and one row in the master registry |
| Implementation status | NOT_FIXED_YET 134 · FIXED 30 · PARTIALLY_FIXED 13 · UNVERIFIED 17 · NOT_A_DEFECT 9 · SUPERSEDED 0 |
| Verification level | STATIC_CODE_REVIEW 158 · EXISTING_TEST_EVIDENCE 25 · NOT_VERIFIED 20 · CURRENT_TEST_VERIFIED / HARDWARE_VERIFIED / PRODUCTION_VERIFIED 0 |
| Severity | High 50 · Medium 100 · Low 53 |
| Remediation groups | Critical 11 · High 40 · Medium 57 · Low 39 · Fixed-needs-retest 30 · Unverified 17 · Not a defect 9 |

**Status counts reconcile:** 134 + 30 + 13 + 17 + 9 = 203. **Group counts reconcile:** 11 + 40 + 57 + 39 + 30 + 17 + 9 = 203.

## 3. Mission A — new coverage and findings

**Workflows re-audited:** inventory (Partial → Deep), menu and pricing (Light → Partial), customers (Light → Deep),
reports (Light → Partial), plus cross-module interactions ([04](04_AUDIT_COVERAGE_MATRIX.md)).

**Newly registered issues (9):**
- [MENU-0002](ISSUES/MENU-0002.md) **High.** Removing a sold size fails with 500 forever and locks the station's lock, close and several screens.
- [API-0003](ISSUES/API-0003.md) **High.** Deterministic 5xx errors are treated as "outcome unknown", so kept requests can stay pending forever (the root cause behind MENU-0002).
- [INV-0007](ISSUES/INV-0007.md) **Medium.** The count sheet posts its variance against a stale balance, so sales during a count are applied twice.
- [CUS-0001](ISSUES/CUS-0001.md) **Medium.** Customer PII is readable and editable by any session.
- [INV-0008](ISSUES/INV-0008.md) **Low.** Purchase costs are readable through ungated movement and item endpoints.
- [RPT-0002](ISSUES/RPT-0002.md) **Low (unverified).** Reports and the close snapshot are not read from a single snapshot.
- [MENU-0003](ISSUES/MENU-0003.md) **Low (unverified).** A price change during an open cart may give an unexplained refusal.
- [CUS-0002](ISSUES/CUS-0002.md) **Low.** There is no customer erasure path (legal question).
- [INV-0011](ISSUES/INV-0011.md) **Low.** Historical food cost uses current recipes. This was an unregistered Codex lead.

**Historical items whose status changed:**
- [RPT-0001](ISSUES/RPT-0001.md) (FIN-CAND-REPORT-RANGE-001): unable to verify → confirmed, NOT_FIXED_YET.
- [FIN-0025](ISSUES/FIN-0025.md) (FIN-CAND-REFUND-REPLAY-001): confirmed in the resilience audit; kept.
- Unregistered prior-tool leads now confirmed:
  - [FIN-0023](ISSUES/FIN-0023.md) (FIN-SHIFT-002, expense posted to a closed shift);
  - [PRT-0017](ISSUES/PRT-0017.md) (close-slip recompute);
  - [INV-0012](ISSUES/INV-0012.md) (INV-DAT-001);
  - [DB-0009](ISSUES/DB-0009.md) (FIN-DEAD-STATE-001);
  - [ARCH-0006](ISSUES/ARCH-0006.md) (FIN-LSP-001, latent).
- [SEC-0031](ISSUES/SEC-0031.md) (SEC-CRYPTO-001): not a defect. PBKDF2-SHA256 at 210 000 iterations.

## 4. Deduplication decisions (examples)

| Root cause | Merged historical IDs | Canonical |
|---|---|---|
| Split void leaves the cash leg posted | FIN-002, FIN3b, AUD-FIN-02, SC-19 | FIN-0002 |
| Employee PII ungated | SEC-003, AUD-SEC-01 | SEC-0001 |
| Two active business days | FIN-009, FIN-CAND-BUSINESS-DAY-OPEN-RACE-001, CI-10, SC-33, IND-DB-IO-01 | FIN-0022 |
| Dashboard unsigned stock | FIN-013, FIN-029, C13, FIN-CAND-INVENTORY-DASHBOARD-UNSIGNED-BALANCE-001 | INV-0006 |
| PIN-proof token swap | UI-020, SEC-008, IND-SEC-IO-02 | SEC-0009 |
| Monolithic `Program.cs` / hand-written gates | AUD-ARCH-01, AUD-CQ-01, AUD-CQ-02, AUD-PERF-03, IND-ARCH-IO-01 | ARCH-0001 |
| Paper-out invisible / uncertain state | PRT-002, PRA-10, PRA-12 | PRT-0004 |

Distinct failure scenarios that share a root cause stay as separate canonical issues when their fix or test differs:
for example FIN-0015 (checkout signed inputs) and FIN-0016 (delivery-service fee), or FIN-0013 (admission race) and
FIN-0014 (late confirmation into a closed day).

## 5. Quality gate

| Check | Result |
|---|---|
| All previous audit directories reviewed | Yes (section 1) |
| Remediation documentation reviewed | Yes |
| Historical IDs fully accounted for | Yes: 417 of 417 mapped; aliases unique (the generator rejected one duplicate, `SEC7`, which was then resolved) |
| Under-audited workflows re-analysed | Yes (04) |
| New discoveries included | Yes (9) |
| Statuses independently verified | Yes, against current source (static), except UNVERIFIED items |
| Issue files self-contained | Yes: each carries its own evidence, behaviour, remediation, tests and acceptance; no earlier audit file is needed |
| Registry consistent | 203 rows = 203 files; every row links to an existing file |
| Evidence references | 155 cited files exist; 411 cited line numbers are within their files |
| Runtime uncertainty marked | Yes (verification level, plus 05) |
| Markdown links | Checked by script (see the closing report) |
| Only `All Issues/` created | Checked by `git status` comparison (see the closing report) |
| No source, docs or audit file changed; no commit | Yes |

## 6. Known gaps (not claimed as complete)
- No test, build or application run was permitted, so no issue has current-test, hardware or production verification.
- 17 issues remain UNVERIFIED (05).
- Blind spots are listed in [04](04_AUDIT_COVERAGE_MATRIX.md): concurrent menu edits, custody crash boundaries, the Call Center client beyond the paths traced, and hardware.
- About 9 000 prior-tool working files were searched by ID and keyword rather than read individually. An unindexed observation without an ID could therefore be missing.
