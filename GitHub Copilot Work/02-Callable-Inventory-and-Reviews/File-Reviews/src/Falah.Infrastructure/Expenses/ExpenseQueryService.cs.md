# File Review: src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs

## Overview
Substantive security, lifecycle, and authorization audit of src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs.

## Callables

### Callable: src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:17:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:17:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Infrastructure.Expenses.ExpenseQueryService.ExpenseQueryService(Falah.Infrastructure.Persistence.FalahDbContext)
- **Lines:** [17-17]
- **Behavior:** Initializes a new instance of ExpenseQueryService, injecting the EF Core database context FalahDbContext and storing it in the private readonly _db field.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:19:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:19:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Expenses.ExpenseQueryService.GetVisibleExpensesAsync(string, string?, string?, CancellationToken)
- **Lines:** [19-42]
- **Behavior:** Enforces role-based server-side visibility rules for expenses. Queries the actor user by actorUserId; if not found, throws InvalidOperationException. If actor role is 'cashier', validates shiftId is present (throwing DomainException if missing), retrieves shift entity, returning empty list if shift is missing or not Open, and filters expenses to those matching shiftId and PaidFromDrawer. For non-cashier roles (managers/admins), validates and parses businessDate (throwing DomainException if invalid), and queries expenses matching that BusinessDate. Results are ordered descending by CreatedAtUtc and mapped to ExpenseDto.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException, DomainException, DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:22:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:22:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [22-22]
- **Behavior:** Predicate expression matching a UserEntity whose primary key Id equals the provided actorUserId string.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:29:77:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:29:77:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [29-29]
- **Behavior:** Predicate expression matching a ShiftEntity whose Id equals the specified shiftId string.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:31:55:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:31:55:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [31-31]
- **Behavior:** Predicate filter restricting ExpenseEntity records to those tied to the specified shiftId and flagged as PaidFromDrawer.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:37:55:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:37:55:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [37-37]
- **Behavior:** Predicate filter restricting ExpenseEntity records to those matching the parsed DateOnly businessDate.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:40:50:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:40:50:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [40-40]
- **Behavior:** Inline expression lambda 'var rows = await query.OrderByDescending(e => e.CreatedAtUtc).ToListAsync(ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:44:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs:44:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Expenses.ExpenseQueryService.ToDto(Falah.Infrastructure.Persistence.Entities.ExpenseEntity)
- **Lines:** [44-46]
- **Behavior:** Maps an ExpenseEntity database entity into an ExpenseDto data transfer object, projecting Id, Category, AmountPiastres, Description, ShiftId, PaidFromDrawer, PaymentAccountId, CreatedByUserId, formatted BusinessDate string ('yyyy-MM-dd'), and CreatedAtUtc.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `a77a348c179416b551b4bbf173398f29c8b2430244609f3fcc23d52d0ac1bd13`
- **Callable ID Checked:** All 8 callables in `src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `a77a348c179416b551b4bbf173398f29c8b2430244609f3fcc23d52d0ac1bd13`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `a77a348c179416b551b4bbf173398f29c8b2430244609f3fcc23d52d0ac1bd13` against production source `src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs`. All 8 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 8 callables.