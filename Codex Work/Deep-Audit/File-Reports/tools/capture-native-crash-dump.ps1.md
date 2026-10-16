# tools/capture-native-crash-dump.ps1

Operational tool side-effect and correctness review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `34411713e369216419343979366d809505134713de6df854dd9ca29780dfd63b`. Working SHA-256 `34411713e369216419343979366d809505134713de6df854dd9ca29780dfd63b`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-46.

Symbols indexed (index is not a claim every method was reviewed): configuration/script top-level.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

Launches and closes/kills selected executable, autoaccepts ProcDump EULA, writes full-memory dumps and deletes EVERY *.dmp in supplied DumpDir after clean exit. Caller-selected path has no ownership guard. Full dumps may hold tokens/customer data. Not executed. Recommend dedicated new owned directory and delete only files produced by this attempt; explicit consent for debugger, termination and sensitive dumps.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
