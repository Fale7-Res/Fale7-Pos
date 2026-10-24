# Substantive Callable Audit: `src/Falah.PrintAgent/PrintJobProcessor.cs`

- **Source File:** `src/Falah.PrintAgent/PrintJobProcessor.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `2ef3ef9c1d54c7a16bfe506d95e2803aa1970a09e4fbecdf473a1b82fc3ea640`
- **Auditor Specialist:** `printing-workflow-auditor`
- **Total File Callables:** **5**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Core processing loop of PrintAgent polling pending jobs from server, executing rasterization/formatting, dispatching to transport, and reporting execution results.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.PrintAgent.PrintJobProcessor.PrintJobProcessor(Falah.PrintAgent.IPrintServerClient, Falah.PrintAgent.ITicketTransport, System.Text.Encoding, string, Action<string>, Falah.PrintAgent.UnreportedAttemptStore?, byte?, bool, int, bool, int)`
- **Callable ID:** `src/Falah.PrintAgent/PrintJobProcessor.cs:46:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `46-63`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.PrintAgent.PrintJobProcessor.PrintJobProcessor(Falah.PrintAgent.IPrintServerClient, Falah.PrintAgent.ITicketTransport, System.Text.Encoding, string, Action<string>, Falah.PrintAgent.UnreportedAttemptStore?, byte?, bool, int, bool, int). Manages internal state transition within src/Falah.PrintAgent/PrintJobProcessor.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 46-63):**
```csharp
  46 |     public PrintJobProcessor(
  47 |         IPrintServerClient server, ITicketTransport transport, Encoding encoding,
  48 |         string reportedByUserId, Action<string> log, UnreportedAttemptStore? unreported = null, byte? codePageTableId = null,
  49 |         bool renderAsImage = false, int imageWidthDots = 576, bool thickenStrokes = true, int imageMarginDots = 24)
  50 |     {
  51 |         _server = server;
  52 |         _transport = transport;
  53 |         _encoding = encoding;
  54 |         _codePageTableId = codePageTableId;
  55 |         _renderAsImage = renderAsImage;
  56 |         _imageWidthDots = imageWidthDots;
  57 |         _thickenStrokes = thickenStrokes;
  58 |         _imageMarginDots = imageMarginDots;
  59 |         _reportedByUserId = reportedByUserId;
  60 |         _log = log;
  61 |         _unreported = unreported ?? new UnreportedAttemptStore(
  62 |             Path.Combine(AppContext.BaseDirectory, "unreported-print-attempts.json"));
  63 |     }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.PrintAgent.PrintJobProcessor.DrainAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintJobProcessor.cs:68:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `68-79`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.PrintJobProcessor.DrainAsync(string, CancellationToken). Manages internal state transition within src/Falah.PrintAgent/PrintJobProcessor.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 68-79):**
```csharp
  68 |     public async Task<int> DrainAsync(string route, CancellationToken ct)
  69 |     {
  70 |         var pending = await _server.GetPendingAsync(route, ct);
  71 |         var processed = 0;
  72 |         foreach (var job in pending)
  73 |         {
  74 |             ct.ThrowIfCancellationRequested();
  75 |             await ProcessAsync(job, ct);
  76 |             processed++;
  77 |         }
  78 |         return processed;
  79 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `Falah.PrintAgent.PrintJobProcessor.ProcessAsync(Falah.Application.Printing.PrintJobDto, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintJobProcessor.cs:81:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `81-129`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Fetches ticket payload from server, passes to transport, records attempt, and calls ReportResultAsync.

**B. Trust Boundaries & Validation:**  
Server HTTP API responses.

**C. State Mutations & Side Effects:**  
Local retry cache mutation, network HTTP reporting.

**D. Concurrency & Transaction Implications:**  
Serial execution per agent instance.

**E. Specialist Risks & Invariants:**  
Spooler false positives report Success to server even when printer hardware is jammed.

> **Verbatim Code Excerpt (Lines 81-129):**
```csharp
  81 |     public async Task ProcessAsync(PrintJobDto job, CancellationToken ct)
  82 |     {
  83 |         // This job already came out of a printer; only the telling failed. Tell it again - do not
  84 |         // print it again.
  85 |         if (_unreported.Find(job.Id) is { } already)
  86 |         {
  87 |             _log($"job {job.Id} already printed; re-reporting attempt {already.AttemptRequestId} instead of reprinting");
  88 |             await ReportAsync(new ReportPrintJobResultRequest(
  89 |                 already.AttemptRequestId, job.Id, already.Succeeded, already.Error, _reportedByUserId), ct);
  90 |             return;
  91 |         }
  92 | 
  93 |         // Minted before anything is sent, and reused for every retry of the report below.
  94 |         var attemptRequestId = $"pra-{Guid.NewGuid():N}";
  95 |         bool succeeded;
  96 |         string? error;
  97 | 
  98 |         try
  99 |         {
 100 |             var ticket = await _server.GetTicketAsync(job.Id, ct);
 101 |             var payload = _renderAsImage && OperatingSystem.IsWindows()
 102 |                 ? BitmapTicketRenderer.Render(ToDomain(ticket), _imageWidthDots, _thickenStrokes, _imageMarginDots)
 103 |                 : EscPosTicketRenderer.Render(ToDomain(ticket), _encoding, _codePageTableId);
 104 |             // Persist BEFORE the first byte can leave. After a crash or cancelled send, paper may
 105 |             // already exist; report an uncertain failure for manual inspection, never auto-reprint.
 106 |             _unreported.Remember(job.Id, new UnreportedAttemptStore.StoredAttempt(
 107 |                 attemptRequestId, false, "Print interrupted; output may exist. Inspect paper before retrying.", DateTimeOffset.UtcNow));
 108 |             await _transport.SendAsync(payload, ct);
 109 |             succeeded = true;
 110 |             error = null;
 111 |             _log($"printed {job.Route} job {job.Id} for order {job.OrderNumber} via {_transport.Description}");
 112 |         }
 113 |         catch (OperationCanceledException) when (ct.IsCancellationRequested)
 114 |         {
 115 |             // The durable uncertain attempt remains for the next process to report.
 116 |             throw;
 117 |         }
 118 |         catch (Exception ex)
 119 |         {
 120 |             succeeded = false;
 121 |             error = $"{ex.GetType().Name}: {ex.Message} ({_transport.Description})";
 122 |             _log($"FAILED {job.Route} job {job.Id}: {error}");
 123 |         }
 124 | 
 125 |         _unreported.Remember(job.Id, new UnreportedAttemptStore.StoredAttempt(
 126 |             attemptRequestId, succeeded, error, DateTimeOffset.UtcNow));
 127 |         await ReportAsync(new ReportPrintJobResultRequest(
 128 |             attemptRequestId, job.Id, succeeded, error, _reportedByUserId), ct);
 129 |     }
```

---

<a id="callable-4"></a>
### Callable 4: `Falah.PrintAgent.PrintJobProcessor.ReportAsync(Falah.Application.Printing.ReportPrintJobResultRequest, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintJobProcessor.cs:131:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `131-161`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.PrintJobProcessor.ReportAsync(Falah.Application.Printing.ReportPrintJobResultRequest, CancellationToken). Manages internal state transition within src/Falah.PrintAgent/PrintJobProcessor.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 131-161):**
```csharp
 131 |     private async Task ReportAsync(ReportPrintJobResultRequest request, CancellationToken ct)
 132 |     {
 133 |         for (var attempt = 1; attempt <= ReportAttempts; attempt++)
 134 |         {
 135 |             try
 136 |             {
 137 |                 await _server.ReportResultAsync(request, ct);
 138 |                 // The server has it. Nothing left to remember, and nothing left to re-report.
 139 |                 _unreported.Forget(request.PrintJobId);
 140 |                 return;
 141 |             }
 142 |             catch (OperationCanceledException) when (ct.IsCancellationRequested)
 143 |             {
 144 |                 throw;
 145 |             }
 146 |             catch (Exception ex) when (attempt < ReportAttempts)
 147 |             {
 148 |                 _log($"could not report job {request.PrintJobId} (attempt {attempt}): {ex.Message}");
 149 |                 await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), ct);
 150 |             }
 151 |             catch (Exception ex)
 152 |             {
 153 |                 // The paper is already out of the printer and the server does not know. Writing the
 154 |                 // attempt down is what stops the next poll from printing the same ticket again.
 155 |                 _unreported.Remember(request.PrintJobId, new UnreportedAttemptStore.StoredAttempt(
 156 |                     request.RequestId, request.Succeeded, request.Error, DateTimeOffset.UtcNow));
 157 |                 _log($"could not report the outcome of job {request.PrintJobId} to the server: {ex.Message} - " +
 158 |                      "kept on disk; it will be re-reported, not reprinted");
 159 |             }
 160 |         }
 161 |     }
```

---

<a id="callable-5"></a>
### Callable 5: `Falah.PrintAgent.PrintJobProcessor.ToDomain(Falah.Application.Printing.ComposedTicketDto)`
- **Callable ID:** `src/Falah.PrintAgent/PrintJobProcessor.cs:165:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `165-169`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.PrintJobProcessor.ToDomain(Falah.Application.Printing.ComposedTicketDto). Manages internal state transition within src/Falah.PrintAgent/PrintJobProcessor.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 165-169):**
```csharp
 165 |     public static ComposedTicket ToDomain(ComposedTicketDto dto) => new(
 166 |         Enum.Parse<PrintRoute>(dto.Route, ignoreCase: true),
 167 |         dto.OrderId,
 168 |         dto.OrderNumber,
 169 |         dto.Lines.Select(Falah.Application.Printing.TicketElementMapping.ToDomain).ToList());
```

---
