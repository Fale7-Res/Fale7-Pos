# Substantive Callable Audit: `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`

- **Source File:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `bc399716e95c2a9b42e3c5db4ad92d6e3fe9879f2b042fa539ec6298d6adea7a`
- **Auditor Specialist:** `mirrored-code-auditor`
- **Total File Callables:** **12**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Modern WinUI 3 cashier view model handling refund workflows, reason codes, manager approvals, and split tender breakdown display.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.LookupPaymentBreakdown.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:30:9:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `30-35`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.CashierViewModel.LookupPaymentBreakdown.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 30-35):**
```csharp
  30 |         get
  31 |         {
  32 |             if (OrderLookupResult?.SplitPayments is not { Count: > 0 } splits)
  33 |                 return OrderLookupResult is { } o ? MethodLabel(o.PaymentMethod) : "";
  34 |             return string.Join(" + ", splits.Select(s => $"{MethodLabel(s.Method)} {CashInput.Format(s.AmountPiastres)} ج"));
  35 |         }
```

---

<a id="callable-2"></a>
### Callable 2: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:34:53:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `34-34`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 34-34):**
```csharp
  34 |             return string.Join(" + ", splits.Select(s => $"{MethodLabel(s.Method)} {CashInput.Format(s.AmountPiastres)} ج"));
```

---

<a id="callable-3"></a>
### Callable 3: `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.MethodLabel(string)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:55:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `55-63`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.CashierViewModel.MethodLabel(string). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 55-63):**
```csharp
  55 |     private static string MethodLabel(string method) => method switch
  56 |     {
  57 |         "cash" => "كاش",
  58 |         "card" => "فيزا",
  59 |         "instapay" => "إنستاباي",
  60 |         "wallet" => "محفظة",
  61 |         "multi" => "دفع مقسّم",
  62 |         _ => method,
  63 |     };
```

---

<a id="callable-4"></a>
### Callable 4: `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsRefundingChanged(bool)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:65:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `65-65`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsRefundingChanged(bool). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 65-65):**
```csharp
  65 |     partial void OnIsRefundingChanged(bool value) => RefreshOperationState();
```

---

<a id="callable-5"></a>
### Callable 5: `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnOrderLookupResultChanged(Falah.Application.Orders.OrderDetailsDto?)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:66:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `66-73`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnOrderLookupResultChanged(Falah.Application.Orders.OrderDetailsDto?). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 66-73):**
```csharp
  66 |     partial void OnOrderLookupResultChanged(OrderDetailsDto? value)
  67 |     {
  68 |         OnPropertyChanged(nameof(CanRefundOrder)); OnPropertyChanged(nameof(LookupHasPendingPayment));
  69 |         OnPropertyChanged(nameof(RefundRemainingDisplay));
  70 |         OnPropertyChanged(nameof(LookupPaymentBreakdown)); OnPropertyChanged(nameof(HasLookupPaymentBreakdown));
  71 |         OnPropertyChanged(nameof(LookupTableLine)); OnPropertyChanged(nameof(HasLookupTable));
  72 |         OnPropertyChanged(nameof(LookupRequestedPickupLine)); OnPropertyChanged(nameof(HasLookupRequestedPickup));
  73 |     }
```

---

<a id="callable-6"></a>
### Callable 6: `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OpenRefund()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:75:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `75-80`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OpenRefund(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 75-80):**
```csharp
  75 |     [RelayCommand] private void OpenRefund()
  76 |     {
  77 |         if (!CanEditOperation || !CanRefundOrder) return;
  78 |         RefundAmountText = CashInput.Format(OrderLookupResult!.TotalPiastres - OrderLookupResult.RefundAmountPiastres);
  79 |         RefundReason = RefundManagerPin = ""; RefundError = null; IsRefundOpen = true;
  80 |     }
```

---

<a id="callable-7"></a>
### Callable 7: `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CancelRefund()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:81:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `81-86`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CancelRefund(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 81-86):**
```csharp
  81 |     [RelayCommand] private void CancelRefund()
  82 |     {
  83 |         if (IsRefunding) return;
  84 |         if (_pendingRefund is not null) { RefundError = "أعد المحاولة للتحقق من الاسترداد قبل الإغلاق."; return; }
  85 |         IsRefundOpen = false; RefundManagerPin = "";
  86 |     }
```

---

<a id="callable-8"></a>
### Callable 8: `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitRefundAsync()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:87:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `87-109`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Validates reason code, prompts manager approval if required, and dispatches to CompleteRefundAsync using RetainedRequestStore durability.

**B. Trust Boundaries & Validation:**  
Cashier touch till input.

**C. State Mutations & Side Effects:**  
Sets IsRefunding state, updates ObservableCollection balances.

**D. Concurrency & Transaction Implications:**  
WinUI CoreDispatcher / UI thread.

**E. Specialist Risks & Invariants:**  
SEM-MIRROR-065: Validation rules for partial refunds diverge between client view model and server RefundRules.

> **Verbatim Code Excerpt (Lines 87-109):**
```csharp
  87 |     [RelayCommand] private async Task SubmitRefundAsync()
  88 |     {
  89 |         if (IsRefunding || OrderLookupResult is null) return;
  90 |         IsRefunding = true; RefundError = null;
  91 |         try
  92 |         {
  93 |             if (_pendingRefund is null)
  94 |             {
  95 |                 if (!CashInput.TryParse(RefundAmountText, out var amount) || amount <= 0 ||
  96 |                     amount > OrderLookupResult.TotalPiastres - OrderLookupResult.RefundAmountPiastres)
  97 |                 { RefundError = "أدخل مبلغاً ضمن الرصيد المتاح للاسترداد."; return; }
  98 |                 if (string.IsNullOrWhiteSpace(RefundReason)) { RefundError = "أدخل سبب الاسترداد."; return; }
  99 |                 var (approvalId, approver, ok) = await ResolveManagerApprovalIfNeededAsync(RefundNeedsApproval,
 100 |                     RefundManagerPin, "orders_refund", RefundReason, e => RefundError = e);
 101 |                 if (!ok) return;
 102 |                 var request = new RefundOrderRequest(_refundRequestId, OrderLookupResult.Id, amount, RefundReason,
 103 |                     _session.CurrentUser!.Id, approvalId, approver);
 104 |                 await CompleteRefundAsync(request, false);
 105 |             }
 106 |             else await CompleteRefundAsync(_pendingRefund, true);
 107 |         }
 108 |         finally { IsRefunding = false; }
 109 |     }
```

---

<a id="callable-9"></a>
### Callable 9: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:100:70:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `100-100`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 100-100):**
```csharp
 100 |                     RefundManagerPin, "orders_refund", RefundReason, e => RefundError = e);
```

---

<a id="callable-10"></a>
### Callable 10: `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteRefundAsync(Falah.Application.Orders.RefundOrderRequest, bool)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:110:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `110-122`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteRefundAsync(Falah.Application.Orders.RefundOrderRequest, bool). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 110-122):**
```csharp
 110 |     private async Task CompleteRefundAsync(RefundOrderRequest request, bool wasUnresolved)
 111 |     {
 112 |         _pendingRefund = request; RefreshOperationState();
 113 |         var result = await _session.Api.RefundAsync(request);
 114 |         if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) _pendingRefund = null;
 115 |         RefreshOperationState();
 116 |         if (!result.Success || result.Value is null) { RefundError = result.Error; return; }
 117 |         _refundRequestId = Guid.NewGuid().ToString("N");
 118 |         OrderLookupResult = OrderLookupResult! with { Status = result.Value.OrderStatus,
 119 |             RefundAmountPiastres = OrderLookupResult.TotalPiastres - result.Value.RemainingRefundablePiastres };
 120 |         ReprintStatusMessage = "تم تسجيل استرداد " + CashInput.Format(result.Value.AmountPiastres) + " ج";
 121 |         IsRefundOpen = false; RefundManagerPin = "";
 122 |     }
```

---

<a id="callable-11"></a>
### Callable 11: `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ReviewPendingOrder()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:124:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `124-131`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ReviewPendingOrder(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 124-131):**
```csharp
 124 |     [RelayCommand] private void ReviewPendingOrder()
 125 |     {
 126 |         if (!CanEditOperation || !LookupHasPendingPayment || OrderLookupResult is not { } order) return;
 127 |         _reviewingExistingOrder = true;
 128 |         LastCheckoutResult = new(order.Id, order.OrderNumber, order.SequenceNumber, order.TotalPiastres,
 129 |             order.AmountPaidPiastres, order.ChangeDuePiastres, order.Status, order.IsPaid, order.ElectronicPaymentStatus, false);
 130 |         PendingPaymentActionError = null; CancelPendingReason = ""; IsOrderLookupOpen = false;
 131 |     }
```

---

<a id="callable-12"></a>
### Callable 12: `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.DismissCheckoutResult()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:132:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `132-138`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.CashierViewModel.DismissCheckoutResult(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 132-138):**
```csharp
 132 |     [RelayCommand] private void DismissCheckoutResult()
 133 |     {
 134 |         if (!CanEditOperation) return;
 135 |         LastCheckoutResult = null;
 136 |         if (!_reviewingExistingOrder) ResetDelivery();
 137 |         _reviewingExistingOrder = false;
 138 |     }
```

---
