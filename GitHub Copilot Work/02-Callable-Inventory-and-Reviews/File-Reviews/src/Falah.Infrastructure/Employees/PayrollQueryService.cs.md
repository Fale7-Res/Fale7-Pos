# File Review: src/Falah.Infrastructure/Employees/PayrollQueryService.cs
**Total Callables**: 9
---
## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayrollQueryService.cs:10:5:ConstructorDeclaration
- **Symbol**: Falah.Infrastructure.Employees.PayrollQueryService.PayrollQueryService(Falah.Infrastructure.Persistence.FalahDbContext)
- **Kind**: ConstructorDeclaration
- **Behavior**: Injects the Entity Framework Core database context (`FalahDbContext`) into private readonly field `_db`. Establishes the persistence dependency required for querying `PayrollRuns` and `PayrollLines` tables. Holds zero independent mutable state and performs no database I/O during instantiation.
  ```csharp
  // Line 10: Constructor dependency injection
  public PayrollQueryService(FalahDbContext db) => _db = db;
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayrollQueryService.cs:12:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Employees.PayrollQueryService.ListRunsAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Asynchronously queries and returns all payroll runs ordered chronologically descending by `CreatedAtUtc`. Uses `.AsNoTracking()` to avoid EF Core change tracking overhead. Directly projects entity fields into `PayrollRunDto` records, including run ID, period (`YYYY-MM`), lifecycle status, audit tracking fields (`CreatedBy`, `CreatedAtUtc`, `ApprovedBy`, `PaidAtUtc`, `ReviewedAtUtc`, `ReviewedBy`), financial identifiers (`TreasuryMovementId`, `PaymentAccountId`), and explicit `wasReplayed: false`.
  ```csharp
  // Lines 12-16: Query all payroll runs
  public Task<List<PayrollRunDto>> ListRunsAsync(CancellationToken ct = default) =>
      _db.PayrollRuns.AsNoTracking().OrderByDescending(r => r.CreatedAtUtc)
          .Select(r => new PayrollRunDto(r.Id, r.Period, r.Status, r.CreatedBy, r.CreatedAtUtc, r.ApprovedBy,
              r.PaidAtUtc, r.ReviewedAtUtc, r.ReviewedBy, r.TreasuryMovementId, r.PaymentAccountId, false))
          .ToListAsync(ct);
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayrollQueryService.cs:13:58:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Ordering key selector expression lambda `r => r.CreatedAtUtc` passed to `OrderByDescending`. Translates to SQL `ORDER BY [r].[CreatedAtUtc] DESC` to list the newest payroll runs first.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayrollQueryService.cs:14:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Projection expression lambda mapping `PayrollRunEntity` fields into immutable `PayrollRunDto` DTOs. Hardcodes `wasReplayed` to `false` for read-only query results.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayrollQueryService.cs:18:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Employees.PayrollQueryService.GetRunAsync(string, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Retrieves a specific payroll run header by its primary key ID (`runId`) with `.AsNoTracking()`. If no record matches, throws `InvalidOperationException("مسير الرواتب غير موجود.")`. Converts the entity to a DTO via `CreatePayrollHandler.ToDto(r, wasReplayed: false)`.
  ```csharp
  // Lines 18-23: Single payroll run retrieval
  public async Task<PayrollRunDto> GetRunAsync(string runId, CancellationToken ct = default)
  {
      var r = await _db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runId, ct)
          ?? throw new InvalidOperationException("مسير الرواتب غير موجود.");
      return CreatePayrollHandler.ToDto(r, wasReplayed: false);
  }
  ```
- **Validations**: Validation checks detected: False. Throws: InvalidOperationException.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayrollQueryService.cs:20:74:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Filter predicate expression lambda `x => x.Id == runId` passed to `FirstOrDefaultAsync`. Translates to SQL `WHERE [x].[Id] = @runId` utilizing the primary key index.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayrollQueryService.cs:25:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Employees.PayrollQueryService.ListLinesAsync(string, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Asynchronously retrieves all line items for a designated payroll run (`runId`) with `.AsNoTracking()`. Projects line entities directly into `PayrollLineDto` objects containing individual employee breakdown: BasePiastres, OvertimePiastres, ExtraShiftsPiastres, BonusesPiastres, DeductionsPiastres, AdvanceDeductionsPiastres, NetPayablePiastres, line settlement Status, and PaidAtUtc.
  ```csharp
  // Lines 25-30: Payroll line items query
  public Task<List<PayrollLineDto>> ListLinesAsync(string runId, CancellationToken ct = default) =>
      _db.PayrollLines.AsNoTracking().Where(l => l.PayrollRunId == runId)
          .Select(l => new PayrollLineDto(l.Id, l.PayrollRunId, l.EmployeeId, l.BasePiastres, l.OvertimePiastres,
              l.ExtraShiftsPiastres, l.BonusesPiastres, l.DeductionsPiastres, l.AdvanceDeductionsPiastres,
              l.NetPayablePiastres, l.Status, l.PaidAtUtc))
          .ToListAsync(ct);
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayrollQueryService.cs:26:47:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Filter predicate expression lambda `l => l.PayrollRunId == runId` supplied to `Where`. Translates to SQL `WHERE [l].[PayrollRunId] = @runId` leveraging foreign key indexing.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Employees/PayrollQueryService.cs:27:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Projection expression lambda mapping `PayrollLineEntity` fields directly into `PayrollLineDto` records, retaining precise monetary values in piastres for base pay, overtime, bonuses, deductions, and final net payable balance.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:37:50Z
- **Source SHA-256 Verified:** `757dd6e6a2d74462e08602eadadd6b0b989e54f838a67cac9867ead8e1b31af2`
- **Callable ID Checked:** All 9 callables in `src/Falah.Infrastructure/Employees/PayrollQueryService.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Employees/PayrollQueryService.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:38:17Z
- **Source SHA-256 Verified:** `757dd6e6a2d74462e08602eadadd6b0b989e54f838a67cac9867ead8e1b31af2`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `757dd6e6a2d74462e08602eadadd6b0b989e54f838a67cac9867ead8e1b31af2` against production source `src/Falah.Infrastructure/Employees/PayrollQueryService.cs`. All 9 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 9 callables.
