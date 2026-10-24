# src/Falah.Client.CallCenterWinUI/Controls/PermissionDeniedStateView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `e5a58eee941fb30f8d878f1132f8f760609959ad50e253e373703630ea07f757`; worktree SHA256 `e5a58eee941fb30f8d878f1132f8f760609959ad50e253e373703630ea07f757`; LF-equivalent `True`.

Actual role: CallCenter static permission-denied presentation.

## Function/resource review

- `MessageBlock` at `src/Falah.Client.CallCenterWinUI/Controls/PermissionDeniedStateView.xaml:18`: Arabic role-denied explanation, title and lock icon in wrapping centered layout. Comment says per-screen guard but this control alone enforces nothing: actual visibility and server auth must be verified separately.

- `Foreground` at `src/Falah.Client.CallCenterWinUI/Controls/PermissionDeniedStateView.xaml:14`: Icon uses StaticResource WarningBrush; muted text and title from station typography. No retry, privilege escalation or PIN request in denied panel.

## Tests and limitations

NOT_APPLICABLE authorization decisions; verify blocked screen never renders unauthorized data behind overlay, and backend rejects independently.

## Recommendations (not implemented)

Do not equate denied panel registration with enforced authorization; ensure explanatory accessible state.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
