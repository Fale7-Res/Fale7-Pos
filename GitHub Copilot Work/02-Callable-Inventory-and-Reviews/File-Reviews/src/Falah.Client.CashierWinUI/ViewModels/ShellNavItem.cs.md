# src/Falah.Client.CashierWinUI/ViewModels/ShellNavItem.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `0acb8c21ee329f90563f9ff232e176179f02deba2b306c1d8bf0e012f93be93e`; worktree SHA256 `3c290ee3ffd372cdd77f9c1e9663a7d9fb6995d775ff1ff3b3817ee6d36f0506`; LF-equivalent `True`.

Actual role: Immutable shell page/group navigation metadata.

## Function/resource review

- `ShellNavItem` at `src/Falah.Client.CashierWinUI/ViewModels/ShellNavItem.cs:8`: Key,label,optional permission raw catalog string; permission is visibility metadata not server access authority. Null permission always-visible semantics owned ShellVM filter.

- `ShellNavGroup` at `src/Falah.Client.CashierWinUI/ViewModels/ShellNavItem.cs:18`: IsDirect Count==1, Single index0without guard. Builder only creates nonempty groups from allowed items, so normal invariant valid; public constructing empty group would throw if Single read. No data writes.

## Tests and limitations

Need builder permission/group invariants, no native test in this file.

## Recommendations (not implemented)

Preserve nonempty group invariant and test catalog entries reachable once.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
