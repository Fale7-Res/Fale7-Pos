# src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `263dc6671313a9bbb74357b9a5f3d795b8d4efc7e84878aeb1e2f2f35f81cf96`; worktree SHA256 `263dc6671313a9bbb74357b9a5f3d795b8d4efc7e84878aeb1e2f2f35f81cf96`; LF-equivalent `True`.

Actual role: CallCenter cart empty-state and note event bridge.

## Function/resource review

- `CallCenterOrderPanelView` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml.cs:9`: Every DataContextChanged attaches current vm.Cart.CollectionChanged with no removal of previous, no Unloaded detach or subscription tracker. Session caches VM while MainWindow recreates views after lock, keeping old UserControls alive through event handlers. Unlike cashier CartPanelView correct unsubscribe.

- `UpdateEmptyCartVisibility` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml.cs:19`: Shows hint iff ViewModel null/cart empty and hides actual scroll; correct empty switch for subscribed current context. Old collection callbacks read new context if reused, unnecessary updates.

- `OnNoteChipClicked` at `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml.cs:32`: Requires actual CartLine/Tag and VM; delegates to guarded AppendNoteText. No direct network/printing in code-behind.

## Tests and limitations

Missing lock/unlock repeated-subscription count and garbage-collection weak reference test. Native binding/visual result not observed.

## Recommendations (not implemented)

Detach prior VM collection on DataContext change/Unloaded; reattach once on Loaded. Source-level memory retention risk, actual long-session growth unmeasured.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
