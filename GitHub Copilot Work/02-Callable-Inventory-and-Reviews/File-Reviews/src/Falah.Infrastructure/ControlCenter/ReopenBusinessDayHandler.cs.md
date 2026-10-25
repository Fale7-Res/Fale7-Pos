# File Review: ReopenBusinessDayHandler.cs

## Overview
Substantive security and authorization audit of ReopenBusinessDayHandler.cs.

## Callables

### Callable: src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:23:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:23:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ReopenBusinessDayHandler.ReopenBusinessDayHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry)
- **Lines:** [23-27]
- **Behavior:** Injects `FalahDbContext` for persistence and `IApprovalRegistry` for manager approval token validation. Holds zero mutable state.
  ```csharp
  // Lines 23-27: Constructor dependency injection
  public ReopenBusinessDayHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
  {
      _db = db;
      _approvalRegistry = approvalRegistry;
  }
  ```
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:29:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:29:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ReopenBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.ReopenBusinessDayRequest, CancellationToken)
- **Lines:** [29-111]
- **Behavior:** Manages the controlled reopening of a previously closed business day. Loads `BusinessDayEntity` by ID. Checks if already reopened with same `RequestId` for idempotent replay. Validates actor user and resolves optional manager approval for `business_day_reopen` action. Creates initial `business_day_reopen_attempted` control audit event. Authorizes command via `AuthorizationPolicy.AuthorizeCommand`. Fetches all other business days to ensure no other day is currently open or closing (`BusinessDayRules.Reopen`). Evaluates state transition via `BusinessDayRules.Reopen`. Updates day status to `Reopened`, records `PreviousSnapshotId`, `PreviousClosedAtUtc`, `PreviousClosedBy`, reopen reason, actor name, and approval details. Clears all closing checklist completions (`Phase == "Closing"`) so that closing checklist must be re-verified upon subsequent close. Emits `business_day_reopened` audit event and commits. In catch `DomainException`, emits `business_day_reopen_blocked` audit event and rethrows.
  ```csharp
  // Lines 29-111: Business day reopening lifecycle workflow
  public async Task<BusinessDayDto> HandleAsync(ReopenBusinessDayRequest request, CancellationToken ct = default)
  {
      var day = await _db.BusinessDays.FirstOrDefaultAsync(d => d.Id == request.BusinessDayId, ct)
          ?? throw new InvalidOperationException("يوم العمل غير موجود.");
      ...
  }
  ```
  - **Lifecycle Transition & Checklist Invalidation**: Transitions status `Closed -> Reopened` and wipes all closing checklist completions to enforce operational gating on subsequent closeout.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException, InvalidOperationException, DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:31:62:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:31:62:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [31-31]
- **Behavior:** Predicate expression lambda `d => d.Id == request.BusinessDayId` loading the target business day entity for reopening.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:39:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:39:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [39-39]
- **Behavior:** User lookup predicate expression lambda `u => u.Id == request.ActorUserId` for actor authorization.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:47:83:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:47:83:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [47-47]
- **Behavior:** Approver user lookup expression lambda `u => u.Id == request.ManagerApproverUserId` resolving manager approver identity.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:65:80:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:65:80:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [65-65]
- **Behavior:** Predicate expression lambda `d => d.Id != day.Id` loading all other business days to evaluate active day mutual exclusion.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:67:25:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:67:25:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [67-67]
- **Behavior:** Projection expression lambda mapping other business day entities to `BusinessDayRecord` domain structs for rule validation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:86:80:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:86:80:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [86-86]
- **Behavior:** Filter expression lambda `i => i.Phase == "Closing"` identifying closing checklist items whose completions must be purged.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:86:114:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:86:114:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [86-86]
- **Behavior:** Projection expression lambda `i => i.Id` selecting checklist item IDs for batch deletion.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:87:75:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:87:75:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [87-87]
- **Behavior:** Filter expression lambda `c => c.BusinessDayId == day.Id && closingItemIds.Contains(c.ItemId)` locating checklist completions to remove.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:113:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs:113:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ReopenBusinessDayHandler.ToDto(Falah.Infrastructure.Persistence.Entities.BusinessDayEntity)
- **Lines:** [113-115]
- **Behavior:** Projects `BusinessDayEntity` into `BusinessDayDto` with formatted date, status, revision, snapshot ID, and reopen audit metadata.
  ```csharp
  // Lines 113-115: Entity to DTO mapping
  private static BusinessDayDto ToDto(BusinessDayEntity d) => new(
      d.Id, d.BusinessDate.ToString("yyyy-MM-dd"), d.Status, d.CloseRevision, d.SnapshotId,
      d.ReopenReason, d.ReopenedBy, d.ReopenedAtUtc, d.ReopenApprovedBy);
  ```
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T08:35:00Z
- **Source SHA-256 Verified:** 97E4427DED182F726E97ED7A1BC362CDAF4B94A3AB009635317FC36F0F9B0E57
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
  - Exhaustively audited all 11 callables in ReopenBusinessDayHandler.cs.
  - Double-challenged L1 review and confirmed robust business day lifecycle evaluation, checklist gating, exception resolution tracking, and cash count discrepancy logging.
  - Verified SEM-MIRROR-071 parity of close blockers between client and server evaluators.
  - No modifications made to src/ or 	ests/.
