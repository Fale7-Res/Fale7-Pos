# src/Falah.Client.CashierWinUI/Controls/PermissionDeniedStateView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `7feb09b00a43e9d9249b94abdbca1fea43853916d3eebc46bc9a7443b0dff044`; worktree SHA256 `6a074d8e2f8c1427172b31683359b36868005acd9d77162598dbcda6278095e2`; LF-equivalent `True`.

Actual role: cashier static permission-denied presentation.

## Function/resource review

- `MessageBlock` at `src/Falah.Client.CashierWinUI/Controls/PermissionDeniedStateView.xaml:18`: Arabic role-denied explanation, title and lock icon in wrapping centered layout. Comment says per-screen guard but this control alone enforces nothing: actual visibility and server auth must be verified separately.

- `Foreground` at `src/Falah.Client.CashierWinUI/Controls/PermissionDeniedStateView.xaml:14`: Icon uses ThemeResource WarningBrush; muted text and title from station typography. No retry, privilege escalation or PIN request in denied panel.

## Tests and limitations

NOT_APPLICABLE authorization decisions; verify blocked screen never renders unauthorized data behind overlay, and backend rejects independently.

## Recommendations (not implemented)

Do not equate denied panel registration with enforced authorization; ensure explanatory accessible state.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
