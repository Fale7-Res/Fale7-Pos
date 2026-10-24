# src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `0cdacf865d71367c914cd3bef3ae47e9a0b46c4e0fb2ca877c7de7fc647fd406`; worktree SHA256 `f7993cc650a8a21a7316e759e1c2ce4d8632a608ed4ad0e58160607cb6f268f5`; LF-equivalent `True`.

Actual role: Menu/category/product/image/tax/price-rule HTTP authoring adapter.

## Function/resource review

- `GetAllCategoriesAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:9`: GET all categories uses SendAsync rather than read-only ReadAsync; failure message incorrectly implies unknown money operation. GetAllProductsAsync same. No automatic resend or writes in these GET wrappers.

- `CreateCategoryAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:13`: POST typed CreateCategoryRequest; UpdateCategory PUT encoded category ID; SetCategoryActive POST active; bool commands use SendEmptyAsync so successful empty response accepted. Server validates actor/input; no SQL here.

- `CreateProductAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:19`: Typed product POST; UpdateProduct PUT encoded ID; SetProductImage PUT JSON typed request, not file path/process. RemoveProductImage Delete uses encoded product and actor query, no path traversal in client URL construction.

- `CreateTaxRuleAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:27`: Create POST, Update PUT encoded tax rule ID. Price rule Save uses POST DTO and decoded authoritative response; no client-side hidden price mutation.

## Tests and limitations

All methods accept and forward cancellation token to HTTP/helper. Current tests should assert no-content success for each bool mutation and read-vs-write diagnostic. Native UI and real API not exercised.

## Recommendations (not implemented)

Use read helper for read-only GET diagnostics; keep proper ID escaping and server-only financial authority.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
