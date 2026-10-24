# src/Falah.PrintAgent/PrintServerClient.cs

Authenticated HTTP printing client.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `27f4e0c6c3e54a197e9992182884a6961fb1df7f65f6dddfa7f6aa216d763a56`. Working SHA-256 `27f4e0c6c3e54a197e9992182884a6961fb1df7f65f6dddfa7f6aa216d763a56`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-64.

Symbols indexed (index is not a claim every method was reviewed): so@8, IPrintServerClient@11, PrintServerClient@24.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 28: `public PrintServerClient(string baseAddress, TimeSpan timeout)`
- Line 33: `public async Task AuthenticateAsync(string pin, CancellationToken ct)`
- Line 42: `public async Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct)`
- Line 49: `public async Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct)`
- Line 56: `public async Task ReportResultAsync(ReportPrintJobResultRequest request, CancellationToken ct)`
- Line 63: `public void Dispose() => _http.Dispose();`

AuthenticateAsync obtains one bearer session and stores it on HttpClient; GetPendingAsync escapes route, GetTicketAsync and ReportResultAsync escape IDs and enforce HTTP status. There is no 401 refresh/relogin: expired/deactivated session or restarted/deleted session can make the poll loop fail indefinitely. Recommend controlled reauthentication with credential policy and visible agent health, never automatic duplicate send. Base URL and timeout are constructor supplied; secret values excluded from reports.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
