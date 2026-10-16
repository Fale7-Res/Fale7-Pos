# src/Falah.Application/Printing/TicketElementMapping.cs

Printing API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `247fe0b899d9ca3697083c2b32a691172ff0dd50801a5f0ea6df246e76438efc`. Working SHA-256 `1f465ebdd0d8275f7d51b24f3603fa9042f70aad6b1fe3c1bb92910a6e39eef3`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-83.

Symbols indexed (index is not a claim every method was reviewed): used@9, TicketElementMapping@15.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 23: `public static TicketElementDto ToDto(TicketElement element) => element switch`
- Line 38: `public static TicketElement ToDomain(TicketElementDto dto) => dto.Kind switch`
- Line 52: `private static string AlignToWire(TicketAlign a) => a switch`
- Line 57: `private static TicketAlign AlignFromWire(string a) => a switch`
- Line 62: `private static string SizeToWire(TicketTextSize s) => s switch`
- Line 68: `private static TicketTextSize SizeFromWire(string s) => s switch`
- Line 74: `private static string RuleToWire(TicketRuleStyle r) => r switch`
- Line 79: `private static TicketRuleStyle RuleFromWire(string r) => r switch`

Ticket wire types carry content rather than device bytes. Shared mapping handles text/row/rule/cut/QR and enum strings in both directions; unknown elements fall back to text. ComposedTicketDto comment omits qr despite real mapping support. Create/report request IDs distinguish queue creation from attempt reporting. Boolean Succeeded cannot prove physical output; PRT-002.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
