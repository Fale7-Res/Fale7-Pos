# src/Falah.PrintAgent/AgentOptions.cs

Station configuration and transport construction.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `7b940f8aed07130a6af1cf621aa3a1dfcf5c1174d3b34051ad11df2076802a5f`. Working SHA-256 `678b4396c34d4b3885fa2d17087b8ef31c5c303820da18c97cb3cd4ec31cb5a7`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-139.

Symbols indexed (index is not a claim every method was reviewed): AgentOptions@11.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 79: `public Encoding ResolveEncoding()`
- Line 86: `public ITicketTransport CreateTransport() => Transport.ToLowerInvariant() switch`
- Line 97: `public IReadOnlyList<string> Validate()`
- Line 130: `public static AgentOptions Load(string path)`

Validate canonicalizes enum route aliases and checks required identity/transport fields, but not URI syntax, timeout range, printer port, image dimensions or margins. Invalid values can fail after the configuration gate. ResolveEncoding registers legacy encodings; CreateTransport selects a single destination. Windows-only bitmap mode silently falls back to text on non-Windows. Recommend reject incompatible render/platform combinations and bound dimensions before processing. Tests: route alias validation exists in PrintAgentTests; invalid dimensions/timeouts and cross-platform Arabic fallback need coverage.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
