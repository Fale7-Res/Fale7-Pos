# src/Falah.Application/Pricing/TaxRuleRequests.cs

Pricing API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `455302a91ad1cb54ee244d2a0fda5aea41b780cc76fb0e6b9daa685f46080cc9`. Working SHA-256 `455302a91ad1cb54ee244d2a0fda5aea41b780cc76fb0e6b9daa685f46080cc9`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-16.

Symbols indexed (index is not a claim every method was reviewed): CreateTaxRuleRequest@10, CreateTaxRuleResult@12, UpdateTaxRuleRequest@14, TaxRuleDto@16.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 10: `public sealed record CreateTaxRuleRequest(string RequestId, string Name, int RateBasisPoints, string Mode, bool Enabled, string ActorUserId);`
- Line 12: `public sealed record CreateTaxRuleResult(string TaxRuleId, bool WasReplayed);`
- Line 14: `public sealed record UpdateTaxRuleRequest(string TaxRuleId, string Name, int RateBasisPoints, string Mode, bool Enabled, string ActorUserId, string? RequestId = null);`
- Line 16: `public sealed record TaxRuleDto(string Id, string Name, int RateBasisPoints, string Mode, bool Enabled);`

Offers specify product sets, day/time windows, integer fixed prices; tax requests specify basis points/mode. Time interpretation, overlap, nonnegative values and enabling one tax rule require handler/domain validation. Record types neither calculate nor authorize these changes.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
