# src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `16e9dba5bd2305650d309c15a2ac58848e15e2d3820ae05ff1473b2ee02a06ed`; worktree SHA256 `67022609cfa8b4a82b59f2b225725d921e8596defeda694af4a1a47bfef79eea`; LF-equivalent `True`.

Actual role: CallCenter workspace composition and order/reprint overlays.

## Function/resource review

- `IsShiftOpen` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml:16`: No-shift pane offers refresh only; this client intentionally cannot open shift. Active workspace fixed420px intake plus remaining catalogue; no responsive breakpoint in markup.

- `CallCenterDeliveryEditorView` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml:59`: Shared VM inherited by nested address component. Later overlays stack variant, result, lookup, so VM exclusive flags essential; no native modal focus isolation defined.

- `VariantPickerProduct.Variants` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml:73`: Variant buttons root-command bind correctly; pending offer shown explicitly. MaxHeight500 stack has no ScrollViewer, many sizes/large text may clip; native unobserved.

- `HasCheckoutResult` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml:97`: Confirmation reports recorded order and conditional print warning with separate RetryPrint. Order success never equated with successful physical print. StartNewOrder gates CanEditOperation, supporting unresolved print blocking.

- `IsOrderLookupOpen` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml:129`: MaxHeight560 fixed420 stack lacks scroll. Long errors/customer/address/payment lines can exceed it. Shows subtotal total and status, not full items/receipt preview.

- `IsLookedUpPaymentUnconfirmed` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml:168`: Warning versus muted brush and explicit LookedUpPaymentLine communicate unconfirmed electronic money. Reprint button routes ReprintLookedUpOrderCommand, source UI012 kitchen-route issue remains outside markup.

## Tests and limitations

NOT_EXECUTED. Test short viewport, many variants/long lookup, payment warning, print rejection versus ambiguity and one active overlay. No XAML callable symbols.

## Recommendations (not implemented)

Use bounded scroll in variant/lookup overlays; preserve real-order versus print status distinction and native focus validation.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
