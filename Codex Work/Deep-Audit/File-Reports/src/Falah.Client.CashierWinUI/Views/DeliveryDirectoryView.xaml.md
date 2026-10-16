# src/Falah.Client.CashierWinUI/Views/DeliveryDirectoryView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `464ee93909bc32a6d92cb354e677b356b53ac23ef32e19b32ab3b912f0bf117b`; worktree SHA256 `f05e5df544b463bc1d35f5a16765a3ffaa3916df31747c4a1b4bdc9c1b2ca5c1`; LF-equivalent `True`.

Actual role: Driver and delivery service add/edit/retire overlay.

## Function/resource review

- `SetTabCommand` at `src/Falah.Client.CashierWinUI/Views/DeliveryDirectoryView.xaml:29`: Drivers/services segmented tabs pass explicit keys. Selected white/brand versus neutral/muted; converter theme refresh risk UI004 shared. CanAct guards input/actions, tab command must preserve unresolved state.

- `ToggleDriverCommand` at `src/Falah.Client.CashierWinUI/Views/DeliveryDirectoryView.xaml:78`: Driver name add and active toggle, no harddelete. Text+pill shows active/inactive. Actual relationship to Employee/CanDeliver must be verified backend; this markup alone does not prove duplicate identity model.

- `SelectServiceCommand` at `src/Falah.Client.CashierWinUI/Views/DeliveryDirectoryView.xaml:113`: Service list shows nullable fee as unvalued rather than0, selected root command target explicit; empty Count updates bound correctly. Fixed330px form beside list may constrain narrow viewport.

- `ServiceFeeText` at `src/Falah.Client.CashierWinUI/Views/DeliveryDirectoryView.xaml:165`: Fee optional and active only existing service, two-way fields gatedCanAct. Note promises old orders retain snapshot name/fee; backend transaction/domain review determines truth.

- `ManagerPin` at `src/Falah.Client.CashierWinUI/Views/DeliveryDirectoryView.xaml:183`: Conditional manager approval PasswordBox6, footer errors/success/load-retry. No actual PIN read. Retry-aware SaveLabel for service, but driveradd/toggle labels remain static even if retained outcome unknown.

- `CloseCommand` at `src/Falah.Client.CashierWinUI/Views/DeliveryDirectoryView.xaml:21`: Max980x860 stretched margin24, body lists/form scroll independently, footer outside. No loading spinner despite LoadError; no generic recovered action panel in markup.

## Tests and limitations

NOT_EXECUTED. Test nullfee versus0, old order snapshots, active driver links, permission approval and fresh-process retained action UI. XAML has no callable declarations.

## Recommendations (not implemented)

Expose explicit retry context for every retained action, maintain historical snapshot contract, verify focus and narrow viewports.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
