# src/Falah.Client.CallCenterWinUI/Controls/ModalHeaderView.xaml.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `28bacf2ea9d1c40ea3ad4d6feaf99485cb100dd47ea9d08f9240c1ae730929f5`; worktree SHA256 `28bacf2ea9d1c40ea3ad4d6feaf99485cb100dd47ea9d08f9240c1ae730929f5`; LF-equivalent `True`.

Actual role: CallCenter title/close command/automation-ID dependency properties.

## Function/resource review

- `ModalHeaderView` at `src/Falah.Client.CallCenterWinUI/Controls/ModalHeaderView.xaml.cs:8`: Initialize default title; Get/Set properties directly wrap DPs. No long-lived event subscriptions.

- `OnTitleChanged` at `src/Falah.Client.CallCenterWinUI/Controls/ModalHeaderView.xaml.cs:11`: Updates actual title. OnCloseCommandChanged assigns ICommand so native control CanExecute behavior applies.

- `OnCloseAutomationIdChanged` at `src/Falah.Client.CallCenterWinUI/Controls/ModalHeaderView.xaml.cs:17`: Sets actual close-button AutomationId. Default empty, per-overlay IDs can be supplied; no credentials placed here.

## Tests and limitations

Native DP ordering and command CanExecute required; no direct data/printer actions.

## Recommendations (not implemented)

Use stable IDs for real UI probes while preserving pending-operation guarded close commands.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
