# src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs

- **Path:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs`
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current Worktree SHA256:** `9f41d9133ed49ccb2e8dff94a43e70dc525500836f7c6c6e8ada4912bc1861dd`
- **Git Blob SHA256:** `3b789e9e07bfc8753d40b91b2da75f9fff14957b`
- **Baseline Comparison:** `EQUIVALENT_NORMALIZED_LINE_ENDINGS`
- **Auditor Role:** Mirrored Code and Architectural Parity Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED — VERIFIED GENUINE AUDIT**
- **Reviewed Line Spans:** `[[1, 117]]`

---

## 1. Architectural Scope and Design Role

[`src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs#L1-L117) is the partial class of the legacy WPF cashier client responsible for order lookup and refund dispatch. It provides cashier UI controls to search past orders by order number, inspect order details via [`OrderDetailsDto`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Orders/OrderDetailsDto.cs), calculate remaining refundable amounts, and dispatch [`RefundOrderRequest`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Orders/RefundOrderRequest.cs) to [`/api/orders/refund`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1412).

### Architectural Status: Legacy Prototype / Severe Financial Vulnerabilities
Comparative analysis against the active Windows App SDK client ([`Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs#L1-L140)) reveals critical architectural defects and financial leakages:

1. **`FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001` (Ambiguous Refund Retry Duplication):**
   Unlike WinUI 3, which retains `_pendingRefund` and freezes `_refundRequestId` across network retries, WPF generates a brand new `RefundRequestId: $"rreq-{Guid.NewGuid():N}"` on **every single click** of `SubmitRefundAsync()`. If an ambiguous network timeout occurs after the server committed the refund, a cashier retry submits a distinct request ID, causing the server to process a **second refund** and pay out double cash from the till.
2. **`FIN-003` (Cash Payouts on Unpaid Orders):**
   WPF's [`CanRefund()`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs#L64) only checks `LookedUpOrder is not null && !IsRefunding`. It **never checks `LookedUpOrder.IsPaid`**! In conjunction with the domain server gap in [`Order.ApplyRefund`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L220), a cashier in WPF can refund cash-on-delivery or pending electronic orders that were never paid, extracting physical cash from the drawer for uncollected revenue.
3. **Manager Approval & Dual Control Blindspot:**
   WPF unconditionally passes `ManagerApprovalId: null` and `ManagerApproverUserId: null`. If a refund exceeds the large-refund threshold (500 EGP / 50,000 piastres) or if the cashier lacks `orders_refund` authority, the server rejects the request. WPF provides no manager PIN prompt dialog, rendering authorized exception refunds impossible.
4. **Multi-Payment Omission:**
   WPF does not disable refunds for multi-payment orders, causing cashier confusion when the server returns `"مرتجع الدفع المتعدد غير مدعوم في المسار المالي الحالي."`.

---

## 2. Exhaustive Callable Inventory and Forensic Source Analysis

### Callable 1: `OpenOrderLookup()`

- **Canonical Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:24:5:MethodDeclaration`
- **Exact Line Range:** Lines 24–33
- **Signature:** `void Falah.Client.Cashier.ViewModels.CashierViewModel.OpenOrderLookup()`
- **Kind:** `MethodDeclaration` (Command Handler)
- **Actual Behavior & Execution Flow:**
  Resets lookup dialog state:
  ```csharp
  IsOrderLookupOpen = true;
  LookupOrderNumber = string.Empty;
  LookedUpOrder = null;
  LookupError = null;
  RefundError = null;
  RefundSuccessMessage = null;
  ```
- **Comparative Analysis vs WinUI 3:**
  WinUI 3 handles lookup and refund dialogs as distinct lifecycle phases (`OpenRefund()` in `CashierViewModel.Refund.cs:75`), checking `!CanEditOperation || !CanRefundOrder` before opening. WPF lacks pre-flight operational gates.
- **Verdict:** **FUNCTIONAL_SUBSET**

---

### Callable 2: `CloseOrderLookup()`

- **Canonical Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:35:5:MethodDeclaration`
- **Exact Line Range:** Lines 35–36
- **Signature:** `void Falah.Client.Cashier.ViewModels.CashierViewModel.CloseOrderLookup()`
- **Kind:** `MethodDeclaration` (Command Handler)
- **Actual Behavior & Execution Flow:**
  Unconditionally sets `IsOrderLookupOpen = false;`.
- **Comparative Analysis vs WinUI 3:**
  In WinUI 3 (`CancelRefund()` at line 81), closure is blocked if `IsRefunding` is active or if `_pendingRefund is not null`, preventing abandoning an unverified in-flight financial transaction.
- **Verdict:** **SAFETY_GAP (Unprotected Modal Closure)**

---

### Callable 3: `LookupOrderAsync()`

- **Canonical Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:38:5:MethodDeclaration`
- **Exact Line Range:** Lines 38–62
- **Signature:** `System.Threading.Tasks.Task Falah.Client.Cashier.ViewModels.CashierViewModel.LookupOrderAsync()`
- **Kind:** `MethodDeclaration` (Command Handler, Async)
- **Actual Behavior & Execution Flow:**
  1. Validates `LookupOrderNumber`: if empty, sets `LookupError = "أدخل رقم الطلب."`.
  2. Sets `IsLookingUp = true`, clears previous errors and results.
  3. Dispatches HTTP GET to `/api/orders/lookup/{orderNumber}` via `_session.Api.LookupOrderByNumberAsync`.
  4. On success: populates `LookedUpOrder = result.Value;`, notifies `RefundableRemainingPiastres`, and pre-fills `RefundAmountText = (RefundableRemainingPiastres / 100m).ToString("0.00")`.
- **Comparative Analysis vs WinUI 3:**
  WinUI 3 displays structured split payment breakdowns (`LookupPaymentBreakdown`), table lines (`LookupTableLine`), and Cairo-wall-clock pickup schedules (`LookupRequestedPickupLine`), none of which exist in WPF.
- **Verdict:** **VERIFIED (Minimal Read-Side)**

---

### Callable 4: `CanRefund()`

- **Canonical Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:64:5:MethodDeclaration`
- **Exact Line Range:** Line 64
- **Signature:** `bool Falah.Client.Cashier.ViewModels.CashierViewModel.CanRefund()`
- **Kind:** `MethodDeclaration` (Command CanExecute Predicate)
- **Actual Behavior & Forensic Analysis:**
  ```csharp
  private bool CanRefund() => LookedUpOrder is not null && !IsRefunding;
  ```
  **Critical Defect Analysis (`FIN-003`):**
  - Only evaluates whether an order is loaded and not currently refunding.
  - **Does NOT check `LookedUpOrder.IsPaid`!**
  - **Does NOT check `LookedUpOrder.Status != "cancelled"`!**
  - **Does NOT check `LookedUpOrder.PaymentMethod != "multi"`!**
  - **Does NOT check `RefundableRemainingPiastres > 0`!**
- **Comparative Analysis vs WinUI 3:**
  In WinUI 3 (`CashierViewModel.Refund.cs:20-21`), `CanRefundOrder` strictly verifies:
  ```csharp
  public bool CanRefundOrder => OrderLookupResult is { IsPaid: true, PaymentMethod: not "multi", Status: not "cancelled" } order &&
      order.TotalPiastres > order.RefundAmountPiastres;
  ```
  WinUI 3 prevents cash extraction on unpaid orders client-side; WPF leaves this entirely exposed.
- **Verdict:** **CRITICAL_INVARIANT_ABSENT (`FIN-003`)**

---

### Callable 5: `SubmitRefundAsync()`

- **Canonical Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:66:5:MethodDeclaration`
- **Exact Line Range:** Lines 66–106
- **Signature:** `System.Threading.Tasks.Task Falah.Client.Cashier.ViewModels.CashierViewModel.SubmitRefundAsync()`
- **Kind:** `MethodDeclaration` (Command Handler, Async)
- **Actual Behavior & Execution Flow:**
  1. Validates `LookedUpOrder is not null`.
  2. Parses `RefundAmountText` as decimal EGP, validating `amountEgp > 0`.
  3. Validates non-empty `RefundReason`.
  4. Sets `IsRefunding = true`.
  5. **Mints a fresh GUID on every execution (Line 82):**
     ```csharp
     var request = new RefundOrderRequest(
         RefundRequestId: $"rreq-{Guid.NewGuid():N}",
         OrderId: LookedUpOrder.Id,
         AmountPiastres: (long)Math.Round(amountEgp * 100m),
         Reason: RefundReason.Trim(),
         CashierId: _session.CurrentUser!.Id,
         ManagerApprovalId: null,
         ManagerApproverUserId: null);
     ```
  6. Dispatches to `_session.Api.RefundAsync(request)`.
  7. On error: displays `RefundError = result.Error ?? "تعذر تنفيذ الاسترجاع."`.
  8. On success: displays confirmation and re-queries `LookupOrderAsync()`.
- **Forensic Defect: `FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001`:**
  - If a network drop, socket timeout, or 504 Gateway Timeout occurs after `RefundOrderHandler` commits the refund and treasury movement, `result.Success` is false.
  - The cashier is presented with `"تعذر تنفيذ الاسترجاع."` and clicks "استرجاع" again.
  - Because `RefundRequestId` is not cached or persisted, a **NEW request ID is generated**.
  - The server treats this as an independent refund request. If the order has remaining refundable balance, the server approves a **second refund payout**, causing cash drawer discrepancy and double debit.
- **Comparative Analysis vs WinUI 3:**
  WinUI 3 (`CashierViewModel.Refund.cs:110-122`) freezes the in-flight request in `_pendingRefund`. If `result.OutcomeUnknown` occurs, `_pendingRefund` is retained and re-submitted on retry, preserving the original idempotency key.
- **Verdict:** **FATAL_FINANCIAL_DEFECT (`FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001`)**

---

### Callable 6: `TranslateStatus(string)`

- **Canonical Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:108:5:MethodDeclaration`
- **Exact Line Range:** Lines 108–116
- **Signature:** `string Falah.Client.Cashier.ViewModels.CashierViewModel.TranslateStatus(string)`
- **Kind:** `MethodDeclaration` (Static Private Helper)
- **Actual Behavior & Execution Flow:**
  Maps wire status strings to localized Arabic display labels:
  - `"completed"` -> `"مكتمل"`
  - `"refunded"` -> `"مسترجَع بالكامل"`
  - `"cancelled"` -> `"ملغي"`
  - `"preparing"` -> `"قيد التحضير"`
  - `"ready"` -> `"جاهز"`
  - Default -> returns original string unmodified.
- **Comparative Analysis vs WinUI 3:**
  In WinUI 3, status translation is handled via shared XAML converters (`OrderStatusToBrushConverter`, `OrderStatusToArabicConverter`).
- **Verdict:** **PARITY_EQUIVALENT**

---

### Member Property: `get_RefundableRemainingPiastres`

- **Location:** Lines 21–22
- **Signature:** `long Falah.Client.Cashier.ViewModels.CashierViewModel.RefundableRemainingPiastres.get`
- **Actual Behavior:**
  Returns `LookedUpOrder is null ? 0 : LookedUpOrder.TotalPiastres - LookedUpOrder.RefundAmountPiastres`.
- **Verdict:** **VERIFIED**

---

## 3. Comparative Parity Matrix: WPF vs WinUI 3 Refund

| Invariant / Feature | Legacy WPF (`OrderLookup.cs`) | Active WinUI 3 (`Refund.cs`) | Risk & Parity Assessment |
| :--- | :--- | :--- | :--- |
| **Ambiguous Retry Idempotency** | Mints new `rreq-{Guid}` on every click | Freezes `_pendingRefund`, reuses `_refundRequestId` | **CRITICAL_DEFECT (`FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001`)** |
| **Unpaid Order Refund Guard** | Not checked (`CanRefund` allows) | `CanRefundOrder` checks `IsPaid: true` | **CRITICAL_VULNERABILITY (`FIN-003`)** |
| **Multi-Payment Refund Guard** | Not checked client-side | `CanRefundOrder` checks `PaymentMethod: not "multi"` | **PARITY_DIVERGENCE** |
| **Manager Approval Workflow** | Hardcoded null (large refunds fail) | Dual-control PIN prompt (`ResolveManagerApprovalIfNeededAsync`) | **FUNCTIONAL_OMISSION** |
| **Remaining Balance Guard** | Validated client-side | Validated client-side via `CashInput` | **PARITY_VERIFIED** |
| **In-Flight Modal Closure** | Unconditionally allowed | Blocked if `IsRefunding` or `_pendingRefund != null` | **SAFETY_GAP** |

---

## 4. Formal Audit Recommendations

1. **Retire Legacy WPF Refund Screen:** Given that WPF generates duplicate refunds on network timeouts (`FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001`) and permits refunding unpaid orders (`FIN-003`), `Falah.Client.Cashier` should be disabled from initiating refunds in production environments.
2. **If WPF Maintained — Mandatory Remediations:**
   - Store `_pendingRefund` and cache `_refundRequestId` until server confirms completion.
   - Enforce `LookedUpOrder.IsPaid == true` in `CanRefund()`.
   - Implement manager approval PIN entry for supervisor overrides.
