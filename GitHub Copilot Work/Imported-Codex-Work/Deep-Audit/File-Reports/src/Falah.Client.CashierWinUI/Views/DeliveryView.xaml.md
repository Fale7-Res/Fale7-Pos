# src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `bf3d1a1236572fc2994741d53ff3ca0c719485f487840402232f260b459bd578`; worktree SHA256 `1800c3471c45c7b83b202ef9db923007b2df099aa894dbbb5b2f8a1b7d772a1b`; LF-equivalent `True`.

Actual role: Live dispatch board, driver roster and nested management overlays.

## Function/resource review

- `BoardSummary` at `src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml:28`: Real board state summary and labeled refresh, directory permission visibility. Fixed320 driver panel plus board column widths leaves address star constrained at smaller screens.

- `Board` at `src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml:28`: Rows show order/status, customer+phone/address, assigned driver, amount collection, payment warning and elapsed. Customer horizontal StackPanel can measure unbounded TextBlock despite Wrap, requiring native test.

- `MarkReadyCommand` at `src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml:134`: Buttons strictly shown New→MarkReady, Ready→assign, OutForDelivery→outcome. No action for Preparing/Assigned/HandedToDriver in this markup. Whether these statuses reachable requires server path review, not inferred from comment that all legal transitions offered.

- `IsPaymentUnconfirmed` at `src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml:124`: Textual PaymentStateLabel plus warning color avoids conflating collected with unconfirmed transfer. Collection amount remains server DTO display, not recalculated by XAML.

- `OpenDriverAccountCommand` at `src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml:191`: Driver list buttons pass actual driver to root, async account/stat consistency must hold across rapid selection/close; prior VM generation candidate applies.

- `ActionNote` at `src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml:212`: Success text unwrapped white on themedTextPrimary, so long notes and dark mode risk overflow/low contrast. Error wraps420. Dispatch modals then directory layered last, requiring VM mutual exclusion/focus control.

## Tests and limitations

NOT_EXECUTED. Test actual server delivery states, reverse account/stat responses, pending mutation navigation, address wrapping and dark/narrow viewport. No callable declarations.

## Recommendations (not implemented)

Validate state-action matrix against real backend and display coherent per-driver snapshot. Adapt board rather than assuming desktop density fits every terminal.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
