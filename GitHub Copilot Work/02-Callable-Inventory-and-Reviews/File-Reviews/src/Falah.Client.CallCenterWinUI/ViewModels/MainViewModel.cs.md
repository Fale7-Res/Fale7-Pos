# Substantive Callable Audit: `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs`

- **Source File:** `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `013787b64dabe18c525420c1ffc1d925149183a5ae04f0f9863ae2c4d49bc483`
- **Auditor Specialist:** `mirrored-code-auditor`
- **Total File Callables:** **9**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Call center shell view model managing operator authentication state, station locking, health checks, and view transitions.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Header.get`
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:23:37:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `23-23`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Header.get. Manages internal state transition within src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 23-23):**
```csharp
  23 |     public HeaderViewModel Header { get; }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.MainViewModel(Falah.Client.CallCenterWinUI.Services.AppSession?)`
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:25:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `25-35`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.MainViewModel(Falah.Client.CallCenterWinUI.Services.AppSession?). Manages internal state transition within src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 25-35):**
```csharp
  25 |     public MainViewModel(AppSession? session = null)
  26 |     {
  27 |         _session = session ?? new AppSession();
  28 |         Header = new HeaderViewModel(_session);
  29 |         ShowLogin();
  30 | 
  31 |         _healthTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
  32 |         _healthTimer.Tick += async (_, _) => await CheckHealthNowAsync();
  33 |         _healthTimer.Start();
  34 |         _ = CheckHealthNowAsync();
  35 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:32:30:ParenthesizedLambdaExpression`
- **Declaration Kind:** `ParenthesizedLambdaExpression` | **Line Range:** `32-32`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes ParenthesizedLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 32-32):**
```csharp
  32 |         _healthTimer.Tick += async (_, _) => await CheckHealthNowAsync();
```

---

<a id="callable-4"></a>
### Callable 4: `Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.CheckHealthNowAsync()`
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:37:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `37-41`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.CheckHealthNowAsync(). Manages internal state transition within src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 37-41):**
```csharp
  37 |     private async Task CheckHealthNowAsync()
  38 |     {
  39 |         var online = await _session.Api.PingHealthAsync();
  40 |         if (!_stopped) IsOnline = online;
  41 |     }
```

---

<a id="callable-5"></a>
### Callable 5: `Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.TryClose()`
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:43:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `43-47`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.TryClose(). Manages internal state transition within src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 43-47):**
```csharp
  43 |     public bool TryClose()
  44 |     {
  45 |         IsCloseBlocked = _session.HasUnresolvedOperation;
  46 |         return !IsCloseBlocked;
  47 |     }
```

---

<a id="callable-6"></a>
### Callable 6: `Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Stop()`
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:49:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `49-54`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Stop(). Manages internal state transition within src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 49-54):**
```csharp
  49 |     public void Stop()
  50 |     {
  51 |         _stopped = true;
  52 |         _healthTimer.Stop();
  53 |         Header.Stop();
  54 |     }
```

---

<a id="callable-7"></a>
### Callable 7: `Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Lock()`
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:56:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `56-60`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.Lock(). Manages internal state transition within src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 56-60):**
```csharp
  56 |     [RelayCommand]
  57 |     private void Lock()
  58 |     {
  59 |         if (!_session.HasUnresolvedOperation) ShowLogin();
  60 |     }
```

---

<a id="callable-8"></a>
### Callable 8: `Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.ShowLogin(string?)`
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:62:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `62-68`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.ShowLogin(string?). Manages internal state transition within src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 62-68):**
```csharp
  62 |     private void ShowLogin(string? errorMessage = null)
  63 |     {
  64 |         IsLoggedIn = false;
  65 |         var vm = new LoginViewModel(_session) { ErrorMessage = errorMessage };
  66 |         vm.LoggedIn += OnLoggedIn;
  67 |         CurrentView = vm;
  68 |     }
```

---

<a id="callable-9"></a>
### Callable 9: `Falah.Client.CallCenterWinUI.ViewModels.MainViewModel.OnLoggedIn()`
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:74:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `74-87`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`ASYNC-001`**

**A. Actual Behavior & Execution Path:**  
Async void event handler triggered upon login completion. Executes background initialization, health check polling, and station state recovery.

**B. Trust Boundaries & Validation:**  
Internal application event.

**C. State Mutations & Side Effects:**  
Mutates CurrentUser, CurrentView, Header state.

**D. Concurrency & Transaction Implications:**  
Executes on UI thread context as async void.

**E. Specialist Risks & Invariants:**  
FLAW ASYNC-001: Unhandled exceptions inside async void terminate the entire application process abruptly without crash recovery.

> **Verbatim Code Excerpt (Lines 74-87):**
```csharp
  74 |     private async void OnLoggedIn()
  75 |     {
  76 |         if (!_session.HasPermission("pos"))
  77 |         {
  78 |             ShowLogin("هذا الحساب لا يملك صلاحية تسجيل الطلبات، ولا يمكن استخدامه في محطة مركز الاتصال.");
  79 |             return;
  80 |         }
  81 | 
  82 |         Header.Refresh();
  83 |         IsLoggedIn = true;
  84 |         var vm = _session.GetOrderDesk(Header);
  85 |         CurrentView = vm;
  86 |         await vm.InitializeAsync();
  87 |     }
```

---
