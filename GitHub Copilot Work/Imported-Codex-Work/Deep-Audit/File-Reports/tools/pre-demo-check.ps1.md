# tools/pre-demo-check.ps1

Operational tool side-effect and correctness review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `860355cb970d49aefe5b3a9e0707f06e1c3df466d97e2abd404d5454dce28498`. Working SHA-256 `860355cb970d49aefe5b3a9e0707f06e1c3df466d97e2abd404d5454dce28498`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-118.

Symbols indexed (index is not a claim every method was reviewed): configuration/script top-level.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

Claims read-only but login writes a persistent Session through server. SQL queries themselves are reads; SQL Express service name is hardcoded even when SqlInstance override differs. Warm-up/timed GETs measure one PC state, not load reliability. Fix advice to drain pending jobs via file transport would mark unprinted jobs Succeeded (PRT-002). Do not execute on user-active DB during read-only audit; credentials must not appear in process/log reports.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
