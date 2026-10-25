# src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs

- **Path:** `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs`
- **Component:** `Falah.Client.CashierWinUI.ViewModels.UsersViewModel`
- **Assembly:** `Falah.Client.CashierWinUI`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`UsersViewModel.cs` is the primary WinUI presentation controller for user administration in the Fale7 Cashier application. It manages the retrieval of staff accounts, dynamic rendering of permission toggles based on the server's authoritative catalog, diffing of permission grants and revocations, and user activation/deactivation.

### Client-Side Security Responsibilities:
1. **Dynamic Permission Catalog Binding:** Rather than hardcoding a static list of permissions in XAML or C#, `UsersViewModel` fetches the authoritative catalog from `GET /api/permissions/catalog` via `_session.Api.GetPermissionCatalogAsync()`, eliminating UI-backend permission drift.
2. **Client-Side Privilege Escalation Prevention:** In `RebuildToggles`, initializes `PermissionToggle.CanActorGrant` to disable toggles for permissions the current logged-in user does not personally hold (`_session.HasPermission(def.Key)`), aligning UI behavior with `UserManagementRules.AssertNoPrivilegeEscalation`.
3. **Owner Account Immunity:** Evaluates `IsOwnerSelected` (`SelectedUser?.Role == "owner"`), completely disabling administrative edit actions (`CanEditSelected`) to prevent cashiers or administrators from attempting deactivations or permission changes against the protected owner account.
4. **Durable Request Coordination:** Interacts with `RetainedRequest<UpdateUserPermissionsRequest>` (`_pendingPermissions`) and `RetainedRequest<SetUserActiveRequest>` (`_pendingActive`) to preserve pending operations across network ambiguities or terminal power cuts.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-UVM-001` | Constructor | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:23:5:ConstructorDeclaration` | [23](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L23) | Constructor (DI Hook) |
| `CAL-UVM-002` | `PermissionToggle.Key::get` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:28:38:GetAccessorDeclaration` | [28](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L28) | Property Accessor |
| `CAL-UVM-003` | `PermissionToggle.Key::init` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:28:43:InitAccessorDeclaration` | [28](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L28) | Property Accessor |
| `CAL-UVM-004` | `PermissionToggle.Category::get` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:29:43:GetAccessorDeclaration` | [29](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L29) | Property Accessor |
| `CAL-UVM-005` | `PermissionToggle.Category::init` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:29:48:InitAccessorDeclaration` | [29](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L29) | Property Accessor |
| `CAL-UVM-006` | `PermissionToggle.CanActorGrant::get` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:60:46:GetAccessorDeclaration` | [60](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L60) | Property Accessor |
| `CAL-UVM-007` | `PermissionToggle.CanActorGrant::init` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:60:51:InitAccessorDeclaration` | [60](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L60) | Property Accessor |
| `CAL-UVM-008` | `Users::get` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:63:57:GetAccessorDeclaration` | [63](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L63) | Property Accessor |
| `CAL-UVM-009` | `Permissions::get` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:64:65:GetAccessorDeclaration` | [64](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L64) | Property Accessor |
| `CAL-UVM-010` | `OnIsBusyChanged` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:82:5:MethodDeclaration` | [82](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L82) | State Notification |
| `CAL-UVM-011` | `OnIsLoadingChanged` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:83:5:MethodDeclaration` | [83](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L83) | State Notification |
| `CAL-UVM-012` | `OnLoadErrorChanged` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:84:5:MethodDeclaration` | [84](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L84) | State Notification |
| `CAL-UVM-013` | `OnSelectedUserChanged` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:86:5:MethodDeclaration` | [86-91](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L86-L91) | Selection Lifecycle |
| `CAL-UVM-014` | `RebuildToggles` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:93:5:MethodDeclaration` | [93-105](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L93-L105) | UI Projection |
| `CAL-UVM-015` | `RebuildToggles::<lambda_category>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:97:46:SimpleLambdaExpression` | [97](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L97) | Lambda Expression |
| `CAL-UVM-016` | `RebuildToggles::<lambda_key>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:97:70:SimpleLambdaExpression` | [97](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L97) | Lambda Expression |
| `CAL-UVM-017` | `LoadAsync` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:107:5:MethodDeclaration` | [107-112](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L107-L112) | Relay Command |
| `CAL-UVM-018` | `ReloadAsync` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:114:5:MethodDeclaration` | [114-143](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L114-L143) | Data Orchestration |
| `CAL-UVM-019` | `ReloadAsync::<lambda_user_match>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:136:49:SimpleLambdaExpression` | [136](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L136) | Lambda Expression |
| `CAL-UVM-020` | `SelectUser` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:145:5:MethodDeclaration` | [145-146](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L145-L146) | Relay Command |
| `CAL-UVM-021` | `SavePermissionsAsync` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:148:5:MethodDeclaration` | [148-164](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L148-L164) | Relay Command |
| `CAL-UVM-022` | `SavePermissionsAsync::<lambda_grant_pred>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:155:39:SimpleLambdaExpression` | [155](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L155) | Lambda Expression |
| `CAL-UVM-023` | `SavePermissionsAsync::<lambda_grant_proj>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:155:109:SimpleLambdaExpression` | [155](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L155) | Lambda Expression |
| `CAL-UVM-024` | `SavePermissionsAsync::<lambda_revoke_pred>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:156:40:SimpleLambdaExpression` | [156](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L156) | Lambda Expression |
| `CAL-UVM-025` | `SavePermissionsAsync::<lambda_revoke_proj>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:156:110:SimpleLambdaExpression` | [156](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L156) | Lambda Expression |
| `CAL-UVM-026` | `SendPermissionsAsync` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:166:5:MethodDeclaration` | [166-182](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L166-L182) | Command Execution |
| `CAL-UVM-027` | `SendPermissionsAsync::<lambda_api>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:172:71:SimpleLambdaExpression` | [172](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L172) | Lambda Expression |
| `CAL-UVM-028` | `ToggleActiveAsync` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:184:5:MethodDeclaration` | [184-203](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L184-L203) | Relay Command |
| `CAL-UVM-029` | `ToggleActiveAsync::<lambda_api>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:193:66:SimpleLambdaExpression` | [193](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs#L193) | Lambda Expression |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-UVM-001: Constructor `UsersViewModel(AppSession session)`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:23:5:ConstructorDeclaration`
- **Exact Line Range:** Line 23
- **Signature:** `public UsersViewModel(AppSession session)`
- **Actual Behavior & Inputs:** Stores the authenticated ambient `AppSession` in private readonly field `_session`.
- **Dependencies:** `AppSession` provides current user details (`CurrentUser`), permission checks (`HasPermission`), and API client gateway (`_session.Api`).
- **Security Implications:** View model actions rely on `_session.CurrentUser` to supply caller identity when minting command requests.

---

### CAL-UVM-002 to CAL-UVM-007: `PermissionToggle` Accessors
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:28:38:GetAccessorDeclaration` (`Key::get`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:28:43:InitAccessorDeclaration` (`Key::init`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:29:43:GetAccessorDeclaration` (`Category::get`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:29:48:InitAccessorDeclaration` (`Category::init`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:60:46:GetAccessorDeclaration` (`CanActorGrant::get`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:60:51:InitAccessorDeclaration` (`CanActorGrant::init`)
- **Exact Line Range:** Lines 28, 29, 60
- **Actual Behavior & Inputs:** Encapsulates the UI state of a single permission toggle for the selected user.
- **Security Implications:** `CanActorGrant` is the client-side reflection of `UserManagementRules.AssertNoPrivilegeEscalation`. When `false`, the toggle switch is disabled in the UI, stopping operators from toggling permissions they do not hold.

---

### CAL-UVM-008 & CAL-UVM-009: `Users::get` & `Permissions::get`
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:63:57:GetAccessorDeclaration`
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:64:65:GetAccessorDeclaration`
- **Exact Line Range:** Lines 63, 64
- **Signatures:**
  - `public ObservableCollection<UserDetailsDto> Users { get; } = new();`
  - `public ObservableCollection<PermissionToggle> Permissions { get; } = new();`
- **Actual Behavior:** Readonly getters exposing observable collections bound to WinUI XAML list and toggle grid controls.

---

### CAL-UVM-010 to CAL-UVM-012: `OnIsBusyChanged`, `OnIsLoadingChanged`, `OnLoadErrorChanged`
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:82:5:MethodDeclaration`
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:83:5:MethodDeclaration`
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:84:5:MethodDeclaration`
- **Exact Line Range:** Lines 82, 83, 84
- **Signatures:**
  - `partial void OnIsBusyChanged(bool value)`
  - `partial void OnIsLoadingChanged(bool value)`
  - `partial void OnLoadErrorChanged(string? value)`
- **Actual Behavior:**
  - `OnIsBusyChanged`: Invokes `RefreshActions()` and notifies `_session.SetManagementOperationPending(this, !CanEditAction)`.
  - `OnIsLoadingChanged` & `OnLoadErrorChanged`: Trigger `RefreshActions()` to re-evaluate computed availability properties.
- **Security Implications:** Blocks navigation away from the management screen while a write operation is active or pending confirmation.

---

### CAL-UVM-013: `OnSelectedUserChanged(UserDetailsDto? value)`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:86:5:MethodDeclaration`
- **Exact Line Range:** Lines 86–91
- **Signature:** `partial void OnSelectedUserChanged(UserDetailsDto? value)`
- **Actual Behavior & Inputs:**
  - Notifies change for `IsOwnerSelected`.
  - Calls `RefreshActions()`.
  - Calls `RebuildToggles()`.
- **Side Effects:** Clears and repopulates the `Permissions` observable collection.

---

### CAL-UVM-014 to CAL-UVM-016: `RebuildToggles()` & Lambdas
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:93:5:MethodDeclaration`
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:97:46:SimpleLambdaExpression` (`d => d.Category`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:97:70:SimpleLambdaExpression` (`d => d.Key`)
- **Exact Line Range:** Lines 93–105, line 97
- **Signature:** `private void RebuildToggles()`
- **Actual Behavior & Inputs:**
  - Clears `Permissions`.
  - If `SelectedUser` is null, returns early.
  - Sorts `_catalog` by `Category` then `Key`.
  - Adds a new `PermissionToggle` for each catalog item:
    - `IsGranted = SelectedUser.Permissions.Contains(def.Key)`
    - `CanActorGrant = SelectedUser.Permissions.Contains(def.Key) || _session.HasPermission(def.Key)`
- **Trust Boundaries & Security Implications:**
  - Guarantees that a user holding a permission can have it revoked by an administrator even if the administrator does not personally hold that permission (`SelectedUser.Permissions.Contains(def.Key)` is true).
  - Guarantees that new permissions cannot be granted unless the administrator holds them (`_session.HasPermission(def.Key)`).

---

### CAL-UVM-017 to CAL-UVM-019: `LoadAsync()`, `ReloadAsync()`, & Lambda
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:107:5:MethodDeclaration` (`LoadAsync`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:114:5:MethodDeclaration` (`ReloadAsync`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:136:49:SimpleLambdaExpression` (`u => u.Id == selectedId`)
- **Exact Line Range:** Lines 107–112, 114–143, 136
- **Actual Behavior & Inputs:**
  - Validates `!CanEditAction || IsUserDialogOpen`; returns if busy/pending.
  - Fetches users via `_session.Api.GetAllUsersAsync(actor)` where `actor = _session.CurrentUser!.Id`.
  - Fetches dynamic catalog via `_session.Api.GetPermissionCatalogAsync()`.
  - Re-establishes selected user using `selectedId ?? SelectedUser?.Id`.
- **Security Implications:** Transmits caller ID `actor` in query parameters. Minimizes UI-state inconsistency across network calls.

---

### CAL-UVM-020: `SelectUser(UserDetailsDto user)`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:145:5:MethodDeclaration`
- **Exact Line Range:** Lines 145–146
- **Signature:** `private void SelectUser(UserDetailsDto user)`
- **Actual Behavior:** Sets `SelectedUser = user` if `CanEditAction && !IsUserDialogOpen && !IsLoading`.

---

### CAL-UVM-021 to CAL-UVM-025: `SavePermissionsAsync()` & Diff Lambdas
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:148:5:MethodDeclaration` (`SavePermissionsAsync`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:155:39:SimpleLambdaExpression` (grant filter)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:155:109:SimpleLambdaExpression` (grant key projection)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:156:40:SimpleLambdaExpression` (revoke filter)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:156:110:SimpleLambdaExpression` (revoke key projection)
- **Exact Line Range:** Lines 148–164, 155, 156
- **Signature:** `private async Task SavePermissionsAsync()`
- **Actual Behavior & Inputs:**
  - If pending retained request exists, retries `SendPermissionsAsync(_pendingPermissions.Value!)`.
  - Computes `grant` list: permissions marked `IsGranted == true` but NOT in `SelectedUser.Permissions`.
  - Computes `revoke` list: permissions marked `IsGranted == false` but PRESENT in `SelectedUser.Permissions`.
  - If both lists are empty, sets `ActionError = "لا توجد تغييرات لحفظها."`.
  - Mints `UpdateUserPermissionsRequest(SelectedUser.Id, grant.ToArray(), revoke.ToArray(), _session.CurrentUser.Id)`.
  - Calls `SendPermissionsAsync(request)`.
- **Security Implications:** Sends explicit deltas (`grant` and `revoke`) rather than replacing the complete permissions set, reducing race-condition overwrite windows when two administrators edit different permissions concurrently.

---

### CAL-UVM-026 & CAL-UVM-027: `SendPermissionsAsync` & Lambda
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:166:5:MethodDeclaration`
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:172:71:SimpleLambdaExpression` (`r => _session.Api.UpdateUserPermissionsAsync(r)`)
- **Exact Line Range:** Lines 166–182, line 172
- **Signature:** `private async Task SendPermissionsAsync(UpdateUserPermissionsRequest request)`
- **Actual Behavior & Inputs:**
  - Invokes `_pendingPermissions.SendAsync(request, r => _session.Api.UpdateUserPermissionsAsync(r))`.
  - If unsuccessful, surfaces `_pendingPermissions.FailureMessage(result.Error)` to `ActionError`.
  - On success, sets `ActionNote = "تم تحديث الصلاحيات."` and reloads via `ReloadAsync(request.UserId)`.

---

### CAL-UVM-028 & CAL-UVM-029: `ToggleActiveAsync()` & Lambda
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:184:5:MethodDeclaration`
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:193:66:SimpleLambdaExpression` (`r => _session.Api.SetUserActiveAsync(r)`)
- **Exact Line Range:** Lines 184–203, line 193
- **Signature:** `private async Task ToggleActiveAsync()`
- **Actual Behavior & Inputs:**
  - Resolves request from `_pendingActive.Value` or constructs `new SetUserActiveRequest(SelectedUser.Id, !SelectedUser.Active, null, _session.CurrentUser.Id)`.
  - Executes via `_pendingActive.SendAsync(request, r => _session.Api.SetUserActiveAsync(r))`.
  - On success, sets `ActionNote = "تم تحديث حالة المستخدم."` and reloads via `ReloadAsync(request.UserId)`.
- **Security Implications:** Toggles active status using the target user's current inverted state (`!SelectedUser.Active`). If retained on power loss, the retry executes the *exact requested state* rather than inverting again, preventing double-toggle hazards.

---

## 4. Verification & Recommendations

1. **Context Alignment:** Ensure `ActorUserId` sent in requests is corroborated against server-side session identity (`context.ActorId()`) to eliminate SEC-001 exposure.
2. **Static Verification:** Verified complete against `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs`. No application source modified.
