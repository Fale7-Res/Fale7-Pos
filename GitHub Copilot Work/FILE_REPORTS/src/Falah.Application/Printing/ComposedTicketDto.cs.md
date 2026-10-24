# src/Falah.Application/Printing/ComposedTicketDto.cs

Printing API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `c5ba69716c9e9f44a21d732295b2ac9b9bb682c12a122c006a7b16bc19dbb168`. Working SHA-256 `0d7923d24b034f9c7f651a8830f7694a060a413f181a5588e9cbe1a86139875c`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-34.

Symbols indexed (index is not a claim every method was reviewed): TicketCellDto@5, hierarchy@11, crosses@12, TicketElementDto@20, ComposedTicketDto@34.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 5: `public sealed record TicketCellDto(string Text, int WidthPercent, string Align = "start", bool Bold = false);`
- Line 20: `public sealed record TicketElementDto(`
- Line 34: `public sealed record ComposedTicketDto(string Route, string OrderId, string OrderNumber, IReadOnlyList<TicketElementDto> Lines);`

Ticket wire types carry content rather than device bytes. Shared mapping handles text/row/rule/cut/QR and enum strings in both directions; unknown elements fall back to text. ComposedTicketDto comment omits qr despite real mapping support. Create/report request IDs distinguish queue creation from attempt reporting. Boolean Succeeded cannot prove physical output; PRT-002.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
