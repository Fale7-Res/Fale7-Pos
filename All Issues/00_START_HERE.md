# All Issues — Start Here

> Documentation cleanup (2026-11-08): imported audits and older reports are preserved in [the verified archive](../docs/archive/documentation-20261108.zip). Original historical paths below identify archive entries; use [the archive guide](../docs/archive/README.md) and its manifest to locate them. Current source, canonical issues and implementation evidence remain outside the archive.


This folder is the **single authoritative issue registry** for Falah Restaurant POS. It consolidates:
- every finding from four audits;
- the remediation backlog;
- the other tools' audit work;
- the issues newly discovered while completing coverage.

**Every issue was re-verified against the current source code.** A future engineer or AI agent needs only this
folder and the repository source.

**State audited:**
- branch `clients-and-operations-audit`, HEAD `b3071be`, **including uncommitted changes**;
- source read on 2026-11-04/05;
- static review only: no tests, builds or application runs.

## How to use it

1. **Pick work** from [03_PRIORITIZED_FIX_ROADMAP.md](03_PRIORITIZED_FIX_ROADMAP.md). Phase 0 comes first: back up the
   untracked work and get the test suite green.
2. **Open the issue file** from [01_MASTER_ISSUE_REGISTRY.md](01_MASTER_ISSUE_REGISTRY.md). Each `ISSUES/<ID>.md` has 18
   numbered sections after a header table:
   - evidence with `file:line` and symbols;
   - expected vs actual behaviour, root cause, impact, scenario;
   - safeguards, remediation and alternatives;
   - required tests, related issues, dependencies;
   - acceptance criteria and remaining uncertainty.
3. **Write the RED test first.** Specifications are in [06_TEST_AND_ACCEPTANCE_PLAN.md](06_TEST_AND_ACCEPTANCE_PLAN.md).
   Then fix the issue and run its class and neighbours.
4. **Update the issue file** after a fix: set the implementation status and verification level, and add the commit and
   test evidence. Keep the IDs stable. New issues take the next free number in their prefix; gaps in the numbering
   are intentional.
5. **Respect the project rules** (`CLAUDE.md`):
   - never touch the real `Falah` database; tests use `FalahTest_*`;
   - migrations are additive and applied only after a verified backup;
   - business-policy questions go to the owner or accountant (each such issue says so under *Dependencies*).

## Files

| File | Purpose |
|---|---|
| [01_MASTER_ISSUE_REGISTRY.md](01_MASTER_ISSUE_REGISTRY.md) | One row per canonical issue (203), grouped by remediation priority, with totals |
| [02_HISTORICAL_ID_MAPPING.md](02_HISTORICAL_ID_MAPPING.md) | All 417 historical IDs → canonical issue, original source and status, current status, relationship |
| [03_PRIORITIZED_FIX_ROADMAP.md](03_PRIORITIZED_FIX_ROADMAP.md) | Phases 0–7, root-cause work items, owner decisions |
| [04_AUDIT_COVERAGE_MATRIX.md](04_AUDIT_COVERAGE_MATRIX.md) | Coverage per workflow; cross-system checks; blind spots |
| [05_UNVERIFIED_RISKS.md](05_UNVERIFIED_RISKS.md) | What still needs runtime, hardware or site evidence |
| [06_TEST_AND_ACCEPTANCE_PLAN.md](06_TEST_AND_ACCEPTANCE_PLAN.md) | Test specifications and production acceptance gate |
| [07_SOURCE_AND_EVIDENCE_INDEX.md](07_SOURCE_AND_EVIDENCE_INDEX.md) | Source file → issues; sources → IDs; test evidence |
| [08_AUDIT_CONSOLIDATION_SUMMARY.md](08_AUDIT_CONSOLIDATION_SUMMARY.md) | Sources, counts, deduplication, quality gate, gaps |
| [ISSUES/](ISSUES/) | 203 issue records |

## Status vocabulary

**Implementation status:**
- `FIXED`: the defect is absent from current source. Static unless the verification level says otherwise; re-test required.
- `PARTIALLY_FIXED`: "Done:" and "Remaining:" are stated in the evidence.
- `NOT_FIXED_YET`
- `UNVERIFIED`
- `NOT_A_DEFECT`: counterevidence given.
- `SUPERSEDED`: none at present.
- Historical *duplicates* are recorded in the mapping as "Duplicate/merged" against their canonical issue.

**Verification level:** `STATIC_CODE_REVIEW` · `EXISTING_TEST_EVIDENCE` (a recorded run that predates some current
edits) · `NOT_VERIFIED`. No issue has `CURRENT_TEST_VERIFIED`, `HARDWARE_VERIFIED` or `PRODUCTION_VERIFIED`.

**Priority:** P0 = before production · P1 = high · P2 = medium · P3 = low.

## Headline (details in 08)

| Measure | Count |
|---|---|
| Canonical issues | 203 |
| NOT_FIXED_YET | 134 |
| PARTIALLY_FIXED | 13 |
| FIXED (re-test needed) | 30 |
| UNVERIFIED | 17 |
| NOT_A_DEFECT | 9 |
| Critical (P0) open | 11, plus [TEST-0001](ISSUES/TEST-0001.md) (unverified suite state) |

**Critical issues:**
[DB-0001](ISSUES/DB-0001.md), [DOC-0003](ISSUES/DOC-0003.md), [FIN-0001](ISSUES/FIN-0001.md),
[FIN-0002](ISSUES/FIN-0002.md), [FIN-0007](ISSUES/FIN-0007.md), [FIN-0010](ISSUES/FIN-0010.md),
[FIN-0028](ISSUES/FIN-0028.md), [PAYR-0001](ISSUES/PAYR-0001.md), [PRT-0012](ISSUES/PRT-0012.md),
[SEC-0001](ISSUES/SEC-0001.md), [SEC-0006](ISSUES/SEC-0006.md).

## ID prefixes

`FIN` money, payments, refunds, shifts, business day · `PAYR` payroll and employees · `INV` inventory · `MENU` menu
and pricing · `CUS` customers · `RPT` reports · `SEC` security and privacy · `PRT` printing · `HW` hardware · `UI`
clients · `API` API contracts · `DB` database, migrations, backup · `PERF` performance · `ARCH` architecture and code
quality · `OPS` operations · `TEST` testing · `DOC` documentation.
