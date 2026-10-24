# Substantive Callable Audit: `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `ac7a5a9aefba464dfb2444c86d6a5bb22149455b2f82856f3fea4971931ff941`
- **Auditor Specialist:** `printing-workflow-auditor`
- **Total File Callables:** **10**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Print job creation handler dispatching ticket generation across the 6 system routes (Receipt, Kitchen, Grill, Shawarma, Appetizers, CallCenter). Hosts critical defect PRT-001 (ticket duplication).

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Infrastructure.Printing.CreatePrintJobsHandler.CreatePrintJobsHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Audit.IAuditLog)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:30:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `30-30`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Infrastructure.Printing.CreatePrintJobsHandler.CreatePrintJobsHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Audit.IAuditLog). Manages internal state transition within src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 30-30):**
```csharp
  30 |     public CreatePrintJobsHandler(FalahDbContext db, IAuditLog auditLog) { _db = db; _auditLog = auditLog; }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Infrastructure.Printing.CreatePrintJobsHandler.HandleAsync(Falah.Application.Printing.CreatePrintJobsRequest, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:32:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `32-93`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`PRT-001`**

**A. Actual Behavior & Execution Path:**  
Checks existing print jobs by CreateRequestId. If not found, groups order items by preparation stations, generates PrintJobEntity instances, and persists them to PrintJobs table.

**B. Trust Boundaries & Validation:**  
Print request from cashier checkout auto-print or manual reprint dialog.

**C. State Mutations & Side Effects:**  
Inserts PrintJobEntity records to database.

**D. Concurrency & Transaction Implications:**  
Only dedupes on RequestId. Missing unique composite constraint on (OrderId, Route) in PrintJobs table allows concurrent checkout events or auto-print/reprint collisions to generate duplicate kitchen tickets.

**E. Specialist Risks & Invariants:**  
CRITICAL FLAW PRT-001: Duplicate kitchen prep tickets causing food waste and double meal preparation.

> **Verbatim Code Excerpt (Lines 32-93):**
```csharp
  32 |     public async Task<CreatePrintJobsResult> HandleAsync(CreatePrintJobsRequest request, CancellationToken ct = default)
  33 |     {
  34 |         var existing = await _db.PrintJobs.AsNoTracking()
  35 |             .Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
  36 |         if (existing.Count > 0) return new CreatePrintJobsResult(existing.Select(ToDto).ToList(), WasReplayed: true);
  37 | 
  38 |         PrintRoute? customerRoute = null;
  39 |         if (!string.IsNullOrEmpty(request.CustomerRoute))
  40 |         {
  41 |             if (!Enum.TryParse<PrintRoute>(request.CustomerRoute, out var parsed) || parsed is not (PrintRoute.Receipt or PrintRoute.CallCenter))
  42 |                 throw new DomainException("مسار الطباعة المطلوب للعميل غير صالح.");
  43 |             customerRoute = parsed;
  44 |         }
  45 | 
  46 |         var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
  47 |             ?? throw new InvalidOperationException("الطلب غير موجود.");
  48 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
  49 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
  50 | 
  51 |         IReadOnlyCollection<PrintRoute> kitchenRoutes = request.IncludeKitchenRoutes
  52 |             ? PrintRules.DetermineKitchenRoutes(order.Items.Select(i => i.PreparationStation))
  53 |             : Array.Empty<PrintRoute>();
  54 | 
  55 |         var routes = new List<PrintRoute>(kitchenRoutes);
  56 |         if (customerRoute is { } cr) routes.Insert(0, cr);
  57 |         if (routes.Count == 0) throw new DomainException("لا توجد محطة طباعة مطلوبة لهذا الطلب.");
  58 | 
  59 |         var now = DateTimeOffset.UtcNow;
  60 |         var jobs = routes.Select(route => new PrintJobEntity
  61 |         {
  62 |             Id = $"printjob-{request.RequestId}-{route}", CreateRequestId = request.RequestId, OrderId = order.Id, OrderNumber = order.OrderNumber,
  63 |             Route = route.ToString(), Status = nameof(PrintJobStatus.Pending), AttemptCount = 0,
  64 |             IsReprint = request.IsReprint, CreatedBy = actorUser.Name, CreatedAtUtc = now,
  65 |         }).ToList();
  66 | 
  67 |         try
  68 |         {
  69 |             _db.PrintJobs.AddRange(jobs);
  70 |             // Only a genuine REPRINT is audited here - a routine first print of a fresh order is
  71 |             // not a sensitive event (PrintJobEntity/PrintJobAttemptEntity already carry its own full
  72 |             // attempt history); reproducing an already-committed order's tickets later is the
  73 |             // noteworthy action worth surfacing in the general trail too.
  74 |             if (request.IsReprint)
  75 |             {
  76 |                 _auditLog.Append(new AuditEvent(
  77 |                     $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
  78 |                     "إعادة طباعة طلب", "order",
  79 |                     $"إعادة طباعة الطلب {order.OrderNumber} - المسارات: {string.Join(", ", routes)}"
  80 |                         + (string.IsNullOrWhiteSpace(request.Reason) ? "" : $" - السبب: {request.Reason.Trim()}"),
  81 |                     "normal", now, ReferenceId: order.Id, Reason: request.Reason));
  82 |             }
  83 |             await _db.SaveChangesAsync(ct);
  84 |         }
  85 |         catch (DbUpdateException)
  86 |         {
  87 |             var replay = await _db.PrintJobs.AsNoTracking().Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
  88 |             if (replay.Count > 0) return new CreatePrintJobsResult(replay.Select(ToDto).ToList(), WasReplayed: true);
  89 |             throw new PrintingConflictException("تعذر إنشاء طلبات الطباعة بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
  90 |         }
  91 | 
  92 |         return new CreatePrintJobsResult(jobs.Select(ToDto).ToList(), WasReplayed: false);
  93 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:35:20:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `35-35`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 35-35):**
```csharp
  35 |             .Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
```

---

<a id="callable-4"></a>
### Callable 4: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:46:61:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `46-46`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 46-46):**
```csharp
  46 |         var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
```

---

<a id="callable-5"></a>
### Callable 5: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:46:95:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `46-46`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 46-46):**
```csharp
  46 |         var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
```

---

<a id="callable-6"></a>
### Callable 6: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:48:76:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `48-48`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 48-48):**
```csharp
  48 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
```

---

<a id="callable-7"></a>
### Callable 7: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:52:68:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `52-52`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 52-52):**
```csharp
  52 |             ? PrintRules.DetermineKitchenRoutes(order.Items.Select(i => i.PreparationStation))
```

---

<a id="callable-8"></a>
### Callable 8: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:60:34:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `60-65`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 60-65):**
```csharp
  60 |         var jobs = routes.Select(route => new PrintJobEntity
  61 |         {
  62 |             Id = $"printjob-{request.RequestId}-{route}", CreateRequestId = request.RequestId, OrderId = order.Id, OrderNumber = order.OrderNumber,
  63 |             Route = route.ToString(), Status = nameof(PrintJobStatus.Pending), AttemptCount = 0,
  64 |             IsReprint = request.IsReprint, CreatedBy = actorUser.Name, CreatedAtUtc = now,
  65 |         }).ToList();
```

---

<a id="callable-9"></a>
### Callable 9: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:87:67:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `87-87`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 87-87):**
```csharp
  87 |             var replay = await _db.PrintJobs.AsNoTracking().Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
```

---

<a id="callable-10"></a>
### Callable 10: `Falah.Infrastructure.Printing.CreatePrintJobsHandler.ToDto(Falah.Infrastructure.Persistence.Entities.PrintJobEntity)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:95:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `95-96`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Printing.CreatePrintJobsHandler.ToDto(Falah.Infrastructure.Persistence.Entities.PrintJobEntity). Manages internal state transition within src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 95-96):**
```csharp
  95 |     internal static PrintJobDto ToDto(PrintJobEntity j) => new(
  96 |         j.Id, j.OrderId, j.OrderNumber, j.Route, j.Status, j.AttemptCount, j.LastAttemptAtUtc, j.LastError, j.IsReprint, j.CreatedAtUtc);
```

---
