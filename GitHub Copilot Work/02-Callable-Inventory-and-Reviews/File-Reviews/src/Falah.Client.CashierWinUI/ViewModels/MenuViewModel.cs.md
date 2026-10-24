# src/Falah.Client.CashierWinUI/ViewModels/MenuViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `de283d740a6199287b168008bd6bdd8a2c930646927f76195a2134d6d7d87d34`; worktree SHA256 `40be2325e93ca488b4883896691482c0fdeb201daefef203c876af40bd3e3069`; LF-equivalent `True`.

Actual role: Live menu/tax/price overview, availability toggle and authoring editor host.

## Function/resource review

- `MenuViewModel` at `src/Falah.Client.CashierWinUI/ViewModels/MenuViewModel.cs:21`: Constructs MenuEditor and reloads when Editor.Closed; same owner lifetime, no external event leak established. OpenEditor delegates existing authoring permissions.

- `MenuProductRow` at `src/Falah.Client.CashierWinUI/ViewModels/MenuViewModel.cs:44`: Price is actual variant minimum or base, station label real preparation enum; IsAvailable separately mutable only after successful request.

- `ApplyFilter` at `src/Falah.Client.CashierWinUI/ViewModels/MenuViewModel.cs:77`: Searches product/category client snapshot with CurrentCultureIgnoreCase; rebuilds observable list per keystroke. No backend query pagination, menu expected small but scale unmeasured.

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/MenuViewModel.cs:31`: Menu then taxes/rules sequential. Applies menu before tax/rules finish; LoadError can show partially updated snapshot. Single busy guard returns rather than coalesces reload after Editor.Closed during load, so new changes can remain stale until manual refresh.

- `ToggleAvailabilityAsync` at `src/Falah.Client.CashierWinUI/ViewModels/MenuViewModel.cs:130`: Requires menu_manage, captures target=!row state once; on success updates row. Unknown response leaves row stale but next identical toggle sends same target while row unchanged, idempotent intended setter. No journal/global busy gate; errors can be corrected with reload; effect not duplicate monetary posting.

## Tests and limitations

Need concurrent editor-close/load and unknown availability response refresh tests. No menu settings changed.

## Recommendations (not implemented)

Coalesce pending reloads and retain clear snapshot/error messaging; avoid treating reference docs as current functionality because price editor now implemented.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
