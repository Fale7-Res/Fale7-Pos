# src/Falah.Server/Falah.Server.http

Backend deployment configuration.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `06a2f5989583127808b1fdfdd5b475ffcb170a92f6214b3fbee599ac3753fe3d`. Working SHA-256 `4703465669e023af67d69f30d6e1bb9310040ae67fb4fd3a089d5911674b5626`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-6.

Symbols indexed (index is not a claim every method was reviewed): configuration/script top-level.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

Server project targets net8.0 and references actual Infrastructure/Domain/Application, Windows service support, EF design and Swagger. Configuration uses localhost HTTP 5151 and automatic backups to an external E: directory; Development overrides logging only. launchSettings is development-only and includes optional HTTPS. Falah.Server.http still targets nonexistent weatherforecast endpoint: stale smoke-test artifact. No file executed; no database/system settings changed.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
