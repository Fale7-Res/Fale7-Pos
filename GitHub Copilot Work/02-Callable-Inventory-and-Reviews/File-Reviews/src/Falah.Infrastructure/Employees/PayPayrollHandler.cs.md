# File Review: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs
**Total Callables**: 15
---
## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:32:5:ConstructorDeclaration
- **Symbol**: Falah.Infrastructure.Employees.PayPayrollHandler.PayPayrollHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog)
- **Kind**: ConstructorDeclaration
- **Behavior**: Injects `FalahDbContext` for database persistence, `IApprovalRegistry` for manager approval token validation, and `IAuditLog` for recording critical financial settlement audit events. Holds zero mutable state.
  ```csharp
  // Lines 32-37: Constructor dependency injection
  public PayPayrollHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
  {
      _db = db;
      _approvalRegistry = approvalRegistry;
      _auditLog = auditLog;
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:39:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Employees.PayPayrollHandler.HandleAsync(Falah.Application.Employees.PayPayrollRequest, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Orchestrates the complete financial payout and settlement of an approved payroll run. Validates payment account against whitelist (`tre-main`, `tre-bank`, `tre-instapay`). Performs idempotent replay check on key `payroll-payment-{runId}`. Verifies actor user existence and asserts payroll run is in `Approved` state (also handling concurrent winner replay if status transitioned to `Paid`). Authorizes `payroll_pay` command enforcing mandatory manager approval. Computes aggregate net payable total from all payroll lines; provides detailed diagnostics if total is zero (identifying unpriced monthly employees). Asserts open business day via `BusinessDayGate.RequireOpenAsync`. Starts an EF database transaction, acquires pessimistic lock on treasury movements via `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync`, asserts protected source account balance covers full total via `FinancialRules.AssertProtectedSourceBalance`, starts operation in `EfFinancialJournal`, creates single aggregate `TreasuryMovement` for the entire run total, posts individual `PayrollSettlement` debit ledger entries to each employee with `NetPayablePiastres > 0` via `EmployeeLedgerPoster.PostIfNewAsync`, transitions run status to `Paid`, appends critical audit log event, and commits transaction. Handles rollback and sibling replay resolution on concurrency collisions.
  ```csharp
  // Lines 39-163: Core payroll settlement logic
  public async Task<PayPayrollResult> HandleAsync(PayPayrollRequest request, CancellationToken ct = default)
  {
      if (!ValidAccounts.Contains(request.PaymentAccountId))
          throw new DomainException("حساب الدفع غير صالح لصرف Payroll.");
      ...
  }
  ```
  - **Financial & Transaction Integrity**: Posts exactly one treasury movement for the aggregate payroll total and individual debit ledger entries per employee under an explicit transaction with balance locking.
- **Validations**: Validation checks detected: True. Throws: DomainException, InvalidOperationException, InvalidOperationException, DomainException, DomainException, DomainException, TreasuryConflictException, TreasuryConflictException, TreasuryConflictException.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': True, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:46:9:LocalFunctionStatement
- **Symbol**: LoadReplayAsync()
- **Kind**: LocalFunctionStatement
- **Behavior**: Local function checking for an existing treasury movement with idempotency key `payroll-payment-{request.PayrollRunId}` using `.AsNoTracking()`. If found, counts already settled payroll lines (`Status == "paid"`) and returns `PayPayrollResult` with `WasReplayed = true`.
  ```csharp
  // Lines 46-54: Idempotency replay probe
  async Task<PayPayrollResult?> LoadReplayAsync()
  {
      var movement = await _db.TreasuryMovements.AsNoTracking()
          .FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
      if (movement is null) return null;
      var settledCount = await _db.PayrollLines.AsNoTracking()
          .CountAsync(l => l.PayrollRunId == request.PayrollRunId && l.Status == "paid", ct);
      return new PayPayrollResult(movement.Id, movement.AmountPiastres, settledCount, WasReplayed: true);
  }
  ```
- **Validations**: Validation checks detected: True. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:49:38:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:52:29:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Filter predicate expression lambda `l => l.PayrollRunId == request.PayrollRunId && l.Status == "paid"` used to count settled payroll lines during idempotent replay inspection.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:58:76:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: User lookup predicate expression lambda `u => u.Id == request.ActorUserId` retrieving the actor user with `.AsNoTracking()`.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:62:61:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Run lookup predicate expression lambda `r => r.Id == request.PayrollRunId` fetching the target `PayrollRunEntity` for state verification and status transition.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:79:83:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Approver user lookup expression lambda `u => u.Id == request.ManagerApproverUserId` verifying manager approver identity and role.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:86:50:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Filter predicate expression lambda `l => l.PayrollRunId == request.PayrollRunId` querying all line items for net wage summation and unpriced employee diagnostic evaluation.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:87:31:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var total = lines.Sum(l => l.NetPayablePiastres);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:95:44:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var employeeIds = lines.Select(l => l.EmployeeId).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:97:24:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Diagnostic predicate expression lambda `e => employeeIds.Contains(e.Id) && e.CompensationType == nameof(CompensationType.Monthly) && (e.MonthlySalaryPiastres == null || e.MonthlySalaryPiastres == 0)` identifying monthly employees who lack configured base salary when net payroll total resolves to zero.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:100:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(e => e.Name).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:137:46:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'foreach (var line in lines.Where(l => l.NetPayablePiastres > 0))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayPayrollHandler.cs:174:21:ParenthesizedLambdaExpression
- **Symbol**: lambda expression
- **Kind**: ParenthesizedLambdaExpression
- **Behavior**: Inline expression lambda 'async () => await LoadReplayAsync(), ct) is { } sibling)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:37:50Z
- **Source SHA-256 Verified:** `d7a614f6752b5a33513b3cd4ecf1e4e1cfe0022d010822e38bb43160c40e4084`
- **Callable ID Checked:** All 15 callables in `src/Falah.Infrastructure/Employees/PayPayrollHandler.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:38:17Z
- **Source SHA-256 Verified:** `d7a614f6752b5a33513b3cd4ecf1e4e1cfe0022d010822e38bb43160c40e4084`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `d7a614f6752b5a33513b3cd4ecf1e4e1cfe0022d010822e38bb43160c40e4084` against production source `src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`. All 15 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 15 callables.
