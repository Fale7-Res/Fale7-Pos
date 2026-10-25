# Substantive Callable Audit: `src/Falah.PrintAgent/PrintServerClient.cs`

- **Source File:** `src/Falah.PrintAgent/PrintServerClient.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `27f4e0c6c3e54a197e9992182884a6961fb1df7f65f6dddfa7f6aa216d763a56`
- **Auditor Role:** Printing & Operational Workflow Specialist
- **Total File Callables:** **10** (4 Interface Declarations + 1 Constructor + 4 Class Methods + 1 Dispose)
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

[`PrintServerClient`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintServerClient.cs) is the dedicated HTTP communication client for the Fale7 Print Agent. It encapsulates all remote procedure calls made by station print agents to the central POS server (`Falah.Server`):
1. Authenticating the agent terminal using a system PIN (`/api/auth/login-by-pin`).
2. Polling for pending print jobs queued for a specific route (`/api/print-jobs/pending?route={route}`).
3. Downloading ticket payload data for a specific print job (`/api/print-jobs/{jobId}/ticket`).
4. Reporting physical execution results and idempotency attempt IDs back to the server (`/api/print-jobs/{jobId}/report-result`).

The client is split behind the [`IPrintServerClient`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintServerClient.cs#L11-L17) interface to decouple the agent's core queue processing loop from the transport layer, ensuring that [`PrintJobProcessor`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs) can be rigorously unit tested without requiring a live web host.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `IPrintServerClient.AuthenticateAsync(string, CancellationToken)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:13:5:MethodDeclaration`
- **Exact Line Range:** Line `13`
- **Signature:** `Task AuthenticateAsync(string pin, CancellationToken ct);`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Interface contract defining terminal PIN authentication. Expects agent operator/station PIN; returns `Task` completing upon session token acquisition.

> **Verbatim Code Excerpt (Line 13):**
```csharp
    Task AuthenticateAsync(string pin, CancellationToken ct);
```

---

<a id="callable-2"></a>
### Callable 2: `IPrintServerClient.GetPendingAsync(string, CancellationToken)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:14:5:MethodDeclaration`
- **Exact Line Range:** Line `14`
- **Signature:** `Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct);`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Interface contract querying active pending jobs for a specific route (e.g. `"Receipt"`, `"Charcoal"`, `"Grill"`). Returns read-only list of `PrintJobDto`.

> **Verbatim Code Excerpt (Line 14):**
```csharp
    Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct);
```

---

<a id="callable-3"></a>
### Callable 3: `IPrintServerClient.GetTicketAsync(string, CancellationToken)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:15:5:MethodDeclaration`
- **Exact Line Range:** Line `15`
- **Signature:** `Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct);`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Interface contract retrieving the pre-composed ticket layout payload for a specific `jobId`.

> **Verbatim Code Excerpt (Line 15):**
```csharp
    Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct);
```

---

<a id="callable-4"></a>
### Callable 4: `IPrintServerClient.ReportResultAsync(ReportPrintJobResultRequest, CancellationToken)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:16:5:MethodDeclaration`
- **Exact Line Range:** Line `16`
- **Signature:** `Task ReportResultAsync(ReportPrintJobResultRequest request, CancellationToken ct);`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Interface contract posting the execution outcome of an attempt (success or failure) to the server.

> **Verbatim Code Excerpt (Line 16):**
```csharp
    Task ReportResultAsync(ReportPrintJobResultRequest request, CancellationToken ct);
```

---

<a id="callable-5"></a>
### Callable 5: `PrintServerClient.PrintServerClient(string, TimeSpan)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:28:5:ConstructorDeclaration`
- **Exact Line Range:** Lines `28–31`
- **Signature:** `public PrintServerClient(string baseAddress, TimeSpan timeout)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Instantiates the internal `HttpClient` with target base URL and network timeout:
`_http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = timeout };`

**B. Callers, Dependencies & Side Effects:**  
Called by `Program.Main` in [`src/Falah.PrintAgent/Program.cs:121`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs#L121).

> **Verbatim Code Excerpt (Lines 28-31):**
```csharp
    public PrintServerClient(string baseAddress, TimeSpan timeout)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = timeout };
    }
```

---

<a id="callable-6"></a>
### Callable 6: `PrintServerClient.AuthenticateAsync(string, CancellationToken)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:33:5:MethodDeclaration`
- **Exact Line Range:** Lines `33–40`
- **Signature:** `public async Task AuthenticateAsync(string pin, CancellationToken ct)`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`PRT-004`**

**A. Actual Behavior, Inputs & Return Values:**  
Performs initial terminal login:
1. Sends `POST /api/auth/login-by-pin` with `LoginByPinRequest(pin)`.
2. Calls `response.EnsureSuccessStatusCode()`.
3. Reads `LoginResultDto` and sets `_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token)`.

**B. Defect Analysis of PRT-004 (Single-Shot Authentication):**
- The method logs in once at startup.
- **The PIN is discarded:** The method does not store `pin` in a private field `_pin`.
- The acquired `Bearer` token is assigned directly to `DefaultRequestHeaders`.
- There is no mechanism to re-authenticate if the token becomes invalid.

> **Verbatim Code Excerpt (Lines 33-40):**
```csharp
    public async Task AuthenticateAsync(string pin, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin), ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResultDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Login response was empty.");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
    }
```

---

<a id="callable-7"></a>
### Callable 7: `PrintServerClient.GetPendingAsync(string, CancellationToken)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:42:5:MethodDeclaration`
- **Exact Line Range:** Lines `42–47`
- **Signature:** `public async Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct)`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`PRT-004`**

**A. Actual Behavior, Inputs & Return Values:**  
Executes `GET /api/print-jobs/pending?route={route}` using `_http.GetFromJsonAsync<List<PrintJobDto>>`. Returns the list of pending jobs, or an empty list if response is null.

**B. Defect Analysis of PRT-004 (Infinite 401 Loop):**
- If the central POS server restarts, all in-memory sessions in `SessionService` are wiped.
- When `GetPendingAsync` executes, the server responds with HTTP `401 Unauthorized`.
- `GetFromJsonAsync` throws `HttpRequestException: Response status code does not indicate success: 401 (Unauthorized)`.
- In [`Program.cs:146-155`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs#L146-L155), the exception is caught, logged as `"poll failed: HttpRequestException: 401"`, and the loop waits 2 seconds before calling `processor.DrainAsync` again.
- On the next poll, `GetPendingAsync` is invoked again using the **same dead Bearer token**.
- **Result:** The print agent enters an infinite failure loop. Tickets stop printing across the entire restaurant until an administrator notices and manually restarts the agent process.

> **Verbatim Code Excerpt (Lines 42-47):**
```csharp
    public async Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct)
    {
        var jobs = await _http.GetFromJsonAsync<List<PrintJobDto>>(
            $"/api/print-jobs/pending?route={Uri.EscapeDataString(route)}", ct);
        return jobs ?? new List<PrintJobDto>();
    }
```

---

<a id="callable-8"></a>
### Callable 8: `PrintServerClient.GetTicketAsync(string, CancellationToken)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:49:5:MethodDeclaration`
- **Exact Line Range:** Lines `49–54`
- **Signature:** `public async Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Executes `GET /api/print-jobs/{jobId}/ticket`. Returns `ComposedTicketDto`. Throws `InvalidOperationException` if payload is empty.

> **Verbatim Code Excerpt (Lines 49-54):**
```csharp
    public async Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct)
    {
        var ticket = await _http.GetFromJsonAsync<ComposedTicketDto>(
            $"/api/print-jobs/{Uri.EscapeDataString(jobId)}/ticket", ct);
        return ticket ?? throw new InvalidOperationException($"Ticket content for job {jobId} was empty.");
    }
```

---

<a id="callable-9"></a>
### Callable 9: `PrintServerClient.ReportResultAsync(ReportPrintJobResultRequest, CancellationToken)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:56:5:MethodDeclaration`
- **Exact Line Range:** Lines `56–61`
- **Signature:** `public async Task ReportResultAsync(ReportPrintJobResultRequest request, CancellationToken ct)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Executes `POST /api/print-jobs/{jobId}/report-result`. Calls `response.EnsureSuccessStatusCode()`.
- If server returns 401 Unauthorized, throws `HttpRequestException`. In [`PrintJobProcessor.ReportAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs#L146), this exhausts 3 retries and dumps the attempt to disk (`_unreported.Remember`), preventing duplicate physical printing, but leaving the job unresolved on the server until the process is restarted.

> **Verbatim Code Excerpt (Lines 56-61):**
```csharp
    public async Task ReportResultAsync(ReportPrintJobResultRequest request, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync(
            $"/api/print-jobs/{Uri.EscapeDataString(request.PrintJobId)}/report-result", request, ct);
        response.EnsureSuccessStatusCode();
    }
```

---

<a id="callable-10"></a>
### Callable 10: `PrintServerClient.Dispose()`
- **Canonical Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:63:5:MethodDeclaration`
- **Exact Line Range:** Line `63`
- **Signature:** `public void Dispose() => _http.Dispose();`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Disposes the underlying `HttpClient` instance.

> **Verbatim Code Excerpt (Line 63):**
```csharp
    public void Dispose() => _http.Dispose();
```

---

## 3. Subsystem Vulnerability & Integrity Analysis

| Finding ID | Vulnerability / Communication Gap | Root Cause in `PrintServerClient.cs` | Severity | Remediation Strategy |
| :--- | :--- | :--- | :--- | :--- |
| **PRT-004** | Agent Never Re-authenticates After Session Loss | `AuthenticateAsync` is called once on startup; PIN is not retained; requests do not catch 401/403. When server restarts or sliding token expires, agent loops infinitely on 401 Unauthorized without polling or printing. | **HIGH** | Retain station PIN in `PrintServerClient`; implement a `DelegatingHandler` or 401 catch block that automatically invokes `AuthenticateAsync` and retries the failed request. |
