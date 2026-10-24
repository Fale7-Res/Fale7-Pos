# tools/repro-native-crash.ps1

Operational tool side-effect and correctness review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `f9ffbaee946ae7848cf90589a955b38019481b0e762ae90bd953b6f406f7db8a`. Working SHA-256 `f9ffbaee946ae7848cf90589a955b38019481b0e762ae90bd953b6f406f7db8a`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-94.

Symbols indexed (index is not a claim every method was reviewed): configuration/script top-level.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

Launches executable repeatedly and closes/kills it; reads Application event log. Despite no sign-in, startup may connect/poll and executable input is unconstrained. Useful phase/exit-code separation, but graceful mode forcibly kills after timeout. Not executed while user application active; use isolated binaries/configuration and retained logs.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
