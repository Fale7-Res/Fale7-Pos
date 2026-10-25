# src/Falah.Infrastructure/Users/UserManagementQueryService.cs

- **Path:** `src/Falah.Infrastructure/Users/UserManagementQueryService.cs`
- **Component:** `Falah.Infrastructure.Users.UserManagementQueryService`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`UserManagementQueryService` provides read-only administrative projections of user accounts and permissions in Fale7 POS. Its architectural characteristics include:
- **Administrative Transparency:** Unlike `GET /api/users` (which returns only active users for operational till and lock-screen pickers), `UserManagementQueryService` projects both active and inactive accounts so that administrators can audit, inspect, and reactivate accounts ([UserManagementQueryService.cs#L8-L10](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L8-L10)).
- **Credential Hygiene:** Strictly strips credential secrets (`PinHash`, `PinSalt`, `PinIterations`) from projected DTOs, ensuring that password/PIN hashes are never serialized over the wire ([UserManagementQueryService.cs#L27-L29](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L27-L29)).
- **Permission Normalization:** Parses comma-separated permission strings into distinct, whitespace-trimmed collections (`IReadOnlyList<string>`).
- **Permission Catalog Authority:** Exposes the authoritative catalog of domain permission keys and localized labels via `PermissionCatalog.All`.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Signature | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-UMQ-001` | Constructor | `public UserManagementQueryService(FalahDbContext db)` | 14 | Substantive (DI Hook) |
| `CAL-UMQ-002` | `ListAllAsync` | `public async Task<List<UserDetailsDto>> ListAllAsync(CancellationToken ct = default)` | 16-17 | Substantive (Data Projection) |
| `CAL-UMQ-003` | `ListAllAsync::<method_group>ToDto` | `ToDto` | 17 | Trivial / Method Group Projection |
| `CAL-UMQ-004` | `GetAsync` | `public async Task<UserDetailsDto?> GetAsync(string id, CancellationToken ct = default)` | 19-23 | Substantive (Data Retrieval) |
| `CAL-UMQ-005` | `GetAsync::<lambda_user_lookup>` | `u => u.Id == id` | 21 | Generated / Linq Expression |
| `CAL-UMQ-006` | `GetPermissionCatalog` | `public IReadOnlyList<PermissionDefinition> GetPermissionCatalog()` | 25 | Substantive (Domain Catalog) |
| `CAL-UMQ-007` | `ToDto` | `private static UserDetailsDto ToDto(UserEntity u)` | 27-29 | Substantive (DTO Transformer) |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-UMQ-001: Constructor `UserManagementQueryService(FalahDbContext db)`
- **Line:** [14](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L14)
- **Actual Behavior & Inputs:** Injects `FalahDbContext` into private readonly field `_db`.
- **Callers & Dependencies:** Resolved via ASP.NET Core DI in `Program.cs` for endpoints `GET /api/users/all` and `GET /api/users/{id}`.
- **Side Effects:** Stores DB context reference.
- **Classification Justification:** Substantive dependency injection hook.

---

### CAL-UMQ-002: `ListAllAsync(CancellationToken ct = default)`
- **Line Span:** [16-17](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L16-L17)
- **Actual Behavior & Inputs:**
  1. Executes `await _db.Users.AsNoTracking().ToListAsync(ct)` to retrieve all users from the SQL database without tracking overhead.
  2. Projects in-memory using `.Select(ToDto).ToList()`.
  3. Returns a complete list of `UserDetailsDto` (containing both active and inactive users).
- **Callers & Dependencies:**
  - Invoked exclusively by `GET /api/users/all` in `src/Falah.Server/Program.cs` lines 1885-1886.
- **Side Effects:** Issues SQL `SELECT` query on the `Users` table.
- **Trust Boundaries & Security Implications:**
  - **Endpoint Authorization Gate:** `GET /api/users/all` is protected at the Minimal API layer by `await RequireUsersManageAsync(db, actorUserId, ct)`. An authenticated caller lacking `users_manage` or `"all"` receives `422 UnprocessableEntity`.
  - **Credential Safety:** The projection strictly omits `PinHash`, `PinSalt`, and `PinIterations`. Only public identifiers and granted permission lists are returned.
  - **Memory Impact:** In realistic restaurant deployments (10-50 staff accounts), loading all users into memory is fast and negligible in memory consumption.
- **Failure Modes:** Database connectivity errors or cancellation token cancellation.
- **Test Coverage:**
  - *Covered:* `AuthenticationTests.The_user_list_is_not_readable_without_logging_in`, `UserManagementVerticalSliceTests`.
  - *Missing Tests:* Missing verification that `ListAllAsync` returns inactive accounts while `GET /api/users` excludes them.

---

### CAL-UMQ-003: `ListAllAsync::<method_group>ToDto`
- **Line:** [17](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L17)
- **Actual Behavior & Inputs:** Method group delegate reference passed to LINQ `Select` to transform each `UserEntity` into a `UserDetailsDto`.
- **Classification Justification:** Trivial method group reference.

---

### CAL-UMQ-004: `GetAsync(string id, CancellationToken ct = default)`
- **Line Span:** [19-23](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L19-L23)
- **Actual Behavior & Inputs:**
  1. Queries `_db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct)`.
  2. If user is null, returns `null`.
  3. Otherwise, returns `ToDto(user)`.
- **Callers & Dependencies:**
  - Invoked by `GET /api/users/{id}` in `src/Falah.Server/Program.cs` lines 1888-1894.
- **Side Effects:** Issues SQL `SELECT TOP(1)` query by primary key.
- **Trust Boundaries & Security Implications:**
  - Protected at endpoint level by `RequireUsersManageAsync(db, actorUserId, ct)`.
  - Safe against SQL injection (parameterized by EF Core).
- **Test Coverage:**
  - *Covered:* Tested indirectly via user modification tests.
  - *Missing Tests:* Direct unit test asserting that querying an unknown `id` returns `null` rather than throwing.

---

### CAL-UMQ-005: `GetAsync::<lambda_user_lookup>` (`u => u.Id == id`)
- **Line:** [21](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L21)
- **Actual Behavior & Inputs:** Expression predicate for `FirstOrDefaultAsync` performing primary key lookup on `id`.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL query filter.

---

### CAL-UMQ-006: `GetPermissionCatalog()`
- **Line:** [25](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L25)
- **Actual Behavior & Inputs:** Takes no inputs; returns static read-only list `PermissionCatalog.All`.
- **Callers & Dependencies:**
  - Invoked by `GET /api/permissions/catalog` in `src/Falah.Server/Program.cs` line 1896.
  - Dependencies: `Falah.Domain.Authorization.PermissionCatalog`.
- **Side Effects:** None (pure in-memory getter).
- **Trust Boundaries & Security Implications:**
  - Exposes metadata about system permissions (keys, descriptions, roles).
  - Contains no secret or tenant-specific information. Safe for authenticated client consumption to render permission toggle UIs.
- **Test Coverage:**
  - *Covered:* Used during UI initialization and catalog validation.
  - *Missing Tests:* Missing unit test asserting immutability of the returned collection.

---

### CAL-UMQ-007: `ToDto(UserEntity u)`
- **Line Span:** [27-29](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UserManagementQueryService.cs#L27-L29)
- **Actual Behavior & Inputs:**
  1. Accepts `UserEntity u`.
  2. Parses `u.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()`.
  3. Constructs and returns `UserDetailsDto(u.Id, u.Name, u.Role, u.Active, permissions)`.
- **Side Effects:** In-memory allocation of string array and List.
- **Trust Boundaries & Security Implications:**
  - Ensures robust parsing even if the database contains extra whitespace or trailing commas.
  - Redacts sensitive columns (`PinHash`, `PinSalt`, `PinIterations`).
- **Test Coverage:**
  - *Covered:* Exercised across all user query operations.
  - *Missing Tests:* Edge cases with empty permissions string (`""`) or permissions with mixed whitespace.

---

## 4. Verification & Recommendations

1. **Defensive Validation:** Add explicit test cases for `ToDto` handling malformed or null `Permissions` strings to ensure no unhandled exceptions can be triggered during list operations.
2. **Verification:** Static source audit confirmed; no application source modified.
