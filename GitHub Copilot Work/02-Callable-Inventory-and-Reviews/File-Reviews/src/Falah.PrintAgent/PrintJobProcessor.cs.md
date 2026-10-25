# Substantive Callable Audit: `src/Falah.PrintAgent/PrintJobProcessor.cs`

- **Source File:** `src/Falah.PrintAgent/PrintJobProcessor.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `2ef3ef9c1d54c7a16bfe506d95e2803aa1970a09e4fbecdf473a1b82fc3ea640`
- **Auditor Role:** Printing & Operational Workflow Specialist
- **Total File Callables:** **5**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

[`PrintJobProcessor`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs) is the core operational execution engine of the Fale7 Print Agent. It bridges the asynchronous, distributed central server (`Falah.Server`) and the physical thermal receipt hardware (Xprinter XP-D200N / XP-80C class printers) running across 6 distinct routing channels:
1. `Receipt` (Cashier thermal printer, 80mm)
2. `Kitchen` (Legacy kitchen channel)
3. `Charcoal` (Station grill/charcoal prep printer)
4. `Grill` (Station grill prep printer)
5. `DeliveryOrder` (Dispatch desk packing slip)
6. `DriverDispatch` (Delivery driver handover ticket)

In high-volume restaurant operations, printing is an irreversible physical side-effect. Once ink/heat touches thermal paper and the mechanical knife blade descends, physical goods (food tickets, receipts) enter the physical workflow. Physical tickets cannot be "rolled back" by an HTTP error or a database transaction rollback. If a print agent fails to report the completion of a print job due to a temporary network blip or server restart, a naive agent would re-fetch the `Pending` job on the next poll cycle and print it again—resulting in duplicate meal preparations, wasted ingredients, kitchen conflict, and cashier confusion.

To solve this distributed consensus and physical idempotency challenge, [`PrintJobProcessor`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs) enforces two core architectural invariants:
1. **At-Most-Once Physical Execution via Write-Ahead Journaling:** Every physical attempt is assigned a globally unique client-minted request identifier (`attemptRequestId = $"pra-{Guid.NewGuid():N}"`). Before raw bytes are dispatched to the printer transport, an *uncertain* attempt is persisted to local durable disk storage via [`UnreportedAttemptStore`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/UnreportedAttemptStore.cs). If the workstation experiences power loss or process termination mid-print, the next startup detects the stored attempt and refuses to auto-reprint, re-reporting the failure for operator inspection.
2. **Sequential Queue Draining (Single Mechanical Paper Path):** Thermal POS printers feature a single paper feed path and mechanical cutter. Draining is executed strictly sequentially per route, preventing interleaved byte sequences from corrupting physical tickets.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `PrintJobProcessor.PrintJobProcessor(IPrintServerClient, ITicketTransport, Encoding, string, Action<string>, UnreportedAttemptStore?, byte?, bool, int, bool, int)`
- **Callable ID:** `src/Falah.PrintAgent/PrintJobProcessor.cs:46:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `46-63`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Constructs the processing engine with its server communication client, physical hardware transport, encoding parameters, and diagnostic logger.
- `server` (`IPrintServerClient`): HTTP/API communication client used for polling pending jobs, downloading ticket payloads, and reporting execution results.
- `transport` (`ITicketTransport`): Hardware transport abstraction (`WindowsSpoolerTransport`, `TcpTicketTransport`, or `ConsoleTicketTransport`).
- `encoding` (`Encoding`): Character encoding for text mode (typically UTF-8 or Windows-1256).
- `reportedByUserId` (`string`): Machine/user identifier attached to report payloads.
- `log` (`Action<string>`): Logging sink for operational diagnostics.
- `unreported` (`UnreportedAttemptStore?`): Optional disk-backed attempt store. If null, falls back to a default file `unreported-print-attempts.json` in `AppContext.BaseDirectory`.
- `codePageTableId` (`byte?`): Optional ESC/POS code page table selector for text mode.
- `renderAsImage` (`bool`, default `false`): Feature flag selecting whether to render tickets as 1-bit monochrome raster images via `BitmapTicketRenderer` rather than ESC/POS text mode.
- `imageWidthDots` (`int`, default `576`): Dot width for 80mm print head (8 dots/mm $\times$ 72mm printable width = 576 dots).
- `thickenStrokes` (`bool`, default `true`): Dilates font strokes by 1 pixel horizontally to prevent thermal print fade.
- `imageMarginDots` (`int`, default `24`): Lateral whitespace margin.

**B. Callers, Dependencies & Side Effects:**  
Instantiated in `Falah.PrintAgent.Program.Main` ([`Program.cs:127-140`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs#L127-L140)) and in test fixtures (`PrintAgentTests.cs`). Note: When instantiated via `Program.cs`, the caller supplies `UnreportedAttemptStore.ForRoute(options.Route, AppContext.BaseDirectory)`, ensuring route-isolated disk journals. If the optional parameter is omitted, the default fallback creates an unpartitioned file (`unreported-print-attempts.json`), which could lead to cross-route file lock contention if multiple agent instances run concurrently on the same machine without route arguments.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Initializes state before polling or hardware activation. The constructor ensures that the disk-backed store is instantiated immediately, reading any uncompleted attempts from disk into memory prior to processing any network jobs.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Thread-safety of state is guaranteed by single-threaded per-route agent lifecycles managed by a machine-wide named mutex (`Global\FalahPrintAgent-{options.Route}`) in [`Program.cs:100`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs#L100).

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in integration test fixtures `PrintAgentTests.cs` (e.g. `Runs_full_cycle_successfully`).
- *Missing Tests:* Verification of constructor fallback when `AppContext.BaseDirectory` is read-only.

> **Verbatim Code Excerpt (Lines 46-63):**
```csharp
    public PrintJobProcessor(
        IPrintServerClient server, ITicketTransport transport, Encoding encoding,
        string reportedByUserId, Action<string> log, UnreportedAttemptStore? unreported = null, byte? codePageTableId = null,
        bool renderAsImage = false, int imageWidthDots = 576, bool thickenStrokes = true, int imageMarginDots = 24)
    {
        _server = server;
        _transport = transport;
        _encoding = encoding;
        _codePageTableId = codePageTableId;
        _renderAsImage = renderAsImage;
        _imageWidthDots = imageWidthDots;
        _thickenStrokes = thickenStrokes;
        _imageMarginDots = imageMarginDots;
        _reportedByUserId = reportedByUserId;
        _log = log;
        _unreported = unreported ?? new UnreportedAttemptStore(
            Path.Combine(AppContext.BaseDirectory, "unreported-print-attempts.json"));
    }
```

---

<a id="callable-2"></a>
### Callable 2: `PrintJobProcessor.DrainAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintJobProcessor.cs:68:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `68-79`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Drains all pending print jobs queued for a specific route.
1. Calls `_server.GetPendingAsync(route, ct)` to retrieve the current batch of pending jobs from the central server.
2. Initializes `processed = 0`.
3. Iterates sequentially through `pending`:
   - Checks `ct.ThrowIfCancellationRequested()`.
   - Awaits `ProcessAsync(job, ct)`.
   - Increments `processed`.
4. Returns the total count of processed jobs (`int`).

**B. Callers, Dependencies & Side Effects:**  
Called by the agent main polling loop in [`Program.cs:161`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs#L161) and by integration tests. Triggers network I/O, local disk writes, and physical printer transmissions.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Mechanical Paper Path Serialization:** Thermal printers cannot interleave jobs. By awaiting `ProcessAsync` sequentially without `Task.WhenAll`, `DrainAsync` strictly enforces that ticket $N$ finishes transmission, line feeds, and knife cuts before ticket $N+1$ begins.
- **Fail-Stop on Server Disconnection:** If `_server.GetPendingAsync` throws (e.g. HTTP 401 unauthorized due to token expiration, or HTTP 503 during server maintenance), `DrainAsync` bubbles the exception directly to `Program.cs`, which logs the error and backs off without advancing state or corrupting queues.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Jobs are fetched in chronological order (`CreatedAtUtc` ascending from `PrintJobQueryService`). Processing is strictly serial per agent instance.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Validated in `PrintAgentTests.cs` across all standard execution scenarios.
- *Missing Tests:* Drain cancellation mid-batch test verifying that remaining pending jobs in the returned list are not dropped or corrupted.

> **Verbatim Code Excerpt (Lines 68-79):**
```csharp
    public async Task<int> DrainAsync(string route, CancellationToken ct)
    {
        var pending = await _server.GetPendingAsync(route, ct);
        var processed = 0;
        foreach (var job in pending)
        {
            ct.ThrowIfCancellationRequested();
            await ProcessAsync(job, ct);
            processed++;
        }
        return processed;
    }
```

---

<a id="callable-3"></a>
### Callable 3: `PrintJobProcessor.ProcessAsync(PrintJobDto, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintJobProcessor.cs:81:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `81-129`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Executes end-to-end processing for a single print job:
1. **Pre-flight Idempotency Check (Lines 85-91):** Queries `_unreported.Find(job.Id)`. If an attempt record already exists in the local store, logs a warning (`"job {job.Id} already printed; re-reporting attempt..."`), immediately invokes `ReportAsync` with the stored attempt data, and terminates without sending any bytes to the printer.
2. **Attempt Key Minting (Line 94):** Generates a client-side idempotency key `attemptRequestId = $"pra-{Guid.NewGuid():N}"`.
3. **Ticket Fetch & Rendering (Lines 100-103):** Fetches `ticket` via `_server.GetTicketAsync(job.Id, ct)`. Converts wire DTO to domain ticket via `ToDomain(ticket)`. Selects rendering pipeline:
   - If `_renderAsImage && OperatingSystem.IsWindows()`: renders high-fidelity 1-bit monochrome raster image via `BitmapTicketRenderer.Render(...)`.
   - Otherwise: encodes standard ESC/POS command stream via `EscPosTicketRenderer.Render(...)`.
4. **Write-Ahead Safety Journaling (Lines 106-107):** Writes a durable record to `_unreported` with `Succeeded = false` and message `"Print interrupted; output may exist. Inspect paper before retrying."` BEFORE sending bytes to the printer.
5. **Physical Transmission (Line 108):** Awaits `_transport.SendAsync(payload, ct)`. On success, sets `succeeded = true; error = null`.
6. **Exception & Cancellation Handling (Lines 113-123):**
   - If `OperationCanceledException` occurs and `ct.IsCancellationRequested` is true, rethrows immediately, leaving the durable uncertain attempt on disk.
   - If any other exception occurs, records `succeeded = false`, constructs diagnostic error message including transport description, and logs the failure.
7. **Post-Transmission Recording & Reporting (Lines 125-128):** Updates `_unreported` with actual outcome, then calls `ReportAsync` to deliver the outcome to the server.

**B. Callers, Dependencies & Side Effects:**  
Called by `DrainAsync` (line 75). Interacts with `_server`, `_transport`, `_unreported`, and the static renderers (`BitmapTicketRenderer` / `EscPosTicketRenderer`). Side effects: disk I/O, network HTTP calls, physical printer spooling.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Defense Against PRT-001 (Duplicate Kitchen Tickets):** Prevents duplicate tickets by checking `_unreported.Find(job.Id)` before transmitting bytes.
- **Exposure to PRT-002 (Spooler Buffering Conflation):** In `WindowsSpoolerTransport`, `SendAsync` sends raw bytes to the Windows Print Spooler (`spoolsv.exe`) via `OpenPrinter` and `WritePrinter`. As soon as the spooler accepts the raw bytes into its memory buffer, `WritePrinter` succeeds and `SendAsync` returns true. However, if the thermal printer is out of paper, powered off, or experiencing a cutter jam, the paper has NOT actually printed. Because `PrintJobProcessor` interprets `SendAsync` completion as physical success (`succeeded = true`), it reports `Succeeded = true` to the server while the job sits stalled in the Windows spooler queue.
- **Write-Ahead Crash Invariant:** Line 106 ensures that if power cuts out during `_transport.SendAsync`, the workstation restarts with a recorded attempt on disk. The next run will NOT re-print the job.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Scoped to the single-threaded route drain loop. Thread-safety across different processes on the same machine relies on mutexes and route-partitioned file stores.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `PrintAgentTests.A_job_whose_outcome_could_not_be_reported_is_re_reported_not_reprinted` and `Uncertain_print_attempt_is_recorded_before_transport_send`.
- *Missing Tests:* Test simulating transport cancellation midway through byte transmission to verify the uncertain attempt remains on disk.

> **Verbatim Code Excerpt (Lines 81-129):**
```csharp
    public async Task ProcessAsync(PrintJobDto job, CancellationToken ct)
    {
        // This job already came out of a printer; only the telling failed. Tell it again - do not
        // print it again.
        if (_unreported.Find(job.Id) is { } already)
        {
            _log($"job {job.Id} already printed; re-reporting attempt {already.AttemptRequestId} instead of reprinting");
            await ReportAsync(new ReportPrintJobResultRequest(
                already.AttemptRequestId, job.Id, already.Succeeded, already.Error, _reportedByUserId), ct);
            return;
        }

        // Minted before anything is sent, and reused for every retry of the report below.
        var attemptRequestId = $"pra-{Guid.NewGuid():N}";
        bool succeeded;
        string? error;

        try
        {
            var ticket = await _server.GetTicketAsync(job.Id, ct);
            var payload = _renderAsImage && OperatingSystem.IsWindows()
                ? BitmapTicketRenderer.Render(ToDomain(ticket), _imageWidthDots, _thickenStrokes, _imageMarginDots)
                : EscPosTicketRenderer.Render(ToDomain(ticket), _encoding, _codePageTableId);
            // Persist BEFORE the first byte can leave. After a crash or cancelled send, paper may
            // already exist; report an uncertain failure for manual inspection, never auto-reprint.
            _unreported.Remember(job.Id, new UnreportedAttemptStore.StoredAttempt(
                attemptRequestId, false, "Print interrupted; output may exist. Inspect paper before retrying.", DateTimeOffset.UtcNow));
            await _transport.SendAsync(payload, ct);
            succeeded = true;
            error = null;
            _log($"printed {job.Route} job {job.Id} for order {job.OrderNumber} via {_transport.Description}");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // The durable uncertain attempt remains for the next process to report.
            throw;
        }
        catch (Exception ex)
        {
            succeeded = false;
            error = $"{ex.GetType().Name}: {ex.Message} ({_transport.Description})";
            _log($"FAILED {job.Route} job {job.Id}: {error}");
        }

        _unreported.Remember(job.Id, new UnreportedAttemptStore.StoredAttempt(
            attemptRequestId, succeeded, error, DateTimeOffset.UtcNow));
        await ReportAsync(new ReportPrintJobResultRequest(
            attemptRequestId, job.Id, succeeded, error, _reportedByUserId), ct);
    }
```

---

<a id="callable-4"></a>
### Callable 4: `PrintJobProcessor.ReportAsync(ReportPrintJobResultRequest, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintJobProcessor.cs:131:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `131-161`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Delivers the print execution outcome to the central server with built-in retry and disk journaling fallback:
1. Loops for `attempt = 1` to `ReportAttempts` (3 iterations):
   - Awaits `_server.ReportResultAsync(request, ct)`.
   - On success: immediately purges the local journal via `_unreported.Forget(request.PrintJobId)` and returns.
   - If `OperationCanceledException` and cancellation is requested: rethrows immediately without suppressing.
   - If non-cancellation exception occurs and `attempt < ReportAttempts`: logs warning and delays for `400ms * attempt` (400ms, 800ms) before retrying.
   - If non-cancellation exception occurs on final attempt (`attempt == ReportAttempts`): catches exception, persists the unconfirmed attempt to disk via `_unreported.Remember(...)`, and logs that the outcome will be re-reported on subsequent poll cycles.

**B. Callers, Dependencies & Side Effects:**  
Called by `ProcessAsync` at line 88 (re-reporting existing attempt) and line 127 (reporting fresh attempt). Interacts with `_server` (HTTP POST) and `_unreported` (disk persistence).

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Durable Offline Safety Invariant:** Once paper emerges from the thermal print head, failure to report the result to the server MUST NOT cause the ticket to be reprinted. By persisting the attempt to disk when all 3 network attempts fail, `ReportAsync` ensures that on the next poll cycle, `ProcessAsync` will detect the job in `_unreported` and re-attempt reporting rather than re-triggering printer hardware.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Serial execution within the agent loop. Server-side idempotency is enforced by matching `AttemptRequestId` in `PrintJobQueryService.RecordAttemptAsync`.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `PrintAgentTests.Retries_failed_report_with_backoff`.
- *Missing Tests:* Verification of `ReportAsync` behavior when server returns HTTP 404 (e.g. job deleted while printing).

> **Verbatim Code Excerpt (Lines 131-161):**
```csharp
    private async Task ReportAsync(ReportPrintJobResultRequest request, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= ReportAttempts; attempt++)
        {
            try
            {
                await _server.ReportResultAsync(request, ct);
                // The server has it. Nothing left to remember, and nothing left to re-report.
                _unreported.Forget(request.PrintJobId);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < ReportAttempts)
            {
                _log($"could not report job {request.PrintJobId} (attempt {attempt}): {ex.Message}");
                await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), ct);
            }
            catch (Exception ex)
            {
                // The paper is already out of the printer and the server does not know. Writing the
                // attempt down is what stops the next poll from printing the same ticket again.
                _unreported.Remember(request.PrintJobId, new UnreportedAttemptStore.StoredAttempt(
                    request.RequestId, request.Succeeded, request.Error, DateTimeOffset.UtcNow));
                _log($"could not report the outcome of job {request.PrintJobId} to the server: {ex.Message} - " +
                     "kept on disk; it will be re-reported, not reprinted");
            }
        }
    }
```

---

<a id="callable-5"></a>
### Callable 5: `PrintJobProcessor.ToDomain(ComposedTicketDto)`
- **Callable ID:** `src/Falah.PrintAgent/PrintJobProcessor.cs:165:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `165-170`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Static mapper converting wire transfer DTO `ComposedTicketDto` into the rich domain record `ComposedTicket`:
- Maps `Route` by parsing enum: `Enum.Parse<PrintRoute>(dto.Route, ignoreCase: true)`.
- Passes through `OrderId` and `OrderNumber`.
- Maps lines collection via `TicketElementMapping.ToDomain` projection.
- Returns `new ComposedTicket(...)`.

**B. Callers, Dependencies & Side Effects:**  
Called by `ProcessAsync` (lines 102 and 103). Pure mapping method; no side effects, no I/O.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Hardware-agnostic object translation. Preserves exact line ordering and element hierarchy required by renderers.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Pure static function. Thread-safe by definition.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered transitively across all print agent integration tests.
- *Missing Tests:* Explicit test verifying that invalid/unknown route strings throw `ArgumentException` with clean diagnostic information.

> **Verbatim Code Excerpt (Lines 165-170):**
```csharp
    public static ComposedTicket ToDomain(ComposedTicketDto dto) => new(
        Enum.Parse<PrintRoute>(dto.Route, ignoreCase: true),
        dto.OrderId,
        dto.OrderNumber,
        dto.Lines.Select(Falah.Application.Printing.TicketElementMapping.ToDomain).ToList());
```

---

## 3. Subsystem Vulnerability & Integrity Analysis

| Finding ID | Vulnerability / Architectural Gap | Root Cause in `PrintJobProcessor.cs` | Severity | Remediation Strategy |
| :--- | :--- | :--- | :--- | :--- |
| **PRT-001** | Duplicate Kitchen Print Jobs | Central server queuing multiple jobs for same `(OrderId, Route)`. `PrintJobProcessor` handles local recovery via `_unreported`, but cannot prevent distinct server-queued job entities. | **CRITICAL** | Enforce unique DB constraint `IX_PrintJobs_OrderId_Route` in `FalahDbContext` for active states. |
| **PRT-002** | Spooler Buffering Conflated with Physical Print | `_transport.SendAsync` returns true upon Windows Spooler (`spoolsv.exe`) buffer acceptance. If paper is out, power is off, or blade is jammed, `ProcessAsync` records `succeeded = true` and reports success. | **HIGH** | Implement bidirectional ESC/POS status polling (`DLE EOT 1`, `DLE EOT 4`) in `WindowsSpoolerTransport` prior to reporting success. |
| **PRT-004** | Agent Token Expiration Halts Drain | If the agent's authentication token expires during `DrainAsync`, `GetPendingAsync` throws 401 and halts the loop. | **MEDIUM** | Implement automated bearer token refresh inside `PrintServerClient`. |
