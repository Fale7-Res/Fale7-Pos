# src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs

- **Path:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current Worktree SHA256:** `bc399716e95c2a9b42e3c5db4ad92d6e3fe9879f2b042fa539ec6298d6adea7a`
- **Git Blob SHA256:** `edca407e8d263ab557963eace4bf57c54e0858ab`
- **Baseline Comparison:** `EQUIVALENT_NORMALIZED_LINE_ENDINGS`
- **Auditor Role:** Mirrored Code and Architectural Parity Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED — VERIFIED GENUINE AUDIT**
- **Reviewed Line Spans:** `[[1, 140]]`

---

## 1. Architectural Scope and Design Role

[`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs#L1-L140) provides the primary refund and return orchestration on the WinUI 3 touch till. It works in conjunction with `CashierViewModel.OrderLookup` to allow cashiers to inspect historical order details, format split payment breakdowns, collect refund amounts and reason codes, solicit supervisor PIN approvals, and dispatch [`RefundOrderRequest`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Orders/RefundOrderRequest.cs) commands.

### Core Architectural Responsibilities:
1. **Idempotent Refund Retry (`_pendingRefund`):** Addresses the fatal defect identified in legacy WPF (`FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001`). When an API call returns an ambiguous outcome (`OutcomeUnknown`), WinUI 3 retains `_pendingRefund` and reuses `_refundRequestId`, ensuring duplicate cash disbursements cannot occur upon cashier retry.
2. **Client-Side Defense Against `FIN-003`:** Enforces `CanRefundOrder` requiring `OrderLookupResult.IsPaid == true`. This blocks cash extraction on unpaid COD or unconfirmed electronic orders at the UI boundary.
3. **Dual-Control Supervisor PIN Verification:** Integrates with `ResolveManagerApprovalIfNeededAsync` to enforce dual control for cashiers lacking `orders_refund` permissions or for refunds exceeding large-value thresholds.
4. **Historical Tender & Order Breakdown Display:** Resolves split payment details (`LookupPaymentBreakdown`), dine-in table assignment (`LookupTableLine`), and Cairo-wall-clock pickup schedules (`LookupRequestedPickupLine`).
5. **Cross-Shift Reconciliation Awareness (`FIN-004`):** Submits refunds that the server allocates against the current shift's cash drawer while preserving the original order's shift identity.

*(Disambiguation Note: Consistent with the Audit Coordinator directive, SEM-MIRROR-065 is strictly reserved for attendance correction validation and is not attached to this refund review).*

---

## 2. Exhaustive Callable Inventory and Source Review

### Callable 1: `get_RefundNeedsApproval`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:19:5:PropertyDeclaration`
- **Exact Line Range:** Line 19
- **Signature:** `bool Falah.Client.CashierWinUI.ViewModels.CashierViewModel.RefundNeedsApproval.get`
- **Kind:** `PropertyDeclaration` (Getter)
- **Actual Behavior & Semantics:**
  - Evaluates `!_session.HasPermission("orders_refund")`.
  - Determines whether manager PIN input is mandated for the current cashier.
- **Verdict:** **PARITY_VERIFIED**

---

### Callable 2: `get_CanRefundOrder` (Client-Side Defense against FIN-003)

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:20:5:PropertyDeclaration`
- **Exact Line Range:** Lines 20–21
- **Signature:** `bool Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CanRefundOrder.get`
- **Kind:** `PropertyDeclaration` (Getter)
- **Actual Behavior & Invariants:**
  ```csharp
  public bool CanRefundOrder => OrderLookupResult is { IsPaid: true, PaymentMethod: not "multi", Status: not "cancelled" } order &&
      order.TotalPiastres > order.RefundAmountPiastres;
  ```
  **Forensic Analysis:**
  - **`IsPaid: true` Check:** Defends against `FIN-003`. Unpaid COD and pending electronic orders have `IsPaid == false`, disabling the refund button and preventing fraudulent cash drawer extraction.
  - **`PaymentMethod: not "multi"` Check:** Disables refund for split tender orders, matching server domain limitations.
  - **`Status: not "cancelled"` Check:** Prevents refunding cancelled orders.
  - **Balance Check:** Ensures `TotalPiastres > RefundAmountPiastres`.
- **Verdict:** **PARITY_SUPERIOR (Blocks FIN-003 Client-Side)**

---

### Callable 3: `get_LookupHasPendingPayment`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:22:5:PropertyDeclaration`
- **Exact Line Range:** Line 22
- **Signature:** `bool Falah.Client.CashierWinUI.ViewModels.CashierViewModel.LookupHasPendingPayment.get`
- **Kind:** `PropertyDeclaration` (Getter)
- **Actual Behavior & Semantics:**
  - Returns `true` if `OrderLookupResult?.ElectronicPaymentStatus == "pending"`.
  - Used to display electronic payment warning banners and activate `ReviewPendingOrder()`.
- **Verdict:** **VERIFIED**

---

### Callable 4: `get_LookupPaymentBreakdown`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:30:9:GetAccessorDeclaration`
- **Exact Line Range:** Lines 28–36
- **Signature:** `string Falah.Client.CashierWinUI.ViewModels.CashierViewModel.LookupPaymentBreakdown.get`
- **Kind:** `GetAccessorDeclaration`
- **Associated Lambda:** Line 34 (`s => $"{MethodLabel(s.Method)} {CashInput.Format(s.AmountPiastres)} ج"`) with ID `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:34:53:SimpleLambdaExpression`.
- **Actual Behavior & Semantics:**
  - Formats split payment legs into a human-readable string (e.g., `"كاش 100.00 ج + فيزا 50.00 ج"`).
  - Falls back to single method label if split payments are absent.
- **Verdict:** **VERIFIED**

---

### Callable 5: `get_HasLookupPaymentBreakdown`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:38:5:PropertyDeclaration`
- **Exact Line Range:** Line 38
- **Signature:** `bool Falah.Client.CashierWinUI.ViewModels.CashierViewModel.HasLookupPaymentBreakdown.get`
- **Kind:** `PropertyDeclaration` (Getter)
- **Actual Behavior & Semantics:**
  - Returns `LookupPaymentBreakdown.Length > 0`. Binds to UI element visibility.
- **Verdict:** **VERIFIED**

---

### Callable 6: `get_LookupTableLine`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:41:5:PropertyDeclaration`
- **Exact Line Range:** Lines 41–42
- **Signature:** `string Falah.Client.CashierWinUI.ViewModels.CashierViewModel.LookupTableLine.get`
- **Kind:** `PropertyDeclaration` (Getter)
- **Actual Behavior & Semantics:**
  - Formats table number (e.g., `"طاولة 5"`) if present.
- **Verdict:** **VERIFIED**

---

### Callable 7: `get_HasLookupTable`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:44:5:PropertyDeclaration`
- **Exact Line Range:** Line 44
- **Signature:** `bool Falah.Client.CashierWinUI.ViewModels.CashierViewModel.HasLookupTable.get`
- **Kind:** `PropertyDeclaration` (Getter)
- **Actual Behavior & Semantics:**
  - Evaluates `LookupTableLine.Length > 0`.
- **Verdict:** **VERIFIED**

---

### Callable 8: `get_LookupRequestedPickupLine`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:48:5:PropertyDeclaration`
- **Exact Line Range:** Lines 48–51
- **Signature:** `string Falah.Client.CashierWinUI.ViewModels.CashierViewModel.LookupRequestedPickupLine.get`
- **Kind:** `PropertyDeclaration` (Getter)
- **Actual Behavior & Timezone Handling:**
  - Converts UTC `RequestedPickupAtUtc` to Cairo local timezone via `TimeZoneInfo.ConvertTime(at, BusinessClock.CairoTimeZone)`.
  - Formats as `"yyyy-MM-dd HH:mm"`.
- **Verdict:** **TIMEZONE_PARITY_VERIFIED**

---

### Callable 9: `get_HasLookupRequestedPickup`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:53:5:PropertyDeclaration`
- **Exact Line Range:** Line 53
- **Signature:** `bool Falah.Client.CashierWinUI.ViewModels.CashierViewModel.HasLookupRequestedPickup.get`
- **Kind:** `PropertyDeclaration` (Getter)
- **Actual Behavior & Semantics:**
  - Returns `LookupRequestedPickupLine.Length > 0`.
- **Verdict:** **VERIFIED**

---

### Callable 10: `MethodLabel(string)`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:55:5:MethodDeclaration`
- **Exact Line Range:** Lines 55–63
- **Signature:** `string Falah.Client.CashierWinUI.ViewModels.CashierViewModel.MethodLabel(string)`
- **Kind:** `MethodDeclaration` (Static Private Helper)
- **Actual Behavior & Semantics:**
  - Localizes payment methods to Arabic: `"cash"` -> `"كاش"`, `"card"` -> `"فيزا"`, `"instapay"` -> `"إنستاباي"`, `"wallet"` -> `"محفظة"`, `"multi"` -> `"دفع مقسّم"`.
- **Verdict:** **VERIFIED**

---

### Callable 11: `get_RefundRemainingDisplay`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:64:5:PropertyDeclaration`
- **Exact Line Range:** Line 64
- **Signature:** `string Falah.Client.CashierWinUI.ViewModels.CashierViewModel.RefundRemainingDisplay.get`
- **Kind:** `PropertyDeclaration` (Getter)
- **Actual Behavior & Semantics:**
  - Formats remaining refundable balance: `CashInput.Format(Math.Max(0, TotalPiastres - RefundAmountPiastres)) + " ج"`.
- **Verdict:** **VERIFIED**

---

### Callable 12: `OnIsRefundingChanged(bool)`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:65:5:MethodDeclaration`
- **Exact Line Range:** Line 65
- **Signature:** `void Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsRefundingChanged(bool)`
- **Kind:** `MethodDeclaration` (Partial Callback)
- **Actual Behavior & Semantics:**
  - Calls `RefreshOperationState()` to update busy state across POS views.
- **Verdict:** **VERIFIED**

---

### Callable 13: `OnOrderLookupResultChanged(OrderDetailsDto?)`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:66:5:MethodDeclaration`
- **Exact Line Range:** Lines 66–73
- **Signature:** `void Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnOrderLookupResultChanged(Falah.Application.Orders.OrderDetailsDto?)`
- **Kind:** `MethodDeclaration` (Partial Callback)
- **Actual Behavior & Semantics:**
  - Fires reactive property change notifications for all lookup and refund state properties.
- **Verdict:** **VERIFIED**

---

### Callable 14: `OpenRefund()`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:75:5:MethodDeclaration`
- **Exact Line Range:** Lines 75–80
- **Signature:** `void Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OpenRefund()`
- **Kind:** `MethodDeclaration` (Command Handler)
- **Actual Behavior & Execution Flow:**
  - Checks `if (!CanEditOperation || !CanRefundOrder) return;`.
  - Pre-populates `RefundAmountText` with exact remaining refundable piastres formatted via `CashInput`.
  - Clears reason and PIN, opens refund modal (`IsRefundOpen = true`).
- **Verdict:** **VERIFIED**

---

### Callable 15: `CancelRefund()`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:81:5:MethodDeclaration`
- **Exact Line Range:** Lines 81–86
- **Signature:** `void Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CancelRefund()`
- **Kind:** `MethodDeclaration` (Command Handler)
- **Actual Behavior & Guard Logic:**
  - `if (IsRefunding) return;`
  - `if (_pendingRefund is not null) { RefundError = "أعد المحاولة للتحقق من الاسترداد قبل الإغلاق."; return; }`
  - Blocks modal closing if an in-flight refund has an unknown outcome, forcing verification.
- **Verdict:** **SAFETY_VERIFIED**

---

### Callable 16: `SubmitRefundAsync()`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:87:5:MethodDeclaration`
- **Exact Line Range:** Lines 87–109
- **Signature:** `System.Threading.Tasks.Task Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitRefundAsync()`
- **Kind:** `MethodDeclaration` (Command Handler, Async)
- **Associated Lambda:** Line 100 (`e => RefundError = e`) with ID `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:100:70:SimpleLambdaExpression`.
- **Actual Behavior & Idempotent Retry Execution:**
  1. Re-entrancy guard: `if (IsRefunding || OrderLookupResult is null) return;`.
  2. If `_pendingRefund is null`:
     - Parses `RefundAmountText` via `CashInput.TryParse`.
     - Validates `amount > 0` and `amount <= remainingRefundable`.
     - Validates non-empty `RefundReason`.
     - Solicits manager approval PIN via `ResolveManagerApprovalIfNeededAsync`.
     - Constructs `request = new RefundOrderRequest(_refundRequestId, OrderLookupResult.Id, amount, RefundReason, CashierId, approvalId, approver)`.
     - Calls `CompleteRefundAsync(request, false)`.
  3. If `_pendingRefund is not null` (Retry Path):
     - Calls `CompleteRefundAsync(_pendingRefund, true)` using the **exact frozen request**, preserving `_refundRequestId`!
- **Verdict:** **PARITY_SUPERIOR (Solves FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001)**

---

### Callable 17: `CompleteRefundAsync(RefundOrderRequest, bool)`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:110:5:MethodDeclaration`
- **Exact Line Range:** Lines 110–122
- **Signature:** `System.Threading.Tasks.Task Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteRefundAsync(Falah.Application.Orders.RefundOrderRequest, bool)`
- **Kind:** `MethodDeclaration` (Private Async Helper)
- **Actual Behavior & Execution Flow:**
  1. Freezes `_pendingRefund = request; RefreshOperationState();`.
  2. Dispatches `_session.Api.RefundAsync(request)`.
  3. If success or non-ambiguous failure: clears `_pendingRefund = null;`.
  4. If `OutcomeUnknown`: retains `_pendingRefund`.
  5. On success:
     - Rotates key: `_refundRequestId = Guid.NewGuid().ToString("N");`.
     - Updates `OrderLookupResult` with new status and updated `RefundAmountPiastres`.
     - Sets `ReprintStatusMessage` and closes modal.
- **Verdict:** **VERIFIED**

---

### Callable 18: `ReviewPendingOrder()`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:124:5:MethodDeclaration`
- **Exact Line Range:** Lines 124–131
- **Signature:** `void Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ReviewPendingOrder()`
- **Kind:** `MethodDeclaration` (Command Handler)
- **Actual Behavior & Semantics:**
  - Allows an operator to review an existing order with pending electronic payment in the main checkout view to confirm or cancel it.
- **Verdict:** **VERIFIED**

---

### Callable 19: `DismissCheckoutResult()`

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:132:5:MethodDeclaration`
- **Exact Line Range:** Lines 132–138
- **Signature:** `void Falah.Client.CashierWinUI.ViewModels.CashierViewModel.DismissCheckoutResult()`
- **Kind:** `MethodDeclaration` (Command Handler)
- **Actual Behavior & Semantics:**
  - Clears `LastCheckoutResult` and conditionally resets delivery state.
- **Verdict:** **VERIFIED**

---

## 3. Financial Invariant & Risk Corroboration

1. **`FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001` (Resolved in WinUI 3):**
   WinUI 3 freezes `_pendingRefund` and maintains `_refundRequestId` across network retries. In contrast, WPF re-mints `rreq-{Guid}` on every click, creating duplicate refund records and double payouts.
2. **`FIN-003` (Cash Extraction on Unpaid Orders):**
   WinUI 3 blocks this at the UI layer via `CanRefundOrder => OrderLookupResult is { IsPaid: true, ... }`. However, the backend domain `Order.ApplyRefund` still lacks the `IsPaid` guard.
3. **`FIN-004` (Cross-Shift Refund Drawer Discrepancy):**
   `RefundOrderHandler` books the cash payout against the operational shift drawer (`OperationalShiftId`), while the order references `OriginalShiftId`. Both IDs are carried on `OrderRefund` entities. Reports must separate operational drawer movements from historical order shift totals.

---

## 4. Formal Recommendations

1. **Retain WinUI 3 Idempotency Pattern:** Standardize the `_pendingRefund` frozen DTO pattern as the system-wide baseline for all financial commands.
2. **Unify UI & Server Guards for FIN-003:** While WinUI 3 enforces `IsPaid: true`, ensure the backend domain aggregate ([`Order.ApplyRefund`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L220)) also enforces `if (!IsPaid) throw ...` to protect against rogue API clients.
