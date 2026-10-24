# src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `b5d2195c3ca1de571754e75e5c7c7e4471af3111d749d6f3628b199de22127c4`

Worktree SHA-256: `124a969a63f823004a0b4ee64ef499842ad61128518dc236124f122ed8a8deb9`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Drawer-only petty cash expense dialog.

## Source paths and function/resource analysis

- `OpenAddExpense` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs:30`: Resets category/amount/description/PIN, refuses while another command is busy or unresolved. Category strings intentionally differ from some labels; avoid renaming historic grouping keys.

- `SetExpenseCategory` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs:43`: Assigns caller string without busy guard; actual category buttons also remain enabled in XAML. Pending DTO remains immutable, but displayed selection can diverge from frozen payload.

- `CancelAddExpense` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs:46`: Prevents closing during busy/unresolved send. Does not clear a previously entered manager PIN on cancellation.

- `SubmitExpenseAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs:49`: Requires positive parsed amount and description; approval path exchanges PIN once with permission expenses_add_drawer. Uses tre-drawer and current shift/user. finally restores busy state even validation returns after approval decision.

- `CompleteExpenseAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs:52`: Uses one request identity until success and retains ambiguous DTO only in memory (UI-001). No direct SQL or printer access; successful return closes dialog.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing concrete expense restart-after-commit/lost-response test; missing busy category/UI consistency test and theme contrast screenshot assertions.

## Recommendations (not implemented)

Persist exact expense DTO before send; freeze every editable category control with operation state; clear sensitive PIN state after resolved/cancelled dialog. Theme defect belongs to CashierView.xaml, not this category business value.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
