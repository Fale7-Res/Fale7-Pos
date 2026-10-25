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
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.MainViewModel(Falah.Client.CallCenterWinUI.Services.AppSession?) across lines 25-35.
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
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.CheckHealthNowAsync() across lines 37-41.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:43:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:43:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.TryClose()
- **Lines:** [43-47]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.TryClose() across lines 43-47.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:49:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:49:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Stop()
- **Lines:** [49-54]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Stop() across lines 49-54.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:56:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:56:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Lock()
- **Lines:** [56-60]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Lock() across lines 56-60.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:62:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:62:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.ShowLogin(string?)
- **Lines:** [62-68]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.ShowLogin(string?) across lines 62-68.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:74:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:74:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.OnLoggedIn()
- **Lines:** [74-87]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.OnLoggedIn() across lines 74-87.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
