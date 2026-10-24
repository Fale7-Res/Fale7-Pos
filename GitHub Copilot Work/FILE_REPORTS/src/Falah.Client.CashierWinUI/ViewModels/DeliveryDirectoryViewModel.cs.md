# src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `2994d5d969bfd9a219944c6e9dafb5eb7d016ea681d2c6831c68e4d6059a88e9`; worktree SHA256 `32572081689e600ee9740614b02ea4b1d75573739357c1c6a39ac0dc83049c31`; LF-equivalent `True`.

Actual role: Driver/service authoring and reactivation with typed durable operations.

## Function/resource review

- `OpenAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:73`: Requires actual delivery_manage and no global pending; loads real active/inactive directory, resets service/PIN. Comment suggests manager approval alternative but CanOpen requires permission, so fallback unavailable ordinary navigation.

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:82`: Driver+service sequential fetch sets IsReady only after both; IsLoading callback registers pending if recovered _write. Constructor no initial recovery presentation/context.

- `SetTab` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:93`: Refuses busy/pending and unknown tab; Service select/new also refuse pending. ResetServiceForm resets editing pointer, optional fee and active=true, not DB hard delete.

- `AddDriverAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:155`: Uses recovered generic _write first or validates name/CreateDriverRequest. ToggleDriver captures explicit desired active state. Each sends immutable original even if clicked another action during pending, correct retry but note string may describe wrong operation.

- `SaveServiceAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:181`: Blank fee means null, otherwise strict CashInput; request create/update includes actual name/fee/active/approval. Fresh create ignores ServiceActive false since create contract no active field (server default active); UI could show inactive choice for new service yet creates active, XAML visibility to verify.

- `CompleteAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:206`: Generic request dispatch maps4known DTOs, helper persists before send; success clears forms/PIN, reloads real state, Changed event updates parent. CanAct excludes busy/loading/notready but NOT pending, while individual commands use _write.Value safety.

- `ResolveApprovalAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:200`: Single server PIN approval when NeedsApproval; normally false due CanOpen permission. No local manufactured approver IDs.

## Tests and limitations

Need recovered request operation label/form consistency, create service inactive control behavior and journal same-operator ownership. No driver/service writes run.

## Recommendations (not implemented)

Expose actual recovered command context, initial pending registration; align new-service active control with supported backend contract.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
