# src/Falah.Application/Users/UserSummaryDto.cs

Users API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `ccd44316d3741e88dbdb98bff379ae22d6bb626e4c9331153b2c22fe83ecfafd`. Working SHA-256 `ccd44316d3741e88dbdb98bff379ae22d6bb626e4c9331153b2c22fe83ecfafd`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-13.

Symbols indexed (index is not a claim every method was reviewed): UserSummaryDto@3, LoginByPinRequest@7, LoginResultDto@12.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 3: `public sealed record UserSummaryDto(string Id, string Name, string Role, IReadOnlyList<string> Permissions);`
- Line 7: `public sealed record LoginByPinRequest(string Pin);`
- Line 12: `public sealed record LoginResultDto(`

Login result adds bearer token only to successful authentication response, user detail excludes PIN. Creation accepts initial credential and permissions; PIN changes have self/admin shapes. Treat credential requests as sensitive. DTOs rely on handler privilege and uniqueness enforcement; never serialize request records into ordinary logs.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
