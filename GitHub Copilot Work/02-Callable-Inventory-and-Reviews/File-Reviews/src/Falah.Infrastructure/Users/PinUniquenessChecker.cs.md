# Security Review: src/Falah.Infrastructure/Users/PinUniquenessChecker.cs

## File Overview
- **Canonical Source Path:** `src/Falah.Infrastructure/Users/PinUniquenessChecker.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Source SHA-256:** `bf5c54f6230fc18e6c55484a58bad12759551a426dad04f01b086c2356d81a4b`
- **Auditor:** DEEP_SECURITY_SPECIALIST
- **Review Date:** 2026-10-25T10:10:00Z
- **Total Callables:** 4
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## Callable Reviews

### PinUniquenessChecker.AssertNotInUseAsync — Lines 16-23
- **Callable ID:** `src/Falah.Infrastructure/Users/PinUniquenessChecker.cs:16:5:MethodDeclaration`
- **Signature:** `public static async Task AssertNotInUseAsync(FalahDbContext db, string candidatePin, string? excludeUserId, CancellationToken ct)`
- **Behavior:** Queries `db.Users.AsNoTracking()` filtering on `u.Active && u.Id != excludeUserId` and projects only `{ u.PinHash, u.PinSalt, u.PinIterations }` into an in-memory list via `ToListAsync(ct)`. Iterates over the projected active user records and evaluates `PinHasher.Verify(candidatePin, u.PinHash, u.PinSalt, u.PinIterations)` for each record using LINQ `.Any(...)`. If any active user's stored hash verifies against `candidatePin`, it halts and throws `DomainException("رمز PIN مستخدم بالفعل لحساب آخر - اختر رمزًا مختلفًا.")`.
- **Security Analysis:** 
  1. *Application-Level Uniqueness:* Because `PinHasher` employs per-user random 16-byte salts (`PinSalt`) with 210,000 PBKDF2 iterations, identical PINs produce distinct cryptographic hashes. Consequently, database unique indexing cannot enforce PIN uniqueness, forcing uniqueness verification into application-level code.
  2. *Check-Then-Act Race Hazard (SEC-009):* The read operation occurs outside an exclusive lock or serializable transaction. When concurrent requests set the same PIN, both read the current state before either writes, resulting in duplicate active PINs.
  3. *CPU Exhaustion / DoS Vulnerability (SEC-010):* Performs up to $N$ sequential PBKDF2 verifications (210,000 iterations each) in memory.
- **Finding Links:** `SEC-009`, `SEC-010`
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

```csharp
    public static async Task AssertNotInUseAsync(FalahDbContext db, string candidatePin, string? excludeUserId, CancellationToken ct)
    {
        var activeUsers = await db.Users.AsNoTracking()
            .Where(u => u.Active && u.Id != excludeUserId)
            .Select(u => new { u.PinHash, u.PinSalt, u.PinIterations }).ToListAsync(ct);
        if (activeUsers.Any(u => PinHasher.Verify(candidatePin, u.PinHash, u.PinSalt, u.PinIterations)))
            throw new DomainException("رمز PIN مستخدم بالفعل لحساب آخر - اختر رمزًا مختلفًا.");
    }
```

---

### PinUniquenessChecker.<AssertNotInUseAsync>b__0_0 (Filter Predicate) — Line 19
- **Callable ID:** `src/Falah.Infrastructure/Users/PinUniquenessChecker.cs:19:20:SimpleLambdaExpression`
- **Signature:** `u => u.Active && u.Id != excludeUserId`
- **Behavior:** Database query predicate translated by EF Core into SQL `WHERE [Active] = 1 AND [Id] <> @excludeUserId`.
- **Security Analysis:** Excludes deactivated users (`u.Active == false`). This allows reuse of a former employee's PIN, but creates an edge case where reactivating a user later can reintroduce duplicate active PINs without validation (linked to `SEC-011`).
- **Finding Links:** `SEC-011`
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### PinUniquenessChecker.<AssertNotInUseAsync>b__0_1 (Projection) — Line 20
- **Callable ID:** `src/Falah.Infrastructure/Users/PinUniquenessChecker.cs:20:21:SimpleLambdaExpression`
- **Signature:** `u => new { u.PinHash, u.PinSalt, u.PinIterations }`
- **Behavior:** Anonymous type projection instructing EF Core to query only `PinHash`, `PinSalt`, and `PinIterations` from the `Users` table.
- **Security Analysis:** Prevents loading unnecessary PII or sensitive operational data (names, roles, permissions, session tokens) into application memory during uniqueness verification.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### PinUniquenessChecker.<AssertNotInUseAsync>b__0_2 (Verification Predicate) — Line 21
- **Callable ID:** `src/Falah.Infrastructure/Users/PinUniquenessChecker.cs:21:29:SimpleLambdaExpression`
- **Signature:** `u => PinHasher.Verify(candidatePin, u.PinHash, u.PinSalt, u.PinIterations)`
- **Behavior:** Evaluates `PinHasher.Verify` for each active user record. Converts base64 salt and expected hash, derives the PBKDF2 hash using `Rfc2898DeriveBytes.Pbkdf2`, and executes constant-time comparison via `CryptographicOperations.FixedTimeEquals`.
- **Security Analysis:** Constant-time comparison prevents side-channel timing leaks on hash prefixes. Short-circuits immediately via `.Any(...)` upon finding the first match.
- **Finding Links:** `SEC-010`
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

## New Finding: SEC-009

**Canonical ID:** `SEC-009`  
**Category:** SECURITY / CONCURRENCY  
**Severity:** MEDIUM (CVSS v3.1: 5.9 - AV:N/AC:H/PR:H/UI:N/S:U/C:H/I:H/A:N)  
**Confidence:** CONFIRMED_MECHANISM_AND_STATIC_TRACE  
**Source:** `src/Falah.Infrastructure/Users/PinUniquenessChecker.cs` Lines 16-23  

```csharp
    public static async Task AssertNotInUseAsync(FalahDbContext db, string candidatePin, string? excludeUserId, CancellationToken ct)
    {
        var activeUsers = await db.Users.AsNoTracking()
            .Where(u => u.Active && u.Id != excludeUserId)
            .Select(u => new { u.PinHash, u.PinSalt, u.PinIterations }).ToListAsync(ct);
        if (activeUsers.Any(u => PinHasher.Verify(candidatePin, u.PinHash, u.PinSalt, u.PinIterations)))
            throw new DomainException("رمز PIN مستخدم بالفعل لحساب آخر - اختر رمزًا مختلفًا.");
    }
```

**Defect:**
Time-of-check to time-of-use (TOCTOU) concurrency race condition. In `CreateUserHandler.cs` (lines 59-86) and `ChangePinHandler.cs` (lines 51-64), `PinUniquenessChecker.AssertNotInUseAsync` is called before generating the PBKDF2 hash and calling `_db.SaveChangesAsync()`. 
Because the uniqueness check queries the database using `AsNoTracking()` without acquiring an exclusive row/table lock, application lock (`sp_getapplock`), or serializable transaction, two concurrent administrative requests setting the same PIN will both pass the check simultaneously. Both requests compute hashes and commit their changes, resulting in two active staff accounts sharing the identical PIN.
Because POS terminal authentication identifies employees exclusively via PIN, duplicate active PINs cause non-deterministic user logins and critical identity confusion / privilege elevation.

**Proposed Fix Specification:**
1. Wrap user creation and PIN modification operations in a transaction with `System.Data.IsolationLevel.Serializable` or acquire a distributed/database application lock (e.g. `sp_getapplock` keyed on `"pos_user_pin_allocation"`).
2. Alternatively, serialize user PIN mutation operations via a mutex or semaphore in single-instance deployments, ensuring atomicity between verification and commitment.

---

## New Finding: SEC-010

**Canonical ID:** `SEC-010`  
**Category:** SECURITY / DENIAL_OF_SERVICE  
**Severity:** LOW (CVSS v3.1: 5.3 - AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:N/A:L)  
**Confidence:** CONFIRMED_MECHANISM_AND_STATIC_TRACE  
**Source:** `src/Falah.Infrastructure/Users/PinUniquenessChecker.cs` Lines 18-23  

```csharp
        var activeUsers = await db.Users.AsNoTracking()
            .Where(u => u.Active && u.Id != excludeUserId)
            .Select(u => new { u.PinHash, u.PinSalt, u.PinIterations }).ToListAsync(ct);
        if (activeUsers.Any(u => PinHasher.Verify(candidatePin, u.PinHash, u.PinSalt, u.PinIterations)))
            throw new DomainException("رمز PIN مستخدم بالفعل لحساب آخر - اختر رمزًا مختلفًا.");
```

**Defect:**
Linear computational complexity during PIN uniqueness checks leads to CPU thread starvation. `PinHasher.DefaultIterations` is configured to 210,000 PBKDF2 iterations using HMAC-SHA256. For $N$ active employees, checking a non-colliding candidate PIN forces the server to compute $N \times 210,000$ SHA-256 iterations sequentially on the request thread.
On typical modern server hardware, 210,000 iterations require ~50ms to 100ms of dedicated CPU time. In an installation with 30-50 active staff members, a single uniqueness check consumes 1.5 to 5.0 seconds of continuous 100% CPU core utilization. An attacker or rogue user submitting multiple rapid PIN updates or user creation calls can saturate all available thread pool worker threads, causing a denial of service on the POS backend.

**Proposed Fix Specification:**
1. Enforce strict rate limiting on `POST /api/users` and `POST /api/users/change-pin` (e.g., maximum 3 attempts per minute per IP/session).
2. Validate basic PIN strength rules (`PinHasher.AssertValidPin`) prior to invoking uniqueness checking, rejecting trivial or malformed PINs before spending CPU cycles.
3. Compute PBKDF2 verifications asynchronously or in parallel via `Parallel.ForEach` across worker cores to reduce latency for legitimate multi-staff configurations.
