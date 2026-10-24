# src/Falah.Client.CashierWinUI/Views/PayrollView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `aa4ff8884875a9052c1fb8dad6c6666cc41b80c9fd1513f712480944c6a7cb89`; worktree SHA256 `4842e9bfbd64f181656205f57c2e8787dea17d8f62f7c1f035141b31f123f5de`; LF-equivalent `True`.

Actual role: Monthly payroll run selection, lines, transition and pay overlay.

## Function/resource review

- `CreateRunCommand` at `src/Falah.Client.CashierWinUI/Views/PayrollView.xaml:29`: Period input disabled CanEditAction and creation visibility CanManage; refresh icon lacks accessible name. No generic recovered-operation retry button outside live run context.

- `Lines` at `src/Falah.Client.CashierWinUI/Views/PayrollView.xaml:42`: Line table shows employee paid status, base, additions/subtractions breakdown and net. Fixed470px amount columns plus320px run pane squeezes name at narrow workspace. Eager rows, vertical-only scrolling.

- `AdvanceRunCommand` at `src/Falah.Client.CashierWinUI/Views/PayrollView.xaml:133`: Next action visible only NextStatusLabel and enabled CanAdvance, both transient selected-run derived. This markup corroborates UI017 recovered transition cannot reach independent retry action when selection absent.

- `OpenPayCommand` at `src/Falah.Client.CashierWinUI/Views/PayrollView.xaml:137`: Pay button gated approved/unpaid + CanOpenPay; dialog only IsPayOpen, SelectedRun period and account. No automatic markup hydration from retained pay DTO; UI017 persistence recovery remains source-confirmed risk.

- `PayRunCommand` at `src/Falah.Client.CashierWinUI/Views/PayrollView.xaml:218`: Payment account disabled CanEditAction, submit static confirmation caption. Server debits and employee settlements promised explicitly, so financial write context is visible. No actual payout executed.

- `ActionNote` at `src/Falah.Client.CashierWinUI/Views/PayrollView.xaml:191`: Literal white on TextPrimaryBrush toast repeats dark-theme contrast root; action error separate danger toast. No native focus trap or live announcement declared.

## Tests and limitations

NOT_EXECUTED. Need new-process retained transition/pay UI recovery, reverse completion selected-line loads, wide monetary values, dark toast and narrow viewport. No callable XAML symbols.

## Recommendations (not implemented)

Expose pending operation reconciliation independent of selected run/dialog; retain exact original target/account and semantic toast colors.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
