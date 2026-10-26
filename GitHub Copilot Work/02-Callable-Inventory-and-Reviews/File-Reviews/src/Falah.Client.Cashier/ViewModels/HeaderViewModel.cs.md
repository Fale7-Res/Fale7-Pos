# File Review: src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs.

## Callables

### Callable: src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs:19:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs:19:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.HeaderViewModel.HeaderViewModel(Falah.Client.Cashier.Services.AppSession)
- **Lines:** [19-26]
- **Behavior:** Captures the shared `AppSession`, performs initial clock formatting via `UpdateClock`, and starts a `DispatcherTimer` set to refresh every 30 seconds.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs:24:29:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs:24:29:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [24-24]
- **Behavior:** Inline expression lambda '_clockTimer.Tick += (_, _) => UpdateClock();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs:34:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs:34:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.HeaderViewModel.UpdateClock()
- **Lines:** [34-40]
- **Behavior:** Reads current local time and formats Arabic time and date strings using `ar-EG` culture, updating `TimeText` and `DateText` properties.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs:55:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs:55:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.HeaderViewModel.Refresh()
- **Lines:** [55-61]
- **Behavior:** Notifies property change updates for shift status and operator identity properties (`IsShiftOpen`, `ShiftLabel`, `UserName`, and `RoleLabel`).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
