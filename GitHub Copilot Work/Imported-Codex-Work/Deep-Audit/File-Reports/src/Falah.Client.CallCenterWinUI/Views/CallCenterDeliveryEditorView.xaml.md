# src/Falah.Client.CallCenterWinUI/Views/CallCenterDeliveryEditorView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `3645c88eab358dc65f711b2929843426a77eb9fce2834d4042aa753095660f76`; worktree SHA256 `fa6fe4684602329cfc44514c3f1d957cfcd3858704c7320883752ce2886d7067`; LF-equivalent `True`.

Actual role: RTL customer phone/address and delivery service draft overlay.

## Function/resource review

- `IsDeliveryEditorOpen` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterDeliveryEditorView.xaml:15`: Grid overlay with real header close, middle star ScrollViewer and pinned footer. MaxWidth760/margin28 supports vertical scroll; no ContentDialog focus confinement present here.

- `CustomerPhone` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterDeliveryEditorView.xaml:36`: TwoWay immediate phone and LookupCustomerCommand remain editable during IsLookingUpCustomer. Spinner only reports activity; corroborates UI007 phoneA/B stale reply reachability rather than disproving it with field locking.

- `SavedAddresses` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterDeliveryEditorView.xaml:49`: Combo visible when Count>0 and saves selected address; template shows area+street only, not building/unit, so multiple same-street addresses ambiguous until loaded into fields.

- `DeliveryServices` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterDeliveryEditorView.xaml:66`: TwoWay service selection separate area field. User may change fields during service reload; explicit error and reload command communicate network failure.

- `DeliveryBuilding` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterDeliveryEditorView.xaml:82`: Building/floor/apartment three equal columns plus street/landmark/notes, all real bound fields; telephone scope and Arabic headers aid input context. No local max lengths or numeric-only building assumption.

- `SaveDeliveryDetailsCommand` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterDeliveryEditorView.xaml:107`: Footer error wraps and save remains reachable through command. This is draft capture; actual customer upsert/order write happens checkout VM. Form edits no per-field CanEditOperation binding; modal opening/saving must be guarded in VM.

## Tests and limitations

NOT_EXECUTED. Test delayed phone lookup while phone changes (UI007), duplicate-street addresses, narrow/scaled layout, keyboard trap/return focus and pending checkout reopening. No callable declarations indexed for XAML.

## Recommendations (not implemented)

Bind address identity clearly and guard async lookup publication with captured phone/generation. Verify modal command gates and focus using native observation later.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
