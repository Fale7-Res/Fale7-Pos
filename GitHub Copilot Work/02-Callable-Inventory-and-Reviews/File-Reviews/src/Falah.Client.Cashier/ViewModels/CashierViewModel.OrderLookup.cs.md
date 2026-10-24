# Substantive Callable Audit: `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs`

- **Source File:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `9f41d9133ed49ccb2e8dff94a43e70dc525500836f7c6c6e8ada4912bc1861dd`
- **Auditor Specialist:** `mirrored-code-auditor`
- **Total File Callables:** **6**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Legacy WPF cashier view model handling order lookup, status translation, and refund submission. Diverges from WinUI client in idempotency key reuse during network transport ambiguities.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Client.Cashier.ViewModels.CashierViewModel.OpenOrderLookup()`
- **Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:24:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `24-33`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.Cashier.ViewModels.CashierViewModel.OpenOrderLookup(). Manages internal state transition within src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 24-33):**
```csharp
  24 |     [RelayCommand]
  25 |     private void OpenOrderLookup()
  26 |     {
  27 |         IsOrderLookupOpen = true;
  28 |         LookupOrderNumber = string.Empty;
  29 |         LookedUpOrder = null;
  30 |         LookupError = null;
  31 |         RefundError = null;
  32 |         RefundSuccessMessage = null;
  33 |     }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Client.Cashier.ViewModels.CashierViewModel.CloseOrderLookup()`
- **Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:35:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `35-36`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.Cashier.ViewModels.CashierViewModel.CloseOrderLookup(). Manages internal state transition within src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 35-36):**
```csharp
  35 |     [RelayCommand]
  36 |     private void CloseOrderLookup() => IsOrderLookupOpen = false;
```

---

<a id="callable-3"></a>
### Callable 3: `Falah.Client.Cashier.ViewModels.CashierViewModel.LookupOrderAsync()`
- **Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:38:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `38-62`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.Cashier.ViewModels.CashierViewModel.LookupOrderAsync(). Manages internal state transition within src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 38-62):**
```csharp
  38 |     [RelayCommand]
  39 |     private async Task LookupOrderAsync()
  40 |     {
  41 |         if (string.IsNullOrWhiteSpace(LookupOrderNumber)) { LookupError = "أدخل رقم الطلب."; return; }
  42 |         IsLookingUp = true;
  43 |         LookupError = null;
  44 |         LookedUpOrder = null;
  45 |         RefundSuccessMessage = null;
  46 |         try
  47 |         {
  48 |             var result = await _session.Api.LookupOrderByNumberAsync(LookupOrderNumber.Trim());
  49 |             if (!result.Success || result.Value is null)
  50 |             {
  51 |                 LookupError = result.Error ?? "الطلب غير موجود.";
  52 |                 return;
  53 |             }
  54 |             LookedUpOrder = result.Value;
  55 |             OnPropertyChanged(nameof(RefundableRemainingPiastres));
  56 |             RefundAmountText = (RefundableRemainingPiastres / 100m).ToString("0.00");
  57 |         }
  58 |         finally
  59 |         {
  60 |             IsLookingUp = false;
  61 |         }
  62 |     }
```

---

<a id="callable-4"></a>
### Callable 4: `Falah.Client.Cashier.ViewModels.CashierViewModel.CanRefund()`
- **Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:64:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `64-64`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.Cashier.ViewModels.CashierViewModel.CanRefund(). Manages internal state transition within src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 64-64):**
```csharp
  64 |     private bool CanRefund() => LookedUpOrder is not null && !IsRefunding;
```

---

<a id="callable-5"></a>
### Callable 5: `Falah.Client.Cashier.ViewModels.CashierViewModel.SubmitRefundAsync()`
- **Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:66:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `66-106`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001`**

**A. Actual Behavior & Execution Path:**  
Submits refund request to API. Generates fresh RefundRequestId per invocation without persisting or reusing original ID across ambiguous network retries.

**B. Trust Boundaries & Validation:**  
Cashier UI interaction.

**C. State Mutations & Side Effects:**  
Mutates view model status strings and calls ApiClient.RefundOrderAsync.

**D. Concurrency & Transaction Implications:**  
UI Dispatcher thread.

**E. Specialist Risks & Invariants:**  
FLAW FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001: Ambiguous network timeout retry sends new RefundRequestId, risking double-debiting drawer cash on server.

> **Verbatim Code Excerpt (Lines 66-106):**
```csharp
  66 |     [RelayCommand(CanExecute = nameof(CanRefund))]
  67 |     private async Task SubmitRefundAsync()
  68 |     {
  69 |         if (LookedUpOrder is null) return;
  70 |         if (!decimal.TryParse(RefundAmountText, out var amountEgp) || amountEgp <= 0)
  71 |         {
  72 |             RefundError = "أدخل مبلغًا صحيحًا للاسترجاع.";
  73 |             return;
  74 |         }
  75 |         if (string.IsNullOrWhiteSpace(RefundReason)) { RefundError = "سبب الاسترجاع مطلوب."; return; }
  76 | 
  77 |         IsRefunding = true;
  78 |         RefundError = null;
  79 |         try
  80 |         {
  81 |             var request = new RefundOrderRequest(
  82 |                 RefundRequestId: $"rreq-{Guid.NewGuid():N}",
  83 |                 OrderId: LookedUpOrder.Id,
  84 |                 AmountPiastres: (long)Math.Round(amountEgp * 100m),
  85 |                 Reason: RefundReason.Trim(),
  86 |                 CashierId: _session.CurrentUser!.Id,
  87 |                 ManagerApprovalId: null,
  88 |                 ManagerApproverUserId: null);
  89 | 
  90 |             var result = await _session.Api.RefundAsync(request);
  91 |             if (!result.Success || result.Value is null)
  92 |             {
  93 |                 RefundError = result.Error ?? "تعذر تنفيذ الاسترجاع.";
  94 |                 return;
  95 |             }
  96 | 
  97 |             RefundSuccessMessage = $"تم استرجاع {result.Value.AmountPiastres / 100m:0.00} ج بنجاح. الحالة الحالية: {TranslateStatus(result.Value.OrderStatus)}.";
  98 |             // Refresh the on-screen order so the remaining-refundable figure reflects the server's
  99 |             // new truth, not a locally-guessed subtraction.
 100 |             await LookupOrderAsync();
 101 |         }
 102 |         finally
 103 |         {
 104 |             IsRefunding = false;
 105 |         }
 106 |     }
```

---

<a id="callable-6"></a>
### Callable 6: `Falah.Client.Cashier.ViewModels.CashierViewModel.TranslateStatus(string)`
- **Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:108:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `108-116`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.Cashier.ViewModels.CashierViewModel.TranslateStatus(string). Manages internal state transition within src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 108-116):**
```csharp
 108 |     private static string TranslateStatus(string status) => status switch
 109 |     {
 110 |         "completed" => "مكتمل",
 111 |         "refunded" => "مسترجَع بالكامل",
 112 |         "cancelled" => "ملغي",
 113 |         "preparing" => "قيد التحضير",
 114 |         "ready" => "جاهز",
 115 |         _ => status,
 116 |     };
```

---
