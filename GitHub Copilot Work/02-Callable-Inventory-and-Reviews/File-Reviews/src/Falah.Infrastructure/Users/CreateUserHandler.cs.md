# src/Falah.Infrastructure/Users/CreateUserHandler.cs

- **Path:** `src/Falah.Infrastructure/Users/CreateUserHandler.cs`
- **Component:** `Falah.Infrastructure.Users.CreateUserHandler`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`CreateUserHandler` executes the administrative command to provision new user accounts in Fale7 POS. It fulfills key architectural and business invariants:
- **Owner Account Bootstrap & Immutability:** Enforces that exactly one user with the `owner` role can exist in the system. The first owner may be provisioned through this handler, but any subsequent creation of an owner is strictly forbidden ([CreateUserHandler.cs#L37-L46](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L37-L46)).
- **Privilege Escalation Prevention:** Enforces `users_manage` authorization and guarantees that the executing actor cannot grant permissions that the actor does not personally possess ([UserManagementRules.AssertNoPrivilegeEscalation](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L29-L35)).
- **PIN Collision & Storage Hygiene:** Verifies PIN uniqueness across all active users and stores credentials strictly using PBKDF2-SHA256 salted hashes ([PinHasher.cs](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/PinHasher.cs)).
- **Idempotency Contract:** Utilizes a client-minted `RequestId` to construct a deterministic entity ID (`user-{RequestId}`) and gracefully handles retry replays across multiple failure stages.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Signature | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-CU-001` | Constructor | `public CreateUserHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)` | 23-27 | Substantive (DI Hook) |
| `CAL-CU-002` | `HandleAsync` | `public async Task<CreateUserResult> HandleAsync(CreateUserRequest request, CancellationToken ct = default)` | 29-95 | Substantive (Security & Transaction Core) |
| `CAL-CU-003` | `HandleAsync::<lambda_replay_check>` | `u => u.Id == id` | 32 | Generated / Linq Expression |
| `CAL-CU-004` | `HandleAsync::<lambda_owner_check>` | `u => u.Role == UserManagementRules.OwnerRole` | 43 | Generated / Linq Expression |
| `CAL-CU-005` | `HandleAsync::<lambda_actor_lookup>` | `u => u.Id == request.ActorUserId` | 48 | Generated / Linq Expression |
| `CAL-CU-006` | `HandleAsync::<lambda_pin_replay_check>` | `u => u.Id == id` | 66 | Generated / Linq Expression |
| `CAL-CU-007` | `HandleAsync::<lambda_dbupdate_replay_check>` | `u => u.Id == id` | 90 | Generated / Linq Expression |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-CU-001: Constructor `CreateUserHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)`
- **Line Span:** [23-27](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L23-L27)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Users/CreateUserHandler.cs:23:5:ConstructorDeclaration`
- **Exact Line Range:** Lines 23–27
- **Actual Behavior & Inputs:** Captures `FalahDbContext` for persistence and `IApprovalRegistry` for manager approval token validation.
- **Callers & Dependencies:** Injected via ASP.NET Core DI in Minimal API endpoint `POST /api/users` ([Program.cs#L1898](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1898)).
- **Side Effects:** Stores references in private fields `_db` and `_approvalRegistry`.
- **Classification Justification:** Substantive dependency wiring.

---

### CAL-CU-002: `HandleAsync(CreateUserRequest request, CancellationToken ct = default)`
- **Line Span:** [29-95](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L29-L95)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Users/CreateUserHandler.cs:29:5:MethodDeclaration`
- **Exact Line Range:** Lines 29–95
- **Actual Behavior & Execution Flow:**
  1. **Deterministic Identity Formation:** Mints `id = $"user-{request.RequestId}"`.
  2. **Stage 1 Replay Guard:** Queries `_db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct)`. If an entity exists, returns `new CreateUserResult(existing.Id, WasReplayed: true)` without executing further validation.
  3. **Input Validation:** Asserts non-whitespace on `request.Name` and `request.Role` (throws `DomainException`).
  4. **Single-Owner Invariant:** If `request.Role == "owner"`, checks if any owner already exists (`_db.Users.AnyAsync(u => u.Role == "owner", ct)`). Throws `DomainException("يوجد حساب مالك بالفعل - لا يمكن إنشاء حساب مالك ثانٍ.")` if an owner already exists.
  5. **Actor Identity Resolution:** Resolves actor record: `_db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)`. Throws `InvalidOperationException("المستخدم غير موجود.")` if not found.
  6. **Authorization Enforcement:** Invokes `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage")`. Note: Gated strictly on permission without approval fallback.
  7. **Privilege Escalation Assertion:**
     - Validates all requested permissions exist in `PermissionCatalog.Keys` (`AssertKnownPermissions`).
     - Asserts `AssertNoPrivilegeEscalation(actor.Permissions, permissions)`: verifies that the actor holds `"all"` or every single permission being assigned to the new user.
  8. **PIN Uniqueness & Stage 2 Replay Check:**
     - Executes `PinUniquenessChecker.AssertNotInUseAsync(_db, request.InitialPin, excludeUserId: null, ct)`.
     - Catches `DomainException`: If a concurrent retry with the identical `RequestId` already committed the user with this PIN, queries `_db.Users` by `id`. If found, returns `WasReplayed = true`; otherwise rethrows genuine PIN collision.
  9. **Cryptographic PIN Hashing:** Computes `PinHasher.Hash(request.InitialPin)` to generate salt, iterations (100,000+ PBKDF2), and hash.
  10. **Entity Assembly & Audit Trail:**
      - Builds `UserEntity` with `Active = true` and comma-separated permissions.
      - Builds `ControlAuditEventEntity` (`Action = "user_created"`, `Actor = actorUser.Name`, `EntityId = id`).
  11. **Stage 3 Replay Guard & Commit:**
      - Adds entities and executes `await _db.SaveChangesAsync(ct)`.
      - Catches `DbUpdateException`: If primary key collision occurred due to parallel request race, queries by `id` and returns `WasReplayed = true`; otherwise rethrows.
  12. Returns `CreateUserResult(entity.Id, WasReplayed: false)`.
- **Callers & Dependencies:**
  - Invoked by Minimal API route `POST /api/users` ([Program.cs#L1898](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1898)).
  - Dependencies: `PinUniquenessChecker`, `PinHasher`, `AuthorizationPolicy`, `UserManagementRules`, `OrderMapper`.
- **Side Effects:**
  - Inserts row into `Users` table and `ControlAuditEvents` table.
- **Trust Boundaries & Security Vulnerabilities:**
  - **CRITICAL VULNERABILITY (SEC-001):** The handler trusts `request.ActorUserId` from the client JSON body rather than the cryptographically resolved session actor from `HttpContext.ActorId()`. Due to the casing mismatch defect in `AuthenticationMiddleware.FindActorMismatchAsync`, an authenticated attacker can send `"ActorUserId": "usr-admin"` in PascalCase. The middleware mismatch check evaluates to `false`, Minimal API deserializes `"usr-admin"`, and this handler queries the administrator's record. Consequently:
    - The `users_manage` check passes.
    - `AssertNoPrivilegeEscalation` evaluates against the administrator's permissions (`"all"`), completely bypassing privilege escalation restrictions!
  - **Audit Log Spoofing:** `ControlAuditEventEntity.Actor = actorUser.Name` records the name of the spoofed actor (`request.ActorUserId`), concealing the real caller's identity in the audit ledger.
  - **Single Owner Race Condition:** The owner existence check (`AnyAsync`) and subsequent insert are not wrapped in a serializable transaction lock. Two concurrent requests to create an owner during initial setup could theoretically race, although mitigated by DB unique indexes if defined.
- **Concurrency & Failure Modes:**
  - Robust 3-stage idempotency handling guarantees that client network retries carrying identical `RequestId` do not produce duplicate accounts or spurious PIN collision errors.
- **Test Coverage:**
  - *Covered:* `UserManagementVerticalSliceTests.Creating_a_user_with_valid_data_succeeds_and_the_new_pin_logs_in`, `UserManagementVerticalSliceTests.Creating_a_second_owner_is_rejected`, `UserManagementVerticalSliceTests.Actor_cannot_grant_permissions_they_do_not_hold`.
  - *Missing Tests:* Missing regression test for concurrent requests sharing identical `RequestId` hitting Stage 2 (PIN catch) and Stage 3 (DbUpdateException catch); missing test demonstrating SEC-001 PascalCase exploit prevention.

---

### CAL-CU-003: `HandleAsync::<lambda_replay_check>` (`u => u.Id == id`)
- **Line:** [32](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L32)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Users/CreateUserHandler.cs:32:75:SimpleLambdaExpression`
- **Exact Line Range:** Lines 32–32
- **Actual Behavior & Inputs:** Expression predicate for `FirstOrDefaultAsync` performing initial idempotency lookup on primary key `id`.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL query filter.

---

### CAL-CU-004: `HandleAsync::<lambda_owner_check>` (`u => u.Role == UserManagementRules.OwnerRole`)
- **Line:** [43](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L43)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Users/CreateUserHandler.cs:43:56:SimpleLambdaExpression`
- **Exact Line Range:** Lines 43–43
- **Actual Behavior & Inputs:** Expression predicate for `AnyAsync` asserting whether any row in `Users` has `Role == "owner"`.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL `EXISTS` query.

---

### CAL-CU-005: `HandleAsync::<lambda_actor_lookup>` (`u => u.Id == request.ActorUserId`)
- **Line:** [48](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L48)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Users/CreateUserHandler.cs:48:76:SimpleLambdaExpression`
- **Exact Line Range:** Lines 48–48
- **Actual Behavior & Inputs:** Expression predicate for `FirstOrDefaultAsync` resolving the acting user from the untrusted request model.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL query filter.

---

### CAL-CU-006: `HandleAsync::<lambda_pin_replay_check>` (`u => u.Id == id`)
- **Line:** [66](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L66)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Users/CreateUserHandler.cs:66:77:SimpleLambdaExpression`
- **Exact Line Range:** Lines 66–66
- **Actual Behavior & Inputs:** Expression predicate used within the PIN collision exception handler to confirm whether the apparent PIN collision was caused by a completed concurrent replay of the same request.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL query filter.

---

### CAL-CU-007: `HandleAsync::<lambda_dbupdate_replay_check>` (`u => u.Id == id`)
- **Line:** [90](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L90)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Users/CreateUserHandler.cs:90:77:SimpleLambdaExpression`
- **Exact Line Range:** Lines 90–90
- **Actual Behavior & Inputs:** Expression predicate used within the `DbUpdateException` catch handler to verify if primary key duplicate key violation was due to an identical replayed request.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL query filter.

---

## 4. Verification & Recommendations

1. **Remediation for SEC-001:** Overwrite `request.ActorUserId` with `context.ActorId()` at the endpoint layer in `Program.cs`, or inject `HttpContext` to pass the authenticated actor identity directly to `CreateUserHandler`.
2. **Remediation for Audit Integrity:** Record both the authenticated session user ID and the client IP address in `ControlAuditEventEntity`.
3. **Verification:** Static source audit confirmed; no application source modified.
