# File Review: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs.

## Callables

### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:23:37:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:23:37:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Header.get
- **Lines:** [23-23]
- **Behavior:** Accessor 'get' for property in Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Header.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:25:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:25:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.MainViewModel(Falah.Client.CallCenterWinUI.Services.AppSession?)
- **Lines:** [25-35]
- **Behavior:** Initializes the Call Center root view model, setting up the AppSession, instantiating HeaderViewModel, navigating to the login view via ShowLogin(), and configuring a 5-second DispatcherTimer (_healthTimer) to periodically check server health via CheckHealthNowAsync().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:32:30:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:32:30:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [32-32]
- **Behavior:** Inline expression lambda '_healthTimer.Tick += async (_, _) => await CheckHealthNowAsync();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:37:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:37:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.CheckHealthNowAsync()
- **Lines:** [37-41]
- **Behavior:** Asynchronously probes server health via _session.Api.PingHealthAsync() and, unless the view model is flagged as stopped (_stopped), updates the IsOnline property.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:43:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:43:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.TryClose()
- **Lines:** [43-47]
- **Behavior:** Checks whether an inflight or unresolved operation exists on the session (_session.HasUnresolvedOperation); updates IsCloseBlocked accordingly, and returns true only if window close is safe.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:49:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:49:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Stop()
- **Lines:** [49-54]
- **Behavior:** Shuts down background polling by setting _stopped = true, stopping the health timer (_healthTimer.Stop()), and stopping the header view model (Header.Stop()).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:56:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:56:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Lock()
- **Lines:** [56-60]
- **Behavior:** RelayCommand locking the call center station by navigating back to ShowLogin(), guarded so that locking is disallowed if an unresolved operation is currently pending on the session.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:62:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:62:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.ShowLogin(string?)
- **Lines:** [62-68]
- **Behavior:** Transitions the station to locked/login state; sets IsLoggedIn = false, constructs a new LoginViewModel passing any error message, hooks its LoggedIn event to OnLoggedIn, and assigns it to CurrentView.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:74:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:74:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.OnLoggedIn()
- **Lines:** [74-87]
- **Behavior:** Event handler triggered upon successful login. Enforces that the authenticated user possesses the 'pos' permission; if not, logs out with an Arabic error message. If authorized, refreshes the header, marks IsLoggedIn = true, retrieves the order intake desk ViewModel via _session.GetOrderDesk(Header), sets CurrentView, and calls InitializeAsync().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---