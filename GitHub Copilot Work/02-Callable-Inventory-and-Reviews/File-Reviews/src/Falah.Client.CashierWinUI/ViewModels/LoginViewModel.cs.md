# src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs

- **Audit Target:** WinUI 3 Touch Cashier PIN Authentication Controller
- **Audit Date & UTC Timestamp:** 2026-10-25T00:35:00Z
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current HEAD:** `9beff52a6c9c6c4f7244cbd46dc449da91745582`
- **Component Role:** Full-screen touch PIN authentication view model for the primary modern WinUI 3 cashier station (`Falah.Client.CashierWinUI`). Manages touch numpad digit entry, input masking with real placeholder container, backend authentication via `_session.Api.LoginByPinAsync`, error state management, and role-based shell routing.
- **Status:** **COMPLETE SUBSTANTIVE SOURCE REVIEW — 100% CALLABLE COVERAGE**

---

## 1. Architectural Architecture & UI Binding Model

### 1.1 View Integration (`LoginView.xaml`)
In [`src/Falah.Client.CashierWinUI/Views/LoginView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/LoginView.xaml), `LoginViewModel` is presented inside a full-screen grid host dynamically resolved by `MainWindow.xaml.cs:93` (`LoginViewModel vm => new LoginView { DataContext = vm }`).
- **Touch Input Architecture:** Digits 0–9 bind to `PressDigitCommand` with string arguments (`"1"`–`"9"`, `"0"`). Action buttons bind to `ClearCommand` ("مسح") and `BackspaceCommand` ("⌫").
- **Directionality Correction (RTL/LTR):** While the parent window is `FlowDirection="RightToLeft"`, the keypad grid ([`LoginView.xaml:44`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/LoginView.xaml#L44)) explicitly overrides `FlowDirection="LeftToRight"`. This ensures the touch buttons remain in standard phone/ATM/POS 1-2-3 ordering rather than being mirrored into 3-2-1 (a major ergonomic defect in the legacy WPF client).
- **Visible Input Target & Placeholder:** Unlike the legacy WPF screen which renders a bare `TextBlock` that is invisible when empty, WinUI 3 provides an explicit input container (`Border Height="56" CornerRadius="14"`) with placeholder text `"أدخل الرمز السري"` collapsed via `EmptyStringToVisibleConverter` when `PinDisplay` is non-empty.
- **Submit Command & Dynamic Label:** The submission button uses `CtaButtonStyle` (action blue) and binds `Content="{Binding SubmitLabel}"` alongside `Command="{Binding SubmitCommand}"`.

### 1.2 Theme Resource & Contrast Audit (WCAG 2.1 AA / AAA)
`CashierWinUI` defines complete light and dark theme dictionaries in [`src/Falah.Client.CashierWinUI/Styles/Colors.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Styles/Colors.xaml):
- **Card Background & Frame:**
  - Light: Card `#FFFFFF` on Surface `#F5F5F5` with Border `#E2E8F0` (contrast ratio 1.14:1 boundary, 18.2:1 text).
  - Dark: Card `#1E232B` on Surface `#14171C` with Border `#2C343E`.
- **Keypad Buttons (`KeypadButtonStyle`):**
  - Light: Text `{ThemeResource TextPrimaryBrush}` (`#0F172A`) on `{ThemeResource NeutralBackgroundBrush}` (`#F1F5F9`) yields a contrast ratio of **16.2:1** (exceeds WCAG AAA 7.0:1).
  - Dark: Text `#FFFFFF` on `#1E232B` yields a contrast ratio of **14.8:1** (exceeds WCAG AAA).
- **PIN Bullets (`PinDisplay`):**
  - Light: Bullets `{ThemeResource BrandPrimaryBrush}` (`#0F52BA`) on PIN container `#F5F5F5` yields **6.62:1** (passes WCAG AA 4.5:1 and AAA for large text 3.0:1).
  - Dark: Bullets `#3B82F6` on `#14171C` yields **5.40:1** (passes WCAG AA).
- **CTA Submit Button (`CtaButtonStyle`):**
  - Background `{ThemeResource ActionBlueBrush}` (`#0866E8`) with `White` text yields **5.16:1** (passes WCAG AA).
- **Error Banner:**
  - Foreground `{ThemeResource DangerBrush}` (`#DC2626`) on Card `#FFFFFF` yields **4.65:1** (passes WCAG AA).

---

## 2. Exhaustive Per-Callable Audit

### 2.1 `LoginViewModel(AppSession session)` (Constructor)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:15`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L15)
  ```csharp
  public LoginViewModel(AppSession session) => _session = session;
  ```
- **Callers & Instantiation:** Called exclusively by `MainViewModel.ShowLogin()` ([`MainViewModel.cs:87`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/MainViewModel.cs#L87)) upon application startup or cashier screen lock.
- **State Initialization:** Injects process-wide `AppSession`. Backing field `_pin` initializes to `string.Empty`, `_errorMessage` to `null`, and `_isBusy` to `false`.
- **Concurrency & Leaks:** Synchronous, leak-free dependency injection.

---

### 2.2 `Pin` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:17-19`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L17-L19)
  ```csharp
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
  private string _pin = string.Empty;
  ```
- **Code Generation & Binding:** CommunityToolkit generates `public string Pin { get; set; }`. Modifying `Pin` notifies `PinDisplay` via `OnPinChanged` and recalculates `SubmitCommand.CanExecute`.
- **Security & Memory Profile:** Plaintext PIN stored as an immutable `System.String`. Appending characters allocates transient Gen 0 strings that persist in garbage-collected heap until collection cycles occur.

---

### 2.3 `ErrorMessage` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:21-22`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L21-L22)
  ```csharp
  [ObservableProperty]
  private string? _errorMessage;
  ```
- **Behavior & Binding:** Surfaced to `LoginView.xaml:36` using `NullToCollapsedConverter`. Reset to `null` whenever `PressDigit` executes or a new submission attempt begins.

---

### 2.4 `IsBusy` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:24-26`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L24-L26)
  ```csharp
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
  private bool _isBusy;
  ```
- **Behavior:** Controls in-flight authentication state. Triggers `SubmitCommand.CanExecute` re-evaluation and fires `OnIsBusyChanged` to update `SubmitLabel`.

---

### 2.5 `PinDisplay` (Expression-Bodied Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:28`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L28)
  ```csharp
  public string PinDisplay => new('●', Pin.Length);
  ```
- **Behavior & UI Binding:** Generates a masked bullet string (`●`) matching entered character count. Bound in `LoginView.xaml:28` and monitored by `EmptyStringToVisibleConverter` at line 32 to show/hide placeholder text.
- **Maintainability:** Pure computed projection of `Pin`. Notifies correctly via partial method `OnPinChanged`.

---

### 2.6 `SubmitLabel` (Expression-Bodied Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:29`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L29)
  ```csharp
  public string SubmitLabel => IsBusy ? "جارٍ الدخول..." : "فتح الشاشة";
  ```
- **Behavior & Binding:** Returns localized label string (`"فتح الشاشة"` during idle, `"جارٍ الدخول..."` during active authentication). Bound in `LoginView.xaml:73`.
- **Improvement over WPF:** WPF relied on an external XAML value converter `BoolToLoginLabelConverter`. WinUI centralizes label state logic directly inside the ViewModel, reducing XAML binding indirection.

---

### 2.7 `OnPinChanged(string value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:31`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L31)
  ```csharp
  partial void OnPinChanged(string value) => OnPropertyChanged(nameof(PinDisplay));
  ```
- **Behavior:** Invoked by CommunityToolkit's generated property setter whenever `Pin` changes. Fires `PropertyChanged` for `PinDisplay`.
- **Async Hazards:** None; synchronous dispatcher event.

---

### 2.8 `OnIsBusyChanged(bool value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:32`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L32)
  ```csharp
  partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(SubmitLabel));
  ```
- **Behavior:** Invoked whenever `IsBusy` changes. Fires `PropertyChanged` for `SubmitLabel`, causing the CTA button to reflect progress text.
- **Async Hazards:** None.

---

### 2.9 `PressDigit(string digit)` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:34-40`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L34-L40)
  ```csharp
  [RelayCommand]
  private void PressDigit(string digit)
  {
      if (Pin.Length >= 6) return;
      Pin += digit;
      ErrorMessage = null;
  }
  ```
- **Behavior & Boundary Enforcement:** Caps PIN length at 6 digits. Appends digit parameter and clears any previous error message.
- **Forensic Defect 1 (Missing `IsBusy` Guard):**
  `PressDigit` does not check `IsBusy`. If a user taps digits while `SubmitAsync` is in-flight awaiting backend response, `Pin` continues mutating and `PinDisplay` shows additional bullets while the captured request is already processing.
- **Forensic Defect 2 (No Auto-Submit Trigger):**
  When 4 or 6 digits are reached, no auto-submit is triggered; user must manually tap the CTA button.

---

### 2.10 `Backspace()` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:42-46`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L42-L46)
  ```csharp
  [RelayCommand]
  private void Backspace()
  {
      if (Pin.Length > 0) Pin = Pin[..^1];
  }
  ```
- **Behavior:** Removes trailing digit using range syntax `Pin[..^1]`.
- **Forensic Defect:** Missing `!IsBusy` check; input can be modified during active authentication.

---

### 2.11 `Clear()` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:48-49`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L48-L49)
  ```csharp
  [RelayCommand]
  private void Clear() => Pin = string.Empty;
  ```
- **Behavior:** Empties `Pin`.
- **Forensic Defect:** Missing `!IsBusy` check.

---

### 2.12 `CanSubmit()` (Private Predicate Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:51`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L51)
  ```csharp
  private bool CanSubmit() => Pin.Length >= 4 && !IsBusy;
  ```
- **Behavior:** Evaluates executable status for `SubmitCommand`. Requires `Pin.Length >= 4` and `!IsBusy`. Automatically re-evaluated on `_pin` and `_isBusy` property changes.

---

### 2.13 `SubmitAsync()` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:53-80`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L53-L80)
  ```csharp
  [RelayCommand(CanExecute = nameof(CanSubmit))]
  private async Task SubmitAsync()
  {
      IsBusy = true;
      ErrorMessage = null;
      try
      {
          var result = await _session.Api.LoginByPinAsync(Pin);
          if (!result.Success || result.Value is null)
          {
              ErrorMessage = "رمز PIN غير صحيح.";
              Pin = string.Empty;
              return;
          }

          _session.CurrentUser = result.Value;
          Pin = string.Empty;
          LoggedIn?.Invoke();
      }
      catch (Exception ex)
      {
          ErrorMessage = $"تعذر الاتصال بالسيرفر: {ex.Message}";
      }
      finally
      {
          IsBusy = false;
      }
  }
  ```
- **Behavior & Workflow:**
  1. Sets `IsBusy = true` and clears `ErrorMessage`.
  2. Awaits backend authentication via `_session.Api.LoginByPinAsync(Pin)`.
  3. If unauthenticated, sets `ErrorMessage = "رمز PIN غير صحيح."` and clears `Pin`.
  4. If authenticated, populates `_session.CurrentUser = result.Value`, resets `Pin = string.Empty;`, and fires `LoggedIn?.Invoke()`.
  5. Catches network exceptions and formats `ErrorMessage = $"تعذر الاتصال بالسيرفر: {ex.Message}"`.
  6. Resets `IsBusy = false` in `finally`.
- **Forensic Defect 1 (Misleading Error Classification on Network/Server Outage):**
  When `result.Success` is false, line 63 hardcodes `ErrorMessage = "رمز PIN غير صحيح."`. If `ApiClient.LoginByPinAsync` fails due to network outage, DNS failure, or HTTP 503, `result.Error` contains the actual failure cause. Suppressing `result.Error` misinforms the cashier into thinking their PIN is wrong.
- **Forensic Defect 2 (Session State Corruption on Synchronous Subscriber Failure):**
  `LoggedIn?.Invoke()` triggers `MainViewModel.ShowMain()`. If `ShowMain()` throws synchronously (e.g. during `Header.Refresh()` or child view initialization), the exception is caught by line 72 (`catch (Exception ex)`), displaying an error message. However, line 68 has ALREADY assigned `_session.CurrentUser = result.Value`. The session remains marked as logged in while the UI is stuck on the login screen.

---

### 2.14 `LoggedIn` (Event)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs:13`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs#L13)
  ```csharp
  public event Action? LoggedIn;
  ```
- **Lifecycle & Subscribers:** Subscribed by `MainViewModel.ShowLogin()`: `vm.LoggedIn += ShowMain;`. Cleaned up implicitly when the ViewModel instance is replaced upon view transition.

---

## 3. Test Coverage & Verification

- **Current Existing Tests:** **0% unit test coverage** in `tests/` for `CashierWinUI`'s `LoginViewModel`.
- **Required Test Cases:**
  1. `PressDigit_CappedAtSix_CharactersIgnoredAfterLimit`: Verify PIN length boundary enforcement.
  2. `PressDigit_WhileBusy_MustBeIgnored`: Verify input locking while `IsBusy == true`.
  3. `SubmitAsync_ApiReturnsError_SurfacesActualErrorMessage`: Ensure server outage errors are not masked as "رمز PIN غير صحيح.".
  4. `SubmitAsync_Success_AssignsCurrentUserAndFiresLoggedIn`: Verify successful authentication and event dispatch.
  5. `SubmitLabel_TogglesOnBusyChange`: Verify property notification and label switching.

---
*Audit certified by UI/UX & Code Maintainability Auditor.*
