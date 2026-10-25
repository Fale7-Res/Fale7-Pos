# src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs

- **Audit Target:** WinUI 3 Cashier POS State & Operation Coordination Controller
- **Audit Date & UTC Timestamp:** 2026-10-25T01:00:00Z
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current HEAD:** `9beff52a6c9c6c4f7244cbd46dc449da91745582`
- **Component Role:** Secondary partial class governing operational busy state, permission checks, financial aggregates notification, tender presentation, initialization lifecycle, and cart clearing dialogs for `Falah.Client.CashierWinUI`.
- **Status:** **COMPLETE SUBSTANTIVE SOURCE REVIEW — 100% CALLABLE COVERAGE (21+ Callables / 49 Members)**

---

## 1. Architectural Architecture & State Coordination Model

### 1.1 Concurrency Lock Architecture (`IsOperationBusy` & `CanEditOperation`)
`CashierViewModel.State.cs` defines the centralized concurrency gate for the entire Cashier POS client:
- **`IsOperationBusy` ([lines 19-21](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L19-L21)):** Aggregates 11 distinct operational flags:
  `IsRefunding || IsProcessingCheckout || IsOpeningShift || IsClosingShift || IsSubmittingExpense || IsSubmittingSafeDrop || IsSubmittingCashMovement || IsSubmittingCashDrawer || IsReprinting || IsConfirmingElectronicPayment || IsCancellingElectronicPayment`.
- **`CanEditOperation` ([lines 22-24](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L22-L24)):** Ensures that neither an active operation is in-flight NOR any pending command/modal state exists (`_pendingRefund`, `_pendingReprint`, `_pendingCheckout`, `_pendingExpense`, `_pendingSafeDrop`, `_pendingCashMovement`, `_pendingCashDrawer`, `_pendingOpenShift`, `_pendingCloseShift`, `_pendingElectronicConfirm`, `_pendingElectronicCancel`).
- **Pervasive Gatekeeping:** All mutations in `CashierViewModel.cs` (`SelectProduct`, `SelectOffer`, `SelectVariant`, `AddToCart`, `IncreaseQuantity`, `DecreaseQuantity`, `RemoveLine`, `AppendNoteText`, `ClearCart`, `SetOrderType`) strictly check `if (!CanEditOperation) return;`. This prevents double-taps, race conditions, or cart modifications while a financial transaction is in flight.

---

## 2. Deep Dive Architectural & Performance Vectors

### 2.1 Critical Fire-and-Forget Exception Vector in `InitializeAsync` (ASYNC-001)
In [`CashierViewModel.State.cs:86-111`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L86-L111):
```csharp
[RelayCommand]
public async Task InitializeAsync()
{
    if (IsInitializing) return;
    IsInitializing = true;
    SessionError = null;
    try
    {
        var shift = await _session.Api.GetCurrentShiftAsync();
        if (!shift.Success && shift.StatusCode != 404) { SessionError = shift.Error; return; }
        _session.ShiftId = shift.Value?.ShiftId;
        _session.ShiftName = shift.Value?.ShiftName;
        OnPropertyChanged(nameof(IsShiftOpen));
        _header.Refresh();
        var settings = await _session.Api.GetSettingsAsync();
        if (settings.Success && settings.Value is not null)
        {
            _header.SetIdentity(settings.Value.RestaurantName);
            if (_session.ShiftId is null)
                OpeningCashText = CashInput.Format(Math.Max(0, settings.Value.DefaultOpeningCashPiastres));
        }
        if (IsShiftOpen) await LoadMenuAsync();
    }
    finally { IsInitializing = false; }
}
```
- **The Core Flaw:** `InitializeAsync` contains `try` and `finally` but **has NO `catch (Exception ex)` block**.
- In [`MainViewModel.cs:125`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/MainViewModel.cs#L125) and [`ShellViewModel.cs:346`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs#L346), `InitializeAsync()` is called via fire-and-forget:
  ```csharp
  _ = vm.InitializeAsync();
  ```
- If `GetCurrentShiftAsync()`, `GetSettingsAsync()`, or `LoadMenuAsync()` throws an unhandled network/HTTP exception (e.g. server down, socket reset, JSON deserialization error), the exception is unobserved. The application will silently fail to initialize, leaving the cashier on an empty or stuck screen with `IsInitializing = false` and no error prompt.

### 2.2 Redundant Domain Tax Engine Execution in `NotifyTotals()` (PERF-001)
In [`CashierViewModel.State.cs:74-84`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L74-L84):
- `NotifyTotals()` raises 10 separate `PropertyChanged` notifications.
- Line 76 notifies `GrandTotalPiastres`, which evaluates:
  `Falah.Domain.Financial.TaxEngine.Calculate(taxable, _taxRule)`
- Line 79 notifies `TaxDisplay`, which immediately evaluates:
  `Falah.Domain.Financial.TaxEngine.Calculate(taxable, _taxRule)` a second time with the exact same inputs!
- When rapid cart operations occur (e.g. barcode scanner adding items or cashier adjusting quantities), domain calculations are executed multiple times sequentially on the UI thread without caching.

---

## 3. Exhaustive Per-Callable Audit

### 3.1 `OnIsProcessingCheckoutChanged(bool value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:9`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L9)
  ```csharp
  partial void OnIsProcessingCheckoutChanged(bool value) => RefreshOperationState();
  ```
- **Behavior:** Refreshes operational state and command execution readiness when checkout processing state toggles.
- **Verdict:** Correct.

---

### 3.2 `OnIsOpeningShiftChanged(bool value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:10`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L10)
  ```csharp
  partial void OnIsOpeningShiftChanged(bool value) => RefreshOperationState();
  ```
- **Behavior:** Refreshes operation state on shift open.
- **Verdict:** Correct.

---

### 3.3 `OnIsClosingShiftChanged(bool value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:11`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L11)
  ```csharp
  partial void OnIsClosingShiftChanged(bool value) => RefreshOperationState();
  ```
- **Behavior:** Refreshes operation state on shift close.
- **Verdict:** Correct.

---

### 3.4 `OnIsSubmittingExpenseChanged(bool value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:12`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L12)
  ```csharp
  partial void OnIsSubmittingExpenseChanged(bool value) => RefreshOperationState();
  ```
- **Behavior:** Refreshes operation state on expense submission.
- **Verdict:** Correct.

---

### 3.5 `OnIsSubmittingSafeDropChanged(bool value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:13`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L13)
  ```csharp
  partial void OnIsSubmittingSafeDropChanged(bool value) => RefreshOperationState();
  ```
- **Behavior:** Refreshes operation state on safe drop submission.
- **Verdict:** Correct.

---

### 3.6 `OnIsSubmittingCashMovementChanged(bool value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:14`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L14)
  ```csharp
  partial void OnIsSubmittingCashMovementChanged(bool value) => RefreshOperationState();
  ```
- **Behavior:** Refreshes operation state on cash in / out.
- **Verdict:** Correct.

---

### 3.7 `OnIsSubmittingCashDrawerChanged(bool value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:15`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L15)
  ```csharp
  partial void OnIsSubmittingCashDrawerChanged(bool value) => RefreshOperationState();
  ```
- **Behavior:** Refreshes operation state on manual drawer kick.
- **Verdict:** Correct.

---

### 3.8 `OnIsReprintingChanged(bool value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:16`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L16)
  ```csharp
  partial void OnIsReprintingChanged(bool value) => RefreshOperationState();
  ```
- **Behavior:** Refreshes operation state on ticket reprint.
- **Verdict:** Correct.

---

### 3.9 `OnIsConfirmingElectronicPaymentChanged(bool value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:17`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L17)
  ```csharp
  partial void OnIsConfirmingElectronicPaymentChanged(bool value) => RefreshOperationState();
  ```
- **Behavior:** Refreshes operation state on electronic payment confirmation.
- **Verdict:** Correct.

---

### 3.10 `OnIsCancellingElectronicPaymentChanged(bool value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:18`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L18)
  ```csharp
  partial void OnIsCancellingElectronicPaymentChanged(bool value) => RefreshOperationState();
  ```
- **Behavior:** Refreshes operation state on electronic payment cancellation.
- **Verdict:** Correct.

---

### 3.11 `IsOperationBusy` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:19-21`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L19-L21)
  ```csharp
  public bool IsOperationBusy => IsRefunding || IsProcessingCheckout || IsOpeningShift || IsClosingShift || IsSubmittingExpense ||
      IsSubmittingSafeDrop || IsSubmittingCashMovement || IsSubmittingCashDrawer || IsReprinting ||
      IsConfirmingElectronicPayment || IsCancellingElectronicPayment;
  ```
- **Behavior:** Master busy flag across all POS operational tasks.
- **Verdict:** Comprehensive aggregate.

---

### 3.12 `CanEditOperation` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:22-24`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L22-L24)
  ```csharp
  public bool CanEditOperation => !IsOperationBusy && _pendingRefund is null && _pendingReprint is null && _pendingCheckout is null && _pendingExpense is null &&
      _pendingSafeDrop is null && _pendingCashMovement is null && _pendingCashDrawer is null &&
      _pendingOpenShift is null && _pendingCloseShift is null && !_pendingElectronicConfirm.HasPending && !_pendingElectronicCancel.HasPending;
  ```
- **Behavior:** Authorizes catalogue, cart, and order mutations.
- **Verdict:** Core concurrency and data integrity guardian.

---

### 3.13 `CanDiscount` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:25`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L25)
  ```csharp
  public bool CanDiscount => _session.HasPermission("orders_discount");
  ```
- **Behavior:** Evaluates discount capability against operator permissions.
- **Verdict:** Correct RBAC gate.

---

### 3.14 `CanViewDrawerBalance` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:26`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L26)
  ```csharp
  public bool CanViewDrawerBalance => _session.HasPermission("treasury_manage") || _session.HasPermission("reports_view");
  ```
- **Behavior:** Gates cash count and drawer balance viewing.
- **Verdict:** Correct.

---

### 3.15 `UsesWallet` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:27`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L27)
  ```csharp
  public bool UsesWallet => IsSplitPaymentEnabled ? SplitMethod1 == "wallet" || SplitMethod2 == "wallet" : PaymentMethod == "wallet";
  ```
- **Behavior:** Detects mobile wallet payment method for QR/phone prompt display.
- **Verdict:** Correct.

---

### 3.16 `IsCashOnDelivery` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:28`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L28)
  ```csharp
  public bool IsCashOnDelivery => IsDelivery && !IsSplitPaymentEnabled && PaymentMethod == "cash" && GrandTotalPiastres > 0;
  ```
- **Behavior:** Identifies COD delivery orders to disable tender entry at checkout time.
- **Verdict:** Correct business logic.

---

### 3.17 `CanEnterTender` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:29`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L29)
  ```csharp
  public bool CanEnterTender => CanEditOperation && !IsCashOnDelivery && !IsSplitPaymentEnabled && PaymentMethod == "cash";
  ```
- **Behavior:** Enables amount paid numeric pad only for direct cash sales.
- **Verdict:** Correct.

---

### 3.18 `TenderLabel` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:30`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L30)
  ```csharp
  public string TenderLabel => IsCashOnDelivery ? "يُحصّل عند التوصيل بالجنيه" : "المبلغ المستلم بالجنيه";
  ```
- **Behavior:** Dynamic tender field header.
- **Verdict:** Correct.

---

### 3.19 `NotifyPaymentPresentation()` (Private Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:31-35`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L31-L35)
  ```csharp
  private void NotifyPaymentPresentation()
  {
      OnPropertyChanged(nameof(UsesWallet)); OnPropertyChanged(nameof(IsCashOnDelivery));
      OnPropertyChanged(nameof(CanEnterTender)); OnPropertyChanged(nameof(TenderLabel));
  }
  ```
- **Behavior:** Batch notifies payment UI bindings.
- **Verdict:** Correct.

---

### 3.20 `OnPaymentMethodChanged(string value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:36`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L36)
  ```csharp
  partial void OnPaymentMethodChanged(string value) => NotifyPaymentPresentation();
  ```
- **Behavior:** Refreshes tender presentation when payment method switches.
- **Verdict:** Correct.

---

### 3.21 `OnSplitMethod1Changed(string value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:37`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L37)
  ```csharp
  partial void OnSplitMethod1Changed(string value) => OnPropertyChanged(nameof(UsesWallet));
  ```
- **Behavior:** Notifies `UsesWallet`.
- **Verdict:** Correct.

---

### 3.22 `OnSplitMethod2Changed(string value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:38`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L38)
  ```csharp
  partial void OnSplitMethod2Changed(string value) => OnPropertyChanged(nameof(UsesWallet));
  ```
- **Behavior:** Notifies `UsesWallet`.
- **Verdict:** Correct.

---

### 3.23 `OnIsSplitPaymentEnabledChanged(bool value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:39`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L39)
  ```csharp
  partial void OnIsSplitPaymentEnabledChanged(bool value) => NotifyPaymentPresentation();
  ```
- **Behavior:** Refreshes tender presentation on split payment toggle.
- **Verdict:** Correct.

---

### 3.24 `CanOpenShift` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:40`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L40)
  ```csharp
  public bool CanOpenShift => _session.HasPermission("shifts_open_own") || _session.HasPermission("shifts_manage");
  ```
- **Behavior:** RBAC permission check for opening a shift.
- **Verdict:** Correct.

---

### 3.25 `CanCloseShift` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:41`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L41)
  ```csharp
  public bool CanCloseShift => _session.HasPermission("shifts_blind_close");
  ```
- **Behavior:** RBAC permission check for closing a shift.
- **Verdict:** Correct.

---

### 3.26 `CanCountCash` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:42`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L42)
  ```csharp
  public bool CanCountCash => _session.HasPermission("cash_count");
  ```
- **Behavior:** RBAC check for drawer count.
- **Verdict:** Correct.

---

### 3.27 `IsDelivery` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:43`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L43)
  ```csharp
  public bool IsDelivery => OrderType == "delivery";
  ```
- **Behavior:** Delivery order predicate.
- **Verdict:** Correct.

---

### 3.28 `IsPickup` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:44`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L44)
  ```csharp
  public bool IsPickup => OrderType == "pickup";
  ```
- **Behavior:** Pickup order predicate.
- **Verdict:** Correct.

---

### 3.29 `OrderTypeLabel` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:45`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L45)
  ```csharp
  public string OrderTypeLabel => OrderType switch { "delivery" => "توصيل", "pickup" => "استلام من المطعم", _ => "تيك أواي" };
  ```
- **Behavior:** Localized label for order type.
- **Verdict:** Correct.

---

### 3.30 `DeliveryFeeDisplay` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:46`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L46)
  ```csharp
  public string DeliveryFeeDisplay => CashInput.Format(IsDelivery ? SelectedDeliveryService?.FeePiastres ?? 0 : 0) + " ج";
  ```
- **Behavior:** Formatted delivery fee string.
- **Verdict:** Correct.

---

### 3.31 `DiscountDisplay` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:47`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L47)
  ```csharp
  public string DiscountDisplay => CashInput.Format(DiscountAmountPiastres) + " ج";
  ```
- **Behavior:** Formatted discount amount string.
- **Verdict:** Correct.

---

### 3.32 `CompDisplay` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:48`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L48)
  ```csharp
  public string CompDisplay => CashInput.Format(CompAmountPiastres) + " ج";
  ```
- **Behavior:** Formatted complimentary deduction string.
- **Verdict:** Correct.

---

### 3.33 `HasTax` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:49`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L49)
  ```csharp
  public bool HasTax => _taxRule is { Enabled: true, RateBasisPoints: > 0 };
  ```
- **Behavior:** Predicate indicating active sales tax.
- **Verdict:** Correct.

---

### 3.34 `TaxLabel` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:50`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L50)
  ```csharp
  public string TaxLabel => _taxRule?.Mode == Falah.Domain.Financial.TaxMode.Inclusive ? "الضريبة (مشمولة)" : "الضريبة";
  ```
- **Behavior:** Distinguishes inclusive vs exclusive tax in invoice breakdown.
- **Verdict:** Correct.

---

### 3.35 `TaxDisplay` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:51-52`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L51-L52)
  ```csharp
  public string TaxDisplay => CashInput.Format(Falah.Domain.Financial.TaxEngine.Calculate(
      Falah.Domain.Common.Money.FromPiastres(Math.Max(0, CartSubtotalPiastres - DiscountAmountPiastres)), _taxRule).Tax.Piastres) + " ج";
  ```
- **Behavior:** Calculates sales tax amount via `TaxEngine`.
- **Defect:** Repeated calculation (Section 2.2).
- **Verdict:** Redundant execution.

---

### 3.36 `CheckoutTitle` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:53`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L53)
  ```csharp
  public string CheckoutTitle => IsElectronicPaymentPending ? "بانتظار تأكيد التحويل" : "تم تسجيل الطلب";
  ```
- **Behavior:** Header title on checkout completion screen.
- **Verdict:** Correct.

---

### 3.37 `CheckoutSubtitle` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:54`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L54)
  ```csharp
  public string CheckoutSubtitle => IsElectronicPaymentPending ? "الطلب مسجل؛ المبلغ لم يُحصّل بعد" : "راجع رقم الطلب والمبلغ المسجل";
  ```
- **Behavior:** Instruction text on checkout completion screen.
- **Verdict:** Correct.

---

### 3.38 `HasPrintWarning` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:59`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L59)
  ```csharp
  public bool HasPrintWarning => LastCheckoutResult is { PrintJobsQueued: false };
  ```
- **Behavior:** Surfaces warning when sale recorded but kitchen tickets failed to queue.
- **Verdict:** Critical operational safeguard.

---

### 3.39 `PrintWarningText` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:61`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L61)
  ```csharp
  public string PrintWarningText => "تم تسجيل الطلب لكن لم تُنشأ تذاكر الطباعة. أعد الطباعة من «البحث عن طلب».";
  ```
- **Behavior:** Explanatory warning banner text.
- **Verdict:** Correct.

---

### 3.40 `CatalogueTitle` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:62`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L62)
  ```csharp
  public string CatalogueTitle => string.IsNullOrWhiteSpace(SearchQuery) ? SelectedCategory?.Name ?? "قائمة الطعام" : "نتائج البحث";
  ```
- **Behavior:** Dynamic catalogue header.
- **Verdict:** Correct.

---

### 3.41 `IsInitializing` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:63`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L63)
  ```csharp
  [ObservableProperty] private bool _isInitializing;
  ```
- **Behavior:** Busy flag during startup and shift re-check.
- **Verdict:** Correct.

---

### 3.42 `SessionError` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:64`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L64)
  ```csharp
  [ObservableProperty] private string? _sessionError;
  ```
- **Behavior:** Error message surfaced if session/shift initialization fails.
- **Verdict:** Correct.

---

### 3.43 `IsClearCartOpen` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:65`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L65)
  ```csharp
  [ObservableProperty] private bool _isClearCartOpen;
  ```
- **Behavior:** Visibility flag for confirmation dialog before clearing cart.
- **Verdict:** Correct.

---

### 3.44 `RefreshOperationState()` (Private Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:67-73`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L67-L73)
  ```csharp
  private void RefreshOperationState()
  {
      OnPropertyChanged(nameof(CanEditOperation)); OnPropertyChanged(nameof(CanEnterTender));
      OnPropertyChanged(nameof(CanConfirmPendingPayment)); OnPropertyChanged(nameof(CanCancelPendingPayment));
      NotifyCheckoutAvailability();
      _header.Refresh();
  }
  ```
- **Behavior:** Synchronizes UI availability with underlying operations.
- **Verdict:** Correct.

---

### 3.45 `NotifyTotals()` (Private Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:74-84`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L74-L84)
  ```csharp
  private void NotifyTotals()
  {
      OnPropertyChanged(nameof(GrandTotalPiastres)); OnPropertyChanged(nameof(GrandTotalDisplay));
      OnPropertyChanged(nameof(DeliveryFeeDisplay)); OnPropertyChanged(nameof(DiscountDisplay));
      OnPropertyChanged(nameof(CompDisplay)); OnPropertyChanged(nameof(ChangeDuePiastres));
      OnPropertyChanged(nameof(HasTax)); OnPropertyChanged(nameof(TaxLabel)); OnPropertyChanged(nameof(TaxDisplay));
      NotifyPaymentPresentation();
      NotifyCheckoutAvailability();
      if (IsCheckoutOpen && (PaymentMethod != "cash" || IsCashOnDelivery) && CanEditOperation)
          AmountPaidText = CashInput.Format(GrandTotalPiastres);
  }
  ```
- **Behavior:** Triggers property changed events for financial breakdowns.
- **Verdict:** Functional but causes repetitive calculations (Section 2.2).

---

### 3.46 `InitializeAsync()` (RelayCommand Async Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:86-111`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L86-L111)
  ```csharp
  [RelayCommand]
  public async Task InitializeAsync()
  ```
- **Behavior:** Initializes shift, restaurant identity, default opening cash, and catalogue.
- **Defect:** Missing `catch (Exception ex)` block; called fire-and-forget in multiple locations.
- **Verdict:** High-risk unhandled exception hazard (ASYNC-001).

---

### 3.47 `RequestClearCart()` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:113`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L113)
  ```csharp
  [RelayCommand] private void RequestClearCart() { if (CanEditOperation && Cart.Count > 0) IsClearCartOpen = true; }
  ```
- **Behavior:** Opens confirmation modal to prevent accidental loss of active order.
- **Verdict:** Correct UX protection.

---

### 3.48 `CancelClearCart()` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:114`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L114)
  ```csharp
  [RelayCommand] private void CancelClearCart() => IsClearCartOpen = false;
  ```
- **Behavior:** Dismisses confirmation modal.
- **Verdict:** Correct.

---

### 3.49 `ConfirmClearCart()` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:115`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L115)
  ```csharp
  [RelayCommand] private void ConfirmClearCart() { if (!CanEditOperation) return; ClearCart(); IsClearCartOpen = false; }
  ```
- **Behavior:** Confirms and executes `ClearCart()`.
- **Verdict:** Correct.

---

## 4. Test Coverage & Verification

- **Current Existing Tests:**
  `tests/Falah.Client.Tests/FinalVerificationTests.cs` tests retry commands when secondary reads fail, but has **0% coverage** for `InitializeAsync` network exception resiliency or `CanEditOperation` interlocking.
- **Required Test Cases:**
  1. `InitializeAsync_NetworkException_SurfacesSessionErrorWithoutCrashing`: Verify graceful degradation on connection outage.
  2. `CanEditOperation_FalseWhenAnyOperationPending_RejectsMutations`: Exhaustive test of all 11 pending operation flags.
  3. `NotifyTotals_UpdatesAmountPaidText_WhenNonCashPaymentActive`: Verify automated tender matching.

---
*Audit certified by UI/UX & Code Maintainability Auditor.*
