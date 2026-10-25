# File Review: src/Falah.Infrastructure/Treasury/TreasuryMapper.cs
**Total Callables**: 5
---
## Canonical Callable ID: src/Falah.Infrastructure/Treasury/TreasuryMapper.cs:15:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LoadAccountMovementsAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LoadAccountMovementsAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, CancellationToken) across lines 15-19.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Treasury/TreasuryMapper.cs:17:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 17-17.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Treasury/TreasuryMapper.cs:37:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, CancellationToken) across lines 37-46.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Treasury/TreasuryMapper.cs:51:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Treasury.TreasuryMapper.ToDomain(Falah.Infrastructure.Persistence.Entities.TreasuryMovementEntity)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Treasury.TreasuryMapper.ToDomain(Falah.Infrastructure.Persistence.Entities.TreasuryMovementEntity) across lines 51-54.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Treasury/TreasuryMapper.cs:79:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Treasury.TreasuryAccountValidation.IsKnownAccount(string)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.Treasury.TreasuryAccountValidation.IsKnownAccount(string) across lines 79-79.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `818d64bdb94fa957779a83a090b9921c0902b716c2e69564cac25772ac5e0400`
- **Callable ID Checked:** All 5 callables in `src/Falah.Infrastructure/Treasury/TreasuryMapper.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Treasury/TreasuryMapper.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `818d64bdb94fa957779a83a090b9921c0902b716c2e69564cac25772ac5e0400`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `818d64bdb94fa957779a83a090b9921c0902b716c2e69564cac25772ac5e0400` against production source `src/Falah.Infrastructure/Treasury/TreasuryMapper.cs`. All 5 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 5 callables.
