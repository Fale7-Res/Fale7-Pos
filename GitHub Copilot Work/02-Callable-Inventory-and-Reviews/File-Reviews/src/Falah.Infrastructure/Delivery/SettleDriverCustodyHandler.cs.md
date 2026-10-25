# File Review: SettleDriverCustodyHandler.cs

## Overview
Substantive security, cash custody, and authorization audit of SettleDriverCustodyHandler.cs.

## Callables

### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:29:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:29:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Infrastructure.Delivery.SettleDriverCustodyHandler.SettleDriverCustodyHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog)
- **Lines:** [29-34]
- **Behavior:** Initializes SettleDriverCustodyHandler by injecting and storing FalahDbContext as _db, IApprovalRegistry as _approvalRegistry, and IAuditLog as _auditLog in private readonly fields for database persistence, authorization checks, and audit logging.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:36:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:36:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Delivery.SettleDriverCustodyHandler.HandleAsync(Falah.Application.Delivery.SettleDriverCustodyRequest, CancellationToken)
- **Lines:** [36-194]
- **Behavior:** Asynchronously settles all unresolved custodies and completed deliveries for a driver: checks idempotency via DriverSettlements, validates non-negative cash, resolves actor, authorizes 'delivery_manage' with optional manager approval, opens transaction, locks Deliveries and DriverCustodies via raw SQL WITH (UPDLOCK, ROWLOCK, HOLDLOCK) to prevent concurrent double-settlement, builds driver settlement scope via DeliveryRules.BuildDriverSettlementScope (requiring all deliveries to be in terminal states Delivered/Failed/Returned), computes trip totals via DeliveryRules.CalculateTripSettlement, enforces 'delivery_settlement_approve' authorization and non-empty reason if variance exists, verifies open business day via BusinessDayGate, posts cash movement to TreasuryAccounts.Main via EfFinancialJournal and TreasuryMovementFactory, marks custodies and deliveries as Settled, appends DeliveryEventEntity records and DriverSettlementEntity, logs audit event, and commits transaction with BalanceLockContention retry handling.
- **Validations:** Validation checks detected: True. Throws: DomainException, InvalidOperationException, DomainException, DomainException, DomainException, DomainException, DeliveryConflictException, DeliveryConflictException, DeliveryConflictException.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:39:87:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:39:87:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [39-39]
- **Behavior:** Predicate lambda `s => s.IdempotencyKey == idempotencyKey` querying DriverSettlements DbSet with AsNoTracking() to check whether the settlement request was previously processed.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:44:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:44:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [44-44]
- **Behavior:** Predicate lambda `u => u.Id == request.ActorUserId` querying Users DbSet with AsNoTracking() to load the actor user record for authorization.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:51:83:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:51:83:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [51-51]
- **Behavior:** Predicate lambda `u => u.Id == request.ManagerApproverUserId` querying Users DbSet with AsNoTracking() to resolve the manager approver user.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:59:81:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:59:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [59-59]
- **Behavior:** Predicate lambda `d => d.Id == request.DriverId` querying Drivers DbSet with AsNoTracking() to load the driver details.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:73:52:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:73:52:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [73-73]
- **Behavior:** Inline expression lambda 'var orderIds = deliveryEntities.Select(d => d.OrderId).ToList();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:74:112:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:74:112:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [74-74]
- **Behavior:** Predicate lambda `o => orderIds.Contains(o.Id)` querying Orders DbSet with AsNoTracking() to fetch orders corresponding to locked deliveries.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:75:50:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:75:50:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [75-75]
- **Behavior:** Inline expression lambda 'var ordersById = orders.ToDictionary(o => o.Id);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:76:49:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:76:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [76-76]
- **Behavior:** Projection lambda `d => DeliveryFactsComposer.Compose(d, ordersById[d.OrderId])` transforming delivery entities and their matching orders into domain DeliveryFacts.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:127:62:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:127:62:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [127-127]
- **Behavior:** Inline expression lambda 'var custodyIds = scope.UnsettledCustodies.Select(c => c.Id).ToHashSet();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:128:71:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:128:71:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [128-128]
- **Behavior:** Projection lambda `d => d.Id` extracting delivery IDs from scope.ResolvedDeliveries into a HashSet for batch status updates.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:130:65:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:130:65:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [130-130]
- **Behavior:** Predicate lambda `c => custodyIds.Contains(c.Id)` filtering tracked custody entities that are ready to be marked as Settled.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:136:67:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:136:67:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [136-136]
- **Behavior:** Predicate lambda `d => resolvedDeliveryIds.Contains(d.Id)` filtering tracked delivery entities that are ready to transition to Settled status.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:180:89:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:180:89:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [180-180]
- **Behavior:** Predicate lambda `s => s.IdempotencyKey == idempotencyKey` querying DriverSettlements DbSet with AsNoTracking() inside the exception handling block to attempt an immediate replay lookup.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:186:21:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:186:21:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [186-186]
- **Behavior:** Asynchronous delegate `async () => await _db.DriverSettlements.AsNoTracking().FirstOrDefaultAsync(s => s.IdempotencyKey == idempotencyKey, ct)` passed to BalanceLockContention.AwaitSiblingAsync to poll for the concurrent winner's committed settlement entity.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:186:96:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:186:96:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [186-186]
- **Behavior:** Predicate lambda `s => s.IdempotencyKey == idempotencyKey` querying DriverSettlements within the polling delegate passed to AwaitSiblingAsync.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:196:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs:196:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Delivery.SettleDriverCustodyHandler.ToResult(Falah.Infrastructure.Persistence.Entities.DriverSettlementEntity, bool)
- **Lines:** [196-197]
- **Behavior:** Private static projection mapping DriverSettlementEntity properties (Id, DriverId, ExpectedPiastres, ActualPiastres, DifferencePiastres, OrderCount, TreasuryMovementId, wasReplayed) into a SettleDriverCustodyResult record.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:37:50Z
- **Source SHA-256 Verified:** `0fc0eb77d5f271e3f4e3b7add9257e55e5287702737f5fab84718e7283f4d46a`
- **Callable ID Checked:** All 18 callables in `src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:38:17Z
- **Source SHA-256 Verified:** `0fc0eb77d5f271e3f4e3b7add9257e55e5287702737f5fab84718e7283f4d46a`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `0fc0eb77d5f271e3f4e3b7add9257e55e5287702737f5fab84718e7283f4d46a` against production source `src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs`. All 18 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 18 callables.
