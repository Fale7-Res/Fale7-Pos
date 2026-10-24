# src/Falah.Server/AuthenticationMiddleware.cs

Bearer resolution and request actor consistency.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `11672919bc8579df8e305e0a96e70fb8f4f1624ec08d3d0a69bc28b39753fa07`. Working SHA-256 `3e64ad8ef5324fe99aa19a0dead024a4afe8f99e038f8e280631b2eb8eabc5eb`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-145.

Symbols indexed (index is not a claim every method was reviewed): AuthenticationMiddleware@17, HttpContextActorExtensions@136.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 20: `public AuthenticationMiddleware(RequestDelegate next) => _next = next;`
- Line 30: `public async Task InvokeAsync(HttpContext context, SessionService sessions)`
- Line 91: `private static async Task<string?> FindActorMismatchAsync(HttpContext context, string authenticatedUserId)`
- Line 140: `public static Falah.Infrastructure.Persistence.Entities.UserEntity Actor(this HttpContext context) =>`
- Line 144: `public static string ActorId(this HttpContext context) => context.Actor().Id;`

API calls except login require session resolution. Query/body claims are compared against a fixed actor-field list; property lookup is exact case while endpoint JSON binding is case-insensitive, and closedByUserId is absent. This is an identity consistency mechanism, not endpoint permission enforcement. JSON body is buffered, parsed once here and again by endpoint, and rewound. No explicit middleware body-size cap. Security coordinator independently owns validation of casing/missing-field consequences. No active exploit performed.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
