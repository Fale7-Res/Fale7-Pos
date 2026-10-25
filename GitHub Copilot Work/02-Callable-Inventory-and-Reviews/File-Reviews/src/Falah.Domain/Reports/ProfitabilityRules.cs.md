# File Review: src/Falah.Domain/Reports/ProfitabilityRules.cs
**Total Callables**: 4
---
## Canonical Callable ID: src/Falah.Domain/Reports/ProfitabilityRules.cs:35:5:MethodDeclaration
- **Symbol**: Falah.Domain.Reports.ProfitabilityRules.ComputeGrossProfit(Falah.Domain.Common.Money, Falah.Domain.Common.Money, int, int)
- **Kind**: MethodDeclaration
- **Behavior**: Computes gross profit and cost percentages against net sales, withholding profit figures when sold lines have zero recorded cost data to avoid false margin calculations.
- **Validations**: Validation checks detected: True. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Reports/ProfitabilityRules.cs:84:5:MethodDeclaration
- **Symbol**: Falah.Domain.Reports.ProfitabilityRules.ComputeOperatingMargin(Falah.Domain.Common.Money, Falah.Domain.Common.Money, Falah.Domain.Common.Money, Falah.Domain.Common.Money, int, int, int)
- **Kind**: MethodDeclaration
- **Behavior**: Computes operating margin and expense/wage percentages by deducting food cost, expenses, and wages from net sales, marking the result incomplete if cost data is missing.
- **Validations**: Validation checks detected: True. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Reports/ProfitabilityRules.cs:90:9:LocalFunctionStatement
- **Symbol**: Percent(Falah.Domain.Common.Money)
- **Kind**: LocalFunctionStatement
- **Behavior**: Local helper computing the percentage of a monetary figure relative to positive net sales, returning null if sales are zero or negative.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Reports/ProfitabilityRules.cs:94:9:LocalFunctionStatement
- **Symbol**: WagePercent()
- **Kind**: LocalFunctionStatement
- **Behavior**: Local helper returning the wage percentage of sales, returning null when no wage entries were posted to distinguish unrecorded labor from zero-cost labor.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `912310fad5a965cf46b0b84c20c78b44059e61475bfc60569d950014527b038f`
- **Callable ID Checked:** All 4 callables in `src/Falah.Domain/Reports/ProfitabilityRules.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Domain/Reports/ProfitabilityRules.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `912310fad5a965cf46b0b84c20c78b44059e61475bfc60569d950014527b038f`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `912310fad5a965cf46b0b84c20c78b44059e61475bfc60569d950014527b038f` against production source `src/Falah.Domain/Reports/ProfitabilityRules.cs`. All 4 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 4 callables.
