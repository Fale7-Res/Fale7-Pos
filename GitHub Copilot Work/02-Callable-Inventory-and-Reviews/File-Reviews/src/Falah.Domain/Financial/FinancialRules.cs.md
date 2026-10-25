# Deep Per-Callable Source Audit: `FinancialRules.cs`

- **File Path:** [`src/Falah.Domain/Financial/FinancialRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/FinancialRules.cs)
- **Source SHA-256:** `0c48f0fef3792f1bd9e85023ad85978ae8583f9f7d2e2bcaa65f3159aef1b926`
- **Role:** Pure Domain Financial Ledger, Tender Allocation, Sequence & Drawer Balance Rules
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T01:40:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 12 (7 Methods, 5 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### Callable: `src/Falah.Domain/Financial/FinancialRules.cs:13:5:MethodDeclaration`
- **Signature:** `public static void AssertSplitPaymentAllocation(IReadOnlyList<PaymentSplit> splits, Money total)`
- **Exact Line Range:** Lines 13–24
- **Behavior & Financial Math:**
  Enforces split payment mathematical and structural invariants:
  1. Validates `splits.Count >= 2`. A single tender method cannot be registered as a split.
  2. Asserts every split amount has `part.Amount.Piastres > 0`. Zero or negative amounts are rejected.
  3. Enforces unique tender methods across splits (`splits.Select(p => p.Method).Distinct().Count() == splits.Count`).
  4. Sums all split amounts in integer piastres: `var allocated = Money.FromPiastres(splits.Sum(p => p.Amount.Piastres));`.
  5. Asserts `allocated == total`. Rejects over-allocation and under-allocation with descriptive error.
- **Classification:** Domain invariant guard method.

---

### Callable: `src/Falah.Domain/Financial/FinancialRules.cs:16:24:SimpleLambdaExpression`
- **Signature:** `part => part.Amount.Piastres <= 0`
- **Exact Line Range:** Line 16
- **Behavior:**
  Validation predicate passed to `.Any(...)` detecting zero or negative split amounts.
- **Classification:** In-memory validation lambda.

---

### Callable: `src/Falah.Domain/Financial/FinancialRules.cs:18:27:SimpleLambdaExpression`
- **Signature:** `part => part.Method`
- **Exact Line Range:** Line 18
- **Behavior:**
  Selector lambda extracting `PaymentMethod` enum from `PaymentSplit` for distinct method verification.
- **Classification:** In-memory selector lambda.

---

### Callable: `src/Falah.Domain/Financial/FinancialRules.cs:21:55:SimpleLambdaExpression`
- **Signature:** `part => part.Amount.Piastres`
- **Exact Line Range:** Line 21
- **Behavior:**
  Selector lambda extracting `long` piastres from each `PaymentSplit` for summation.
- **Classification:** In-memory selector lambda.

---

### Callable: `src/Falah.Domain/Financial/FinancialRules.cs:30:5:MethodDeclaration`
- **Signature:** `public static long AllocateNextOrderSequence(long lastAllocated, long existingMax)`
- **Exact Line Range:** Lines 30–31
- **Behavior & Monotonic Sequence Logic:**
  `Math.Max(127, Math.Max(lastAllocated, existingMax)) + 1;`
  Guarantees monotonically increasing sequence numbers. Protects against sequence reuse or regression across server restarts when in-memory counters reset, anchoring to the higher of the in-memory counter or `existingMax` from persistent storage, with a historical minimum floor of 127.
- **Classification:** Monotonic sequence calculation method.

---

### Callable: `src/Falah.Domain/Financial/FinancialRules.cs:33:5:MethodDeclaration`
- **Signature:** `public static string FormatOrderNumber(long sequence)`
- **Exact Line Range:** Line 33
- **Behavior:**
  Formats a 4-digit zero-padded order display string (e.g. `128 -> "#0128"`).
- **Classification:** String formatting helper.

---

### Callable: `src/Falah.Domain/Financial/FinancialRules.cs:35:5:MethodDeclaration`
- **Signature:** `public static Money TreasuryBalanceFromMovements(IEnumerable<TreasuryMovement> movements, string accountId)`
- **Exact Line Range:** Lines 35–38
- **Behavior & Double-Entry Ledger Derivation:**
  Derives account balance purely from immutable posted treasury movements:
  - Filters `movements.Where(m => m.Status == MovementStatus.Posted)`.
  - Aggregates:
    - Adds `m.Amount` when `m.DestinationAccountId == accountId` (inflows).
    - Subtracts `m.Amount` when `m.SourceAccountId == accountId` (outflows).
  - Starts from `Money.Zero`.
- **Classification:** Functional ledger reduction method.

---

### Callable: `src/Falah.Domain/Financial/FinancialRules.cs:36:25:SimpleLambdaExpression`
- **Signature:** `m => m.Status == MovementStatus.Posted`
- **Exact Line Range:** Line 36
- **Behavior:**
  Filter predicate ensuring only posted (settled) movements contribute to account balances; pending or cancelled movements are excluded.
- **Classification:** In-memory filter lambda.

---

### Callable: `src/Falah.Domain/Financial/FinancialRules.cs:36:87:ParenthesizedLambdaExpression`
- **Signature:** `(sum, m) => sum + (m.DestinationAccountId == accountId ? m.Amount : Money.Zero) - (m.SourceAccountId == accountId ? m.Amount : Money.Zero)`
- **Exact Line Range:** Line 36
- **Behavior:**
  Accumulator lambda passed to `IEnumerable.Aggregate` implementing double-entry debit/credit delta summation.
- **Classification:** In-memory reduction lambda.

---

### Callable: `src/Falah.Domain/Financial/FinancialRules.cs:40:5:MethodDeclaration`
- **Signature:** `public static Money CalculateExpectedDrawerCash(Money openingCash, Money cashSales, Money drawerExpenses, Money cashIn, Money cashOut)`
- **Exact Line Range:** Lines 40–42
- **Behavior & Shift Cash Reconciliation Formula:**
  Calculates theoretical drawer balance:
  `openingCash + cashSales + cashIn - drawerExpenses - cashOut;`
- **Architectural & Formula Analysis:**
  - Notice the formula arguments: `openingCash`, `cashSales`, `drawerExpenses`, `cashIn`, `cashOut`.
  - Notice that Cash Refunds and Safe Drops are not separate parameters.
  - In [`ShiftReconciliation.cs:33-35`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs#L33-L35), the caller accommodates this by subtracting refunds directly from `cashSales` (`cashSales - refundTotal`), and grouping safe drops into `cashOut`.
  - However, as proven in `FIN-004`, `ShiftReconciliation.cs` queries refunds by `r.ShiftId == shiftId` instead of `r.OperationalShiftId == shiftId`, resulting in cross-shift refunds being omitted from `refundTotal`!
- **Classification:** Pure financial calculation method.

---

### Callable: `src/Falah.Domain/Financial/FinancialRules.cs:44:5:MethodDeclaration`
- **Signature:** `public static bool DiscrepancyNeedsApproval(Money difference, Money threshold)`
- **Exact Line Range:** Lines 44–48
- **Behavior:**
  Floors threshold at zero (`var flooredThreshold = threshold.IsNegative ? Money.Zero : threshold;`) and evaluates `difference.Abs() > flooredThreshold`.
  Determines whether a blind close cash discrepancy exceeds the permitted tolerance, mandating manager approval.
- **Classification:** Policy decision predicate.

---

### Callable: `src/Falah.Domain/Financial/FinancialRules.cs:52:5:MethodDeclaration`
- **Signature:** `public static void AssertProtectedSourceBalance(IEnumerable<TreasuryMovement> movements, string sourceAccountId, Money amount)`
- **Exact Line Range:** Lines 52–57
- **Behavior & Overdraft Protection:**
  Checks whether `sourceAccountId` is in `TreasuryAccounts.Protected` (`tre-main`, `tre-drawer`, `tre-bank`, `tre-instapay`). If protected, calculates balance via `TreasuryBalanceFromMovements` and asserts `balance >= amount`. Throws `DomainException("رصيد حساب المصدر غير كافٍ لتنفيذ الحركة.")` if insufficient.
- **Classification:** Source account balance protection guard.

---

## Comprehensive Vulnerability Summary & Fix Recommendations

| Finding ID | Severity | Root Cause Code Span | Impact | Recommended Remediation |
| :--- | :--- | :--- | :--- | :--- |
| **`FIN-RECON-DRAWER-001`** | **MEDIUM** | `FinancialRules.cs:40-42` | `CalculateExpectedDrawerCash` signature lacks explicit `refunds` and `safeDrops` parameters, forcing callers to net them into `cashSales` and `cashOut`, creating reconciliation ambiguity. | Refactor signature to explicitly accept `Money cashRefunds` and `Money safeDrops` for transparent drawer auditing. |


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:30:00Z
- **Source SHA-256 Verified:** `0c48f0fef3792f1bd9e85023ad85978ae8583f9f7d2e2bcaa65f3159aef1b926`
- **Callable ID Checked:** `src/Falah.Domain/Financial/FinancialRules.cs:13:5:MethodDeclaration`, `src/Falah.Domain/Financial/FinancialRules.cs:16:24:SimpleLambdaExpression`, `src/Falah.Domain/Financial/FinancialRules.cs:18:27:SimpleLambdaExpression`, `src/Falah.Domain/Financial/FinancialRules.cs:21:55:SimpleLambdaExpression`, `src/Falah.Domain/Financial/FinancialRules.cs:30:5:MethodDeclaration`, `src/Falah.Domain/Financial/FinancialRules.cs:33:5:MethodDeclaration`, `src/Falah.Domain/Financial/FinancialRules.cs:35:5:MethodDeclaration`, `src/Falah.Domain/Financial/FinancialRules.cs:36:25:SimpleLambdaExpression`, `src/Falah.Domain/Financial/FinancialRules.cs:36:87:ParenthesizedLambdaExpression`, `src/Falah.Domain/Financial/FinancialRules.cs:40:5:MethodDeclaration`, `src/Falah.Domain/Financial/FinancialRules.cs:44:5:MethodDeclaration`, `src/Falah.Domain/Financial/FinancialRules.cs:52:5:MethodDeclaration` (12 of 12 callables checked)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Domain/Financial/FinancialRules.cs` (59 lines).
  2. Verified `FinancialRules.cs:40-42` finding `FIN-RECON-DRAWER-001`: `CalculateExpectedDrawerCash` takes strictly `(Money openingCash, Money cashSales, Money drawerExpenses, Money cashIn, Money cashOut)`. It lacks explicit parameters for refunds or safe drops. Callers in `ShiftReconciliation.cs:33-35` net refunds directly into `cashSales` (`cashSales - refundTotal`) and safe drops into `cashOut`, creating reconciliation ambiguity and contributing directly to the cross-shift refund omission defect (`FIN-004`).
  3. Verified `AssertSplitPaymentAllocation` at lines 13-24 strictly validates count >= 2, non-zero positive amounts, distinct payment methods, and exact allocation match to total.
  4. Verified `AllocateNextOrderSequence` at lines 30-31 strictly floors sequence allocation at `Math.Max(127, Math.Max(lastAllocated, existingMax)) + 1`.
  5. Verified `TreasuryBalanceFromMovements` double-entry ledger logic and `AssertProtectedSourceBalance` overdraft protection.
  6. All 12 canonical callable IDs, method signatures, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:32:35Z
- **Source SHA-256 Verified:** `0c48f0fef3792f1bd9e85023ad85978ae8583f9f7d2e2bcaa65f3159aef1b926`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `0c48f0fef3792f1bd9e85023ad85978ae8583f9f7d2e2bcaa65f3159aef1b926` against `src/Falah.Domain/Financial/FinancialRules.cs` (59 lines).
2. Concur with L1 analysis on `FIN-RECON-DRAWER-001` in `CalculateExpectedDrawerCash` (lines 40-42) where omission of dedicated refund and safe drop parameters induces netting into `cashSales` and `cashOut` by downstream callers (`ShiftReconciliation.cs`).
3. Additional L2 Challenger Assessment:
   - `AllocateNextOrderSequence` (lines 30-31) hardcodes legacy floor at 127 (`Math.Max(127, Math.Max(lastAllocated, existingMax)) + 1`). This is purely algorithmic and thread-unsafe unless callers serialize sequence allocation inside database transactions.
   - `DiscrepancyNeedsApproval` (lines 44-48) correctly handles negative thresholds by flooring at zero (`flooredThreshold = threshold.IsNegative ? Money.Zero : threshold`), and strictly triggers manager approval when `difference.Abs() > flooredThreshold`. At exact threshold equality, approval is not required.
   - Double-entry ledger summation in `TreasuryBalanceFromMovements` and `AssertProtectedSourceBalance` verified against `TreasuryAccounts.Protected`.
4. All 12 callables rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
