# tools/wer-localdumps.ps1

Operational tool side-effect and correctness review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `088e8c4c84089685d805bf8c1bc9a593067401070dbf7988862086c78e8ef567`. Working SHA-256 `088e8c4c84089685d805bf8c1bc9a593067401070dbf7988862086c78e8ef567`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-32.

Symbols indexed (index is not a claim every method was reviewed): configuration/script top-level.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

Writes HKLM per-executable WER LocalDumps settings or recursively removes them, with external E: default dump path. This is a global machine configuration change and can erase preexisting dump configuration. Not executed. Recommend snapshot/restore exact prior values and explicit approval; dump storage must be access controlled.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
