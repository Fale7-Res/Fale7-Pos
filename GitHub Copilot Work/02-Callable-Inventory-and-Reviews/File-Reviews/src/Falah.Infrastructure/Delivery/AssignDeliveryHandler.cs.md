# File Review: AssignDeliveryHandler.cs

## Overview
Substantive security, cash custody, and authorization audit of AssignDeliveryHandler.cs.

## Callables

### Callable: src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:29:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:29:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Infrastructure.Delivery.AssignDeliveryHandler.AssignDeliveryHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog)
- **Lines:** [29-34]
- **Behavior:** Initializes AssignDeliveryHandler by injecting and storing FalahDbContext as _db, IApprovalRegistry as _approvalRegistry, and IAuditLog as _auditLog in private readonly fields for database access, authorization evaluation, and audit logging.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:36:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:36:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Delivery.AssignDeliveryHandler.HandleAsync(Falah.Application.Delivery.AssignDeliveryRequest, CancellationToken)
- **Lines:** [36-151]
- **Behavior:** Asynchronously assigns an order in Ready status to an active driver, transitioning it atomically to Assigned and OutForDelivery: checks idempotency via DeliveryEvents (eventId = $"delivery-assignment-{request.RequestId}"), resolves actor user and enforces 'delivery_manage' permission with optional manager approval, opens ReadCommitted transaction, locks delivery via raw SQL WITH (UPDLOCK, ROWLOCK, HOLDLOCK), verifies Ready status, verifies driver assignability via DeliveryRules.AssertDriverAssignable, asserts lifecycle transitions via DeliveryTransitions.AssertTransition, creates DriverCustodyEntity in Open status for cash orders, mutates delivery status/trip/timestamps, logs assignment DeliveryEventEntity, enqueues DriverDispatch PrintJobEntity (deduplicated by RequestId), appends audit log event, and commits transaction.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException, InvalidOperationException, DomainException, InvalidOperationException, DomainException, InvalidOperationException, InvalidOperationException, DeliveryConflictException.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:39:89:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:39:89:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [39-39]
- **Behavior:** Predicate lambda `e => e.Id == eventId` querying DeliveryEvents DbSet with AsNoTracking() to check whether this assignment request ID has already been executed.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:42:84:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:42:84:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [42-42]
- **Behavior:** Predicate lambda `d => d.Id == request.DeliveryId` querying Deliveries DbSet with AsNoTracking() to reload the delivery entity when replaying an idempotent assignment request.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:47:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:47:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [47-47]
- **Behavior:** Predicate lambda `u => u.Id == request.ActorUserId` querying Users DbSet with AsNoTracking() to load the actor user record for authorization.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:54:83:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:54:83:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [54-54]
- **Behavior:** Predicate lambda `u => u.Id == request.ManagerApproverUserId` querying Users DbSet with AsNoTracking() to resolve the manager approver user.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:71:85:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:71:85:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [71-71]
- **Behavior:** Predicate lambda `d => d.Id == request.DriverId` querying Drivers DbSet with AsNoTracking() to load the target driver record for assignment.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:81:77:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:81:77:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [81-81]
- **Behavior:** Predicate lambda `o => o.Id == delivery.OrderId` querying Orders DbSet with AsNoTracking() to load the associated order record for delivery facts composition.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:142:86:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:142:86:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [142-142]
- **Behavior:** Predicate lambda `e => e.Id == eventId` querying DeliveryEvents DbSet with AsNoTracking() within the DbUpdateException catch handler to confirm if a race condition resulted from a concurrent identical insert.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:145:88:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:145:88:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [145-145]
- **Behavior:** Predicate lambda `d => d.Id == request.DeliveryId` querying Deliveries DbSet with AsNoTracking() inside the exception replay path to reload and return the delivery entity.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:153:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:153:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.Delivery.AssignDeliveryHandler.ToResult(Falah.Infrastructure.Persistence.Entities.DeliveryEntity, bool)
- **Lines:** [153-154]
- **Behavior:** Private static helper method projecting DeliveryEntity state (Id, Status, DriverId, DriverNameAtHandover, TripId, wasReplayed) into an AssignDeliveryResult record.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:37:50Z
- **Source SHA-256 Verified:** `a083e3ed905ba0874f782967aa4d3dc9cfbaddbf8fd06c59d4269ff27f0e3e2b`
- **Callable ID Checked:** All 11 callables in `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-26T02:50:00Z
- **Source SHA-256 Verified:** `a083e3ed905ba0874f782967aa4d3dc9cfbaddbf8fd06c59d4269ff27f0e3e2b`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Independent Code-Level Findings & Per-Callable Rationale:**
  1. `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:29:5:ConstructorDeclaration`:
     - *Verified Lines 29–34*: Injects `FalahDbContext`, `IApprovalRegistry`, and `IAuditLog`. Pure dependency injection into readonly fields. Verdict: **ACCEPTED**.
  2. `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:36:5:MethodDeclaration`:
     - *Verified Lines 36–151*: `HandleAsync(AssignDeliveryRequest, CancellationToken)` executes atomic dispatch assignment. Pre-checks idempotency against `DeliveryEvents` using `delivery-assignment-{request.RequestId}`. Enforces `delivery_manage` permission. Acquires pessimistic lock `SELECT ... WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}` inside a ReadCommitted transaction. Enforces that order status is strictly `Ready`. Validates driver assignability via `DeliveryRules.AssertDriverAssignable`. Validates lifecycle state transitions: `Ready -> Assigned` and `Assigned -> OutForDelivery`. Creates `DriverCustodyEntity` in `Open` status for cash orders via `DeliveryRules.CreateCashCustody`. Creates `PrintJobEntity` for driver dispatch ticket with idempotent key `printjob-{request.RequestId}-DriverDispatch`. Logs audit event. Robust exception recovery: intercepts `DbUpdateException`, rolls back transaction, checks for idempotent replay, and returns existing state. Verdict: **ACCEPTED**.
  3. `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:39:89:SimpleLambdaExpression`:
     - *Verified Line 39*: `e => e.Id == eventId` on `_db.DeliveryEvents.AsNoTracking()`. Idempotency pre-check query predicate. Verdict: **ACCEPTED**.
  4. `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:42:84:SimpleLambdaExpression`:
     - *Verified Line 42*: `d => d.Id == request.DeliveryId` on `_db.Deliveries.AsNoTracking()`. Replay query reloading delivery entity. Verdict: **ACCEPTED**.
  5. `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:47:76:SimpleLambdaExpression`:
     - *Verified Line 47*: `u => u.Id == request.ActorUserId` on `_db.Users.AsNoTracking()`. Actor user resolution predicate. Verdict: **ACCEPTED**.
  6. `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:54:83:SimpleLambdaExpression`:
     - *Verified Line 54*: `u => u.Id == request.ManagerApproverUserId` on `_db.Users.AsNoTracking()`. Manager approver lookup predicate. Verdict: **ACCEPTED**.
  7. `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:71:85:SimpleLambdaExpression`:
     - *Verified Line 71*: `d => d.Id == request.DriverId` on `_db.Drivers.AsNoTracking()`. Driver existence and profile lookup predicate. Verdict: **ACCEPTED**.
  8. `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:81:77:SimpleLambdaExpression`:
     - *Verified Line 81*: `o => o.Id == delivery.OrderId` on `_db.Orders.AsNoTracking()`. Loads linked order to compose financial delivery facts. Verdict: **ACCEPTED**.
  9. `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:142:86:SimpleLambdaExpression`:
     - *Verified Line 142*: `e => e.Id == eventId` on `_db.DeliveryEvents.AsNoTracking()`. DbUpdateException catch handler replay check predicate. Verdict: **ACCEPTED**.
  10. `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:145:88:SimpleLambdaExpression`:
      - *Verified Line 145*: `d => d.Id == request.DeliveryId` on `_db.Deliveries.AsNoTracking()`. DbUpdateException reload predicate. Verdict: **ACCEPTED**.
  11. `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:153:5:MethodDeclaration`:
      - *Verified Lines 153–155*: `ToResult(DeliveryEntity delivery, bool wasReplayed)` maps entity properties to immutable `AssignDeliveryResult`. Side-effect-free projection. Verdict: **ACCEPTED**.
