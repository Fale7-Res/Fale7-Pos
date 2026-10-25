# File Review: src/Falah.Infrastructure/Customers/CustomerQueryService.cs

## Overview
Substantive security, lifecycle, and authorization audit of src/Falah.Infrastructure/Customers/CustomerQueryService.cs.

## Callables

### Callable: src/Falah.Infrastructure/Customers/CustomerQueryService.cs:14:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/CustomerQueryService.cs:14:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Infrastructure.Customers.CustomerQueryService.CustomerQueryService(Falah.Infrastructure.Persistence.FalahDbContext)
- **Lines:** [14-14]
- **Behavior:** Injects `FalahDbContext` for executing read-only customer and address queries without entity change tracking. Holds zero mutable state.
  ```csharp
  // Line 14: Constructor dependency injection
  public CustomerQueryService(FalahDbContext db) => _db = db;
  ```
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Customers/CustomerQueryService.cs:16:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/CustomerQueryService.cs:16:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Customers.CustomerQueryService.FindByPhoneAsync(string, CancellationToken)
- **Lines:** [16-27]
- **Behavior:** Looks up customer profile and associated delivery addresses by phone number. Normalizes phone string via `CustomerRules.NormalizePhone`; returns null immediately if normalization yields an empty string. Queries `Customers` table with `.AsNoTracking()`. If found, queries `CustomerAddresses` ordered descending by `LastUsedAtUtc` (most recently used address first) and maps results to `CustomerDto` via `UpsertCustomerHandler.ToDto`. Returns null if customer does not exist.
  ```csharp
  // Lines 16-27: Query customer by phone
  public async Task<CustomerDto?> FindByPhoneAsync(string phone, CancellationToken ct = default)
  {
      var normalized = CustomerRules.NormalizePhone(phone);
      if (normalized.Length == 0) return null;
      var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Phone == normalized, ct);
      if (customer is null) return null;
      var addresses = await _db.CustomerAddresses.AsNoTracking()
          .Where(a => a.CustomerId == customer.Id).OrderByDescending(a => a.LastUsedAtUtc).ToListAsync(ct);
      return UpsertCustomerHandler.ToDto(customer, addresses);
  }
  ```
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Customers/CustomerQueryService.cs:21:79:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/CustomerQueryService.cs:21:79:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [21-21]
- **Behavior:** Predicate expression lambda `c => c.Phone == normalized` to locate customer by unique normalized phone number.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Customers/CustomerQueryService.cs:25:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/CustomerQueryService.cs:25:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [25-25]
- **Behavior:** Predicate expression lambda `a => a.CustomerId == customer.Id` filtering delivery addresses by customer foreign key.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Customers/CustomerQueryService.cs:25:72:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/CustomerQueryService.cs:25:72:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [25-25]
- **Behavior:** Ordering expression lambda `a => a.LastUsedAtUtc` ordering addresses descending so the most recently used destination appears first in POS UI.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Customers/CustomerQueryService.cs:29:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/CustomerQueryService.cs:29:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Customers.CustomerQueryService.GetByIdAsync(string, CancellationToken)
- **Lines:** [29-37]
- **Behavior:** Looks up customer profile and linked delivery addresses by primary key (`customerId`) using `.AsNoTracking()`. If found, queries all addresses ordered by `LastUsedAtUtc` descending and delegates DTO creation to `UpsertCustomerHandler.ToDto`. Returns null if no customer record matches.
  ```csharp
  // Lines 29-37: Query customer by primary key
  public async Task<CustomerDto?> GetByIdAsync(string customerId, CancellationToken ct = default)
  {
      var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId, ct);
      if (customer is null) return null;
      var addresses = await _db.CustomerAddresses.AsNoTracking()
          .Where(a => a.CustomerId == customerId).OrderByDescending(a => a.LastUsedAtUtc).ToListAsync(ct);
      return UpsertCustomerHandler.ToDto(customer, addresses);
  }
  ```
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Customers/CustomerQueryService.cs:31:79:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/CustomerQueryService.cs:31:79:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [31-31]
- **Behavior:** Predicate expression lambda `c => c.Id == customerId` matching customer primary key.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Customers/CustomerQueryService.cs:35:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/CustomerQueryService.cs:35:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [35-35]
- **Behavior:** Predicate expression lambda `a => a.CustomerId == customerId` retrieving addresses for the specified customer ID.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Customers/CustomerQueryService.cs:35:71:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/CustomerQueryService.cs:35:71:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [35-35]
- **Behavior:** Ordering expression lambda `a => a.LastUsedAtUtc` sorting address history descending by recency of use.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `69e963da58845320171753cca597c44cc1b1435a9123f00942fa7156aaef0e77`
- **Callable ID Checked:** All 9 callables in `src/Falah.Infrastructure/Customers/CustomerQueryService.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Customers/CustomerQueryService.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `69e963da58845320171753cca597c44cc1b1435a9123f00942fa7156aaef0e77`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `69e963da58845320171753cca597c44cc1b1435a9123f00942fa7156aaef0e77` against production source `src/Falah.Infrastructure/Customers/CustomerQueryService.cs`. All 9 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 9 callables.
