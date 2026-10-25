# File Review: src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs
**Total Callables**: 7
---
## Canonical Callable ID: src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs:24:5:ConstructorDeclaration
- **Symbol**: Falah.Infrastructure.Employees.CorrectAttendanceHandler.CorrectAttendanceHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry)
- **Kind**: ConstructorDeclaration
- **Behavior**: Initializes CorrectAttendanceHandler, injecting FalahDbContext and IApprovalRegistry into private readonly fields _db and _approvalRegistry.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs:30:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Employees.CorrectAttendanceHandler.HandleAsync(Falah.Application.Employees.CorrectAttendanceRequest, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Corrects an existing attendance record and adjusts employee payroll ledger entries within a ReadCommitted database transaction. Validates reason presence and AttendanceStatus enum parsing. Acquires pessimistic locks (UPDLOCK, ROWLOCK, HOLDLOCK) on the attendance record; checks for previous audit replay via auditId; verifies actor user and manager approval under 'attendance_manage' permission. Updates attendance record fields (Status, LateMinutes, WorkHours, OvertimeHours, TatbiqCount, IsManualCorrection, CorrectedBy, CorrectionReason). If the employee has a daily wage, locks the employee's ledger rows, recalculates late penalties and overtime using PayrollRules, and posts delta ledger adjustments (crediting/debiting via EmployeeLedgerPoster) without altering past records. Finally records an audit event and commits the transaction.
- **Validations**: Validation checks detected: True. Throws: DomainException, DomainException, InvalidOperationException, DomainException, InvalidOperationException, InvalidOperationException, DomainException.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': True, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': True, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs:45:81:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Predicate lambda matching an AuditEventEntity by its deterministic audit identifier for idempotent correction replays.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs:55:76:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Predicate lambda querying UserEntity matching request.ActorUserId.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs:60:83:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Predicate lambda querying UserEntity matching request.ManagerApproverUserId.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs:68:79:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Predicate lambda querying the EmployeeEntity matching record.EmployeeId.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs:88:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(e => e.Id).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:37:50Z
- **Source SHA-256 Verified:** `e00e9fa8c36f3fa6a75e371c85307e1666803ef7cc57972513b868772a4225ba`
- **Callable ID Checked:** All 7 callables in `src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:38:17Z
- **Source SHA-256 Verified:** `e00e9fa8c36f3fa6a75e371c85307e1666803ef7cc57972513b868772a4225ba`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `e00e9fa8c36f3fa6a75e371c85307e1666803ef7cc57972513b868772a4225ba` against production source `src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs`. All 7 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 7 callables.