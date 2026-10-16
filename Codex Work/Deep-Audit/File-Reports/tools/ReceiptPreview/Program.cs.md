# tools/ReceiptPreview/Program.cs

Operational tool side-effect and correctness review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `c484d32e941af42d914cc6644dccc41b064c2bbd38746e472a075db661cd98f1`. Working SHA-256 `c484d32e941af42d914cc6644dccc41b064c2bbd38746e472a075db661cd98f1`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-190.

Symbols indexed (index is not a claim every method was reviewed): configuration/script top-level.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

Record/configuration/script top-level or lambdas; see source range and analysis.

Offline synthetic fixtures use production PrintRules and BitmapTicketRenderer then decode raster bytes to PNG; independent ZXing checks the configured QR. This validates fixture raster representation, not a real persisted order or physical ink. Uses 203 dpi assumption, pixel loops and an output directory supplied by argument. Covers receipt/reprint/shift/expense only, not kitchen/delivery/pickup variants. Do not substitute fixture preview for live-order evidence. Safe relative to DB/printer after inspection, but not executed by this agent.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
