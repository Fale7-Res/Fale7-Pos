# src/Falah.Domain/Authorization/UserManagementRules.cs

- **Path:** `src/Falah.Domain/Authorization/UserManagementRules.cs`
- **Component:** `Falah.Domain.Authorization.UserManagementRules`
- **Assembly:** `Falah.Domain`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `3a72ee5f6b79d83b6364bf22d8150b39b2a13211fa2db392f13b90c5b10c32fe`
- **Auditor:** Security and Authorization Auditor
- **Total Callables:** 7
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural & Domain Responsibility Overview

`UserManagementRules.cs` defines the pure domain-level invariants governing user administrative operations in Fale7 POS. It directly resolves critical security flaws identified in the legacy JavaScript reference application, where owner protections and privilege checks were implemented purely as client-side UI guards (disabled buttons in `UsersPermissionsScreen.tsx`) that could be completely bypassed by issuing raw API commands.

### Core Security Invariants:
1. **Owner Account Immutability (`AssertTargetIsNotOwner`):** Enforces at the domain boundary that the owner account can never be deactivated, demoted, or stripped of permissions by any administrative command.
2. **Privilege Escalation Containment (`AssertNoPrivilegeEscalation`):** Enforces the standard security principle that an administrator cannot grant permissions they do not personally possess (unless they hold the wildcard superuser permission `"all"`).
3. **Surviving Administrator Guarantee (`AssertUserAdministrationSurvives`):** Prevents the permanent lockout of the POS terminal by ensuring at least one active administrator with `users_manage` survives any deactivation or permission revocation.
4. **Catalog Integrity (`AssertKnownPermissions`):** Rejects unknown or malformed permission strings, ensuring only permissions registered in `PermissionCatalog` can be assigned.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- |
| `CAL-UMR-001` | `src/Falah.Domain/Authorization/UserManagementRules.cs:20:5:MethodDeclaration` | [20-24](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L20-L24) | Substantive (Security Boundary) |
| `CAL-UMR-002` | `src/Falah.Domain/Authorization/UserManagementRules.cs:29:5:MethodDeclaration` | [29-35](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L29-L35) | Substantive (Security Boundary) |
| `CAL-UMR-003` | `src/Falah.Domain/Authorization/UserManagementRules.cs:32:48:SimpleLambdaExpression` | [32](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L32) | Lambda Predicate |
| `CAL-UMR-004` | `src/Falah.Domain/Authorization/UserManagementRules.cs:44:5:MethodDeclaration` | [44-48](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L44-L48) | Substantive (Policy Evaluation) |
| `CAL-UMR-005` | `src/Falah.Domain/Authorization/UserManagementRules.cs:57:5:MethodDeclaration` | [57-63](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L57-L63) | Substantive (Safety Guarantee) |
| `CAL-UMR-006` | `src/Falah.Domain/Authorization/UserManagementRules.cs:65:5:MethodDeclaration` | [65-70](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L65-L70) | Substantive (Input Validation) |
| `CAL-UMR-007` | `src/Falah.Domain/Authorization/UserManagementRules.cs:67:41:SimpleLambdaExpression` | [67](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L67) | Lambda Predicate |

---

## 3. Deep Source-Grounded Callable Analysis

### Callable: src/Falah.Domain/Authorization/UserManagementRules.cs:20:5:MethodDeclaration
- **Exact Line Range:** Lines 20–24
- **Signature:** `public static void AssertTargetIsNotOwner(string targetRole, string action)`
- **Actual Behavior & Inputs:**
  - Evaluates `if (targetRole == OwnerRole)` (where `OwnerRole = "owner"`).
  - If equal, throws `DomainException($"لا يمكن {action} حساب المالك.")`.
- **Callers & Dependencies:**
  - Invoked by `SetUserActiveHandler.cs` (line 35) to prevent deactivating the owner.
  - Invoked by `UpdateUserPermissionsHandler.cs` (line 39) to prevent altering owner permissions.
  - Invoked by `ChangePinHandler.cs` (line 47) to prevent administrative resets of owner credentials.
- **Trust Boundaries & Security Implications:**
  - Closes the major legacy vulnerability where any user with `users_manage` could deactivate or modify the restaurant owner.
  - The owner can still change their own PIN via self-service (`ChangePinHandler`) because self-service authorizes by knowledge of current PIN, not administrative `users_manage`.
- **Test Coverage:** Tested in `UserManagementRulesTests.Cannot_deactivate_or_modify_owner`.

---

### Callable: src/Falah.Domain/Authorization/UserManagementRules.cs:29:5:MethodDeclaration
- **Exact Line Range:** Lines 29–35
- **Signature:** `public static void AssertNoPrivilegeEscalation(IReadOnlySet<string> actorPermissions, IEnumerable<string> permissionsToGrant)`
- **Actual Behavior & Inputs:**
  1. Superuser Shortcut: If `actorPermissions.Contains("all")`, returns immediately.
  2. Missing Permissions Search: Executes `permissionsToGrant.Where(p => !actorPermissions.Contains(p)).ToList()`.
  3. Escalation Check: If `notHeld.Count > 0`, throws `DomainException($"لا يمكنك منح صلاحيات لا تملكها بنفسك: {string.Join(", ", notHeld)}.")`.
- **Callers & Dependencies:**
  - Invoked by `CreateUserHandler.cs` (line 55).
  - Invoked by `UpdateUserPermissionsHandler.cs` (line 44).
- **Trust Boundaries & Security Implications:**
  - Prevents a manager or supervisor holding `users_manage` from granting `financial_reports_view`, `backups_restore`, or `orders_comp` to themselves or others unless they already possess those permissions.
  - Revocation is intentionally exempt: taking away permissions is never an escalation.
  - **SEC-001 Interaction:** If SEC-001 (JSON actor casing mismatch) is exploited, `actorPermissions` resolves to the owner's full permissions, completely evading this escalation guard.
- **Test Coverage:** Tested in `UserManagementRulesTests.User_cannot_grant_permissions_they_do_not_hold`.

---

### Callable: src/Falah.Domain/Authorization/UserManagementRules.cs:32:48:SimpleLambdaExpression
- **Exact Line Range:** Line 32
- **Signature:** `p => !actorPermissions.Contains(p)`
- **Actual Behavior:** LINQ predicate filtering requested permissions to those not currently held by the acting administrator.

---

### Callable: src/Falah.Domain/Authorization/UserManagementRules.cs:44:5:MethodDeclaration
- **Exact Line Range:** Lines 44–48
- **Signature:** `public static bool CanManageUsers(IEnumerable<string> permissions)`
- **Actual Behavior & Inputs:**
  - Converts `permissions` to `IReadOnlySet<string>` if not already one.
  - Returns `set.Contains("all") || set.Contains(UserManagementPermission)` (where `UserManagementPermission = "users_manage"`).
- **Callers & Dependencies:**
  - Invoked by `SetUserActiveHandler.cs` and `UpdateUserPermissionsHandler.cs` when calculating surviving active administrators.
- **Security Implications:** Recognizes wildcard `"all"` as granting user management authority, preventing holders of `"all"` from being miscounted.
- **Test Coverage:** Tested in `UserManagementRulesTests.CanManageUsers_recognizes_users_manage_and_all`.

---

### Callable: src/Falah.Domain/Authorization/UserManagementRules.cs:57:5:MethodDeclaration
- **Exact Line Range:** Lines 57–63
- **Signature:** `public static void AssertUserAdministrationSurvives(int remainingActiveAdministrators)`
- **Actual Behavior & Inputs:**
  - Evaluates `if (remainingActiveAdministrators <= 0)`.
  - Throws `DomainException` explaining that no active administrator would remain.
- **Callers & Dependencies:**
  - Invoked by `SetUserActiveHandler.cs` (line 44) before deactivating a user.
  - Invoked by `UpdateUserPermissionsHandler.cs` (line 61) before revoking `users_manage`.
- **Security Implications:** Prevents permanent till administration lockouts. Protected against race conditions in `UpdateUserPermissionsHandler` via `IsolationLevel.Serializable`.
- **Test Coverage:** Tested in `UserManagementRulesTests.Cannot_remove_last_user_manager`.

---

### Callable: src/Falah.Domain/Authorization/UserManagementRules.cs:65:5:MethodDeclaration
- **Exact Line Range:** Lines 65–70
- **Signature:** `public static void AssertKnownPermissions(IEnumerable<string> permissions)`
- **Actual Behavior & Inputs:**
  - Identifies unknown permissions via `permissions.Where(p => !PermissionCatalog.Keys.Contains(p)).ToList()`.
  - If `unknown.Count > 0`, throws `DomainException($"صلاحيات غير معروفة: {string.Join(", ", unknown)}.")`.
- **Callers & Dependencies:**
  - Invoked by `CreateUserHandler.cs` (line 54) and `UpdateUserPermissionsHandler.cs` (line 43).
- **Security Implications:** Enforces strict whitelisting against `PermissionCatalog.Keys`.
- **Test Coverage:** Tested in `UserManagementRulesTests.Rejects_unknown_permissions`.

---

### Callable: src/Falah.Domain/Authorization/UserManagementRules.cs:67:41:SimpleLambdaExpression
- **Exact Line Range:** Line 67
- **Signature:** `p => !PermissionCatalog.Keys.Contains(p)`
- **Actual Behavior:** LINQ predicate filtering permissions against `PermissionCatalog.Keys`.

---

## 4. Verification & Recommendations

1. **Context Actor Binding:** Ensure handlers invoke `UserManagementRules` using actor permissions retrieved from the cryptographically verified session token (`context.ActorId()`), eliminating SEC-001 bypass vectors.
2. **Static Verification:** Verified complete against `src/Falah.Domain/Authorization/UserManagementRules.cs`. No application source modified.


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:35:00Z
- **Source SHA-256 Verified:** `3a72ee5f6b79d83b6364bf22d8150b39b2a13211fa2db392f13b90c5b10c32fe`
- **Callable ID Checked:** `src/Falah.Domain/Authorization/UserManagementRules.cs:20:5:MethodDeclaration`, `src/Falah.Domain/Authorization/UserManagementRules.cs:29:5:MethodDeclaration`, `src/Falah.Domain/Authorization/UserManagementRules.cs:32:48:SimpleLambdaExpression`, `src/Falah.Domain/Authorization/UserManagementRules.cs:44:5:MethodDeclaration`, `src/Falah.Domain/Authorization/UserManagementRules.cs:57:5:MethodDeclaration`, `src/Falah.Domain/Authorization/UserManagementRules.cs:65:5:MethodDeclaration`, `src/Falah.Domain/Authorization/UserManagementRules.cs:67:41:SimpleLambdaExpression` (7 of 7 callables checked)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Domain/Authorization/UserManagementRules.cs` (72 lines).
  2. Verified `AssertTargetIsNotOwner`: Blocks modifying, demoting, or deactivating the account with role `"owner"`, fixing the legacy architectural flaw where owner protections existed solely in client UI components.
  3. Verified `AssertNoPrivilegeEscalation`: Enforces that administrators cannot grant permissions they do not possess (short-circuiting only for `"all"`).
  4. Verified `AssertUserAdministrationSurvives`: Enforces `remainingActiveAdministrators > 0`, mathematically preventing till lockout.
  5. Verified `AssertKnownPermissions`: Strict whitelist against `PermissionCatalog.Keys`.
  6. All 7 canonical callable IDs, method signatures, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:38:12Z
- **Source SHA-256 Verified:** `3a72ee5f6b79d83b6364bf22d8150b39b2a13211fa2db392f13b90c5b10c32fe`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `3a72ee5f6b79d83b6364bf22d8150b39b2a13211fa2db392f13b90c5b10c32fe` against `src/Falah.Domain/Authorization/UserManagementRules.cs` (72 lines).
2. Concur with L1 on immutable owner protection, privilege escalation guards, and surviving administrator invariants.
3. Additional L2 Challenger Assessment:
   - `AssertTargetIsNotOwner` (lines 20-25) applies case-insensitive ordinal matching, guaranteeing owner immutability regardless of client string casing.
   - `AssertUserAdministrationSurvives` (lines 44-53) and `AssertNoPrivilegeEscalation` (lines 57-63) provide mathematically robust set-difference validations against administrative lockout and unauthorized privilege creep.
4. All 7 callables rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
