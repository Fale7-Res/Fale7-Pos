# src/Falah.Client.CashierWinUI/Views/ShellView.xaml.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `de550fa6e2db97adc94ac2eeee4d9b4343d9a5a2fbf4964d2979094df422d1da`; worktree SHA256 `733cc83e3e7a09cdf3b08309922b01a27bcb0e4d8d0efdaf4de7bf073a787e51`; LF-equivalent `True`.

Actual role: Code-generated grouped native nav controls and VM-to-page mapping.

## Function/resource review

- `AttachViewModel` at `src/Falah.Client.CashierWinUI/Views/ShellView.xaml.cs:22`: Detaches previous VM property subscription, sets new model, builds bar/content/selected styling. No Unloaded unsubscribe; shell VM not retained in session normally, so no definite external event-root leak proved solely here.

- `CreateNavButton` at `src/Falah.Client.CashierWinUI/Views/ShellView.xaml.cs:51`: Actual Arabic automation name set. Single group clicks Navigate; multi creates native MenuFlyout item per allowed page. Captured item per foreach current C# semantics safe, not closure-all-last bug.

- `ApplyCurrentPage` at `src/Falah.Client.CashierWinUI/Views/ShellView.xaml.cs:35`: Explicit cases every current known page model including placeholder; no implicit WinUI WPF-type template assumption. Each change creates new native page; cached cashier VM events must detach in child.

- `ApplySelectedItem` at `src/Falah.Client.CashierWinUI/Views/ShellView.xaml.cs:36`: Imperatively assigns app.Resources brush each selection, no theme-change listener. Selected nav stays previous Brush after live theme switch if selection unchanged, related UI-004. TextOnDark stable across themes partly mitigates; accent differs.

- `BuildNavBar` at `src/Falah.Client.CashierWinUI/Views/ShellView.xaml.cs:33`: Clears old buttons/list and re-adds per group; no process/DB/printer operation, no hidden unfiltered nav. OnPropertyChanged only page/selected, no collection changed subscription assumes permissions fixed per login.

## Tests and limitations

Native focus/selected contrast and live theme UI test absent; source correctness not screenshot proof.

## Recommendations (not implemented)

Use theme resource expressions or ActualThemeChanged refresh and appropriate unload cleanup; preserve explicit permission-safe group creation.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
