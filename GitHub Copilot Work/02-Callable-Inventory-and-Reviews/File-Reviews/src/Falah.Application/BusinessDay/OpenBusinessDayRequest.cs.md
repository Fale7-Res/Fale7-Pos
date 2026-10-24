# src/Falah.Application/BusinessDay/OpenBusinessDayRequest.cs

BusinessDay API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `c31ea5e804c08f8d4326394f5cc096e33661b722f859c4d8793b39484d85882d`. Working SHA-256 `c31ea5e804c08f8d4326394f5cc096e33661b722f859c4d8793b39484d85882d`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-11.

Symbols indexed (index is not a claim every method was reviewed): OpenBusinessDayRequest@9, OpenBusinessDayResult@11.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 9: `public sealed record OpenBusinessDayRequest(string RequestId, string OpenedByUserId, DateOnly? BusinessDate = null);`
- Line 11: `public sealed record OpenBusinessDayResult(string BusinessDayId, string BusinessDate, string Status, bool WasReplayed);`

Commands carry request IDs for lifecycle changes and snapshot metadata; cash counts carry denomination rows plus client expected amount. DTOs do not enforce nonnegative quantities, authoritative expected balance, or close consistency; handlers must. No transaction exists in the contract itself.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
