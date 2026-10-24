# src/Falah.Client.Cashier/Views/AppHeaderView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `bce17007bbd937253de6376da46eee1a93b4633e68e025f4e76198b08a25e305`; worktree SHA256 `bce17007bbd937253de6376da46eee1a93b4633e68e025f4e76198b08a25e305`; LF-equivalent `True`.

Actual role: Legacy WPF64px header with restaurant, shift, time, lock and operator badge.

## Function/resource review

- `IsShiftOpen` at `src/Falah.Client.Cashier/Views/AppHeaderView.xaml:30`: WPF DataTrigger dot emerald/slate plus explicit ShiftLabel text, so not sole-color state. Right logo group and left time/user groups float without shared columns, longnames/narrowwindow can overlap.

- `TimeText` at `src/Falah.Client.Cashier/Views/AppHeaderView.xaml:45`: Clock/dateLTR insideRTL header; VM determines timezone. Fixed64height plus9/10px secondarytext may clip atOStextscale.

- `LockCommand` at `src/Falah.Client.Cashier/Views/AppHeaderView.xaml:50`: RelativeSource ancestorWindow.DataContext correctly finds main shell command beyond HeaderVM. No bearer revocation behavior in markup; coordinator tracks lock/session separately.

- `UserName[0]` at `src/Falah.Client.Cashier/Views/AppHeaderView.xaml:60`: Initial index binding may fail for emptyusername, WPF binding fallback rather than checked glyph. Full name/role remain visible in text; no photo/credentialread.

## Tests and limitations

NOT_EXECUTED; no native window interaction. No callable declarations. Need longArabicname/emptyuser/smallwidth/lockpending tests.

## Recommendations (not implemented)

Use grid columns/trimming and safe initial converter if legacy maintained; do not infer app security from lock icon.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
