# File Review: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:18:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:18:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.OrdersViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [18-18]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.OrdersViewModel(Falah.Client.CashierWinUI.Services.AppSession) across lines 18-18.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:20:59:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:20:59:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.Orders.get
- **Lines:** [20-20]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.Orders.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:31:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:31:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.OnSelectedOrderChanged(Falah.Application.Orders.OrderDetailsDto?)
- **Lines:** [31-31]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.OnSelectedOrderChanged(Falah.Application.Orders.OrderDetailsDto?) across lines 31-31.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:35:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:35:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.OnSearchQueryChanged(string)
- **Lines:** [35-35]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.OnSearchQueryChanged(string) across lines 35-35.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:37:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:37:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.ApplyFilter()
- **Lines:** [37-51]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.ApplyFilter() across lines 37-51.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:44:35:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:44:35:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [44-47]
- **Behavior:** Executes logic for lambda expression across lines 44-47.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:53:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:53:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.LoadAsync()
- **Lines:** [53-76]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.LoadAsync() across lines 53-76.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:78:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:78:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.OpenOrder(Falah.Application.Orders.OrderDetailsDto)
- **Lines:** [78-79]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.OpenOrder(Falah.Application.Orders.OrderDetailsDto) across lines 78-79.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:81:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:81:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.CloseOrder()
- **Lines:** [81-82]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.CloseOrder() across lines 81-82.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
