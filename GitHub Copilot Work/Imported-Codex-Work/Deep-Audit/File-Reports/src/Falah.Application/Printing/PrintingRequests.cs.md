# src/Falah.Application/Printing/PrintingRequests.cs

Printing API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `132837514fd334d8066193fc04ce8a57759246c32df73b4de901d809eea6237d`. Working SHA-256 `b49f26ad9bcd56d10236fdd5f0e6c4000b86492e6f96cd92b57a2e4055111d3b`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-65.

Symbols indexed (index is not a claim every method was reviewed): CreatePrintJobsRequest@15, ReprintDocumentRequest@26, PrintJobDto@29, PrintRouteStatusDto@40, PrintingOverviewDto@44, CreatePrintJobsResult@49, ReportPrintJobResultRequest@56, PrintJobAttemptDto@58, PrintJobDetailsDto@60, PrintingConflictException@62.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 15: `public sealed record CreatePrintJobsRequest(`
- Line 26: `public sealed record ReprintDocumentRequest(`
- Line 29: `public sealed record PrintJobDto(`
- Line 40: `public sealed record PrintRouteStatusDto(`
- Line 44: `public sealed record PrintingOverviewDto(`
- Line 49: `public sealed record CreatePrintJobsResult(IReadOnlyList<PrintJobDto> Jobs, bool WasReplayed);`
- Line 56: `public sealed record ReportPrintJobResultRequest(string RequestId, string PrintJobId, bool Succeeded, string? Error, string ReportedByUserId);`
- Line 58: `public sealed record PrintJobAttemptDto(DateTimeOffset AttemptedAtUtc, bool Succeeded, string? Error, string ReportedBy);`
- Line 60: `public sealed record PrintJobDetailsDto(PrintJobDto Job, IReadOnlyList<PrintJobAttemptDto> Attempts);`
- Line 64: `public PrintingConflictException(string message) : base(message) { }`

Ticket wire types carry content rather than device bytes. Shared mapping handles text/row/rule/cut/QR and enum strings in both directions; unknown elements fall back to text. ComposedTicketDto comment omits qr despite real mapping support. Create/report request IDs distinguish queue creation from attempt reporting. Boolean Succeeded cannot prove physical output; PRT-002.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
