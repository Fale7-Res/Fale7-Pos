# src/Falah.Server/Properties/launchSettings.json

Backend deployment configuration.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `7b1391e4768c6b048fb7175781b6ad4625f91b4594a4d302cc326c77e7f371b6`. Working SHA-256 `d9fb432339a204fd5864f25432605f4007ccfc41bf7e73979b1329129f924e46`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-41.

Symbols indexed (index is not a claim every method was reviewed): configuration/script top-level.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

Server project targets net8.0 and references actual Infrastructure/Domain/Application, Windows service support, EF design and Swagger. Configuration uses localhost HTTP 5151 and automatic backups to an external E: directory; Development overrides logging only. launchSettings is development-only and includes optional HTTPS. Falah.Server.http still targets nonexistent weatherforecast endpoint: stale smoke-test artifact. No file executed; no database/system settings changed.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
