# src/Falah.Client.CallCenterWinUI/ViewModels/HeaderViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `cc13f51634b4bd08e2fe3408b841cdfda634b1cf70b3da00b99d24270f8d3f86`; worktree SHA256 `cc13f51634b4bd08e2fe3408b841cdfda634b1cf70b3da00b99d24270f8d3f86`; LF-equivalent `True`.

Actual role: CallCenter persistent restaurant/user/shift header and wall clock.

## Function/resource review

- `HeaderViewModel` at `src/Falah.Client.CallCenterWinUI/ViewModels/HeaderViewModel.cs:14`: Retains shared session, starts30second native DispatcherTimer, immediately initializes date/time; CallCenter manually Refresh called by operation state code.

- `UpdateClock` at `src/Falah.Client.CallCenterWinUI/ViewModels/HeaderViewModel.cs:23`: Uses DateTime.Now machine-local timezone and ar-EG formatting, unlike explicitly Cairo BusinessClock. Station with different Windows timezone shows differing clock/date even though backend business date Cairo. Legitimate UI timer, no scheduled destructive action.

- `Refresh` at `src/Falah.Client.CallCenterWinUI/ViewModels/HeaderViewModel.cs:61`: Notifies CanLock/shift/name/role from actual session; no backend fetch. Header can show stale shift if session not refreshed externally. UserInitial slices UTF16 first char, possible half-surrogate for emoji names, minor presentation issue.

- `SetIdentity` at `src/Falah.Client.CallCenterWinUI/ViewModels/HeaderViewModel.cs:32`: Stores actual configured restaurant name but initial fallback hardcoded restaurant branding; no display-name project rename implied. Stop only stops clock; no financial state.

## Tests and limitations

Headless tests substitute HeaderViewModel, so actual timer/timezone/formatting/event subscription not validated there.

## Recommendations (not implemented)

Use explicit restaurant timezone where operational clock must match; dispose session subscription if header lifetime becomes shorter than session; test Unicode initials.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
