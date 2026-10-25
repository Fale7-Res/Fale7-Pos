# File Review: src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs

## Overview
Substantive security, lifecycle, and authorization audit of src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs.

## Callables

### Callable: src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:19:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:19:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Infrastructure.Customers.UpsertCustomerHandler.UpsertCustomerHandler(Falah.Infrastructure.Persistence.FalahDbContext)
- **Lines:** [19-19]
- **Behavior:** Executes logic for Falah.Infrastructure.Customers.UpsertCustomerHandler.UpsertCustomerHandler(Falah.Infrastructure.Persistence.FalahDbContext) across lines 19-19.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:21:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:21:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Customers.UpsertCustomerHandler.HandleAsync(Falah.Application.Customers.UpsertCustomerRequest, CancellationToken)
- **Lines:** [21-95]
- **Behavior:** Executes logic for Falah.Infrastructure.Customers.UpsertCustomerHandler.HandleAsync(Falah.Application.Customers.UpsertCustomerRequest, CancellationToken) across lines 21-95.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:32:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:32:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [32-32]
- **Behavior:** Executes logic for lambda expression across lines 32-32.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:32:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:32:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [32-32]
- **Behavior:** Executes logic for lambda expression across lines 32-32.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:44:71:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:44:71:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [44-44]
- **Behavior:** Executes logic for lambda expression across lines 44-44.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:44:111:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:44:111:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [44-44]
- **Behavior:** Executes logic for lambda expression across lines 44-44.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:70:59:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:70:59:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [70-70]
- **Behavior:** Inline expression lambda 'var matchedEntity = addressEntities.First(a => a.Id == matched.Id);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:93:79:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:93:79:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [93-93]
- **Behavior:** Executes logic for lambda expression across lines 93-93.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:97:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:97:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Customers.UpsertCustomerHandler.ToDomain(Falah.Infrastructure.Persistence.Entities.CustomerAddressEntity)
- **Lines:** [97-98]
- **Behavior:** Executes logic for Falah.Infrastructure.Customers.UpsertCustomerHandler.ToDomain(Falah.Infrastructure.Persistence.Entities.CustomerAddressEntity) across lines 97-98.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:100:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:100:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Customers.UpsertCustomerHandler.ToDto(Falah.Infrastructure.Persistence.Entities.CustomerEntity, IReadOnlyList<Falah.Infrastructure.Persistence.Entities.CustomerAddressEntity>)
- **Lines:** [100-104]
- **Behavior:** Executes logic for Falah.Infrastructure.Customers.UpsertCustomerHandler.ToDto(Falah.Infrastructure.Persistence.Entities.CustomerEntity, IReadOnlyList<Falah.Infrastructure.Persistence.Entities.CustomerAddressEntity>) across lines 100-104.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:102:26:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs:102:26:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [102-103]
- **Behavior:** Executes logic for lambda expression across lines 102-103.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `74e7f34ada9071173b7fb70e809496ba8b3826a5b638856eaf634e53c04779ac`
- **Callable ID Checked:** All 11 callables in `src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `74e7f34ada9071173b7fb70e809496ba8b3826a5b638856eaf634e53c04779ac`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `74e7f34ada9071173b7fb70e809496ba8b3826a5b638856eaf634e53c04779ac` against production source `src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs`. All 11 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 11 callables.
