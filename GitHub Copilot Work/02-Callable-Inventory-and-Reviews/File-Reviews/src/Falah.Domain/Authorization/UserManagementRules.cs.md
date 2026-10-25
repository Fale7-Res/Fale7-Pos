# src/Falah.Domain/Authorization/UserManagementRules.cs

- **Path:** `src/Falah.Domain/Authorization/UserManagementRules.cs`
- **Component:** `Falah.Domain.Authorization.UserManagementRules`
- **Assembly:** `Falah.Domain`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`UserManagementRules.cs` defines the domain-level invariants governing user administrative operations in Fale7 POS. It directly resolves critical security flaws identified in the legacy JavaScript reference application, where owner protections and privilege checks were implemented purely as client-side UI guards (disabled buttons in `UsersPermissionsScreen.tsx`) that could be completely bypassed by issuing raw API commands.

### Core Security Invariants:
1. **Owner Account Immutability (`AssertTargetIsNotOwner`):** Enforces at the domain boundary that the owner account can never be deactivated, demoted, or stripped of permissions by any administrative command.
2. **Privilege Escalation Containment (`AssertNoPrivilegeEscalation`):** Enforces the standard security principle that an administrator cannot grant permissions they do not personally possess (unless they hold the wildcard superuser permission `"all"`).
3. **Surviving Administrator Guarantee (`AssertUserAdministrationSurvives`):** Prevents the permanent lockout of the POS terminal by ensuring at least one active administrator with `users_manage` survives any deactivation or permission revocation.
4. **Catalog Integrity (`AssertKnownPermissions`):** Rejects unknown or malformed permission strings, ensuring only permissions registered in `PermissionCatalog` can be assigned.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Signature | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-UMR-001` | `AssertTargetIsNotOwner` | `public static void AssertTargetIsNotOwner(string targetRole, string action)` | [20-24](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L20-L24) | Substantive (Security Boundary) |
| `CAL-UMR-002` | `AssertNoPrivilegeEscalation` | `public static void AssertNoPrivilegeEscalation(IReadOnlySet<string> actorPermissions, IEnumerable<string> permissionsToGrant)` | [29-35](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L29-L35) | Substantive (Security Boundary) |
| `CAL-UMR-003` | `AssertNoPrivilegeEscalation::<lambda_notHeld>` | `p => !actorPermissions.Contains(p)` | [32](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L32) | Lambda Predicate |
| `CAL-UMR-004` | `CanManageUsers` | `public static bool CanManageUsers(IEnumerable<string> permissions)` | [44-48](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L44-L48) | Substantive (Policy Evaluation) |
| `CAL-UMR-005` | `AssertUserAdministrationSurvives` | `public static void AssertUserAdministrationSurvives(int remainingActiveAdministrators)` | [57-63](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L57-L63) | Substantive (Safety Guarantee) |
| `CAL-UMR-006` | `AssertKnownPermissions` | `public static void AssertKnownPermissions(IEnumerable<string> permissions)` | [65-70](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L65-L70) | Substantive (Input Validation) |
| `CAL-UMR-007` | `AssertKnownPermissions::<lambda_unknown>` | `p => !PermissionCatalog.Keys.Contains(p)` | [67](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L67) | Lambda Predicate |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-UMR-001
- **Callable ID:** `src/Falah.Domain/Authorization/UserManagementRules.cs:20:5:MethodDeclaration`
- **Exact Line Range:** Lines 20–24
- **Signature:** `AssertTargetIsNotOwner(string, string)`: `AssertTargetIsNotOwner(string targetRole, string action)`
- **Line Span:** [20-24](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L20-L24)
- **Actual Behavior & Inputs:**
  - Evaluates `if (targetRole == OwnerRole)` (where `OwnerRole = "owner"`).
  - If equal, throws `DomainException($"لا يمكن {action} حساب المالك.")`.
- **Callers & Dependencies:**
  - Invoked by `SetUserActiveHandler.cs` (line 35) to prevent deactivating the owner.
  - Invoked by `UpdateUserPermissionsHandler.cs` (line 38) to prevent altering owner permissions.
- **Side Effects:** Pure assertion; throws domain exception on invariant breach.
- **Trust Boundaries & Security Implications:**
  - **Owner Protection:** Closes the major legacy vulnerability where any user with `users_manage` could deactivate the restaurant owner.
  - **Exempt Self-Service PIN Change:** The owner can still change their own PIN via `ChangePinHandler` because self-service PIN change authorizes by knowledge of current PIN, rather than administrative `users_manage`.
  - **Exact String Sensitivity:** Checks `targetRole == OwnerRole` with exact ordinal casing. If a database role were ever stored in mixed casing (`"Owner"`), this check would fail to match. However, role seeding is strictly normalized.
- **Test Coverage:** Tested in `UserManagementRulesTests.Cannot_deactivate_or_modify_owner`.

---

### CAL-UMR-002
- **Callable ID:** `src/Falah.Domain/Authorization/UserManagementRules.cs:29:5:MethodDeclaration`
- **Exact Line Range:** Lines 29–35
- **Signature:** `AssertNoPrivilegeEscalation(IReadOnlySet<string>, IEnumerable<string>)` & CAL-UMR-003: `AssertNoPrivilegeEscalation` & Lambda
- **Line Span:** [29-35](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L29-L35)
- **Actual Behavior & Inputs:**
  1. Superuser Shortcut: If `actorPermissions.Contains("all")`, returns immediately.
  2. Missing Permissions Search: Executes `permissionsToGrant.Where(p => !actorPermissions.Contains(p)).ToList()`.
  3. Escalation Check: If `notHeld.Count > 0`, throws `DomainException($"لا يمكنك منح صلاحيات لا تملكها بنفسك: {string.Join(", ", notHeld)}.")`.
- **Callers & Dependencies:**
  - Invoked by `CreateUserHandler.cs` (line 53).
  - Invoked by `UpdateUserPermissionsHandler.cs` (line 42).
- **Side Effects:** Pure assertion.
- **Trust Boundaries & Security Implications:**
  - **Horizontal/Vertical Escalation Guard:** Guarantees that a junior manager or supervisor with `users_manage` cannot grant `financial_reports_view`, `backups_restore`, or `orders_comp` to themselves or others unless they already hold those permissions.
  - **Revocation Exemption:** Taking away permissions is intentionally exempt from this assertion, as revoking permissions is never a privilege escalation.
  - **Critical Exploit Interaction (SEC-001):** When SEC-001 (JSON actor casing mismatch) is exploited, an attacker provides `"ActorUserId": "<owner-id>"`. Minimal API deserializes this as the owner, causing `actorPermissions` to resolve to the owner's full permissions, completely evading this escalation guard!
- **Test Coverage:** Tested in `UserManagementRulesTests.User_cannot_grant_permissions_they_do_not_hold`.

---

### CAL-UMR-004
- **Callable ID:** `src/Falah.Domain/Authorization/UserManagementRules.cs:44:5:MethodDeclaration`
- **Exact Line Range:** Lines 44–48
- **Signature:** `CanManageUsers(IEnumerable<string>)`: `CanManageUsers(IEnumerable<string> permissions)`
- **Line Span:** [44-48](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L44-L48)
- **Actual Behavior & Inputs:**
  - Converts `permissions` to `IReadOnlySet<string>` if not already one.
  - Returns `set.Contains("all") || set.Contains(UserManagementPermission)` (where `UserManagementPermission = "users_manage"`).
- **Callers & Dependencies:**
  - Invoked by `SetUserActiveHandler.cs` and `UpdateUserPermissionsHandler.cs` when calculating the count of surviving active administrators.
- **Trust Boundaries & Security Implications:**
  - Correctly recognizes wildcard `"all"` as granting user management authority, preventing holders of `"all"` from being miscounted as non-administrators.
- **Test Coverage:** Tested in `UserManagementRulesTests.CanManageUsers_recognizes_users_manage_and_all`.

---

### CAL-UMR-005
- **Callable ID:** `src/Falah.Domain/Authorization/UserManagementRules.cs:57:5:MethodDeclaration`
- **Exact Line Range:** Lines 57–63
- **Signature:** `AssertUserAdministrationSurvives(int)`: `AssertUserAdministrationSurvives(int remainingActiveAdministrators)`
- **Line Span:** [57-63](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L57-L63)
- **Actual Behavior & Inputs:**
  - Checks `if (remainingActiveAdministrators <= 0)`.
  - Throws `DomainException` with detailed Arabic message explaining that no active administrator would remain.
- **Callers & Dependencies:**
  - Invoked by `SetUserActiveHandler.cs` (line 44) before deactivating a user.
  - Invoked by `UpdateUserPermissionsHandler.cs` (line 52) before revoking `users_manage`.
- **Side Effects:** Pure assertion.
- **Trust Boundaries & Security Implications:**
  - **Permanent Lockout Prevention:** Closes the vulnerability where the last user holding `users_manage` is deactivated or demoted (either accidentally by themselves or via a manager approval token), leaving the till without any administrative user.
  - **Concurrency Hazard:** The calculation of `remainingActiveAdministrators` is performed in EF Core read queries before executing the update. If two administrators attempt to deactivate each other simultaneously, a race condition exists without row locking where both queries see `count == 2`, both decrement to `1`, and both commit, leaving 0 active administrators.
- **Test Coverage:** Tested in `UserManagementRulesTests.Cannot_remove_last_user_manager`.

---

### CAL-UMR-006
- **Callable ID:** `src/Falah.Domain/Authorization/UserManagementRules.cs:65:5:MethodDeclaration`
- **Exact Line Range:** Lines 65–70
- **Signature:** `AssertKnownPermissions(IEnumerable<string>)` & CAL-UMR-007: `AssertKnownPermissions` & Lambda
- **Line Span:** [65-70](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L65-L70)
- **Actual Behavior & Inputs:**
  - Evaluates `unknown = permissions.Where(p => !PermissionCatalog.Keys.Contains(p)).ToList()`.
  - If `unknown.Count > 0`, throws `DomainException($"صلاحيات غير معروفة: {string.Join(", ", unknown)}.")`.
- **Callers & Dependencies:**
  - Invoked by `CreateUserHandler.cs` (line 52) and `UpdateUserPermissionsHandler.cs` (line 41).
- **Side Effects:** Pure assertion.
- **Trust Boundaries & Security Implications:**
  - **Permission Whitelisting:** Prevents assigning arbitrary, misspelled, or obsolete permission strings to users.
  - Note: Does `PermissionCatalog.Keys` contain `"all"`? `PermissionCatalog` registers standard granular keys; `"all"` is checked separately where applicable.
- **Test Coverage:** Tested in `UserManagementRulesTests.Rejects_unknown_permissions`.

---

## 4. Concurrency & Security Synthesis

1. **Defense-in-Depth Reliance on Caller Authentication:**
   The methods in `UserManagementRules` are pure domain assertions. They operate on whatever `actorPermissions` collection is passed to them. When command handlers trust `request.ActorUserId` instead of `HttpContext.ActorId()`, these domain rules are rendered ineffective against identity spoofing (SEC-001).
2. **Surviving Administrator Concurrency Gap:**
   In `SetUserActiveHandler` and `UpdateUserPermissionsHandler`, `remainingActiveAdministrators` is evaluated via `await _db.Users.CountAsync(...)`. In high-concurrency environments with multiple terminals, concurrent deactivations or revocations can race past `AssertUserAdministrationSurvives` unless serialized with table locks or serializable transaction isolation.

---

## 5. Verification & Recommendations

1. **Transaction Isolation for Admin Survival:** Enforce serializable isolation or an atomic database constraint on `AssertUserAdministrationSurvives` to prevent concurrent mutual deactivation.
2. **Context Binding:** Ensure handlers invoke `UserManagementRules` using actor permissions retrieved from the cryptographically verified session token (`context.Actor()`), eliminating SEC-001 bypass vectors.
3. **Static Verification:** Verified complete against `src/Falah.Domain/Authorization/UserManagementRules.cs`. No application source modified.

- **Canonical Callable ID:** `src/Falah.Domain/Authorization/UserManagementRules.cs:32:48:SimpleLambdaExpression` (Lines 32–32, `SimpleLambdaExpression`)
- **Canonical Callable ID:** `src/Falah.Domain/Authorization/UserManagementRules.cs:67:41:SimpleLambdaExpression` (Lines 67–67, `SimpleLambdaExpression`)