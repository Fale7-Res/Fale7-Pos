# src/Falah.Infrastructure/Authorization/SessionService.cs

- **Path:** `src/Falah.Infrastructure/Authorization/SessionService.cs`
- **Component:** `Falah.Infrastructure.Authorization.SessionService`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `eb65442b2f36e359dee026d2988ef67def1b7484ca39fca09a8dbeff271cab81`
- **Auditor:** Security and Authorization Auditor
- **Total Callables:** 7
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural & Domain Responsibility Overview

`SessionService` manages the persistent server-side lifecycle of authenticated sessions in Fale7 POS. It interfaces between `SessionToken` domain cryptography and the SQL persistence tier (`_db.UserSessions`).

### Security Architecture & Guarantees:
1. **Digest Persistence:** Plaintext bearer tokens are never stored; only SHA-256 hexadecimal digests (`SessionToken.Hash`) reside in `_db.UserSessions`.
2. **Immediate Active User Check:** On every request, `ResolveAsync` queries `_db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == session.UserId)`. If `!user.Active`, the session is immediately refused, enabling instant employee lockouts.
3. **Throttled Sliding Expiration:** Updates `session.LastSeenAtUtc` only when more than 1 minute has elapsed since the last recorded activity, bounding database write volume.
4. **Session Invalidation Gap:** Identified that PIN changes (`ChangePinHandler`) and permission revocations (`UpdateUserPermissionsHandler`) do not invalidate active sessions stored in `_db.UserSessions`.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- |
| `CAL-SS-001` | `src/Falah.Infrastructure/Authorization/SessionService.cs:20:5:ConstructorDeclaration` | [20](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L20) | Constructor |
| `CAL-SS-002` | `src/Falah.Infrastructure/Authorization/SessionService.cs:22:5:MethodDeclaration` | [22-37](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L22-L37) | Substantive (Security Boundary) |
| `CAL-SS-003` | `src/Falah.Infrastructure/Authorization/SessionService.cs:42:5:MethodDeclaration` | [42-67](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L42-L67) | Substantive (Authentication Core) |
| `CAL-SS-004` | `src/Falah.Infrastructure/Authorization/SessionService.cs:47:66:SimpleLambdaExpression` | [47](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L47) | Lambda Predicate |
| `CAL-SS-005` | `src/Falah.Infrastructure/Authorization/SessionService.cs:53:71:SimpleLambdaExpression` | [53](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L53) | Lambda Predicate |
| `CAL-SS-006` | `src/Falah.Infrastructure/Authorization/SessionService.cs:69:5:MethodDeclaration` | [69-77](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L69-L77) | Substantive (Session Termination) |
| `CAL-SS-007` | `src/Falah.Infrastructure/Authorization/SessionService.cs:73:66:SimpleLambdaExpression` | [73](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs#L73) | Lambda Predicate |

---

## 3. Deep Source-Grounded Callable Analysis

### Callable: src/Falah.Infrastructure/Authorization/SessionService.cs:20:5:ConstructorDeclaration
- **Exact Line Range:** Line 20
- **Signature:** `public SessionService(FalahDbContext db)`
- **Actual Behavior & Inputs:** Captures `FalahDbContext` in private readonly field `_db`.
- **Concurrency & Lifecycle:** Scoped service lifetime bound to the HTTP request scope.

---

### Callable: src/Falah.Infrastructure/Authorization/SessionService.cs:22:5:MethodDeclaration
- **Exact Line Range:** Lines 22–37
- **Signature:** `public async Task<string> IssueAsync(string userId, CancellationToken ct = default)`
- **Actual Behavior & Execution Path:**
  1. Generates high-entropy bearer token: `token = SessionToken.Issue()`.
  2. Constructs `UserSessionEntity`:
     - `Id = $"sess-{Guid.NewGuid():N}"`
     - `TokenHash = SessionToken.Hash(token)`
     - `UserId = userId`
     - `CreatedAtUtc = now`
     - `LastSeenAtUtc = now`
  3. Inserts into `_db.UserSessions` and commits via `await _db.SaveChangesAsync(ct)`.
  4. Returns the unhashed token string to caller.
- **Security Implications:** Zero storage of plaintext token. High entropy (256-bit) prevents prediction.
- **Test Coverage:** Tested in `AuthenticationTests.Logging_out_stops_the_token_working_immediately`.

---

### Callable: src/Falah.Infrastructure/Authorization/SessionService.cs:42:5:MethodDeclaration
- **Exact Line Range:** Lines 42–67
- **Signature:** `public async Task<UserEntity?> ResolveAsync(string? token, CancellationToken ct = default)`
- **Actual Behavior & Execution Path:**
  1. Returns `null` if token is null, empty, or whitespace.
  2. Computes `hash = SessionToken.Hash(token)`.
  3. Queries `session = await _db.UserSessions.FirstOrDefaultAsync(s => s.TokenHash == hash, ct)`.
  4. If session is null or `session.RevokedAtUtc is not null`, returns `null`.
  5. Evaluates `SessionToken.IsExpired(session.LastSeenAtUtc, now)`. If expired (>16h idle), returns `null`.
  6. Queries target user: `user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == session.UserId, ct)`. If user is null or `!user.Active`, returns `null`.
  7. If `now - session.LastSeenAtUtc > TimeSpan.FromMinutes(1)`, updates `session.LastSeenAtUtc = now` and commits via `await _db.SaveChangesAsync(ct)`.
  8. Returns authenticated `user`.
- **Security Implications:**
  - Guarantees immediate lockout upon deactivating a staff account.
  - Side effect: Modifies change tracker and commits `LastSeenAtUtc` update during authentication.
- **Test Coverage:** Tested in `AuthenticationTests.A_forged_token_is_refused`.

---

### Callable: src/Falah.Infrastructure/Authorization/SessionService.cs:47:66:SimpleLambdaExpression
- **Exact Line Range:** Line 47
- **Signature:** `s => s.TokenHash == hash`
- **Actual Behavior:** EF Core LINQ expression translating to SQL `WHERE [TokenHash] = @hash`.

---

### Callable: src/Falah.Infrastructure/Authorization/SessionService.cs:53:71:SimpleLambdaExpression
- **Exact Line Range:** Line 53
- **Signature:** `u => u.Id == session.UserId`
- **Actual Behavior:** EF Core LINQ expression translating to SQL primary key lookup `WHERE [Id] = @UserId`.

---

### Callable: src/Falah.Infrastructure/Authorization/SessionService.cs:69:5:MethodDeclaration
- **Exact Line Range:** Lines 69–77
- **Signature:** `public async Task RevokeAsync(string? token, CancellationToken ct = default)`
- **Actual Behavior & Execution Path:**
  1. Validates input token; returns early if null or whitespace.
  2. Computes `hash = SessionToken.Hash(token)`.
  3. Queries session from `_db.UserSessions`.
  4. If session is null or `session.RevokedAtUtc is not null`, returns early.
  5. Sets `session.RevokedAtUtc = DateTimeOffset.UtcNow`.
  6. Commits via `await _db.SaveChangesAsync(ct)`.
- **Security Implications:** Idempotent session termination.

---

### Callable: src/Falah.Infrastructure/Authorization/SessionService.cs:73:66:SimpleLambdaExpression
- **Exact Line Range:** Line 73
- **Signature:** `s => s.TokenHash == hash`
- **Actual Behavior:** EF Core LINQ expression translating to SQL `WHERE [TokenHash] = @hash`.

---

## 4. Verification & Recommendations

1. **Session Invalidation on Credential Change:** Update `ChangePinHandler` and `UpdateUserPermissionsHandler` to mark all active sessions for the target user as revoked.
2. **Scheduled Pruning:** Implement background service to prune sessions where `RevokedAtUtc` or `LastSeenAtUtc` is older than 30 days.
3. **Static Verification:** Verified complete against `src/Falah.Infrastructure/Authorization/SessionService.cs`. No application source modified.


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:35:00Z
- **Source SHA-256 Verified:** `eb65442b2f36e359dee026d2988ef67def1b7484ca39fca09a8dbeff271cab81`
- **Callable ID Checked:** `src/Falah.Infrastructure/Authorization/SessionService.cs:20:5:ConstructorDeclaration`, `src/Falah.Infrastructure/Authorization/SessionService.cs:22:5:MethodDeclaration`, `src/Falah.Infrastructure/Authorization/SessionService.cs:42:5:MethodDeclaration`, `src/Falah.Infrastructure/Authorization/SessionService.cs:47:66:SimpleLambdaExpression`, `src/Falah.Infrastructure/Authorization/SessionService.cs:53:71:SimpleLambdaExpression`, `src/Falah.Infrastructure/Authorization/SessionService.cs:69:5:MethodDeclaration`, `src/Falah.Infrastructure/Authorization/SessionService.cs:73:66:SimpleLambdaExpression` (7 of 7 callables checked)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Infrastructure/Authorization/SessionService.cs` (79 lines).
  2. Verified cryptographic hashing of tokens using SHA-256 (`SessionToken.Hash`) and database persistence in `_db.UserSessions`.
  3. Verified `ResolveAsync` immediate lockout behavior for inactive accounts (`!user.Active`) and sliding window update throttled to 1-minute intervals.
  4. Confirmed security finding: Changing PIN (`ChangePinHandler`) or modifying user permissions does not invoke session revocation on `_db.UserSessions`, allowing compromised tokens to remain active until idle expiration (16 hours).
  5. All 7 canonical callable IDs, method signatures, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:38:12Z
- **Source SHA-256 Verified:** `eb65442b2f36e359dee026d2988ef67def1b7484ca39fca09a8dbeff271cab81`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `eb65442b2f36e359dee026d2988ef67def1b7484ca39fca09a8dbeff271cab81` against `src/Falah.Infrastructure/Authorization/SessionService.cs` (79 lines).
2. Concur with L1 on cryptographic token hashing and active user status verification.
3. Additional L2 Challenger Assessment:
   - In `ResolveAsync` (lines 42-67), the sliding expiration update is throttled to 1-minute intervals to reduce DB write churn.
   - However, the service provides zero mechanisms for session revocation upon credential change (`ChangePinHandler`) or permission update (`UpdateUserPermissionsHandler`), allowing compromised session tokens to survive password resets until 16-hour idle expiry.
4. All 7 callables rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
