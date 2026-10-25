# File Review: src/Falah.Client.Cashier/ViewModels/MainViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.Cashier/ViewModels/MainViewModel.cs.

## Callables

### Callable: src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:21:37:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:21:37:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.MainViewModel.Header.get
- **Lines:** [21-21]
- **Behavior:** Accessor 'get' for property in Falah.Client.Cashier.ViewModels.MainViewModel.Header.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:23:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:23:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.MainViewModel.MainViewModel()
- **Lines:** [23-33]
- **Behavior:** Constructor initializing cashier WPF client root state; instantiates AppSession and HeaderViewModel, displays the login view via ShowLogin(), configures and starts a 5-second DispatcherTimer (_healthTimer) for periodic server pinging, and triggers an immediate health check.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:30:30:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:30:30:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [30-30]
- **Behavior:** Timer tick event callback lambda querying _session.Api.PingHealthAsync() and updating IsOnline accordingly.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:35:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:35:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.MainViewModel.CheckHealthNowAsync()
- **Lines:** [35-35]
- **Behavior:** Asynchronously executes an immediate health ping against _session.Api.PingHealthAsync() and assigns the result to IsOnline.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:40:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:40:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.MainViewModel.Lock()
- **Lines:** [40-41]
- **Behavior:** RelayCommand returning the client to the PIN login screen via ShowLogin() without altering or closing active shift state on the server.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:43:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:43:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.MainViewModel.ShowLogin()
- **Lines:** [43-49]
- **Behavior:** Displays the PIN login screen; sets IsLoggedIn = false, constructs LoginViewModel, hooks its LoggedIn event to ShowCashier, and assigns it to CurrentView.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:51:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:51:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.MainViewModel.ShowCashier()
- **Lines:** [51-58]
- **Behavior:** Navigates to the cashier ordering view after authentication; calls Header.Refresh(), sets IsLoggedIn = true, constructs CashierViewModel, binds it to CurrentView, and loads the product menu via LoadMenuAsync() if a shift is already open.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---