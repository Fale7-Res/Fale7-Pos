# src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `9f41d9133ed49ccb2e8dff94a43e70dc525500836f7c6c6e8ada4912bc1861dd`; worktree SHA256 `9f41d9133ed49ccb2e8dff94a43e70dc525500836f7c6c6e8ada4912bc1861dd`; LF-equivalent `True`.

Actual role: WPF order lookup and refund reference flow.

## Function/resource review

- `LookupOrderAsync` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:39`: Trims number, sets loading, reads actual order, prefill remaining total. No overlap epoch/guard, late response can target another number. Close always allowed during pending read/refund.

- `SubmitRefundAsync` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:67`: Positive currentculture amount/reason, rounds to piastres. Fresh Guid each retry no frozen payload/outcomeUnknown gate, no pre-check remaining/paid/multi or manager approval fallback; backend must reject unauthorized/oversized. Command CanRefund depends selection/busy but no generated NotifyCanExecuteChangedFor fields, button may remain stale.

- `TranslateStatus` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs:97`: Exact known lowercase Arabic state labels, unknown raw preserved. After success sets message then LookupOrderAsync immediately clears RefundSuccessMessage, so user success banner disappears while refresh starts. Display bug only; server outcome independent.

## Tests and limitations

Reference not active and not source-linked client tests. No real refund performed.

## Recommendations (not implemented)

Retire accidental reference use or add actual active-client safeguards under future approved implementation; preserve history actual read.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
