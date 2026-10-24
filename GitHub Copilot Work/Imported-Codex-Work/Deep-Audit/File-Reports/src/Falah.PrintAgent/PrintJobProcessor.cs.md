# src/Falah.PrintAgent/PrintJobProcessor.cs

Queue processing and durable attempt reporting.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `31051c0c991f43c10d1ab5335195e82db8b7f03a2b59f42707b35831a4d5d979`. Working SHA-256 `2ef3ef9c1d54c7a16bfe506d95e2803aa1970a09e4fbecdf473a1b82fc3ea640`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-170.

Symbols indexed (index is not a claim every method was reviewed): PrintJobProcessor@28, carry@163.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 46: `public PrintJobProcessor(`
- Line 68: `public async Task<int> DrainAsync(string route, CancellationToken ct)`
- Line 81: `public async Task ProcessAsync(PrintJobDto job, CancellationToken ct)`
- Line 131: `private async Task ReportAsync(ReportPrintJobResultRequest request, CancellationToken ct)`
- Line 165: `public static ComposedTicket ToDomain(ComposedTicketDto dto) => new(`

DrainAsync handles jobs serially from a snapshot. ProcessAsync re-reports journaled outcomes, maps server DTOs to production renderers, journals uncertain BEFORE SendAsync, then journals success/failure and reports. ReportAsync retries three times using one attempt ID, persists failures, and avoids ordinary report-retry duplicate output. Cross-machine duplicate snapshot risk is PRT-001; terminal status means transport acceptance PRT-002. A persisted pre-send uncertain entry is deliberately conservative and can mark Failed despite zero bytes; manual inspection is required. A crash after server report but before local Forget leaves a stale local entry, normally harmless because succeeded jobs leave Pending. No claim/lease check occurs immediately before send.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
