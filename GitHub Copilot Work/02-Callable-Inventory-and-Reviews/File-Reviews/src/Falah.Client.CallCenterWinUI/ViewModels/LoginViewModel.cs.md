# src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs

- **Audit Target:** WinUI 3 Call Center PIN Authentication Controller
- **Audit Date & UTC Timestamp:** 2026-10-25T00:40:00Z
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current HEAD:** `9beff52a6c9c6c4f7244cbd46dc449da91745582`
- **Component Role:** Full-screen touch/mouse PIN authentication view model for the remote Call Center workstation (`Falah.Client.CallCenterWinUI`). Manages operator PIN entry, input masking, backend authentication via `_session.Api.LoginByPinAsync`, error handling, and transition to the operator order desk.
- **Status:** **COMPLETE SUBSTANTIVE SOURCE REVIEW — 100% CALLABLE COVERAGE**

---

## 1. Architectural Architecture & UI Binding Model

### 1.1 View Integration (`LoginView.xaml`)
In [`src/Falah.Client.CallCenterWinUI/Views/LoginView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Views/LoginView.xaml), `LoginViewModel` is presented inside a centralized card dialog resolved in `MainWindow.xaml.cs:66` (`LoginViewModel vm => new LoginView { DataContext = vm }`).
- **Touch & Click Keypad:** Digits 0–9 bind to `PressDigitCommand` with string arguments (`"1"`–`"9"`, `"0"`). Clear and backspace bind to `ClearCommand` and `BackspaceCommand`.
- **Directionality Correction (RTL/LTR):** The keypad grid ([`LoginView.xaml:44`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Views/LoginView.xaml#L44)) explicitly overrides the window's RTL flow with `FlowDirection="LeftToRight"`, ensuring standard 1-2-3 numpad placement.
- **Input Container & Masking:** Uses a rounded container (`Border Height="56" CornerRadius="14"`) displaying masked bullets (`PinDisplay`) or placeholder text `"أدخل الرمز السري"` when empty.
- **Submit Command & Dynamic Label:** The submission button uses `CtaButtonStyle` and binds `Content="{Binding SubmitLabel}"` alongside `Command="{Binding SubmitCommand}"`.

### 1.2 Theming & Color Palette Drift
Unlike `CashierWinUI` (which underwent a full design system overhaul to restore the reference blue palette `#0F52BA` and supports dynamic Light/Dark `ThemeDictionaries`), `CallCenterWinUI` remains pinned to a legacy static green palette (`#156B5A` / `#146C59`):
- **Resource Dictionary Architecture:** Uses static resources (`{StaticResource ...}`) and hardcoded hex literals (`#F3F5F2`, `#EDF3EE`, `#64748B`, `White`), lacking dark theme support.
- **Contrast Ratios (WCAG 2.1):**
  - **PIN Bullets (`BrandPrimaryBrush` `#156B5A` on `#F5F6F3`):** Produces a contrast ratio of **5.89:1** (passes WCAG AA 4.5:1).
  - **Keypad Buttons (`#25342E` text on `#F0F3EF` background):** Produces **11.7:1** (passes WCAG AAA 7.0:1).
  - **CTA Button (`White` on `#146C59`):** Produces **6.01:1** (passes WCAG AA).
  - **Error Message (`DangerBrush` `#DC2626` on `White`):** Produces **4.65:1** (passes WCAG AA).
  - **Ergonomic Contrast Hazard:** At line 78, help text hardcodes `Foreground="#64748B"` on an outer `#F3F5F2` background, yielding **4.37:1**, falling short of the WCAG AA 4.5:1 requirement for small text.

### 1.3 Critical Async Vector: `async void OnLoggedIn` (ASYNC-001)
In [`src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:66,74-87`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs#L74-L87):
```csharp
private async void OnLoggedIn()
{
    if (!_session.HasPermission("pos"))
    {
        ShowLogin("هذا الحساب لا يملك صلاحية تسجيل الطلبات، ولا يمكن استخدامه في محطة مركز الاتصال.");
        return;
    }

    Header.Refresh();
    IsLoggedIn = true;
    var vm = _session.GetOrderDesk(Header);
    CurrentView = vm;
    await vm.InitializeAsync();
}
```
- **Crash Hazard:** `OnLoggedIn` is declared `async void`. When `LoginViewModel.SubmitAsync` calls `LoggedIn?.Invoke()`, `OnLoggedIn` begins execution. Upon hitting `await vm.InitializeAsync()`, control immediately yields back to `SubmitAsync`, which executes its `finally { IsBusy = false; }` block and terminates. If `vm.InitializeAsync()` subsequently throws an exception (e.g. backend network failure while fetching open shifts or menu categories), the exception cannot be caught by `SubmitAsync` or `ShowLogin`. It escapes to the UI Dispatcher SynchronizationContext and terminates the application process.
- **Session Corruption on Unauthorized Login:** If the authenticated operator lacks `"pos"` permission, `OnLoggedIn` calls `ShowLogin(...)`. However, `_session.CurrentUser` was already populated on line 68 of `SubmitAsync`. Because `ShowLogin` does NOT clear `_session.CurrentUser`, the session remains permanently logged in under the unauthorized account in memory while the user sees the lock screen.

---

## 2. Exhaustive Per-Callable Audit

### 2.1 `LoginViewModel(AppSession session)` (Constructor)
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:15:5:ConstructorDeclaration`
- **Exact Line Range:** Lines 15–15
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:15`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L15)
  ```csharp
  public LoginViewModel(AppSession session) => _session = session;
  ```
- **Callers & Instantiation:** Called exclusively by `MainViewModel.ShowLogin(string? errorMessage)` ([`MainViewModel.cs:65`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs#L65)).
- **State Initialization:** Injects `AppSession`. Backing field `_pin` initializes to `string.Empty`, `_errorMessage` to `null` (or the parameter passed to `ShowLogin`), and `_isBusy` to `false`.
- **Concurrency & Leaks:** Pure dependency assignment; no subscriptions or leaks.

---

### 2.2 `Pin` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:17-19`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L17-L19)
  ```csharp
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
  private string _pin = string.Empty;
  ```
- **Behavior & Notification:** Backed by generated property `Pin`. Modifying `Pin` notifies `PinDisplay` via `OnPinChanged` and recalculates `SubmitCommand.CanExecute`.
- **Security Lifecycle:** Stored as an immutable `System.String`. Appending characters causes multiple heap allocations that persist in Gen 0 managed heap until garbage collected.

---

### 2.3 `ErrorMessage` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:21-22`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L21-L22)
  ```csharp
  [ObservableProperty]
  private string? _errorMessage;
  ```
- **Behavior & Binding:** Surfaced to `LoginView.xaml:36` via `NullToCollapsedConverter`. Can be pre-populated by `ShowLogin(string? errorMessage)` when an unauthorized operator attempts login.

---

### 2.4 `IsBusy` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:24-26`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L24-L26)
  ```csharp
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
  private bool _isBusy;
  ```
- **Behavior:** Reflects ongoing backend authentication. Triggers `SubmitCommand.CanExecute` re-evaluation and updates `SubmitLabel` via `OnIsBusyChanged`.

---

### 2.5 `PinDisplay` (Expression-Bodied Property Getter)
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:28`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L28)
  ```csharp
  public string PinDisplay => new('●', Pin.Length);
  ```
- **Behavior & UI Binding:** Generates a masked bullet string (`●`) matching entered character count. Bound in `LoginView.xaml:28` and used by `EmptyStringToVisibleConverter` at line 32 to show/hide placeholder text.

---

### 2.6 `SubmitLabel` (Expression-Bodied Property Getter)
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:29`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L29)
  ```csharp
  public string SubmitLabel => IsBusy ? "جارٍ الدخول..." : "فتح الشاشة";
  ```
- **Behavior & Binding:** Returns progress label (`"جارٍ الدخول..."`) when `IsBusy` is true, or `"فتح الشاشة"` when idle. Bound in `LoginView.xaml:73`.

---

### 2.7 `OnPinChanged(string value)` (Partial Method)
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:31:5:MethodDeclaration`
- **Exact Line Range:** Lines 31–31
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:31`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L31)
  ```csharp
  partial void OnPinChanged(string value) => OnPropertyChanged(nameof(PinDisplay));
  ```
- **Behavior:** Fires `PropertyChanged` for `PinDisplay` on every keystroke, keeping bullets in sync with `Pin`.

---

### 2.8 `OnIsBusyChanged(bool value)` (Partial Method)
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:32:5:MethodDeclaration`
- **Exact Line Range:** Lines 32–32
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:32`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L32)
  ```csharp
  partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(SubmitLabel));
  ```
- **Behavior:** Fires `PropertyChanged` for `SubmitLabel` when `IsBusy` changes.

---

### 2.9 `PressDigit(string digit)` (RelayCommand Method)
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:34:5:MethodDeclaration`
- **Exact Line Range:** Lines 34–40
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:34-40`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L34-L40)
  ```csharp
  [RelayCommand]
  private void PressDigit(string digit)
  {
      if (Pin.Length >= 6) return;
      Pin += digit;
      ErrorMessage = null;
  }
  ```
- **Behavior & Bounds:** Capped at 6 characters. Appends the digit and clears active error message.
- **Forensic Defect 1 (Missing `IsBusy` Guard):** Does not check `IsBusy`. Operator can type extra digits while authentication is in progress.
- **Forensic Defect 2 (No Auto-Submit Trigger):** Reaching 4 or 6 digits does not submit automatically; requires explicit tap on CTA button.

---

### 2.10 `Backspace()` (RelayCommand Method)
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:42:5:MethodDeclaration`
- **Exact Line Range:** Lines 42–46
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:42-46`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L42-L46)
  ```csharp
  [RelayCommand]
  private void Backspace()
  {
      if (Pin.Length > 0) Pin = Pin[..^1];
  }
  ```
- **Behavior:** Truncates the last character of `Pin`.
- **Forensic Defect:** Missing `!IsBusy` check.

---

### 2.11 `Clear()` (RelayCommand Method)
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:48:5:MethodDeclaration`
- **Exact Line Range:** Lines 48–49
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:48-49`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L48-L49)
  ```csharp
  [RelayCommand]
  private void Clear() => Pin = string.Empty;
  ```
- **Behavior:** Resets `Pin` to empty string.
- **Forensic Defect:** Missing `!IsBusy` check.

---

### 2.12 `CanSubmit()` (Private Predicate Method)
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:51:5:MethodDeclaration`
- **Exact Line Range:** Lines 51–51
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:51`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L51)
  ```csharp
  private bool CanSubmit() => Pin.Length >= 4 && !IsBusy;
  ```
- **Behavior:** Predicate for `SubmitCommand`. Evaluates to true only if `Pin.Length >= 4` and `!IsBusy`.

---

### 2.13 `SubmitAsync()` (RelayCommand Method)
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:53:5:MethodDeclaration`
- **Exact Line Range:** Lines 53–80
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:53-80`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L53-L80)
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
  2. Awaits `_session.Api.LoginByPinAsync(Pin)`.
  3. On unsuccessful response, sets `ErrorMessage = "رمز PIN غير صحيح."` and clears `Pin`.
  4. On successful response, assigns `_session.CurrentUser = result.Value`, resets `Pin`, and invokes `LoggedIn?.Invoke()`.
  5. Catches network exceptions and formats `ErrorMessage = $"تعذر الاتصال بالسيرفر: {ex.Message}"`.
  6. Resets `IsBusy = false` in `finally`.
- **Forensic Defect 1 (Misleading Error Classification):**
  When `result.Success` is false, line 63 hardcodes `ErrorMessage = "رمز PIN غير صحيح."`. If the remote POS server is offline, unreachable, or returns HTTP 503, `result.Error` contains the exact reason. Discarding `result.Error` misinforms the call center operator that their PIN is incorrect.
- **Forensic Defect 2 (Trigger for ASYNC-001 Crash):**
  Line 70 fires `LoggedIn?.Invoke()`, which triggers `MainViewModel.OnLoggedIn()`. As detailed in Section 1.3, because `OnLoggedIn` is `async void`, any unhandled exception in `CallCenterViewModel.InitializeAsync` crashes the entire application process.

---

### 2.14 `LoggedIn` (Event)
- **Source Anchor:** [`src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs:13`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs#L13)
  ```csharp
  public event Action? LoggedIn;
  ```
- **Lifecycle & Subscribers:** Subscribed by `MainViewModel.ShowLogin()`: `vm.LoggedIn += OnLoggedIn;`. Event reference discarded when view model is replaced.

---

## 3. Test Coverage & Verification

- **Current Existing Tests:**
  In [`tests/Falah.Client.Tests/FinalVerificationTests.cs:444`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Client.Tests/FinalVerificationTests.cs#L444), only type assignment upon screen lock is tested:
  ```csharp
  Assert.IsType<CC.ViewModels.LoginViewModel>(root.CurrentView);
  ```
  **0% coverage** exists for the actual execution logic of `CallCenterWinUI`'s `LoginViewModel`.
- **Required Test Cases:**
  1. `PressDigit_CappedAtSix_CharactersIgnoredAfterLimit`: Verify PIN length boundary enforcement.
  2. `PressDigit_WhileBusy_MustBeIgnored`: Verify input locking while `IsBusy == true`.
  3. `SubmitAsync_ApiReturnsError_SurfacesActualErrorMessage`: Ensure server outage errors are surfaced accurately.
  4. `SubmitAsync_Success_AssignsCurrentUserAndFiresLoggedIn`: Verify successful authentication and event dispatch.
  5. `SubmitAsync_UnauthorizedUser_SessionCleared`: Verify that when a user lacks "pos" permission, `_session.CurrentUser` is properly reset to null.

---
*Audit certified by UI/UX & Code Maintainability Auditor.*
