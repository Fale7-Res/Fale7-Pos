# src/Falah.PrintAgent/appsettings.json

Print-agent shipped configuration.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `2a7bb865b7d240ca6d3a506571b61538ffd3d6337149296c4a2933afcd2fa8da`. Working SHA-256 `bc63c3468c3f467713eb490613ee7396a4e8be41630ce2c94edb34058c9f9b15`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-21.

Symbols indexed (index is not a claim every method was reviewed): configuration/script top-level.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

Inspected all fields; configuration contains a reusable demo credential (value intentionally redacted), physical Windows queue transport, bitmap mode 576 dots/24-dot margins, and localhost HTTP server. It is unsafe as a printer-free smoke-test default. Keep per-environment credentials outside distributable source and select file transport explicitly for simulations; do not mistake file simulation for physical proof.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
