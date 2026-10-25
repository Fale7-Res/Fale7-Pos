# File Review: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:30:9:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:30:9:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.LookupPaymentBreakdown.get
- **Lines:** [30-35]
- **Behavior:** Formats the payment breakdown of the currently looked-up order for display. If the order used split payments (SplitPayments.Count > 0), joins each leg formatted with localized method name and piastre amount in EGP. Otherwise formats the single payment method using MethodLabel. Returns empty string if no order is looked up.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:34:53:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:34:53:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [34-34]
- **Behavior:** Selector lambda formatting an individual split payment leg as "{MethodLabel(s.Method)} {CashInput.Format(s.AmountPiastres)} ج".
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:55:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:55:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.MethodLabel(string)
- **Lines:** [55-63]
- **Behavior:** Maps internal payment method strings to Arabic display labels: "cash" -> "كاش", "card" -> "فيزا", "instapay" -> "إنستاباي", "wallet" -> "محفظة", "multi" -> "دفع مقسّم", returning the raw string as fallback.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:65:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:65:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsRefundingChanged(bool)
- **Lines:** [65-65]
- **Behavior:** Lifecycle notification callback triggered when IsRefunding changes, invoking RefreshOperationState() to update UI command enabling and busy indicators.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:66:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:66:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnOrderLookupResultChanged(Falah.Application.Orders.OrderDetailsDto?)
- **Lines:** [66-73]
- **Behavior:** Lifecycle notification callback triggered when OrderLookupResult changes, raising PropertyChanged notifications for derived order properties: CanRefundOrder, LookupHasPendingPayment, RefundRemainingDisplay, LookupPaymentBreakdown, HasLookupPaymentBreakdown, LookupTableLine, HasLookupTable, LookupRequestedPickupLine, and HasLookupRequestedPickup.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:75:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:75:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OpenRefund()
- **Lines:** [75-80]
- **Behavior:** Relay command opening the refund dialog for an eligible looked-up order. Validates CanEditOperation and CanRefundOrder (order must be paid, non-multi payment, non-cancelled, with remaining refundable balance). Prepopulates RefundAmountText with remaining balance, resets reason and PIN, and sets IsRefundOpen = true.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:81:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:81:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CancelRefund()
- **Lines:** [81-86]
- **Behavior:** Relay command cancelling the refund dialog. Guards against dismissal while refund is actively executing (IsRefunding). If an unconfirmed refund request is pending (_pendingRefund is not null), blocks closing and prompts operator to retry verification first to avoid duplicate refunds. Otherwise closes modal and resets manager PIN.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:87:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:87:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitRefundAsync()
- **Lines:** [87-109]
- **Behavior:** Relay command validating and submitting order refund. If an unresolved refund request is pending, retries it directly. Otherwise parses RefundAmountText (must be > 0 and <= remaining balance), requires non-empty RefundReason, evaluates manager PIN approval if RefundNeedsApproval (!HasPermission("orders_refund")), constructs RefundOrderRequest with idempotent _refundRequestId, and invokes CompleteRefundAsync.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:100:70:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:100:70:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [100-100]
- **Behavior:** Inline expression lambda 'RefundManagerPin, "orders_refund", RefundReason, e => RefundError = e);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:110:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:110:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteRefundAsync(Falah.Application.Orders.RefundOrderRequest, bool)
- **Lines:** [110-122]
- **Behavior:** Submits refund request to _session.Api.RefundAsync. Tracks pending request in _pendingRefund and refreshes operation state. If outcome is unknown, preserves _pendingRefund for safe idempotency retry. On success, regenerates _refundRequestId, updates OrderLookupResult with new order status and updated RefundAmountPiastres, sets reprint notice, closes dialog, and clears PIN.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:124:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:124:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ReviewPendingOrder()
- **Lines:** [124-131]
- **Behavior:** Relay command allowing cashier to review an order with pending electronic payment (LookupHasPendingPayment). Sets _reviewingExistingOrder = true, populates LastCheckoutResult with order snapshot data, clears pending errors, and closes the lookup dialog.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:132:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:132:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.DismissCheckoutResult()
- **Lines:** [132-138]
- **Behavior:** Relay command dismissing checkout result overlay. Clears LastCheckoutResult, resets delivery state if not reviewing an existing order, and resets _reviewingExistingOrder = false.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
