# Security Review: src/Falah.Infrastructure/Security/SessionService.cs

## File Overview
- **Canonical Source Path:** `src/Falah.Infrastructure/Authorization/SessionService.cs` (Referenced as `src/Falah.Infrastructure/Security/SessionService.cs`)
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Source SHA-256:** `eb65442b2f36e359dee026d2988ef67def1b7484ca39fca09a8dbeff271cab81`
- **Auditor:** DEEP_SECURITY_SPECIALIST
- **Review Date:** 2026-10-25T10:10:00Z
- **Total Callables:** 7
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## Callable Reviews

### SessionService..ctor — Line 20
- **Callable ID:** `src/Falah.Infrastructure/Authorization/SessionService.cs:20:5:ConstructorDeclaration`
- **Signature:** `public SessionService(FalahDbContext db)`
- **Behavior:** Injects and stores the scoped EF Core database context `FalahDbContext` into private readonly field `_db`.
- **Security Analysis:** Scoped to the HTTP request lifecycle, ensuring transactional integration with caller execution context.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### SessionService.IssueAsync — Lines 22-37
- **Callable ID:** `src/Falah.Infrastructure/Authorization/SessionService.cs:22:5:MethodDeclaration`
- **Signature:** `public async Task<string> IssueAsync(string userId, CancellationToken ct = default)`
- **Behavior:** Mints a 256-bit cryptographically secure random bearer token via `SessionToken.Issue()` (`RandomNumberGenerator.GetBytes(32)` converted to hex). Hashes the token using SHA-256 (`SessionToken.Hash(token)`). Persists a new `UserSessionEntity` record into `_db.UserSessions` with `Id = "sess-" + Guid.NewGuid():N`, `TokenHash`, `UserId`, `CreatedAtUtc`, and `LastSeenAtUtc`. Commits changes via `await _db.SaveChangesAsync(ct)`. Returns the unhashed token string to the caller.
- **Security Analysis:**
  - *Storage Hygiene:* The plaintext token is never stored in memory or on disk; only the SHA-256 hexadecimal hash is saved in `UserSessions`. A database leak does not expose active bearer tokens.
  - *Entropy:* 256 bits of CSPRNG entropy renders online or offline brute-force token prediction computationally impossible.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

```csharp
    public async Task<string> IssueAsync(string userId, CancellationToken ct = default)
    {
        var token = SessionToken.Issue();
        var now = DateTimeOffset.UtcNow;
        _db.UserSessions.Add(new UserSessionEntity
        {
            Id = $"sess-{Guid.NewGuid():N}",
            TokenHash = SessionToken.Hash(token),
            UserId = userId,
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
        });
        await _db.SaveChangesAsync(ct);
        // The only moment the plain token exists. It is never stored and cannot be recovered later.
        return token;
    }
```

---

### SessionService.ResolveAsync — Lines 42-67
- **Callable ID:** `src/Falah.Infrastructure/Authorization/SessionService.cs:42:5:MethodDeclaration`
- **Signature:** `public async Task<UserEntity?> ResolveAsync(string? token, CancellationToken ct = default)`
- **Behavior:**
  1. Returns `null` immediately if `token` is null, empty, or whitespace.
  2. Computes `hash = SessionToken.Hash(token)`.
  3. Queries `_db.UserSessions.FirstOrDefaultAsync(s => s.TokenHash == hash, ct)`. If no session is found or `session.RevokedAtUtc is not null`, returns `null`.
  4. Checks idle expiration: `SessionToken.IsExpired(session.LastSeenAtUtc, now)` (> 16 hours idle). Returns `null` if expired.
  5. Queries user entity: `_db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == session.UserId, ct)`.
  6. **Active User Check (SEC-012 Mitigation):** Evaluates `if (user is null || !user.Active) return null;`.
  7. If active, throttles sliding session extension: updates `session.LastSeenAtUtc = now` and commits to `_db` only if `now - session.LastSeenAtUtc > TimeSpan.FromMinutes(1)`.
  8. Returns authenticated `user`.
- **Security Analysis:**
  - *SEC-012 In-Flight Mitigation:* The explicit check `if (user is null || !user.Active) return null;` guarantees that deactivated users cannot authenticate via existing bearer tokens while deactivated.
  - *Throttling:* Throttles database updates to 1-minute intervals to avoid write amplification on every API request.
- **Finding Links:** `SEC-012`
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

```csharp
    public async Task<UserEntity?> ResolveAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var hash = SessionToken.Hash(token);
        var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.TokenHash == hash, ct);
        if (session is null || session.RevokedAtUtc is not null) return null;

        var now = DateTimeOffset.UtcNow;
        if (SessionToken.IsExpired(session.LastSeenAtUtc, now)) return null;

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == session.UserId, ct);
        // A deactivated account stops working immediately rather than waiting for its session to
        // age out - deactivating a user has to mean something the moment it happens.
        if (user is null || !user.Active) return null;

        // Keep the session alive while it is in use. Written only once the clock has actually
        // moved, so a busy till does not write a row on every request.
        if (now - session.LastSeenAtUtc > TimeSpan.FromMinutes(1))
        {
            session.LastSeenAtUtc = now;
            await _db.SaveChangesAsync(ct);
        }

        return user;
    }
```

---

### SessionService.<ResolveAsync>b__3_0 (Session Hash Lookup Predicate) — Line 47
- **Callable ID:** `src/Falah.Infrastructure/Authorization/SessionService.cs:47:66:SimpleLambdaExpression`
- **Signature:** `s => s.TokenHash == hash`
- **Behavior:** EF Core LINQ predicate querying `UserSessions` by hashed bearer token.
- **Security Analysis:** Lookups query indexed hash column; token plaintext is never compared or queried.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### SessionService.<ResolveAsync>b__3_1 (User Identity Lookup Predicate) — Line 53
- **Callable ID:** `src/Falah.Infrastructure/Authorization/SessionService.cs:53:71:SimpleLambdaExpression`
- **Signature:** `u => u.Id == session.UserId`
- **Behavior:** Untracked database query loading `UserEntity` corresponding to the session owner.
- **Security Analysis:** Ensures user identity and current active status are re-evaluated from the authoritative database on every request.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### SessionService.RevokeAsync — Lines 69-77
- **Callable ID:** `src/Falah.Infrastructure/Authorization/SessionService.cs:69:5:MethodDeclaration`
- **Signature:** `public async Task RevokeAsync(string? token, CancellationToken ct = default)`
- **Behavior:** Validates input token; hashes token; loads tracked session entity from `_db.UserSessions`; if session exists and is not already revoked, sets `session.RevokedAtUtc = DateTimeOffset.UtcNow` and calls `await _db.SaveChangesAsync(ct)`.
- **Security Analysis:** Provides explicit single-token revocation. However, the service lacks a bulk revocation method (`RevokeAllUserSessionsAsync(string userId)`), preventing handlers like `SetUserActiveHandler` or `ChangePinHandler` from terminating all sessions belonging to a specific user.
- **Finding Links:** `SEC-012`
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

```csharp
    public async Task RevokeAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        var hash = SessionToken.Hash(token);
        var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.TokenHash == hash, ct);
        if (session is null || session.RevokedAtUtc is not null) return;
        session.RevokedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
```

---

### SessionService.<RevokeAsync>b__4_0 (Session Lookup Predicate) — Line 73
- **Callable ID:** `src/Falah.Infrastructure/Authorization/SessionService.cs:73:66:SimpleLambdaExpression`
- **Signature:** `s => s.TokenHash == hash`
- **Behavior:** LINQ predicate loading tracked session record for revocation mutation.
- **Security Analysis:** Ensures tracked entity is retrieved for in-place mutation.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

## Technical Finding Analysis: SEC-012 Mitigation Verification

**Canonical ID:** `SEC-012`  
**Category:** SECURITY / SESSION_MANAGEMENT  
**Severity:** LOW  
**Disposition:** CONFIRMED_IN_FLIGHT_MITIGATION  
**Source:** `src/Falah.Infrastructure/Authorization/SessionService.cs` Lines 53-56  

```csharp
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == session.UserId, ct);
        // A deactivated account stops working immediately rather than waiting for its session to
        // age out - deactivating a user has to mean something the moment it happens.
        if (user is null || !user.Active) return null;
```

**Verification Details:**
1. **In-Flight Mitigation:** The security audit verifies that `SessionService.ResolveAsync` actively mitigates unauthorized access for deactivated accounts. When an employee is deactivated via `SetUserActiveHandler`, their user entity has `Active = false`. Any subsequent request presenting that user's session token triggers line 56 (`if (user is null || !user.Active) return null;`), returning `null`. Downstream, `AuthenticationMiddleware.InvokeAsync` halts execution and returns HTTP 401 Unauthorized.
2. **Architectural Gap:** While active exploitation during deactivation is prevented, `SessionService` provides no user-scoped revocation API. When `SetUserActiveHandler`, `ChangePinHandler`, or `UpdateUserPermissionsHandler` executes, dormant session tokens are not invalidated in `_db.UserSessions`. If an employee account is reactivated within 16 hours, the old token regains authorization without re-authentication.
3. **Recommended Enhancement:** Add `RevokeAllForUserAsync(string userId, CancellationToken ct)` to `SessionService` and invoke it during account deactivation and PIN changes.
