# src/Falah.Application/Users/UserManagementRequests.cs

Users API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `675c03a175e0e751c99162015dc8fa0a8e73745185638bad7b1b76f388245e0c`. Working SHA-256 `675c03a175e0e751c99162015dc8fa0a8e73745185638bad7b1b76f388245e0c`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-31.

Symbols indexed (index is not a claim every method was reviewed): CreateUserRequest@8, CreateUserResult@11, SetUserActiveRequest@17, ChangePinRequest@23, UpdateUserPermissionsRequest@28, UserDetailsDto@31.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 8: `public sealed record CreateUserRequest(`
- Line 11: `public sealed record CreateUserResult(string UserId, bool WasReplayed);`
- Line 17: `public sealed record SetUserActiveRequest(string UserId, bool Active, string? Reason, string ActorUserId);`
- Line 23: `public sealed record ChangePinRequest(string UserId, string? CurrentPin, string NewPin, string ActorUserId);`
- Line 28: `public sealed record UpdateUserPermissionsRequest(`
- Line 31: `public sealed record UserDetailsDto(string Id, string Name, string Role, bool Active, IReadOnlyList<string> Permissions);`

Login result adds bearer token only to successful authentication response, user detail excludes PIN. Creation accepts initial credential and permissions; PIN changes have self/admin shapes. Treat credential requests as sensitive. DTOs rely on handler privilege and uniqueness enforcement; never serialize request records into ordinary logs.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
