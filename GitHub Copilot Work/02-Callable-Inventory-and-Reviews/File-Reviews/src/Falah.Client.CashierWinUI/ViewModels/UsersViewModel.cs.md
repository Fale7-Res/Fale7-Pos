# File Review: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:23:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:23:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.UsersViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [23-23]
- **Behavior:** Injects and stores the singleton `AppSession` instance in private field `_session` for user management API communication and permission validation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:28:38:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:28:38:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.PermissionToggle.Key.get
- **Lines:** [28-28]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.UsersViewModel.PermissionToggle.Key.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:28:43:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:28:43:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.PermissionToggle.Key.init
- **Lines:** [28-28]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.UsersViewModel.PermissionToggle.Key.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:29:43:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:29:43:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.PermissionToggle.Category.get
- **Lines:** [29-29]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.UsersViewModel.PermissionToggle.Category.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:29:48:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:29:48:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.PermissionToggle.Category.init
- **Lines:** [29-29]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.UsersViewModel.PermissionToggle.Category.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:60:46:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:60:46:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.PermissionToggle.CanActorGrant.get
- **Lines:** [60-60]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.UsersViewModel.PermissionToggle.CanActorGrant.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:60:51:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:60:51:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.PermissionToggle.CanActorGrant.init
- **Lines:** [60-60]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.UsersViewModel.PermissionToggle.CanActorGrant.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:63:57:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:63:57:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.Users.get
- **Lines:** [63-63]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.UsersViewModel.Users.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:64:65:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:64:65:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.Permissions.get
- **Lines:** [64-64]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.UsersViewModel.Permissions.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:82:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:82:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.OnIsBusyChanged(bool)
- **Lines:** [82-82]
- **Behavior:** Partial change notification handler for `IsBusy`: invokes `RefreshActions()` to re-evaluate command can-execute states and registers or unregisters pending management operation state with `_session.SetManagementOperationPending(this, !CanEditAction)`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:83:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:83:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.OnIsLoadingChanged(bool)
- **Lines:** [83-83]
- **Behavior:** Partial change notification handler for `IsLoading`: invokes `RefreshActions()` to update command availability and dependent UI property bindings.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:84:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:84:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.OnLoadErrorChanged(string?)
- **Lines:** [84-84]
- **Behavior:** Partial change notification handler for `LoadError`: triggers `RefreshActions()` so UI commands and controls refresh their enabled state in response to load errors.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:86:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:86:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.OnSelectedUserChanged(Falah.Application.Users.UserDetailsDto?)
- **Lines:** [86-91]
- **Behavior:** Partial change notification handler for `SelectedUser`: notifies property change for `IsOwnerSelected`, invokes `RefreshActions()`, and calls `RebuildToggles()` to populate permission toggles reflecting the selected user.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:93:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:93:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.RebuildToggles()
- **Lines:** [93-105]
- **Behavior:** Rebuilds the `Permissions` toggle collection from the cached `_catalog` ordered by Category then Key; sets `IsGranted` based on `SelectedUser.Permissions` and sets `CanActorGrant` to true if the selected user already holds the permission or the current logged-in user holds the permission in `_session` (preventing privilege escalation).
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:97:46:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:97:46:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [97-97]
- **Behavior:** Inline expression lambda 'foreach (var def in _catalog.OrderBy(d => d.Category).ThenBy(d => d.Key))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:97:70:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:97:70:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [97-97]
- **Behavior:** Inline expression lambda 'foreach (var def in _catalog.OrderBy(d => d.Category).ThenBy(d => d.Key))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:107:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:107:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.LoadAsync()
- **Lines:** [107-112]
- **Behavior:** RelayCommand for initial or manual refresh; validates `CanEditAction && !IsUserDialogOpen` before delegating to `ReloadAsync()`.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:114:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:114:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.ReloadAsync(string?)
- **Lines:** [114-143]
- **Behavior:** Core asynchronous data loader guarded by `IsLoading`; calls `_session.Api.GetAllUsersAsync(actor)r and `_session.Api.GetPermissionCatalogAsync()`; stores catalog definitions in `_catalog`, populates `Users` collection, restores previously selected user by ID or defaults to first user, and notifies `HasNoUsers` in `finally`.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:136:49:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:136:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [136-136]
- **Behavior:** Predicate lambda `u => u.Id == selectedId` locating the user matching the previous selection ID in `Users` collection.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:145:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:145:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.SelectUser(Falah.Application.Users.UserDetailsDto)
- **Lines:** [145-146]
- **Behavior:** RelayCommand selecting a user from the master list; verifies `CanEditAction && !IsUserDialogOpen && !IsLoading` before assigning `SelectedUser = user`.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:148:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:148:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.SavePermissionsAsync()
- **Lines:** [148-164]
- **Behavior:** RelayCommand persisting modified permissions; computes delta `grant` and `revoke` lists between `Permissions` collection and `SelectedUser.Permissions`, sets error note if no delta exists, and dispatches `UpdateUserPermissionsRequest` via `SendPermissionsAsync`.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:155:39:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:155:39:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [155-155]
- **Behavior:** Filter predicate lambda `p => p.IsGranted && !SelectedUser.Permissions.Contains(p.Key)` identifying newly granted permissions.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:155:109:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:155:109:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [155-155]
- **Behavior:** Projection lambda p => p.Key extracting the permission key string from a granted `PermissionToggle`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:156:40:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:156:40:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [156-156]
- **Behavior:** Filter predicate lambda `p => !p.IsGranted && SelectedUser.Permissions.Contains(p.Key)` identifying revoked permissions.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:156:110:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:156:110:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [156-156]
- **Behavior:** Projection lambda p => p.Key extracting the permission key string from a revoked `PermissionToggle`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:166:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:166:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.SendPermissionsAsync(Falah.Application.Users.UpdateUserPermissionsRequest)
- **Lines:** [166-182]
- **Behavior:** Private asynchronous helper; manages `IsBusy`, dispatches request through `_pendingPermissions.SendAsync(request, r => _session.Api.UpdateUserPermissionsAsync(r))`, surfaces failure message on error, sets `ActionNote = "تم تحديث الصلاحيات."i on success, and reloads users preserving selection.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:172:71:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:172:71:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [172-172]
- **Behavior:** Invocation lambda r => _session.Api.UpdateUserPermissionsAsync(r) delegating request to API client.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:184:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:184:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.ToggleActiveAsync()
- **Lines:** [184-203]
- **Behavior:** RelayCommand toggling active status of `SelectedUser`; constructs or retries `SetUserActiveRequest`, sends via `_pendingActive.SendAsync(request, r => _session.Api.SetUserActiveAsync(r))`, sets `ActionNote = "تم تحديث حالة المستخدم.", and reloads users preserving selection.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:193:66:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:193:66:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [193-193]
- **Behavior:** Invocation lambda r => _session.Api.SetUserActiveAsync(r) forwarding active status toggle request to API client.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---