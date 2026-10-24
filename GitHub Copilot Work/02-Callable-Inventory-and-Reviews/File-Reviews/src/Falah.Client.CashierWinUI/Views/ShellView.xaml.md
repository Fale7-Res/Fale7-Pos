# src/Falah.Client.CashierWinUI/Views/ShellView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `a02a7b0789e9b744d66d404aa4f7b1036da167cb43b137b41f9ae76f73c99113`; worktree SHA256 `66e9438fe9a254cbe115fed35e8aa29454f0a42c40cb0d4bd6959711c9453df7`; LF-equivalent `True`.

Actual role: Role-filtered grouped navigation chrome and page host.

## Function/resource review

- `NavBar` at `src/Falah.Client.CashierWinUI/Views/ShellView.xaml:30`: Code-generated horizontal StackPanel inside horizontal ScrollViewer, AutomationProperties.Name Arabic.46DIPminimum; overflow accessible via scroll but no static fixed page count. App header sits outside shell.

- `PageHost` at `src/Falah.Client.CashierWinUI/Views/ShellView.xaml:6`: Stretched star content, manual code-behind type mapping. Source comments about past NavigationView crash not current runtime reproduction; no reason to treat removed control as present defect.

## Tests and limitations

Native nav keyboard/RTL scrolling and permission state tested separately; no actual screenshot here.

## Recommendations (not implemented)

Keep complete catalog/host mapping and user-facing module names, validate page cancellation lifecycle.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
