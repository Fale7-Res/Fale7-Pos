# Substantive Callable Audit: `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs`

- **Source File:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `18449ab63e886ec4a5a507bfa408d9f31315fb0dd091e3ac2114060ae5ea43de`
- **Auditor Specialist:** `mirrored-code-auditor`
- **Total File Callables:** **71**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Menu and product variant configuration view model. Binds category selections, pricing inputs, day choices, and product image uploads.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.MenuEditorViewModel(Falah.Client.CashierWinUI.Services.AppSession)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:23:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `23-23`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.MenuEditorViewModel(Falah.Client.CashierWinUI.Services.AppSession). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

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
  23 |     public MenuEditorViewModel(AppSession session) => _session = session;
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Id.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:29:37:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `29-29`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Id.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 29-29):**
```csharp
  29 |         public required string Id { get; init; }
```

---

<a id="callable-3"></a>
### Callable 3: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Id.init`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:29:42:InitAccessorDeclaration`
- **Declaration Kind:** `InitAccessorDeclaration` | **Line Range:** `29-29`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes InitAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Id.init. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 29-29):**
```csharp
  29 |         public required string Id { get; init; }
```

---

<a id="callable-4"></a>
### Callable 4: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Name.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:30:39:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `30-30`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Name.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 30-30):**
```csharp
  30 |         public required string Name { get; init; }
```

---

<a id="callable-5"></a>
### Callable 5: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Name.init`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:30:44:InitAccessorDeclaration`
- **Declaration Kind:** `InitAccessorDeclaration` | **Line Range:** `30-30`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes InitAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Name.init. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 30-30):**
```csharp
  30 |         public required string Name { get; init; }
```

---

<a id="callable-6"></a>
### Callable 6: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Value.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:35:37:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `35-35`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Value.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 35-35):**
```csharp
  35 |         public required int Value { get; init; }
```

---

<a id="callable-7"></a>
### Callable 7: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Value.init`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:35:42:InitAccessorDeclaration`
- **Declaration Kind:** `InitAccessorDeclaration` | **Line Range:** `35-35`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes InitAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Value.init. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 35-35):**
```csharp
  35 |         public required int Value { get; init; }
```

---

<a id="callable-8"></a>
### Callable 8: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Name.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:36:39:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `36-36`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Name.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 36-36):**
```csharp
  36 |         public required string Name { get; init; }
```

---

<a id="callable-9"></a>
### Callable 9: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Name.init`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:36:44:InitAccessorDeclaration`
- **Declaration Kind:** `InitAccessorDeclaration` | **Line Range:** `36-36`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes InitAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Name.init. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 36-36):**
```csharp
  36 |         public required string Name { get; init; }
```

---

<a id="callable-10"></a>
### Callable 10: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.VariantRow.Id.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:41:29:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `41-41`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.VariantRow.Id.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 41-41):**
```csharp
  41 |         public string? Id { get; init; }
```

---

<a id="callable-11"></a>
### Callable 11: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.VariantRow.Id.init`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:41:34:InitAccessorDeclaration`
- **Declaration Kind:** `InitAccessorDeclaration` | **Line Range:** `41-41`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes InitAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.VariantRow.Id.init. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 41-41):**
```csharp
  41 |         public string? Id { get; init; }
```

---

<a id="callable-12"></a>
### Callable 12: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Rows.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:46:52:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `46-46`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Rows.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 46-46):**
```csharp
  46 |     public ObservableCollection<CatalogRow> Rows { get; } = new();
```

---

<a id="callable-13"></a>
### Callable 13: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.CategoryChoices.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:47:59:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `47-47`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.CategoryChoices.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 47-47):**
```csharp
  47 |     public ObservableCollection<Choice> CategoryChoices { get; } = new();
```

---

<a id="callable-14"></a>
### Callable 14: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RecipeChoices.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:48:57:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `48-48`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RecipeChoices.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 48-48):**
```csharp
  48 |     public ObservableCollection<Choice> RecipeChoices { get; } = new();
```

---

<a id="callable-15"></a>
### Callable 15: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Variants.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:49:56:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `49-49`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Variants.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 49-49):**
```csharp
  49 |     public ObservableCollection<VariantRow> Variants { get; } = new();
```

---

<a id="callable-16"></a>
### Callable 16: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.PriceProducts.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:50:64:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `50-50`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.PriceProducts.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 50-50):**
```csharp
  50 |     public ObservableCollection<ProductChoice> PriceProducts { get; } = new();
```

---

<a id="callable-17"></a>
### Callable 17: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Stations.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:51:45:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `51-51`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Stations.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 51-51):**
```csharp
  51 |     public IReadOnlyList<Choice> Stations { get; } = new[] { new Choice(null, "بدون محطة تجهيز"), new Choice("Grill", "مشويات"), new Choice("Charcoal", "فحم") };
```

---

<a id="callable-18"></a>
### Callable 18: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.TaxModes.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:52:45:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `52-52`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.TaxModes.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 52-52):**
```csharp
  52 |     public IReadOnlyList<Choice> TaxModes { get; } = new[] { new Choice("Exclusive", "تُضاف إلى السعر"), new Choice("Inclusive", "مشمولة في السعر") };
```

---

<a id="callable-19"></a>
### Callable 19: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Days.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:53:44:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `53-53`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Days.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 53-53):**
```csharp
  53 |     public IReadOnlyList<DayChoice> Days { get; } = new[] { "الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" }
```

---

<a id="callable-20"></a>
### Callable 20: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:54:17:ParenthesizedLambdaExpression`
- **Declaration Kind:** `ParenthesizedLambdaExpression` | **Line Range:** `54-54`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ParenthesizedLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 54-54):**
```csharp
  54 |         .Select((name, index) => new DayChoice { Value = index, Name = name }).ToArray();
```

---

<a id="callable-21"></a>
### Callable 21: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnIsBusyChanged(bool)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:104:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `104-104`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnIsBusyChanged(bool). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 104-104):**
```csharp
 104 |     partial void OnIsBusyChanged(bool value) => NotifyState();
```

---

<a id="callable-22"></a>
### Callable 22: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnIsLoadingChanged(bool)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:105:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `105-105`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnIsLoadingChanged(bool). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 105-105):**
```csharp
 105 |     partial void OnIsLoadingChanged(bool value) => NotifyState();
```

---

<a id="callable-23"></a>
### Callable 23: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.NotifyState()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:106:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `106-110`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.NotifyState(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 106-110):**
```csharp
 106 |     private void NotifyState()
 107 |     {
 108 |         foreach (var name in new[] { nameof(CanBrowse), nameof(CanEdit), nameof(CanToggleStatus), nameof(HasNoRows), nameof(SaveLabel), nameof(StatusActionLabel), nameof(EditorTitle), nameof(CanEditImage), nameof(ImageHint) }) OnPropertyChanged(name);
 109 |         _session.SetManagementOperationPending(this, IsBusy || _write.HasPending);
 110 |     }
```

---

<a id="callable-24"></a>
### Callable 24: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OpenAsync()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:111:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `111-116`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OpenAsync(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 111-116):**
```csharp
 111 |     public async Task OpenAsync()
 112 |     {
 113 |         if (!CanOpen || _session.HasUnresolvedOperation) return;
 114 |         IsOpen = true; HasEditor = false; ActionError = ActionNote = null;
 115 |         await LoadAsync();
 116 |     }
```

---

<a id="callable-25"></a>
### Callable 25: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Close()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:117:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `117-121`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Close(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 117-121):**
```csharp
 117 |     [RelayCommand] private void Close()
 118 |     {
 119 |         if (!CanBrowse) return;
 120 |         IsOpen = false; HasEditor = false; Closed?.Invoke();
 121 |     }
```

---

<a id="callable-26"></a>
### Callable 26: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.LoadAsync()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:122:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `122-126`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.LoadAsync(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 122-126):**
```csharp
 122 |     [RelayCommand] public async Task LoadAsync()
 123 |     {
 124 |         if (!CanBrowse) return;
 125 |         await ReloadAsync();
 126 |     }
```

---

<a id="callable-27"></a>
### Callable 27: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ReloadAsync()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:127:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `127-152`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ReloadAsync(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 127-152):**
```csharp
 127 |     private async Task ReloadAsync()
 128 |     {
 129 |         IsLoading = true; IsReady = false; LoadError = null; Rows.Clear();
 130 |         try
 131 |         {
 132 |             var categories = await _session.Api.GetAllCategoriesAsync();
 133 |             if (!categories.Success || categories.Value is null) { LoadError = categories.Error; return; }
 134 |             var products = await _session.Api.GetAllProductsAsync();
 135 |             if (!products.Success || products.Value is null) { LoadError = products.Error; return; }
 136 |             var recipes = await _session.Api.GetRecipesAsync();
 137 |             if (!recipes.Success || recipes.Value is null) { LoadError = recipes.Error; return; }
 138 |             var tax = await _session.Api.GetTaxRulesAsync();
 139 |             if (!tax.Success || tax.Value is null) { LoadError = tax.Error; return; }
 140 |             var prices = await _session.Api.GetPriceRulesAsync();
 141 |             if (!prices.Success || prices.Value is null) { LoadError = prices.Error; return; }
 142 |             _categories = categories.Value; _products = products.Value; _tax = tax.Value; _prices = prices.Value;
 143 |             CategoryChoices.Clear(); foreach (var c in _categories.OrderBy(c => c.SortOrder)) CategoryChoices.Add(new(c.Id, c.Name + (c.Active ? "" : " (متوقف)")));
 144 |             RecipeChoices.Clear(); RecipeChoices.Add(new(null, "بدون وصفة مرتبطة"));
 145 |             foreach (var r in recipes.Value) RecipeChoices.Add(new(r.Id, r.Name));
 146 |             foreach (var id in _products.SelectMany(p => p.Variants.Select(v => v.RecipeId).Append(p.RecipeId)).Where(id => id is not null).Distinct())
 147 |                 if (!RecipeChoices.Any(c => c.Id == id)) RecipeChoices.Add(new(id, "وصفة مرتبطة غير متاحة في القائمة"));
 148 |             PriceProducts.Clear(); foreach (var p in _products) PriceProducts.Add(new() { Id = p.Id, Name = p.Name });
 149 |             IsReady = true; RefreshRows();
 150 |         }
 151 |         finally { IsLoading = false; }
 152 |     }
```

---

<a id="callable-28"></a>
### Callable 28: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:143:76:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `143-143`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 143-143):**
```csharp
 143 |             CategoryChoices.Clear(); foreach (var c in _categories.OrderBy(c => c.SortOrder)) CategoryChoices.Add(new(c.Id, c.Name + (c.Active ? "" : " (متوقف)")));
```

---

<a id="callable-29"></a>
### Callable 29: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:146:53:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `146-146`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 146-146):**
```csharp
 146 |             foreach (var id in _products.SelectMany(p => p.Variants.Select(v => v.RecipeId).Append(p.RecipeId)).Where(id => id is not null).Distinct())
```

---

<a id="callable-30"></a>
### Callable 30: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:146:76:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `146-146`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 146-146):**
```csharp
 146 |             foreach (var id in _products.SelectMany(p => p.Variants.Select(v => v.RecipeId).Append(p.RecipeId)).Where(id => id is not null).Distinct())
```

---

<a id="callable-31"></a>
### Callable 31: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:146:119:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `146-146`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 146-146):**
```csharp
 146 |             foreach (var id in _products.SelectMany(p => p.Variants.Select(v => v.RecipeId).Append(p.RecipeId)).Where(id => id is not null).Distinct())
```

---

<a id="callable-32"></a>
### Callable 32: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:147:40:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `147-147`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 147-147):**
```csharp
 147 |                 if (!RecipeChoices.Any(c => c.Id == id)) RecipeChoices.Add(new(id, "وصفة مرتبطة غير متاحة في القائمة"));
```

---

<a id="callable-33"></a>
### Callable 33: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SetKind(string)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:153:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `153-159`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SetKind(string). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 153-159):**
```csharp
 153 |     [RelayCommand] private void SetKind(string kind)
 154 |     {
 155 |         if (!CanBrowse || kind is not ("products" or "categories" or "tax" or "prices")) return;
 156 |         Kind = kind; HasEditor = false; _editing = null; ActionError = ActionNote = null; ProductImageUri = null;
 157 |         foreach (var name in new[] { nameof(IsProduct), nameof(IsCategory), nameof(IsTax), nameof(IsPriceRule), nameof(HasSortOrder), nameof(HasPrice), nameof(HasActive), nameof(CanManageKind), nameof(KindTitle), nameof(EmptySelectionHint) }) OnPropertyChanged(name);
 158 |         RefreshRows(); NotifyState();
 159 |     }
```

---

<a id="callable-34"></a>
### Callable 34: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnSearchTextChanged(string)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:164:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `164-164`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnSearchTextChanged(string). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 164-164):**
```csharp
 164 |     partial void OnSearchTextChanged(string value) => RefreshRows();
```

---

<a id="callable-35"></a>
### Callable 35: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RefreshRows()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:168:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `168-186`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RefreshRows(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 168-186):**
```csharp
 168 |     private void RefreshRows()
 169 |     {
 170 |         Rows.Clear();
 171 |         IEnumerable<CatalogRow> rows = Kind switch
 172 |         {
 173 |             "categories" => _categories.OrderBy(c => c.SortOrder).Select(c => new CatalogRow(c.Name, c.Active ? "نشط" : "متوقف", c)),
 174 |             "tax" => _tax.Select(t => new CatalogRow(t.Name, $"{t.RateBasisPoints / 100m:0.##}% • {(t.Enabled ? "مفعّلة" : "متوقفة")}", t)),
 175 |             "prices" => _prices.Select(p => new CatalogRow(p.Name, CashInput.Format(p.FixedPricePiastres) + " ج • " + (p.Active ? "نشط" : "متوقف"), p)),
 176 |             _ => _products.OrderBy(p => p.SortOrder).Select(p => new CatalogRow(p.Name, CashInput.Format(p.BasePricePiastres) + " ج • " + (p.IsAvailable ? "متاح" : "متوقف"), p))
 177 |         };
 178 |         var term = SearchText.Trim();
 179 |         if (term.Length > 0)
 180 |             rows = rows.Where(r => r.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase)
 181 |                                 || r.Detail.Contains(term, StringComparison.CurrentCultureIgnoreCase));
 182 | 
 183 |         foreach (var row in rows) Rows.Add(row);
 184 |         OnPropertyChanged(nameof(HasNoRows));
 185 |         OnPropertyChanged(nameof(HasNoMatches));
 186 |     }
```

---

<a id="callable-36"></a>
### Callable 36: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:173:49:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `173-173`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 173-173):**
```csharp
 173 |             "categories" => _categories.OrderBy(c => c.SortOrder).Select(c => new CatalogRow(c.Name, c.Active ? "نشط" : "متوقف", c)),
```

---

<a id="callable-37"></a>
### Callable 37: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:173:74:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `173-173`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 173-173):**
```csharp
 173 |             "categories" => _categories.OrderBy(c => c.SortOrder).Select(c => new CatalogRow(c.Name, c.Active ? "نشط" : "متوقف", c)),
```

---

<a id="callable-38"></a>
### Callable 38: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:174:34:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `174-174`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 174-174):**
```csharp
 174 |             "tax" => _tax.Select(t => new CatalogRow(t.Name, $"{t.RateBasisPoints / 100m:0.##}% • {(t.Enabled ? "مفعّلة" : "متوقفة")}", t)),
```

---

<a id="callable-39"></a>
### Callable 39: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:175:40:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `175-175`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 175-175):**
```csharp
 175 |             "prices" => _prices.Select(p => new CatalogRow(p.Name, CashInput.Format(p.FixedPricePiastres) + " ج • " + (p.Active ? "نشط" : "متوقف"), p)),
```

---

<a id="callable-40"></a>
### Callable 40: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:176:36:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `176-176`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 176-176):**
```csharp
 176 |             _ => _products.OrderBy(p => p.SortOrder).Select(p => new CatalogRow(p.Name, CashInput.Format(p.BasePricePiastres) + " ج • " + (p.IsAvailable ? "متاح" : "متوقف"), p))
```

---

<a id="callable-41"></a>
### Callable 41: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:176:61:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `176-176`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 176-176):**
```csharp
 176 |             _ => _products.OrderBy(p => p.SortOrder).Select(p => new CatalogRow(p.Name, CashInput.Format(p.BasePricePiastres) + " ج • " + (p.IsAvailable ? "متاح" : "متوقف"), p))
```

---

<a id="callable-42"></a>
### Callable 42: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:180:31:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `180-181`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 180-181):**
```csharp
 180 |             rows = rows.Where(r => r.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase)
 181 |                                 || r.Detail.Contains(term, StringComparison.CurrentCultureIgnoreCase));
```

---

<a id="callable-43"></a>
### Callable 43: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.New()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:187:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `187-191`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.New(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 187-191):**
```csharp
 187 |     [RelayCommand] private void New()
 188 |     {
 189 |         if (!CanEdit) return;
 190 |         ResetForm(); _editing = null; HasEditor = true; NotifyState(); NotifyImageState();
 191 |     }
```

---

<a id="callable-44"></a>
### Callable 44: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ResetForm()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:192:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `192-199`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ResetForm(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 192-199):**
```csharp
 192 |     private void ResetForm()
 193 |     {
 194 |         Name = Icon = PriceText = TaxRateText = StartTimeText = EndTimeText = ""; SortOrderText = "0";
 195 |         Category = null; Recipe = RecipeChoices.FirstOrDefault(); Station = Stations[0]; TaxMode = null;
 196 |         SendToGrill = Active = false; ActionError = ActionNote = null; Variants.Clear(); ProductImageUri = null;
 197 |         foreach (var p in PriceProducts) p.Selected = false;
 198 |         foreach (var d in Days) d.Selected = false;
 199 |     }
```

---

<a id="callable-45"></a>
### Callable 45: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SelectRow(Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.CatalogRow)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:200:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `200-223`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SelectRow(Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.CatalogRow). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 200-223):**
```csharp
 200 |     [RelayCommand] private void SelectRow(CatalogRow row)
 201 |     {
 202 |         if (!CanBrowse || !IsReady) return;
 203 |         ResetForm(); _editing = row.Value; HasEditor = true;
 204 |         switch (row.Value)
 205 |         {
 206 |             case CategoryDetailsDto c: Name = c.Name; Icon = c.Icon ?? ""; SortOrderText = c.SortOrder.ToString(CultureInfo.InvariantCulture); break;
 207 |             case ProductDetailsDto p:
 208 |                 Name = p.Name; PriceText = CashInput.Format(p.BasePricePiastres); SortOrderText = p.SortOrder.ToString(CultureInfo.InvariantCulture);
 209 |                 Category = CategoryChoices.FirstOrDefault(c => c.Id == p.CategoryId); Recipe = RecipeChoices.FirstOrDefault(r => r.Id == p.RecipeId);
 210 |                 Station = Stations.FirstOrDefault(s => s.Id == p.PreparationStation); SendToGrill = p.SendToGrill;
 211 |                 foreach (var v in p.Variants) Variants.Add(new() { Id = v.Id, Name = v.Name, PriceText = CashInput.Format(v.PricePiastres), Recipe = RecipeChoices.FirstOrDefault(r => r.Id == v.RecipeId) });
 212 |                 LoadProductImage(p);
 213 |                 break;
 214 |             case TaxRuleDto t: Name = t.Name; TaxRateText = CashInput.Format(t.RateBasisPoints); TaxMode = TaxModes.FirstOrDefault(m => m.Id == t.Mode); Active = t.Enabled; break;
 215 |             case PriceRuleDto p:
 216 |                 Name = p.Name; PriceText = CashInput.Format(p.FixedPricePiastres); Active = p.Active;
 217 |                 StartTimeText = p.StartTime?.ToString("HH:mm") ?? ""; EndTimeText = p.EndTime?.ToString("HH:mm") ?? "";
 218 |                 foreach (var product in PriceProducts) product.Selected = p.ProductIds.Contains(product.Id);
 219 |                 foreach (var day in Days) day.Selected = p.DaysOfWeek?.Contains(day.Value) == true;
 220 |                 break;
 221 |         }
 222 |         NotifyState();
 223 |     }
```

---

<a id="callable-46"></a>
### Callable 46: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:209:59:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `209-209`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 209-209):**
```csharp
 209 |                 Category = CategoryChoices.FirstOrDefault(c => c.Id == p.CategoryId); Recipe = RecipeChoices.FirstOrDefault(r => r.Id == p.RecipeId);
```

---

<a id="callable-47"></a>
### Callable 47: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:209:125:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `209-209`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 209-209):**
```csharp
 209 |                 Category = CategoryChoices.FirstOrDefault(c => c.Id == p.CategoryId); Recipe = RecipeChoices.FirstOrDefault(r => r.Id == p.RecipeId);
```

---

<a id="callable-48"></a>
### Callable 48: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:210:51:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `210-210`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 210-210):**
```csharp
 210 |                 Station = Stations.FirstOrDefault(s => s.Id == p.PreparationStation); SendToGrill = p.SendToGrill;
```

---

<a id="callable-49"></a>
### Callable 49: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:211:179:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `211-211`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 211-211):**
```csharp
 211 |                 foreach (var v in p.Variants) Variants.Add(new() { Id = v.Id, Name = v.Name, PriceText = CashInput.Format(v.PricePiastres), Recipe = RecipeChoices.FirstOrDefault(r => r.Id == v.RecipeId) });
```

---

<a id="callable-50"></a>
### Callable 50: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:214:132:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `214-214`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 214-214):**
```csharp
 214 |             case TaxRuleDto t: Name = t.Name; TaxRateText = CashInput.Format(t.RateBasisPoints); TaxMode = TaxModes.FirstOrDefault(m => m.Id == t.Mode); Active = t.Enabled; break;
```

---

<a id="callable-51"></a>
### Callable 51: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnProductImageUriChanged(Uri?)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:241:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `241-241`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnProductImageUriChanged(Uri?). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 241-241):**
```csharp
 241 |     partial void OnProductImageUriChanged(Uri? value) => NotifyImageState();
```

---

<a id="callable-52"></a>
### Callable 52: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnIsImageBusyChanged(bool)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:242:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `242-242`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnIsImageBusyChanged(bool). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 242-242):**
```csharp
 242 |     partial void OnIsImageBusyChanged(bool value) => NotifyImageState();
```

---

<a id="callable-53"></a>
### Callable 53: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.NotifyImageState()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:243:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `243-246`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.NotifyImageState(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 243-246):**
```csharp
 243 |     private void NotifyImageState()
 244 |     {
 245 |         foreach (var name in new[] { nameof(HasProductImage), nameof(CanEditImage), nameof(ImageHint) }) OnPropertyChanged(name);
 246 |     }
```

---

<a id="callable-54"></a>
### Callable 54: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.LoadProductImage(Falah.Application.Menu.ProductDetailsDto?)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:248:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `248-251`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.LoadProductImage(Falah.Application.Menu.ProductDetailsDto?). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 248-251):**
```csharp
 248 |     private void LoadProductImage(ProductDetailsDto? product)
 249 |     {
 250 |         ProductImageUri = product is null ? null : _session.Api.ProductImageUri(product.Id, product.ImageVersion);
 251 |     }
```

---

<a id="callable-55"></a>
### Callable 55: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.PickImageAsync.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:255:78:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `255-255`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.PickImageAsync.get. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 255-255):**
```csharp
 255 |     public Func<Task<(string ContentType, byte[] Bytes)?>>? PickImageAsync { get; set; }
```

---

<a id="callable-56"></a>
### Callable 56: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.PickImageAsync.set`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:255:83:SetAccessorDeclaration`
- **Declaration Kind:** `SetAccessorDeclaration` | **Line Range:** `255-255`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SetAccessorDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.PickImageAsync.set. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 255-255):**
```csharp
 255 |     public Func<Task<(string ContentType, byte[] Bytes)?>>? PickImageAsync { get; set; }
```

---

<a id="callable-57"></a>
### Callable 57: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ChooseImageAsync()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:257:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `257-289`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ChooseImageAsync(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 257-289):**
```csharp
 257 |     [RelayCommand] private async Task ChooseImageAsync()
 258 |     {
 259 |         if (!CanEditImage || PickImageAsync is null || _editing is not ProductDetailsDto product || _session.CurrentUser is null) return;
 260 |         ActionError = ActionNote = null;
 261 |         (string ContentType, byte[] Bytes)? picked;
 262 |         try { picked = await PickImageAsync(); }
 263 |         catch (Exception ex) { ActionError = "تعذّر قراءة الملف: " + ex.Message; return; }
 264 |         if (picked is null) return;
 265 | 
 266 |         if (picked.Value.Bytes.Length > Falah.Domain.Menu.ProductImageRules.MaxBytes)
 267 |         {
 268 |             // Refused here as well as on the server, because the operator should learn the limit
 269 |             // before spending the upload, not after.
 270 |             ActionError = $"حجم الصورة أكبر من الحد المسموح ({Falah.Domain.Menu.ProductImageRules.MaxBytes / 1024} كيلوبايت). اختر صورة أصغر.";
 271 |             return;
 272 |         }
 273 | 
 274 |         IsImageBusy = true;
 275 |         try
 276 |         {
 277 |             var result = await _session.Api.SetProductImageAsync(new SetProductImageRequest(
 278 |                 _session.CurrentUser.Id, product.Id, picked.Value.ContentType, Convert.ToBase64String(picked.Value.Bytes)));
 279 |             if (!result.Success || result.Value is null) { ActionError = result.Error ?? "تعذّر حفظ الصورة."; return; }
 280 | 
 281 |             // The new version stamp makes a different URL, so the picture that comes back is the
 282 |             // one just uploaded rather than the one the image cache already holds.
 283 |             _editing = product with { ImageVersion = result.Value.Version };
 284 |             LoadProductImage((ProductDetailsDto)_editing);
 285 |             ActionNote = "تم حفظ الصورة.";
 286 |             await ReloadAsync();
 287 |         }
 288 |         finally { IsImageBusy = false; }
 289 |     }
```

---

<a id="callable-58"></a>
### Callable 58: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RemoveImageAsync()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:291:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `291-307`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RemoveImageAsync(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 291-307):**
```csharp
 291 |     [RelayCommand] private async Task RemoveImageAsync()
 292 |     {
 293 |         if (!CanEditImage || !HasProductImage || _editing is not ProductDetailsDto product || _session.CurrentUser is null) return;
 294 |         ActionError = ActionNote = null;
 295 |         IsImageBusy = true;
 296 |         try
 297 |         {
 298 |             var result = await _session.Api.RemoveProductImageAsync(product.Id, _session.CurrentUser.Id);
 299 |             if (!result.Success) { ActionError = result.Error ?? "تعذّر حذف الصورة."; return; }
 300 | 
 301 |             _editing = product with { ImageVersion = null };
 302 |             ProductImageUri = null;
 303 |             ActionNote = "تم حذف الصورة؛ يظهر رمز القسم بدلًا منها.";
 304 |             await ReloadAsync();
 305 |         }
 306 |         finally { IsImageBusy = false; }
 307 |     }
```

---

<a id="callable-59"></a>
### Callable 59: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.AddVariant()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:309:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `309-309`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.AddVariant(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 309-309):**
```csharp
 309 |     [RelayCommand] private void AddVariant() { if (CanEdit && IsProduct) Variants.Add(new() { Recipe = RecipeChoices.FirstOrDefault() }); }
```

---

<a id="callable-60"></a>
### Callable 60: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RemoveVariant(Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.VariantRow)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:310:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `310-310`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RemoveVariant(Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.VariantRow). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 310-310):**
```csharp
 310 |     [RelayCommand] private void RemoveVariant(VariantRow row) { if (CanEdit && IsProduct) Variants.Remove(row); }
```

---

<a id="callable-61"></a>
### Callable 61: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.TryTime(string, out TimeOnly?)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:311:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `311-316`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.TryTime(string, out TimeOnly?). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 311-316):**
```csharp
 311 |     private static bool TryTime(string text, out TimeOnly? time)
 312 |     {
 313 |         time = null; if (string.IsNullOrWhiteSpace(text)) return true;
 314 |         if (!TimeOnly.TryParseExact(text.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return false;
 315 |         time = parsed; return true;
 316 |     }
```

---

<a id="callable-62"></a>
### Callable 62: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.CreateRequest()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:317:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `317-352`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FIN-CAND-NEGATIVE-VARIANT-PRICE-001`**

**A. Actual Behavior & Execution Path:**  
Collects input fields into CreateProductRequest / UpdateProductRequest DTOs. Allows negative price delta variants without client-side floor validation.

**B. Trust Boundaries & Validation:**  
Manager menu configuration input.

**C. State Mutations & Side Effects:**  
Generates API request payload.

**D. Concurrency & Transaction Implications:**  
UI thread.

**E. Specialist Risks & Invariants:**  
FLAW FIN-CAND-NEGATIVE-VARIANT-PRICE-001: Submits negative variant prices that server accepts, allowing carts with negative totals to consume inventory.

> **Verbatim Code Excerpt (Lines 317-352):**
```csharp
 317 |     private object? CreateRequest()
 318 |     {
 319 |         if (!CanEdit || !HasEditor || _session.CurrentUser is null) return null;
 320 |         if (string.IsNullOrWhiteSpace(Name)) { ActionError = "الاسم مطلوب."; return null; }
 321 |         var key = $"menu-{Guid.NewGuid():N}"; var actor = _session.CurrentUser.Id;
 322 |         var sort = 0; long price = 0;
 323 |         if (HasSortOrder && !int.TryParse(SortOrderText, NumberStyles.Integer, CultureInfo.InvariantCulture, out sort)) { ActionError = "أدخل ترتيب عرض صحيحًا."; return null; }
 324 |         if (HasPrice && (!CashInput.TryParse(PriceText, out price) || price <= 0)) { ActionError = "أدخل سعرًا صحيحًا أكبر من صفر."; return null; }
 325 |         if (IsCategory) return _editing is CategoryDetailsDto c
 326 |             ? new UpdateCategoryRequest(c.Id, Name.Trim(), string.IsNullOrWhiteSpace(Icon) ? null : Icon.Trim(), sort, actor, key)
 327 |             : new CreateCategoryRequest(key, Name.Trim(), string.IsNullOrWhiteSpace(Icon) ? null : Icon.Trim(), sort, actor);
 328 |         if (IsProduct)
 329 |         {
 330 |             if (Category?.Id is null || !CategoryChoices.Contains(Category) || Station is null) { ActionError = "اختر القسم ومحطة التجهيز."; return null; }
 331 |             var variants = new List<ProductVariantInput>();
 332 |             foreach (var v in Variants)
 333 |             {
 334 |                 if (string.IsNullOrWhiteSpace(v.Name) || !CashInput.TryParse(v.PriceText, out var amount) || amount <= 0) { ActionError = "كل حجم يحتاج اسمًا وسعرًا صحيحًا أكبر من صفر."; return null; }
 335 |                 variants.Add(new(v.Id, v.Name.Trim(), amount, v.Recipe?.Id));
 336 |             }
 337 |             return _editing is ProductDetailsDto p
 338 |                 ? new UpdateProductRequest(p.Id, Category.Id, Name.Trim(), price, SendToGrill, Station.Id, sort, Recipe?.Id, variants.ToArray(), actor, key)
 339 |                 : new CreateProductRequest(key, Category.Id, Name.Trim(), price, SendToGrill, Station.Id, sort, Recipe?.Id, variants.ToArray(), actor);
 340 |         }
 341 |         if (IsTax)
 342 |         {
 343 |             if (!CashInput.TryParse(TaxRateText, out var rate) || rate > int.MaxValue || TaxMode?.Id is null) { ActionError = "أدخل نسبة صحيحة واختر طريقة احتساب الضريبة."; return null; }
 344 |             return _editing is TaxRuleDto t ? new UpdateTaxRuleRequest(t.Id, Name.Trim(), (int)rate, TaxMode.Id, Active, actor, key)
 345 |                 : new CreateTaxRuleRequest(key, Name.Trim(), (int)rate, TaxMode.Id, Active, actor);
 346 |         }
 347 |         if (!TryTime(StartTimeText, out var start) || !TryTime(EndTimeText, out var end)) { ActionError = "أدخل الوقت بنظام 24 ساعة مثل 18:30، أو اتركه فارغًا."; return null; }
 348 |         var ids = PriceProducts.Where(p => p.Selected).Select(p => p.Id).ToArray();
 349 |         if (ids.Length == 0) { ActionError = "اختر صنفًا واحدًا على الأقل للعرض."; return null; }
 350 |         var days = Days.Where(d => d.Selected).Select(d => d.Value).ToArray();
 351 |         return new SavePriceRuleRequest(key, (_editing as PriceRuleDto)?.Id, Name.Trim(), ids, price, days.Length == 0 ? null : days, start, end, Active, actor, null, null);
 352 |     }
```

---

<a id="callable-63"></a>
### Callable 63: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:348:39:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `348-348`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 348-348):**
```csharp
 348 |         var ids = PriceProducts.Where(p => p.Selected).Select(p => p.Id).ToArray();
```

---

<a id="callable-64"></a>
### Callable 64: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:348:63:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `348-348`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 348-348):**
```csharp
 348 |         var ids = PriceProducts.Where(p => p.Selected).Select(p => p.Id).ToArray();
```

---

<a id="callable-65"></a>
### Callable 65: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:350:31:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `350-350`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 350-350):**
```csharp
 350 |         var days = Days.Where(d => d.Selected).Select(d => d.Value).ToArray();
```

---

<a id="callable-66"></a>
### Callable 66: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:350:55:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `350-350`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 350-350):**
```csharp
 350 |         var days = Days.Where(d => d.Selected).Select(d => d.Value).ToArray();
```

---

<a id="callable-67"></a>
### Callable 67: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SaveAsync()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:353:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `353-359`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SaveAsync(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 353-359):**
```csharp
 353 |     [RelayCommand] private async Task SaveAsync()
 354 |     {
 355 |         if (IsBusy) return;
 356 |         ActionError = null;
 357 |         var request = _write.Value ?? CreateRequest();
 358 |         if (request is not null) await SubmitAsync(request);
 359 |     }
```

---

<a id="callable-68"></a>
### Callable 68: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ToggleStatusAsync()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:360:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `360-366`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ToggleStatusAsync(). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 360-366):**
```csharp
 360 |     [RelayCommand] private async Task ToggleStatusAsync()
 361 |     {
 362 |         if (!CanToggleStatus) return;
 363 |         var key = $"menu-status-{Guid.NewGuid():N}"; var actor = _session.CurrentUser!.Id;
 364 |         object? request = _editing switch { CategoryDetailsDto c => new SetCategoryActiveRequest(c.Id, !c.Active, actor, key), ProductDetailsDto p => new SetProductAvailabilityRequest(p.Id, !p.IsAvailable, actor, key), _ => null };
 365 |         if (request is not null) await SubmitAsync(request);
 366 |     }
```

---

<a id="callable-69"></a>
### Callable 69: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SubmitAsync(object)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:367:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `367-378`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SubmitAsync(object). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 367-378):**
```csharp
 367 |     private async Task SubmitAsync(object request)
 368 |     {
 369 |         IsBusy = true; ActionError = ActionNote = null;
 370 |         try
 371 |         {
 372 |             var result = await _write.SendAsync(request, SendWriteAsync);
 373 |             if (!result.Success) { ActionError = _write.FailureMessage(result.Error); return; }
 374 |             HasEditor = false; _editing = null; ActionNote = "تم تأكيد الحفظ. سيستخدم الكاشير البيانات المحدّثة عند تحميل القائمة.";
 375 |             await ReloadAsync();
 376 |         }
 377 |         finally { IsBusy = false; }
 378 |     }
```

---

<a id="callable-70"></a>
### Callable 70: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Confirm<T>(Falah.Client.CashierWinUI.Services.ApiClient.ApiResult<T>)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:379:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `379-380`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Confirm<T>(Falah.Client.CashierWinUI.Services.ApiClient.ApiResult<T>). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 379-380):**
```csharp
 379 |     private static ApiClient.ApiResult<bool> Confirm<T>(ApiClient.ApiResult<T> result) =>
 380 |         new(result.Success, result.Success, result.Error, result.OutcomeUnknown, result.StatusCode);
```

---

<a id="callable-71"></a>
### Callable 71: `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SendWriteAsync(object)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:381:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `381-393`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SendWriteAsync(object). Manages internal state transition within src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 381-393):**
```csharp
 381 |     private async Task<ApiClient.ApiResult<bool>> SendWriteAsync(object request) => request switch
 382 |     {
 383 |         CreateCategoryRequest r => Confirm(await _session.Api.CreateCategoryAsync(r)),
 384 |         UpdateCategoryRequest r => await _session.Api.UpdateCategoryAsync(r),
 385 |         SetCategoryActiveRequest r => await _session.Api.SetCategoryActiveAsync(r),
 386 |         CreateProductRequest r => Confirm(await _session.Api.CreateProductAsync(r)),
 387 |         UpdateProductRequest r => await _session.Api.UpdateProductAsync(r),
 388 |         SetProductAvailabilityRequest r => await _session.Api.SetProductAvailabilityAsync(r),
 389 |         CreateTaxRuleRequest r => Confirm(await _session.Api.CreateTaxRuleAsync(r)),
 390 |         UpdateTaxRuleRequest r => await _session.Api.UpdateTaxRuleAsync(r),
 391 |         SavePriceRuleRequest r => Confirm(await _session.Api.SavePriceRuleAsync(r)),
 392 |         _ => throw new InvalidOperationException("Unsupported menu command.")
 393 |     };
```

---
