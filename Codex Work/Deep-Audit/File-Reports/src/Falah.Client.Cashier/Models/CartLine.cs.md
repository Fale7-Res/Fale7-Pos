# src/Falah.Client.Cashier/Models/CartLine.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `2b88b7b2210ac59d2bab4fc9b229ce4e5c428424c9bed2ba0459e8d143f1211f`; worktree SHA256 `2b88b7b2210ac59d2bab4fc9b229ce4e5c428424c9bed2ba0459e8d143f1211f`; LF-equivalent `True`.

Actual role: WPF reference mutable line display/identity state.

## Function/resource review

- `LineTotalPiastres` at `src/Falah.Client.Cashier/Models/CartLine.cs:29`: Unchecked price*intQuantity estimate, no internal quantity validation; product/variant/offer identities immutable init, notes/quantity observable. Backend DTO sends quantity/identity, no trusting display price.

- `OnQuantityChanged` at `src/Falah.Client.Cashier/Models/CartLine.cs:33`: Updates line total/display, parent grand totals require own VM commands/recompute. Notes no local length bound; server must validate quantity/note length; no DB or printer action.

## Tests and limitations

Need extreme quantity overflow and property/direct setter aggregate notification; active VM tests not automatically WPF evidence.

## Recommendations (not implemented)

Keep estimated price explicitly nonauthoritative and bounded user input; no code changes.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
