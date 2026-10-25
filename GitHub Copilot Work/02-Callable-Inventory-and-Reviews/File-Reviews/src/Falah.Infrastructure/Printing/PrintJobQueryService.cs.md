# Substantive Callable Audit: `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs`

- **Source File:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `be7e9b0dab28dd16af51b952bd4ed000875ca04d6a9b61fa357dc55a0f2e304c`
- **Auditor Role:** Printing & Operational Workflow Specialist
- **Total File Callables:** **18**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

[`PrintJobQueryService`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/PrintJobQueryService.cs) is the central query engine and ticket composition coordinator for the Fale7 POS printing subsystem. It serves two distinct architectural roles:

1. **Hardware Agent Polling & Operational Overview:**
   It provides the read-only projection over `PrintJobEntity` and `PrintJobAttemptEntity`. Station printing daemons poll this service (`ListPendingJobsForRouteAsync`) to drain their route queues, while managerial dashboards query route health, backlog queues, and stuck jobs (`GetOverviewAsync`).

2. **On-Demand Ticket Content Composition:**
   In accordance with the POS immutable historical snapshot architecture, print jobs do not persist serialized ESC/POS byte streams or raster bitmaps. Instead, [`ComposeTicketContentAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/PrintJobQueryService.cs#L99-L140) recomposes the ticket on demand from the order's frozen item pricing, modifier notes, and preparation station snapshots ([`OrderItemEntity.PreparationStation`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/OrderEntities.cs)). This guarantees that menu changes (such as reassigning a shawarma item from Charcoal to Grill) never retroactively alter how historical tickets reprint.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `PrintJobQueryService.PrintJobQueryService(FalahDbContext, SettingsQueryService)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:25:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `25-29`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Initializes the query service with scoped dependencies. Accepts `FalahDbContext db` for database access and `SettingsQueryService settings` for reading dynamic store metadata (restaurant brand name, tagline, phones, footer, and menu QR link). Returns a newly instantiated `PrintJobQueryService`.

**B. Callers, Dependencies & Side Effects:**  
Registered as scoped in ASP.NET Core DI ([`Program.cs:152`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L152)). Injected into print job endpoints (`/api/print-jobs/*`) and [`DailyStatementQueryService`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/DailyStatementQueryService.cs#L41). Has no side effects; stores references in private readonly fields.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Operates at the server application boundary. Has no direct socket or winspool.drv bindings.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Lifetime is scoped to HTTP request. `FalahDbContext` is not thread-safe; caller endpoints must not invoke query methods concurrently on the same service instance.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Instantiated implicitly in integration tests via test WebApplicationFactory (`PrintingVerticalSliceTests.cs`).
- *Missing Tests:* None needed for pure constructor assignment.

> **Verbatim Code Excerpt (Lines 25-29):**
```csharp
    public PrintJobQueryService(FalahDbContext db, SettingsQueryService settings)
    {
        _db = db;
        _settings = settings;
    }
```

---

<a id="callable-2"></a>
### Callable 2: `PrintJobQueryService.ListJobsForOrderAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:31:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `31-33`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Executes an asynchronous non-tracking LINQ query against `_db.PrintJobs` filtering by `OrderId == orderId`, ordered chronologically by `CreatedAtUtc` ascending. Projects results to `List<PrintJobDto>` using [`CreatePrintJobsHandler.ToDto`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs#L95-L97). Returns the complete list of dispatch jobs for that order.

**B. Callers, Dependencies & Side Effects:**  
Invoked by endpoint `GET /api/orders/{id}/print-jobs` ([`Program.cs:2112`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L2112)). Read-only query; no state mutations or database locks.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Used by cashier counter UI and Order Lookup dialog to display which physical stations received tickets, their status (`Pending`, `Succeeded`, `Failed`), attempt counts, and last error messages reported by station hardware agents.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Uses `AsNoTracking()`. If multiple clients trigger prints concurrently (exposing defect **PRT-001**), this method reflects all generated duplicate rows.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in [`PrintingVerticalSliceTests.cs:709-715`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintingVerticalSliceTests.cs#L709-L715) verifying jobs generated by checkout.
- *Missing Tests:* Verification that passing a non-existent `orderId` safely returns an empty list without throwing.

> **Verbatim Code Excerpt (Lines 31-33):**
```csharp
    public async Task<List<PrintJobDto>> ListJobsForOrderAsync(string orderId, CancellationToken ct = default) =>
        (await _db.PrintJobs.AsNoTracking().Where(j => j.OrderId == orderId).OrderBy(j => j.CreatedAtUtc).ToListAsync(ct))
            .Select(CreatePrintJobsHandler.ToDto).ToList();
```

---

<a id="callable-3"></a>
### Callable 3: `PrintJobQueryService.ListPendingJobsForRouteAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:37:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `37-41`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Findings:** **`PRT-001`**, **`PRT-002`**

**A. Actual Behavior, Inputs & Return Values:**  
Queries `_db.PrintJobs` for jobs where `Route == route` and `Status == "Pending"`, ordered by `CreatedAtUtc` ascending (FIFO). Returns `List<PrintJobDto>` representing the pending print backlog for a specific station printer.

**B. Callers, Dependencies & Side Effects:**  
Invoked by endpoint `GET /api/print-jobs/pending?route={route}` ([`Program.cs:2138`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L2138)). Polled continuously by [`PrintServerClient.GetPendingAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintServerClient.cs#L42-L47) inside [`PrintJobProcessor.DrainAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs#L68-L79).

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **FIFO Ordering Invariant:** Ordering by `CreatedAtUtc` ensures physical paper emerges in strict order of customer checkout.
- **Starvation Flaw on Hardware Errors:** The query strictly filters `Status == nameof(PrintJobStatus.Pending)`. When a hardware error occurs (e.g., serial cable disconnected, network port 9100 timeout), [`ReportPrintJobResultHandler`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/ReportPrintJobResultHandler.cs#L47) transitions the job to `Failed`. Once marked `Failed`, the job is **permanently excluded** from this query. Station agents will never automatically retry it; it remains unprinted until manual operator reprint.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
- **PRT-001 Duplication Pipeline:** Because `PrintJobs` lacks a composite unique constraint on `(OrderId, Route)`, concurrent checkouts or manual client prints insert multiple `Pending` records for the same route and order. This method returns both records, and the print agent renders and cuts both tickets.
- **Unbounded Result Set / Memory Risk:** Lacks pagination (`Take(N)`). If a station printer is powered down during a busy weekend and 2,000 orders accumulate, the first poll after restart retrieves all 2,000 entities into server memory simultaneously.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* [`PrintAgentTests.Only_this_stations_route_is_drained`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintAgentTests.cs#L151-L163) and [`PrintAgentTests.A_backlog_is_drained_oldest_first_in_queue_order`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintAgentTests.cs#L165-L176).
- *Missing Tests:* High-volume backlog load test (simulating 1,000 pending items), and regression test proving that `Failed` jobs are never re-polled without manual intervention.

> **Verbatim Code Excerpt (Lines 37-41):**
```csharp
    public async Task<List<PrintJobDto>> ListPendingJobsForRouteAsync(string route, CancellationToken ct = default) =>
        (await _db.PrintJobs.AsNoTracking()
            .Where(j => j.Route == route && j.Status == nameof(PrintJobStatus.Pending))
            .OrderBy(j => j.CreatedAtUtc).ToListAsync(ct))
            .Select(CreatePrintJobsHandler.ToDto).ToList();
```

---

<a id="callable-4"></a>
### Callable 4: `PrintJobQueryService.GetOverviewAsync(CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:49:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `49-86`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Generates a comprehensive snapshot of system-wide printing operations over the preceding 48 hours. Compiles:
1. Per-route statistics for all 6 routes (`Receipt`, `CallCenter`, `Charcoal`, `Grill`, `DriverDispatch`, `DeliveryOrder`): pending count, failed count, stuck jobs count (pending > 5 minutes via `StuckAfter`), oldest pending job timestamp, and last successful print timestamp.
2. An "attention" queue of up to 60 jobs requiring operator action (`Failed` jobs first, then oldest stuck `Pending` jobs).
3. System-wide summary totals. Returns `PrintingOverviewDto`.

**B. Callers, Dependencies & Side Effects:**  
Invoked by `GET /api/print-jobs/overview` ([`Program.cs:2135`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L2135)) and [`DailyStatementQueryService.GetDailyStatementAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/DailyStatementQueryService.cs#L242). Read-only; no side effects.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
`StuckAfter = TimeSpan.FromMinutes(5)` detects when a physical station agent has crashed, lost power, or is blocked on a stalled Windows print spooler.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
- **In-Memory Materialization Bottleneck:** Line 53 queries `_db.PrintJobs.Where(...)` and immediately executes `.ToListAsync(ct)`. The route-level grouping and filtering are executed entirely in C# memory. In high-volume multi-station environments, loading 48 hours of print job history into RAM on every dashboard refresh introduces unnecessary memory allocations.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Exercised in daily statement reports tests.
- *Missing Tests:* Verification of correct ordering in the `attention` list (verifying `Failed` precedes `Pending`), and edge cases where a route has 0 jobs (verifying null timestamps).

> **Verbatim Code Excerpt (Lines 49-86):**
```csharp
    public async Task<PrintingOverviewDto> GetOverviewAsync(CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-2);
        var jobs = await _db.PrintJobs.AsNoTracking()
            .Where(j => j.Status != nameof(PrintJobStatus.Succeeded) || j.LastAttemptAtUtc >= since)
            .ToListAsync(ct);

        var stuckBefore = DateTimeOffset.UtcNow - StuckAfter;
        var routes = new List<PrintRouteStatusDto>();
        foreach (var route in new[] { "Receipt", "CallCenter", "Charcoal", "Grill", nameof(PrintRoute.DriverDispatch), nameof(PrintRoute.DeliveryOrder) })
        {
            var forRoute = jobs.Where(j => j.Route == route).ToList();
            var pending = forRoute.Where(j => j.Status == nameof(PrintJobStatus.Pending)).ToList();
            routes.Add(new PrintRouteStatusDto(
                route,
                pending.Count,
                forRoute.Count(j => j.Status == nameof(PrintJobStatus.Failed)),
                pending.Count(j => j.CreatedAtUtc <= stuckBefore),
                pending.Count == 0 ? null : pending.Min(j => j.CreatedAtUtc),
                forRoute.Where(j => j.Status == nameof(PrintJobStatus.Succeeded))
                    .Select(j => j.LastAttemptAtUtc).DefaultIfEmpty(null).Max()));
        }

        var attention = jobs
            .Where(j => j.Status == nameof(PrintJobStatus.Failed)
                        || (j.Status == nameof(PrintJobStatus.Pending) && j.CreatedAtUtc <= stuckBefore))
            .OrderBy(j => j.Status == nameof(PrintJobStatus.Failed) ? 0 : 1)
            .ThenBy(j => j.CreatedAtUtc)
            .Take(60)
            .Select(CreatePrintJobsHandler.ToDto)
            .ToList();

        return new PrintingOverviewDto(
            routes, attention,
            routes.Sum(r => r.PendingCount), routes.Sum(r => r.FailedCount), routes.Sum(r => r.StuckCount));
    }
```

---

<a id="callable-5"></a>
### Callable 5: `PrintJobQueryService.GetJobDetailsAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:88:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `88-97`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Fetches `PrintJobEntity` by `jobId` and its associated audit trail of `PrintJobAttemptEntity` records ordered by `AttemptedAtUtc` ascending. Returns `PrintJobDetailsDto` containing both the current job snapshot and complete execution attempt history. Throws `InvalidOperationException("طلب الطباعة غير موجود.")` if not found.

**B. Callers, Dependencies & Side Effects:**  
Invoked by `GET /api/print-jobs/{id}` ([`Program.cs:2141`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L2141)). Read-only.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Surfaces unmanaged spooler failures and hardware errors reported by `PrintJobProcessor` (e.g., `"Win32Exception: The printer name is invalid"`, `"SocketException: Connection refused"`).

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Reads `AsNoTracking()`. Safe under concurrent attempt logging.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Tested in `PrintingVerticalSliceTests.cs`.
- *Missing Tests:* Verification of exception throwing when `jobId` does not exist.

> **Verbatim Code Excerpt (Lines 88-97):**
```csharp
    public async Task<PrintJobDetailsDto> GetJobDetailsAsync(string jobId, CancellationToken ct = default)
    {
        var job = await _db.PrintJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new InvalidOperationException("طلب الطباعة غير موجود.");
        var attempts = await _db.PrintJobAttempts.AsNoTracking().Where(a => a.PrintJobId == jobId)
            .OrderBy(a => a.AttemptedAtUtc)
            .Select(a => new PrintJobAttemptDto(a.AttemptedAtUtc, a.Succeeded, a.Error, a.ReportedBy))
            .ToListAsync(ct);
        return new PrintJobDetailsDto(CreatePrintJobsHandler.ToDto(job), attempts);
    }
```

---

<a id="callable-6"></a>
### Callable 6: `PrintJobQueryService.ComposeTicketContentAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:99:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `99-140`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`

**A. Actual Behavior, Inputs & Return Values:**  
Core ticket dispatch orchestrator. Loads `PrintJobEntity`. If `job.DocumentKind` is non-order (ShiftOpen, ShiftClose, Expense), delegates to slip composers. For orders, loads `OrderEntity` with its `Items` collection and dispatches to route-specific composers:
- `Charcoal` / `Grill`: [`PrintRules.ComposeKitchenTicket`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs#L36-L76)
- `Receipt` / `CallCenter`: `ComposeReceiptAsync`
- `DeliveryOrder`: `ComposeDeliveryOrderAsync`
- `DriverDispatch`: `ComposeDriverDispatchAsync`
Passes result through `MarkIfReprint` and transforms to `ComposedTicketDto`. Returns `ComposedTicketDto`.

**B. Callers, Dependencies & Side Effects:**  
Invoked by `GET /api/print-jobs/{id}/ticket` ([`Program.cs:2147`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L2147)), which is called by station agents via [`PrintServerClient.GetTicketAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintServerClient.cs#L49-L54).

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Historical Snapshot Invariant:** Builds `TicketItem` records exclusively from `order.Items` snapshot fields (`UnitPricePiastres`, `TotalPricePiastres`, `Notes`, `PreparationStation`). Subsequent menu edits or price rule modifications cannot affect historical ticket accuracy.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Read-only query across multiple tables (`PrintJobs`, `Orders`, `OrderItems`, `Users`, `Settings`). If settings (e.g. brand name, phone) change between order checkout and station printing, the ticket reflects the newly configured header text while retaining the frozen item and financial data.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Heavily tested in [`PrintingVerticalSliceTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintingVerticalSliceTests.cs) and [`SlipPrintingAndReprintTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/SlipPrintingAndReprintTests.cs).
- *Missing Tests:* Malformed `DocumentKind` strings or corrupted JSON payloads.

> **Verbatim Code Excerpt (Lines 99-140):**
```csharp
    public async Task<ComposedTicketDto> ComposeTicketContentAsync(string jobId, CancellationToken ct = default)
    {
        var job = await _db.PrintJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new InvalidOperationException("طلب الطباعة غير موجود.");
        var route = Enum.Parse<PrintRoute>(job.Route);

        if (job.DocumentKind is { } kindName && kindName != nameof(PrintDocumentKind.Order))
        {
            var slip = Enum.Parse<PrintDocumentKind>(kindName) switch
            {
                PrintDocumentKind.ShiftOpen => await ComposeShiftOpenSlipAsync(job, ct),
                PrintDocumentKind.ShiftClose => await ComposeShiftCloseSlipAsync(job, ct),
                PrintDocumentKind.Expense => await ComposeExpenseSlipAsync(job, ct),
                _ => throw new InvalidOperationException($"نوع مستند طباعة غير معروف: {kindName}"),
            };
            return ToDto(MarkIfReprint(slip, job));
        }

        var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == job.OrderId, ct)
            ?? throw new InvalidOperationException("الطلب المرتبط بطلب الطباعة غير موجود.");
        var cashier = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == order.CashierId, ct);

        var ticketItems = order.Items.Select(i => new TicketItem(
            i.ProductName, i.VariantName, i.Quantity, Money.FromPiastres(i.UnitPricePiastres), Money.FromPiastres(i.TotalPricePiastres),
            i.Notes, i.PriceRuleName, i.ListUnitPricePiastres > 0 ? Money.FromPiastres(i.ListUnitPricePiastres) : null,
            i.PreparationStation)).ToList();

        var orderType = OrderEnumMapping.ParseOrderType(order.OrderType);
        var composed = route switch
        {
            PrintRoute.Charcoal or PrintRoute.Grill =>
                PrintRules.ComposeKitchenTicket(route, (await _settings.GetAsync(ct)).RestaurantName, order.Id, order.OrderNumber,
                    orderType, ticketItems, order.CreatedAtUtc, order.RequestedPickupAtUtc),
            PrintRoute.Receipt or PrintRoute.CallCenter => await ComposeReceiptAsync(route, order, orderType, cashier?.Name ?? order.CashierId, ticketItems, ct),
            PrintRoute.DeliveryOrder => await ComposeDeliveryOrderAsync(order, orderType, cashier?.Name ?? order.CashierId, ticketItems, ct),
            PrintRoute.DriverDispatch => await ComposeDriverDispatchAsync(job, order, ct),
            _ => throw new InvalidOperationException($"مسار طباعة غير معروف: {route}"),
        };

        return ToDto(MarkIfReprint(composed, job));
    }
```

---

<a id="callable-7"></a>
### Callable 7: `PrintJobQueryService.ToDto(ComposedTicket)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:142:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `142-144`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Pure transformation mapping domain record `ComposedTicket` to wire DTO `ComposedTicketDto`. Maps lines through [`TicketElementMapping.ToDto`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Printing/TicketElementMapping.cs#L23-L36).

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeTicketContentAsync`. No side effects.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Ensures polymorphic ticket elements (`TicketText`, `TicketRow`, `TicketRule`, `TicketCut`, `TicketQr`) are serialized into flat JSON records with `kind` discriminators so station agents running .NET can deserialize without polymorphic type registry configuration.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Pure function; completely thread-safe.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Tested in [`PrintAgentTests.Every_element_kind_survives_a_round_trip_through_the_wire_contract`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintAgentTests.cs#L276-L317).
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Lines 142-144):**
```csharp
    private static ComposedTicketDto ToDto(ComposedTicket composed) =>
        new(composed.Route.ToString(), composed.OrderId, composed.OrderNumber,
            composed.Lines.Select(TicketElementMapping.ToDto).ToList());
```

---

<a id="callable-8"></a>
### Callable 8: `PrintJobQueryService.MarkIfReprint(ComposedTicket, PrintJobEntity)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:149:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `149-150`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Checks `job.IsReprint`. If true, delegates to [`PrintRules.MarkAsReprint`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs) to inject a prominent `"إعادة طباعة"` header, reprint timestamp, and operator name onto the paper layout. If false, returns ticket unmodified.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeTicketContentAsync`.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Anti-Fraud Invariant:** Critical physical security control. Prevents cashiers or kitchen staff from using reprinted slips to falsely claim orders were cancelled or unpaid, or presenting duplicate receipts to customers.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Evaluates immutable `PrintJobEntity.IsReprint` boolean set at creation.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* [`SlipPrintingAndReprintTests.A_reprinted_slip_says_so_on_paper_and_in_the_audit_trail`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/SlipPrintingAndReprintTests.cs).
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Lines 149-150):**
```csharp
    private static ComposedTicket MarkIfReprint(ComposedTicket composed, PrintJobEntity job) =>
        job.IsReprint ? PrintRules.MarkAsReprint(composed, job.CreatedAtUtc, job.CreatedBy) : composed;
```

---

<a id="callable-9"></a>
### Callable 9: `PrintJobQueryService.LoadShiftIdentityAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:154:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `154-162`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Fetches `ShiftEntity` by `shiftId`, retrieves opening user (`Users` table) to obtain cashier name, fetches restaurant settings, and constructs a `ShiftSlipIdentity` containing the 6-character short reference code (`ShiftReference`), shift name, cashier name, and opening UTC timestamp. Returns `(ShiftEntity Shift, ShiftSlipIdentity Identity)`. Throws `InvalidOperationException("الوردية غير موجودة.")` if shift not found.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeShiftOpenSlipAsync` and `ComposeShiftCloseSlipAsync`. Read-only.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Formats shift identification headers for thermal slips so physical paper matches digital shift logs.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Reads `AsNoTracking()`.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `SlipPrintingAndReprintTests.cs`.
- *Missing Tests:* Shift where opening user was deleted or disabled.

> **Verbatim Code Excerpt (Lines 154-162):**
```csharp
    private async Task<(ShiftEntity Shift, ShiftSlipIdentity Identity)> LoadShiftIdentityAsync(string shiftId, CancellationToken ct)
    {
        var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shiftId, ct)
            ?? throw new InvalidOperationException("الوردية غير موجودة.");
        var cashier = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == shift.OpenedByUserId, ct);
        var settings = await _settings.GetAsync(ct);
        return (shift, new ShiftSlipIdentity(settings.RestaurantName, settings.SubName, ShiftReference(shift.Id),
            shift.ShiftName, cashier?.Name ?? shift.OpenedByUserId, shift.OpenedAtUtc));
    }
```

---

<a id="callable-10"></a>
### Callable 10: `PrintJobQueryService.ShiftReference(string)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:166:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `166-167`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Extracts the last 6 characters of the shift identifier in uppercase (`shiftId[^6..].ToUpperInvariant()`). If length $\le 6$, returns the full string uppercase. Returns `string`.

**B. Callers, Dependencies & Side Effects:**  
Internal static helper called by `LoadShiftIdentityAsync`. Pure function.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Prevents printing full 32-character GUIDs (`shift-3f8a92...`) onto narrow 80mm thermal receipts where space is constrained and long alphanumeric hashes confuse cashiers during handover.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Thread-safe pure string operation.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Verified in slip printing tests.
- *Missing Tests:* Null or empty string input handling.

> **Verbatim Code Excerpt (Lines 166-167):**
```csharp
    internal static string ShiftReference(string shiftId) =>
        shiftId.Length <= 6 ? shiftId.ToUpperInvariant() : shiftId[^6..].ToUpperInvariant();
```

---

<a id="callable-11"></a>
### Callable 11: `PrintJobQueryService.ComposeShiftOpenSlipAsync(PrintJobEntity, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:169:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `169-173`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Loads shift identity via `LoadShiftIdentityAsync(job.OrderId, ct)`. Delegates to [`PrintRules.ComposeShiftOpenTicket`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs) passing opening cash amount converted from piastres. Returns `ComposedTicket`.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeTicketContentAsync` when `job.DocumentKind == "ShiftOpen"`.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Prints the drawer baseline slip at shift opening.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Read-only snapshot generation.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* [`SlipPrintingAndReprintTests.Opening_a_shift_queues_its_slip_and_closing_it_queues_the_handover`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/SlipPrintingAndReprintTests.cs#L45-L68).
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Lines 169-173):**
```csharp
    private async Task<ComposedTicket> ComposeShiftOpenSlipAsync(PrintJobEntity job, CancellationToken ct)
    {
        var (shift, identity) = await LoadShiftIdentityAsync(job.OrderId, ct);
        return PrintRules.ComposeShiftOpenTicket(identity, Money.FromPiastres(shift.OpeningCashPiastres));
    }
```

---

<a id="callable-12"></a>
### Callable 12: `PrintJobQueryService.ComposeShiftCloseSlipAsync(PrintJobEntity, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:181:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `181-204`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Findings:** **`FIN-004`**

**A. Actual Behavior, Inputs & Return Values:**  
Loads shift identity and invokes [`ShiftReconciliation.ComputeBreakdownAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs). Overwrites `ExpectedCash`, `ActualCash`, and `Difference` with the frozen entity fields (`shift.ExpectedCashPiastres`, `shift.ActualCashPiastres`, `shift.CashDifferencePiastres`). Groups paid-from-drawer expenses by category, gathers operational warnings via `LoadCloseWarningsAsync`, and delegates to [`PrintRules.ComposeShiftCloseTicket`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs). Returns `ComposedTicket`.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeTicketContentAsync` when `job.DocumentKind == "ShiftClose"`.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Financial Reconciliation Invariant:** Core handover document. Frozen totals (`ExpectedCash`, `ActualCash`, `Difference`) are read strictly from persistence to prevent late-arriving ledger movements from altering historical close figures.
- **FIN-004 Corroboration:** While the macro numbers are frozen from the shift entity, the underlying explanatory breakdown lines (`CashSales`, `Refunds`, `Expenses`) are recomputed dynamically via `ComputeBreakdownAsync`. If an order associated with a closed shift is refunded during a later shift, the recomputed explanatory breakdown numbers will diverge from the frozen total on reprints.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Queries multiple tables (`Shifts`, `Expenses`, `Orders`, `DriverCustodies`).

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `SlipPrintingAndReprintTests.cs`.
- *Missing Tests:* Reprinting a shift close ticket after a post-close refund has occurred to detect mathematical divergence between frozen totals and recomputed breakdown.

> **Verbatim Code Excerpt (Lines 181-204):**
```csharp
    private async Task<ComposedTicket> ComposeShiftCloseSlipAsync(PrintJobEntity job, CancellationToken ct)
    {
        var (shift, identity) = await LoadShiftIdentityAsync(job.OrderId, ct);
        var opening = Money.FromPiastres(shift.OpeningCashPiastres);
        var breakdown = await Falah.Infrastructure.Shifts.ShiftReconciliation.ComputeBreakdownAsync(_db, shift.Id, opening, ct);
        var actual = Money.FromPiastres(shift.ActualCashPiastres ?? 0);
        var expected = Money.FromPiastres(shift.ExpectedCashPiastres ?? breakdown.ExpectedCash.Piastres);
        breakdown = breakdown with
        {
            ExpectedCash = expected,
            ActualCash = actual,
            Difference = Money.FromPiastres(shift.CashDifferencePiastres ?? (actual - expected).Piastres),
        };

        var expenses = (await _db.Expenses.AsNoTracking()
                .Where(e => e.ShiftId == shift.Id && e.PaidFromDrawer)
                .Select(e => new { e.Category, e.AmountPiastres }).ToListAsync(ct))
            .GroupBy(e => e.Category)
            .Select(g => new ExpenseSummaryLine(g.Key, Money.FromPiastres(g.Sum(e => e.AmountPiastres))))
            .OrderByDescending(l => l.Amount.Piastres).ToList();

        return PrintRules.ComposeShiftCloseTicket(identity, shift.ClosedAtUtc ?? job.CreatedAtUtc, breakdown,
            expenses, await LoadCloseWarningsAsync(shift, ct), shift.DifferenceReason);
    }
```

---

<a id="callable-13"></a>
### Callable 13: `PrintJobQueryService.LoadCloseWarningsAsync(ShiftEntity, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:208:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `208-226`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Inspects the shift for incomplete operational tasks:
1. Orders awaiting electronic payment confirmation (`ElectronicPaymentStatus == "pending"`).
2. Unsettled driver cash custodies (`DriverCustodies.Status == "Open"`).
3. Unpaid active orders.
Returns `Task<List<string>>` containing human-readable warning lines printed on the handover slip.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeShiftCloseSlipAsync`. Read-only.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Surfaces unresolved money liabilities to manager and cashier before drawer cash is locked.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Executes three concurrent-safe `CountAsync` / `ToListAsync` queries with `AsNoTracking()`.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `SlipPrintingAndReprintTests.cs`.
- *Missing Tests:* Verification that cancelled orders do not count as unpaid orders in the close warnings.

> **Verbatim Code Excerpt (Lines 208-226):**
```csharp
    private async Task<List<string>> LoadCloseWarningsAsync(ShiftEntity shift, CancellationToken ct)
    {
        var warnings = new List<string>();

        var pendingElectronic = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.ShiftId == shift.Id && o.ElectronicPaymentStatus == "pending", ct);
        if (pendingElectronic > 0) warnings.Add($"طلبات بانتظار تأكيد تحويل إلكتروني: {pendingElectronic}");

        var unsettled = await _db.DriverCustodies.AsNoTracking()
            .Where(c => c.Status == "Open").Select(c => c.ExpectedPiastres).ToListAsync(ct);
        if (unsettled.Count > 0)
            warnings.Add($"عهدة سائقين غير مسواة: {unsettled.Count} بقيمة {Money.FromPiastres(unsettled.Sum()).Egp} ج");

        var unpaidOrders = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.ShiftId == shift.Id && !o.IsPaid && o.Status != "cancelled" && o.ElectronicPaymentStatus == null, ct);
        if (unpaidOrders > 0) warnings.Add($"طلبات غير مدفوعة: {unpaidOrders}");

        return warnings;
    }
```

---

<a id="callable-14"></a>
### Callable 14: `PrintJobQueryService.ComposeExpenseSlipAsync(PrintJobEntity, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:228:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `228-239`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Loads `ExpenseEntity` by `job.OrderId`, looks up creator username, fetches restaurant settings, maps treasury account via `DescribePaymentAccount`, and invokes [`PrintRules.ComposeExpenseTicket`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs). Returns `ComposedTicket`. Throws `InvalidOperationException("المصروف غير موجود.")` if missing.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeTicketContentAsync` when `job.DocumentKind == "Expense"`.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Generates petty cash payout receipt to be signed by the recipient and placed in the cash drawer.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Read-only query.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* [`SlipPrintingAndReprintTests.Recording_an_expense_queues_its_slip`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/SlipPrintingAndReprintTests.cs#L85-L103).
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Lines 228-239):**
```csharp
    private async Task<ComposedTicket> ComposeExpenseSlipAsync(PrintJobEntity job, CancellationToken ct)
    {
        var expense = await _db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == job.OrderId, ct)
            ?? throw new InvalidOperationException("المصروف غير موجود.");
        var createdBy = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == expense.CreatedByUserId, ct);
        var settings = await _settings.GetAsync(ct);
        return PrintRules.ComposeExpenseTicket(new ExpenseSlipInfo(
            settings.RestaurantName, settings.SubName, expense.Id, expense.CreatedAtUtc,
            Money.FromPiastres(expense.AmountPiastres), expense.Category, expense.Description,
            createdBy?.Name ?? expense.CreatedByUserId, DescribePaymentAccount(expense.PaymentAccountId),
            expense.ApprovedBy is { } approver ? $"اعتماد: {approver}" : null));
    }
```

---

<a id="callable-15"></a>
### Callable 15: `PrintJobQueryService.DescribePaymentAccount(string?)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:242:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `242-250`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Maps treasury account IDs to human-readable Arabic descriptions:
- `TreasuryAccounts.Drawer` $\rightarrow$ `"درج الكاشير"`
- `TreasuryAccounts.Main` $\rightarrow$ `"الخزينة الرئيسية"`
- `TreasuryAccounts.Bank` $\rightarrow$ `"البنك"`
- `TreasuryAccounts.InstaPay` $\rightarrow$ `"إنستاباي"`
- `null` or `""` $\rightarrow$ `"غير محدد"`
- Other $\rightarrow$ returns input string. Returns `string`.

**B. Callers, Dependencies & Side Effects:**  
Private static helper called by `ComposeExpenseSlipAsync`. Pure function.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Ensures treasury account names on printed slips match UI terminology.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Thread-safe pattern match.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in slip tests.
- *Missing Tests:* Unknown account string fallback.

> **Verbatim Code Excerpt (Lines 242-250):**
```csharp
    private static string DescribePaymentAccount(string? accountId) => accountId switch
    {
        TreasuryAccounts.Drawer => "درج الكاشير",
        TreasuryAccounts.Main => "الخزينة الرئيسية",
        TreasuryAccounts.Bank => "البنك",
        TreasuryAccounts.InstaPay => "إنستاباي",
        null or "" => "غير محدد",
        var other => other,
    };
```

---

<a id="callable-16"></a>
### Callable 16: `PrintJobQueryService.ComposeReceiptAsync(PrintRoute, OrderEntity, OrderType, string, IReadOnlyList<TicketItem>, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:257:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `257-278`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes customer receipt for `Receipt` or `CallCenter` routes. Constructs `ReceiptHeader` with live restaurant settings, cashier name, customer name, delivery address, phone numbers, and menu QR URL. Constructs `ReceiptTotals` directly from order snapshot piastre fields. Delegates to [`PrintRules.ComposeReceiptTicket`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs#L89-L108). Returns `ComposedTicket`.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeTicketContentAsync`. Read-only.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- For `PrintRoute.Receipt`: Composes dual-ticket with mid-stream `TicketCut`.
- Injects conditional `TicketQr` element if `settings.MenuUrl` is configured.
- Stamps `"⚠ غير مدفوع"` banner if `order.ElectronicPaymentStatus == "pending"`.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Uses `order.IsPaid` snapshot. If cashier updates payment method in future implementations, reprint reflects snapshot values.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* [`PrintingVerticalSliceTests.The_configured_menu_link_becomes_the_receipt_qr_and_nothing_else_does`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PrintingVerticalSliceTests.cs#L757-L800).
- *Missing Tests:* Verification of `ElectronicPaymentPending` banner formatting on thermal preview.

> **Verbatim Code Excerpt (Lines 257-278):**
```csharp
    private async Task<ComposedTicket> ComposeReceiptAsync(
        PrintRoute route, OrderEntity order, OrderType orderType, string cashierName,
        IReadOnlyList<TicketItem> ticketItems, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        var header = new ReceiptHeader(
            order.Id, settings.RestaurantName, settings.SubName, settings.Address, order.OrderNumber, order.CreatedAtUtc,
            cashierName, orderType, order.TableNumber, order.CustomerName, order.DeliveryAddress, order.DeliveryArea,
            settings.Phone1, settings.Phone2, settings.ComplaintsPhone,
            ElectronicPaymentPending: order.ElectronicPaymentStatus == "pending",
            Footer: settings.ReceiptFooter,
            RequestedPickupAtUtc: order.RequestedPickupAtUtc,
            MenuUrl: settings.MenuUrl,
            IsPaid: order.IsPaid);
        var totals = new ReceiptTotals(
            Money.FromPiastres(order.SubtotalPiastres), Money.FromPiastres(order.DiscountPiastres),
            Money.FromPiastres(order.TaxAmountPiastres), order.TaxRuleName, Money.FromPiastres(order.DeliveryFeePiastres),
            Money.FromPiastres(order.CompAmountPiastres), Money.FromPiastres(order.TotalPiastres),
            Money.FromPiastres(order.AmountPaidPiastres), Money.FromPiastres(order.ChangeDuePiastres),
            OrderEnumMapping.ParsePaymentMethod(order.PaymentMethod));
        return PrintRules.ComposeReceiptTicket(route, header, ticketItems, totals);
    }
```

---

<a id="callable-17"></a>
### Callable 17: `PrintJobQueryService.ComposeDeliveryOrderAsync(OrderEntity, OrderType, string, IReadOnlyList<TicketItem>, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:284:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `284-308`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Composes delivery order packaging ticket. Calculates outstanding amount `Total - AmountPaid`, determines COD status using [`CheckoutRules.IsCashOnDelivery`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L44-L45), extracts customer phone and delivery notes, and bundles into `DeliveryOrderTicketInfo`. Delegates to [`PrintRules.ComposeDeliveryOrderTicket`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs#L299-L329). Returns `ComposedTicket`.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeTicketContentAsync` when route is `DeliveryOrder`. Read-only.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Prints at the bagging station upon order checkout. Footer prints `"بون تشغيل - ليس إيصال العميل"` to prevent delivery tickets from being used as customer tax receipts.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Reads frozen order fields.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `DeliveryVerticalSliceTests.cs`.
- *Missing Tests:* Delivery order with mixed split payment to verify outstanding amount calculation.

> **Verbatim Code Excerpt (Lines 284-308):**
```csharp
    private async Task<ComposedTicket> ComposeDeliveryOrderAsync(
        OrderEntity order, OrderType orderType, string cashierName, IReadOnlyList<TicketItem> ticketItems, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        var paymentMethod = OrderEnumMapping.ParsePaymentMethod(order.PaymentMethod);
        var header = new ReceiptHeader(
            order.Id, settings.RestaurantName, settings.SubName, settings.Address, order.OrderNumber, order.CreatedAtUtc,
            cashierName, orderType, order.TableNumber, order.CustomerName, order.DeliveryAddress, order.DeliveryArea,
            settings.Phone1, settings.Phone2, settings.ComplaintsPhone,
            ElectronicPaymentPending: order.ElectronicPaymentStatus == "pending",
            Footer: settings.ReceiptFooter,
            RequestedPickupAtUtc: order.RequestedPickupAtUtc);
        var totals = new ReceiptTotals(
            Money.FromPiastres(order.SubtotalPiastres), Money.FromPiastres(order.DiscountPiastres),
            Money.FromPiastres(order.TaxAmountPiastres), order.TaxRuleName, Money.FromPiastres(order.DeliveryFeePiastres),
            Money.FromPiastres(order.CompAmountPiastres), Money.FromPiastres(order.TotalPiastres),
            Money.FromPiastres(order.AmountPaidPiastres), Money.FromPiastres(order.ChangeDuePiastres), paymentMethod);

        var outstanding = Money.FromPiastres(Math.Max(0, order.TotalPiastres - order.AmountPaidPiastres));
        var info = new DeliveryOrderTicketInfo(
            order.CustomerPhone, order.DeliveryNotes, outstanding,
            CheckoutRules.IsCashOnDelivery(orderType, paymentMethod, Money.FromPiastres(order.TotalPiastres)));

        return PrintRules.ComposeDeliveryOrderTicket(header, ticketItems, totals, info);
    }
```

---

<a id="callable-18"></a>
### Callable 18: `PrintJobQueryService.ComposeDriverDispatchAsync(PrintJobEntity, OrderEntity, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:313:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `313-345`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Findings:** **`FIN-019`**

**A. Actual Behavior, Inputs & Return Values:**  
Composes driver dispatch custody slip. Queries `_db.Deliveries` by `order.Id`. Resolves driver name: checks `DriverNameAtHandover`, queries `Drivers` table by `DriverId`, and inspects `DeliveryReassignments` table for any reassignment occurring after `job.CreatedAtUtc` to ensure historical tickets retain the driver assigned at the time the job was queued. Checks if an earlier dispatch job exists to set `isReassignment` flag. Calculates COD `amountToCollect` (`Total - AmountPaid`). Delegates to [`PrintRules.ComposeDriverDispatchTicket`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs#L337-L375). Returns `ComposedTicket`. Throws `InvalidOperationException` if delivery missing.

**B. Callers, Dependencies & Side Effects:**  
Called by `ComposeTicketContentAsync` when route is `DriverDispatch`. Read-only.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Reassignment Flag Invariant:** If `earlierJobExists` is true, stamps `"إعادة إسناد"` across top of ticket, preventing a driver from departing with an order assigned to another courier.
- **Custody Calculation Flaw (FIN-019 link):** Line 339 computes `amountToCollect = Total - AmountPaid`. If an order has a split payment where an electronic leg was already paid (e.g. 50 EGP InstaPay paid, 100 EGP cash to collect), but `order.AmountPaidPiastres` is 0 because electronic payment is pending confirmation, `amountToCollect` demands the driver collect the full 150 EGP, overcharging the customer.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Queries `Deliveries`, `Drivers`, `DeliveryReassignments`, and `PrintJobs`.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `DeliveryVerticalSliceTests.cs`.
- *Missing Tests:* Reassignment race test where multiple drivers are assigned within milliseconds.

> **Verbatim Code Excerpt (Lines 313-345):**
```csharp
    private async Task<ComposedTicket> ComposeDriverDispatchAsync(PrintJobEntity job, OrderEntity order, CancellationToken ct)
    {
        var delivery = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.OrderId == order.Id, ct)
            ?? throw new InvalidOperationException("طلب الدليفري المرتبط بهذا الطلب غير موجود.");
        var driverName = delivery.DriverNameAtHandover
            ?? (delivery.DriverId is { } driverId ? await _db.Drivers.AsNoTracking().Where(d => d.Id == driverId).Select(d => d.Name).FirstOrDefaultAsync(ct) : null)
            ?? "غير معروف";

        var nextReassignment = await _db.DeliveryReassignments.AsNoTracking()
            .Where(h => h.DeliveryId == delivery.Id && h.ChangedAtUtc > job.CreatedAtUtc)
            .OrderBy(h => h.ChangedAtUtc).FirstOrDefaultAsync(ct);
        if (nextReassignment?.OldDriverName is { } originalDriverName)
            driverName = originalDriverName;

        var earlierJobExists = await _db.PrintJobs.AsNoTracking()
            .AnyAsync(j => j.OrderId == order.Id && j.Route == nameof(PrintRoute.DriverDispatch) && j.CreatedAtUtc < job.CreatedAtUtc, ct);

        var settings = await _settings.GetAsync(ct);
        var orderType = OrderEnumMapping.ParseOrderType(order.OrderType);
        var paymentMethod = OrderEnumMapping.ParsePaymentMethod(order.PaymentMethod);
        var isCashOnDelivery = CheckoutRules.IsCashOnDelivery(orderType, paymentMethod, Money.FromPiastres(order.TotalPiastres));
        var amountToCollect = Money.FromPiastres(Math.Max(0, order.TotalPiastres - order.AmountPaidPiastres));
        return PrintRules.ComposeDriverDispatchTicket(
            settings.RestaurantName, order.Id, order.OrderNumber, driverName, order.CustomerName, order.CustomerPhone,
            order.DeliveryAddress, order.DeliveryArea, amountToCollect, isCashOnDelivery,
            electronicPaymentPending: order.ElectronicPaymentStatus == "pending",
            order.DeliveryNotes, job.CreatedAtUtc, earlierJobExists);
    }
```
