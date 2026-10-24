# src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `d8f01752ae087dfaa73071182398a71df1366745d92f4dd9ca35b7b879f4e67a`; worktree SHA256 `24d7807c2ca0ccf99aea7cf633ccfe99655de6acf2d0c19133b45060e90fdc63`; LF-equivalent `True`.

Actual role: Recipe coverage/cost truth, ingredient editor and real save.

## Function/resource review

- `RecipeRow` at `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:34`: Unknown cost dash, partial floor prefixed from, margin returned server only; no invented zero cost. CostsHidden branch intentionally withholding money for author-only role, though placeholder DTO numeric zeros must stay hidden at all consumers.

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:123`: Reports users fetch summaries+coverage, then ingredient list; author-only LoadWithoutCosts fetches plain recipes/menu and hides costs. Missing coverage sets LoadError, preventing falsely reassuring absence. Recipe selection detail always uses cost endpoint; author-only editing gate may fail if backend reports_view required, parent cross-check.

- `ApplyFilter` at `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:95`: Matches product/recipe/category and complete/unknown status. Hidden-cost placeholder KnownLineCount0means all appear unknown to filter despite costs merely unauthorized; text labels CostsHidden distinguishes row but filter semantics may confuse.

- `LoadDetailAsync` at `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:304`: No request generation/cancellation check; selecting A then B can publish slow A into B detail, including recipe edit target from Detail. This is real source race, cross-recipe save potential if operator starts editing inconsistent pane; no backend writes tested.

- `OnDetailChanged` at `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:307`: Rebuilds actual cost lines and all derived cost/margin/missing labels. Margins withheld when incomplete, correctly not zero. CostLineRow shows actual purchase/estimated/prep/circular provenance and missing reasons.

- `StartEdit` at `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:470`: Copies current Detail lines and preserves kind/yield conversion; requires menu_manage. CanStartEdit also nonediting, but direct StartEdit method no IsSaving guard; CancelEdit unconditionally discards while save in flight, no global pending gate.

- `ApplyIngredientFilter` at `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:441`: Active ingredients only, excludes chosen IDs, max60visible, client search. LoadIngredients caches successful list for VM lifetime; newly created stock items may not appear until new page.

- `SaveAsync` at `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:532`: Validates nonempty lines, positive milli-unit quantity. YieldRatioText accepts fractional via MilliUnits then integer /1000truncates, so1.5 becomes1 despite promise integer. Fresh Guid per submit, no retention; stale Detail target and unknown save invite wrong display or duplicate request. Server may upsert by recipe ID, not duplicate money, but inventory future consumption changed.

- `RefreshSummaryAsync` at `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:593`: Preserves selection via _reselecting, fetches real summaries/coverage. No error surfaced on failed summary refresh, keeps old list; old selection/detail mismatch can persist. No direct stock balance writes; recipe affects future checkout consumption backend.

## Tests and limitations

Need delayed A/B recipe detail with edit/save binding, author-only permission integration, fractional yield rejection, unknown-save identity and cancellation tests. No recipe changes made.

## Recommendations (not implemented)

Version every selection response; enforce integer yield exactly; durable immutable saves and recoverable target identity; separate non-cost author detail from protected cost detail.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
