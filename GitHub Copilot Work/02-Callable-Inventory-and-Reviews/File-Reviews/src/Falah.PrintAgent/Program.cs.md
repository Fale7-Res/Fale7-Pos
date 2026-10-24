# src/Falah.PrintAgent/Program.cs

Print agent lifecycle.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `3d2abe5009bb8b4de474f8d85d90bbc30e23d80874d947feee45b76e6cb249da`. Working SHA-256 `b8bcbfaf0e1583c623238dc37db92b843237af6a50f17d70211d76930d75d453`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-158.

Symbols indexed (index is not a claim every method was reviewed): configuration/script top-level.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

Configuration validation precedes both normal and --test-print paths. Test mode sends directly to configured transport without login and can physically print: never treat it as a read-only health check. Normal mode obtains Global route mutex on this machine, authenticates once, creates route journal, polls with retry-on-exception and cancellation on Ctrl+C. Mutex is no cross-machine lease (PRT-001). No installed print-agent service is configured here. Authentication failure exits; later HTTP failures keep polling. Default config selects physical printing, so do not launch during audit.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
