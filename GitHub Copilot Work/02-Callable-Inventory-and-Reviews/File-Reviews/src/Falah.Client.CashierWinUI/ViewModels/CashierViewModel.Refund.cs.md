# Substantive Callable Audit: `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`

- **Source File:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `bc399716e95c2a9b42e3c5db4ad92d6e3fe9879f2b042fa539ec6298d6adea7a`
- **Auditor Specialist:** `mirrored-code-auditor`
- **Total File Callables:** **12**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

`CashierViewModel.Refund.cs` manages order refund interactions on the WinUI 3 touch till. It formats refund amounts, collects reason codes and manager PINs, and submits `RefundOrderRequest` commands to the backend API.

### Architectural Parity Analysis: WinUI 3 vs Legacy WPF (`FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001`)
In `SubmitRefundAsync` and `CompleteRefundAsync`:
WinUI 3 stores in-flight refund requests in `_pendingRefund`. When an API call returns an ambiguous outcome (`result.OutcomeUnknown`), WinUI retains the pending request and re-submits the exact same `_pendingRefund` with its original `RefundRequestId` upon operator retry.
In contrast, legacy WPF (`Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:82`) generates a fresh `RefundRequestId = Guid.NewGuid()` on every click, destroying idempotency and risking duplicate cash deductions on server timeouts.

*(Note: Prior draft incorrectly referenced SEM-MIRROR-065, which is an attendance correction validation comparison. Correct architectural comparison is refund idempotency parity.)*

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

### Key Callable: `SubmitRefundAsync()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:87:109:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `87-109`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`
- **A. Actual Behavior & Execution Path:**
  Validates that order lookup exists and is not currently refunding. If `_pendingRefund` is null, parses amount, checks against `OrderLookupResult.TotalPiastres - RefundAmountPiastres`, requires non-empty reason, prompts manager approval if required, and calls `CompleteRefundAsync(request, false)`. If `_pendingRefund` exists from a previous ambiguous timeout, calls `CompleteRefundAsync(_pendingRefund, true)` preserving the original request ID.
- **B. Trust Boundaries & Validation:** User input from touch UI; validates amount bounds and non-empty reason string.
- **C. State Mutations & Side Effects:** Sets `IsRefunding = true`, resets `RefundError`, mutates UI state.
- **D. Concurrency & Transaction Implications:** Executes on UI dispatcher thread; guarded by `IsRefunding` re-entrance flag.
- **E. Specialist Risks & Invariants:** None; safely preserves request identity across retries.

```csharp
    [RelayCommand] private async Task SubmitRefundAsync()
    {
        if (IsRefunding || OrderLookupResult is null) return;
        IsRefunding = true; RefundError = null;
        try
        {
            if (_pendingRefund is null)
            {
                if (!CashInput.TryParse(RefundAmountText, out var amount) || amount <= 0 ||
                    amount > OrderLookupResult.TotalPiastres - OrderLookupResult.RefundAmountPiastres)
                { RefundError = "أدخل مبلغاً ضمن الرصيد المتاح للاسترداد."; return; }
                if (string.IsNullOrWhiteSpace(RefundReason)) { RefundError = "أدخل سبب الاسترداد."; return; }
                var (approvalId, approver, ok) = await ResolveManagerApprovalIfNeededAsync(RefundNeedsApproval,
                    RefundManagerPin, "orders_refund", RefundReason, e => RefundError = e);
                if (!ok) return;
                var request = new RefundOrderRequest(_refundRequestId, OrderLookupResult.Id, amount, RefundReason,
                    _session.CurrentUser!.Id, approvalId, approver);
                await CompleteRefundAsync(request, false);
            }
            else await CompleteRefundAsync(_pendingRefund, true);
        }
        finally { IsRefunding = false; }
    }
```

---

### Key Callable: `CompleteRefundAsync(RefundOrderRequest request, bool wasUnresolved)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:110:122:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `110-122`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`
- **A. Actual Behavior & Execution Path:**
  Sets `_pendingRefund = request`. Invokes `_session.Api.RefundAsync(request)`. If the request succeeded or if it failed decisively without network ambiguity, clears `_pendingRefund`. If unknown outcome, retains `_pendingRefund` for safe retry. On success, generates a fresh `_refundRequestId` for the NEXT refund operation, updates order lookup state, and sets `ReprintStatusMessage`.
- **B. Trust Boundaries & Validation:** API response handling.
- **C. State Mutations & Side Effects:** Mutates `_pendingRefund`, `OrderLookupResult`, `ReprintStatusMessage`, `IsRefundOpen`.
- **D. Concurrency & Transaction Implications:** Asynchronous HTTP I/O.
- **E. Specialist Risks & Invariants:** Provides resilient idempotency protection missing in legacy WPF.

```csharp
    private async Task CompleteRefundAsync(RefundOrderRequest request, bool wasUnresolved)
    {
        _pendingRefund = request; RefreshOperationState();
        var result = await _session.Api.RefundAsync(request);
        if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) _pendingRefund = null;
        RefreshOperationState();
        if (!result.Success || result.Value is null) { RefundError = result.Error; return; }
        _refundRequestId = Guid.NewGuid().ToString("N");
        OrderLookupResult = OrderLookupResult! with { Status = result.Value.OrderStatus,
            RefundAmountPiastres = OrderLookupResult.TotalPiastres - result.Value.RemainingRefundablePiastres };
        ReprintStatusMessage = "تم تسجيل استرداد " + CashInput.Format(result.Value.AmountPiastres) + " ج";
        IsRefundOpen = false; RefundManagerPin = "";
    }
```
