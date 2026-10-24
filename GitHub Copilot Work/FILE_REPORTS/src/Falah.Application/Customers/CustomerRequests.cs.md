# src/Falah.Application/Customers/CustomerRequests.cs

Customers API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `e4472a17671db5cb1c78c870a16848d65399766dffb63d38270121c3f54454a7`. Working SHA-256 `e4472a17671db5cb1c78c870a16848d65399766dffb63d38270121c3f54454a7`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-18.

Symbols indexed (index is not a claim every method was reviewed): UpsertCustomerRequest@9, CustomerAddressDto@13, CustomerDto@17.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 9: `public sealed record UpsertCustomerRequest(`
- Line 13: `public sealed record CustomerAddressDto(`
- Line 17: `public sealed record CustomerDto(`

Phone-content upsert has no request ID and stores structured address/service text. Personal data flows across API. Concurrent phone/address uniqueness and normalization require handler/database enforcement. No source-side query or write implementation exists in this contract.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
