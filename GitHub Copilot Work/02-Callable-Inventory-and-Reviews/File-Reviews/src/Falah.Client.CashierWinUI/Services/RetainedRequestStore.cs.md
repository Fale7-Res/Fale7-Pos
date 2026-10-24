# Substantive Callable Audit: `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs`

- **Source File:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `25899a9e6b6ad147bd61d624236829f21723de49274041d0f7c40b69819a2303`
- **Auditor Specialist:** `security-auditor`
- **Total File Callables:** **14**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Client offline durability store writing in-flight requests to %LOCALAPPDATA% before HTTP dispatch to guarantee crash and power-loss recovery on POS terminals.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Client.CashierWinUI.Services.IRetainedRequestStore.Save(string, object)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:19:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `19-19`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.Services.IRetainedRequestStore.Save(string, object). Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 19-19):**
```csharp
  19 |     void Save(string ownerKey, object request);
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Client.CashierWinUI.Services.IRetainedRequestStore.Clear(string)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:20:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `20-20`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.Services.IRetainedRequestStore.Clear(string). Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 20-20):**
```csharp
  20 |     void Clear(string ownerKey);
```

---

<a id="callable-3"></a>
### Callable 3: `Falah.Client.CashierWinUI.Services.IRetainedRequestStore.Load(string)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:25:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `25-25`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.Services.IRetainedRequestStore.Load(string). Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 25-25):**
```csharp
  25 |     object? Load(string ownerKey);
```

---

<a id="callable-4"></a>
### Callable 4: `Falah.Client.CashierWinUI.Services.IRetainedRequestStore.List()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:27:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `27-27`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.Services.IRetainedRequestStore.List(). Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 27-27):**
```csharp
  27 |     IReadOnlyList<RetainedRequestRecord> List();
```

---

<a id="callable-5"></a>
### Callable 5: `Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.FileRetainedRequestStore(string, Action<Exception>?)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:40:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `40-45`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.FileRetainedRequestStore(string, Action<Exception>?). Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 40-45):**
```csharp
  40 |     public FileRetainedRequestStore(string directory, Action<Exception>? onError = null)
  41 |     {
  42 |         _directory = directory;
  43 |         _onError = onError;
  44 |         Directory.CreateDirectory(_directory);
  45 |     }
```

---

<a id="callable-6"></a>
### Callable 6: `Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.DirectoryFor(string)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:49:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `49-51`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.DirectoryFor(string). Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 49-51):**
```csharp
  49 |     public static string DirectoryFor(string station) => Path.Combine(
  50 |         Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
  51 |         "Falah", "pending-operations", station);
```

---

<a id="callable-7"></a>
### Callable 7: `Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.PathFor(string)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:53:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `53-57`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.PathFor(string). Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 53-57):**
```csharp
  53 |     private string PathFor(string ownerKey)
  54 |     {
  55 |         var safe = string.Concat(ownerKey.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
  56 |         return Path.Combine(_directory, safe + ".json");
  57 |     }
```

---

<a id="callable-8"></a>
### Callable 8: `lambda expression`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:55:50:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `55-55`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 55-55):**
```csharp
  55 |         var safe = string.Concat(ownerKey.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
```

---

<a id="callable-9"></a>
### Callable 9: `Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.Save(string, object)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:59:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `59-82`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`SEC-002`**

**A. Actual Behavior & Execution Path:**  
Packs request into anonymous envelope with Type, SavedAtUtc, and Payload. Serializes to temp JSON file and moves to final path.

**B. Trust Boundaries & Validation:**  
Receives command DTOs containing user credentials and tender payments from client ViewModels.

**C. State Mutations & Side Effects:**  
Writes plaintext .json file to station LocalApplicationData folder.

**D. Concurrency & Transaction Implications:**  
File system writes are atomic via File.Move replace.

**E. Specialist Risks & Invariants:**  
VULNERABILITY SEC-002: Serializes user PINs (InitialPin, CurrentPin, NewPin) in unencrypted plaintext on disk.

> **Verbatim Code Excerpt (Lines 59-82):**
```csharp
  59 |     public void Save(string ownerKey, object request)
  60 |     {
  61 |         try
  62 |         {
  63 |             var envelope = new
  64 |             {
  65 |                 Type = request.GetType().AssemblyQualifiedName,
  66 |                 SavedAtUtc = DateTimeOffset.UtcNow,
  67 |                 Payload = request,
  68 |             };
  69 |             // Written whole, then moved into place: a station losing power mid-write must never leave
  70 |             // a half-written file that reads back as a different request than the one submitted.
  71 |             var path = PathFor(ownerKey);
  72 |             var temp = path + ".tmp";
  73 |             File.WriteAllText(temp, JsonSerializer.Serialize(envelope, Options));
  74 |             File.Move(temp, path, overwrite: true);
  75 |         }
  76 |         catch (Exception ex)
  77 |         {
  78 |             // Losing the ability to persist must not stop the operation itself - the in-memory
  79 |             // retention still protects this session, which is what the behaviour was before.
  80 |             _onError?.Invoke(ex);
  81 |         }
  82 |     }
```

---

<a id="callable-10"></a>
### Callable 10: `Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.Clear(string)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:84:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `84-92`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.Clear(string). Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 84-92):**
```csharp
  84 |     public void Clear(string ownerKey)
  85 |     {
  86 |         try
  87 |         {
  88 |             var path = PathFor(ownerKey);
  89 |             if (File.Exists(path)) File.Delete(path);
  90 |         }
  91 |         catch (Exception ex) { _onError?.Invoke(ex); }
  92 |     }
```

---

<a id="callable-11"></a>
### Callable 11: `Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.Load(string)`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:94:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `94-113`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.Load(string). Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 94-113):**
```csharp
  94 |     public object? Load(string ownerKey)
  95 |     {
  96 |         try
  97 |         {
  98 |             var path = PathFor(ownerKey);
  99 |             if (!File.Exists(path)) return null;
 100 |             var envelope = JsonSerializer.Deserialize<Envelope>(File.ReadAllText(path), Options);
 101 |             if (envelope is null) return null;
 102 |             var type = Type.GetType(envelope.Type);
 103 |             // A request type that no longer exists cannot be re-sent; leaving the file in place would
 104 |             // strand the screen, so it is dropped and the operator sees no false pending state.
 105 |             if (type is null) { Clear(ownerKey); return null; }
 106 |             return envelope.Payload.Deserialize(type, Options);
 107 |         }
 108 |         catch (Exception ex)
 109 |         {
 110 |             _onError?.Invoke(ex);
 111 |             return null;
 112 |         }
 113 |     }
```

---

<a id="callable-12"></a>
### Callable 12: `Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.List()`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:115:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `115-136`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.List(). Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 115-136):**
```csharp
 115 |     public IReadOnlyList<RetainedRequestRecord> List()
 116 |     {
 117 |         try
 118 |         {
 119 |             var records = new List<RetainedRequestRecord>();
 120 |             foreach (var path in Directory.EnumerateFiles(_directory, "*.json"))
 121 |             {
 122 |                 var envelope = JsonSerializer.Deserialize<Envelope>(File.ReadAllText(path), Options);
 123 |                 if (envelope is null) continue;
 124 |                 records.Add(new RetainedRequestRecord(
 125 |                     Path.GetFileNameWithoutExtension(path),
 126 |                     envelope.Type.Split(',')[0].Split('.').Last(),
 127 |                     envelope.SavedAtUtc));
 128 |             }
 129 |             return records;
 130 |         }
 131 |         catch (Exception ex)
 132 |         {
 133 |             _onError?.Invoke(ex);
 134 |             return Array.Empty<RetainedRequestRecord>();
 135 |         }
 136 |     }
```

---

<a id="callable-13"></a>
### Callable 13: `Falah.Client.CashierWinUI.Services.RetainedRequestStorage.Current.get`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:145:52:GetAccessorDeclaration`
- **Declaration Kind:** `GetAccessorDeclaration` | **Line Range:** `145-145`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes GetAccessorDeclaration for Falah.Client.CashierWinUI.Services.RetainedRequestStorage.Current.get. Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 145-145):**
```csharp
 145 |     public static IRetainedRequestStore? Current { get; set; }
```

---

<a id="callable-14"></a>
### Callable 14: `Falah.Client.CashierWinUI.Services.RetainedRequestStorage.Current.set`
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs:145:57:SetAccessorDeclaration`
- **Declaration Kind:** `SetAccessorDeclaration` | **Line Range:** `145-145`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SetAccessorDeclaration for Falah.Client.CashierWinUI.Services.RetainedRequestStorage.Current.set. Manages internal state transition within src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 145-145):**
```csharp
 145 |     public static IRetainedRequestStore? Current { get; set; }
```

---
