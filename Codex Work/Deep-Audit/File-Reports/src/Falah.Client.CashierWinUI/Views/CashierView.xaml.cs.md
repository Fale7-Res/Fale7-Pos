# src/Falah.Client.CashierWinUI/Views/CashierView.xaml.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `b67a7964a83ddd519f5e605e9dd437f8d65838d5435ee0c1d521cc5d017cfb08`

Worktree SHA-256: `e5de23e3dbcf7056ebaac0efbd4a65cdb7755e0fb48b3e58e46461eebac3f7d0`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: POS keyboard accelerators and adaptive cart width.

## Source paths and function/resource analysis

- `IsWorkspaceInteractive` at `src/Falah.Client.CashierWinUI/Views/CashierView.xaml.cs:27`: Checks only CanEditOperation, result, variant picker and checkout flags. Omits expense/safe-drop/cash-movement/refund/pickup/delivery/lookup/close-shift modals; F2/F3/F12 can be accepted with those open if native accelerator routing reaches workspace. Source guard gap UI-010, exact native behavior unverified.

- `OnSearchAccelerator` at `src/Falah.Client.CashierWinUI/Views/CashierView.xaml.cs:30`: Always marks handled then can focus search under omitted modal. OnOperationsAccelerator opens flyout; OnCheckoutAccelerator invokes same CanOpenCheckout guard but omitted modal remains true.

- `OnWorkspaceSizeChanged` at `src/Falah.Client.CashierWinUI/Views/CashierView.xaml.cs:54`: Queues clamped cart column to 350..420 DIPs after current layout. Multiple size events can enqueue obsolete widths until later ticks; no narrow window alternative below min width.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing native keyboard through every modal and resize density tests.

## Recommendations (not implemented)

Central modal state/focus trapping; keyboard guards should include every open modal, with native regression checks.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
