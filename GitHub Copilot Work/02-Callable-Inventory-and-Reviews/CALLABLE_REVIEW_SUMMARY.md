# Callable-by-Callable Inventory & Review Ledger

- **Total Callables Audited:** **11,653**
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Timestamp:** 2026-10-24T18:15:00Z
- **Verification Standard:** Source-grounded callable review. Revoked premature heuristic promotion in accordance with Phase 1 Integrity Recovery.

---

## 1. Quality Classification Breakdown

| Quality Classification Status | Count | Percentage | Description & Forensic Evidence |
| :--- | :--- | :--- | :--- |
| **GENERATED_OR_TRIVIAL_JUSTIFIED** | **2,801** | 24.0% | Auto-property accessors, simple DTOs, and pure LINQ projections with explicit safety justifications |
| **PRIOR_REASONING_UNVERIFIED** | **7,748** | 66.5% | Lower-risk and test callables downgraded from premature automated regex promotion; subject to verification queue |
| **INDEXED** | **1,004** | 8.6% | Roslyn AST-indexed callables mapped into the repository denominator |
| **SUBSTANTIVE_SOURCE_REVIEWED** | **95** | 0.8% | Deeply analyzed core financial handlers, security middleware, and printing dispatchers |
| **INDEPENDENTLY_VERIFIED** | **5** | 0.1% | Core critical paths independently verified against baseline git tree (SEC-001, FIN-001, FIN-002, FIN-003, PRT-001) |
| **REVIEW_BLOCKED** | **0** | 0.0% | Zero inaccessible or blocked callables |
| **TOTAL ACCOUNTED FOR** | **11,653** | **100.0%** | **100% of repository callables tracked and accounted for** |

---

## 2. Project Distribution (Corrected Denominators)

| Project | Total Callables | Role & Responsibility in Fale7 POS Architecture |
| :--- | :--- | :--- |
| **src/Falah.Infrastructure** | **6,168** | SQL Server persistence, EF Core DbContext, Dapper query services, command handlers, printing dispatch |
| **src/Falah.Client.CashierWinUI** | **1,701** | Active primary POS & Management desktop client (WinUI 3 / Windows App SDK) |
| **tests/Falah.Server.IntegrationTests**| **1,592** | Integration tests, database rollback tests, and vertical slice tests |
| **tests/Falah.Domain.Tests** | **557** | Domain unit tests covering Money, Payroll, Inventory, and Order invariants |
| **src/Falah.Domain** | **487** | Pure business entities, Money value object, authorization rules, and domain invariants |
| **src/Falah.Client.CallCenterWinUI** | **341** | Dedicated phone/delivery order intake client |
| **tests/Falah.Client.Tests** | **290** | Client viewmodel and state unit tests |
| **src/Falah.Server** | **247** | Minimal API endpoints, AuthenticationMiddleware, and SignalR hub handlers |
| **src/Falah.Client.Cashier** | **129** | Legacy WPF cashier desktop client |
| **src/Falah.PrintAgent** | **108** | Physical ESC/POS printer polling worker, Windows Spooler transport, and bitmap rasterizer |
| **src/Falah.Application** | **24** | Request and response DTO records |
| **tools/ReceiptPreview** | **9** | Standalone thermal ticket preview tool |
| **TOTAL** | **11,653** | **Exact repository-wide denominator matching Roslyn AST inventory** |

---

## 3. Callable Kinds

| Callable Kind | Count | Review Strategy |
| :--- | :--- | :--- |
| **SimpleLambdaExpression** | **5,595** | LINQ projections and query selectors |
| **MethodDeclaration** | **3,276** | Full substantive review across business, authorization, and financial dimensions |
| **GetAccessorDeclaration** | **1,015** | Auto-properties justified; computed accessors reviewed |
| **SetAccessorDeclaration** | **748** | Auto-properties justified; notifying/mutating accessors reviewed |
| **ParenthesizedLambdaExpression** | **598** | Event handlers, async callbacks, and complex predicates |
| **ConstructorDeclaration** | **270** | Dependency injection boundaries, invariant assertions |
| **InitAccessorDeclaration** | **95** | Immutable record property initializers |
| **LocalFunctionStatement** | **48** | Scoped helper functions inside business methods |
| **OperatorDeclaration** | **8** | Money addition, subtraction, negation, and equality operators |
| **TOTAL** | **11,653** | |

---

## 4. Multi-Agent Specialist Verification Integration

In accordance with Phase 3 & 4 of the Owner Directive, high-risk callable clusters have been reviewed by dedicated specialist subagents:
- **Financial & Database Integrity:** Reviewed in [`FINANCIAL_AUDITOR_HANDOFF.md`](../Checkpoints/AGENT_HANDOFFS/FINANCIAL_AUDITOR_HANDOFF.md)
- **Security & Authorization:** Reviewed in [`SECURITY_AUDITOR_HANDOFF.md`](../Checkpoints/AGENT_HANDOFFS/SECURITY_AUDITOR_HANDOFF.md)
- **Printing & Operational Workflows:** Reviewed in [`PRINTING_WORKFLOW_AUDITOR_HANDOFF.md`](../Checkpoints/AGENT_HANDOFFS/PRINTING_WORKFLOW_AUDITOR_HANDOFF.md)
- **Mirrored Code & Parity:** Reviewed in [`MIRRORED_CODE_AUDITOR_HANDOFF.md`](../Checkpoints/AGENT_HANDOFFS/MIRRORED_CODE_AUDITOR_HANDOFF.md)
- **UI/UX & Maintainability:** Reviewed in [`UI_MAINTAINABILITY_AUDITOR_HANDOFF.md`](../Checkpoints/AGENT_HANDOFFS/UI_MAINTAINABILITY_AUDITOR_HANDOFF.md)

Machine-readable ledger: [`CALLABLE_REVIEW_REGISTRY.json`](CALLABLE_REVIEW_REGISTRY.json)
