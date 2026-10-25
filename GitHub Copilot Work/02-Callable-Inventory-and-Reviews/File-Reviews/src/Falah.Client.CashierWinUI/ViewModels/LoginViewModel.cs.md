# File Review: src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:15:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:15:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.LoginViewModel.LoginViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [15-15]
- **Behavior:** Initializes LoginViewModel with injected AppSession dependency for managing authenticated cashier user state.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:31:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:31:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.LoginViewModel.OnPinChanged(string)
- **Lines:** [31-31]
- **Behavior:** Partial property-changed callback for Pin; notifies UI of changes to PinDisplay property to update bullet masking.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:32:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:32:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.LoginViewModel.OnIsBusyChanged(bool)
- **Lines:** [32-32]
- **Behavior:** Partial property-changed callback for IsBusy; notifies UI of changes to SubmitLabel property to toggle button text.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:34:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:34:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.LoginViewModel.PressDigit(string)
- **Lines:** [34-40]
- **Behavior:** Appends entered digit string to Pin if length is under 6 characters and clears previous ErrorMessage.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:42:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:42:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.LoginViewModel.Backspace()
- **Lines:** [42-46]
- **Behavior:** Removes the last digit character from Pin if length is greater than 0.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:48:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:48:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.LoginViewModel.Clear()
- **Lines:** [48-49]
- **Behavior:** Clears the entered Pin string.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:51:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:51:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.LoginViewModel.CanSubmit()
- **Lines:** [51-51]
- **Behavior:** Predicate for SubmitCommand; returns true if Pin.Length >= 4 and IsBusy is false.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:53:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:53:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.LoginViewModel.SubmitAsync()
- **Lines:** [53-80]
- **Behavior:** Submits entered PIN to server via Api.LoginByPinAsync(Pin). On failure sets ErrorMessage and resets Pin. On success updates _session.CurrentUser, clears Pin, and invokes LoggedIn event. Handles network exceptions gracefully.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
