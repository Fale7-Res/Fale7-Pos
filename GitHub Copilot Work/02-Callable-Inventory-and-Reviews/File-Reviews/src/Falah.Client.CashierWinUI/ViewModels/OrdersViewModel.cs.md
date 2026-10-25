# File Review: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:18:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:18:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.OrdersViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [18-18]
- **Behavior:** Initializes OrdersViewModel with injected AppSession dependency for authenticated API communication.
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
- **Behavior:** Partial property-changed callback for SelectedOrder. Fires OnPropertyChanged for IsDetailOpen to toggle order detail panel display.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:35:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:35:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.OnSearchQueryChanged(string)
- **Lines:** [35-35]
- **Behavior:** Partial property-changed callback for SearchQuery. Invokes ApplyFilter() on input changes to dynamically filter orders by order number, customer name, or phone.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:37:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:37:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.ApplyFilter()
- **Lines:** [37-51]
- **Behavior:** Filters cached orders in _all by SearchQuery matching OrderNumber, CustomerName, or CustomerPhone (case-insensitive). Clears and repopulates Orders collection and notifies HasNoOrders.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:44:35:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:44:35:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [44-47]
- **Behavior:** LINQ filter predicate matching OrderNumber, CustomerName, or CustomerPhone against trimmed query string q.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:53:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:53:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.LoadAsync()
- **Lines:** [53-76]
- **Behavior:** Asynchronously loads orders from server via Api.GetOrdersAsync(_session.CurrentUser!.Id). On failure sets LoadError; on success populates _all cache, applies filter, and updates HasNoOrders.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:78:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:78:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.OpenOrder(Falah.Application.Orders.OrderDetailsDto)
- **Lines:** [78-79]
- **Behavior:** Sets SelectedOrder to the chosen order, opening the order detail flyout.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:81:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:81:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.OrdersViewModel.CloseOrder()
- **Lines:** [81-82]
- **Behavior:** Resets SelectedOrder to null, closing the order detail flyout.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
