# src/Falah.Client.CashierWinUI/Views/MenuView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `20250d83b198909bbd1f717899722f4974e230e3844bf0b438c1f388da40b92b`; worktree SHA256 `c8ad0b49ce13bf745cfc62a555c0bdb5081ba7d44ddc89099194fe275a55da28`; LF-equivalent `True`.

Actual role: Menu availability read/write list with tax/offer summaries and nested editor.

## Function/resource review

- `CanOpenEditor` at `src/Falah.Client.CashierWinUI/Views/MenuView.xaml:28`: Editor button permission visibility, nested Editor DataContext declared last overlays page. Main availability toggles root row command; no explicit IsEnabled/visibility permission per row, model must guard.

- `Products` at `src/Falah.Client.CashierWinUI/Views/MenuView.xaml:48`: Name inside horizontal StackPanel with unavailable badge means TextWrapping may not get bounded width; fixed360 columns plus330 side panel and action column leaves name narrow. Native visual overflow unobserved.

- `TaxRules` at `src/Falah.Client.CashierWinUI/Views/MenuView.xaml:137`: Shows basis points and raw Mode; disabled badge explicit. This does not convert basis points to percent for operator. Side summaries remain when product loading/error overlay active.

- `PriceRules` at `src/Falah.Client.CashierWinUI/Views/MenuView.xaml:169`: Name and fixed price only, omits Active/days/time/product scope even though subtitle says current offers. A future/inactive rule can appear visually identical unless VM filters; verify actual query/filter contract before claiming wrong price.

- `ActionNote` at `src/Falah.Client.CashierWinUI/Views/MenuView.xaml:193`: Literalwhite text on TextPrimaryBrush repeats dark toast contrast risk; error independent. Parent editor may hide these beneath own overlay.

- `ZeroCountToVisibleConverter` at `src/Falah.Client.CashierWinUI/Views/MenuView.xaml:162`: Count-based empty tax/offer feedback re-evaluates reliably; no fake default rates or offers. No actual pricing change executed.

## Tests and limitations

NOT_EXECUTED. Test permission denial, inactive/timed offers, availability ambiguous retry, large names and narrow layout, dark toast, editor focus. No callable declarations.

## Recommendations (not implemented)

Label offer activity/scope and tax units clearly; keep availability authorization and exact retained write separate from editable pricing.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
