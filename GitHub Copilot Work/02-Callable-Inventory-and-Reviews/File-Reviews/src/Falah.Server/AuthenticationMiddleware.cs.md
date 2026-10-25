# Substantive Callable Audit: `src/Falah.Server/AuthenticationMiddleware.cs`

- **Source File:** `src/Falah.Server/AuthenticationMiddleware.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `3e64ad8ef5324fe99aa19a0dead024a4afe8f99e038f8e280631b2eb8eabc5eb`
- **Auditor Specialist:** `security-auditor`
- **Total File Callables:** **6**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

`AuthenticationMiddleware` sits at the entrance of the ASP.NET Core request pipeline for all HTTP endpoints. It is responsible for resolving bearer tokens into authoritative `UserEntity` instances via `SessionService` and populating `context.Items["falah.actor"]`.

Forensic static source review reveals **two critical security vulnerabilities**:
1. **Finding `SEC-001` (High):** `FindActorMismatchAsync` performs case-sensitive property lookup on JSON requests, allowing PascalCase `"ActorUserId"` bypass.
2. **Finding `SEC-004` (High):** `ActorFields` omits `"closedByUserId"`, permitting identity spoofing during shift closure.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

### Callable 1: `AuthenticationMiddleware(RequestDelegate next)`
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:20:20:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `20-20`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`
- **A. Actual Behavior & Execution Path:** Injects the next HTTP request delegate `_next` into a private readonly field.
- **B. Trust Boundaries & Validation:** Internal ASP.NET Core dependency injection container.
- **C. State Mutations & Side Effects:** Assigns private field. Zero external state mutations.
- **D. Concurrency & Transaction Implications:** Executed once at application startup during pipeline construction; immutable at runtime.
- **E. Specialist Risks & Invariants:** None.

```csharp
    public AuthenticationMiddleware(RequestDelegate next) => _next = next;
```

---

### Callable 2: `InvokeAsync(HttpContext context, SessionService sessions)`
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:30:82:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `30-82`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`
- **A. Actual Behavior & Execution Path:**
  1. Inspects `context.Request.Path`. If path does not start with `/api/` or matches `AnonymousPaths` (`/health`, `/api/auth/login-by-pin`), passes execution immediately to `_next(context)`.
  2. Extracts bearer token from `Authorization` header (`Bearer <token>`).
  3. Invokes `sessions.ResolveAsync(token, context.RequestAborted)` to validate token against database.
  4. If resolution fails, writes HTTP 401 Unauthorized with Arabic error JSON and halts pipeline.
  5. If valid, attaches resolved `UserEntity` to `context.Items[ActorItemKey]`.
  6. Calls `FindActorMismatchAsync(context, user.Id)`. If mismatch detected, halts and returns HTTP 403 Forbidden.
  7. Invokes downstream pipeline `_next(context)`.
- **B. Trust Boundaries & Validation:** Network boundary. Treats all request headers, query parameters, and body claims as untrusted until validated against the server session store.
- **C. State Mutations & Side Effects:** Populates `context.Items[ActorItemKey]`. Mutates HTTP response stream on 401/403 errors.
- **D. Concurrency & Transaction Implications:** Executes concurrently across all incoming HTTP requests. Relies on scoped EF Core context in `SessionService`.
- **E. Specialist Risks & Invariants:** Does not enforce role-based permissions (RBAC); leaves permission checks to minimal API endpoints.

```csharp
    public async Task InvokeAsync(HttpContext context, SessionService sessions)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Swagger and static files are not the business API and carry no actor.
        var isApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
        if (!isApi || AnonymousPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : null;

        var user = await sessions.ResolveAsync(token, context.RequestAborted);
        if (user is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "انتهت الجلسة أو لم يتم تسجيل الدخول. سجّل الدخول مرة أخرى.",
            });
            return;
        }

        context.Items[ActorItemKey] = user;

        // Requests still carry the acting user's id in their query or body - hundreds of call sites
        // and two shipped clients are built that shape, and rewriting them all would be a far
        // larger change than the one that closes the hole. So the id is kept, and disbelieved:
        // whatever it claims must match the session that was actually presented.
        //
        // Deliberately NOT checked: ManagerApproverUserId and ApproverUserId. Those name a SECOND
        // person - a manager approving on a cashier's screen - and forcing them to equal the
        // session would break manager approval entirely.
        var mismatch = await FindActorMismatchAsync(context, user.Id);
        if (mismatch is not null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "لا يمكن تنفيذ عملية باسم مستخدم آخر.",
            });
            return;
        }

        await _next(context);
    }
```

---

### Callable 3: Lambda in `InvokeAsync` (Line 36)
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:36:36:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `36-36`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`
- **A. Actual Behavior & Execution Path:** Pure in-memory string equality comparison predicate matching request path against `AnonymousPaths`.
- **B. Trust Boundaries & Validation:** In-memory string comparison using `StringComparison.OrdinalIgnoreCase`.
- **C. State Mutations & Side Effects:** Pure predicate function. Zero state mutations, zero database interaction, zero side effects.
- **D. Concurrency & Transaction Implications:** Thread-safe pure function.
- **E. Specialist Risks & Invariants:** None.

```csharp
        if (!isApi || AnonymousPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
```

---

### Callable 4: `FindActorMismatchAsync(HttpContext context, string authenticatedUserId)`
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:91:133:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `91-133`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`SEC-001`**, **`SEC-004`**
- **A. Actual Behavior & Execution Path:**
  Inspects `context.Request.Query` and JSON request body against `ActorFields`:
  `('actorUserId', 'cashierId', 'createdByUserId', 'openedByUserId', 'reportedByUserId')`.
  Enables request body buffering via `context.Request.EnableBuffering()`, parses JSON via `JsonDocument.ParseAsync`, and compares string value with authenticated user ID.
- **B. Trust Boundaries & Validation:** Untrusted HTTP request query and body.
- **C. State Mutations & Side Effects:** Buffers request body and resets stream position to 0.
- **D. Concurrency & Transaction Implications:** Thread-safe per request stream.
- **E. Specialist Risks & Invariants:**
  - **SEC-001 (High):** `document.RootElement.TryGetProperty(field, out var value)` is ordinal case-sensitive. Sending PascalCase `"ActorUserId"` bypasses mismatch detection entirely, allowing downstream handlers like `UpdateUserPermissionsHandler` to be fooled.
  - **SEC-004 (High):** `ActorFields` omits `"closedByUserId"`. `POST /api/shifts/close` permits any user to close shifts under another cashier's identity.

```csharp
    private static async Task<string?> FindActorMismatchAsync(HttpContext context, string authenticatedUserId)
    {
        foreach (var field in ActorFields)
        {
            if (context.Request.Query.TryGetValue(field, out var values))
            {
                var claimed = values.ToString();
                if (!string.IsNullOrEmpty(claimed) && claimed != authenticatedUserId) return field;
            }
        }

        if (!context.Request.HasJsonContentType()) return null;

        // Buffered so the endpoint's own model binding can still read the body afterwards.
        context.Request.EnableBuffering();
        try
        {
            using var document = await System.Text.Json.JsonDocument.ParseAsync(
                context.Request.Body, cancellationToken: context.RequestAborted);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return null;

            foreach (var field in ActorFields)
            {
                if (document.RootElement.TryGetProperty(field, out var value)
                    && value.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var claimed = value.GetString();
                    if (!string.IsNullOrEmpty(claimed) && claimed != authenticatedUserId) return field;
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Malformed JSON is the endpoint's problem to report, not this middleware's.
            return null;
        }
        finally
        {
            context.Request.Body.Position = 0;
        }

        return null;
    }
```

---

### Callable 5: `Actor(this HttpContext context)`
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:140:142:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `140-142`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`
- **A. Actual Behavior & Execution Path:** Extension method retrieving `context.Items["falah.actor"]` as `UserEntity`. Throws `InvalidOperationException` if null.
- **B. Trust Boundaries & Validation:** Internal server context extension.
- **C. State Mutations & Side Effects:** None; read-only dictionary lookup.
- **D. Concurrency & Transaction Implications:** Thread-safe.
- **E. Specialist Risks & Invariants:** Throws if called on anonymous routes.

```csharp
    public static Falah.Infrastructure.Persistence.Entities.UserEntity Actor(this HttpContext context) =>
        context.Items[AuthenticationMiddleware.ActorItemKey] as Falah.Infrastructure.Persistence.Entities.UserEntity
            ?? throw new InvalidOperationException("No authenticated actor on this request.");
```

---

### Callable 6: `ActorId(this HttpContext context)`
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:144:144:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `144-144`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`
- **A. Actual Behavior & Execution Path:** Shorthand property accessor returning `context.Actor().Id`.
- **B. Trust Boundaries & Validation:** Internal server context extension.
- **C. State Mutations & Side Effects:** None.
- **D. Concurrency & Transaction Implications:** Thread-safe.
- **E. Specialist Risks & Invariants:** None.

```csharp
    public static string ActorId(this HttpContext context) => context.Actor().Id;
```
