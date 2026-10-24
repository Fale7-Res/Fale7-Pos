# src/Falah.Client.CashierWinUI/Views/OrdersView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `361826db8184d5f30e32f4cdcb2c501fc46fa5d5540e5a3a9c0d5ef7ba87f6be`; worktree SHA256 `26442911aded9d8ccafb02f4c4b4f9e46d377ea60f85364ff63fc2e24197d81b`; LF-equivalent `True`.

Actual role: Read-only searchable order list and detailed item overlay.

## Function/resource review

- `SearchQuery` at `src/Falah.Client.CashierWinUI/Views/OrdersView.xaml:32`: Immediate query text promises number/name/phone; no date or paging controls visible. Actual API cap/filter reviewed in VM independently; UI should disclose truncated history if backend bounded.

- `OpenOrderCommand` at `src/Falah.Client.CashierWinUI/Views/OrdersView.xaml:90`: Nested rows pass real order to root command; header refresh icon unnamed. List type/total/status consistent server strings through converters, customer name wraps, fixed columns leave star narrow.

- `SelectedOrder.Items` at `src/Falah.Client.CashierWinUI/Views/OrdersView.xaml:143`: Detail scroll includes product/variant/notes/quantity/line total and customer address. Boundaries620wide max700height/margin24 require native smaller-screen check.

- `SelectedOrder.PaymentMethod` at `src/Falah.Client.CashierWinUI/Views/OrdersView.xaml:181`: Footer gives total and method only, no IsPaid/pending confirmation status, no split amount legs, refund/cancellation or pending-payment reconciliation command in this view. Thus dashboard orders navigation alone does not expose full pending electronic action; primary cashier lookup separate.

- `IsDetailOpen` at `src/Falah.Client.CashierWinUI/Views/OrdersView.xaml:120`: Header close + scroll + pinned footer, no full receipt rendering or print action. This provides item inspection without physical printer but not faithful receipt preview.

- `HasNoOrders` at `src/Falah.Client.CashierWinUI/Views/OrdersView.xaml:112`: Error/loading/no matching list overlays independent. No detail-specific loading/error region here; VM must delay open or surface detail read failure elsewhere.

## Tests and limitations

NOT_EXECUTED. Native item viewing, stale detail responses, pending electronic visibility, bounded history search and screenreader refresh naming need tests. No callable declarations.

## Recommendations (not implemented)

Clearly show payment confirmation and split legs for financial follow-up; add separate receipt preview later only if authorized.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
