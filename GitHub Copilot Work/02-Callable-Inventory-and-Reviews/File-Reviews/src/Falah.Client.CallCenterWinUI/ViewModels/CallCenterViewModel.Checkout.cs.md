# File Review: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs.

## Callables

### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:34:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:34:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OnLastCheckoutResultChanged(Falah.Application.Orders.CheckoutOrderResult?)
- **Lines:** [34-39]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OnLastCheckoutResultChanged(Falah.Application.Orders.CheckoutOrderResult?) across lines 34-39.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:41:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:41:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SetPaymentMethod(string)
- **Lines:** [41-47]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SetPaymentMethod(string) across lines 41-47.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:68:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:68:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.PersistPending(string, object?)
- **Lines:** [68-72]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.PersistPending(string, object?) across lines 68-72.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:77:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:77:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RecoverPendingOperations()
- **Lines:** [77-92]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RecoverPendingOperations() across lines 77-92.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:96:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:96:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.CanSubmitOrder()
- **Lines:** [96-96]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.CanSubmitOrder() across lines 96-96.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:98:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:98:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SubmitOrderAsync()
- **Lines:** [98-176]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SubmitOrderAsync() across lines 98-176.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:142:36:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:142:36:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [142-142]
- **Behavior:** Executes logic for lambda expression across lines 142-142.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:178:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:178:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SendCheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest)
- **Lines:** [178-235]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SendCheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest) across lines 178-235.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:237:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:237:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.StartNewOrder()
- **Lines:** [237-244]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.StartNewOrder() across lines 237-244.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:246:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:246:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.NullIfEmpty(string)
- **Lines:** [246-246]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.NullIfEmpty(string) across lines 246-246.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:269:9:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:269:9:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.LookedUpPaymentLine.get
- **Lines:** [269-278]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.LookedUpPaymentLine.get across lines 269-278.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:273:52:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:273:52:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [273-273]
- **Behavior:** Executes logic for lambda expression across lines 273-273.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:284:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:284:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OnLookedUpOrderChanged(Falah.Application.Orders.OrderDetailsDto?)
- **Lines:** [284-291]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OnLookedUpOrderChanged(Falah.Application.Orders.OrderDetailsDto?) across lines 284-291.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:300:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:300:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OpenOrderLookup()
- **Lines:** [300-301]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OpenOrderLookup() across lines 300-301.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:303:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:303:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.CloseOrderLookup()
- **Lines:** [303-309]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.CloseOrderLookup() across lines 303-309.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:311:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:311:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.LookupOrderAsync()
- **Lines:** [311-334]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.LookupOrderAsync() across lines 311-334.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:336:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:336:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.ReprintLookedUpOrderAsync()
- **Lines:** [336-344]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.ReprintLookedUpOrderAsync() across lines 336-344.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:346:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:346:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RetryPrintAsync()
- **Lines:** [346-351]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RetryPrintAsync() across lines 346-351.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:353:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:353:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SendPrintAsync(Falah.Application.Printing.CreatePrintJobsRequest)
- **Lines:** [353-382]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SendPrintAsync(Falah.Application.Printing.CreatePrintJobsRequest) across lines 353-382.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:384:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:384:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.ClearResolvedPrint()
- **Lines:** [384-391]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.ClearResolvedPrint() across lines 384-391.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:393:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs:393:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.ClearOrderLookup()
- **Lines:** [393-402]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.ClearOrderLookup() across lines 393-402.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
