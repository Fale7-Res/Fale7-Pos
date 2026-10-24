# Substantive Callable Audit: `src/Falah.Server/AuthenticationMiddleware.cs`

- **Source File:** `src/Falah.Server/AuthenticationMiddleware.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `3e64ad8ef5324fe99aa19a0dead024a4afe8f99e038f8e280631b2eb8eabc5eb`
- **Auditor Specialist:** `security-auditor`
- **Total File Callables:** **6**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

ASP.NET Core request pipeline middleware establishing identity via bearer token resolution and validating claimed request actor consistency. Hosts critical vulnerabilities SEC-001 (case sensitivity mismatch bypass) and SEC-004 (omission of closedByUserId).

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Server.AuthenticationMiddleware.AuthenticationMiddleware(RequestDelegate)`
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:20:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `20-20`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Injects downstream pipeline RequestDelegate into private field _next.

**B. Trust Boundaries & Validation:**  
Internal DI pipeline container.

**C. State Mutations & Side Effects:**  
Assigns _next. Zero state mutations.

**D. Concurrency & Transaction Implications:**  
Thread-safe singleton registration in ASP.NET Core application builder.

**E. Specialist Risks & Invariants:**  
None.

> **Verbatim Code Excerpt (Lines 20-20):**
```csharp
  20 |     public AuthenticationMiddleware(RequestDelegate next) => _next = next;
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Server.AuthenticationMiddleware.InvokeAsync(HttpContext, Falah.Infrastructure.Authorization.SessionService)`
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:30:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `30-82`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Inspects request path. Bypasses /health and login anonymous endpoints. Extracts bearer token from Authorization header and calls SessionService.ResolveAsync. Writes HTTP 401 if missing/invalid. Attaches resolved UserEntity to HttpContext.Items['falah.actor']. Calls FindActorMismatchAsync to prevent identity spoofing, writing HTTP 403 on mismatch.

**B. Trust Boundaries & Validation:**  
Network boundary. Treats all request headers and query/body claims as untrusted until verified against session store.

**C. State Mutations & Side Effects:**  
Mutates HttpContext.Items with authenticated UserEntity; mutates HttpResponse on 401/403.

**D. Concurrency & Transaction Implications:**  
Runs concurrently across all HTTP worker threads. SessionService uses scoped DbContext per request.

**E. Specialist Risks & Invariants:**  
Does not enforce RBAC endpoint permissions; relies on downstream handlers for authorization checks.

> **Verbatim Code Excerpt (Lines 30-82):**
```csharp
  30 |     public async Task InvokeAsync(HttpContext context, SessionService sessions)
  31 |     {
  32 |         var path = context.Request.Path.Value ?? string.Empty;
  33 | 
  34 |         // Swagger and static files are not the business API and carry no actor.
  35 |         var isApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
  36 |         if (!isApi || AnonymousPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
  37 |         {
  38 |             await _next(context);
  39 |             return;
  40 |         }
  41 | 
  42 |         var header = context.Request.Headers.Authorization.ToString();
  43 |         var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
  44 |             ? header["Bearer ".Length..].Trim()
  45 |             : null;
  46 | 
  47 |         var user = await sessions.ResolveAsync(token, context.RequestAborted);
  48 |         if (user is null)
  49 |         {
  50 |             context.Response.StatusCode = StatusCodes.Status401Unauthorized;
  51 |             context.Response.ContentType = "application/json; charset=utf-8";
  52 |             await context.Response.WriteAsJsonAsync(new
  53 |             {
  54 |                 error = "انتهت الجلسة أو لم يتم تسجيل الدخول. سجّل الدخول مرة أخرى.",
  55 |             });
  56 |             return;
  57 |         }
  58 | 
  59 |         context.Items[ActorItemKey] = user;
  60 | 
  61 |         // Requests still carry the acting user's id in their query or body - hundreds of call sites
  62 |         // and two shipped clients are built that shape, and rewriting them all would be a far
  63 |         // larger change than the one that closes the hole. So the id is kept, and disbelieved:
  64 |         // whatever it claims must match the session that was actually presented.
  65 |         //
  66 |         // Deliberately NOT checked: ManagerApproverUserId and ApproverUserId. Those name a SECOND
  67 |         // person - a manager approving on a cashier's screen - and forcing them to equal the
  68 |         // session would break manager approval entirely.
  69 |         var mismatch = await FindActorMismatchAsync(context, user.Id);
  70 |         if (mismatch is not null)
  71 |         {
  72 |             context.Response.StatusCode = StatusCodes.Status403Forbidden;
  73 |             context.Response.ContentType = "application/json; charset=utf-8";
  74 |             await context.Response.WriteAsJsonAsync(new
  75 |             {
  76 |                 error = "لا يمكن تنفيذ عملية باسم مستخدم آخر.",
  77 |             });
  78 |             return;
  79 |         }
  80 | 
  81 |         await _next(context);
  82 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:36:42:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `36-36`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Server/AuthenticationMiddleware.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 36-36):**
```csharp
  36 |         if (!isApi || AnonymousPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
```

---

<a id="callable-4"></a>
### Callable 4: `Falah.Server.AuthenticationMiddleware.FindActorMismatchAsync(HttpContext, string)`
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:91:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `91-133`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`SEC-001`**

**A. Actual Behavior & Execution Path:**  
Compares request query and JSON body against ActorFields: {'actorUserId', 'cashierId', 'createdByUserId', 'openedByUserId', 'reportedByUserId'}. Enables request body buffering, parses JSON via JsonDocument, and compares string value with authenticated user ID.

**B. Trust Boundaries & Validation:**  
Raw client HTTP query parameters and JSON request body.

**C. State Mutations & Side Effects:**  
Buffers request body stream and resets Position to 0.

**D. Concurrency & Transaction Implications:**  
Thread-safe per HttpContext stream.

**E. Specialist Risks & Invariants:**  
VULNERABILITY SEC-001: TryGetProperty is ordinal case-sensitive. PascalCase 'ActorUserId' bypasses check completely. VULNERABILITY SEC-004: ActorFields omits 'closedByUserId'.

> **Verbatim Code Excerpt (Lines 91-133):**
```csharp
  91 |     private static async Task<string?> FindActorMismatchAsync(HttpContext context, string authenticatedUserId)
  92 |     {
  93 |         foreach (var field in ActorFields)
  94 |         {
  95 |             if (context.Request.Query.TryGetValue(field, out var values))
  96 |             {
  97 |                 var claimed = values.ToString();
  98 |                 if (!string.IsNullOrEmpty(claimed) && claimed != authenticatedUserId) return field;
  99 |             }
 100 |         }
 101 | 
 102 |         if (!context.Request.HasJsonContentType()) return null;
 103 | 
 104 |         // Buffered so the endpoint's own model binding can still read the body afterwards.
 105 |         context.Request.EnableBuffering();
 106 |         try
 107 |         {
 108 |             using var document = await System.Text.Json.JsonDocument.ParseAsync(
 109 |                 context.Request.Body, cancellationToken: context.RequestAborted);
 110 |             if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return null;
 111 | 
 112 |             foreach (var field in ActorFields)
 113 |             {
 114 |                 if (document.RootElement.TryGetProperty(field, out var value)
 115 |                     && value.ValueKind == System.Text.Json.JsonValueKind.String)
 116 |                 {
 117 |                     var claimed = value.GetString();
 118 |                     if (!string.IsNullOrEmpty(claimed) && claimed != authenticatedUserId) return field;
 119 |                 }
 120 |             }
 121 |         }
 122 |         catch (System.Text.Json.JsonException)
 123 |         {
 124 |             // Malformed JSON is the endpoint's problem to report, not this middleware's.
 125 |             return null;
 126 |         }
 127 |         finally
 128 |         {
 129 |             context.Request.Body.Position = 0;
 130 |         }
 131 | 
 132 |         return null;
 133 |     }
```

---

<a id="callable-5"></a>
### Callable 5: `Falah.Server.HttpContextActorExtensions.Actor(HttpContext)`
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:140:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `140-142`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Extracts resolved UserEntity from HttpContext.Items['falah.actor']. Throws InvalidOperationException if unauthenticated.

**B. Trust Boundaries & Validation:**  
Internal server boundary; expects middleware to have populated item.

**C. State Mutations & Side Effects:**  
None.

**D. Concurrency & Transaction Implications:**  
Thread-safe read of HttpContext.Items dictionary.

**E. Specialist Risks & Invariants:**  
Throws exception if called on anonymous routes.

> **Verbatim Code Excerpt (Lines 140-142):**
```csharp
 140 |     public static Falah.Infrastructure.Persistence.Entities.UserEntity Actor(this HttpContext context) =>
 141 |         context.Items[AuthenticationMiddleware.ActorItemKey] as Falah.Infrastructure.Persistence.Entities.UserEntity
 142 |             ?? throw new InvalidOperationException("No authenticated actor on this request.");
```

---

<a id="callable-6"></a>
### Callable 6: `Falah.Server.HttpContextActorExtensions.ActorId(HttpContext)`
- **Callable ID:** `src/Falah.Server/AuthenticationMiddleware.cs:144:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `144-144`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Shorthand accessor returning context.Actor().Id.

**B. Trust Boundaries & Validation:**  
Internal server boundary.

**C. State Mutations & Side Effects:**  
None.

**D. Concurrency & Transaction Implications:**  
Thread-safe string property read.

**E. Specialist Risks & Invariants:**  
None.

> **Verbatim Code Excerpt (Lines 144-144):**
```csharp
 144 |     public static string ActorId(this HttpContext context) => context.Actor().Id;
```

---
