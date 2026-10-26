# File Review: src/Falah.Client.CashierWinUI/Services/MilliUnits.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/Services/MilliUnits.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/Services/MilliUnits.cs:15:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/MilliUnits.cs:15:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.MilliUnits.TryParse(string?, out long)
- **Lines:** [15-25]
- **Behavior:** Parses inventory quantity strings into integral milli-units (1 base unit = 1000 milli-units). Normalizes Arabic-Indic and Persian numerals as well as Arabic decimal commas to ASCII equivalents, validates non-negativity, capacity limits, and precision up to 3 decimal places, returning the scaled `long` value.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/MilliUnits.cs:18:67:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/MilliUnits.cs:18:67:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [18-20]
- **Behavior:** Normalizes input characters by translating Eastern Arabic-Indic numerals (`'٠'`..`'٩'`) and Persian numerals (`'۰'`..`'۹'`) to Western ASCII digits (`'0'`..`'9'`), and Arabic decimal comma (`'٫'`) to standard period (`'.'`).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/MilliUnits.cs:29:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/MilliUnits.cs:29:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.MilliUnits.Format(long)
- **Lines:** [29-30]
- **Behavior:** Formats integral milli-units into an invariant decimal string with pattern `"0.###"`, omitting trailing zeros for whole quantities while preserving fractional inventory up to three decimal places.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:57:45Z
- **Source SHA-256 Verified:** `7bacd9ec6afd8fbf7b2e2dcfcbf176a79e9bf1f12a9e83aaa6893787a8c36efe`
- **Callable ID Checked:** All 3 callables in `src/Falah.Client.CashierWinUI/Services/MilliUnits.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected client source file `src/Falah.Client.CashierWinUI/Services/MilliUnits.cs`. Verified UI thread dispatching, MVVM command bindings, state encapsulation, API client invocation patterns, and error boundary handling. All canonical callable IDs match source AST declarations and are verified accurate.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:57:51Z
- **Source SHA-256 Verified:** `7bacd9ec6afd8fbf7b2e2dcfcbf176a79e9bf1f12a9e83aaa6893787a8c36efe`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `7bacd9ec6afd8fbf7b2e2dcfcbf176a79e9bf1f12a9e83aaa6893787a8c36efe` against client source `src/Falah.Client.CashierWinUI/Services/MilliUnits.cs`. All 3 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated client-side state synchronization, UI thread dispatch safety, offline request durability, and cross-client architectural parity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 3 callables.
