# File Review: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:9:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:9:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsProcessingCheckoutChanged(bool)
- **Lines:** [9-9]
- **Behavior:** Partial change handler delegating to RefreshOperationState() when IsProcessingCheckout changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:10:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:10:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsOpeningShiftChanged(bool)
- **Lines:** [10-10]
- **Behavior:** Partial change handler delegating to RefreshOperationState() when IsOpeningShift changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:11:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:11:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsClosingShiftChanged(bool)
- **Lines:** [11-11]
- **Behavior:** Partial change handler delegating to RefreshOperationState() when IsClosingShift changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:12:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:12:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsSubmittingExpenseChanged(bool)
- **Lines:** [12-12]
- **Behavior:** Partial change handler delegating to RefreshOperationState() when IsSubmittingExpense changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:13:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:13:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsSubmittingSafeDropChanged(bool)
- **Lines:** [13-13]
- **Behavior:** Partial change handler delegating to RefreshOperationState() when IsSubmittingSafeDrop changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:14:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:14:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsSubmittingCashMovementChanged(bool)
- **Lines:** [14-14]
- **Behavior:** Partial change handler delegating to RefreshOperationState() when IsSubmittingCashMovement changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:15:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:15:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsSubmittingCashDrawerChanged(bool)
- **Lines:** [15-15]
- **Behavior:** Partial change handler delegating to RefreshOperationState() when IsSubmittingCashDrawer changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:16:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:16:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsReprintingChanged(bool)
- **Lines:** [16-16]
- **Behavior:** Partial change handler delegating to RefreshOperationState() when IsReprinting changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:17:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:17:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsConfirmingElectronicPaymentChanged(bool)
- **Lines:** [17-17]
- **Behavior:** Partial change handler delegating to RefreshOperationState() when IsConfirmingElectronicPayment changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:18:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:18:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsCancellingElectronicPaymentChanged(bool)
- **Lines:** [18-18]
- **Behavior:** Partial change handler delegating to RefreshOperationState() when IsCancellingElectronicPayment changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:31:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:31:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.NotifyPaymentPresentation()
- **Lines:** [31-35]
- **Behavior:** Helper method raising PropertyChanged for tender presentation properties: UsesWallet, IsCashOnDelivery, CanEnterTender, and TenderLabel.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:36:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:36:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnPaymentMethodChanged(string)
- **Lines:** [36-36]
- **Behavior:** Partial change handler invoking NotifyPaymentPresentation() whenever the active payment method changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:37:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:37:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnSplitMethod1Changed(string)
- **Lines:** [37-37]
- **Behavior:** Partial change handler raising PropertyChanged for UsesWallet when the first split payment method changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:38:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:38:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnSplitMethod2Changed(string)
- **Lines:** [38-38]
- **Behavior:** Partial change handler raising PropertyChanged for UsesWallet when the second split payment method changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:39:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:39:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsSplitPaymentEnabledChanged(bool)
- **Lines:** [39-39]
- **Behavior:** Partial change handler calling NotifyPaymentPresentation() whenever split payment mode is toggled.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:67:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:67:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.RefreshOperationState()
- **Lines:** [67-73]
- **Behavior:** Re-evaluates operational lock states across the cashier view: notifies CanEditOperation, CanEnterTender, CanConfirmPendingPayment, CanCancelPendingPayment, invokes NotifyCheckoutAvailability(), and refreshes the header view via _header.Refresh().
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:74:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:74:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.NotifyTotals()
- **Lines:** [74-84]
- **Behavior:** Recalculates and notifies UI of updated order financials: GrandTotalPiastres, GrandTotalDisplay, DeliveryFeeDisplay, DiscountDisplay, CompDisplay, ChangeDuePiastres, HasTax, TaxLabel, and TaxDisplay. Synchronizes AmountPaidText with GrandTotalPiastres for non-cash or cash-on-delivery payments.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:86:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:86:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.InitializeAsync()
- **Lines:** [86-111]
- **Behavior:** RelayCommand initializing the cashier workstation state on startup: queries active shift from _session.Api.GetCurrentShiftAsync(), stores shift ID and name, queries restaurant settings from _session.Api.GetSettingsAsync() to update restaurant identity in header and default opening float, and loads menu catalogue via LoadMenuAsync() if a shift is active.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:113:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:113:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.RequestClearCart()
- **Lines:** [113-113]
- **Behavior:** RelayCommand opening the clear cart confirmation dialog (IsClearCartOpen = true) when CanEditOperation is true and cart is not empty.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:114:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:114:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CancelClearCart()
- **Lines:** [114-114]
- **Behavior:** RelayCommand dismissing the clear cart confirmation dialog (IsClearCartOpen = false).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:115:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:115:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ConfirmClearCart()
- **Lines:** [115-115]
- **Behavior:** RelayCommand confirming cart reset; invokes ClearCart() and sets IsClearCartOpen = false when CanEditOperation is true.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Candidate Findings & Observations
- **Tax Calculation Ignores Comp/Hospitality Reductions (lines 51-52):**
  In `TaxDisplay`:
  ```csharp
  public string TaxDisplay => CashInput.Format(Falah.Domain.Financial.TaxEngine.Calculate(
      Falah.Domain.Common.Money.FromPiastres(Math.Max(0, CartSubtotalPiastres - DiscountAmountPiastres)), _taxRule).Tax.Piastres) + " ج";
  ```
  While `DiscountAmountPiastres` is deducted from `CartSubtotalPiastres` prior to calculating tax, `CompAmountPiastres` is omitted from the taxable base subtraction. For orders with comps (e.g. hospitality, complaints, staff meals), the displayed tax does not reflect the comp deduction even though the grand total is reduced.
