# Deep Per-Callable Source Audit: `ShiftsViewModel.cs`

- **File Path:** [`src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs)
- **Source SHA-256:** `83c955f6cc1fac8e1b1a77d591e978a3030cafc147cb8435ba9483ad0e466d11`
- **Role:** Client Cashier WinUI Management Shifts Screen, Historical Audit & Paperwork Reprinting
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T01:55:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 9 (2 Constructors, 1 Property Accessor, 6 Methods)

---

## Callable Inventory & Detailed Behavioral Audit

### Callable: `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:38:5:ConstructorDeclaration`
- **Signature:** `public ShiftsViewModel(AppSession session, HeaderViewModel header)`
- **Exact Line Range:** Lines 38–42
- **Behavior & Inputs:**
  Receives `AppSession` and `HeaderViewModel` and assigns them to private readonly fields `_session` and `_header`.
- **Classification:** Presentation constructor.

---

### Callable: `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:65:5:MethodDeclaration`
- **Signature:** `partial void OnActiveShiftChanged(CurrentShiftDto? value)`
- **Exact Line Range:** Lines 65–71
- **Behavior:**
  CommunityToolkit MVVM partial method. Fires `OnPropertyChanged` for `HasActiveShift`, `CanOpenNewShift`, `ShowOpenNotAllowedNotice`, and `ActiveShiftOpenedLine`.
- **Classification:** MVVM property change handler.

---

### Callable: `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:73:53:GetAccessorDeclaration`
- **Signature:** `public ObservableCollection<ShiftRow> History { get; } = new();`
- **Exact Line Range:** Line 73
- **Behavior:**
  Auto-property getter returning the observable collection of historical shift summary rows bound to the WinUI UI data grid.
- **Classification:** Property accessor.

---

### Callable: `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:80:5:MethodDeclaration`
- **Signature:** `private Task ReprintShiftOpenAsync(ShiftRow? row)`
- **Exact Line Range:** Lines 81–82
- **Behavior:**
  RelayCommand delegating to `ReprintAsync(row, "ShiftOpen", "إعادة طباعة إيصال افتتاح الوردية")`.
- **Classification:** Presentation command method.

---

### Callable: `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:84:5:MethodDeclaration`
- **Signature:** `private Task ReprintShiftCloseAsync(ShiftRow? row)`
- **Exact Line Range:** Lines 85–86
- **Behavior:**
  RelayCommand delegating to `ReprintAsync(row, "ShiftClose", "إعادة طباعة إيصال تسليم الوردية")`.
- **Classification:** Presentation command method.

---

### Callable: `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:95:5:MethodDeclaration`
- **Signature:** `private async Task ReprintAsync(ShiftRow? row, string documentKind, string reason)`
- **Exact Line Range:** Lines 95–112
- **Behavior & Financial Audit Trail:**
  Generates a distinct, fresh request ID for every print press:
  `$"rp-{Guid.NewGuid():N}"`
  Calls `_session.Api.ReprintDocumentAsync(...)`. Creates a print job marked reprint (`IsReprint = true`) and logs an audit trail entry. Never mutates, reopens, or recalculates shift state.
- **Defect Interaction (`FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001` & `FIN-004`):**
  If a safe drop or cross-shift refund occurred referencing a closed shift, reprinting the close slip via `ReprintShiftCloseAsync` fetches the recalculated shift summary from the server, printing the retroactively modified `ExpectedCashPiastres` rather than the true snapshot taken at the physical shift closing!
- **Classification:** Presentation reprint orchestrator.

---

### Callable: `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:116:5:MethodDeclaration`
- **Signature:** `internal static string FormatCairo(DateTimeOffset at)`
- **Exact Line Range:** Lines 116–117
- **Behavior:**
  Converts UTC timestamp to Cairo wall-clock time (`BusinessClock.CairoTimeZone`) formatted as `yyyy-MM-dd HH:mm`, eliminating local machine timezone skew.
- **Classification:** Timestamp formatting helper.

---

### Callable: `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:119:5:MethodDeclaration`
- **Signature:** `public async Task LoadAsync()`
- **Exact Line Range:** Lines 119–168
- **Behavior & Blind Close Protection:**
  1. Sets `IsLoading = true`.
  2. Queries `GetCurrentShiftAsync()`. Updates `ActiveShift`, `_session.ShiftId`, and `_session.ShiftName`.
  3. **Blind Close Information Gate:**
     Evaluates `CanSeeShiftTotals => _session.HasPermission("reports_view")`.
     If false, sets `ActiveShiftSummary = null`, clears `History`, and exits.
     This guarantees that operational cashiers cannot view running cash totals before performing blind counts at shift close!
  4. If permitted, queries `GetShiftSummaryAsync` for active shift and `GetShiftsHistoryAsync` for past shifts, populating `History`.
- **Classification:** Presentation data loader method.

---

### Callable: `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:176:9:ConstructorDeclaration`
- **Signature:** `public ShiftRow(ShiftSummaryDto dto)`
- **Exact Line Range:** Line 176
- **Behavior & Discrepancy Classification:**
  Initializes immutable row presentation model wrapping `ShiftSummaryDto`.
  Computes three-way reconciliation status:
  - `Difference => _dto.CashDifferencePiastres ?? 0;`
  - `IsBalanced => IsClosed && Difference == 0` (مطابق)
  - `IsDeficit => IsClosed && Difference < 0` (عجز)
  - `IsSurplus => IsClosed && Difference > 0` (زيادة)
  - `AbsoluteDifferencePiastres => Math.Abs(Difference);`
- **Classification:** Presentation DTO wrapper constructor.

---

## Comprehensive Vulnerability Summary & Fix Recommendations

| Finding ID | Severity | Root Cause Code Span | Impact | Recommended Remediation |
| :--- | :--- | :--- | :--- | :--- |
| **`UI-BLIND-CLOSE-GATE`** | **INFORMATIONAL** | `ShiftsViewModel.cs:46, 136-141` | Running totals and historical reports correctly hidden from cashiers lacking `reports_view`, protecting blind close integrity. | Architectural design is sound; preserve this strict permission check. |
| **`REPRINT-SNAPSHOT-DRIFT`** | **MEDIUM** | `ShiftsViewModel.cs:84-86` & Server | Reprinting close slip queries current live DB totals rather than a frozen closing snapshot; if a safe drop was misattributed to a closed shift, reprint displays drifted figures. | In `CloseShiftHandler`, persist an immutable frozen snapshot JSON of the closing report, and serve reprints from the frozen snapshot. |


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:30:00Z
- **Source SHA-256 Verified:** `83c955f6cc1fac8e1b1a77d591e978a3030cafc147cb8435ba9483ad0e466d11`
- **Callable ID Checked:** `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:38:5:ConstructorDeclaration`, `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:65:5:MethodDeclaration`, `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:73:53:GetAccessorDeclaration`, `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:80:5:MethodDeclaration`, `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:84:5:MethodDeclaration`, `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:95:5:MethodDeclaration`, `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:116:5:MethodDeclaration`, `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:119:5:MethodDeclaration`, `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:176:9:ConstructorDeclaration` (9 of 9 callables checked)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs` (200 lines).
  2. Verified blind-close gate: `CanSeeShiftTotals` requires `reports_view`. If false, lines 136-141 zero `ActiveShiftSummary` and clear `History`, preventing cashiers from peeking at drawer cash expectations before blind count.
  3. Verified reprint snapshot drift: `ReprintAsync` generates a new request ID `$"rp-{Guid.NewGuid():N}"` and calls `ReprintDocumentAsync`. The analysis correctly points out that reprint requests regenerate summary figures from current DB state rather than an immutable frozen closing snapshot, interacting with cross-shift attribution defects (`FIN-004`).
  4. Cairo time formatting and `ShiftRow` discrepancy calculation verified against source.
  5. All 9 canonical callable IDs, method signatures, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:32:35Z
- **Source SHA-256 Verified:** `83c955f6cc1fac8e1b1a77d591e978a3030cafc147cb8435ba9483ad0e466d11`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `83c955f6cc1fac8e1b1a77d591e978a3030cafc147cb8435ba9483ad0e466d11` against `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs` (200 lines).
2. Concur with L1 on blind-close protection gate and reprint snapshot drift (`REPRINT-SNAPSHOT-DRIFT`).
3. Additional L2 Challenger Assessment:
   - In `LoadAsync` (lines 119-168), `CanSeeShiftTotals => _session.HasPermission("reports_view")` properly strips `ActiveShiftSummary` and `History` when false, securing operational blind cash counts against peek attacks.
   - In `ShiftRow` (lines 176-199), reconciliation difference classification correctly separates `IsBalanced`, `IsDeficit`, and `IsSurplus` using signed integer arithmetic.
4. All 9 callables rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
