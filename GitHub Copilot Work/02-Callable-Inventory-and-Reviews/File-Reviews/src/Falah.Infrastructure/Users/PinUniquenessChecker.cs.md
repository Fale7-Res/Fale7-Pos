# src/Falah.Infrastructure/Users/PinUniquenessChecker.cs

- **Path:** `src/Falah.Infrastructure/Users/PinUniquenessChecker.cs`
- **Component:** `Falah.Infrastructure.Users.PinUniquenessChecker`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`PinUniquenessChecker` enforces the domain invariant that **no two active users in the restaurant can share the same PIN** ([PinUniquenessChecker.cs#L8-L13](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/PinUniquenessChecker.cs#L8-L13)).

In Fale7 POS, login is single-factor PIN-only: the cashier enters a 4-to-6 digit PIN on the unlock screen without entering a username. Therefore, PIN uniqueness among active users is the sole barrier against identity collisions during authentication.

Because `PinHasher` employs unique per-user random salts and PBKDF2 key stretching, two identical PINs produce entirely distinct hash strings. Consequently, database-level unique constraints cannot enforce uniqueness; the check must execute at the application layer by verifying the candidate PIN against every active user's stored salt and hash.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Signature | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-PUC-001` | `AssertNotInUseAsync` | `public static async Task AssertNotInUseAsync(FalahDbContext db, string candidatePin, string? excludeUserId, CancellationToken ct)` | 16-23 | Substantive (Security Boundary) |
| `CAL-PUC-002` | `AssertNotInUseAsync::<lambda_filter>` | `u => u.Active && u.Id != excludeUserId` | 19 | Generated / Linq Expression |
| `CAL-PUC-003` | `AssertNotInUseAsync::<lambda_select>` | `u => new { u.PinHash, u.PinSalt, u.PinIterations }` | 20 | Generated / Linq Expression |
| `CAL-PUC-004` | `AssertNotInUseAsync::<lambda_verify>` | `u => PinHasher.Verify(candidatePin, u.PinHash, u.PinSalt, u.PinIterations)` | 21 | Generated / Linq Expression |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-PUC-001: `AssertNotInUseAsync(FalahDbContext db, string candidatePin, string? excludeUserId, CancellationToken ct)`
- **Line Span:** [16-23](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/PinUniquenessChecker.cs#L16-L23)
- **Actual Behavior & Inputs:**
  1. **Inputs:** `db` (database context), `candidatePin` (plaintext PIN string to be validated), `excludeUserId` (optional user ID to exclude, used during PIN changes to avoid false self-collision), `ct` (cancellation token).
  2. **Active User Query:** Executes `db.Users.AsNoTracking().Where(u => u.Active && u.Id != excludeUserId).Select(u => new { u.PinHash, u.PinSalt, u.PinIterations }).ToListAsync(ct)`.
  3. **In-Memory Cryptographic Iteration:** Executes `activeUsers.Any(...)` testing `PinHasher.Verify(candidatePin, u.PinHash, u.PinSalt, u.PinIterations)` against each active user record.
  4. **Rejection:** If any user's hash matches `candidatePin`, throws `DomainException("رمز PIN مستخدم بالفعل لحساب آخر - اختر رمزًا مختلفًا.")`.
  5. If no match is found, completes normally.
- **Callers & Dependencies:**
  - Invoked by `CreateUserHandler.HandleAsync` ([CreateUserHandler.cs#L59](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L59)).
  - Invoked by `ChangePinHandler.HandleAsync` ([ChangePinHandler.cs#L51](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/ChangePinHandler.cs#L51)).
  - Dependencies: `FalahDbContext`, `PinHasher.Verify`, `_db.Users`.
- **Side Effects:** Issues SQL `SELECT` against `Users` table; consumes thread-pool CPU time computing PBKDF2 stretching.
- **Critical Concurrency & Security Forensics:**
  - **1. THE CONCURRENT PIN RACE CONDITION:**
    - `AssertNotInUseAsync` does NOT acquire table or key-range locks (e.g. `UPDLOCK, HOLDLOCK`).
    - If Administrator 1 creates User A with PIN `1234` and Administrator 2 simultaneously creates User B with PIN `1234`:
      1. Both read `Users` concurrently; neither user exists yet.
      2. Both calls to `AssertNotInUseAsync` pass without detecting a collision.
      3. Both insert their user entities with different random salts and commit.
      4. **The Collision:** Two active users now possess the exact same PIN.
      5. **The Catastrophic Authentication Failure:**
         In `POST /api/auth/login-by-pin` ([Program.cs#L1855-L1856](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1855-L1856)):
         ```csharp
         var candidates = await db.Users.AsNoTracking().Where(u => u.Active).ToListAsync(ct);
         var user = candidates.FirstOrDefault(u => PinHasher.Verify(request.Pin, ...));
         ```
         `FirstOrDefault` picks whichever candidate row SQL Server returns first. Cashier A entering their PIN will randomly authenticate as Cashier B (or vice versa), violating account segregation and audit attribution.
  - **2. COMPUTATIONAL DENIAL OF SERVICE (DoS) EXPOSURE:**
    - `PinHasher.Verify` executes 100,000 iterations of HMAC-SHA256 per candidate.
    - For $N$ active staff accounts, evaluating a non-colliding candidate PIN requires $N \times 100,000$ iterations.
    - On low-power POS hardware (e.g. Intel Celeron J1900 or Elkhart Lake processors typical of restaurant terminals), computing 2,000,000 - 5,000,000 PBKDF2 iterations blocks the thread for several seconds.
    - An attacker with access to `/api/users/{id}/change-pin` can intentionally trigger repeated uniqueness checks to spike CPU usage and degrade POS checkout responsiveness.
- **Test Coverage:**
  - *Covered:* `UserManagementVerticalSliceTests.User_cannot_change_pin_to_one_already_in_use_by_another_user`, `UserManagementVerticalSliceTests.User_can_keep_their_own_pin_when_updating`.
  - *Missing Tests:* Missing concurrency test simulating simultaneous creation of two users with identical PINs; missing benchmark for PBKDF2 execution time across multiple active accounts.

---

### CAL-PUC-002 to CAL-PUC-004: Expression Lambdas
- **CAL-PUC-002 (`u => u.Active && u.Id != excludeUserId`):** Database query filter restricting evaluation to active accounts other than the entity being updated.
- **CAL-PUC-003 (`u => new { u.PinHash, u.PinSalt, u.PinIterations }`):** SQL column projection minimizing network and memory transfer.
- **CAL-PUC-004 (`u => PinHasher.Verify(...)`):** In-memory predicate executing cryptographic PBKDF2 comparison.
- **Classification Justification:** Generated LINQ expressions and delegate predicates.

---

## 4. Verification & Recommendations

1. **Remediation for Concurrency Race:** Wrap user creation and PIN updates in an explicit transaction with `SERIALIZABLE` isolation or table lock `WITH (TABLOCKX)` on `Users` during uniqueness verification.
2. **Remediation for DoS:** Retain the existing IP-based rate limiting (`"login"` policy) on all PIN change and user creation endpoints to limit request throughput.
3. **Verification:** Static source audit confirmed; no application source modified.
