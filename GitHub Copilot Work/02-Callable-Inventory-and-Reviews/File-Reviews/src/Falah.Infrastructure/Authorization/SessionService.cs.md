# src/Falah.Infrastructure/Authorization/SessionService.cs

- **Path:** `src/Falah.Infrastructure/Authorization/SessionService.cs`
- **Component:** `Falah.Infrastructure.Authorization.SessionService`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`SessionService` is the foundational server-side session management authority for Fale7 POS. Its explicit architectural mission is to make caller identity an unforgeable server-established fact rather than a client-asserted claim ([SessionService.cs#L8-L16](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L8-L16)).

Before `SessionService` was introduced, endpoints trusted client-supplied `actorUserId` fields, allowing callers on the local network to read manager IDs from public pickers and execute privileged commands. `SessionService` establishes cryptographically bound sessions stored in `_db.UserSessions`, where:
- Plaintext session tokens are never stored in the database (only SHA-256 digests via `SessionToken.Hash`).
- Session resolution unconditionally checks user active status on every request, providing immediate lockout capabilities.
- Sliding session lifetimes are maintained with a write-throttling threshold (1 minute).

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Signature | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-SS-001` | Constructor | `public SessionService(FalahDbContext db)` | 20 | Substantive (DI Hook) |
| `CAL-SS-002` | `IssueAsync` | `public async Task<string> IssueAsync(string userId, CancellationToken ct = default)` | 22-37 | Substantive (Security Boundary) |
| `CAL-SS-003` | `ResolveAsync` | `public async Task<UserEntity?> ResolveAsync(string? token, CancellationToken ct = default)` | 42-67 | Substantive (Authentication Core) |
| `CAL-SS-004` | `ResolveAsync::<lambda_session>` | `s => s.TokenHash == hash` | 47 | Generated / Linq Expression |
| `CAL-SS-005` | `ResolveAsync::<lambda_user>` | `u => u.Id == session.UserId` | 53 | Generated / Linq Expression |
| `CAL-SS-006` | `RevokeAsync` | `public async Task RevokeAsync(string? token, CancellationToken ct = default)` | 69-77 | Substantive (Session Termination) |
| `CAL-SS-007` | `RevokeAsync::<lambda_session>` | `s => s.TokenHash == hash` | 73 | Generated / Linq Expression |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-SS-001: Constructor `SessionService(FalahDbContext db)`
- **Line:** [20](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L20)
- **Actual Behavior & Inputs:** Injects the EF Core `FalahDbContext` instance into the private readonly field `_db`.
- **Callers & Dependencies:** Invoked by ASP.NET Core dependency injection container per HTTP request scope (registered via `builder.Services.AddScoped<SessionService>()` or resolved in middleware).
- **Side Effects:** Binds database context lifecycle to service scope.
- **Trust Boundaries & Failure Modes:** If `db` is null, throws `NullReferenceException` on first operation.
- **Concurrency & Thread Safety:** Inherits EF Core DbContext instance thread affinity (non-thread-safe; must not be invoked across parallel threads concurrently).
- **Classification Justification:** Substantive dependency assignment, foundational to data tier operations.

---

### CAL-SS-002: `IssueAsync(string userId, CancellationToken ct = default)`
- **Line Span:** [22-37](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L22-L37)
- **Actual Behavior & Inputs:**
  1. Calls `SessionToken.Issue()` to generate a high-entropy cryptographically secure random session string (32 bytes via `RandomNumberGenerator`, Base64/Hex formatted).
  2. Obtains current timestamp: `now = DateTimeOffset.UtcNow`.
  3. Constructs a new `UserSessionEntity`:
     - `Id`: `sess-{Guid.NewGuid():N}`
     - `TokenHash`: `SessionToken.Hash(token)` (SHA-256 digest)
     - `UserId`: `userId`
     - `CreatedAtUtc`: `now`
     - `LastSeenAtUtc`: `now`
  4. Adds entity to `_db.UserSessions` and persists via `await _db.SaveChangesAsync(ct)`.
  5. Returns the raw plaintext `token` to the caller.
- **Callers & Dependencies:**
  - Invoked exclusively by `POST /api/auth/login-by-pin` in `src/Falah.Server/Program.cs` lines 1867-1871 upon successful PIN verification.
  - Dependencies: `SessionToken.Issue()`, `SessionToken.Hash()`, `_db.UserSessions`, `_db.SaveChangesAsync`.
- **Side Effects:**
  - Persists a new row in the `UserSessions` SQL table.
  - Modifies the EF Core Change Tracker state.
- **Trust Boundaries & Security Implications:**
  - **No Session Fixation:** Every call to `IssueAsync` generates a freshly minted token with high cryptographic entropy.
  - **Token Storage Hygiene:** The plaintext token is never stored in persistent storage or memory cache. Only the one-way SHA-256 hash resides in the database.
  - **Single Session per Login:** Does not invalidate preexisting sessions for the same `userId`. A user can maintain multiple active sessions concurrently across multiple till stations or browser tabs.
  - **Absence of Pruning / Storage Growth:** There is no automatic purging or cap on sessions per user; expired sessions remain in the `UserSessions` table indefinitely unless an external maintenance task runs.
- **Failure Modes:** Database connectivity failure or cancellation token abort throws during `SaveChangesAsync`.
- **Test Coverage:**
  - *Covered:* Tested in `AuthenticationTests.Logging_out_stops_the_token_working_immediately` and `UserManagementVerticalSliceTests.Creating_a_user_with_valid_data_succeeds_and_the_new_pin_logs_in`.
  - *Missing Tests:* Missing verification of token entropy distribution and behavior when database timeout occurs during login.

---

### CAL-SS-003: `ResolveAsync(string? token, CancellationToken ct = default)`
- **Line Span:** [42-67](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L42-L67)
- **Actual Behavior & Inputs:**
  1. Input Validation: If `token` is null, empty, or whitespace, immediately returns `null`.
  2. Hash Computation: Computes `hash = SessionToken.Hash(token)`.
  3. Session Lookup: Queries `_db.UserSessions.FirstOrDefaultAsync(s => s.TokenHash == hash, ct)`.
  4. Revocation Check: If session is null or `session.RevokedAtUtc is not null`, returns `null`.
  5. Expiration Check: Evaluates `SessionToken.IsExpired(session.LastSeenAtUtc, now)`. If expired (exceeding sliding window of 24h), returns `null`.
  6. User Active Status Check: Queries `_db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == session.UserId, ct)`. If user is null or `!user.Active`, immediately returns `null`.
  7. Sliding Window Update (Throttled): If `now - session.LastSeenAtUtc > TimeSpan.FromMinutes(1)`, mutates `session.LastSeenAtUtc = now` and persists via `await _db.SaveChangesAsync(ct)`.
  8. Returns the verified `UserEntity`.
- **Callers & Dependencies:**
  - Invoked on every non-anonymous request by `Falah.Server.AuthenticationMiddleware.InvokeAsync` ([AuthenticationMiddleware.cs#L47](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs#L47)).
  - Dependencies: `SessionToken.Hash`, `SessionToken.IsExpired`, `_db.UserSessions`, `_db.Users`.
- **Side Effects:**
  - Issues database SELECT queries on `UserSessions` and `Users`.
  - Conditionally issues a database UPDATE on `UserSessions.LastSeenAtUtc` and commits via `SaveChangesAsync(ct)`.
- **Trust Boundaries & Security Implications:**
  - **Immediate Deactivation Guarantee:** By reading `user.Active` from `_db.Users` on every resolution, deactivating a user via `SetUserActiveHandler` immediately blocks all requests across all stations without waiting for token expiration.
  - **Uniform Rejection Profile:** Rejections for non-existent tokens, revoked tokens, expired tokens, and inactive users all return `null`, preventing enumeration or timing oracle exploitation.
  - **Shared DbContext Mutation Risk:** Calling `_db.SaveChangesAsync(ct)` inside an authentication resolution step flushes any pending changes currently tracked by the scoped `FalahDbContext`. Because `AuthenticationMiddleware` executes before business endpoint handlers, this is safe in normal routing, but would cause unintended commits if executed after dirtying the context.
  - **Session Invalidation Gap (PIN Reset):** `ResolveAsync` does NOT verify if the user's PIN was changed or if permissions were revoked. Changing a user's PIN or permissions does not invalidate previously issued sessions.
- **Concurrency & Failure Modes:**
  - If multiple concurrent HTTP requests for the same session arrive after the 1-minute throttle threshold, both may attempt to update `LastSeenAtUtc`. Because `UserSessionEntity` lacks a concurrency stamp (`[Timestamp]`), standard SQL `UPDATE` executes with last-write-wins without throwing concurrency exceptions.
- **Test Coverage:**
  - *Covered:* `AuthenticationTests.A_forged_token_is_refused`, `AuthenticationTests.A_cashier_cannot_act_under_the_managers_name`.
  - *Missing Tests:* Verification of sliding window 1-minute update throttle behavior; test verifying immediate session denial when user is deactivated in parallel.

---

### CAL-SS-004: `ResolveAsync::<lambda_session>` (`s => s.TokenHash == hash`)
- **Line:** [47](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L47)
- **Actual Behavior & Inputs:** Expression predicate passed to EF Core `FirstOrDefaultAsync` to translate into SQL `WHERE [s].[TokenHash] = @hash`.
- **Dependencies:** Relies on unique index or index on `TokenHash` in SQL database schema.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL query filter.

---

### CAL-SS-005: `ResolveAsync::<lambda_user>` (`u => u.Id == session.UserId`)
- **Line:** [53](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L53)
- **Actual Behavior & Inputs:** Expression predicate passed to `FirstOrDefaultAsync` with `AsNoTracking()` to translate into primary key lookup `WHERE [u].[Id] = @session_UserId`.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL clustered index lookup.

---

### CAL-SS-006: `RevokeAsync(string? token, CancellationToken ct = default)`
- **Line Span:** [69-77](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L69-L77)
- **Actual Behavior & Inputs:**
  1. Validates input token: if null or whitespace, early returns.
  2. Computes `hash = SessionToken.Hash(token)`.
  3. Queries `_db.UserSessions.FirstOrDefaultAsync(s => s.TokenHash == hash, ct)`.
  4. If session is null or `session.RevokedAtUtc is not null`, early returns (idempotent no-op).
  5. Sets `session.RevokedAtUtc = DateTimeOffset.UtcNow`.
  6. Commits via `await _db.SaveChangesAsync(ct)`.
- **Callers & Dependencies:**
  - Invoked by `POST /api/auth/logout` endpoint in `src/Falah.Server/Program.cs` line 1878.
- **Side Effects:** Mutates `UserSessionEntity.RevokedAtUtc` and updates the database.
- **Trust Boundaries & Security Implications:**
  - Idempotent and safe against replay attacks.
  - Allows an authenticated caller to terminate their session immediately.
  - Since revocation is keyed purely by token hash, a user cannot revoke another user's session without possessing their token.
- **Test Coverage:**
  - *Covered:* `AuthenticationTests.Logging_out_stops_the_token_working_immediately`.
  - *Missing Tests:* Calling `RevokeAsync` twice concurrently to verify idempotency without database locking failure.

---

### CAL-SS-007: `RevokeAsync::<lambda_session>` (`s => s.TokenHash == hash`)
- **Line:** [73](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L73)
- **Actual Behavior & Inputs:** Expression predicate passed to EF Core `FirstOrDefaultAsync` to locate the target session row for revocation.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL query filter.

---

## 4. Verification & Recommendations

1. **Security Recommendation (PIN Change Invalidation):** Update `ChangePinHandler` to query all active sessions for `request.UserId` and set `RevokedAtUtc = DateTimeOffset.UtcNow`, forcing re-authentication upon credential changes.
2. **Security Recommendation (Session Cleanup):** Introduce a scheduled background cleanup job (`SessionCleanupHostedService`) to delete sessions where `RevokedAtUtc` is older than 30 days or `LastSeenAtUtc` is older than 30 days to prevent unbounded growth of `UserSessions`.
3. **Verification:** Static source audit confirmed; no application source modified.
