# src/Falah.Application/Pricing/PriceRuleRequests.cs

Pricing API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `0b43050f59d43e7766aff8404ae48344f1d7e5f8d6247b79a1fadc1f94f6ec97`. Working SHA-256 `0b43050f59d43e7766aff8404ae48344f1d7e5f8d6247b79a1fadc1f94f6ec97`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-16.

Symbols indexed (index is not a claim every method was reviewed): SavePriceRuleRequest@9, PriceRuleDto@14.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 9: `public sealed record SavePriceRuleRequest(`
- Line 14: `public sealed record PriceRuleDto(`

Offers specify product sets, day/time windows, integer fixed prices; tax requests specify basis points/mode. Time interpretation, overlap, nonnegative values and enabling one tax rule require handler/domain validation. Record types neither calculate nor authorize these changes.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
