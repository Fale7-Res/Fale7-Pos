# tools/install-falah-service.ps1

Operational tool side-effect and correctness review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `c588e78bec6d6d4f305745e4e26ed6bb966bf3d559c03083544c56ae5b4471fd`. Working SHA-256 `c588e78bec6d6d4f305745e4e26ed6bb966bf3d559c03083544c56ae5b4471fd`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-134.

Symbols indexed (index is not a claim every method was reviewed): configuration/script top-level.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

Administrative, mutating installer: deletes/recreates named service before publish success, publishes to external E: path by default, installs automatic SQL-dependent service and starts server (which migrates/seeds and runs backups). No rollback if publish fails after existing service removal. Health URL can answer another preexisting process. Do not run as audit check. Recommend staged publish, explicit configuration/DB verification, preserve old service and identity, verify PID-bound health.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
