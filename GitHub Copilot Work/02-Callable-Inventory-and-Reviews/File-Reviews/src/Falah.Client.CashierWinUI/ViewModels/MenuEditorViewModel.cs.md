# src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `5ccf3b79ac5c4843a7af0d39c3d7d1327a6c7e21c5dc724bf89583569d11964d`; worktree SHA256 `18449ab63e886ec4a5a507bfa408d9f31315fb0dd091e3ac2114060ae5ea43de`; LF-equivalent `True`.

Actual role: Explicit category/product/variant/tax/price-rule/image management editor.

## Function/resource review

- `OpenAsync` at `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:111`: Any menu/settings/offers permission can open editor internally, but shell only exposes Menu under menu_manage and Settings separate; offers-only entry reachability must be cross-checked. CanBrowse excludes pending generic journal but not image busy. Recovered pending can make Load return before any catalog/form appears, Save retry exists but native visibility HasEditor may hide it.

- `ReloadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:125`: Sequential reads categories/allproducts/recipes/tax/prices regardless chosen kind; first denied endpoint blocks entire editor for otherwise authorized tax/offer-only manager. IsReady only after all reads. Retains unavailable linked recipe placeholder to avoid silently dropping existing ID.

- `SetKind` at `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:153`: Known4kinds only, resets editing/error/image and refreshes rows/gates. RefreshRows filters actual snapshots by name/details; no DB writes. Product display baseprice may differ from variant min but editor explicit base field not sale estimate.

- `SelectRow` at `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:200`: Copies selected actual typed record into form; taxRate uses CashInput format of basispoints so percent parsing100matches stored units. Category choice membership checked on save; Station not membership checked but server enum validation remains needed.

- `ChooseImageAsync` at `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:257`: Captures product before file-picker await, size checked AFTER platform reads full bytes. Upload immediately outside generic formSave/journal by explicit design. IsImageBusy only gates image controls, not CanBrowse/CanEdit; selecting B during A upload allowed in VM. Success overwrites _editing with capturedA and newVersion regardless current selection then Reload, possibly targetingAwithBvisibleform on later save. Parent alerted source race, native callsite mitigation pending.

- `RemoveImageAsync` at `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:291`: Same stale _editing overwrite after awaited delete; file actual backend mutation immediate, not cancelled by closing form. No global session busy for image action, token/user can change before captured CurrentUser after picker.

- `CreateRequest` at `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:317`: Name mandatory, invariant intsort, positive strictmoney, actual category/station/product/variant IDs, known tax mode and bounded basispoints int; price rule needs productIDs, optional days nullmeansall and TimeOnlyHH:mm. No local start/end consistency/domain range, server validation required. New records Active false defaults tax/prices intentional but UI must make disabled state clear.

- `SaveAsync` at `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:353`: Retained exact generic DTO before send; dispatch9knowncreate/update/toggle shapes, does not reread form on retry. Submit closes editor on confirmed outcome and reloads. ToggleStatus actual category.Active/product.IsAvailable inverse capturedonce, journal stable across ambiguity.

- `TryTime` at `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:311`: Empty nullable or exact HH:mm invariant; invalid returns ActionError. AddVariant/RemoveVariant guarded CanEdit but not IsImageBusy, same mixed action context concern.

## Tests and limitations

Need delayed imageAselectB/formsave test and close/lock while upload; authority-only permission matrix, generic recovered retry HasEditor visibility and linked missing recipe preservation. No files picked or product/tax/price mutations executed.

## Recommendations (not implemented)

Freeze selection/global navigation while image operation or validate generation before all response mutations; keep immutable target independent visible form, per-role loading and explicit recovered panel.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
