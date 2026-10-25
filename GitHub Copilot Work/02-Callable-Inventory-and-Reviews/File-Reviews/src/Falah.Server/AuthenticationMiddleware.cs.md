# src/Falah.Server/AuthenticationMiddleware.cs

- **Path:** `src/Falah.Server/AuthenticationMiddleware.cs`
- **Component:** `Falah.Server.AuthenticationMiddleware`, `HttpContextActorExtensions`
- **Assembly:** `Falah.Server`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `3e64ad8ef5324fe99aa19a0dead024a4afe8f99e038f8e280631b2eb8eabc5eb`
- **Auditor:** Security and Authorization Auditor
- **Total Callables:** 6
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural & Domain Responsibility Overview

`AuthenticationMiddleware` is the perimeter security gate for the Fale7 POS HTTP server. Positioned before endpoint routing in `Program.cs`, it is responsible for:
1. Extracting bearer tokens from incoming `Authorization` HTTP headers.
2. Resolving caller identity against database sessions via `SessionService.ResolveAsync`.
3. Attaching authenticated `UserEntity` instances to `context.Items["falah.actor"]`.
4. Defending against caller identity spoofing via `FindActorMismatchAsync`.

### Critical Vulnerabilities Identified:
- **SEC-001 (Critical):** `FindActorMismatchAsync` performs case-sensitive property lookup (`TryGetProperty`) matching `"actorUserId"` strictly in lowercase. ASP.NET Core Minimal API model binding is case-insensitive, allowing an attacker to submit `"ActorUserId"` in PascalCase and bypass mismatch detection entirely.
- **SEC-004 (High):** `ActorFields` omits `"closedByUserId"`. `POST /api/shifts/close` permits any user to close shifts under another cashier's identity without triggering mismatch detection.
- **Architectural Disconnect:** `HttpContextActorExtensions.Actor()` and `ActorId()` exist to enforce server-derived identity, but are not invoked by minimal API endpoints, leaving handlers dependent on client-supplied DTO properties.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- |
| `CAL-AM-001` | `src/Falah.Server/AuthenticationMiddleware.cs:20:5:ConstructorDeclaration` | [20](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs#L20) | Constructor |
| `CAL-AM-002` | `src/Falah.Server/AuthenticationMiddleware.cs:30:5:MethodDeclaration` | [30-82](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs#L30-L82) | Substantive (Security Boundary) |
| `CAL-AM-003` | `src/Falah.Server/AuthenticationMiddleware.cs:36:42:SimpleLambdaExpression` | [36](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs#L36) | Lambda Predicate |
| `CAL-AM-004` | `src/Falah.Server/AuthenticationMiddleware.cs:91:5:MethodDeclaration` | [91-133](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs#L91-L133) | Substantive (Security Boundary) |
| `CAL-AM-005` | `src/Falah.Server/AuthenticationMiddleware.cs:140:5:MethodDeclaration` | [140-142](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs#L140-L142) | Extension Method |
| `CAL-AM-006` | `src/Falah.Server/AuthenticationMiddleware.cs:144:5:MethodDeclaration` | [144](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs#L144) | Extension Method |

---

## 3. Deep Source-Grounded Callable Analysis

### Callable: src/Falah.Server/AuthenticationMiddleware.cs:20:5:ConstructorDeclaration
- **Exact Line Range:** Line 20
- **Signature:** `public AuthenticationMiddleware(RequestDelegate next)`
- **Actual Behavior & Inputs:** Stores downstream pipeline delegate `_next` in private readonly field.
- **Trust Boundaries:** Internal ASP.NET Core middleware pipeline initialization.
- **Side Effects:** None.
- **Concurrency & Lifecycle:** Singleton-like registration per middleware pipeline instance.

---

### Callable: src/Falah.Server/AuthenticationMiddleware.cs:30:5:MethodDeclaration
- **Exact Line Range:** Lines 30–82
- **Signature:** `public async Task InvokeAsync(HttpContext context, SessionService sessions)`
- **Actual Behavior & Execution Path:**
  1. Inspects `context.Request.Path`. If path does not start with `/api/` or matches `AnonymousPaths` (`/health`, `/api/auth/login-by-pin`), bypasses authentication and calls `await _next(context)`.
  2. Extracts `Authorization` header. If missing or not starting with `Bearer `, sets `token = null`.
  3. Calls `user = await sessions.ResolveAsync(token, context.RequestAborted)`.
  4. If `user is null`, halts pipeline and writes HTTP 401 Unauthorized with Arabic error message.
  5. If authenticated, stores `context.Items[ActorItemKey] = user`.
  6. Calls `FindActorMismatchAsync(context, user.Id)`. If a mismatch is returned, halts pipeline and writes HTTP 403 Forbidden (`"لا يمكن تنفيذ عملية باسم مستخدم آخر."`).
  7. Passes control downstream via `await _next(context)`.
- **Trust Boundaries & Validation:** Network perimeter. Enforces authentication on all non-exempt `/api/` paths.
- **Side Effects:** Writes HTTP 401/403 responses and modifies `context.Items`.
- **Concurrency & Failure Modes:** Executes concurrently per request thread. Relies on scoped `SessionService`.

---

### Callable: src/Falah.Server/AuthenticationMiddleware.cs:36:42:SimpleLambdaExpression
- **Exact Line Range:** Line 36
- **Signature:** `p => path.Equals(p, StringComparison.OrdinalIgnoreCase)`
- **Actual Behavior & Inputs:** Evaluates case-insensitive ordinal equality of request path against `AnonymousPaths` entries (`/health`, `/api/auth/login-by-pin`).
- **Trust Boundaries:** Internal predicate.

---

### Callable: src/Falah.Server/AuthenticationMiddleware.cs:91:5:MethodDeclaration
- **Exact Line Range:** Lines 91–133
- **Signature:** `private static async Task<string?> FindActorMismatchAsync(HttpContext context, string authenticatedUserId)`
- **Actual Behavior & Execution Path:**
  1. **Query Inspection:** For each field in `ActorFields` (`actorUserId`, `cashierId`, `createdByUserId`, `openedByUserId`, `reportedByUserId`), checks `context.Request.Query.TryGetValue(field, out var values)`. If claimed value is non-empty and `!= authenticatedUserId`, returns `field`.
  2. **Content-Type Check:** If `!context.Request.HasJsonContentType()`, returns `null`.
  3. **Body Buffering & Rewind:** Calls `context.Request.EnableBuffering()`. Reads and parses body via `JsonDocument.ParseAsync(context.Request.Body, ...)`. In a `finally` block, unconditionally resets `context.Request.Body.Position = 0` so downstream model binding can read the stream.
  4. **Body Property Matching:** Checks `document.RootElement.TryGetProperty(field, out var value)`. If value is a string and `!= authenticatedUserId`, returns `field`.
  5. Catches `JsonException` and returns `null` (delegating malformed JSON handling to endpoints).
- **VULNERABILITY FORENSICS (SEC-001 & SEC-004):**
  - **SEC-001:** `TryGetProperty` is strictly ordinal case-sensitive. Sending `"ActorUserId"` or `"ACTORUSERID"` returns `false`. The mismatch check is bypassed, but ASP.NET Core deserializes it case-insensitively into the command DTO.
  - **SEC-004:** `"closedByUserId"` is absent from `ActorFields`. Shifts can be closed under another cashier's identity.

---

### Callable: src/Falah.Server/AuthenticationMiddleware.cs:140:5:MethodDeclaration
- **Exact Line Range:** Lines 140–142
- **Signature:** `public static UserEntity Actor(this HttpContext context)`
- **Actual Behavior & Inputs:** Retrieves `context.Items[AuthenticationMiddleware.ActorItemKey]` cast to `UserEntity`. Throws `InvalidOperationException("No authenticated actor on this request.")` if null.
- **Architectural Disconnect:** Never invoked by any Minimal API endpoint or Infrastructure command handler.

---

### Callable: src/Falah.Server/AuthenticationMiddleware.cs:144:5:MethodDeclaration
- **Exact Line Range:** Line 144
- **Signature:** `public static string ActorId(this HttpContext context)`
- **Actual Behavior:** Returns `context.Actor().Id`.
- **Architectural Disconnect:** Never invoked by any Minimal API endpoint or Infrastructure command handler.

---

## 4. Verification & Recommendations

1. **Remediate SEC-001:** Replace `TryGetProperty` in `FindActorMismatchAsync` with `document.RootElement.EnumerateObject()` performing case-insensitive name comparison.
2. **Remediate SEC-004:** Append `"closedByUserId"` to `ActorFields`.
3. **Propagate Identity Authoritatively:** Refactor endpoint handlers to obtain caller identity directly from `context.ActorId()`.
4. **Static Verification:** Verified complete against `src/Falah.Server/AuthenticationMiddleware.cs`. No application source modified.


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:35:00Z
- **Source SHA-256 Verified:** `3e64ad8ef5324fe99aa19a0dead024a4afe8f99e038f8e280631b2eb8eabc5eb`
- **Callable ID Checked:** `src/Falah.Server/AuthenticationMiddleware.cs:20:5:ConstructorDeclaration`, `src/Falah.Server/AuthenticationMiddleware.cs:30:5:MethodDeclaration`, `src/Falah.Server/AuthenticationMiddleware.cs:36:42:SimpleLambdaExpression`, `src/Falah.Server/AuthenticationMiddleware.cs:91:5:MethodDeclaration`, `src/Falah.Server/AuthenticationMiddleware.cs:140:5:MethodDeclaration`, `src/Falah.Server/AuthenticationMiddleware.cs:144:5:MethodDeclaration` (6 of 6 callables checked)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Server/AuthenticationMiddleware.cs` (146 lines).
  2. Confirmed `SEC-001`: In `FindActorMismatchAsync` (lines 114-120), `document.RootElement.TryGetProperty(field, out var value)` performs strictly ordinal case-sensitive property lookup. Because ASP.NET Core Minimal API JSON deserialization is case-insensitive, submitting `"ActorUserId"` or `"ACTORUSERID"` in PascalCase bypasses mismatch detection entirely while binding to the request DTO.
  3. Confirmed `SEC-004`: `ActorFields` (lines 86-89) omits `"closedByUserId"`. Any authenticated user can call `POST /api/shifts/close` providing a different cashier's ID without triggering mismatch detection.
  4. Verified `HttpContextActorExtensions.Actor()` and `ActorId()` are defined but not consumed by endpoint handlers.
  5. All 6 canonical callable IDs, method signatures, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:38:12Z
- **Source SHA-256 Verified:** `3e64ad8ef5324fe99aa19a0dead024a4afe8f99e038f8e280631b2eb8eabc5eb`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `3e64ad8ef5324fe99aa19a0dead024a4afe8f99e038f8e280631b2eb8eabc5eb` against `src/Falah.Server/AuthenticationMiddleware.cs` (146 lines).
2. Concur with L1 analysis confirming `SEC-001` (case-sensitivity mismatch in `FindActorMismatchAsync` via `TryGetProperty`) and `SEC-004` (omission of `closedByUserId` from `ActorFields`).
3. Additional L2 Challenger Assessment:
   - In `FindActorMismatchAsync` (lines 91-138), parsing failures in request body JSON are silently swallowed, allowing non-JSON payloads to bypass actor checks completely.
   - While `context.Items["Actor"]` is populated from the authenticated session, downstream endpoint handlers across the application deserialize actor identity directly from untrusted request bodies (`request.ActorUserId`), decoupling authentication middleware validation from application authorization.
4. All 6 callables rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
