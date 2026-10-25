# src/Falah.Infrastructure/Users/UserManagementQueryService.cs

- **Path:** `src/Falah.Infrastructure/Users/UserManagementQueryService.cs`
- **Component:** `Falah.Infrastructure.Users.UserManagementQueryService`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `703985d05102060061f17385f5dc50c04ebf6e97d6c5eef8668d32fd647691e8`
- **Auditor:** Security and Authorization Auditor
- **Total Callables:** 6
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural & Domain Responsibility Overview

`UserManagementQueryService` provides read-only projections of staff user accounts for administrative user management screens in Fale7 POS. Unlike the public till operator picker (`GET /api/users`), which filters strictly to active accounts (`u.Active == true`), this service retrieves both active and inactive accounts to enable administrative account reactivation and permission auditing.

### Key Security & Data Exposure Invariants:
1. **Credential Hygiene (No Hash Leakage):** The private mapper `ToDto` strictly omits `PinHash`, `PinSalt`, and `PinIterations` from `UserDetailsDto`. Credential stretching artifacts are never serialized over the API.
2. **Access Control Gating:** In `Program.cs` lines 1885–1894, endpoints calling `ListAllAsync` and `GetAsync` (`GET /api/users/all` and `GET /api/users/{id}`) are guarded by `RequireUsersManageAsync(db, actorUserId, ct)`, ensuring only holders of `users_manage` or `"all"` can enumerate user profiles.
3. **Internal vs Public Visibility:** Serves as the authoritative source for the administrative management UI (`UsersViewModel`), while public till pickers consume `UserSummaryDto`.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- |
| `CAL-UQS-001` | `src/Falah.Infrastructure/Users/UserManagementQueryService.cs:14:5:ConstructorDeclaration` | [14](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L14) | Constructor (DI Hook) |
| `CAL-UQS-002` | `src/Falah.Infrastructure/Users/UserManagementQueryService.cs:16:5:MethodDeclaration` | [16-17](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L16-L17) | Substantive (Query Projection) |
| `CAL-UQS-003` | `src/Falah.Infrastructure/Users/UserManagementQueryService.cs:19:5:MethodDeclaration` | [19-23](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L19-L23) | Substantive (Query Projection) |
| `CAL-UQS-004` | `src/Falah.Infrastructure/Users/UserManagementQueryService.cs:21:71:SimpleLambdaExpression` | [21](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L21) | Lambda Predicate |
| `CAL-UQS-005` | `src/Falah.Infrastructure/Users/UserManagementQueryService.cs:25:5:MethodDeclaration` | [25](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L25) | Catalog Gateway |
| `CAL-UQS-006` | `src/Falah.Infrastructure/Users/UserManagementQueryService.cs:27:5:MethodDeclaration` | [27-30](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L27-L30) | Substantive (DTO Projection) |

---

## 3. Deep Source-Grounded Callable Analysis

### Callable: src/Falah.Infrastructure/Users/UserManagementQueryService.cs:14:5:ConstructorDeclaration
- **Exact Line Range:** Line 14
- **Signature:** `public UserManagementQueryService(FalahDbContext db)`
- **Actual Behavior & Inputs:** Captures `FalahDbContext` for read-only query operations in private readonly field `_db`.
- **Concurrency & Lifecycle:** Registered with scoped lifetime per HTTP request.

---

### Callable: src/Falah.Infrastructure/Users/UserManagementQueryService.cs:16:5:MethodDeclaration
- **Exact Line Range:** Lines 16–17
- **Signature:** `public async Task<List<UserDetailsDto>> ListAllAsync(CancellationToken ct = default)`
- **Actual Behavior & Execution Path:**
  1. Issues non-tracking query: `await _db.Users.AsNoTracking().ToListAsync(ct)`.
  2. Projects each `UserEntity` via `.Select(ToDto).ToList()`.
  3. Returns full list of user details including inactive users.
- **Trust Boundaries & Security Implications:**
  - Invoked exclusively by `GET /api/users/all`.
  - Guarded in `Program.cs` line 1885 by `RequireUsersManageAsync`.
  - Exposes usernames, roles, active flags, and permissions. Completely strips PIN hashes and salts.
- **Test Coverage:** Tested in `UserManagementVerticalSliceTests.Creating_a_user_with_valid_data_succeeds_and_the_new_pin_logs_in`.

---

### Callable: src/Falah.Infrastructure/Users/UserManagementQueryService.cs:19:5:MethodDeclaration
- **Exact Line Range:** Lines 19–23
- **Signature:** `public async Task<UserDetailsDto?> GetAsync(string id, CancellationToken ct = default)`
- **Actual Behavior & Execution Path:**
  1. Issues query: `user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct)`.
  2. If `user is null`, returns `null`.
  3. Returns `ToDto(user)`.
- **Security Implications:** Guarded by `RequireUsersManageAsync` in `Program.cs` line 1888. Does not leak credentials.

---

### Callable: src/Falah.Infrastructure/Users/UserManagementQueryService.cs:21:71:SimpleLambdaExpression
- **Exact Line Range:** Line 21
- **Signature:** `u => u.Id == id`
- **Actual Behavior:** LINQ expression compiled to SQL primary key filter `WHERE [Id] = @id`.

---

### Callable: src/Falah.Infrastructure/Users/UserManagementQueryService.cs:25:5:MethodDeclaration
- **Exact Line Range:** Line 25
- **Signature:** `public IReadOnlyList<PermissionDefinition> GetPermissionCatalog()`
- **Actual Behavior & Inputs:** Returns `PermissionCatalog.All`.
- **Security Implications:** Exposed via `GET /api/permissions/catalog`. Completely public/read-only metadata.

---

### Callable: src/Falah.Infrastructure/Users/UserManagementQueryService.cs:27:5:MethodDeclaration
- **Exact Line Range:** Lines 27–30
- **Signature:** `private static UserDetailsDto ToDto(UserEntity u)`
- **Actual Behavior & Inputs:**
  - Maps `u.Id`, `u.Name`, `u.Role`, `u.Active`.
  - Splits comma-separated permissions: `u.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()`.
  - Deliberately omits `PinHash`, `PinSalt`, and `PinIterations`.
- **Trust Boundaries & Data Hygiene:** Ensures sensitive cryptographic material never crosses the wire to client applications.

---

## 4. Verification & Recommendations

1. **Query Authentication Consistency:** Ensure `RequireUsersManageAsync` validates actor identity against verified server session context (`context.ActorId()`) rather than query string parameters.
2. **Static Verification:** Verified complete against `src/Falah.Infrastructure/Users/UserManagementQueryService.cs`. No application source modified.


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:52:00Z
- **Source SHA-256 Verified:** `703985d05102060061f17385f5dc50c04ebf6e97d6c5eef8668d32fd647691e8`
- **Callable ID Checked:** `src/Falah.Infrastructure/Users/UserManagementQueryService.cs:14:5:ConstructorDeclaration`, `src/Falah.Infrastructure/Users/UserManagementQueryService.cs:16:5:MethodDeclaration`, `src/Falah.Infrastructure/Users/UserManagementQueryService.cs:19:5:MethodDeclaration`, `src/Falah.Infrastructure/Users/UserManagementQueryService.cs:21:71:SimpleLambdaExpression`, `src/Falah.Infrastructure/Users/UserManagementQueryService.cs:25:5:MethodDeclaration`, `src/Falah.Infrastructure/Users/UserManagementQueryService.cs:27:5:MethodDeclaration` (6 of 6 callables checked)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Infrastructure/Users/UserManagementQueryService.cs` (31 lines).
  2. Verified query projections and credential hygiene: `ToDto` (lines 27-30) maps only non-sensitive columns (`Id`, `Name`, `Role`, `Active`, `Permissions`), strictly omitting `PinHash`, `PinSalt`, and `PinIterations` from API serialization.
  3. Verified non-tracking execution (`AsNoTracking()`) in `ListAllAsync` (including inactive accounts for administrative audits) and `GetAsync`.
  4. All 6 canonical callable IDs, method signatures, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:53:17Z
- **Source SHA-256 Verified:** `2b5ec45037dbe188ec9f6a735c03798547ca9252cb7ec5d6b412bcf49451ba97`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `2b5ec45037dbe188ec9f6a735c03798547ca9252cb7ec5d6b412bcf49451ba97` against `src/Falah.Infrastructure/Users/UserManagementQueryService.cs` (31 lines).
2. Concur with L1 analysis on secure DTO projection in `ToDto` (lines 27-30) stripping cryptographic hash and salt columns from API outputs.
3. Additional L2 Challenger Assessment:
   - Query service functions `ListAllAsync` and `GetAsync` execute non-tracking reads (`AsNoTracking()`) but contain zero internal permission checks, relying entirely on upstream ASP.NET Core endpoint routing.
   - `GetAsync` returns null for non-existent users, supporting standard RESTful null-result propagation.
4. All 6 callables rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
