# src/Falah.PrintAgent/Falah.PrintAgent.csproj

Build/tool dependency manifest review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `75087030f1ad851ff320203969e7804f7af86171926bc482c81b79faaa226044`. Working SHA-256 `b5e6fadcc1fe27aefff4101601931076b8478c427f2cfe2bb6f90450e73802a3`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-42.

Symbols indexed (index is not a claim every method was reviewed): configuration/script top-level.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

Inspected target framework, references, package versions and copy-to-output declarations. Application targets net8.0 and references Domain; PrintAgent targets net8.0 with Windows-only System.Drawing functionality, serial/encoding and QRCoder packages; ReceiptPreview targets net8.0-windows with ZXing. dotnet-ef manifest pins 8.0.11 and no roll-forward. No restore/install executed here. Manifest alone provides no vulnerability verdict or runtime proof.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
