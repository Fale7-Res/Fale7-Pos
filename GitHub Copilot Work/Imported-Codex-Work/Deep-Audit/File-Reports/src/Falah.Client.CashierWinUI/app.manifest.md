# src/Falah.Client.CashierWinUI/app.manifest

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `eb957956d56b0ec9ca4b66582c844e6d2ce3dcd474292a7a30f4c60a9fff939f`; worktree SHA256 `eb957956d56b0ec9ca4b66582c844e6d2ce3dcd474292a7a30f4c60a9fff939f`; LF-equivalent `True`.

Actual role: Native Windows compatibility declaration for unpackaged WinUI.

## Function/resource review

- `supportedOS` at `src/Falah.Client.CashierWinUI/app.manifest:5`: Five compatibility GUID entries, no requestedExecutionLevel, auto-start, service registration, scheduled task, file association or printer configuration changes in this manifest. Minimum SDK/runtime actual project target remains authoritative; GUID support list alone does not prove older OS runs WinUI.

- `assembly` at `src/Falah.Client.CashierWinUI/app.manifest:2`: No explicit DPI awareness declaration visible; WindowsAppSDK/runtime defaults may provide behavior, so cannot infer DPI-unaware bug solely from omission. Native multi-monitor DPI check required.

## Tests and limitations

NOT_APPLICABLE C# functions/database/printing. XML parsed inventory separately; no software installation executed.

## Recommendations (not implemented)

Document actual tested Windows build/architecture and validate DPI behavior; do not add admin privileges unnecessarily.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
