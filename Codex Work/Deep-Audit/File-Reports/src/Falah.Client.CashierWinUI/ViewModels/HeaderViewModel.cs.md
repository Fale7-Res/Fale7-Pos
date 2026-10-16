# src/Falah.Client.CashierWinUI/ViewModels/HeaderViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `73c2dcc59a4523395f1b203cecfefdc486fb79eb1a39645166f83585f3e45b98`; worktree SHA256 `73c2dcc59a4523395f1b203cecfefdc486fb79eb1a39645166f83585f3e45b98`; LF-equivalent `True`.

Actual role: cashier persistent restaurant/user/shift header and wall clock.

## Function/resource review

- `HeaderViewModel` at `src/Falah.Client.CashierWinUI/ViewModels/HeaderViewModel.cs:14`: Retains shared session, starts30second native DispatcherTimer, immediately initializes date/time. Cashier additionally subscribes session.OperationStateChanged; Stop does not remove that subscription, though normally both live process lifetime.

- `UpdateClock` at `src/Falah.Client.CashierWinUI/ViewModels/HeaderViewModel.cs:24`: Uses DateTime.Now machine-local timezone and ar-EG formatting, unlike explicitly Cairo BusinessClock. Station with different Windows timezone shows differing clock/date even though backend business date Cairo. Legitimate UI timer, no scheduled destructive action.

- `Refresh` at `src/Falah.Client.CashierWinUI/ViewModels/HeaderViewModel.cs:23`: Notifies CanLock/shift/name/role from actual session; no backend fetch. Header can show stale shift if session not refreshed externally. UserInitial slices UTF16 first char, possible half-surrogate for emoji names, minor presentation issue.

- `SetIdentity` at `src/Falah.Client.CashierWinUI/ViewModels/HeaderViewModel.cs:33`: Stores actual configured restaurant name but initial fallback hardcoded restaurant branding; no display-name project rename implied. Stop only stops clock; no financial state.

## Tests and limitations

Headless tests substitute HeaderViewModel, so actual timer/timezone/formatting/event subscription not validated there.

## Recommendations (not implemented)

Use explicit restaurant timezone where operational clock must match; dispose session subscription if header lifetime becomes shorter than session; test Unicode initials.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
