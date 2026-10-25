# Substantive Callable Audit: `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `ac7a5a9aefba464dfb2444c86d6a5bb22149455b2f82856f3fea4971931ff941`
- **Auditor Role:** Printing & Operational Workflow Specialist
- **Total File Callables:** **10** (2 Methods + 1 Static Helper + 7 Inline Expressions)
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

[`CreatePrintJobsHandler`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs) is the central server-side ingestion authority for all physical ticket generation in Fale7 POS. It translates business events (checkout completion, manual reprints, call center submissions) into discrete, route-partitioned [`PrintJobEntity`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/PrintJobEntity.cs) records in the database.

The handler enforces strict architectural decoupling:
1. **Financial Decoupling:** It reads orders, order items, and user accounts using `.AsNoTracking()`. It never writes to orders, payments, shifts, or treasury accounts. Printing is a downstream physical side-effect that cannot mutate financial truth.
2. **Frozen Snapshot Routing:** Kitchen routes are evaluated by passing the order items' `PreparationStation` strings to [`PrintRules.DetermineKitchenRoutes`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs#L24-L30). These values are read directly from the frozen `OrderItemEntity.PreparationStation` recorded at sale time, ensuring that subsequent changes to product catalog station assignments cannot alter the routing of historical orders.
3. **Idempotency Strategy & PRT-001 Vulnerability:** The handler generates deterministic primary keys (`printjob-{request.RequestId}-{route}`) and performs pre-flight checks against `CreateRequestId`. However, the absence of a database-level unique constraint on `(OrderId, Route)` exposes the system to duplicate ticket generation in the kitchen when requests carry distinct `RequestId` values.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `CreatePrintJobsHandler.CreatePrintJobsHandler(FalahDbContext, IAuditLog)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:30:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `30-30`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Initializes the handler with required dependencies:
- `db` ([`FalahDbContext`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs)): Entity Framework Core database context for reading order data and persisting `PrintJobEntity` rows.
- `auditLog` ([`IAuditLog`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Audit/IAuditLog.cs)): Operational audit sink for logging reprint events.
Assigns private fields `_db` and `_auditLog`.

**B. Callers, Dependencies & Side Effects:**  
Injected into [`OrderAutoPrintService`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs#L35), API controllers, and test harnesses. Scoped service lifecycle.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Initializes database access before any print job records can be queued.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Instance is scoped per HTTP request. `FalahDbContext` is not thread-safe and must be confined to single-threaded handler execution.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Instantiated and validated across integration test suite `PrintAgentTests.cs` and `PrintJobQueryServiceTests.cs`.
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Line 30):**
```csharp
    public CreatePrintJobsHandler(FalahDbContext db, IAuditLog auditLog) { _db = db; _auditLog = auditLog; }
```

---

<a id="callable-2"></a>
### Callable 2: `CreatePrintJobsHandler.HandleAsync(CreatePrintJobsRequest, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:32:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `32-93`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`PRT-001`**

**A. Actual Behavior, Inputs & Return Values:**  
Executes print job creation workflow:
1. **Pre-flight Idempotency Check (Lines 34-36):** Queries `_db.PrintJobs.AsNoTracking().Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct)`. If existing jobs are found, immediately returns `CreatePrintJobsResult(..., WasReplayed: true)`.
2. **Customer Route Validation (Lines 38-44):** If `request.CustomerRoute` is non-empty, parses to `PrintRoute` and validates that it is either `Receipt` or `CallCenter`. Throws `DomainException` if invalid.
3. **Entity Verification (Lines 46-49):** Loads order (including items) and actor user via `.AsNoTracking()`. Throws `InvalidOperationException` if either is missing.
4. **Kitchen Route Resolution (Lines 51-53):** If `request.IncludeKitchenRoutes` is true, calls `PrintRules.DetermineKitchenRoutes(...)` over frozen `order.Items.Select(i => i.PreparationStation)`.
5. **Route Aggregation (Lines 55-58):** Merges customer route and kitchen routes. Throws `DomainException("لا توجد محطة طباعة مطلوبة لهذا الطلب.")` if empty.
6. **Job Entity Construction (Lines 60-65):** Maps each route to `PrintJobEntity`:
   - `Id = $"printjob-{request.RequestId}-{route}"`
   - `CreateRequestId = request.RequestId`
   - `OrderId = order.Id`, `OrderNumber = order.OrderNumber`
   - `Route = route.ToString()`, `Status = nameof(PrintJobStatus.Pending)`
   - `AttemptCount = 0`, `IsReprint = request.IsReprint`, `CreatedBy = actorUser.Name`
7. **Persistence & Reprint Auditing (Lines 67-84):** Adds entities to `_db.PrintJobs`. If `request.IsReprint == true`, appends `AuditEvent` to `_auditLog`. Calls `await _db.SaveChangesAsync(ct)`.
8. **Concurrency Conflict Catch (Lines 85-90):** If `DbUpdateException` occurs (primary key collision), re-queries by `request.RequestId`. If found, returns replay result; otherwise throws `PrintingConflictException`.
9. **Return Value:** Returns `CreatePrintJobsResult(jobs.Select(ToDto).ToList(), WasReplayed: false)`.

**B. Callers, Dependencies & Side Effects:**  
Called by [`OrderAutoPrintService.TryPrintAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs#L77), `ReprintPrintJobCommandHandler`, and direct API endpoints. Persists rows in `PrintJobs` table and `AuditLogs` table.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Analysis of PRT-001 (Duplicate Kitchen Print Jobs):**
  - The handler relies exclusively on `j.CreateRequestId == request.RequestId` and primary key `printjob-{request.RequestId}-{route}` for idempotency.
  - However, in `FalahDbContext.cs:365-377`, the `PrintJobs` table has only:
    `b.HasKey(j => j.Id);`
    `b.HasIndex(j => new { j.Route, j.Status });`
    `b.HasIndex(j => j.CreateRequestId);`
  - There is **no composite unique index on `(OrderId, Route)`** for active/first-print jobs.
  - If a cashier experiences a network glitch and clicks Checkout a second time (minting a new `CheckoutRequestId`), or if auto-print runs concurrently with manual operator printing, two distinct `RequestId` values are generated for the same `OrderId`.
  - Both requests succeed in `HandleAsync`, creating duplicate `Pending` jobs for `Charcoal`, `Grill`, or `Receipt`.
  - The physical print agent drains both jobs sequentially, resulting in duplicate food preparation tickets in the kitchen.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Runs in its own transaction context. The catch block on `DbUpdateException` properly handles simultaneous execution with the *same* `RequestId`, but provides zero protection against distinct `RequestId` values targeting the same order.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `PrintJobQueryServiceTests.cs` and `PrintAgentTests.cs`.
- *Missing Tests:* Concurrent order checkout test simulating two distinct requests for the same `OrderId` to expose duplicate kitchen ticket creation.

> **Verbatim Code Excerpt (Lines 32-93):**
```csharp
    public async Task<CreatePrintJobsResult> HandleAsync(CreatePrintJobsRequest request, CancellationToken ct = default)
    {
        var existing = await _db.PrintJobs.AsNoTracking()
            .Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
        if (existing.Count > 0) return new CreatePrintJobsResult(existing.Select(ToDto).ToList(), WasReplayed: true);

        PrintRoute? customerRoute = null;
        if (!string.IsNullOrEmpty(request.CustomerRoute))
        {
            if (!Enum.TryParse<PrintRoute>(request.CustomerRoute, out var parsed) || parsed is not (PrintRoute.Receipt or PrintRoute.CallCenter))
                throw new DomainException("مسار الطباعة المطلوب للعميل غير صالح.");
            customerRoute = parsed;
        }

        var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new InvalidOperationException("الطلب غير موجود.");
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");

        IReadOnlyCollection<PrintRoute> kitchenRoutes = request.IncludeKitchenRoutes
            ? PrintRules.DetermineKitchenRoutes(order.Items.Select(i => i.PreparationStation))
            : Array.Empty<PrintRoute>();

        var routes = new List<PrintRoute>(kitchenRoutes);
        if (customerRoute is { } cr) routes.Insert(0, cr);
        if (routes.Count == 0) throw new DomainException("لا توجد محطة طباعة مطلوبة لهذا الطلب.");

        var now = DateTimeOffset.UtcNow;
        var jobs = routes.Select(route => new PrintJobEntity
        {
            Id = $"printjob-{request.RequestId}-{route}", CreateRequestId = request.RequestId, OrderId = order.Id, OrderNumber = order.OrderNumber,
            Route = route.ToString(), Status = nameof(PrintJobStatus.Pending), AttemptCount = 0,
            IsReprint = request.IsReprint, CreatedBy = actorUser.Name, CreatedAtUtc = now,
        }).ToList();

        try
        {
            _db.PrintJobs.AddRange(jobs);
            // Only a genuine REPRINT is audited here - a routine first print of a fresh order is
            // not a sensitive event (PrintJobEntity/PrintJobAttemptEntity already carry its own full
            // attempt history); reproducing an already-committed order's tickets later is the
            // noteworthy action worth surfacing in the general trail too.
            if (request.IsReprint)
            {
                _auditLog.Append(new AuditEvent(
                    $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                    "إعادة طباعة طلب", "order",
                    $"إعادة طباعة الطلب {order.OrderNumber} - المسارات: {string.Join(", ", routes)}"
                        + (string.IsNullOrWhiteSpace(request.Reason) ? "" : $" - السبب: {request.Reason.Trim()}"),
                    "normal", now, ReferenceId: order.Id, Reason: request.Reason));
            }
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.PrintJobs.AsNoTracking().Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
            if (replay.Count > 0) return new CreatePrintJobsResult(replay.Select(ToDto).ToList(), WasReplayed: true);
            throw new PrintingConflictException("تعذر إنشاء طلبات الطباعة بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }

        return new CreatePrintJobsResult(jobs.Select(ToDto).ToList(), WasReplayed: false);
    }
```

---

<a id="callable-3"></a>
### Callable 3: Lambda `j => j.CreateRequestId == request.RequestId` (Pre-flight)
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:35:20:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `35-35`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Predicate for EF Core LINQ query filtering `PrintJobs` by `CreateRequestId`. Translated to SQL `WHERE [p].[CreateRequestId] = @__request_RequestId_0`.

**B. Callers, Dependencies & Side Effects:**  
Passed to `.Where()` at line 35. Read-only database query.

> **Verbatim Code Excerpt (Line 35):**
```csharp
    .Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
```

---

<a id="callable-4"></a>
### Callable 4: Lambda `o => o.Id == request.OrderId`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:46:83:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `46-46`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:46:61:SimpleLambdaExpression`
- **Exact Line Range:** Lines 46–46
- **Canonical Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:46:95:SimpleLambdaExpression`
- **Exact Line Range:** Lines 46–46
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Predicate for EF Core query finding `OrderEntity` by primary key `Id`. Evaluated with `.Include(o => o.Items)` to eager-load order items.

> **Verbatim Code Excerpt (Line 46):**
```csharp
    var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
```

---

<a id="callable-5"></a>
### Callable 5: Lambda `u => u.Id == request.ActorUserId`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:48:69:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `48-48`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:48:76:SimpleLambdaExpression`
- **Exact Line Range:** Lines 48–48
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Predicate finding the operating cashier or supervisor user record by primary key `Id`.

> **Verbatim Code Excerpt (Line 48):**
```csharp
    var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
```

---

<a id="callable-6"></a>
### Callable 6: Lambda `i => i.PreparationStation`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:52:50:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `52-52`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:52:68:SimpleLambdaExpression`
- **Exact Line Range:** Lines 52–52
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Selects the frozen preparation station string from each `OrderItemEntity`. Enables `PrintRules.DetermineKitchenRoutes` to evaluate station routing from point-of-sale snapshots.

> **Verbatim Code Excerpt (Line 52):**
```csharp
    PrintRules.DetermineKitchenRoutes(order.Items.Select(i => i.PreparationStation))
```

---

<a id="callable-7"></a>
### Callable 7: Lambda `route => new PrintJobEntity { ... }`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:60:29:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `60-65`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:60:34:SimpleLambdaExpression`
- **Exact Line Range:** Lines 60–65
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Projection function creating `PrintJobEntity` instances for each resolved route. Constructs deterministic primary key `printjob-{request.RequestId}-{route}`.

> **Verbatim Code Excerpt (Lines 60-65):**
```csharp
        var jobs = routes.Select(route => new PrintJobEntity
        {
            Id = $"printjob-{request.RequestId}-{route}", CreateRequestId = request.RequestId, OrderId = order.Id, OrderNumber = order.OrderNumber,
            Route = route.ToString(), Status = nameof(PrintJobStatus.Pending), AttemptCount = 0,
            IsReprint = request.IsReprint, CreatedBy = actorUser.Name, CreatedAtUtc = now,
        }).ToList();
```

---

<a id="callable-8"></a>
### Callable 8: Lambda `j => j.CreateRequestId == request.RequestId` (Replay Catch)
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:87:66:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `87-87`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:87:67:SimpleLambdaExpression`
- **Exact Line Range:** Lines 87–87
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Replay query predicate invoked when `DbUpdateException` indicates a unique key violation during `SaveChangesAsync`. Queries existing rows matching `request.RequestId`.

> **Verbatim Code Excerpt (Line 87):**
```csharp
    var replay = await _db.PrintJobs.AsNoTracking().Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
```

---

<a id="callable-9"></a>
### Callable 9: Projection Lambda `existing.Select(ToDto)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:36:51:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `36-36`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Transforms internal `PrintJobEntity` records into public `PrintJobDto` instances via method group `ToDto`.

> **Verbatim Code Excerpt (Line 36):**
```csharp
    if (existing.Count > 0) return new CreatePrintJobsResult(existing.Select(ToDto).ToList(), WasReplayed: true);
```

---

<a id="callable-10"></a>
### Callable 10: `CreatePrintJobsHandler.ToDto(PrintJobEntity)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:95:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `95-96`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Internal static mapping function translating `PrintJobEntity` properties to immutable `PrintJobDto`:
- Passes: `Id`, `OrderId`, `OrderNumber`, `Route`, `Status`, `AttemptCount`, `LastAttemptAtUtc`, `LastError`, `IsReprint`, `CreatedAtUtc`.
- Returns: `PrintJobDto`.

**B. Callers, Dependencies & Side Effects:**  
Called internally by `HandleAsync` and by `ReprintDocumentHandler.HandleAsync` ([`ReprintDocumentHandler.cs:31, 73`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs#L31)). Pure mapping function.

> **Verbatim Code Excerpt (Lines 95-96):**
```csharp
    internal static PrintJobDto ToDto(PrintJobEntity j) => new(
        j.Id, j.OrderId, j.OrderNumber, j.Route, j.Status, j.AttemptCount, j.LastAttemptAtUtc, j.LastError, j.IsReprint, j.CreatedAtUtc);
```

---

## 3. Subsystem Vulnerability & Integrity Analysis

| Finding ID | Vulnerability / Architectural Gap | Root Cause in `CreatePrintJobsHandler.cs` | Severity | Remediation Strategy |
| :--- | :--- | :--- | :--- | :--- |
| **PRT-001** | Duplicate Kitchen Print Jobs | Idempotency relies on `CreateRequestId`. If client generates distinct `RequestId`s for retried checkout or auto-print + manual print, lack of `IX_PrintJobs_OrderId_Route` allows duplicate active `Pending` jobs. | **CRITICAL** | Add unique partial index `IX_PrintJobs_OrderId_Route` in `FalahDbContext` where `IsReprint = false` and `Status = 'Pending'`. |
| **PRT-005** | Missing Transactional Atomicity Between Audit and Print | `_auditLog.Append` adds memory event before `_db.SaveChangesAsync`. If `SaveChangesAsync` fails, audit entry may remain in-memory or desynchronize. | **LOW** | Commit audit log inside the same transactional scope or use outbox. |
