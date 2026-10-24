# src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `37beafea4cb892e939a39905e7c7b9d30ed52acc2507e25079e0d2a244ec1c5e`; worktree SHA256 `0606ca2008dfe343a653116ed071834d760aa11695df274b580c38a8e6cc59e7`; LF-equivalent `True`.

Actual role: Employee roster create/update with compensation and personnel fields.

## Function/resource review

- `Open` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs:63`: Requires employees_manage/no global pending, uses caller real roster no extra read. Sets HasForm=false even recovered _write exists; generic Save can retry but button visibility tied HasForm must be checked. NotifyState registers pending only after opening.

- `Select` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs:139`: Refuses busy/pending and null; copies every existing personnel/compensation field preserving null optional money vs zero. DateOnly wrapped UTC date for picker; conversion back uses HireDate.Date preserving chosen calendar date.

- `ResetForm` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs:132`: Clears PII/defaults new employee values; create form has IsActive even CreateEmployeeRequest lacks IsActive, so inactive new-entry UX may disagree server default if field shown.

- `TryOptionalMoney` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs:186`: Blank means null, nonblank strict CashInput. CreateRequest validates mandatory name/number/job/phone, compensation choice and rates, trim optional personnel strings; server must validate lifecycle/arrangement membership and compensation business rules.

- `CreateRequest` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs:199`: Builds exact UpdateEmployeeRequest for existing ID or Guid CreateEmployeeRequest. Every known finance/PII field transmitted deliberately; mutable fields no external process execution. Local Optional function only trim/null.

- `SaveAsync` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs:229`: Uses retained generic DTO first, not reconstructed form; success patches roster with authoritative returned DTO and emits Saved. _write journal includes NationalId/address/phone, confidential local data source surface; reviewer read no real records.

- `CanEdit` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs:91`: Checks permission and !busy but not _write.HasPending, so UI fields may change despite frozen payload; retry sends original, display may mislead. New/Select explicitly refuse pending. SendWrite switch only known employee DTO types.

## Tests and limitations

No roster data written; require recovered editor target/retry visibility, unknown payload vs displayed edits, optional fields and unauthorized save tests.

## Recommendations (not implemented)

Freeze fields under pending and present original command; protected local journal for employee PII, restrict server permissions independently.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
