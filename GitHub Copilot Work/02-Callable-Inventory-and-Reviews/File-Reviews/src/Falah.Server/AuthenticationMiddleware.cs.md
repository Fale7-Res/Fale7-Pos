# Security Review: src/Falah.Server/AuthenticationMiddleware.cs

## File Overview
- **Canonical Source Path:** `src/Falah.Server/AuthenticationMiddleware.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Source SHA-256:** `3e64ad8ef5324fe99aa19a0dead024a4afe8f99e038f8e280631b2eb8eabc5eb`
- **Auditor:** DEEP_SECURITY_SPECIALIST
- **Review Date:** 2026-10-25T10:10:00Z
- **Total Callables:** 6
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## Callable Reviews

### AuthenticationMiddleware..ctor — Line 20
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:20:5:ConstructorDeclaration`
- **Signature:** `public AuthenticationMiddleware(RequestDelegate next)`
- **Behavior:** Captures the downstream HTTP request delegate `_next` in a private readonly field.
- **Security Analysis:** Standard ASP.NET Core middleware plumbing.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### AuthenticationMiddleware.InvokeAsync — Lines 30-82
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:30:5:MethodDeclaration`
- **Signature:** `public async Task InvokeAsync(HttpContext context, SessionService sessions)`
- **Behavior:**
  1. Inspects `context.Request.Path`. If path does not start with `/api/` or matches `AnonymousPaths` (`/health`, `/api/auth/login-by-pin`), skips authentication and invokes `await _next(context)`.
  2. Parses the `Authorization` header. If missing or not starting with `"Bearer "`, sets `token = null`.
  3. Resolves the token via `user = await sessions.ResolveAsync(token, context.RequestAborted)`.
  4. If `user is null`, halts execution and writes HTTP 401 Unauthorized with JSON error message `"انتهت الجلسة أو لم يتم تسجيل الدخول. سجّل الدخول مرة أخرى."`.
  5. If authenticated, attaches user entity to `context.Items[ActorItemKey] = user`.
  6. Evaluates `mismatch = await FindActorMismatchAsync(context, user.Id)`.
  7. If a mismatch is detected, halts execution and writes HTTP 403 Forbidden with JSON error message `"لا يمكن تنفيذ عملية باسم مستخدم آخر."`.
  8. Passes request to downstream pipeline via `await _next(context)`.
- **Security Analysis:** Perimeter authentication enforcement point. Establishes server-verified identity in `context.Items[ActorItemKey]`. However, because downstream endpoints deserialize command DTOs directly from the request body rather than reading `context.Actor()`, security depends entirely on the effectiveness of `FindActorMismatchAsync`.
- **Finding Links:** `SEC-001`, `SEC-004`
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

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

### AuthenticationMiddleware.<InvokeAsync>b__3_0 (Anonymous Path Filter) — Line 36
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:36:42:SimpleLambdaExpression`
- **Signature:** `p => path.Equals(p, StringComparison.OrdinalIgnoreCase)`
- **Behavior:** Performs case-insensitive ordinal string comparison of the request path against `AnonymousPaths` (`/health`, `/api/auth/login-by-pin`).
- **Security Analysis:** Correctly uses `StringComparison.OrdinalIgnoreCase` to prevent casing-based routing bypasses on public endpoints.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### AuthenticationMiddleware.FindActorMismatchAsync — Lines 91-133
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:91:5:MethodDeclaration`
- **Signature:** `private static async Task<string?> FindActorMismatchAsync(HttpContext context, string authenticatedUserId)`
- **Behavior:**
  1. Inspects query parameters for every field in `ActorFields`:
     `{"actorUserId", "cashierId", "createdByUserId", "openedByUserId", "reportedByUserId"}`.
     If `context.Request.Query.TryGetValue(field, out var values)` returns a non-empty string `!= authenticatedUserId`, returns `field`.
  2. If `!context.Request.HasJsonContentType()`, returns `null`.
  3. Enables request body buffering (`context.Request.EnableBuffering()`).
  4. Parses the JSON body with `JsonDocument.ParseAsync`. In a `finally` block, rewinds `context.Request.Body.Position = 0`.
  5. For each field in `ActorFields`, executes:
     `document.RootElement.TryGetProperty(field, out var value)`.
     If the property exists and its string value is non-empty and `!= authenticatedUserId`, returns `field`.
  6. Catches `JsonException` and returns `null`.
- **Security Analysis:**
  - *SEC-001 (Casing Mismatch Bypass):* `JsonElement.TryGetProperty(string, out JsonElement)` is strictly ordinal case-sensitive. The array `ActorFields` defines only camelCase strings. When an attacker sends PascalCase (e.g. `{"ActorUserId": "admin-1"}`), `TryGetProperty` returns `false`. However, ASP.NET Core Minimal API JSON model binding defaults to `PropertyNameCaseInsensitive = true`, binding `ActorUserId` into the request DTO. Downstream handlers (e.g. `CreateUserHandler.cs:48`, `ChangePinHandler.cs:44`, `UpdateUserPermissionsHandler.cs:32`) authorize the operation using `request.ActorUserId` instead of `context.ActorId()`, achieving complete privilege elevation.
  - *SEC-004 (Omission of closedByUserId):* `ActorFields` completely omits `"closedByUserId"`. In `CloseShiftHandler.cs:52-54`, shift closures take `request.ClosedByUserId` from the JSON body to establish the acting cashier. An authenticated attacker can close any shift and spoof financial shift handover records under another user's identity.
- **Finding Links:** `SEC-001`, `SEC-004`
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

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

### HttpContextActorExtensions.Actor — Lines 140-142
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:140:5:MethodDeclaration`
- **Signature:** `public static Falah.Infrastructure.Persistence.Entities.UserEntity Actor(this HttpContext context)`
- **Behavior:** Retrieves `context.Items[AuthenticationMiddleware.ActorItemKey]` cast to `UserEntity`. Throws `InvalidOperationException("No authenticated actor on this request.")` if null.
- **Security Analysis:** Intended to provide server-authoritative caller identity to endpoints. However, it is never invoked by minimal API endpoints in `Program.cs`, leaving command handlers reliant on unvalidated request DTO fields.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### HttpContextActorExtensions.ActorId — Line 144
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:144:5:MethodDeclaration`
- **Signature:** `public static string ActorId(this HttpContext context)`
- **Behavior:** Returns `context.Actor().Id`.
- **Security Analysis:** Extension helper for retrieving authenticated caller ID. Unconsumed across application routing handlers.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

## Technical Finding Analysis: SEC-001 & SEC-004

### Confirmed Finding: SEC-001
**Category:** SECURITY / AUTHORIZATION_BYPASS  
**Severity:** HIGH (CVSS v3.1: 8.8 - AV:N/AC:L/PR:L/UI:N/S:U/C:H/I:H/A:H)  
**Confidence:** CONFIRMED_MECHANISM_AND_STATIC_TRACE  
**Source:** `src/Falah.Server/AuthenticationMiddleware.cs` Lines 112-120  

```csharp
            foreach (var field in ActorFields)
            {
                if (document.RootElement.TryGetProperty(field, out var value)
                    && value.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var claimed = value.GetString();
                    if (!string.IsNullOrEmpty(claimed) && claimed != authenticatedUserId) return field;
                }
            }
```

**Defect:**
Case-sensitive JSON property lookup in `FindActorMismatchAsync` bypasses identity mismatch checks when clients submit PascalCase property names.
- `document.RootElement.TryGetProperty(field, out var value)` executes strictly case-sensitive ordinal string comparison. Because `ActorFields` contains camelCase strings (`"actorUserId"`, etc.), submitting `{"ActorUserId": "admin-user-id"}` causes `TryGetProperty` to return `false`.
- The middleware finds no mismatch and permits the request through to endpoint routing.
- ASP.NET Core Minimal API model binding (`System.Text.Json`) uses `PropertyNameCaseInsensitive = true` by default, binding `"ActorUserId"` directly to the command request record's `ActorUserId` property.
- Handlers throughout `Falah.Infrastructure` (e.g. `CreateUserHandler.cs:48-51`, `ChangePinHandler.cs:44-46`, `UpdateUserPermissionsHandler.cs:32-35`) load the actor entity directly from `request.ActorUserId` and check permissions on that loaded entity:
  ```csharp
  var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
      ?? throw new InvalidOperationException("المستخدم غير موجود.");
  var actor = OrderMapper.ToActor(actorUser);
  AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage");
  ```
- Any authenticated user (such as a cashier or kitchen staff) can forge an administrator identity simply by capitalizing the JSON key (`"ActorUserId"`), gaining unauthorized access to administrative functions.

**Proposed Fix Specification:**
1. In `FindActorMismatchAsync`, replace `TryGetProperty` with case-insensitive property enumeration:
   ```csharp
   foreach (var property in document.RootElement.EnumerateObject())
   {
       if (ActorFields.Any(f => f.Equals(property.Name, StringComparison.OrdinalIgnoreCase)))
       {
           if (property.Value.ValueKind == JsonValueKind.String)
           {
               var claimed = property.Value.GetString();
               if (!string.IsNullOrEmpty(claimed) && claimed != authenticatedUserId)
                   return property.Name;
           }
       }
   }
   ```
2. In `Program.cs`, refactor endpoint route handlers to inject `context.ActorId()` into commands directly rather than deserializing actor IDs from client JSON bodies.

---

### Confirmed Finding: SEC-004
**Category:** SECURITY / IDENTITY_SPOOFING  
**Severity:** HIGH (CVSS v3.1: 7.5 - AV:N/AC:L/PR:L/UI:N/S:U/C:N/I:H/A:N)  
**Confidence:** CONFIRMED_MECHANISM_AND_STATIC_TRACE  
**Source:** `src/Falah.Server/AuthenticationMiddleware.cs` Lines 86-89; `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs` Lines 52-55  

```csharp
    private static readonly string[] ActorFields =
    {
        "actorUserId", "cashierId", "createdByUserId", "openedByUserId", "reportedByUserId",
    };
```
```csharp
    var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ClosedByUserId, ct)
        ?? throw new InvalidOperationException("المستخدم غير موجود.");
    var actor = OrderMapper.ToActor(cashierUser);
```

**Defect:**
`ActorFields` omits `"closedByUserId"`, enabling arbitrary cashier/shift close identity spoofing.
- `POST /api/shifts/close` consumes `CloseShiftRequest(..., string ClosedByUserId, ...)`.
- In `CloseShiftHandler.cs` (lines 52-55), the handler queries the user entity using `request.ClosedByUserId`, builds the `actor` representation, evaluates `AuthorizationPolicy.AuthorizeShiftClose`, and registers the shift closure and audit event (`AuditEvent`) under `cashierUser.Name` and `cashierUser.Id`.
- Because `"closedByUserId"` is omitted from `ActorFields`, `AuthenticationMiddleware.FindActorMismatchAsync` never inspects this property.
- Any authenticated user can execute a shift close attributing the action, discrepancy reconciliation, and drawer cash handover to another cashier or manager.

**Proposed Fix Specification:**
1. Append `"closedByUserId"` to `ActorFields` in `AuthenticationMiddleware.cs`:
   ```csharp
   private static readonly string[] ActorFields =
   {
       "actorUserId", "cashierId", "createdByUserId", "openedByUserId", "reportedByUserId", "closedByUserId",
   };
   ```
2. Refactor `POST /api/shifts/close` to populate `ClosedByUserId` strictly from `context.ActorId()`.
