# src/Falah.Client.CashierWinUI/Falah.Client.CashierWinUI.csproj

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `9b01f2a3ad96a5ae266ab05ce415745d4d2ae4c2d3b4dfa477f56738bfbd21c3`; worktree SHA256 `9b01f2a3ad96a5ae266ab05ce415745d4d2ae4c2d3b4dfa477f56738bfbd21c3`; LF-equivalent `True`.

Actual role: Unpackaged WinUI3 x64 build/dependency metadata.

## Function/resource review

- `TargetFramework` at `src/Falah.Client.CashierWinUI/Falah.Client.CashierWinUI.csproj:5`: net8.0-windows10.0.19041.0 with TargetPlatformMinVersion17763; UseWinUI/WinExe/x64/win-x64. WindowsAppSDKSelfContained true carries Windows App SDK, not automatically proof .NET runtime self-contained because SelfContained absent here.

- `PackageReference` at `src/Falah.Client.CashierWinUI/Falah.Client.CashierWinUI.csproj:32`: Pinned WindowsAppSDK1.7.260224002, Windows SDK BuildTools10.0.22621.3233, CommunityToolkit.Mvvm8.3.2. Supply-chain advisory lookup handled parent; no vulnerability safety inferred.

- `EnableMsixTooling` at `src/Falah.Client.CashierWinUI/Falah.Client.CashierWinUI.csproj:25`: Required PRI build resources while WindowsPackageType None, no MSIX identity. ApplicationManifest app.manifest declares native compatibility/DPI separately.

- `ScreenLayoutProbe` at `src/Falah.Client.CashierWinUI/Falah.Client.CashierWinUI.csproj:47`: Compile-time property defines SCREEN_LAYOUT_PROBE and linked test native probe; cashier additionally has FOOTER_LAYOUT_PROBE and management probe. Release normal build unaffected unless property explicitly supplied. Probe-only fixture must never be reported real backend UI validation.

## Tests and limitations

Build not run by this reviewer; parent owns environment/build evidence. Native probe source separate tracked review; headless sources linking does not compile XAML.

## Recommendations (not implemented)

Publish explicit supported Windows/x64/.NET requirements and avoid deploying probe configuration; inspect locked transitive assets separately.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
