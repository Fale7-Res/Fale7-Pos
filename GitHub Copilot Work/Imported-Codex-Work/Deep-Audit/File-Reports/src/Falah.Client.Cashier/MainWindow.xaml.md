# src/Falah.Client.Cashier/MainWindow.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `54fa8b4897e5248793f0db5d5e4ead95b7000138580b1f4ea50d19c532c84366`; worktree SHA256 `54fa8b4897e5248793f0db5d5e4ead95b7000138580b1f4ea50d19c532c84366`; LF-equivalent `True`.

Actual role: WPF reference root with implicit VM type templates.

## Function/resource review

- `Window.Resources` at `src/Falah.Client.Cashier/MainWindow.xaml:12`: WPF DataTemplate DataType resolves Login/Cashier into views, unlike active WinUI manual switch. Initial1366x820centered window, no maximize/min-size behavior.

- `DockPanel` at `src/Falah.Client.Cashier/MainWindow.xaml:20`: Header visibility uses ancestor root IsLoggedIn after header DataContext replaced; offline banner root IsOnline, CurrentView content remaining area. All static palette; no WinUI live theme.

## Tests and limitations

NOT_APPLICABLE money mutation; WPF native window/resize binding not runtime-tested.

## Recommendations (not implemented)

Reference-only designation, test correct DPI/window when deployed; no close gate in code-behind.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
