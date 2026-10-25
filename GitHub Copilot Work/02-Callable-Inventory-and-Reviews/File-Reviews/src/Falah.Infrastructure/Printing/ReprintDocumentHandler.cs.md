# Substantive Callable Audit: `src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `6410ccea64cadcf7b37be00f368207ffdda5bc074ddd6b3290e20f1cfa91238f`
- **Auditor Role:** Printing & Operational Workflow Specialist
- **Total File Callables:** **8** (1 Constructor + 1 Method + 6 Inline Lambda Expressions)
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

[`ReprintDocumentHandler`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs) is the operational print reproduction service for non-order business documents in Fale7 POS:
1. **Shift Opening Float Slip (`PrintDocumentKind.ShiftOpen`):** Proves initial float handed to cashier.
2. **Shift Closing Handover Slip (`PrintDocumentKind.ShiftClose`):** Records blind cash count, cash sales, expenses, and difference (surplus/shortage).
3. **Expense Disbursement Voucher (`PrintDocumentKind.Expense`):** Verifies cash withdrawal from till or treasury for vendor payments, supplies, or petty cash.

### Core Invariant: Zero Financial Mutation
The handler creates a print job and nothing else:
- No shift is reopened.
- No closed cash reconciliation is updated.
- No expense is posted or voided.
- No ledger or treasury balances move.

The document must already exist in durable storage. However, reproducing financial cash slips is a high-risk operational event that can facilitate cash drawer fraud or duplicate vendor payouts. Therefore, the handler appends every reprint action to the system-wide audit log (`IAuditLog`), marks the print job with `IsReprint = true`, and routes the job to the cashier printer (`PrintRoute.Receipt`).

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `ReprintDocumentHandler.ReprintDocumentHandler(FalahDbContext, IAuditLog)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs:25:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `25-25`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Constructs the document reprint handler with its database context and audit logger:
- `db` ([`FalahDbContext`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs)): Entity Framework context used to verify document existence (`Shifts`, `Expenses`) and persist reprint `PrintJobEntity` rows.
- `auditLog` ([`IAuditLog`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Audit/IAuditLog.cs)): Audit trail sink.
Assigns private fields `_db` and `_auditLog`.

**B. Callers, Dependencies & Side Effects:**  
Injected into server API endpoints (e.g. `POST /api/printing/reprint-document`). Scoped lifecycle.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Initializes state before print job creation.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Thread-safety governed by scoped single-thread execution per HTTP request.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in integration test fixtures `ReprintDocumentTests.cs`.
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Line 25):**
```csharp
    public ReprintDocumentHandler(FalahDbContext db, IAuditLog auditLog) { _db = db; _auditLog = auditLog; }
```

---

<a id="callable-2"></a>
### Callable 2: `ReprintDocumentHandler.HandleAsync(ReprintDocumentRequest, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs:27:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `27-74`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`PRT-003` / `SEC-004`**

**A. Actual Behavior, Inputs & Return Values:**  
Processes a document reprint request:
1. **Idempotency Replay Lookup (Lines 29-31):** Checks if jobs with `CreateRequestId == request.RequestId` already exist. If found, returns `CreatePrintJobsResult(..., WasReplayed: true)`.
2. **Document Kind Validation (Lines 33-35):** Parses `request.DocumentKind`. If parsing fails or kind is `PrintDocumentKind.Order`, throws `DomainException("نوع المستند المطلوب إعادة طباعته غير صالح.")`. (Orders must be reprinted through `CreatePrintJobsHandler`).
3. **Actor User Existence Query (Lines 37-38):** Retrieves user from `_db.Users.AsNoTracking()`. Throws `InvalidOperationException("المستخدم غير موجود.")` if missing.
4. **Document Existence & State Verification (Lines 40-49):**
   - `ShiftOpen`: Verifies `_db.Shifts.AnyAsync(s => s.Id == request.DocumentId)`.
   - `ShiftClose`: Verifies `_db.Shifts.AnyAsync(s => s.Id == request.DocumentId && s.ClosedAtUtc != null)`. Prevents printing a handover slip for an active, unclosed shift.
   - `Expense`: Verifies `_db.Expenses.AnyAsync(e => e.Id == request.DocumentId)`.
   - If document does not exist, throws `InvalidOperationException("المستند المطلوب إعادة طباعته غير موجود.")`.
5. **Job Entity Construction (Lines 51-53):** Calls [`DocumentPrintJobs.Build`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/DocumentPrintJobs.cs#L22-L42), setting `Route = nameof(PrintRoute.Receipt)`, `IsReprint = true`, and `reprintRequestId = request.RequestId`.
6. **Audit Trail Recording & Persistence (Lines 55-63):**
   - Adds job to `_db.PrintJobs`.
   - Appends `AuditEvent`: Action `$"إعادة طباعة {label}"`, Area `kind == Expense ? "expense" : "shift"`, Actor ID/Name/Role, reference ID, and reason.
   - Awaits `_db.SaveChangesAsync(ct)`.
7. **Concurrency Conflict Catch (Lines 65-71):** Catches `DbUpdateException`. Queries by `request.RequestId` and returns replay if found; otherwise throws `PrintingConflictException`.
8. **Return Value:** Returns `CreatePrintJobsResult` with single job DTO and `WasReplayed: false`.

**B. Callers, Dependencies & Side Effects:**  
Called by API endpoint `POST /api/printing/reprint-document`. Side effects: inserts record in `PrintJobs` table and `AuditLogs` table.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Hardware Route Assignment:** All document slips (`ShiftOpen`, `ShiftClose`, `Expense`) route strictly to `PrintRoute.Receipt` (the cashier till printer). They are never routed to kitchen stations or call center printers.
- **Physical Header Banner:** Because `IsReprint` is true, when the print agent fetches the job, [`PrintJobQueryService.GetTicketAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/PrintJobQueryService.cs#L107-L113) calls [`PrintRules.MarkAsReprint`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs#L484), injecting `"إعادة طباعة"` and the reprint timestamp at the top of the slip.

**D. Concurrency, Security & Integrity Vulnerabilities (PRT-003 / SEC-004):**
- **Missing Role/Manager Authorization:** In lines 37-38, the handler verifies only that `request.ActorUserId` corresponds to an existing user. It does **NOT** check `actor.Role` (e.g. `Manager`, `Admin`) nor does it require supervisor PIN elevation! Any cashier can trigger unlimited reprints of closed shift handover slips and expense vouchers.
- **Missing Mandatory Reason Validation:** Line 61 allows `request.Reason` to be null or whitespace. For sensitive financial vouchers, omitting a mandatory reason weakens non-repudiation in financial audit disputes.
- **Absence of Reprint Counter Tracking:** Neither `PrintJobEntity` nor `DocumentPrintJobs.Build` tracks how many times a given document has been reprinted. The ticket prints `"إعادة طباعة"` but lacks an ordinal counter (e.g., "نسخة رقم 2").

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `ReprintDocumentTests.cs` (verifying replay, shift close existence check, and expense existence).
- *Missing Tests:* Test verifying that unauthorized roles (e.g. untrusted staff) cannot execute reprints without supervisor elevation.

> **Verbatim Code Excerpt (Lines 27-74):**
```csharp
    public async Task<CreatePrintJobsResult> HandleAsync(ReprintDocumentRequest request, CancellationToken ct = default)
    {
        var existing = await _db.PrintJobs.AsNoTracking().Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
        if (existing.Count > 0)
            return new CreatePrintJobsResult(existing.Select(CreatePrintJobsHandler.ToDto).ToList(), WasReplayed: true);

        if (!Enum.TryParse<PrintDocumentKind>(request.DocumentKind, ignoreCase: true, out var kind)
            || kind == PrintDocumentKind.Order)
            throw new DomainException("نوع المستند المطلوب إعادة طباعته غير صالح.");

        var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");

        var (label, exists) = kind switch
        {
            PrintDocumentKind.ShiftOpen => (PrintRules.ShiftOpenLabel,
                await _db.Shifts.AsNoTracking().AnyAsync(s => s.Id == request.DocumentId, ct)),
            PrintDocumentKind.ShiftClose => (PrintRules.ShiftCloseLabel,
                await _db.Shifts.AsNoTracking().AnyAsync(s => s.Id == request.DocumentId && s.ClosedAtUtc != null, ct)),
            _ => (PrintRules.ExpenseLabel,
                await _db.Expenses.AsNoTracking().AnyAsync(e => e.Id == request.DocumentId, ct)),
        };
        if (!exists) throw new InvalidOperationException("المستند المطلوب إعادة طباعته غير موجود.");

        var now = DateTimeOffset.UtcNow;
        var job = DocumentPrintJobs.Build(kind, request.DocumentId, label, actor.Name, now, reprintRequestId: request.RequestId);

        try
        {
            _db.PrintJobs.Add(job);
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actor.Id, actor.Name, actor.Role,
                $"إعادة طباعة {label}", kind == PrintDocumentKind.Expense ? "expense" : "shift",
                $"إعادة طباعة {label} ({request.DocumentId})"
                    + (string.IsNullOrWhiteSpace(request.Reason) ? "" : $" - السبب: {request.Reason.Trim()}"),
                "normal", now, ReferenceId: request.DocumentId, Reason: request.Reason));
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.PrintJobs.AsNoTracking().Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
            if (replay.Count > 0)
                return new CreatePrintJobsResult(replay.Select(CreatePrintJobsHandler.ToDto).ToList(), WasReplayed: true);
            throw new PrintingConflictException("تعذر إنشاء أمر إعادة الطباعة بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }

        return new CreatePrintJobsResult(new[] { CreatePrintJobsHandler.ToDto(job) }, WasReplayed: false);
    }
```

---

<a id="callable-3"></a>
### Callable 3: Lambda `j => j.CreateRequestId == request.RequestId` (Pre-flight Replay)
- **Callable ID:** `src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs:29:66:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `29-29`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Predicate for EF Core LINQ query filtering `PrintJobs` table by `CreateRequestId`. Translated to SQL `WHERE [p].[CreateRequestId] = @__request_RequestId_0`.

> **Verbatim Code Excerpt (Line 29):**
```csharp
    var existing = await _db.PrintJobs.AsNoTracking().Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
```

---

<a id="callable-4"></a>
### Callable 4: Lambda `u => u.Id == request.ActorUserId`
- **Callable ID:** `src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs:37:65:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `37-37`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Predicate finding the operating user in `_db.Users` by primary key `Id`.

> **Verbatim Code Excerpt (Line 37):**
```csharp
    var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
```

---

<a id="callable-5"></a>
### Callable 5: Lambda `s => s.Id == request.DocumentId` (ShiftOpen)
- **Callable ID:** `src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs:43:45:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `43-43`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Predicate checking existence of a shift record matching `request.DocumentId`. Translated to `SELECT 1 FROM [Shifts] WHERE [s].[Id] = @__request_DocumentId_0`.

> **Verbatim Code Excerpt (Line 43):**
```csharp
    await _db.Shifts.AsNoTracking().AnyAsync(s => s.Id == request.DocumentId, ct)
```

---

<a id="callable-6"></a>
### Callable 6: Lambda `s => s.Id == request.DocumentId && s.ClosedAtUtc != null` (ShiftClose)
- **Callable ID:** `src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs:45:45:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `45-45`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Predicate checking existence of a shift that is strictly in `Closed` status (`ClosedAtUtc != null`). Prevents premature printing of blind cash close slips while a till is still operating.

> **Verbatim Code Excerpt (Line 45):**
```csharp
    await _db.Shifts.AsNoTracking().AnyAsync(s => s.Id == request.DocumentId && s.ClosedAtUtc != null, ct)
```

---

<a id="callable-7"></a>
### Callable 7: Lambda `e => e.Id == request.DocumentId` (Expense)
- **Callable ID:** `src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs:47:47:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `47-47`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Predicate checking existence of an expense record in `_db.Expenses`.

> **Verbatim Code Excerpt (Line 47):**
```csharp
    await _db.Expenses.AsNoTracking().AnyAsync(e => e.Id == request.DocumentId, ct)
```

---

<a id="callable-8"></a>
### Callable 8: Lambda `j => j.CreateRequestId == request.RequestId` (Replay Catch)
- **Callable ID:** `src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs:67:70:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `67-67`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Secondary lookup predicate executed if `DbUpdateException` is caught during `SaveChangesAsync`, confirming whether another concurrent thread succeeded with the same `RequestId`.

> **Verbatim Code Excerpt (Line 67):**
```csharp
    var replay = await _db.PrintJobs.AsNoTracking().Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
```

---

## 3. Subsystem Vulnerability & Integrity Analysis

| Finding ID | Vulnerability / Security Gap | Root Cause in `ReprintDocumentHandler.cs` | Severity | Remediation Strategy |
| :--- | :--- | :--- | :--- | :--- |
| **PRT-003** | Missing Role Authorization for Financial Reprints | Lines 37-38 check user existence without validating `actor.Role` or requiring supervisor authorization. Any till operator can reprint closed shift reconciliation slips and expense vouchers. | **HIGH** | Require `Permissions.CanReprintSlips` and demand supervisor PIN authentication for closing slips and expenses. |
| **SEC-004** | Optional Reprint Reason & Missing Ordinal Tracking | `request.Reason` can be empty; no reprint counter is stored or printed on paper (e.g. "Reprint #2"). Duplicate physical vouchers can circulate without visual tracking. | **MEDIUM** | Enforce non-empty `Reason` validation and track `ReprintCount` on document entities to print on paper. |
