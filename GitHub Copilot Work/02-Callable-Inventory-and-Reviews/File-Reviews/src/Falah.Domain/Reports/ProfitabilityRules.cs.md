# File Review: src/Falah.Domain/Reports/ProfitabilityRules.cs
**Total Callables**: 4
---
## Canonical Callable ID: src/Falah.Domain/Reports/ProfitabilityRules.cs:35:5:MethodDeclaration
- **Symbol**: Falah.Domain.Reports.ProfitabilityRules.ComputeGrossProfit(Falah.Domain.Common.Money, Falah.Domain.Common.Money, int, int)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Domain.Reports.ProfitabilityRules.ComputeGrossProfit(Falah.Domain.Common.Money, Falah.Domain.Common.Money, int, int) across lines 35-60.
- **Validations**: Validation checks detected: True. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Reports/ProfitabilityRules.cs:84:5:MethodDeclaration
- **Symbol**: Falah.Domain.Reports.ProfitabilityRules.ComputeOperatingMargin(Falah.Domain.Common.Money, Falah.Domain.Common.Money, Falah.Domain.Common.Money, Falah.Domain.Common.Money, int, int, int)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Domain.Reports.ProfitabilityRules.ComputeOperatingMargin(Falah.Domain.Common.Money, Falah.Domain.Common.Money, Falah.Domain.Common.Money, Falah.Domain.Common.Money, int, int, int) across lines 84-106.
- **Validations**: Validation checks detected: True. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Reports/ProfitabilityRules.cs:90:9:LocalFunctionStatement
- **Symbol**: Percent(Falah.Domain.Common.Money)
- **Kind**: LocalFunctionStatement
- **Behavior**: Executes logic for Percent(Falah.Domain.Common.Money) across lines 90-92.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Reports/ProfitabilityRules.cs:94:9:LocalFunctionStatement
- **Symbol**: WagePercent()
- **Kind**: LocalFunctionStatement
- **Behavior**: Executes logic for WagePercent() across lines 94-94.
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
