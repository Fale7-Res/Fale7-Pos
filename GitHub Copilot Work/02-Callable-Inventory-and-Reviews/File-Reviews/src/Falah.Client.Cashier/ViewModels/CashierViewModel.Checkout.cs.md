# File Review: src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs.

## Callables

### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:23:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:23:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.OnLastCheckoutResultChanged(Falah.Application.Orders.CheckoutOrderResult?)
- **Lines:** [23-27]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.OnLastCheckoutResultChanged(Falah.Application.Orders.CheckoutOrderResult?) across lines 23-27.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:31:9:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:31:9:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.ChangeDuePiastres.get
- **Lines:** [31-36]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.ChangeDuePiastres.get across lines 31-36.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:39:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:39:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.OpenCheckout()
- **Lines:** [39-48]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.OpenCheckout() across lines 39-48.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:50:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:50:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.CloseCheckout()
- **Lines:** [50-51]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.CloseCheckout() across lines 50-51.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:53:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:53:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SetPaymentMethod(string)
- **Lines:** [53-59]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.SetPaymentMethod(string) across lines 53-59.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:61:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:61:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.PressAmountKey(string)
- **Lines:** [61-71]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.PressAmountKey(string) across lines 61-71.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:73:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:73:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.QuickCash(string)
- **Lines:** [73-78]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.QuickCash(string) across lines 73-78.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:80:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:80:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.CanSubmitPayment()
- **Lines:** [80-80]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.CanSubmitPayment() across lines 80-80.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:82:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:82:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SubmitPaymentAsync()
- **Lines:** [82-140]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.SubmitPaymentAsync() across lines 82-140.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:102:36:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:102:36:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [102-102]
- **Behavior:** Executes logic for lambda expression across lines 102-102.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:142:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:142:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.StartNewOrder()
- **Lines:** [142-147]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.StartNewOrder() across lines 142-147.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
