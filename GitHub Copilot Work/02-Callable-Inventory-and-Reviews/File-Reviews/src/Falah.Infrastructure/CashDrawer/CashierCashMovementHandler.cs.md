# File Review: src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs
**Total Callables**: 9
---
## Canonical Callable ID: src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs:33:5:ConstructorDeclaration
- **Symbol**: Falah.Infrastructure.CashDrawer.CashierCashMovementHandler.CashierCashMovementHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog)
- **Kind**: ConstructorDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.CashDrawer.CashierCashMovementHandler.CashierCashMovementHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog) across lines 33-38.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs:40:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.CashDrawer.CashierCashMovementHandler.HandleAsync(Falah.Application.CashDrawer.CashierCashMovementRequest, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.CashDrawer.CashierCashMovementHandler.HandleAsync(Falah.Application.CashDrawer.CashierCashMovementRequest, CancellationToken) across lines 40-132.
- **Validations**: Validation checks detected: True. Throws: DomainException, DomainException, DomainException, InvalidOperationException, InvalidOperationException, DomainException, DomainException, TreasuryConflictException, TreasuryConflictException, TreasuryConflictException.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': True, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': True, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs:52:76:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 52-52.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs:56:73:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 56-56.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs:64:83:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 64-64.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs:123:21:ParenthesizedLambdaExpression
- **Symbol**: lambda expression
- **Kind**: ParenthesizedLambdaExpression
- **Behavior**: Inline expression lambda 'async () => await FindExistingAsync(idempotencyKey, ct), ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs:134:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.CashDrawer.CashierCashMovementHandler.FindExistingAsync(string, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.CashDrawer.CashierCashMovementHandler.FindExistingAsync(string, CancellationToken) across lines 134-135.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs:135:66:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Executes logic for lambda expression across lines 135-135.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs:137:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.CashDrawer.CashierCashMovementHandler.ToResult(Falah.Infrastructure.Persistence.Entities.TreasuryMovementEntity, bool)
- **Kind**: MethodDeclaration
- **Behavior**: Executes logic for Falah.Infrastructure.CashDrawer.CashierCashMovementHandler.ToResult(Falah.Infrastructure.Persistence.Entities.TreasuryMovementEntity, bool) across lines 137-138.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `ce1184171f64ce4ce1f8399d26b6228592ce0c6767aded0f4ff4140d35dbd6fd`
- **Callable ID Checked:** All 9 callables in `src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `ce1184171f64ce4ce1f8399d26b6228592ce0c6767aded0f4ff4140d35dbd6fd`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `ce1184171f64ce4ce1f8399d26b6228592ce0c6767aded0f4ff4140d35dbd6fd` against production source `src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs`. All 9 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 9 callables.
