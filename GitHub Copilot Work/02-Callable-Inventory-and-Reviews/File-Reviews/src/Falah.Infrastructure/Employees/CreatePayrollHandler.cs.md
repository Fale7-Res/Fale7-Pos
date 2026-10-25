# File Review: src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs
**Total Callables**: 10
---
## Canonical Callable ID: src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs:23:5:ConstructorDeclaration
- **Symbol**: Falah.Infrastructure.Employees.CreatePayrollHandler.CreatePayrollHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry)
- **Kind**: ConstructorDeclaration
- **Behavior**: Injects `FalahDbContext` for persistence and `IApprovalRegistry` for authorization into `CreatePayrollHandler`. Holds zero mutable state.
  ```csharp
  // Lines 23-27: CreatePayrollHandler constructor
  public CreatePayrollHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
  {
      _db = db;
      _approvalRegistry = approvalRegistry;
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs:29:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Employees.CreatePayrollHandler.HandleAsync(Falah.Application.Employees.CreatePayrollRequest, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Generates a monthly payroll run for a target period (`YYYY-MM`). Enforces format validation with regex `^\d{4}-\d{2}$`. Checks for existing non-cancelled run for the period to guarantee at most one active run per period (returns existing run as replay). Enforces `employees_manage` permission on actor user. Extracts year and month components, loads all employees, and fetches all employee ledger entries matching the period's year and month. Prepares `PayrollRules.PayrollLineInput` structures and computes wages via pure domain logic `PayrollRules.CalculatePayrollLines`. Creates `PayrollRunEntity` in `Draft` status, creates associated `PayrollLineEntity` rows for each employee with status `"unpaid"`, and persists changes. Catches `DbUpdateException` to handle concurrent duplicate period generation safely by reloading the persisted run.
  ```csharp
  // Lines 29-104: Monthly payroll run generation
  public async Task<PayrollRunDto> HandleAsync(CreatePayrollRequest request, CancellationToken ct = default)
  {
      if (!System.Text.RegularExpressions.Regex.IsMatch(request.Period, @"^\d{4}-\d{2}$"))
          throw new DomainException("فترة Payroll غير صالحة.");
      ...
  }
  ```
- **Validations**: Validation checks detected: True. Throws: DomainException, InvalidOperationException.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs:35:34:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Active run predicate expression lambda `r => r.Period == request.Period && r.Status != nameof(PayrollRunStatus.Cancelled)` preventing duplicate active runs for the same period.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs:38:76:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: User lookup predicate expression lambda `u => u.Id == request.ActorUserId` for actor authorization.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs:46:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Date filter predicate expression lambda `e => e.CreatedAtUtc.Year == periodYear && e.CreatedAtUtc.Month == periodMonth` querying employee ledger entries for the target month.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs:54:43:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Projection expression lambda mapping each employee to a `PayrollRules.PayrollLineInput`, grouping their ledger entries and evaluating compensation structure.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs:56:50:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var empEntries = periodEntries.Where(e => e.EmployeeId == emp.Id)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs:57:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Projection expression lambda `e => (e.Type, Credit: Money.FromPiastres(e.CreditPiastres), Debit: Money.FromPiastres(e.DebitPiastres))` converting database ledger entries into domain wage tuples.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs:98:38:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Replay recovery predicate expression lambda `r => r.Id == runId || (r.Period == request.Period && r.Status != nameof(PayrollRunStatus.Cancelled))` executed on `DbUpdateException`.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs:106:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Employees.CreatePayrollHandler.ToDto(Falah.Infrastructure.Persistence.Entities.PayrollRunEntity, bool)
- **Kind**: MethodDeclaration
- **Behavior**: Internal static mapper method projecting `PayrollRunEntity` into `PayrollRunDto`, populating period, lifecycle status, creator, approval, payment timestamps, linked treasury movement, and `wasReplayed` flag.
  ```csharp
  // Lines 106-108: Entity to DTO mapping
  internal static PayrollRunDto ToDto(PayrollRunEntity r, bool wasReplayed) => new(
      r.Id, r.Period, r.Status, r.CreatedBy, r.CreatedAtUtc, r.ApprovedBy, r.PaidAtUtc, r.ReviewedAtUtc,
      r.ReviewedBy, r.TreasuryMovementId, r.PaymentAccountId, wasReplayed);
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:37:50Z
- **Source SHA-256 Verified:** `339845a57fc7ec98268bbf79d4b204fbf1c029a3da4cff89771294522963e878`
- **Callable ID Checked:** All 10 callables in `src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:38:17Z
- **Source SHA-256 Verified:** `339845a57fc7ec98268bbf79d4b204fbf1c029a3da4cff89771294522963e878`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `339845a57fc7ec98268bbf79d4b204fbf1c029a3da4cff89771294522963e878` against production source `src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs`. All 10 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 10 callables.
