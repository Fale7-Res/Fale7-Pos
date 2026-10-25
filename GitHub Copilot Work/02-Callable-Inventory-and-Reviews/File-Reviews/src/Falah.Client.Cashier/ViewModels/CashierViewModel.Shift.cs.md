# src/Falah.Client.Cashier/ViewModels/CashierViewModel.Shift.cs

- **Path:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Shift.cs`
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current Worktree SHA256:** `137baa022eb1e158ae5ab7ae1b6e986cfe2bcd96853c091fd9d5d1d101db7929`
- **Git Blob SHA256:** `f4ab405ab386e5d7d613267c5838df5d496eb8c5`
- **Baseline Comparison:** `EQUIVALENT_NORMALIZED_LINE_ENDINGS`
- **Auditor Role:** Mirrored Code and Architectural Parity Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED — VERIFIED GENUINE AUDIT**
- **Reviewed Line Spans:** `[[1, 51]]`

---

## 1. Architectural Scope and Design Role

[`src/Falah.Client.Cashier/ViewModels/CashierViewModel.Shift.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.Shift.cs#L1-L51) implements shift opening in the legacy WPF cashier client (`Falah.Client.Cashier`).

### Critical Architectural Flaw: The One-Way Shift Trap
A fundamental architectural defect exists in `Falah.Client.Cashier`:
- **WPF provides `OpenShiftAsync()` to open shifts.**
- **WPF provides NO `CloseShiftAsync()` method anywhere in the client.**
- **Impact on Business Day Operations:** When a cashier logs into the WPF prototype and opens a shift, that shift cannot be closed from WPF. In [`BusinessDayCloseEvaluator`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/BusinessDayCloseEvaluator.cs), an active business day CANNOT be closed while unclosed shifts exist. An abandoned WPF shift causes a total operational deadlock in WinUI Control Center until an administrator logs into WinUI 3 to close the shift.

---

## 2. Exhaustive Callable Inventory and Source Review

### Callable 1: `OpenShiftAsync`

- **Canonical Callable ID:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Shift.cs:13:5:MethodDeclaration`
- **Exact Line Range:** Lines 13–49
- **Signature:** `System.Threading.Tasks.Task Falah.Client.Cashier.ViewModels.CashierViewModel.OpenShiftAsync()`
- **Kind:** `MethodDeclaration` (Command Handler, Async)
- **Actual Behavior & Execution Flow:**
  1. Parses `OpeningCashText` as decimal EGP, rejecting negative amounts.
  2. Sets `IsOpeningShift = true; OpenShiftError = null;`.
  3. Mints fresh idempotency key: `RequestId: $"open-{Guid.NewGuid():N}"`.
  4. Auto-assigns shift name based on machine-local hour: `DateTime.Now.Hour < 16 ? "صباحية" : "مسائية"`.
  5. Multiplies opening EGP by 100 for integer piastres (`(long)Math.Round(openingEgp * 100m)`).
  6. Dispatches POST to `/api/shifts/open` via `_session.Api.OpenShiftAsync(request)`.
  7. On error: displays `OpenShiftError = result.Error ?? "تعذر فتح الوردية.";`.
  8. On success: updates `_session.ShiftId` and `_session.ShiftName`, notifies `IsShiftOpen`, refreshes header, and invokes `LoadMenuAsync()`.
- **Forensic Risks & Architectural Defects:**
  - **Fresh GUID Per Attempt:** Like WPF refund, minting `$"open-{Guid.NewGuid():N}"` inside the method body means an ambiguous timeout retry will generate a brand new request ID, potentially attempting to open a second shift.
  - **Absence of Close Shift:** Zero UI or command exists to close shifts, causing business day closure blockage.
- **Verdict:** **HIGH_RISK_ASYMMETRY (Shift Open Without Shift Close)**

---

## 3. Recommendations

1. **Deprecate WPF Shift Management:** Disable shift opening from WPF to prevent creating orphaned open shifts that block end-of-day business day closing.
2. **If Retained:** Add full parity shift close workflow including cash count and handover.


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:32:00Z
- **Source SHA-256 Verified:** `137baa022eb1e158ae5ab7ae1b6e986cfe2bcd96853c091fd9d5d1d101db7929`
- **Callable ID Checked:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Shift.cs:13:5:MethodDeclaration` (1 of 1 callable checked)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Shift.cs` (51 lines).
  2. Confirmed critical WPF shift trap: `OpenShiftAsync` is implemented, but the WPF client provides NO shift close mechanism whatsoever. Because `BusinessDayCloseEvaluator` forbids business day closing while any shift remains open, opening a shift in WPF deadlocks store-wide end-of-day operations until an administrator intervenes via WinUI 3.
  3. Confirmed local clock usage at line 28: `DateTime.Now.Hour < 16 ? "صباحية" : "مسائية"`, which is susceptible to local client clock skew rather than `BusinessClock.CairoTimeZone`.
  4. Canonical callable ID, method signature, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:34:33Z
- **Source SHA-256 Verified:** `137baa022eb1e158ae5ab7ae1b6e986cfe2bcd96853c091fd9d5d1d101db7929`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `137baa022eb1e158ae5ab7ae1b6e986cfe2bcd96853c091fd9d5d1d101db7929` against `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Shift.cs` (51 lines).
2. Concur with L1 on critical shift deadlock trap: WPF allows opening shifts but contains zero implementation for closing them, permanently blocking business day close until administrative override.
3. Additional L2 Challenger Assessment:
   - Shift shift name derivation at line 28 (`DateTime.Now.Hour < 16 ? "صباحية" : "مسائية"`) relies on workstation local time rather than Egyptian business timezone, risking misclassification if POS workstation clock drifts across the 16:00 boundary.
4. Single callable rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
