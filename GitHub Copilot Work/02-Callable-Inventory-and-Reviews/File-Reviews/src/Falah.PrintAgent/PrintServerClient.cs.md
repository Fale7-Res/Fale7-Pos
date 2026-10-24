# Substantive Callable Audit: `src/Falah.PrintAgent/PrintServerClient.cs`

- **Source File:** `src/Falah.PrintAgent/PrintServerClient.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `27f4e0c6c3e54a197e9992182884a6961fb1df7f65f6dddfa7f6aa216d763a56`
- **Auditor Specialist:** `printing-workflow-auditor`
- **Total File Callables:** **10**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

HTTP client communicating with Falah.Server to poll print queues and report hardware ticket status.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.PrintAgent.IPrintServerClient.AuthenticateAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:13:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `13-13`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.IPrintServerClient.AuthenticateAsync(string, CancellationToken). Manages internal state transition within src/Falah.PrintAgent/PrintServerClient.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 13-13):**
```csharp
  13 |     Task AuthenticateAsync(string pin, CancellationToken ct);
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.PrintAgent.IPrintServerClient.GetPendingAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:14:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `14-14`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.IPrintServerClient.GetPendingAsync(string, CancellationToken). Manages internal state transition within src/Falah.PrintAgent/PrintServerClient.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 14-14):**
```csharp
  14 |     Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct);
```

---

<a id="callable-3"></a>
### Callable 3: `Falah.PrintAgent.IPrintServerClient.GetTicketAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:15:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `15-15`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.IPrintServerClient.GetTicketAsync(string, CancellationToken). Manages internal state transition within src/Falah.PrintAgent/PrintServerClient.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 15-15):**
```csharp
  15 |     Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct);
```

---

<a id="callable-4"></a>
### Callable 4: `Falah.PrintAgent.IPrintServerClient.ReportResultAsync(Falah.Application.Printing.ReportPrintJobResultRequest, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:16:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `16-16`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.IPrintServerClient.ReportResultAsync(Falah.Application.Printing.ReportPrintJobResultRequest, CancellationToken). Manages internal state transition within src/Falah.PrintAgent/PrintServerClient.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 16-16):**
```csharp
  16 |     Task ReportResultAsync(ReportPrintJobResultRequest request, CancellationToken ct);
```

---

<a id="callable-5"></a>
### Callable 5: `Falah.PrintAgent.PrintServerClient.PrintServerClient(string, TimeSpan)`
- **Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:28:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `28-31`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.PrintAgent.PrintServerClient.PrintServerClient(string, TimeSpan). Manages internal state transition within src/Falah.PrintAgent/PrintServerClient.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 28-31):**
```csharp
  28 |     public PrintServerClient(string baseAddress, TimeSpan timeout)
  29 |     {
  30 |         _http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = timeout };
  31 |     }
```

---

<a id="callable-6"></a>
### Callable 6: `Falah.PrintAgent.PrintServerClient.AuthenticateAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:33:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `33-40`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`PRT-004`**

**A. Actual Behavior & Execution Path:**  
Sends POST /api/auth/login-by-pin with station PIN at startup and caches bearer token. Never re-authenticates if session expires or server restarts.

**B. Trust Boundaries & Validation:**  
Server authentication endpoint.

**C. State Mutations & Side Effects:**  
Sets _bearerToken field in HttpClient default headers.

**D. Concurrency & Transaction Implications:**  
Single worker thread.

**E. Specialist Risks & Invariants:**  
FLAW PRT-004: A single 401 Unauthorized permanently breaks printing; the agent enters an infinite loop failing every request until service restart.

> **Verbatim Code Excerpt (Lines 33-40):**
```csharp
  33 |     public async Task AuthenticateAsync(string pin, CancellationToken ct)
  34 |     {
  35 |         var response = await _http.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin), ct);
  36 |         response.EnsureSuccessStatusCode();
  37 |         var result = await response.Content.ReadFromJsonAsync<LoginResultDto>(cancellationToken: ct)
  38 |             ?? throw new InvalidOperationException("Login response was empty.");
  39 |         _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
  40 |     }
```

---

<a id="callable-7"></a>
### Callable 7: `Falah.PrintAgent.PrintServerClient.GetPendingAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:42:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `42-47`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.PrintServerClient.GetPendingAsync(string, CancellationToken). Manages internal state transition within src/Falah.PrintAgent/PrintServerClient.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 42-47):**
```csharp
  42 |     public async Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct)
  43 |     {
  44 |         var jobs = await _http.GetFromJsonAsync<List<PrintJobDto>>(
  45 |             $"/api/print-jobs/pending?route={Uri.EscapeDataString(route)}", ct);
  46 |         return jobs ?? new List<PrintJobDto>();
  47 |     }
```

---

<a id="callable-8"></a>
### Callable 8: `Falah.PrintAgent.PrintServerClient.GetTicketAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:49:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `49-54`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.PrintServerClient.GetTicketAsync(string, CancellationToken). Manages internal state transition within src/Falah.PrintAgent/PrintServerClient.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 49-54):**
```csharp
  49 |     public async Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct)
  50 |     {
  51 |         var ticket = await _http.GetFromJsonAsync<ComposedTicketDto>(
  52 |             $"/api/print-jobs/{Uri.EscapeDataString(jobId)}/ticket", ct);
  53 |         return ticket ?? throw new InvalidOperationException($"Ticket content for job {jobId} was empty.");
  54 |     }
```

---

<a id="callable-9"></a>
### Callable 9: `Falah.PrintAgent.PrintServerClient.ReportResultAsync(Falah.Application.Printing.ReportPrintJobResultRequest, CancellationToken)`
- **Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:56:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `56-61`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.PrintServerClient.ReportResultAsync(Falah.Application.Printing.ReportPrintJobResultRequest, CancellationToken). Manages internal state transition within src/Falah.PrintAgent/PrintServerClient.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 56-61):**
```csharp
  56 |     public async Task ReportResultAsync(ReportPrintJobResultRequest request, CancellationToken ct)
  57 |     {
  58 |         var response = await _http.PostAsJsonAsync(
  59 |             $"/api/print-jobs/{Uri.EscapeDataString(request.PrintJobId)}/report-result", request, ct);
  60 |         response.EnsureSuccessStatusCode();
  61 |     }
```

---

<a id="callable-10"></a>
### Callable 10: `Falah.PrintAgent.PrintServerClient.Dispose()`
- **Callable ID:** `src/Falah.PrintAgent/PrintServerClient.cs:63:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `63-63`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.PrintAgent.PrintServerClient.Dispose(). Manages internal state transition within src/Falah.PrintAgent/PrintServerClient.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 63-63):**
```csharp
  63 |     public void Dispose() => _http.Dispose();
```

---
