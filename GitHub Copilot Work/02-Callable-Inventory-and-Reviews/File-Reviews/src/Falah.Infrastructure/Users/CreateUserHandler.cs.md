# src/Falah.Infrastructure/Users/CreateUserHandler.cs

- **Path:** `src/Falah.Infrastructure/Users/CreateUserHandler.cs`
- **Component:** `Falah.Infrastructure.Users.CreateUserHandler`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `a8f333191ce7b9c9c3365b3046dccaac141e8415b1d5dc1145a509e511eb785d`
- **Auditor:** Security and Authorization Auditor
- **Total Callables:** 7
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural & Domain Responsibility Overview

`CreateUserHandler` provisions new staff, manager, and emergency owner accounts in Fale7 POS. It executes `POST /api/users`, enforcing administrative gating, credential strength, PIN uniqueness, and privilege containment.

### Security Invariants & Policy Boundaries:
1. **Administrative Authorization:** Gated strictly on `users_manage` via `AuthorizationPolicy.AuthorizeCommand`. Deliberately accepts no manager-approval token fallback.
2. **Owner Account Singularity:** Allows creating an account with `Role = "owner"` if and only if **zero** owners currently exist in the database (`!await _db.Users.AnyAsync(u => u.Role == "owner")`). Once an owner exists, subsequent attempts to provision another owner account are strictly rejected.
3. **Privilege Escalation Containment:** Enforces `UserManagementRules.AssertNoPrivilegeEscalation(actor.Permissions, permissions)`, ensuring an administrator cannot grant permissions they do not possess.
4. **PIN Security & Uniqueness:** Asserts uniqueness across all active users via `PinUniquenessChecker.AssertNotInUseAsync`, and hashes credentials via `PinHasher.Hash` (210,000 PBKDF2 rounds with 16-byte random salt).
5. **Idempotency & Replay Handling:** Uses deterministic identifier `$"user-{request.RequestId}"`, gracefully recognizing replays both before insert and upon catching `DbUpdateException`.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- |
| `CAL-CU-001` | `src/Falah.Infrastructure/Users/CreateUserHandler.cs:23:5:ConstructorDeclaration` | [23-27](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L23-L27) | Constructor (DI Hook) |
| `CAL-CU-002` | `src/Falah.Infrastructure/Users/CreateUserHandler.cs:29:5:MethodDeclaration` | [29-95](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L29-L95) | Substantive (Security Boundary) |
| `CAL-CU-003` | `src/Falah.Infrastructure/Users/CreateUserHandler.cs:32:75:SimpleLambdaExpression` | [32](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L32) | Lambda Predicate |
| `CAL-CU-004` | `src/Falah.Infrastructure/Users/CreateUserHandler.cs:43:56:SimpleLambdaExpression` | [43](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L43) | Lambda Predicate |
| `CAL-CU-005` | `src/Falah.Infrastructure/Users/CreateUserHandler.cs:48:76:SimpleLambdaExpression` | [48](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L48) | Lambda Predicate |
| `CAL-CU-006` | `src/Falah.Infrastructure/Users/CreateUserHandler.cs:66:77:SimpleLambdaExpression` | [66](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L66) | Lambda Predicate |
| `CAL-CU-007` | `src/Falah.Infrastructure/Users/CreateUserHandler.cs:90:77:SimpleLambdaExpression` | [90](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs#L90) | Lambda Predicate |

---

## 3. Deep Source-Grounded Callable Analysis

### Callable: src/Falah.Infrastructure/Users/CreateUserHandler.cs:23:5:ConstructorDeclaration
- **Exact Line Range:** Lines 23–27
- **Signature:** `public CreateUserHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)`
- **Actual Behavior & Inputs:** Injects EF Core context and approval registry for permission checks.
- **Concurrency & Lifecycle:** Scoped service per HTTP request.

---

### Callable: src/Falah.Infrastructure/Users/CreateUserHandler.cs:29:5:MethodDeclaration
- **Exact Line Range:** Lines 29–95
- **Signature:** `public async Task<CreateUserResult> HandleAsync(CreateUserRequest request, CancellationToken ct = default)`
- **Actual Behavior & Execution Path:**
  1. Computes deterministic ID: `id = $"user-{request.RequestId}"`.
  2. Queries existing user by `id`. If found, returns `CreateUserResult(existing.Id, WasReplayed: true)`.
  3. Validates non-empty `Name` and `Role`.
  4. **Single Owner Rule:** If `request.Role == "owner"`, checks `await _db.Users.AsNoTracking().AnyAsync(u => u.Role == "owner", ct)`. If an owner already exists, throws `DomainException("يوجد حساب مالك بالفعل - لا يمكن إنشاء حساب مالك ثانٍ.")`.
  5. **Authorization:** Queries `actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)`. Enforces `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage")`.
  6. **Privilege Containment:** Validates permissions:
     - `UserManagementRules.AssertKnownPermissions(permissions)`
     - `UserManagementRules.AssertNoPrivilegeEscalation(actor.Permissions, permissions)`
  7. **PIN Uniqueness & Hashing:** Calls `PinUniquenessChecker.AssertNotInUseAsync`. Catches `DomainException` to confirm whether a concurrent replay occurred. Computes `hashed = PinHasher.Hash(request.InitialPin)`.
  8. **Entity Insertion:** Adds `UserEntity` and `ControlAuditEventEntity` (`Action = "user_created"`), committing via `await _db.SaveChangesAsync(ct)`. Catches `DbUpdateException` to return replayed state if primary key collision occurred.
- **Security Implications & Vulnerability Forensics:**
  - **SEC-001 Interaction:** Trusts `request.ActorUserId` directly to load `actorUser`. If SEC-001 casing bypass is used, an unprivileged cashier can provide an administrator ID and provision accounts.
  - **Role Escalation Boundary:** While permissions cannot be escalated beyond what the actor holds, creating a user with `Role = "manager"` gives the new account manager approval minting rights in `IssueManagerApprovalHandler`.
- **Test Coverage:** Tested in `UserManagementVerticalSliceTests.Creating_a_user_with_valid_data_succeeds_and_the_new_pin_logs_in`.

---

### Callable: src/Falah.Infrastructure/Users/CreateUserHandler.cs:32:75:SimpleLambdaExpression
- **Exact Line Range:** Line 32
- **Signature:** `u => u.Id == id`
- **Actual Behavior:** LINQ expression checking if deterministic user ID already exists (idempotency fast path).

---

### Callable: src/Falah.Infrastructure/Users/CreateUserHandler.cs:43:56:SimpleLambdaExpression
- **Exact Line Range:** Line 43
- **Signature:** `u => u.Role == UserManagementRules.OwnerRole`
- **Actual Behavior:** LINQ expression checking for the existence of any existing user holding the `owner` role.

---

### Callable: src/Falah.Infrastructure/Users/CreateUserHandler.cs:48:76:SimpleLambdaExpression
- **Exact Line Range:** Line 48
- **Signature:** `u => u.Id == request.ActorUserId`
- **Actual Behavior:** LINQ expression loading the acting administrator record from `_db.Users`.

---

### Callable: src/Falah.Infrastructure/Users/CreateUserHandler.cs:66:77:SimpleLambdaExpression
- **Exact Line Range:** Line 66
- **Signature:** `u => u.Id == id`
- **Actual Behavior:** LINQ expression verifying whether a caught PIN uniqueness collision was actually a concurrent replay of the same `RequestId`.

---

### Callable: src/Falah.Infrastructure/Users/CreateUserHandler.cs:90:77:SimpleLambdaExpression
- **Exact Line Range:** Line 90
- **Signature:** `u => u.Id == id`
- **Actual Behavior:** LINQ expression confirming whether a caught database exception was a duplicate key insertion for this request.

---

## 4. Verification & Recommendations

1. **Context Actor Binding (SEC-001 Fix):** Enforce that `actorUser` is resolved from `context.ActorId()` rather than trusted `request.ActorUserId`.
2. **Static Verification:** Verified complete against `src/Falah.Infrastructure/Users/CreateUserHandler.cs`. No application source modified.


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:35:00Z
- **Source SHA-256 Verified:** `a8f333191ce7b9c9c3365b3046dccaac141e8415b1d5dc1145a509e511eb785d`
- **Callable ID Checked:** `src/Falah.Infrastructure/Users/CreateUserHandler.cs:23:5:ConstructorDeclaration`, `src/Falah.Infrastructure/Users/CreateUserHandler.cs:29:5:MethodDeclaration`, `src/Falah.Infrastructure/Users/CreateUserHandler.cs:32:75:SimpleLambdaExpression`, `src/Falah.Infrastructure/Users/CreateUserHandler.cs:43:56:SimpleLambdaExpression`, `src/Falah.Infrastructure/Users/CreateUserHandler.cs:48:76:SimpleLambdaExpression`, `src/Falah.Infrastructure/Users/CreateUserHandler.cs:66:77:SimpleLambdaExpression`, `src/Falah.Infrastructure/Users/CreateUserHandler.cs:90:77:SimpleLambdaExpression` (7 of 7 callables checked)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Infrastructure/Users/CreateUserHandler.cs` (97 lines).
  2. Verified single owner rule: Lines 42-46 enforce that an account with `Role = "owner"` can only be provisioned if no owner currently exists (`!await _db.Users.AnyAsync(u => u.Role == "owner")`), preserving emergency bootstrap while blocking secondary owner creation.
  3. Verified privilege escalation guard: Line 55 invokes `UserManagementRules.AssertNoPrivilegeEscalation(actor.Permissions, permissions)`.
  4. Verified deterministic request ID idempotency handling (`$"user-{request.RequestId}"`) both pre-insert and post-DbUpdateException.
  5. All 7 canonical callable IDs, method signatures, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:38:12Z
- **Source SHA-256 Verified:** `a8f333191ce7b9c9c3365b3046dccaac141e8415b1d5dc1145a509e511eb785d`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `a8f333191ce7b9c9c3365b3046dccaac141e8415b1d5dc1145a509e511eb785d` against `src/Falah.Infrastructure/Users/CreateUserHandler.cs` (97 lines).
2. Concur with L1 on single-owner provisioning guard and privilege escalation check.
3. Additional L2 Challenger Assessment:
   - Lines 42-46 strictly prohibit creating a secondary owner account (`!await _db.Users.AnyAsync(u => u.Role == "owner")`).
   - Line 55 enforces `UserManagementRules.AssertNoPrivilegeEscalation`, preventing administrators from provisioning accounts with permissions exceeding their own.
   - Deterministic `$"user-{request.RequestId}"` ensures idempotent retry safety.
4. All 7 callables rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
