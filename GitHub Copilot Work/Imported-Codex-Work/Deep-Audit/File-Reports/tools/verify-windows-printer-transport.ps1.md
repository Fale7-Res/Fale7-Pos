# tools/verify-windows-printer-transport.ps1

Operational tool side-effect and correctness review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `9597482c9b45615c98be73e3f877429fafad45982c8e9a3ad67873e4f9bd32b8`. Working SHA-256 `feb755008b5be52d24e8de63dc0eb2dd731bbf20bdbab476d4dcbbfba56f14bc`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-75.

Symbols indexed (index is not a claim every method was reviewed): configuration/script top-level.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

PRT-005: pause result is unchecked; filters all jobs by generic DocumentName Falah ticket and removes them, including preexisting application jobs. Resume occurs in finally even if removal fails. It can print or delete restaurant work despite claim no paper. Agent --test-print bypasses auth. Recommend dedicated isolated queue and capture exact submission job ID; never run on live printer.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
