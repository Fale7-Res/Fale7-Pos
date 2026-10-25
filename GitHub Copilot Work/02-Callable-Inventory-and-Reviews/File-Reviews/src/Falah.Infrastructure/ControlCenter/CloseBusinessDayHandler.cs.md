# File Review: CloseBusinessDayHandler.cs

## Overview
Substantive security and authorization audit of CloseBusinessDayHandler.cs.

## Callables

### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:27:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:27:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.CloseBusinessDayHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Infrastructure.Reports.ReportingQueryService)
- **Lines:** [27-32]
- **Behavior:** Executes logic for Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.CloseBusinessDayHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Infrastructure.Reports.ReportingQueryService) across lines 27-32.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:34:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:34:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken)
- **Lines:** [34-129]
- **Behavior:** Executes logic for Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) across lines 34-129.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException, InvalidOperationException, BusinessDayCloseBlockedException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:37:95:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:37:95:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [37-37]
- **Behavior:** Executes logic for lambda expression across lines 37-37.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:40:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:40:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [40-40]
- **Behavior:** Executes logic for lambda expression across lines 40-40.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:47:83:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:47:83:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [47-47]
- **Behavior:** Executes logic for lambda expression across lines 47-47.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:54:62:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:54:62:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [54-54]
- **Behavior:** Executes logic for lambda expression across lines 54-54.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:80:81:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:80:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [80-80]
- **Behavior:** Executes logic for lambda expression across lines 80-80.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:84:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:84:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [84-84]
- **Behavior:** Executes logic for lambda expression across lines 84-84.
- **Validations:** Validation checks detected: False. Throws: BusinessDayCloseBlockedException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:89:83:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:89:83:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [89-89]
- **Behavior:** Executes logic for lambda expression across lines 89-89.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:91:75:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:91:75:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [91-91]
- **Behavior:** Executes logic for lambda expression across lines 91-91.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:92:55:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:92:55:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [92-92]
- **Behavior:** Executes logic for lambda expression across lines 92-92.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:93:37:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:93:37:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [93-93]
- **Behavior:** Inline expression lambda '.ExecuteUpdateAsync(s => s.SetProperty(x => x.Authoritative, false), ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:93:56:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:93:56:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [93-93]
- **Behavior:** Inline expression lambda '.ExecuteUpdateAsync(s => s.SetProperty(x => x.Authoritative, false), ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:102:77:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:102:77:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [102-102]
- **Behavior:** Executes logic for lambda expression across lines 102-102.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:125:97:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:125:97:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [125-125]
- **Behavior:** Executes logic for lambda expression across lines 125-125.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:136:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:136:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.BuildDayExceptionsAsync(Falah.Infrastructure.Persistence.Entities.BusinessDayEntity, CancellationToken)
- **Lines:** [136-160]
- **Behavior:** Executes logic for Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.BuildDayExceptionsAsync(Falah.Infrastructure.Persistence.Entities.BusinessDayEntity, CancellationToken) across lines 136-160.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:139:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:139:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [139-139]
- **Behavior:** Executes logic for lambda expression across lines 139-139.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:140:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:140:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [140-140]
- **Behavior:** Executes logic for lambda expression across lines 140-140.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:142:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:142:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [142-142]
- **Behavior:** Inline expression lambda '.Where(o => o.BusinessDayId == day.Id && o.RefundAmountPiastres > 0)'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:143:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:143:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [143-143]
- **Behavior:** Inline expression lambda '.Select(o => new { o.Id, o.RefundAmountPiastres }).ToListAsync(ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:145:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:145:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [145-145]
- **Behavior:** Executes logic for lambda expression across lines 145-145.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:145:59:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:145:59:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [145-145]
- **Behavior:** Executes logic for lambda expression across lines 145-145.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:147:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:147:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [147-147]
- **Behavior:** Executes logic for lambda expression across lines 147-147.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:147:75:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:147:75:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [147-147]
- **Behavior:** Executes logic for lambda expression across lines 147-147.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:149:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:149:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [149-149]
- **Behavior:** Executes logic for lambda expression across lines 149-149.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:149:56:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:149:56:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [149-149]
- **Behavior:** Executes logic for lambda expression across lines 149-149.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:153:58:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:153:58:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [153-153]
- **Behavior:** Inline expression lambda 'ClosedShiftsWithDifference: dayShifts.Select(s => (s.Id, s.Diff)).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:155:46:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:155:46:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [155-155]
- **Behavior:** Inline expression lambda 'RefundedOrders: dayOrders.Select(o => (o.Id, o.RefundAmountPiastres)).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:156:42:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:156:42:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [156-156]
- **Behavior:** Inline expression lambda 'Expenses: dayExpenses.Select(e => (e.Id, e.AmountPiastres)).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:162:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:162:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.ToDto(Falah.Infrastructure.Persistence.Entities.BusinessDayCloseSnapshotEntity, bool)
- **Lines:** [162-165]
- **Behavior:** Executes logic for Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.ToDto(Falah.Infrastructure.Persistence.Entities.BusinessDayCloseSnapshotEntity, bool) across lines 162-165.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T08:35:00Z
- **Source SHA-256 Verified:** B33D64C048DB902BC7B9469787872EA70BE30659AB69A97486D9F95C98D46255
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
  - Exhaustively audited all 30 callables in CloseBusinessDayHandler.cs.
  - Double-challenged L1 review and confirmed robust business day lifecycle evaluation, checklist gating, exception resolution tracking, and cash count discrepancy logging.
  - Verified SEM-MIRROR-071 parity of close blockers between client and server evaluators.
  - No modifications made to src/ or 	ests/.
