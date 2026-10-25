# File Review: src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs.

## Callables

### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:24:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:24:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.OpenOrderLookup()
- **Lines:** [24-33]
- **Behavior:** RelayCommand opening the order lookup dialog; sets IsOrderLookupOpen = true and resets search query, found order data, and error/success messages.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:35:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:35:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.CloseOrderLookup()
- **Lines:** [35-36]
- **Behavior:** RelayCommand closing the order lookup dialog by setting IsOrderLookupOpen = false.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:38:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:38:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.LookupOrderAsync()
- **Lines:** [38-62]
- **Behavior:** Asynchronously looks up an order by order number via _session.Api.LookupOrderByNumberAsync. Validates LookupOrderNumber is non-empty; manages IsLookingUp loading flag; on success, sets LookedUpOrder, notifies RefundableRemainingPiastres, and initializes RefundAmountText with the remaining refundable balance in EGP; on error, populates LookupError.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:64:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:64:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.CanRefund()
- **Lines:** [64-64]
- **Behavior:** CanExecute condition for SubmitRefundCommand; returns true only when an order is loaded (LookedUpOrder is not null) and a refund operation is not currently inflight (!IsRefunding).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:66:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:66:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SubmitRefundAsync()
- **Lines:** [66-106]
- **Behavior:** Asynchronously executes an order refund via _session.Api.RefundAsync. Validates positive refund amount format and presence of a non-empty refund reason; generates an idempotent RefundRequestId ('rreq-{Guid}'); submits the request with CashierId from current user session; on success, displays a localized success message and re-fetches the order via LookupOrderAsync() to synchronize remaining refundable balance from the server.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:108:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:108:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.TranslateStatus(string)
- **Lines:** [108-116]
- **Behavior:** Translates raw English order status codes ('completed', 'refunded', 'cancelled', 'preparing', 'ready') into localized Arabic status labels.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---