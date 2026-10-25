# File Review: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:29:72:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:29:72:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.NewUserPermissions.get
- **Lines:** [29-29]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.UsersViewModel.NewUserPermissions.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:41:52:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:41:52:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.RoleOptions.get
- **Lines:** [41-41]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.UsersViewModel.RoleOptions.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:50:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:50:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.OnSelectedNewUserRoleChanged(Falah.Client.CashierWinUI.ViewModels.UsersViewModel.RoleOption?)
- **Lines:** [50-54]
- **Behavior:** Updates NewUserRole with the selected role key and notifies property changes for RoleHint.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:69:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:69:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.OnIsCreateUserOpenChanged(bool)
- **Lines:** [69-69]
- **Behavior:** Triggers action capability refresh when the user creation dialog opens or closes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:70:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:70:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.OnPinTargetChanged(Falah.Application.Users.UserDetailsDto?)
- **Lines:** [70-70]
- **Behavior:** Triggers action capability refresh when the target user for PIN change changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:72:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:72:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.RefreshActions()
- **Lines:** [72-77]
- **Behavior:** Notifies dependent command states, dialog visibilities, and label properties across the user management actions.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:79:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:79:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.OpenCreateUser()
- **Lines:** [79-91]
- **Behavior:** Resets new user input fields, builds the list of permissions filterable by the current operator, and opens the creation modal.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:87:53:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:87:53:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [87-87]
- **Behavior:** Orders permission catalog definitions by permission category.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:87:77:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:87:77:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [87-87]
- **Behavior:** Orders permission catalog definitions by permission key within category.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:93:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:93:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.CloseUserDialog()
- **Lines:** [93-100]
- **Behavior:** Dismisses user creation and PIN change dialogs and clears all entered PIN fields.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:102:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:102:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.ValidatePin(string, string)
- **Lines:** [102-107]
- **Behavior:** Validates PIN matching and domain strength rules via PinHasher.AssertValidPin.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:109:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:109:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.CreateUserAsync()
- **Lines:** [109-137]
- **Behavior:** Validates input, ensures operator does not grant permissions they do not possess, and dispatches CreateUserRequest via RetainedRequest helper.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:122:56:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:122:56:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [122-122]
- **Behavior:** Filters permission toggles to select only granted permissions.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:122:81:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:122:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [122-122]
- **Behavior:** Extracts permission key from granted permission toggle item.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:123:33:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:123:33:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [123-123]
- **Behavior:** Checks whether any requested permission is not held by the current session operator.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:130:66:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:130:66:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [130-130]
- **Behavior:** Invokes ApiClient.CreateUserAsync endpoint for the retained request execution wrapper.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:139:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:139:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.OpenPinChange()
- **Lines:** [139-145]
- **Behavior:** Clears PIN input fields and opens the PIN reset modal for the selected user.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:147:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:147:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.ChangePinAsync()
- **Lines:** [147-180]
- **Behavior:** Validates PIN reset authorization, checks current PIN for self-service or permissions for admin reset, and submits ChangePinRequest.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:163:63:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:163:63:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [163-174]
- **Behavior:** Asynchronous handler for retained PIN change request verifying self-service credential proof before calling the API endpoint.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---### Callable: src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:182:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:182:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.UsersViewModel.RetryUserActionAsync()
- **Lines:** [182-190]
- **Behavior:** Retries whichever user management operation is currently in a pending retained state.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---