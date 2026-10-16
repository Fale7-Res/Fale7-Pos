# src/Falah.Client.Cashier/Falah.Client.Cashier.csproj

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `ada6fe0133e912291939b6897dbe05d6fada3e4532f75488fc1c77e4b33b0ca1`; worktree SHA256 `75b1d126b7408dd31450e3de3a196d229637f7fae4f653d9a6775f0d6b6159f7`; LF-equivalent `True`.

Actual role: WPF technical-reference executable build metadata.

## Function/resource review

- `TargetFramework` at `src/Falah.Client.Cashier/Falah.Client.Cashier.csproj:13`: net8.0-windows, UseWPF true WinExe; nullable and implicit usings. AnyCPU/default runtime not WinUI x64 explicit. References Application + MVVM8.3.2; no SQL or printing dependency in this project file.

## Tests and limitations

Not source-linked by active client test project; build success says little about active WinUI.

## Recommendations (not implemented)

Clearly designate reference executable to prevent accidental installation; dependency advisories parent-owned.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
