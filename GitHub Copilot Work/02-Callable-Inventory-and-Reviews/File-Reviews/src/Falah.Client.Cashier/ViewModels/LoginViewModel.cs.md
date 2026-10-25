# File Review: src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs.

## Callables

### Callable: src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:15:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:15:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.LoginViewModel.LoginViewModel(Falah.Client.Cashier.Services.AppSession)
- **Lines:** [15-15]
- **Behavior:** Initializes LoginViewModel, injecting the client application session (AppSession) and storing it in the private readonly _session field.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:30:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:30:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.LoginViewModel.OnPinChanged(string)
- **Lines:** [30-30]
- **Behavior:** Partial notification callback invoked when the Pin property changes; triggers a PropertyChanged notification for PinDisplay to update the bullet mask string ('●').
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:32:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:32:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.LoginViewModel.PressDigit(string)
- **Lines:** [32-38]
- **Behavior:** RelayCommand handler appending the selected digit to the current Pin if current length is under 6 digits, and clears any active ErrorMessage.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:40:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:40:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.LoginViewModel.Backspace()
- **Lines:** [40-44]
- **Behavior:** RelayCommand handler removing the last entered character from the Pin buffer using range slicing (Pin[..^1]) when Pin is not empty.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:46:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:46:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.LoginViewModel.Clear()
- **Lines:** [46-47]
- **Behavior:** RelayCommand handler resetting the Pin input string to string.Empty.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:49:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:49:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.LoginViewModel.CanSubmit()
- **Lines:** [49-49]
- **Behavior:** CanExecute predicate evaluated for SubmitCommand; returns true only when Pin has at least 4 characters and the ViewModel is not currently busy (!IsBusy).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:51:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs:51:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.LoginViewModel.SubmitAsync()
- **Lines:** [51-78]
- **Behavior:** Asynchronous RelayCommand authenticating the user via _session.Api.LoginByPinAsync(Pin). Sets IsBusy and clears error text; on failed login or null result, displays an invalid PIN error and resets Pin. On success, assigns CurrentUser on the session, clears Pin, and triggers the LoggedIn event. Handles transport exceptions by displaying an error message, and ensures IsBusy is reset to false in the finally block.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---