# src/Falah.Application/Audit/AuditEventDto.cs

Audit API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `a64ead29501f69803e7f23a1be61a9611f2cbdcfb13c9f9eb65e081396b77f4a`. Working SHA-256 `a64ead29501f69803e7f23a1be61a9611f2cbdcfb13c9f9eb65e081396b77f4a`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-6.

Symbols indexed (index is not a claim every method was reviewed): AuditEventDto@3.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 3: `public sealed record AuditEventDto(`

Read-only audit event carries actor, operation, reason and optional old/new values. Rendering/escaping and sensitive-field redaction remain consumer responsibilities; this file writes nothing.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
