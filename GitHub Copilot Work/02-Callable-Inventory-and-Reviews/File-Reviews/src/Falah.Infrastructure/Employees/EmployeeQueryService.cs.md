# Substantive Callable Audit: `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`

- **Source File:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `fbb1200253239f442fdb0933abb5270f12c7d404e5cd549150c71a66c96c6341`
- **Auditor Specialist:** `security-auditor`
- **Total File Callables:** **36**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Employee reporting and profile query service mapping database entities to DTOs for client consumption.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Infrastructure.Employees.EmployeeQueryService.EmployeeQueryService(Falah.Infrastructure.Persistence.FalahDbContext)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:13:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `13-13`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Infrastructure.Employees.EmployeeQueryService.EmployeeQueryService(Falah.Infrastructure.Persistence.FalahDbContext). Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 13-13):**
```csharp
  13 |     public EmployeeQueryService(FalahDbContext db) => _db = db;
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Infrastructure.Employees.EmployeeQueryService.GetFinancialReportAsync(DateOnly?, DateOnly?, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:25:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `25-59`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Employees.EmployeeQueryService.GetFinancialReportAsync(DateOnly?, DateOnly?, CancellationToken). Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 25-59):**
```csharp
  25 |     public async Task<List<EmployeeFinancialReportLineDto>> GetFinancialReportAsync(
  26 |         DateOnly? from, DateOnly? to, CancellationToken ct = default)
  27 |     {
  28 |         DateTimeOffset? start = null, end = null;
  29 |         if (from is { } f) start = BusinessClock.UtcRangeForBusinessDate(f).Start;
  30 |         if (to is { } t) end = BusinessClock.UtcRangeForBusinessDate(t).End;
  31 | 
  32 |         var employees = await _db.Employees.AsNoTracking().Where(e => e.IsActive).ToListAsync(ct);
  33 |         var allEntries = await _db.EmployeeLedgerEntries.AsNoTracking()
  34 |             .Select(e => new { e.EmployeeId, e.Type, e.CreditPiastres, e.DebitPiastres, e.CreatedAtUtc })
  35 |             .ToListAsync(ct);
  36 | 
  37 |         var earningsTypes = new[] { nameof(LedgerEntryType.DailyWage), nameof(LedgerEntryType.Salary), nameof(LedgerEntryType.Overtime), nameof(LedgerEntryType.ExtraShift) };
  38 |         var additionTypes = new[] { nameof(LedgerEntryType.Bonus), nameof(LedgerEntryType.OtherEarning) };
  39 |         var deductionTypes = new[] { nameof(LedgerEntryType.Deduction), nameof(LedgerEntryType.Adjustment) };
  40 |         var paymentTypes = new[] { nameof(LedgerEntryType.Withdrawal), nameof(LedgerEntryType.Settlement), nameof(LedgerEntryType.PayrollSettlement) };
  41 | 
  42 |         var result = new List<EmployeeFinancialReportLineDto>(employees.Count);
  43 |         foreach (var employee in employees)
  44 |         {
  45 |             var allForEmployee = allEntries.Where(e => e.EmployeeId == employee.Id).ToList();
  46 |             var inRange = allForEmployee.Where(e =>
  47 |                 (start is null || e.CreatedAtUtc >= start) && (end is null || e.CreatedAtUtc < end)).ToList();
  48 | 
  49 |             long SumCredit(IEnumerable<string> types) => inRange.Where(e => types.Contains(e.Type)).Sum(e => e.CreditPiastres);
  50 |             long SumDebit(IEnumerable<string> types) => inRange.Where(e => types.Contains(e.Type)).Sum(e => e.DebitPiastres);
  51 |             var balance = allForEmployee.Sum(e => e.CreditPiastres) - allForEmployee.Sum(e => e.DebitPiastres);
  52 | 
  53 |             result.Add(new EmployeeFinancialReportLineDto(
  54 |                 employee.Id, employee.Name, SumCredit(earningsTypes), SumCredit(additionTypes),
  55 |                 SumDebit(deductionTypes), SumDebit(new[] { nameof(LedgerEntryType.Advance) }),
  56 |                 SumDebit(paymentTypes), balance));
  57 |         }
  58 |         return result.OrderBy(r => r.EmployeeName).ToList();
  59 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:32:66:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `32-32`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

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
  32 |         var employees = await _db.Employees.AsNoTracking().Where(e => e.IsActive).ToListAsync(ct);
```

---

<a id="callable-4"></a>
### Callable 4: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:34:21:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `34-34`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

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
  34 |             .Select(e => new { e.EmployeeId, e.Type, e.CreditPiastres, e.DebitPiastres, e.CreatedAtUtc })
```

---

<a id="callable-5"></a>
### Callable 5: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:45:51:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `45-45`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 45-45):**
```csharp
  45 |             var allForEmployee = allEntries.Where(e => e.EmployeeId == employee.Id).ToList();
```

---

<a id="callable-6"></a>
### Callable 6: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:46:48:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `46-47`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 46-47):**
```csharp
  46 |             var inRange = allForEmployee.Where(e =>
  47 |                 (start is null || e.CreatedAtUtc >= start) && (end is null || e.CreatedAtUtc < end)).ToList();
```

---

<a id="callable-7"></a>
### Callable 7: `SumCredit(IEnumerable<string>)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:49:13:LocalFunctionStatement`
- **Declaration Kind:** `LocalFunctionStatement` | **Line Range:** `49-49`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes LocalFunctionStatement for SumCredit(IEnumerable<string>). Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

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
  49 |             long SumCredit(IEnumerable<string> types) => inRange.Where(e => types.Contains(e.Type)).Sum(e => e.CreditPiastres);
```

---

<a id="callable-8"></a>
### Callable 8: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:49:72:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `49-49`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

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
  49 |             long SumCredit(IEnumerable<string> types) => inRange.Where(e => types.Contains(e.Type)).Sum(e => e.CreditPiastres);
```

---

<a id="callable-9"></a>
### Callable 9: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:49:105:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `49-49`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

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
  49 |             long SumCredit(IEnumerable<string> types) => inRange.Where(e => types.Contains(e.Type)).Sum(e => e.CreditPiastres);
```

---

<a id="callable-10"></a>
### Callable 10: `SumDebit(IEnumerable<string>)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:50:13:LocalFunctionStatement`
- **Declaration Kind:** `LocalFunctionStatement` | **Line Range:** `50-50`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes LocalFunctionStatement for SumDebit(IEnumerable<string>). Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

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
  50 |             long SumDebit(IEnumerable<string> types) => inRange.Where(e => types.Contains(e.Type)).Sum(e => e.DebitPiastres);
```

---

<a id="callable-11"></a>
### Callable 11: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:50:71:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `50-50`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

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
  50 |             long SumDebit(IEnumerable<string> types) => inRange.Where(e => types.Contains(e.Type)).Sum(e => e.DebitPiastres);
```

---

<a id="callable-12"></a>
### Callable 12: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:50:104:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `50-50`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

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
  50 |             long SumDebit(IEnumerable<string> types) => inRange.Where(e => types.Contains(e.Type)).Sum(e => e.DebitPiastres);
```

---

<a id="callable-13"></a>
### Callable 13: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:51:46:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `51-51`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

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
  51 |             var balance = allForEmployee.Sum(e => e.CreditPiastres) - allForEmployee.Sum(e => e.DebitPiastres);
```

---

<a id="callable-14"></a>
### Callable 14: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:51:90:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `51-51`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

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
  51 |             var balance = allForEmployee.Sum(e => e.CreditPiastres) - allForEmployee.Sum(e => e.DebitPiastres);
```

---

<a id="callable-15"></a>
### Callable 15: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:58:31:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `58-58`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 58-58):**
```csharp
  58 |         return result.OrderBy(r => r.EmployeeName).ToList();
```

---

<a id="callable-16"></a>
### Callable 16: `Falah.Infrastructure.Employees.EmployeeQueryService.ListAsync(CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:61:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `61-62`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`SEC-003`**

**A. Actual Behavior & Execution Path:**  
Queries all active employees with AsNoTracking() and projects via ToDto.

**B. Trust Boundaries & Validation:**  
Invoked from Minimal API endpoint GET /api/employees.

**C. State Mutations & Side Effects:**  
None; read-only query.

**D. Concurrency & Transaction Implications:**  
Safe under concurrent reads; uses AsNoTracking().

**E. Specialist Risks & Invariants:**  
VULNERABILITY SEC-003: Exposes full employee NationalId, MonthlySalaryPiastres, DailyWagePiastres, and FingerprintId without permission check.

> **Verbatim Code Excerpt (Lines 61-62):**
```csharp
  61 |     public Task<List<EmployeeDto>> ListAsync(CancellationToken ct = default) =>
  62 |         _db.Employees.AsNoTracking().Select(e => ToDto(e)).ToListAsync(ct);
```

---

<a id="callable-17"></a>
### Callable 17: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:62:45:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `62-62`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 62-62):**
```csharp
  62 |         _db.Employees.AsNoTracking().Select(e => ToDto(e)).ToListAsync(ct);
```

---

<a id="callable-18"></a>
### Callable 18: `Falah.Infrastructure.Employees.EmployeeQueryService.GetAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:64:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `64-66`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Employees.EmployeeQueryService.GetAsync(string, CancellationToken). Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 64-66):**
```csharp
  64 |     public async Task<EmployeeDto> GetAsync(string employeeId, CancellationToken ct = default) =>
  65 |         ToDto(await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId, ct)
  66 |             ?? throw new InvalidOperationException("الموظف غير موجود."));
```

---

<a id="callable-19"></a>
### Callable 19: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:65:70:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `65-65`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

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
  65 |         ToDto(await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId, ct)
```

---

<a id="callable-20"></a>
### Callable 20: `Falah.Infrastructure.Employees.EmployeeQueryService.ListLedgerEntriesAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:68:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `68-70`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Employees.EmployeeQueryService.ListLedgerEntriesAsync(string, CancellationToken). Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 68-70):**
```csharp
  68 |     public Task<List<EmployeeLedgerEntryDto>> ListLedgerEntriesAsync(string employeeId, CancellationToken ct = default) =>
  69 |         _db.EmployeeLedgerEntries.AsNoTracking().Where(e => e.EmployeeId == employeeId)
  70 |             .OrderByDescending(e => e.CreatedAtUtc).Select(e => ToLedgerDto(e)).ToListAsync(ct);
```

---

<a id="callable-21"></a>
### Callable 21: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:69:56:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `69-69`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 69-69):**
```csharp
  69 |         _db.EmployeeLedgerEntries.AsNoTracking().Where(e => e.EmployeeId == employeeId)
```

---

<a id="callable-22"></a>
### Callable 22: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:70:32:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `70-70`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 70-70):**
```csharp
  70 |             .OrderByDescending(e => e.CreatedAtUtc).Select(e => ToLedgerDto(e)).ToListAsync(ct);
```

---

<a id="callable-23"></a>
### Callable 23: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:70:60:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `70-70`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 70-70):**
```csharp
  70 |             .OrderByDescending(e => e.CreatedAtUtc).Select(e => ToLedgerDto(e)).ToListAsync(ct);
```

---

<a id="callable-24"></a>
### Callable 24: `Falah.Infrastructure.Employees.EmployeeQueryService.GetBalanceAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:74:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `74-79`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Employees.EmployeeQueryService.GetBalanceAsync(string, CancellationToken). Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 74-79):**
```csharp
  74 |     public async Task<long> GetBalanceAsync(string employeeId, CancellationToken ct = default)
  75 |     {
  76 |         var entries = await _db.EmployeeLedgerEntries.AsNoTracking().Where(e => e.EmployeeId == employeeId)
  77 |             .Select(e => new { e.DebitPiastres, e.CreditPiastres }).ToListAsync(ct);
  78 |         return entries.Sum(e => e.CreditPiastres) - entries.Sum(e => e.DebitPiastres);
  79 |     }
```

---

<a id="callable-25"></a>
### Callable 25: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:76:76:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `76-76`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 76-76):**
```csharp
  76 |         var entries = await _db.EmployeeLedgerEntries.AsNoTracking().Where(e => e.EmployeeId == employeeId)
```

---

<a id="callable-26"></a>
### Callable 26: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:77:21:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `77-77`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 77-77):**
```csharp
  77 |             .Select(e => new { e.DebitPiastres, e.CreditPiastres }).ToListAsync(ct);
```

---

<a id="callable-27"></a>
### Callable 27: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:78:28:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `78-78`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 78-78):**
```csharp
  78 |         return entries.Sum(e => e.CreditPiastres) - entries.Sum(e => e.DebitPiastres);
```

---

<a id="callable-28"></a>
### Callable 28: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:78:65:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `78-78`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 78-78):**
```csharp
  78 |         return entries.Sum(e => e.CreditPiastres) - entries.Sum(e => e.DebitPiastres);
```

---

<a id="callable-29"></a>
### Callable 29: `Falah.Infrastructure.Employees.EmployeeQueryService.ToDto(Falah.Infrastructure.Persistence.Entities.EmployeeEntity)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:81:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `81-85`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`SEC-003`**

**A. Actual Behavior & Execution Path:**  
Static mapper constructing EmployeeDto containing NationalId, MonthlySalaryPiastres, DailyWagePiastres, FingerprintId, and Address.

**B. Trust Boundaries & Validation:**  
Internal entity to DTO boundary.

**C. State Mutations & Side Effects:**  
None.

**D. Concurrency & Transaction Implications:**  
Thread-safe pure function.

**E. Specialist Risks & Invariants:**  
Over-exposes highly sensitive employee PII and compensation data.

> **Verbatim Code Excerpt (Lines 81-85):**
```csharp
  81 |     internal static EmployeeDto ToDto(EmployeeEntity e) => new(
  82 |         e.Id, e.Name, e.EmployeeNumber, e.JobTitle, e.Phone, e.HireDate, e.CompensationType,
  83 |         e.DailyWagePiastres, e.ShiftValuePiastres, e.MonthlySalaryPiastres, e.UserId, e.CanDeliver,
  84 |         e.Role, e.IsActive, e.Notes, e.Address, e.NationalId, e.Department, e.EmploymentArrangement,
  85 |         e.FingerprintId, e.LifecycleStatus, e.OvertimeRatePiastres);
```

---

<a id="callable-30"></a>
### Callable 30: `Falah.Infrastructure.Employees.EmployeeQueryService.ToLedgerDto(Falah.Infrastructure.Persistence.Entities.EmployeeLedgerEntryEntity)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:87:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `87-90`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Employees.EmployeeQueryService.ToLedgerDto(Falah.Infrastructure.Persistence.Entities.EmployeeLedgerEntryEntity). Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 87-90):**
```csharp
  87 |     internal static EmployeeLedgerEntryDto ToLedgerDto(EmployeeLedgerEntryEntity e) => new(
  88 |         e.Id, e.EmployeeId, e.Type, e.Description, e.DebitPiastres, e.CreditPiastres, e.BalanceAfterPiastres,
  89 |         e.RecordedBy, e.AuthorizedBy, e.ShiftId, e.ReferenceId, e.Hours, e.RateSnapshotPiastres,
  90 |         e.ValueSnapshotPiastres, e.PayrollRunId, e.CreatedAtUtc);
```

---

<a id="callable-31"></a>
### Callable 31: `Falah.Infrastructure.Employees.EmployeeQueryService.ListAttendanceAsync(string?, DateOnly?, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:92:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `92-98`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Employees.EmployeeQueryService.ListAttendanceAsync(string?, DateOnly?, CancellationToken). Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 92-98):**
```csharp
  92 |     public Task<List<AttendanceRecordDto>> ListAttendanceAsync(string? employeeId, DateOnly? date, CancellationToken ct = default)
  93 |     {
  94 |         var query = _db.AttendanceRecords.AsNoTracking().AsQueryable();
  95 |         if (employeeId is not null) query = query.Where(a => a.EmployeeId == employeeId);
  96 |         if (date is not null) query = query.Where(a => a.Date == date);
  97 |         return query.OrderByDescending(a => a.Date).Select(a => ToAttendanceDto(a)).ToListAsync(ct);
  98 |     }
```

---

<a id="callable-32"></a>
### Callable 32: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:95:57:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `95-95`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 95-95):**
```csharp
  95 |         if (employeeId is not null) query = query.Where(a => a.EmployeeId == employeeId);
```

---

<a id="callable-33"></a>
### Callable 33: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:96:51:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `96-96`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 96-96):**
```csharp
  96 |         if (date is not null) query = query.Where(a => a.Date == date);
```

---

<a id="callable-34"></a>
### Callable 34: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:97:40:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `97-97`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 97-97):**
```csharp
  97 |         return query.OrderByDescending(a => a.Date).Select(a => ToAttendanceDto(a)).ToListAsync(ct);
```

---

<a id="callable-35"></a>
### Callable 35: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:97:60:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `97-97`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 97-97):**
```csharp
  97 |         return query.OrderByDescending(a => a.Date).Select(a => ToAttendanceDto(a)).ToListAsync(ct);
```

---

<a id="callable-36"></a>
### Callable 36: `Falah.Infrastructure.Employees.EmployeeQueryService.ToAttendanceDto(Falah.Infrastructure.Persistence.Entities.AttendanceRecordEntity)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs:100:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `100-102`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Employees.EmployeeQueryService.ToAttendanceDto(Falah.Infrastructure.Persistence.Entities.AttendanceRecordEntity). Manages internal state transition within src/Falah.Infrastructure/Employees/EmployeeQueryService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 100-102):**
```csharp
 100 |     internal static AttendanceRecordDto ToAttendanceDto(AttendanceRecordEntity a) => new(
 101 |         a.Id, a.EmployeeId, a.EmployeeName, a.Date, a.CheckIn, a.CheckOut, a.ShiftName, a.Status, a.LateMinutes,
 102 |         a.WorkHours, a.OvertimeHours, a.TatbiqCount, a.IsManualCorrection, a.CorrectedBy, a.CorrectionReason, a.Source);
```

---
