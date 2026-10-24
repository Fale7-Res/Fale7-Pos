# src/Falah.Client.CallCenterWinUI/Views/AppHeaderView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `2ca7d6b092df3f4a7fcb7f29a0a0be47a1400a55a9d18fc5bbd021de18258b82`; worktree SHA256 `2ca7d6b092df3f4a7fcb7f29a0a0be47a1400a55a9d18fc5bbd021de18258b82`; LF-equivalent `True`.

Actual role: CallCenter persistent identity/shift/clock/user/lock layout.

## Function/resource review

- `LockButton` at `src/Falah.Client.CallCenterWinUI/Views/AppHeaderView.xaml:19`: Arabic button binds CanLock and actual OnLockClicked; tooltip tells finish financial operation, but busy text fixed even when enabled. Root Lock implementation separate missing logout issue.

- `RestaurantName` at `src/Falah.Client.CallCenterWinUI/Views/AppHeaderView.xaml:8`: 64DIP height, star identity group plus Auto clock/user columns. No wrapping/trimming/max width on potentially long restaurant/user/shift name; native narrow/high-scale overlap test needed.

- `IsShiftOpen` at `src/Falah.Client.CallCenterWinUI/Views/AppHeaderView.xaml:10`: Shift dot plus text, accessible state not color-only because ShiftLabel present. Cashier uses ThemeResource card/semantic brushes, CallCenter literalWhite/static palette; no assumption both have same dark mode.

## Tests and limitations

No screenshot supplied. Need long names, RTL Tab order/text scaling and lock can-execute runtime checks.

## Recommendations (not implemented)

Constrain long identity content and verify operational clock uses intended Cairo zone; preserve textual shift status.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
