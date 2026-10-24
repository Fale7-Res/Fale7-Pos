# src/Falah.Client.CashierWinUI/Views/UsersView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `debceb8f3649010489d74464b0211a977ce531c41a50bdd2346aff586e5c548f`; worktree SHA256 `c527f98477b8576bc04336c4dc87d79145b168ea79ea815e7d0cfc9a8bfe2c12`; LF-equivalent `True`.

Actual role: User selection, effective permission editing, creation and PIN-change overlays.

## Function/resource review

- `CanEditSelected` at `src/Falah.Client.CashierWinUI/Views/UsersView.xaml:63`: Permission ItemsControl and save use selected-user edit gate; individual grants CanActorGrant, owner banner blocks changes conceptually. Client display alone cannot prove server authorization; coordinator owns actual privilege paths.

- `RetryUserActionCommand` at `src/Falah.Client.CashierWinUI/Views/UsersView.xaml:172`: Generic pending retry button visibleHasPendingAction outside selected-user modal context, unlike Payroll; offers recovery surface even when dialogs absent. API auth proof token mutation UI020 remains separate from markup.

- `SelectUserCommand` at `src/Falah.Client.CashierWinUI/Views/UsersView.xaml:140`: Root scoped user selection, role translation and inactive badge, fixed320pane. Load/error/empty occupy same row as list, VM must ensure visibility coherence and stale rows not actionable.

- `CreateUserCard` at `src/Falah.Client.CashierWinUI/Views/UsersView.xaml:188`: Max680x800 stretched, header/star-scroll/footer named for geometry probe. Username/role/permissions and two PasswordBoxes disabledCanEditAction; PIN6maximum and explanatory4..6digits rule, no actual PIN exposed. Role text explicitly says grants independent of label.

- `IsPinChangeOpen` at `src/Falah.Client.CashierWinUI/Views/UsersView.xaml:232`: Self currentPIN conditional, targetname visible, new+confirm6max and busy ring/retry caption. Full scroll keeps tall content reachable. Retained sensitive payload storage is VM/service concern already sent security coordinator.

- `ActionNote` at `src/Falah.Client.CashierWinUI/Views/UsersView.xaml:182`: Literalwhite success text on themeTextPrimary background repeats dark contrast issue. Refresh has explicit Arabic accessible name, improvement over other management views. No native focus trap/live announcements declared.

## Tests and limitations

NOT_EXECUTED. No authentication automation, accounts or PIN changes. Need true token-bearing proof, wrong-user proof header assertion, process-recovered dialog context, all-permission actor boundaries and dark/highcontrast native tests. No callable declarations.

## Recommendations (not implemented)

Keep immutable actor/target reconciliation and secure journal design separate from masked UI, and use semantic paired feedback colors.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
