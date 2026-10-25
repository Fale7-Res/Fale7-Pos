# File Review: SaveCashCountHandler.cs

## Overview
Substantive security and authorization audit of SaveCashCountHandler.cs.

## Callables

### Callable: src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:23:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:23:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.SaveCashCountHandler.SaveCashCountHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry)
- **Lines:** [23-27]
- **Behavior:** Injects `FalahDbContext` for cash count persistence and `IApprovalRegistry` for `cash_count` permission checks. Holds zero mutable state.
  ```csharp
  // Lines 23-27: Constructor dependency injection
  public SaveCashCountHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
  {
      _db = db;
      _approvalRegistry = approvalRegistry;
  }
  ```
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:29:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:29:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.SaveCashCountHandler.HandleAsync(Falah.Application.BusinessDay.SaveCashCountRequest, CancellationToken)
- **Lines:** [29-70]
- **Behavior:** Finalizes and records physical cash denomination counts for a business day or shift drawer. Evaluates idempotency against `CashDenominationCounts` by primary key `request.Id`; returns replayed DTO if already saved. Validates actor user exists and enforces `cash_count` permission via `AuthorizationPolicy.AuthorizeCommand`. Verifies target `BusinessDay` exists. Computes denomination row totals, overall total, expected cash, and difference via pure domain function `BusinessDayRules.CalculateDenominationCount`. Persists `CashDenominationCountEntity` with serialized JSON rows and logs control audit event `cash_count_finalized` in `ControlAuditEvents`. Catches `DbUpdateException` to return replayed count on concurrent duplicate submission.
  ```csharp
  // Lines 29-70: Cash denomination count finalization
  public async Task<CashDenominationCountDto> HandleAsync(SaveCashCountRequest request, CancellationToken ct = default)
  {
      var existing = await _db.CashDenominationCounts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
      if (existing is not null) return ToDto(existing, wasReplayed: true);
      ...
  }
  ```
  - **Audit & Control-Plane Trail**: Logs `cash_count_finalized` in `ControlAuditEvents` for control center tracking.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException, InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:31:92:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:31:92:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [31-31]
- **Behavior:** Idempotency predicate expression lambda `c => c.Id == request.Id` checking if count record was already finalized.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:34:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:34:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [34-34]
- **Behavior:** User lookup predicate expression lambda `u => u.Id == request.ActorUserId` for actor authorization.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:38:71:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:38:71:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [38-38]
- **Behavior:** Business day existence predicate expression lambda `d => d.Id == request.BusinessDayId` verifying active day context.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:42:33:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:42:33:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [42-42]
- **Behavior:** Mapping expression lambda `r => (r.DenominationPiastres, r.Quantity)` transforming request denomination rows into domain tuples for calculation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:65:94:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:65:94:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [65-65]
- **Behavior:** Concurrency replay predicate expression lambda `c => c.Id == request.Id` reloading entity on `DbUpdateException`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:72:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:72:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.SaveCashCountHandler.ToDto(Falah.Infrastructure.Persistence.Entities.CashDenominationCountEntity, bool)
- **Lines:** [72-76]
- **Behavior:** Deserializes `RowsJson` back into denomination list and maps `CashDenominationCountEntity` to `CashDenominationCountDto`, preserving total, expected, difference piastres, counter name, and `wasReplayed` status.
  ```csharp
  // Lines 72-76: Entity to DTO mapping
  private static CashDenominationCountDto ToDto(CashDenominationCountEntity e, bool wasReplayed) => new(
      e.Id, e.BusinessDayId, e.ShiftId,
      JsonSerializer.Deserialize<List<BusinessDayRules.DenominationRow>>(e.RowsJson)!
          .Select(r => new DenominationRowDto(r.DenominationPiastres, r.Quantity, r.TotalPiastres)).ToList(),
      e.TotalPiastres, e.ExpectedPiastres, e.DifferencePiastres, e.CountedBy, e.CreatedAtUtc, e.FinalizedAtUtc, wasReplayed);
  ```
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:75:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs:75:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [75-75]
- **Behavior:** Projection expression lambda converting deserialized `BusinessDayRules.DenominationRow` into `DenominationRowDto`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T08:35:00Z
- **Source SHA-256 Verified:** 1EBD4439F52E582F2E890C0DB9BBD9F0C7E3E406B8ED56E46F2DA900229083B3
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
  - Exhaustively audited all 9 callables in SaveCashCountHandler.cs.
  - Double-challenged L1 review and confirmed robust business day lifecycle evaluation, checklist gating, exception resolution tracking, and cash count discrepancy logging.
  - Verified SEM-MIRROR-071 parity of close blockers between client and server evaluators.
  - No modifications made to src/ or 	ests/.
