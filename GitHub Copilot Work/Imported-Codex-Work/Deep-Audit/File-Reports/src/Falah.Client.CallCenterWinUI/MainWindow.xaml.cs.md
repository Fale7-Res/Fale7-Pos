# src/Falah.Client.CallCenterWinUI/MainWindow.xaml.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `d075891fc1aace2363fa85196441f2b4a0a5afa089ff32a49124795bb9787f87`; worktree SHA256 `d075891fc1aace2363fa85196441f2b4a0a5afa089ff32a49124795bb9787f87`; LF-equivalent `True`.

Actual role: Native CallCenter root window and manual VM-to-view composition.

## Function/resource review

- `MainWindow` at `src/Falah.Client.CallCenterWinUI/MainWindow.xaml.cs:9`: Attaches close veto to TryClose, cleanup stops root/header timers and root property subscription. LockRequested forwards actual root command; initial view attached before native maximize.

- `MaximizeOnTargetDisplay` at `src/Falah.Client.CallCenterWinUI/MainWindow.xaml.cs:29`: Native maximize avoids DPI physical-pixel resize math. No root theme preference handling unlike cashier; station colors use static brushes.

- `OnViewModelPropertyChanged` at `src/Falah.Client.CallCenterWinUI/MainWindow.xaml.cs:19`: Routes login/online/current view to actual visibility/content. ApplyCurrentView only Login/CallCenter known VM, unknown null; does not retain native UserControl when root locks/unlocks.

- `ApplyCurrentView` at `src/Falah.Client.CallCenterWinUI/MainWindow.xaml.cs:27`: New CallCenterView each unlock but session caches same desk; child CallCenterOrderPanelView Cart.CollectionChanged never detaches, retaining old view via cached VM. Native window shutdown eventually releases process memory, repeated lock leaks until then.

## Tests and limitations

Missing repeated lock/unlock native weak-reference memory and close event wiring test. No window interactions performed.

## Recommendations (not implemented)

Detach nested long-lived VM events on unload/DataContext change and bind token revoke to root lock lifecycle.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
