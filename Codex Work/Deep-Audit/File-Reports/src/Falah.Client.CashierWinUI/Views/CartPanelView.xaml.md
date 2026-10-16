# src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `7aa2906e0fdfc7b1693c763cfaeef899478b1ca2bdf00eb7d4a8681a07e0e12f`; worktree SHA256 `a076edb7603af83d2c463e3d75ae3f7ffd75a9b70669e8ce4769fb12dc12b309`; LF-equivalent `True`.

Actual role: Cashier current cart with estimated total and checkout action.

## Function/resource review

- `CompactOrderTypeButton` at `src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml:11`: Collapsed initial compact flyout versus full three-segment order choices delegated codebehind responsive switch. All order type options explicit string contracts; thematic converters return concrete brush resources.

- `OpenPickupEditorCommand` at `src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml:32`: Pickup/delivery summaries show current draft, two-line bound label; OpenDeliveryEditor separate. Header and cart use hit-test gating, which does not itself disable keyboard focus.

- `RemoveLineCommand` at `src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml:58`: Nested root command paths preserve item parameter; delete/increment/decrement have Arabic automation names. Notes flyout actual two-way row text/chips; codebehind must maintain correct line context.

- `OrderFooter` at `src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml:84`: Fixed auto footer beside star cart shows subtotal/delivery/discount/comp/tax conditional rows and explicit estimated total, not authoritative receipt. Many visible rows shrink available cart space; named elements support native probes but no probe run here.

- `FooterCheckoutReason` at `src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml:107`: Wrapped reason explains blocked checkout. Real command plus CanOpenCheckout gates. Cancel uses request-clear confirmation command and CanEditOperation, rather than unconditional clear.

- `FooterTotalAmount` at `src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml:103`: Amount wraps and MinHeight52 rather than fixed narrow row, helping large sums. ThemeResource text/background pairs follow active theme except converter-selected white segments. No full receipt preview or print invocation here.

## Tests and limitations

NOT_EXECUTED. Native focus during pending, compact flyout, note popup binding, all footer rows at1366/150percent and large amounts need validation. XAML has no callable declarations.

## Recommendations (not implemented)

Keep estimated versus authoritative distinction; enforce model guards as well as native IsEnabled and verify pinned footer usability.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
