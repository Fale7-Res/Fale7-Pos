# src/Falah.Client.CashierWinUI/ViewModels/AttendanceViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `7c278e9a5dae77559f3c7509edec07a74a931e1c19be1c534ab7f722c978cf79`; worktree SHA256 `984380332116f705c592b3be9713191d47f6e42d53453604e0877fe5ccc0b0b1`; LF-equivalent `True`.

Actual role: Per-Cairo-date attendance and approved financial correction editor.

## Function/resource review

- `AttendanceViewModel` at `src/Falah.Client.CashierWinUI/ViewModels/AttendanceViewModel.cs:20`: Uses Windows Egypt Standard Time for current date; service backend independent. Loads durable correction through field initializer, but no constructor session pending registration or recovered editor context.

- `ReloadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/AttendanceViewModel.cs:50`: Coalescing do/while with requested date snapshot, discards stale date response and loops; clears old records before each fetch. CanBrowse blocks while correction open/busy. Correctly handles read ordering compared to UI-007.

- `PreviousDay` at `src/Falah.Client.CashierWinUI/ViewModels/AttendanceViewModel.cs:83`: Date-- with CanBrowse then Load; NextDay analogous. Property date changed only label; methods explicitly load. No cancellation when page abandoned.

- `OpenCorrection` at `src/Falah.Client.CashierWinUI/ViewModels/AttendanceViewModel.cs:129`: Requires permission, browse, not loading and no read error. Maps actual known status, count/hour fields, reason blank. Does not guard recovered _correction.HasPending; recovered request can disagree with newly displayed target record.

- `NormalizeNumber` at `src/Falah.Client.CashierWinUI/ViewModels/AttendanceViewModel.cs:146`: Arabic/Persian digits and decimal separator. TryHours finite/nonnegative invariant double; TryCount nonnegative int, rejects decimal/sign. Server must enforce max hours/domain consistency.

- `SaveCorrectionAsync` at `src/Falah.Client.CashierWinUI/ViewModels/AttendanceViewModel.cs:156`: If retained request exists, skips current editor fields and sends original exact DTO; valid retry semantics but UI may show different CorrectionRecord after recovery. Fresh validates permission/status/numeric/reason then Guid request, delegates real handler.

- `OnIsBusyChanged` at `src/Falah.Client.CashierWinUI/ViewModels/AttendanceViewModel.cs:120`: Registers management pending only when busy changes, not at construction when recovered helper already pending; can navigate/lock before any retry unless another path registers. On success closes editor, clears reason, reloads authoritative records to avoid stale overwrite.

## Tests and limitations

AttendanceWorkflowTests need review; missing concrete recovered correction UI target/context and initial pending registration tests. No attendance writes executed.

## Recommendations (not implemented)

Surface recovered correction target and block incompatible new target; register pending at construction and allow explicit retry without misleading active form.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
