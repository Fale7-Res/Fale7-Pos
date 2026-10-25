# src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs

- **Path:** `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs`
- **Component:** `Falah.Client.CashierWinUI.ViewModels.UsersViewModel`
- **Assembly:** `Falah.Client.CashierWinUI`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`UsersViewModel.Actions.cs` is the partial class of `UsersViewModel` responsible for administrative write actions: creating new user accounts, validating credentials, performing self-service PIN updates, executing administrative PIN resets, and managing offline request retention and retries.

### Security & Operational Responsibilities:
1. **Account Provisioning:** Collects username, role selection, initial PIN, and initial permission set.
2. **Client-Side PIN Validation:** Validates PIN confirmation match and invokes domain validation (`PinHasher.AssertValidPin`), ensuring length (4-6 digits), ASCII digit strictness, and trivial repeat rejections occur prior to network transmission.
3. **Strict Role Normalization:** Uses `RoleOptions` to force exact string values (`"cashier"`, `"manager"`, `"owner"`), preventing spelling or casing deviations that would bypass domain immutability checks.
4. **Durable Request Retention:** Uses `RetainedRequest<CreateUserRequest>` (`_pendingCreate`) and `RetainedRequest<ChangePinRequest>` (`_pendingPin`) to preserve operations when network drops or power outages occur mid-flight.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-UVA-001` | `NewUserPermissions::get` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:29:72:GetAccessorDeclaration` | [29](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L29) | Property Accessor |
| `CAL-UVA-002` | `RoleOptions::get` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:41:52:GetAccessorDeclaration` | [41](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L41) | Property Accessor |
| `CAL-UVA-003` | `OnSelectedNewUserRoleChanged` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:50:5:MethodDeclaration` | [50-54](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L50-L54) | State Notification |
| `CAL-UVA-004` | `OnIsCreateUserOpenChanged` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:69:5:MethodDeclaration` | [69](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L69) | State Notification |
| `CAL-UVA-005` | `OnPinTargetChanged` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:70:5:MethodDeclaration` | [70](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L70) | State Notification |
| `CAL-UVA-006` | `RefreshActions` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:72:5:MethodDeclaration` | [72-77](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L72-L77) | UI Notification |
| `CAL-UVA-007` | `OpenCreateUser` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:79:5:MethodDeclaration` | [79-91](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L79-L91) | Relay Command |
| `CAL-UVA-008` | `OpenCreateUser::<lambda_category>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:87:53:SimpleLambdaExpression` | [87](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L87) | Lambda Expression |
| `CAL-UVA-009` | `OpenCreateUser::<lambda_key>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:87:77:SimpleLambdaExpression` | [87](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L87) | Lambda Expression |
| `CAL-UVA-010` | `CloseUserDialog` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:93:5:MethodDeclaration` | [93-100](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L93-L100) | Relay Command |
| `CAL-UVA-011` | `ValidatePin` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:102:5:MethodDeclaration` | [102-107](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L102-L107) | Validation Core |
| `CAL-UVA-012` | `CreateUserAsync` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:109:5:MethodDeclaration` | [109-137](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L109-L137) | Relay Command |
| `CAL-UVA-013` | `CreateUserAsync::<lambda_granted>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:122:56:SimpleLambdaExpression` | [122](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L122) | Lambda Expression |
| `CAL-UVA-014` | `CreateUserAsync::<lambda_proj_key>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:122:81:SimpleLambdaExpression` | [122](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L122) | Lambda Expression |
| `CAL-UVA-015` | `CreateUserAsync::<lambda_escalate_check>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:123:33:SimpleLambdaExpression` | [123](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L123) | Lambda Expression |
| `CAL-UVA-016` | `CreateUserAsync::<lambda_api>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:130:66:SimpleLambdaExpression` | [130](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L130) | Lambda Expression |
| `CAL-UVA-017` | `OpenPinChange` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:139:5:MethodDeclaration` | [139-145](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L139-L145) | Relay Command |
| `CAL-UVA-018` | `ChangePinAsync` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:147:5:MethodDeclaration` | [147-180](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L147-L180) | Relay Command |
| `CAL-UVA-019` | `ChangePinAsync::<lambda_replay>` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:163:63:SimpleLambdaExpression` | [163-174](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L163-L174) | Lambda Expression |
| `CAL-UVA-020` | `RetryUserActionAsync` | `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:182:5:MethodDeclaration` | [182-190](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L182-L190) | Relay Command |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-UVA-001 & CAL-UVA-002: Property Accessors
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:29:72:GetAccessorDeclaration` (`NewUserPermissions::get`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:41:52:GetAccessorDeclaration` (`RoleOptions::get`)
- **Exact Line Range:** Lines 29, 41
- **Actual Behavior:**
  - `NewUserPermissions::get` exposes observable collection of permission toggles for user creation.
  - `RoleOptions::get` returns static array of valid role choices (`cashier`, `manager`, `owner`).
- **Security Implications:** Normalizes user roles strictly to the 3 recognized strings, stopping typo-induced role anomalies.

---

### CAL-UVA-003 to CAL-UVA-006: State Notifications & `RefreshActions`
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:50:5:MethodDeclaration` (`OnSelectedNewUserRoleChanged`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:69:5:MethodDeclaration` (`OnIsCreateUserOpenChanged`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:70:5:MethodDeclaration` (`OnPinTargetChanged`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:72:5:MethodDeclaration` (`RefreshActions`)
- **Exact Line Range:** Lines 50–54, 69, 70, 72–77
- **Actual Behavior:** Triggers property change notifications across 11 derived UI state flags (`CanEditAction`, `HasPendingAction`, `CanCreateUser`, `CanChangeSelectedPin`, `IsSelfPinChange`, etc.).

---

### CAL-UVA-007 to CAL-UVA-009: `OpenCreateUser` & Sorting Lambdas
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:79:5:MethodDeclaration` (`OpenCreateUser`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:87:53:SimpleLambdaExpression` (`d => d.Category`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:87:77:SimpleLambdaExpression` (`d => d.Key`)
- **Exact Line Range:** Lines 79–91, line 87
- **Signature:** `private void OpenCreateUser()`
- **Actual Behavior & Inputs:**
  - Checks `if (!CanCreateUser) return`.
  - Clears input buffers: `NewUserName = NewUserRole = NewUserPin = ConfirmUserPin = ""`.
  - Clears `NewUserPermissions` and repopulates from `_catalog`, evaluating `CanActorGrant = _session.HasPermission(definition.Key)`.
  - Sets `IsCreateUserOpen = true`.
- **Security Implications:** Restricts the administrator from enabling permissions they do not possess during new user creation.

---

### CAL-UVA-010: `CloseUserDialog`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:93:5:MethodDeclaration`
- **Exact Line Range:** Lines 93–100
- **Signature:** `private void CloseUserDialog()`
- **Actual Behavior:** If `CanEditAction` is true, closes creation/PIN dialogs and resets all in-memory PIN strings (`NewUserPin = ConfirmUserPin = CurrentPin = NewPin = ConfirmPin = ""`).
- **Security Implications:** Memory hygiene: wipes transient credential strings from view model state upon dialog cancellation.

---

### CAL-UVA-011: `ValidatePin(string pin, string confirmation)`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:102:5:MethodDeclaration`
- **Exact Line Range:** Lines 102–107
- **Signature:** `private bool ValidatePin(string pin, string confirmation)`
- **Actual Behavior & Inputs:**
  - Evaluates `if (pin != confirmation)` -> sets `ActionError = "الرمزان غير متطابقين."` and returns `false`.
  - Invokes `PinHasher.AssertValidPin(pin)` inside a try-catch block.
  - Catches `DomainException ex` and sets `ActionError = ex.Message`, returning `false`.
- **Security Implications:** Delegates validation directly to Domain authority `PinHasher.AssertValidPin`, ensuring client and server enforce identical credential rules.

---

### CAL-UVM-012 to CAL-UVM-016: `CreateUserAsync()` & Lambdas
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:109:5:MethodDeclaration` (`CreateUserAsync`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:122:56:SimpleLambdaExpression` (`p => p.IsGranted`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:122:81:SimpleLambdaExpression` (`p => p.Key`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:123:33:SimpleLambdaExpression` (`p => !_session.HasPermission(p)`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:130:66:SimpleLambdaExpression` (`r => _session.Api.CreateUserAsync(r)`)
- **Exact Line Range:** Lines 109–137, 122, 123, 130
- **Actual Behavior & Inputs:**
  - If retained request exists, proceeds to send.
  - Otherwise validates: non-empty name/role, `ValidatePin(NewUserPin, ConfirmUserPin)`, and checks `if (permissions.Any(p => !_session.HasPermission(p)))`.
  - Mints `CreateUserRequest($"create-user-{Guid.NewGuid():N}", NewUserName.Trim(), NewUserRole.Trim(), NewUserPin, permissions, _session.CurrentUser.Id)`.
  - Calls `_pendingCreate.SendAsync(request, r => _session.Api.CreateUserAsync(r))`.
- **CRITICAL SECURITY RISK (SEC-002):**
  - `CreateUserRequest` contains the raw plaintext PIN (`NewUserPin`).
  - When `_pendingCreate.SendAsync` executes, `FileRetainedRequestStore.Save` writes the serialized `CreateUserRequest` directly to `%LOCALAPPDATA%\Falah\pending-operations\{station}\users.create.json` in **cleartext**.
  - If a network error or power cut occurs, the new employee's plaintext PIN remains stored unencrypted on disk.
- **Test Coverage:** Tested in `UserManagementVerticalSliceTests.Creating_a_user_with_valid_data_succeeds_and_the_new_pin_logs_in`.

---

### CAL-UVA-017: `OpenPinChange()`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:139:5:MethodDeclaration`
- **Exact Line Range:** Lines 139–145
- **Signature:** `private void OpenPinChange()`
- **Actual Behavior:** Validates `CanChangeSelectedPin`, clears PIN fields, and sets `PinTarget = SelectedUser`.

---

### CAL-UVA-018 & CAL-UVA-019: `ChangePinAsync()` & Replay Lambda
- **Canonical Callable IDs:**
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:147:5:MethodDeclaration` (`ChangePinAsync`)
  - `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:163:63:SimpleLambdaExpression` (`async original => { ... }`)
- **Exact Line Range:** Lines 147–180, 163–174
- **Signature:** `private async Task ChangePinAsync()`
- **Actual Behavior & Inputs:**
  - If self-change, validates `CurrentPin` is present.
  - If admin reset, ensures target is not owner and actor has `users_manage`.
  - Validates `ValidatePin(NewPin, ConfirmPin)`.
  - Mints `ChangePinRequest(PinTarget.Id, IsSelfPinChange ? CurrentPin : null, NewPin, _session.CurrentUser.Id)`.
  - Calls `_pendingPin.SendAsync(request, ...)`.
- **CRITICAL SECURITY FINDINGS:**
  1. **SEC-002 (Plaintext PINs in Retained Store):** `ChangePinRequest` containing `CurrentPin` and `NewPin` is serialized to `%LOCALAPPDATA%\Falah\pending-operations\{station}\users.change-pin.json` in plaintext.
  2. **SEC-008 (Session Token Replacement & Identity Confusion):**
     In lines 168–172:
     ```csharp
     if (wasPending && original.UserId == original.ActorUserId)
     {
         var proof = await _session.Api.LoginByPinAsync(original.NewPin);
         if (proof.Success && proof.Value?.Id == original.UserId) return new ApiClient.ApiResult<bool>(true, true, null);
     }
     ```
     Calling `_session.Api.LoginByPinAsync(original.NewPin)` mutates `ApiClient._sessionToken` and `_http.DefaultRequestHeaders.Authorization`. While the code comment states *"Never change the logged-in session"*, the underlying `ApiClient` actually overwrites its bearer token! If the new PIN happens to authenticate as another user, `_session.CurrentUser` becomes desynchronized from the client's Bearer token, leading to session hijacking.

---

### CAL-UVA-020: `RetryUserActionAsync()`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:182:5:MethodDeclaration`
- **Exact Line Range:** Lines 182–190
- **Signature:** `private async Task RetryUserActionAsync()`
- **Actual Behavior:** Inspects retained request stores in priority order: `_pendingCreate`, `_pendingPin`, `_pendingPermissions`, `_pendingActive`, and invokes the corresponding async handler.
- **Security Implications:** Guarantees that stalled operations are retried with the exact original request data and caller credentials.

---

## 4. Deep Vulnerability Forensics

### Detailed Analysis of SEC-002: Cleartext Credentials in Retained Requests
- **Severity:** `HIGH` (CVSS v3.1: 7.1 - AV:L/AC:L/PR:L/UI:N/S:U/C:H/I:N/A:N)
- **Root Cause:**
  When executing `CreateUserAsync` or `ChangePinAsync`, the request object is passed to `_pendingCreate.SendAsync` or `_pendingPin.SendAsync`.
  `RetainedRequest.SendAsync` unconditionally invokes `RetainedRequestStorage.Current?.Save(ownerKey, request)`.
  In `FileRetainedRequestStore.Save`:
  ```csharp
  File.WriteAllText(temp, JsonSerializer.Serialize(envelope, Options));
  File.Move(temp, path, overwrite: true);
  ```
  The serialized JSON contains `InitialPin`, `CurrentPin`, and `NewPin`.
- **Impact:** An unprivileged user or local background software on the POS terminal can inspect `%LOCALAPPDATA%\\Falah\\pending-operations` and harvest manager and staff PINs.

### Detailed Analysis of SEC-008: Bearer Token Hijacking via `LoginByPinAsync` Replay Proof
- **Severity:** `HIGH` (CVSS v3.1: 7.5 - AV:L/AC:H/PR:L/UI:N/S:U/C:H/I:H/A:N)
- **Root Cause:**
  In `ChangePinAsync` retry logic:
  `var proof = await _session.Api.LoginByPinAsync(original.NewPin);`
  `ApiClient.LoginByPinAsync` overwrites `_http.DefaultRequestHeaders.Authorization = new("Bearer", result.Value.Token)`.
- **Impact:** Desynchronizes the active session identity between `_session.CurrentUser` and `HttpClient.DefaultRequestHeaders.Authorization`. If the new PIN matched another user account, the terminal becomes hijacked under that user's session token.

---

## 5. Verification & Recommendations

1. **Protect Retained Credential Storage (SEC-002):** Encrypt retained operations with DPAPI (`ProtectedData.Protect`) or omit PIN strings from persistent storage.
2. **Prevent Token Overwrite in Replay Proof (SEC-008):** Implement a dedicated `VerifyPinAsync` API endpoint that does NOT mint or store a new session bearer token.
3. **Static Verification:** Verified complete against `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs`. No application source modified.
