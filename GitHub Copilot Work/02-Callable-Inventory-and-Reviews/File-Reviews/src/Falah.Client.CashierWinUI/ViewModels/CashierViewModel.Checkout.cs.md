# File Review: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:13:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:13:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnAmountPaidTextChanged(string)
- **Lines:** [13-13]
- **Behavior:** Partial change handler invoked when AmountPaidText changes. Resets _replaceTenderOnNextKey flag to false so subsequent numpad keystrokes append digits, and notifies UI property change for ChangeDuePiastres.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:28:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:28:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsDiscountEnabledChanged(bool)
- **Lines:** [28-35]
- **Behavior:** Partial change handler invoked when IsDiscountEnabled toggles. When disabled, resets DiscountAmountText to "0" and clears DiscountManagerPin. Notifies PropertyChanged for DiscountAmountPiastres, GrandTotalPiastres, GrandTotalDisplay, and calls NotifyTotals().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:37:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:37:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnDiscountAmountTextChanged(string)
- **Lines:** [37-43]
- **Behavior:** Partial change handler invoked when DiscountAmountText changes. Re-evaluates discount value and notifies PropertyChanged for DiscountAmountPiastres, GrandTotalPiastres, GrandTotalDisplay, and calls NotifyTotals().
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:65:62:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:65:62:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompReasonCodes.get
- **Lines:** [65-65]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompReasonCodes.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:72:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:72:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SetCompReasonCode(string)
- **Lines:** [72-73]
- **Behavior:** RelayCommand setting CompReasonCode to the selected pre-defined reason code (hospitality, complaint, kitchen_error, staff_meal, or owner_guest).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:75:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:75:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsCompEnabledChanged(bool)
- **Lines:** [75-82]
- **Behavior:** Partial change handler invoked when IsCompEnabled toggles. When disabled, resets CompAmountText to "0", clears CompReasonText and CompManagerPin, and raises PropertyChanged for CompAmountPiastres, GrandTotalPiastres, GrandTotalDisplay, and calls NotifyTotals().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:84:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:84:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnCompAmountTextChanged(string)
- **Lines:** [84-90]
- **Behavior:** Partial change handler invoked when CompAmountText changes. Notifies PropertyChanged for CompAmountPiastres, GrandTotalPiastres, GrandTotalDisplay, and calls NotifyTotals().
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:100:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:100:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SetSplitMethod1(string)
- **Lines:** [100-101]
- **Behavior:** RelayCommand updating SplitMethod1 (e.g. "cash", "card", "wallet") for the first tender portion of a split payment.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:103:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:103:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SetSplitMethod2(string)
- **Lines:** [103-104]
- **Behavior:** RelayCommand updating SplitMethod2 (e.g. "cash", "card", "wallet") for the second tender portion of a split payment.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:121:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:121:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ConfirmElectronicPaymentAsync()
- **Lines:** [121-144]
- **Behavior:** RelayCommand confirming an in-flight electronic payment. Guarded by CanConfirmPendingPayment and non-null LastCheckoutResult. Dispatches ConfirmElectronicPaymentRequest via retained request helper _pendingElectronicConfirm.SendAsync executing _session.Api.ConfirmElectronicPaymentAsync. On success, updates LastCheckoutResult with confirmed ElectronicPaymentStatus and IsPaid, and notifies IsElectronicPaymentPending.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:131:77:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:131:77:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [131-131]
- **Behavior:** API client invocation delegate r => _session.Api.ConfirmElectronicPaymentAsync(r) passed to _pendingElectronicConfirm.SendAsync.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:146:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:146:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CancelPendingElectronicPaymentAsync()
- **Lines:** [146-174]
- **Behavior:** RelayCommand cancelling a pending electronic payment. Validates CanCancelPendingPayment, non-null LastCheckoutResult, and enforces non-empty CancelPendingReason. Submits CancelPendingElectronicPaymentRequest via retained request helper _pendingElectronicCancel.SendAsync executing _session.Api.CancelPendingElectronicPaymentAsync. On success, clears LastCheckoutResult and CancelPendingReason.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:161:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:161:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [161-161]
- **Behavior:** API client invocation delegate r => _session.Api.CancelPendingElectronicPaymentAsync(r) passed to _pendingElectronicCancel.SendAsync.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:184:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:184:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnLastCheckoutResultChanged(Falah.Application.Orders.CheckoutOrderResult?)
- **Lines:** [184-194]
- **Behavior:** Partial change handler invoked when LastCheckoutResult is set or cleared. Notifies dependent UI properties: HasCheckoutResult, CheckoutTitle, CheckoutSubtitle, IsElectronicPaymentPending, CanConfirmPendingPayment, CanCancelPendingPayment, HasPrintWarning, and invokes NotifyCheckoutAvailability().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:198:9:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:198:9:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ChangeDuePiastres.get
- **Lines:** [198-203]
- **Behavior:** Getter computing customer change due in piastres. Parses AmountPaidText via CashInput.TryParse; if parsed amount exceeds GrandTotalPiastres, returns the positive difference; otherwise returns 0.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:222:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:222:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.NotifyCheckoutAvailability()
- **Lines:** [222-226]
- **Behavior:** Helper method raising PropertyChanged for CanOpenCheckout and CheckoutBlockedReason to refresh checkout button enabled state and user feedback text.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:228:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:228:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OpenCheckout()
- **Lines:** [228-248]
- **Behavior:** RelayCommand opening the checkout tender dialog. Enforces preconditions (CanEditOperation, non-empty cart, menu loaded without error). Checks required customer and address fields for delivery and pickup orders, routing to their editors if incomplete. Resets PaymentMethod to "cash", pre-populates AmountPaidText with GrandTotalPiastres, sets _replaceTenderOnNextKey flag, mints idempotency key CurrentCheckoutRequestId, and sets IsCheckoutOpen = true.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:250:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:250:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CloseCheckout()
- **Lines:** [250-251]
- **Behavior:** RelayCommand closing the checkout modal. Guarded against execution while IsOperationBusy or if an unconfirmed transaction exists in _pendingCheckout (displaying a warning that retry/verification is required before closing).
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:253:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:253:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SetPaymentMethod(string)
- **Lines:** [253-261]
- **Behavior:** RelayCommand setting payment method (cash, card, wallet, etc.) when CanEditOperation allows. For non-cash methods, automatically sets AmountPaidText to exact GrandTotalPiastres and updates ChangeDuePiastres.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:263:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:263:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.PressAmountKey(string)
- **Lines:** [263-276]
- **Behavior:** RelayCommand handling on-screen numpad key input ("0"-"9", ".", "back", "clear"). Replaces tender amount if _replaceTenderOnNextKey is set; otherwise appends or strips characters, and updates ChangeDuePiastres.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:278:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:278:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.QuickCash(string)
- **Lines:** [278-285]
- **Behavior:** RelayCommand setting preset cash denominations ("exact", "50", "100", "200"). For "exact", formats GrandTotalPiastres. Sets _replaceTenderOnNextKey to true and updates ChangeDuePiastres.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:287:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:287:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CanSubmitPayment()
- **Lines:** [287-287]
- **Behavior:** Command execution predicate returning true when checkout is not currently being processed (!IsProcessingCheckout).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:289:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:289:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitPaymentAsync()
- **Lines:** [289-389]
- **Behavior:** RelayCommand validating and processing checkout payment. Synchronously guards against double clicks with IsProcessingCheckout. Replays _pendingCheckout if present. Enforces non-empty CompReasonText if comp enabled; checks CanDiscount; validates numeric amounts; requests manager approval via ResolveManagerApprovalIfNeededAsync if comp > 0; validates split payment portions; maps cart items to CheckoutItemRequest list; constructs CheckoutOrderRequest with current cashier ID, shift ID, delivery/pickup metadata, and CustomerPrintRoute="Receipt"; and awaits CompleteCheckoutAsync.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:318:87:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:318:87:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [318-318]
- **Behavior:** Error callback delegate e => CheckoutError = e passed to ResolveManagerApprovalIfNeededAsync to surface authorization failures on the checkout dialog.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:342:38:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:342:38:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [342-342]
- **Behavior:** Inline expression lambda '? splitPayments!.Sum(s => s.AmountPiastres)'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:353:36:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:353:36:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [353-353]
- **Behavior:** LINQ transformation delegate mapping Cart LineItem models to CheckoutItemRequest DTOs (ProductId, VariantId, Quantity, AppliedOfferId, Notes).
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:391:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:391:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.NullIfEmpty(string)
- **Lines:** [391-391]
- **Behavior:** Static utility method returning trimmed string or null if string is null, empty, or whitespace, used to normalize optional metadata fields before API transmission.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:393:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:393:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.StartNewOrder()
- **Lines:** [393-400]
- **Behavior:** RelayCommand clearing the completed transaction state when CanEditOperation allows. Resets LastCheckoutResult, CheckoutError, and calls ResetDelivery() to initialize state for the next customer.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:402:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs:402:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteCheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest)
- **Lines:** [402-427]
- **Behavior:** Submits CheckoutOrderRequest to backend via _session.Api.CheckoutAsync(request). Manages _pendingCheckout state to retain request across network ambiguities for idempotent replay. Upon successful response, clears pending request, stores LastCheckoutResult, closes checkout dialog, empties cart, resets discount/comp/split toggles, and clears CurrentCheckoutRequestId.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---


## Candidate Findings & Observations
- **Dead-end Discount Manager PIN Workflow (lines 23-24, 308-309):**
  In `CashierViewModel.Checkout.cs`, `DiscountManagerPin` and `DiscountNeedsApproval` (`!_session.HasPermission("orders_discount")`) are defined and bound to the checkout UI. However, in `SubmitPaymentAsync` (lines 308-309):
  ```csharp
  if (DiscountAmountPiastres > 0 && !CanDiscount)
  { CheckoutError = "لا تملك صلاحية تطبيق الخصم."; return; }
  ```
  The code directly blocks unprivileged cashiers (`!CanDiscount`) without ever attempting manager PIN authorization (`ResolveManagerApprovalIfNeededAsync`). Unlike `CompAmountPiastres` which solicits approval, `DiscountManagerPin` is ignored, rendering manual discounts unusable for staff lacking direct `orders_discount` permissions even when a manager PIN is provided.
