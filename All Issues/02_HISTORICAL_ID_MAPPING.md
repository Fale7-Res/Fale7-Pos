# 02 — Historical ID Mapping

> Documentation cleanup (2026-11-08): imported audits and older reports are preserved in [the verified archive](../docs/archive/documentation-20261108.zip). Original historical paths below identify archive entries; use [the archive guide](../docs/archive/README.md) and its manifest to locate them. Current source, canonical issues and implementation evidence remain outside the archive.


Every historical identifier found in any prior source, mapped to its canonical issue. "Primary" = the main historical ID of the canonical issue; "Duplicate/merged" = another ID for the same underlying defect; "Scenario" = a scenario or interleaving ID from the resilience audit; "No issue" = a scenario where a safeguard holds.

| Historical ID | Canonical ID | Original source | Original status | Current status | Relationship |
|---|---|---|---|---|---|
| A1 | [SEC-0002](ISSUES/SEC-0002.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | prepare-close done (batch 8); balance reads blocked: decision | PARTIALLY_FIXED (DUPLICATE → SEC-0002) | Duplicate/merged |
| A2 | [API-0001](ISSUES/API-0001.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | deferred (see Decisions) | NOT_FIXED_YET | Primary |
| A3 | [ARCH-0005](ISSUES/ARCH-0005.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 7) | FIXED | Primary |
| AUD-API-01 | [API-0001](ISSUES/API-0001.md) | AUDIT_REPORTS/08_DATABASE_API_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → API-0001) | Duplicate/merged |
| AUD-API-02 | [API-0002](ISSUES/API-0002.md) | AUDIT_REPORTS/08_DATABASE_API_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-API-03 | [SEC-0004](ISSUES/SEC-0004.md) | AUDIT_REPORTS/08_DATABASE_API_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → SEC-0004) | Duplicate/merged |
| AUD-ARCH-01 | [ARCH-0001](ISSUES/ARCH-0001.md) | AUDIT_REPORTS/07_ARCHITECTURE_DEPENDENCIES.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-ARCH-02 | [ARCH-0002](ISSUES/ARCH-0002.md) | AUDIT_REPORTS/07_ARCHITECTURE_DEPENDENCIES.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-ARCH-03 | [DB-0007](ISSUES/DB-0007.md) | AUDIT_REPORTS/07_ARCHITECTURE_DEPENDENCIES.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-CQ-01 | [ARCH-0001](ISSUES/ARCH-0001.md) | AUDIT_REPORTS/09_CODE_QUALITY_REFACTORING.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → ARCH-0001) | Duplicate/merged |
| AUD-CQ-02 | [ARCH-0001](ISSUES/ARCH-0001.md) | AUDIT_REPORTS/09_CODE_QUALITY_REFACTORING.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → ARCH-0001) | Duplicate/merged |
| AUD-CQ-03 | [ARCH-0002](ISSUES/ARCH-0002.md) | AUDIT_REPORTS/09_CODE_QUALITY_REFACTORING.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → ARCH-0002) | Duplicate/merged |
| AUD-CQ-04 | [TEST-0001](ISSUES/TEST-0001.md) | AUDIT_REPORTS/09_CODE_QUALITY_REFACTORING.md | Finding (2026-11-04 audit) | UNVERIFIED | Primary |
| AUD-CQ-05 | [DOC-0002](ISSUES/DOC-0002.md) | AUDIT_REPORTS/09_CODE_QUALITY_REFACTORING.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → DOC-0002) | Duplicate/merged |
| AUD-CQ-06 | [ARCH-0004](ISSUES/ARCH-0004.md) | AUDIT_REPORTS/09_CODE_QUALITY_REFACTORING.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-DB-01 | [DB-0001](ISSUES/DB-0001.md) | AUDIT_REPORTS/08_DATABASE_API_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → DB-0001) | Duplicate/merged |
| AUD-DB-02 | [PERF-0001](ISSUES/PERF-0001.md) | AUDIT_REPORTS/08_DATABASE_API_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → PERF-0001) | Duplicate/merged |
| AUD-DB-03 | [PERF-0002](ISSUES/PERF-0002.md) | AUDIT_REPORTS/08_DATABASE_API_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → PERF-0002) | Duplicate/merged |
| AUD-DB-04 | [DB-0003](ISSUES/DB-0003.md) | AUDIT_REPORTS/08_DATABASE_API_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → DB-0003) | Duplicate/merged |
| AUD-DB-05 | [DB-0002](ISSUES/DB-0002.md) | AUDIT_REPORTS/08_DATABASE_API_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-DEP-01 | [OPS-0001](ISSUES/OPS-0001.md) | AUDIT_REPORTS/07_ARCHITECTURE_DEPENDENCIES.md | Finding (2026-11-04 audit) | UNVERIFIED | Primary |
| AUD-DOC-01 | [DOC-0001](ISSUES/DOC-0001.md) | AUDIT_REPORTS/03_SECURITY_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-DOC-02 | [DOC-0002](ISSUES/DOC-0002.md) | AUDIT_REPORTS/10_NEW_FINDINGS.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-DOC-03 | [DOC-0003](ISSUES/DOC-0003.md) | AUDIT_REPORTS/10_NEW_FINDINGS.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-FIN-01 | [FIN-0006](ISSUES/FIN-0006.md) | AUDIT_REPORTS/06_FUNCTIONAL_LOGIC_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → FIN-0006) | Duplicate/merged |
| AUD-FIN-02 | [FIN-0002](ISSUES/FIN-0002.md) | AUDIT_REPORTS/06_FUNCTIONAL_LOGIC_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → FIN-0002) | Duplicate/merged |
| AUD-FIN-03 | [PAYR-0006](ISSUES/PAYR-0006.md) | AUDIT_REPORTS/06_FUNCTIONAL_LOGIC_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-FIN-04 | [PAYR-0007](ISSUES/PAYR-0007.md) | AUDIT_REPORTS/06_FUNCTIONAL_LOGIC_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → PAYR-0007) | Duplicate/merged |
| AUD-FIN-05 | [PAYR-0019](ISSUES/PAYR-0019.md) | AUDIT_REPORTS/06_FUNCTIONAL_LOGIC_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-FIN-06 | [FIN-0042](ISSUES/FIN-0042.md) | AUDIT_REPORTS/06_FUNCTIONAL_LOGIC_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → FIN-0042) | Duplicate/merged |
| AUD-FIN-07 | [FIN-0043](ISSUES/FIN-0043.md) | AUDIT_REPORTS/06_FUNCTIONAL_LOGIC_AUDIT.md | Finding (2026-11-04 audit) | UNVERIFIED (DUPLICATE → FIN-0043) | Duplicate/merged |
| AUD-PERF-01 | [PERF-0001](ISSUES/PERF-0001.md) | AUDIT_REPORTS/04_PERFORMANCE_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-PERF-02 | [PERF-0002](ISSUES/PERF-0002.md) | AUDIT_REPORTS/04_PERFORMANCE_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-PERF-03 | [ARCH-0001](ISSUES/ARCH-0001.md) | AUDIT_REPORTS/04_PERFORMANCE_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → ARCH-0001) | Duplicate/merged |
| AUD-SEC-01 | [SEC-0001](ISSUES/SEC-0001.md) | AUDIT_REPORTS/03_SECURITY_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → SEC-0001) | Duplicate/merged |
| AUD-SEC-02 | [SEC-0003](ISSUES/SEC-0003.md) | AUDIT_REPORTS/03_SECURITY_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-SEC-03 | [SEC-0002](ISSUES/SEC-0002.md) | AUDIT_REPORTS/03_SECURITY_AUDIT.md | Finding (2026-11-04 audit) | PARTIALLY_FIXED (DUPLICATE → SEC-0002) | Duplicate/merged |
| AUD-SEC-04 | [SEC-0004](ISSUES/SEC-0004.md) | AUDIT_REPORTS/03_SECURITY_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-SEC-05 | [SEC-0005](ISSUES/SEC-0005.md) | AUDIT_REPORTS/03_SECURITY_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-SEC-06 | [SEC-0006](ISSUES/SEC-0006.md) | AUDIT_REPORTS/03_SECURITY_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → SEC-0006) | Duplicate/merged |
| AUD-SEC-07 | [SEC-0007](ISSUES/SEC-0007.md) | AUDIT_REPORTS/03_SECURITY_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-SEC-08 | [API-0001](ISSUES/API-0001.md) | AUDIT_REPORTS/03_SECURITY_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → API-0001) | Duplicate/merged |
| AUD-UI-01 | [UI-0019](ISSUES/UI-0019.md) | AUDIT_REPORTS/05_UI_UX_ACCESSIBILITY_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-UI-02 | [UI-0020](ISSUES/UI-0020.md) | AUDIT_REPORTS/05_UI_UX_ACCESSIBILITY_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-UI-03 | [UI-0021](ISSUES/UI-0021.md) | AUDIT_REPORTS/05_UI_UX_ACCESSIBILITY_AUDIT.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| AUD-UI-04 | [RPT-0003](ISSUES/RPT-0003.md) | AUDIT_REPORTS/05_UI_UX_ACCESSIBILITY_AUDIT.md | Finding (2026-11-04 audit) | PARTIALLY_FIXED (DUPLICATE → RPT-0003) | Duplicate/merged |
| C1 | [FIN-0008](ISSUES/FIN-0008.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 1) | FIXED (DUPLICATE → FIN-0008) | Duplicate/merged |
| C10 | [FIN-0037](ISSUES/FIN-0037.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 15) | FIXED | Primary |
| C11 | [PAYR-0018](ISSUES/PAYR-0018.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 6) | FIXED | Primary |
| C12 | [FIN-0039](ISSUES/FIN-0039.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 6) | FIXED | Primary |
| C13 | [INV-0006](ISSUES/INV-0006.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 15) | FIXED (DUPLICATE → INV-0006) | Duplicate/merged |
| C2 | [FIN-0032](ISSUES/FIN-0032.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 4) | FIXED | Primary |
| C3 | [FIN-0033](ISSUES/FIN-0033.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 4) | FIXED | Primary |
| C4 | [FIN-0034](ISSUES/FIN-0034.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 3) | FIXED | Primary |
| C5 | [FIN-0035](ISSUES/FIN-0035.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 3) | FIXED | Primary |
| C6 | [PAYR-0017](ISSUES/PAYR-0017.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 14; daily accrual) | PARTIALLY_FIXED | Primary |
| C7 | [FIN-0036](ISSUES/FIN-0036.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 15) | FIXED | Primary |
| C8 | [FIN-0009](ISSUES/FIN-0009.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 2) | FIXED (DUPLICATE → FIN-0009) | Duplicate/merged |
| C9 | [FIN-0038](ISSUES/FIN-0038.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 5) | FIXED | Primary |
| CAND-SEC-PERIMETER-001 | [SEC-0029](ISSUES/SEC-0029.md) | Copilot 11-Master-Findings/REJECTED_CANDIDATES.json | Rejected | NOT_A_DEFECT | Primary |
| CI-01 | [FIN-0010](ISSUES/FIN-0010.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Checkout vs shift close | NOT_FIXED_YET | Scenario |
| CI-02 | [FIN-0012](ISSUES/FIN-0012.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two purchase payments | NOT_FIXED_YET | Scenario |
| CI-03 | [FIN-0014](ISSUES/FIN-0014.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Close evaluator vs pending checkout | NOT_FIXED_YET | Scenario |
| CI-04 | [FIN-0025](ISSUES/FIN-0025.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Same refund id race | NOT_FIXED_YET | Scenario |
| CI-05 | [PRT-0003](ISSUES/PRT-0003.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Lease expiry during slow drain | NOT_FIXED_YET | Scenario |
| CI-06 | [PRT-0002](ISSUES/PRT-0002.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Late report after handover | NOT_FIXED_YET | Scenario |
| CI-07 | [PRT-0001](ISSUES/PRT-0001.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two creators of kitchen jobs | PARTIALLY_FIXED | Scenario |
| CI-08 | [PAYR-0013](ISSUES/PAYR-0013.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two ledger posts | NOT_FIXED_YET | Scenario |
| CI-09 | [PAYR-0003](ISSUES/PAYR-0003.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Pay vs cancel payroll | NOT_FIXED_YET | Scenario |
| CI-10 | [FIN-0022](ISSUES/FIN-0022.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two business-day opens | NOT_FIXED_YET | Scenario |
| CI-11 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two confirms | No defect | No issue (safeguard holds) |
| CI-12 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two cash-outs one employee | No defect | No issue (safeguard holds) |
| CI-13 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two shift opens | No defect | No issue (safeguard holds) |
| CI-14 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Simultaneous stock deductions | No defect | No issue (safeguard holds) |
| CI-15 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: User deactivated in flight | No defect | No issue (safeguard holds) |
| CI-16 | [UI-0026](ISSUES/UI-0026.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Permission revoked during workflow | NOT_FIXED_YET | Scenario |
| CI-17 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two agents one machine | No defect | No issue (safeguard holds) |
| CI-18 | [HW-0001](ISSUES/HW-0001.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Drawer kick during print backlog | NOT_FIXED_YET | Scenario |
| CI-19 | [UI-0008](ISSUES/UI-0008.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Stale UI responses (also UI-0015, UI-0017) | NOT_FIXED_YET | Scenario |
| COPILOT-EXEC-SEC-001 | [SEC-0028](ISSUES/SEC-0028.md) | Copilot EXECUTIVE_SUMMARY.md §3.5 (claim differs from register SEC-001) | Claimed P1 | NOT_A_DEFECT | Primary |
| COPILOT-PRINT-NO-RETRY | [PRT-0013](ISSUES/PRT-0013.md) | Copilot 06-Printing narrative (no ID assigned there) | Narrative statement | NOT_FIXED_YET | Primary |
| D1 | [SEC-0006](ISSUES/SEC-0006.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | procedure documented (batch 18; docs/LAN_DEPLOYMENT.md); HTTPS needs on-site acceptance | NOT_FIXED_YET | Primary |
| D2 | [DB-0003](ISSUES/DB-0003.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | documented constraint | NOT_FIXED_YET | Primary |
| DB1 | [DB-0001](ISSUES/DB-0001.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | NOT_FIXED_YET | Primary |
| DEPLOYMENT-OFFSITE | [OPS-0002](ISSUES/OPS-0002.md) | All Issues (this consolidation) | New finding / registry alias | NOT_FIXED_YET | Primary |
| DOC1 | [DOC-0006](ISSUES/DOC-0006.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | IMPLEMENTED (CLAUDE.md + 5 skills) | FIXED | Primary |
| FIN-001 | [FIN-0001](ISSUES/FIN-0001.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| FIN-002 | [FIN-0002](ISSUES/FIN-0002.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| FIN-003 | [FIN-0007](ISSUES/FIN-0007.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| FIN-004 | [FIN-0008](ISSUES/FIN-0008.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | FIXED | Primary |
| FIN-005 | [FIN-0012](ISSUES/FIN-0012.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| FIN-006 | [PAYR-0001](ISSUES/PAYR-0001.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| FIN-007 | [DB-0004](ISSUES/DB-0004.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-008 | [DB-0005](ISSUES/DB-0005.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · HIGH | NOT_FIXED_YET | Primary |
| FIN-009 | [FIN-0022](ISSUES/FIN-0022.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-010 | [FIN-0009](ISSUES/FIN-0009.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | FIXED | Primary |
| FIN-011 | [FIN-0011](ISSUES/FIN-0011.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| FIN-012 | [PAYR-0002](ISSUES/PAYR-0002.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| FIN-013 | [INV-0006](ISSUES/INV-0006.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | FIXED | Primary |
| FIN-014 | [DB-0006](ISSUES/DB-0006.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · HIGH | NOT_FIXED_YET | Primary |
| FIN-015 | [FIN-0010](ISSUES/FIN-0010.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| FIN-016 | [FIN-0015](ISSUES/FIN-0015.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| FIN-017 | [FIN-0045](ISSUES/FIN-0045.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_OWNER_BUSINESS_DECISION · MEDIUM | UNVERIFIED | Primary |
| FIN-018 | [INV-0009](ISSUES/INV-0009.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-019 | [FIN-0003](ISSUES/FIN-0003.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| FIN-020 | [INV-0001](ISSUES/INV-0001.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-021 | [FIN-0031](ISSUES/FIN-0031.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_OWNER_BUSINESS_DECISION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-022 | [PAYR-0012](ISSUES/PAYR-0012.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_OWNER_BUSINESS_DECISION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-023 | [PAYR-0009](ISSUES/PAYR-0009.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-024 | [PAYR-0010](ISSUES/PAYR-0010.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-025 | [PAYR-0011](ISSUES/PAYR-0011.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-026 | [PAYR-0013](ISSUES/PAYR-0013.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-027 | [SEC-0026](ISSUES/SEC-0026.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | FIXED | Primary |
| FIN-028 | [PAYR-0014](ISSUES/PAYR-0014.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-029 | [INV-0006](ISSUES/INV-0006.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | FIXED (DUPLICATE → INV-0006) | Duplicate/merged |
| FIN-030 | [FIN-0026](ISSUES/FIN-0026.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | THEORETICAL_BCL_LIMITATION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-031 | [PAYR-0015](ISSUES/PAYR-0015.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-032 | [FIN-0016](ISSUES/FIN-0016.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CANDIDATE_FROM_SPECIALIST_HANDOFF · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-033 | [FIN-0028](ISSUES/FIN-0028.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| FIN-034 | [PAYR-0016](ISSUES/PAYR-0016.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_A_DEFECT | Primary |
| FIN-035 | [PERF-0003](ISSUES/PERF-0003.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-036 | [PERF-0004](ISSUES/PERF-0004.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-037 | [FIN-0005](ISSUES/FIN-0005.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_OWNER_BUSINESS_DECISION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN1 | [PAYR-0017](ISSUES/PAYR-0017.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | IMPLEMENTED (full run pending) | PARTIALLY_FIXED (DUPLICATE → PAYR-0017) | Duplicate/merged |
| FIN2 | [PAYR-0007](ISSUES/PAYR-0007.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED (likely BLOCKED: policy) | NOT_FIXED_YET | Primary |
| FIN2-D | [PAYR-0007](ISSUES/PAYR-0007.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | Owner decision | NOT_FIXED_YET (DUPLICATE → PAYR-0007) | Duplicate/merged |
| FIN3 | [FIN-0006](ISSUES/FIN-0006.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | NOT_FIXED_YET | Primary |
| FIN3b | [FIN-0002](ISSUES/FIN-0002.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | NOT_FIXED_YET (DUPLICATE → FIN-0002) | Duplicate/merged |
| FIN4 | [FIN-0042](ISSUES/FIN-0042.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | NOT_FIXED_YET | Primary |
| FIN5 | [FIN-0020](ISSUES/FIN-0020.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | PARTIALLY_FIXED | Primary |
| FIN5-D | [FIN-0020](ISSUES/FIN-0020.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | Owner decision | PARTIALLY_FIXED (DUPLICATE → FIN-0020) | Duplicate/merged |
| FIN6 | [FIN-0044](ISSUES/FIN-0044.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | NOT_FIXED_YET (DUPLICATE → FIN-0044) | Duplicate/merged |
| FIN7 | [FIN-0043](ISSUES/FIN-0043.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | UNVERIFIED | Primary |
| FIN-ATTR-001 | [FIN-0014](ISSUES/FIN-0014.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → FIN-0014) | Duplicate/merged |
| FIN-CAND-BUSINESS-DAY-OPEN-RACE-001 | [FIN-0022](ISSUES/FIN-0022.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | NOT_FIXED_YET (DUPLICATE → FIN-0022) | Duplicate/merged |
| FIN-CAND-CANCELLED-LOOKUP-PAYMENT-LABEL-001 | [UI-0023](ISSUES/UI-0023.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · LOW | NOT_FIXED_YET | Primary |
| FIN-CAND-CASHCOUNT-001 | [FIN-0019](ISSUES/FIN-0019.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-CAND-CASHIER-LEGACY-DECIMAL-ROUNDING-001 | [UI-0011](ISSUES/UI-0011.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → UI-0011) | Duplicate/merged |
| FIN-CAND-CLOSEDAY-ADMISSION-001 | [FIN-0013](ISSUES/FIN-0013.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · HIGH | NOT_FIXED_YET | Primary |
| FIN-CAND-CLOSE-SNAPSHOT-REVISION-001 | [FIN-0021](ISSUES/FIN-0021.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_OWNER_BUSINESS_DECISION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-CAND-CORRECTION-BASIS-001 | [PAYR-0005](ISSUES/PAYR-0005.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-CAND-DASHBOARD-DERIVED-OPEN-EXCEPTION-COUNT-001 | [RPT-0005](ISSUES/RPT-0005.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED (DUPLICATE → RPT-0005) | Duplicate/merged |
| FIN-CAND-DASHBOARD-REOPENED-DAY-OMISSION-001 | [RPT-0005](ISSUES/RPT-0005.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED | Primary |
| FIN-CAND-DELIVERY-CUSTODY-001 | [FIN-0003](ISSUES/FIN-0003.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → FIN-0003) | Duplicate/merged |
| FIN-CAND-DELIVERY-CUSTODY-EXPECTED-001 | [FIN-0024](ISSUES/FIN-0024.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-CAND-EMPLOYEE-COMPENSATION-SIGN-001 | [PAYR-0010](ISSUES/PAYR-0010.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → PAYR-0010) | Duplicate/merged |
| FIN-CAND-EMPLOYEE-PAYROLL-OVERLAP-001 | [PAYR-0004](ISSUES/PAYR-0004.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · HIGH | NOT_FIXED_YET | Primary |
| FIN-CAND-INVENTORY-DASHBOARD-UNSIGNED-BALANCE-001 | [INV-0006](ISSUES/INV-0006.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | FIXED (DUPLICATE → INV-0006) | Duplicate/merged |
| FIN-CAND-NEGATIVE-VARIANT-PRICE-001 | [MENU-0001](ISSUES/MENU-0001.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-CAND-PACKAGING-COVERAGE-001 | [FIN-0046](ISSUES/FIN-0046.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_OWNER_BUSINESS_DECISION · MEDIUM | UNVERIFIED | Primary |
| FIN-CAND-PAYROLL001 | [PAYR-0003](ISSUES/PAYR-0003.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → PAYR-0003) | Duplicate/merged |
| FIN-CAND-PAYROLL-001 | [PAYR-0003](ISSUES/PAYR-0003.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · HIGH | NOT_FIXED_YET | Primary |
| FIN-CAND-PAYROLL-001-ROOT-CHALLENGE-2026 | [PAYR-0003](ISSUES/PAYR-0003.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → PAYR-0003) | Duplicate/merged |
| FIN-CAND-PAYROLL-READ-AUTHORIZATION-001 | [SEC-0002](ISSUES/SEC-0002.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | PARTIALLY_FIXED (DUPLICATE → SEC-0002) | Duplicate/merged |
| FIN-CAND-PREPGRAPH-001 | [INV-0002](ISSUES/INV-0002.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-CAND-PURCHASE-LINE-SIGNED-INPUT-001 | [INV-0009](ISSUES/INV-0009.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → INV-0009) | Duplicate/merged |
| FIN-CAND-RECIPE-RETARGET-STALE-POINTER-001 | [INV-0013](ISSUES/INV-0013.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED | Primary |
| FIN-CAND-REFUND-REPLAY-001 | [FIN-0025](ISSUES/FIN-0025.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · LOW | NOT_FIXED_YET | Primary |
| FIN-CAND-REPORT-RANGE-001 | [RPT-0001](ISSUES/RPT-0001.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | NOT_FIXED_YET | Primary |
| FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001 | [FIN-0023](ISSUES/FIN-0023.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · LOW | NOT_FIXED_YET | Primary |
| FIN-CAND-WPF-CASH-BOUNDARY-DIVERGENCE-001 | [UI-0011](ISSUES/UI-0011.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → UI-0011) | Duplicate/merged |
| FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001 | [UI-0024](ISSUES/UI-0024.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · LOW | NOT_FIXED_YET | Primary |
| FIN-CHALLENGE-CALLCENTER-CANCELLED-PAYMENT-LINE-001 | [UI-0023](ISSUES/UI-0023.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → UI-0023) | Duplicate/merged |
| FIN-COLLISION-001 | [FIN-0001](ISSUES/FIN-0001.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → FIN-0001) | Duplicate/merged |
| FIN-CONTINUATION-RECEIPT-MANIFEST-2026 | [DOC-0005](ISSUES/DOC-0005.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_A_DEFECT | Primary |
| FIN-CONTINUATION-RECEIPTS-QC-2026 | [DOC-0005](ISSUES/DOC-0005.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_A_DEFECT (DUPLICATE → DOC-0005) | Duplicate/merged |
| FIN-CONTINUATION-SCOPE-RECONCILIATION-2026 | [DOC-0005](ISSUES/DOC-0005.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_A_DEFECT (DUPLICATE → DOC-0005) | Duplicate/merged |
| FIN-DEAD-STATE-001 | [DB-0009](ISSUES/DB-0009.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET | Primary |
| FIN-DISCOUNT-OVERFLOW-001 | [FIN-0026](ISSUES/FIN-0026.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → FIN-0026) | Duplicate/merged |
| FIN-GATE-001 | [FIN-0013](ISSUES/FIN-0013.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → FIN-0013) | Duplicate/merged |
| FIN-LEAD-ATTENDANCE-UTC-001 | [PAYR-0009](ISSUES/PAYR-0009.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → PAYR-0009) | Duplicate/merged |
| FIN-LEAD-COMPENSATION-CASING-001 | [PAYR-0012](ISSUES/PAYR-0012.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → PAYR-0012) | Duplicate/merged |
| FIN-LEAD-CORRECTION-RATE-001 | [PAYR-0005](ISSUES/PAYR-0005.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → PAYR-0005) | Duplicate/merged |
| FIN-LEAD-REVERSED-RANGE-001 | [RPT-0001](ISSUES/RPT-0001.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → RPT-0001) | Duplicate/merged |
| FIN-LEAD-SNAPSHOT-REPORTS-ACCESS-001 | [SEC-0026](ISSUES/SEC-0026.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | FIXED (DUPLICATE → SEC-0026) | Duplicate/merged |
| FIN-LEAD-TAXMODE-001 | [FIN-0031](ISSUES/FIN-0031.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → FIN-0031) | Duplicate/merged |
| FIN-LOCK-ASYM-001 | [DB-0010](ISSUES/DB-0010.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED (DUPLICATE → DB-0010) | Duplicate/merged |
| FIN-LSP-001 | [ARCH-0006](ISSUES/ARCH-0006.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET | Primary |
| FIN-MULTI-ELEC-001 | [FIN-0004](ISSUES/FIN-0004.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CANDIDATE_FROM_SPECIALIST_HANDOFF · HIGH | NOT_FIXED_YET | Primary |
| FIN-PRINT-CLOSE-SLIP-RECOMPUTE-001 | [PRT-0017](ISSUES/PRT-0017.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET | Primary |
| FIN-PRINT-CLOSE-WARNING-SCOPE-001 | [PRT-0017](ISSUES/PRT-0017.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → PRT-0017) | Duplicate/merged |
| FIN-PRINT-LAYOUT-PRICE-001 | [PRT-0018](ISSUES/PRT-0018.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED | Primary |
| FIN-PRINT-RENDER-ENCODING-001 | [PRT-0018](ISSUES/PRT-0018.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED (DUPLICATE → PRT-0018) | Duplicate/merged |
| FIN-RECON-DRAWER-001 | [FIN-0050](ISSUES/FIN-0050.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_A_DEFECT | Primary |
| FIN-REPLAY-002 | [FIN-0001](ISSUES/FIN-0001.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → FIN-0001) | Duplicate/merged |
| FIN-SHIFT-002 | [FIN-0023](ISSUES/FIN-0023.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → FIN-0023) | Duplicate/merged |
| FIN-THREAD-MEM-001 | [TEST-0003](ISSUES/TEST-0003.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED | Primary |
| FIN-TST-001 | [TEST-0002](ISSUES/TEST-0002.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → TEST-0002) | Duplicate/merged |
| FIN-TST-002 | [TEST-0002](ISSUES/TEST-0002.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → TEST-0002) | Duplicate/merged |
| FIN-TST003 | [TEST-0002](ISSUES/TEST-0002.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → TEST-0002) | Duplicate/merged |
| FIN-TST-003 | [TEST-0002](ISSUES/TEST-0002.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → TEST-0002) | Duplicate/merged |
| FIN-TST-004 | [TEST-0002](ISSUES/TEST-0002.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → TEST-0002) | Duplicate/merged |
| FIN-TST-005 | [TEST-0002](ISSUES/TEST-0002.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → TEST-0002) | Duplicate/merged |
| FLOW-001 | [UI-0025](ISSUES/UI-0025.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH_REQUIREMENT_GAP | NOT_FIXED_YET | Primary |
| FLOW-002 | [PRT-0016](ISSUES/PRT-0016.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | NOT_FIXED_YET | Primary |
| HW1 | [HW-0002](ISSUES/HW-0002.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | PARTIALLY_FIXED | Primary |
| IND-API-01 | [PRT-0002](ISSUES/PRT-0002.md) | INDEPENDENT_AUDIT/06_NEW_DATABASE_AND_API_FINDINGS.md | New finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| IND-API-IO-01 | [API-0001](ISSUES/API-0001.md) | INDEPENDENT_AUDIT/06_NEW_DATABASE_AND_API_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → API-0001) | Duplicate/merged |
| IND-ARCH-01 | [ARCH-0003](ISSUES/ARCH-0003.md) | INDEPENDENT_AUDIT/09_NEW_ARCHITECTURE_AND_CLEAN_CODE_FINDINGS.md | New finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| IND-ARCH-IO-01 | [ARCH-0001](ISSUES/ARCH-0001.md) | INDEPENDENT_AUDIT/09_NEW_ARCHITECTURE_AND_CLEAN_CODE_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → ARCH-0001) | Duplicate/merged |
| IND-ARCH-IO-02 | [ARCH-0002](ISSUES/ARCH-0002.md) | INDEPENDENT_AUDIT/09_NEW_ARCHITECTURE_AND_CLEAN_CODE_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → ARCH-0002) | Duplicate/merged |
| IND-ARCH-IO-03 | [FIN-0031](ISSUES/FIN-0031.md) | INDEPENDENT_AUDIT/09_NEW_ARCHITECTURE_AND_CLEAN_CODE_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → FIN-0031) | Duplicate/merged |
| IND-ARCH-IO-04 | [ARCH-0002](ISSUES/ARCH-0002.md) | INDEPENDENT_AUDIT/09_NEW_ARCHITECTURE_AND_CLEAN_CODE_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → ARCH-0002) | Duplicate/merged |
| IND-ARCH-IO-05 | [PAYR-0015](ISSUES/PAYR-0015.md) | INDEPENDENT_AUDIT/09_NEW_ARCHITECTURE_AND_CLEAN_CODE_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → PAYR-0015) | Duplicate/merged |
| IND-DB-IO-01 | [FIN-0022](ISSUES/FIN-0022.md) | INDEPENDENT_AUDIT/06_NEW_DATABASE_AND_API_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → FIN-0022) | Duplicate/merged |
| IND-DB-IO-02 | [FIN-0021](ISSUES/FIN-0021.md) | INDEPENDENT_AUDIT/06_NEW_DATABASE_AND_API_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → FIN-0021) | Duplicate/merged |
| IND-DB-IO-03 | [PRT-0001](ISSUES/PRT-0001.md) | INDEPENDENT_AUDIT/06_NEW_DATABASE_AND_API_FINDINGS.md | Improvement opportunity | PARTIALLY_FIXED (DUPLICATE → PRT-0001) | Duplicate/merged |
| IND-DB-IO-04 | [PAYR-0003](ISSUES/PAYR-0003.md) | INDEPENDENT_AUDIT/06_NEW_DATABASE_AND_API_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → PAYR-0003) | Duplicate/merged |
| IND-DB-IO-05 | [PAYR-0008](ISSUES/PAYR-0008.md) | INDEPENDENT_AUDIT/06_NEW_DATABASE_AND_API_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → PAYR-0008) | Duplicate/merged |
| IND-FIN-01 | [PAYR-0008](ISSUES/PAYR-0008.md) | INDEPENDENT_AUDIT/05_NEW_LOGIC_AND_FINANCIAL_FINDINGS.md | New finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| IND-FIN-02 | [FIN-0015](ISSUES/FIN-0015.md) | INDEPENDENT_AUDIT/05_NEW_LOGIC_AND_FINANCIAL_FINDINGS.md | New finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → FIN-0015) | Duplicate/merged |
| IND-FIN-IO-01 | [FIN-0001](ISSUES/FIN-0001.md) | INDEPENDENT_AUDIT/05_NEW_LOGIC_AND_FINANCIAL_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → FIN-0001) | Duplicate/merged |
| IND-FIN-IO-02 | [ARCH-0003](ISSUES/ARCH-0003.md) | INDEPENDENT_AUDIT/05_NEW_LOGIC_AND_FINANCIAL_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → ARCH-0003) | Duplicate/merged |
| IND-FIN-IO-03 | [PAYR-0019](ISSUES/PAYR-0019.md) | INDEPENDENT_AUDIT/05_NEW_LOGIC_AND_FINANCIAL_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → PAYR-0019) | Duplicate/merged |
| IND-PERF-01 | [PAYR-0008](ISSUES/PAYR-0008.md) | INDEPENDENT_AUDIT/07_NEW_PERFORMANCE_FINDINGS.md | New finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → PAYR-0008) | Duplicate/merged |
| IND-REL-01 | [DOC-0004](ISSUES/DOC-0004.md) | INDEPENDENT_AUDIT/10_TESTING_AND_RELIABILITY_GAPS.md | New finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| IND-SEC-01 | [SEC-0014](ISSUES/SEC-0014.md) | INDEPENDENT_AUDIT/04_NEW_SECURITY_FINDINGS.md | New finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| IND-SEC-IO-01 | [SEC-0017](ISSUES/SEC-0017.md) | INDEPENDENT_AUDIT/04_NEW_SECURITY_FINDINGS.md | Improvement opportunity | FIXED (DUPLICATE → SEC-0017) | Duplicate/merged |
| IND-SEC-IO-02 | [SEC-0009](ISSUES/SEC-0009.md) | INDEPENDENT_AUDIT/04_NEW_SECURITY_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → SEC-0009) | Duplicate/merged |
| IND-TEST-01 | [TEST-0002](ISSUES/TEST-0002.md) | INDEPENDENT_AUDIT/10_TESTING_AND_RELIABILITY_GAPS.md | New finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| IND-UI-01 | [UI-0022](ISSUES/UI-0022.md) | INDEPENDENT_AUDIT/08_NEW_UI_UX_FINDINGS.md | New finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| IND-UI-IO-01 | [UI-0008](ISSUES/UI-0008.md) | INDEPENDENT_AUDIT/08_NEW_UI_UX_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → UI-0008) | Duplicate/merged |
| IND-UI-IO-02 | [UI-0009](ISSUES/UI-0009.md) | INDEPENDENT_AUDIT/08_NEW_UI_UX_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → UI-0009) | Duplicate/merged |
| IND-UI-IO-03 | [UI-0020](ISSUES/UI-0020.md) | INDEPENDENT_AUDIT/08_NEW_UI_UX_FINDINGS.md | Improvement opportunity | NOT_FIXED_YET (DUPLICATE → UI-0020) | Duplicate/merged |
| INV-COST-YIELD-ROUND-001 | [INV-0003](ISSUES/INV-0003.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → INV-0003) | Duplicate/merged |
| INV-DAT-001 | [INV-0012](ISSUES/INV-0012.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET | Primary |
| INV-FIN-001 | [INV-0004](ISSUES/INV-0004.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CANDIDATE_FROM_SPECIALIST_HANDOFF · LOW | NOT_FIXED_YET | Primary |
| INV-OPT-001 | [PERF-0005](ISSUES/PERF-0005.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → PERF-0005) | Duplicate/merged |
| INV-PAR-001 | [INV-0013](ISSUES/INV-0013.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED (DUPLICATE → INV-0013) | Duplicate/merged |
| INV-PERF-001 | [PERF-0005](ISSUES/PERF-0005.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CANDIDATE_FROM_SPECIALIST_HANDOFF · MEDIUM | NOT_FIXED_YET | Primary |
| INV-ROUND-001 | [INV-0003](ISSUES/INV-0003.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | NOT_FIXED_YET | Primary |
| INV-SYNC-001 | [INV-0005](ISSUES/INV-0005.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CANDIDATE_FROM_SPECIALIST_HANDOFF · MEDIUM | NOT_A_DEFECT | Primary |
| NEW-5XX-UNKNOWN (this audit) | [API-0003](ISSUES/API-0003.md) | All Issues (this consolidation) | New finding / registry alias | NOT_FIXED_YET | Primary |
| NEW-COUNT-SHEET (this audit) | [INV-0007](ISSUES/INV-0007.md) | All Issues (this consolidation) | New finding / registry alias | NOT_FIXED_YET | Primary |
| NEW-CUSTOMER-ERASURE (this audit) | [CUS-0002](ISSUES/CUS-0002.md) | All Issues (this consolidation) | New finding / registry alias | NOT_FIXED_YET | Primary |
| NEW-CUSTOMER-PII (this audit) | [CUS-0001](ISSUES/CUS-0001.md) | All Issues (this consolidation) | New finding / registry alias | NOT_FIXED_YET | Primary |
| NEW-FOODCOST-CURRENT (Codex lead, unregistered) | [INV-0011](ISSUES/INV-0011.md) | All Issues (this consolidation) | New finding / registry alias | NOT_FIXED_YET | Primary |
| NEW-INV-COST-READ (this audit) | [INV-0008](ISSUES/INV-0008.md) | All Issues (this consolidation) | New finding / registry alias | NOT_FIXED_YET | Primary |
| NEW-MENU-VARIANT-FK (this audit) | [MENU-0002](ISSUES/MENU-0002.md) | All Issues (this consolidation) | New finding / registry alias | NOT_FIXED_YET | Primary |
| NEW-PRICE-RACE (this audit) | [MENU-0003](ISSUES/MENU-0003.md) | All Issues (this consolidation) | New finding / registry alias | UNVERIFIED | Primary |
| NEW-REPORT-SNAPSHOT (this audit) | [RPT-0002](ISSUES/RPT-0002.md) | All Issues (this consolidation) | New finding / registry alias | UNVERIFIED | Primary |
| OPS1 | [OPS-0005](ISSUES/OPS-0005.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (docs/DAILY_OPERATIONS_VERIFICATION.md) | PARTIALLY_FIXED | Primary |
| OPS1-SCENARIOS | [OPS-0005](ISSUES/OPS-0005.md) | All Issues (this consolidation) | New finding / registry alias | PARTIALLY_FIXED (DUPLICATE → OPS-0005) | Duplicate/merged |
| OPS1-TRACE | [OPS-0004](ISSUES/OPS-0004.md) | All Issues (this consolidation) | New finding / registry alias | FIXED | Primary |
| P1 | [SEC-0024](ISSUES/SEC-0024.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | mechanism done (batch 10); file cleanup + rotation: decision | PARTIALLY_FIXED | Primary |
| P2 | [PRT-0006](ISSUES/PRT-0006.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 9) | FIXED (DUPLICATE → PRT-0006) | Duplicate/merged |
| P3 | [PRT-0001](ISSUES/PRT-0001.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 17) | PARTIALLY_FIXED (DUPLICATE → PRT-0001) | Duplicate/merged |
| PERF-001 | [PERF-0005](ISSUES/PERF-0005.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → PERF-0005) | Duplicate/merged |
| PRA-01 | [HW-0001](ISSUES/HW-0001.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| PRA-02 | [PRT-0003](ISSUES/PRT-0003.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| PRA-03 | [PRT-0010](ISSUES/PRT-0010.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| PRA-04 | [PRT-0011](ISSUES/PRT-0011.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| PRA-05 | [PRT-0012](ISSUES/PRT-0012.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| PRA-06 | [FIN-0025](ISSUES/FIN-0025.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → FIN-0025) | Duplicate/merged |
| PRA-07 | [FIN-0014](ISSUES/FIN-0014.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| PRA-08 | [API-0004](ISSUES/API-0004.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Finding (2026-11-04 audit) | NOT_FIXED_YET | Primary |
| PRA-09 | [DB-0004](ISSUES/DB-0004.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → DB-0004) | Duplicate/merged |
| PRA-10 | [PRT-0004](ISSUES/PRT-0004.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → PRT-0004) | Duplicate/merged |
| PRA-11 | [SEC-0008](ISSUES/SEC-0008.md) | PRODUCTION_RESILIENCE_AUDIT/01_PREVIOUS_AUDITS_REVIEW.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → SEC-0008) | Duplicate/merged |
| PRA-12 | [PRT-0004](ISSUES/PRT-0004.md) | PRODUCTION_RESILIENCE_AUDIT/01_PREVIOUS_AUDITS_REVIEW.md | Finding (2026-11-04 audit) | NOT_FIXED_YET (DUPLICATE → PRT-0004) | Duplicate/merged |
| PRT-001 | [PRT-0001](ISSUES/PRT-0001.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | PARTIALLY_FIXED | Primary |
| PRT-002 | [PRT-0004](ISSUES/PRT-0004.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | NOT_FIXED_YET | Primary |
| PRT-003 | [PRT-0005](ISSUES/PRT-0005.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | NOT_FIXED_YET | Primary |
| PRT-004 | [PRT-0006](ISSUES/PRT-0006.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | FIXED | Primary |
| PRT-005 | [PRT-0007](ISSUES/PRT-0007.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| PRT-006 | [PRT-0008](ISSUES/PRT-0008.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · LOW | NOT_FIXED_YET | Primary |
| PRT-007 | [PRT-0009](ISSUES/PRT-0009.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED | Primary |
| PRT-008 | [PRT-0008](ISSUES/PRT-0008.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → PRT-0008) | Duplicate/merged |
| PRT-REPRINT-001 | [SEC-0030](ISSUES/SEC-0030.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED (DUPLICATE → SEC-0030) | Duplicate/merged |
| PRT-REPRINT-VOUCHER-001 | [SEC-0030](ISSUES/SEC-0030.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED (DUPLICATE → SEC-0030) | Duplicate/merged |
| R1 | [SEC-0015](ISSUES/SEC-0015.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (628/628 integration, 2026-10-24) | FIXED | Primary |
| R2 | [SEC-0016](ISSUES/SEC-0016.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (same run) | FIXED | Primary |
| REC1 | [UI-0002](ISSUES/UI-0002.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 11) | FIXED | Primary |
| REC2 | [UI-0002](ISSUES/UI-0002.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 12) | FIXED (DUPLICATE → UI-0002) | Duplicate/merged |
| REC3 | [UI-0003](ISSUES/UI-0003.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | PARTIALLY_FIXED | Primary |
| REP1 | [RPT-0003](ISSUES/RPT-0003.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 13; API + Daily Statement, no UI yet) | PARTIALLY_FIXED (DUPLICATE → RPT-0003) | Duplicate/merged |
| REP2 | [FIN-0032](ISSUES/FIN-0032.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batches 4 + 19) | FIXED (DUPLICATE → FIN-0032) | Duplicate/merged |
| REP3 | [RPT-0003](ISSUES/RPT-0003.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 13; API + dashboard fields, no UI yet) | PARTIALLY_FIXED (DUPLICATE → RPT-0003) | Duplicate/merged |
| REP4 | [FIN-0006](ISSUES/FIN-0006.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 19; API + Daily Statement, no UI yet) | NOT_FIXED_YET (DUPLICATE → FIN-0006) | Duplicate/merged |
| REP5 | [FIN-0044](ISSUES/FIN-0044.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | deferred (needs business-day basis for wages / food cost) | NOT_FIXED_YET | Primary |
| REP6 | [RPT-0004](ISSUES/RPT-0004.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (docs/METRIC_DEFINITIONS.md) | FIXED | Primary |
| S1 | [FIN-0029](ISSUES/FIN-0029.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 6; assumes VAT-registered) | FIXED | Primary |
| S2 | [FIN-0030](ISSUES/FIN-0030.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | blocked: accountant to decide | NOT_FIXED_YET | Primary |
| S3 | [FIN-0041](ISSUES/FIN-0041.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 16) | FIXED | Primary |
| S4 | [FIN-0040](ISSUES/FIN-0040.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 4) | FIXED | Primary |
| SC-01 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Normal cash sale | No defect | No issue (safeguard holds) |
| SC-02 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Double-click Pay | No defect | No issue (safeguard holds) |
| SC-03 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Server crash before commit | No defect | No issue (safeguard holds) |
| SC-04 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Commit done, response lost | No defect | No issue (safeguard holds) |
| SC-05 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Crash after commit before auto-print | No defect | No issue (safeguard holds) |
| SC-06 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Auto-print DB error | No defect | No issue (safeguard holds) |
| SC-07 | [FIN-0010](ISSUES/FIN-0010.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Sale during shift close | NOT_FIXED_YET | Scenario |
| SC-08 | [FIN-0001](ISSUES/FIN-0001.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Split confirm | NOT_FIXED_YET | Scenario |
| SC-09 | [FIN-0004](ISSUES/FIN-0004.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: InstaPay + Wallet split | NOT_FIXED_YET | Scenario |
| SC-10 | [FIN-0015](ISSUES/FIN-0015.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Negative fee/discount/comp | NOT_FIXED_YET | Scenario |
| SC-11 | [FIN-0014](ISSUES/FIN-0014.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Pending checkout racing day close | NOT_FIXED_YET | Scenario |
| SC-12 | [API-0004](ISSUES/API-0004.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Client timeout under load | NOT_FIXED_YET | Scenario |
| SC-13 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Normal partial refund | No defect | No issue (safeguard holds) |
| SC-14 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Same-id partial refund race | No defect | No issue (safeguard holds) |
| SC-15 | [FIN-0025](ISSUES/FIN-0025.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Same-id full refund race | NOT_FIXED_YET | Scenario |
| SC-16 | [FIN-0007](ISSUES/FIN-0007.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Refund of uncollected order | NOT_FIXED_YET | Scenario |
| SC-17 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Refund and cancel concurrently | No defect | No issue (safeguard holds) |
| SC-18 | [FIN-0005](ISSUES/FIN-0005.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Full refund of split order | NOT_FIXED_YET | Scenario |
| SC-19 | [FIN-0002](ISSUES/FIN-0002.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Cancel pending leg of split | NOT_FIXED_YET | Scenario |
| SC-20 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two confirms at once | No defect | No issue (safeguard holds) |
| SC-21 | [FIN-0014](ISSUES/FIN-0014.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Confirm after shift closed | NOT_FIXED_YET | Scenario |
| SC-22 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Shift close kept request + power cut | No defect | No issue (safeguard holds) |
| SC-23 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two tills open a shift | No defect | No issue (safeguard holds) |
| SC-24 | [UI-0001](ISSUES/UI-0001.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Management Shifts retry | PARTIALLY_FIXED | Scenario |
| SC-25 | [FIN-0011](ISSUES/FIN-0011.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Shift-open float vs withdrawal | NOT_FIXED_YET | Scenario |
| SC-26 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Concurrent expense and safe drop | No defect | No issue (safeguard holds) |
| SC-27 | [PERF-0001](ISSUES/PERF-0001.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Treasury history growth | NOT_FIXED_YET | Scenario |
| SC-28 | [FIN-0012](ISSUES/FIN-0012.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two invoice payments | NOT_FIXED_YET | Scenario |
| SC-29 | [PAYR-0001](ISSUES/PAYR-0001.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Approved to Paid via transition | NOT_FIXED_YET | Scenario |
| SC-30 | [PAYR-0008](ISSUES/PAYR-0008.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Payroll entry at Cairo 00:30 on the 1st | NOT_FIXED_YET | Scenario |
| SC-31 | [PAYR-0014](ISSUES/PAYR-0014.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Overtime retry after approval spent | NOT_FIXED_YET | Scenario |
| SC-32 | [API-0004](ISSUES/API-0004.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Close with full history | NOT_FIXED_YET | Scenario |
| SC-33 | [FIN-0022](ISSUES/FIN-0022.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two day opens | NOT_FIXED_YET | Scenario |
| SC-34 | [FIN-0028](ISSUES/FIN-0028.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: DST start | NOT_FIXED_YET | Scenario |
| SC-35 | [FIN-0028](ISSUES/FIN-0028.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: DST end (hypothesis, reasoned consistent) | NOT_FIXED_YET | Scenario |
| SC-36 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Normal print | No defect | No issue (safeguard holds) |
| SC-37 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Agent crash mid-send | No defect | No issue (safeguard holds) |
| SC-38 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Report lost after printing | No defect | No issue (safeguard holds) |
| SC-39 | [PRT-0003](ISSUES/PRT-0003.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Backlog longer than lease | NOT_FIXED_YET | Scenario |
| SC-40 | [PRT-0002](ISSUES/PRT-0002.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Stale agent report | NOT_FIXED_YET | Scenario |
| SC-41 | [PRT-0004](ISSUES/PRT-0004.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Paper out network/serial | NOT_FIXED_YET | Scenario |
| SC-42 | [PRT-0004](ISSUES/PRT-0004.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Paper out spooler | NOT_FIXED_YET | Scenario |
| SC-43 | [PRT-0013](ISSUES/PRT-0013.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Printer offline 10 minutes | NOT_FIXED_YET | Scenario |
| SC-44 | [PRT-0013](ISSUES/PRT-0013.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Kitchen printer down, receipt OK | NOT_FIXED_YET | Scenario |
| SC-45 | [PRT-0010](ISSUES/PRT-0010.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Journal not writable | NOT_FIXED_YET | Scenario |
| SC-46 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Corrupt journal fails closed | No defect | No issue (safeguard holds) |
| SC-47 | [PRT-0012](ISSUES/PRT-0012.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Agent boots before server | NOT_FIXED_YET | Scenario |
| SC-48 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two agents same machine | No defect | No issue (safeguard holds) |
| SC-49 | [PRT-0001](ISSUES/PRT-0001.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Cashier and Call Center both print kitchen | PARTIALLY_FIXED | Scenario |
| SC-50 | [PRT-0014](ISSUES/PRT-0014.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Call Center customer reprint | NOT_FIXED_YET | Scenario |
| SC-51 | [HW-0001](ISSUES/HW-0001.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Drawer kick during backlog | NOT_FIXED_YET | Scenario |
| SC-52 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Drawer kick with printer off | No defect | No issue (safeguard holds) |
| SC-53 | [DB-0004](ISSUES/DB-0004.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Two backups same second then prune | NOT_FIXED_YET | Scenario |
| SC-54 | [OPS-0006](ISSUES/OPS-0006.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Disk full | NOT_FIXED_YET | Scenario |
| SC-55 | [DB-0005](ISSUES/DB-0005.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Restore while tills write | NOT_FIXED_YET | Scenario |
| SC-56 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Database in recovery at startup | No defect | No issue (safeguard holds) |
| SC-57 | [DB-0007](ISSUES/DB-0007.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Crash mid-migration | NOT_FIXED_YET | Scenario |
| SC-58 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Session expired overnight | No defect | No issue (safeguard holds) |
| SC-59 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: User deactivated mid-session | No defect | No issue (safeguard holds) |
| SC-60 | [UI-0026](ISSUES/UI-0026.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Permission revoked mid-workflow | NOT_FIXED_YET | Scenario |
| SC-61 | [SEC-0014](ISSUES/SEC-0014.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Role "Owner" created | NOT_FIXED_YET | Scenario |
| SC-62 | [SEC-0008](ISSUES/SEC-0008.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Till disk inspected | NOT_FIXED_YET | Scenario |
| SC-63 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Server unreachable | No defect | No issue (safeguard holds) |
| SC-64 | — | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Network back after long outage (manual retry by design) | No defect | No issue (safeguard holds) |
| SC-65 | [UI-0008](ISSUES/UI-0008.md) | PRODUCTION_RESILIENCE_AUDIT/12 or /06 (scenario/interleaving) | Scenario: Late lookup overwrites form | NOT_FIXED_YET | Scenario |
| SEC-001 | [SEC-0016](ISSUES/SEC-0016.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED_AND_CORRECTED · HIGH | FIXED (DUPLICATE → SEC-0016) | Duplicate/merged |
| SEC-002 | [SEC-0008](ISSUES/SEC-0008.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED_AND_CORRECTED · MEDIUM | NOT_FIXED_YET | Primary |
| SEC-003 | [SEC-0001](ISSUES/SEC-0001.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET | Primary |
| SEC-004 | [SEC-0015](ISSUES/SEC-0015.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | FIXED (DUPLICATE → SEC-0015) | Duplicate/merged |
| SEC-005 | [SEC-0002](ISSUES/SEC-0002.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | PARTIALLY_FIXED | Primary |
| SEC-006 | [SEC-0017](ISSUES/SEC-0017.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | FIXED | Primary |
| SEC-007 | [SEC-0018](ISSUES/SEC-0018.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CHALLENGED_COUNTEREVIDENCE_FOUND · MEDIUM | NOT_A_DEFECT | Primary |
| SEC-008 | [SEC-0009](ISSUES/SEC-0009.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · HIGH | NOT_FIXED_YET (DUPLICATE → SEC-0009) | Duplicate/merged |
| SEC-009 | [SEC-0010](ISSUES/SEC-0010.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CANDIDATE_FROM_SPECIALIST_HANDOFF · HIGH | NOT_FIXED_YET | Primary |
| SEC-009-012 | [SEC-0010](ISSUES/SEC-0010.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → SEC-0010) | Duplicate/merged |
| SEC-010 | [SEC-0011](ISSUES/SEC-0011.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CANDIDATE_FROM_SPECIALIST_HANDOFF · MEDIUM | PARTIALLY_FIXED | Primary |
| SEC-011 | [SEC-0012](ISSUES/SEC-0012.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CANDIDATE_FROM_SPECIALIST_HANDOFF · HIGH | NOT_FIXED_YET | Primary |
| SEC-012 | [SEC-0013](ISSUES/SEC-0013.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CANDIDATE_FROM_SPECIALIST_HANDOFF · LOW | NOT_A_DEFECT | Primary |
| SEC-013 | [SEC-0025](ISSUES/SEC-0025.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | FIXED | Primary |
| SEC1 | [SEC-0020](ISSUES/SEC-0020.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | VERIFIED (bounded + audited; full removal = decision SEC1-D) | FIXED | Primary |
| SEC1-D | [SEC-0020](ISSUES/SEC-0020.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | Owner decision | FIXED (DUPLICATE → SEC-0020) | Duplicate/merged |
| SEC2 | [SEC-0021](ISSUES/SEC-0021.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | VERIFIED (targeted 13/13 + regression 137/137; full run pending) | FIXED | Primary |
| SEC3 | [SEC-0017](ISSUES/SEC-0017.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | IMPLEMENTED (targeted green; full run pending; transaction binding = SEC3-D) | FIXED (DUPLICATE → SEC-0017) | Duplicate/merged |
| SEC3-D | [SEC-0017](ISSUES/SEC-0017.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | Owner decision | FIXED (DUPLICATE → SEC-0017) | Duplicate/merged |
| SEC4 | [SEC-0022](ISSUES/SEC-0022.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | IMPLEMENTED (full run pending; operator must change demo PINs) | PARTIALLY_FIXED | Primary |
| SEC5 | [SEC-0023](ISSUES/SEC-0023.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | IMPLEMENTED (full run pending; operator must create the agent account) | PARTIALLY_FIXED | Primary |
| SEC6 | [SEC-0002](ISSUES/SEC-0002.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | IMPLEMENTED (full run pending) | PARTIALLY_FIXED (DUPLICATE → SEC-0002) | Duplicate/merged |
| SEC7 | [SEC-0025](ISSUES/SEC-0025.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | FIXED (DUPLICATE → SEC-0025) | Duplicate/merged |
| SEC-APIN | [SEC-0019](ISSUES/SEC-0019.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | VERIFIED (baseline full run 694/695, the one failure environmental; re-run 34/34) | FIXED | Primary |
| SEC-CAND-TRANSPORT-001 | [SEC-0006](ISSUES/SEC-0006.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_FIXED_YET (DUPLICATE → SEC-0006) | Duplicate/merged |
| SEC-CRYPTO-001 | [SEC-0031](ISSUES/SEC-0031.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_A_DEFECT | Primary |
| SEC-PERIMETER-001 | [SEC-0029](ISSUES/SEC-0029.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_A_DEFECT (DUPLICATE → SEC-0029) | Duplicate/merged |
| SEC-REPRINT-AUTH-001 | [SEC-0030](ISSUES/SEC-0030.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED | Primary |
| T1 | [DB-0008](ISSUES/DB-0008.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | done (batch 7) | FIXED | Primary |
| TEST-CONTEXT-001 | [TEST-0003](ISSUES/TEST-0003.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED (DUPLICATE → TEST-0003) | Duplicate/merged |
| UH-01 | [OPS-0003](ISSUES/OPS-0003.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Unverified hypothesis | UNVERIFIED | Primary |
| UH-02 | [PRT-0003](ISSUES/PRT-0003.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Unverified hypothesis | NOT_FIXED_YET (DUPLICATE → PRT-0003) | Duplicate/merged |
| UH-03 | [PRT-0010](ISSUES/PRT-0010.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Unverified hypothesis | NOT_FIXED_YET (DUPLICATE → PRT-0010) | Duplicate/merged |
| UH-04 | [UI-0006](ISSUES/UI-0006.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Unverified hypothesis | UNVERIFIED (DUPLICATE → UI-0006) | Duplicate/merged |
| UH-05 | [DB-0010](ISSUES/DB-0010.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Unverified hypothesis | UNVERIFIED | Primary |
| UH-06 | [FIN-0028](ISSUES/FIN-0028.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Unverified hypothesis | NOT_FIXED_YET (DUPLICATE → FIN-0028) | Duplicate/merged |
| UH-07 | [OPS-0006](ISSUES/OPS-0006.md) | PRODUCTION_RESILIENCE_AUDIT/00_EXECUTIVE_SUMMARY.md | Unverified hypothesis | NOT_FIXED_YET | Primary |
| UH-08 | [PRT-0005](ISSUES/PRT-0005.md) | PRODUCTION_RESILIENCE_AUDIT/18_COVERAGE_GAPS_AND_UNVERIFIED_RISKS.md | Unverified hypothesis | NOT_FIXED_YET (DUPLICATE → PRT-0005) | Duplicate/merged |
| UI-001 | [UI-0001](ISSUES/UI-0001.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · HIGH | PARTIALLY_FIXED | Primary |
| UI-002 | [UI-0004](ISSUES/UI-0004.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · MEDIUM | NOT_FIXED_YET | Primary |
| UI-003 | [UI-0025](ISSUES/UI-0025.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · HIGH | NOT_FIXED_YET (DUPLICATE → UI-0025) | Duplicate/merged |
| UI-004 | [UI-0005](ISSUES/UI-0005.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | REQUIRES_RUNTIME_VERIFICATION · MEDIUM | UNVERIFIED | Primary |
| UI-005 | [UI-0006](ISSUES/UI-0006.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · HIGH | UNVERIFIED | Primary |
| UI-006 | [UI-0007](ISSUES/UI-0007.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · MEDIUM | NOT_FIXED_YET | Primary |
| UI-007 | [UI-0008](ISSUES/UI-0008.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · HIGH | NOT_FIXED_YET | Primary |
| UI-008 | [DOC-0005](ISSUES/DOC-0005.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | NOT_A_DEFECT (DUPLICATE → DOC-0005) | Duplicate/merged |
| UI-009 | [PRT-0015](ISSUES/PRT-0015.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · MEDIUM | NOT_FIXED_YET | Primary |
| UI-010 | [UI-0009](ISSUES/UI-0009.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · MEDIUM | NOT_FIXED_YET | Primary |
| UI-011 | [UI-0010](ISSUES/UI-0010.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · MEDIUM | NOT_FIXED_YET | Primary |
| UI-012 | [PRT-0014](ISSUES/PRT-0014.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · HIGH | NOT_FIXED_YET | Primary |
| UI-013 | [UI-0011](ISSUES/UI-0011.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · MEDIUM | NOT_FIXED_YET | Primary |
| UI-014 | [UI-0012](ISSUES/UI-0012.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · HIGH | NOT_FIXED_YET | Primary |
| UI-015 | [UI-0013](ISSUES/UI-0013.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · MEDIUM | NOT_FIXED_YET | Primary |
| UI-016 | [UI-0001](ISSUES/UI-0001.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · HIGH | PARTIALLY_FIXED (DUPLICATE → UI-0001) | Duplicate/merged |
| UI-017 | [UI-0014](ISSUES/UI-0014.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · HIGH | NOT_FIXED_YET | Primary |
| UI-018 | [UI-0015](ISSUES/UI-0015.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · HIGH | NOT_FIXED_YET | Primary |
| UI-019 | [UI-0016](ISSUES/UI-0016.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · MEDIUM | NOT_FIXED_YET | Primary |
| UI-020 | [SEC-0009](ISSUES/SEC-0009.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · HIGH | NOT_FIXED_YET | Primary |
| UI-021 | [UI-0017](ISSUES/UI-0017.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · HIGH | NOT_FIXED_YET | Primary |
| UI-022 | [UI-0018](ISSUES/UI-0018.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | CORROBORATED_WITH_QUALIFICATIONS · MEDIUM | NOT_FIXED_YET | Primary |
| UI-023 | [PERF-0006](ISSUES/PERF-0006.md) | Copilot/Codex register (Final-Handoff/ALL_VERIFIED_FINDINGS.json) | INDEPENDENTLY_CORROBORATED · MEDIUM | NOT_FIXED_YET | Primary |
| UI1 | [RPT-0003](ISSUES/RPT-0003.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | IN PROGRESS (screen built, client tests 5/5; not yet run on the desktop) | PARTIALLY_FIXED | Primary |
| UI2 | [RPT-0003](ISSUES/RPT-0003.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | IN PROGRESS (dashboard / daily summary labels fixed; not yet run on the desktop) | PARTIALLY_FIXED (DUPLICATE → RPT-0003) | Duplicate/merged |
| UI3 | [UI-0027](ISSUES/UI-0027.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | NOT_FIXED_YET | Primary |
| UI4 | [UI-0027](ISSUES/UI-0027.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | NOT_FIXED_YET (DUPLICATE → UI-0027) | Duplicate/merged |
| UI5 | [UI-0027](ISSUES/UI-0027.md) | docs/REMEDIATION_PLAN.md (backlog or Decisions table) | NOT STARTED | NOT_FIXED_YET (DUPLICATE → UI-0027) | Duplicate/merged |
| UI-CAND-OWNER-001 | [RPT-0005](ISSUES/RPT-0005.md) | Prior-tool working files (reports from another tool/, not in register) | Unregistered lead/candidate | UNVERIFIED (DUPLICATE → RPT-0005) | Duplicate/merged |
| NEW-PAYROLL-STATUS-CASING (B02 analysis, 2026-11-06) | [UI-0028](ISSUES/UI-0028.md) | Remediation loop B02 analysis | New finding (2026-11-06) | NOT_FIXED_YET | Primary |
| NEW-PICKUP-DST (B01 analysis, 2026-11-06) | [UI-0029](ISSUES/UI-0029.md) | Remediation loop B01 analysis | New finding (2026-11-06) | NOT_FIXED_YET | Primary |
| NEW-LEDGER-CREDIT-CONCURRENCY (B03 analysis, 2026-11-06) | [PAYR-0020](ISSUES/PAYR-0020.md) | Remediation loop B03 analysis | New finding (2026-11-06) | NOT_FIXED_YET | Primary |
| NEW-COD-NO-CANCEL (B05 analysis, 2026-11-06) | [FIN-0051](ISSUES/FIN-0051.md) | Remediation loop B05 analysis | New finding (2026-11-06) | NOT_FIXED_YET | Primary |
| NEW-COD-REFUND-UI (B05 analysis, 2026-11-06) | [UI-0030](ISSUES/UI-0030.md) | Remediation loop B05 analysis | New finding (2026-11-06) | NOT_FIXED_YET | Primary |
| NEW-DELIVERY-PENDING-CASING (B14 analysis, 2026-11-06) | [UI-0031](ISSUES/UI-0031.md) | Remediation loop B14 analysis | New finding (2026-11-06) | NOT_FIXED_YET | Primary |
| NEW-LOGIN-ERROR-COUNTS-AS-WRONG-PIN (B09 analysis, 2026-11-06) | [SEC-0032](ISSUES/SEC-0032.md) | Remediation loop B09 analysis | New finding (2026-11-06) | NOT_FIXED_YET | Primary |
| NEW-ORDER-DELIVERY-PII-READS (B07 analysis, 2026-11-06) | [SEC-0033](ISSUES/SEC-0033.md) | Remediation loop B07 analysis | New finding (2026-11-06) | NOT_FIXED_YET | Primary |
| NEW-DRIVER-ACCOUNT-READS (B07 analysis, 2026-11-06) | [SEC-0034](ISSUES/SEC-0034.md) | Remediation loop B07 analysis | New finding (2026-11-06) | NOT_FIXED_YET | Primary |
| NEW-DELIVERY-COLLECT-PAID (Claude Phase 1, 2026-11-07) | [UI-0032](ISSUES/UI-0032.md) | Claude Phase 1 verification | New finding (2026-11-07) | FIXED | Primary |
| NEW-PIN-COLLISION-ROLLBACK (Claude Phase 1, 2026-11-07) | [SEC-0035](ISSUES/SEC-0035.md) | Claude Phase 1 verification | New finding (2026-11-07) | FIXED | Primary |
| NEW-MAINTENANCE-NULL-ACCESS (Claude Phase 1, 2026-11-07) | [DB-0011](ISSUES/DB-0011.md) | Claude Phase 1 verification | New finding (2026-11-07) | FIXED | Primary |

Historical IDs mapped: **422** of **422**. Unmapped: **0**
