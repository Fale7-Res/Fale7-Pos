# src/Falah.Domain/Authorization/SessionToken.cs

- **Path:** `src/Falah.Domain/Authorization/SessionToken.cs`
- **Component:** `Falah.Domain.Authorization.SessionToken`
- **Assembly:** `Falah.Domain`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`SessionToken.cs` defines the cryptographic primitives, token generation, digest computation, and lifetime evaluation rules for server-side session authentication in Fale7 POS.

### The Architectural Threat Model & Fix Target:
Before the introduction of `SessionToken`, the server accepted caller identity claims directly from incoming HTTP request bodies (`request.ActorUserId` / `request.CashierId`). Any network client on the local till network could query the public user picker, grab an administrator's user ID, and supply it to execute administrative commands without authentication. Furthermore, audit trails were completely unprovable, resting purely on the client's assertion.

`SessionToken` establishes unforgeable server-side identity:
1. **Cryptographic Entropy:** Generates 256-bit (32-byte) pseudo-random tokens using OS CSPRNG (`RandomNumberGenerator.GetBytes(32)`).
2. **One-Way Digest Storage:** Only the hex-encoded SHA-256 hash (`SHA256.HashData`) is stored in `_db.UserSessions`. If persistent database backups or logs are compromised, working bearer tokens cannot be derived from stored hashes.
3. **Sliding Session Expiration:** Evaluates session idle timeout using a 16-hour window (`IdleLifetime = TimeSpan.FromHours(16)`), ensuring abandoned terminals expire automatically.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-ST-001` | `Issue` | `src/Falah.Domain/Authorization/SessionToken.cs:25:5:MethodDeclaration` | [25](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/SessionToken.cs#L25) | Substantive (Cryptographic Primitive) |
| `CAL-ST-002` | `Hash` | `src/Falah.Domain/Authorization/SessionToken.cs:29:5:MethodDeclaration` | [29-30](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/SessionToken.cs#L29-L30) | Substantive (Security Digest) |
| `CAL-ST-003` | `IsExpired` | `src/Falah.Domain/Authorization/SessionToken.cs:37:5:MethodDeclaration` | [37-38](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/SessionToken.cs#L37-L38) | Substantive (Temporal Policy) |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-ST-001: `SessionToken.Issue()`
- **Canonical Callable ID:** `src/Falah.Domain/Authorization/SessionToken.cs:25:5:MethodDeclaration`
- **Exact Line Range:** Line 25
- **Signature:** `public static string Issue()`
- **Actual Behavior & Inputs:**
  - Invokes `RandomNumberGenerator.GetBytes(TokenSizeBytes)` where `TokenSizeBytes = 32`.
  - Converts the 32 raw bytes into a 64-character uppercase hexadecimal string via `Convert.ToHexString`.
  - Returns the resulting bearer token string.
- **Callers & Dependencies:**
  - Invoked exclusively by `SessionService.IssueAsync(userId, ct)` (`src/Falah.Infrastructure/Authorization/SessionService.cs#L24`) when authenticating staff via `POST /api/auth/login-by-pin`.
- **Side Effects:** Consumes 32 bytes of system entropy from the OS Cryptographic Service Provider (BCrypt on Windows / `/dev/urandom` on Linux).
- **Trust Boundaries & Security Implications:**
  - **Keyspace Entropy:** 256 bits of entropy ($2^{256} pprox 1.15 	imes 10^{77}$ combinations) renders online brute-force or token prediction mathematically impossible.
  - **No User Association:** The token contains zero embedded claims, user IDs, or timestamps. It is purely an opaque reference token.
  - **Transmission Hygiene:** Returned to the client once over HTTPS/TLS; never retained in server memory cache.
- **Concurrency & Failure Modes:** `RandomNumberGenerator` is thread-safe and non-blocking in .NET 8.
- **Test Coverage:** Tested in `AuthenticationTests.Logging_out_stops_the_token_working_immediately`.

---

### CAL-ST-002: `SessionToken.Hash(string token)`
- **Canonical Callable ID:** `src/Falah.Domain/Authorization/SessionToken.cs:29:5:MethodDeclaration`
- **Exact Line Range:** Lines 29–30
- **Signature:** `public static string Hash(string token)`
- **Actual Behavior & Inputs:**
  - Encodes the input `token` string to UTF-8 bytes via `System.Text.Encoding.UTF8.GetBytes(token)`.
  - Calculates the cryptographic SHA-256 digest via `SHA256.HashData(...)`.
  - Converts the 32-byte hash digest to a 64-character uppercase hexadecimal string via `Convert.ToHexString`.
- **Callers & Dependencies:**
  - Invoked by `SessionService.IssueAsync` to hash the issued token before inserting into `_db.UserSessions`.
  - Invoked by `SessionService.ResolveAsync` to query `_db.UserSessions` matching `s.TokenHash == hash`.
  - Invoked by `SessionService.RevokeAsync` to locate and revoke the session on logout.
- **Side Effects:** Pure mathematical transformation; zero state mutation.
- **Trust Boundaries & Security Implications:**
  - **Pre-image Resistance:** An attacker obtaining database read access (`UserSessions` table or backups) sees only SHA-256 digests. Because the original token has 256 bits of entropy, SHA-256 cannot be reversed or dictionary-attacked.
  - **KDF vs Fast Hash Distinction:** Unlike user PINs (which have only 4-6 digits of low entropy and require slow PBKDF2), session tokens possess full cryptographic entropy; thus, fast SHA-256 hashing is computationally optimal and immune to dictionary attacks.
  - **Input Sanitization:** Does not throw on empty string, but produces the standard SHA-256 hash of an empty byte array (`e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
- **Test Coverage:** Tested in `AuthenticationTests.A_forged_token_is_refused`.

---

### CAL-ST-003: `SessionToken.IsExpired(DateTimeOffset lastSeenUtc, DateTimeOffset nowUtc)`
- **Canonical Callable ID:** `src/Falah.Domain/Authorization/SessionToken.cs:37:5:MethodDeclaration`
- **Exact Line Range:** Lines 37–38
- **Signature:** `public static bool IsExpired(DateTimeOffset lastSeenUtc, DateTimeOffset nowUtc)`
- **Actual Behavior & Inputs:**
  - Evaluates `nowUtc - lastSeenUtc > IdleLifetime` where `IdleLifetime = TimeSpan.FromHours(16)`.
  - Returns `true` if elapsed time strictly exceeds 16 hours; otherwise `false`.
- **Callers & Dependencies:**
  - Invoked by `SessionService.ResolveAsync` (`src/Falah.Infrastructure/Authorization/SessionService.cs#L51`) on every incoming authenticated HTTP request.
- **Trust Boundaries & Security Implications:**
  - **Sliding Window:** `SessionService` updates `LastSeenAtUtc` on active requests (throttled to 1-minute intervals). As long as a terminal is continuously active, the session persists.
  - **Clock Skew Vulnerability:** If the server clock jumps backward (e.g., NTP adjustment), `nowUtc - lastSeenUtc` becomes negative, evaluating to `false` and extending session lifetime until the clock recovers.
  - **Absence of Absolute Maximum Lifetime:** The session policy implements *only* an idle timeout (16 hours), with **no maximum absolute lifetime** (e.g., 24 or 48 hours). A terminal that executes a background poll or keep-alive ping can remain authenticated indefinitely without re-entering credentials.
- **Test Coverage:** Tested in `AuthenticationTests.Session_expires_after_16_hours_of_inactivity`.

---

## 4. Vulnerability & Risk Synthesis

1. **Absence of Absolute Session Lifetime (Infinite Refresh Risk):**
   Because `SessionToken.IsExpired` checks only `nowUtc - lastSeenUtc > IdleLifetime`, an automated keep-alive ping or active overnight station can maintain a valid session indefinitely. POS terminals in hospitality environments should enforce an absolute session maximum (e.g. 24 hours) forcing cashier shift change re-authentication.
2. **Server Clock Reliance:**
   Pure static comparison relies completely on monotonically advancing UTC timestamps from the host operating system.

---

## 5. Verification & Recommendations

1. **Enforce Absolute Lifetime:** Introduce `AbsoluteLifetime = TimeSpan.FromHours(24)` alongside `IdleLifetime` in `SessionToken` and check both `CreatedAtUtc` and `LastSeenAtUtc`.
2. **Static Verification:** Verified complete against `src/Falah.Domain/Authorization/SessionToken.cs`. No application source modified.
