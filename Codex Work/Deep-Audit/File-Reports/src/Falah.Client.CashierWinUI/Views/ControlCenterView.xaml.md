# src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `ed23bd5b189f84cb0646e92cd197c5450db0d80b9d02c904d6d7af611b942eee`; worktree SHA256 `cc8d56dbe18e27540467b08b2453908e87330acba5535f43bacf8764f1812aea`; LF-equivalent `True`.

Actual role: Business-day selection, reconciliation, saved close snapshot, blockers/checklist, exceptions and financial overlays.

## Function/resource review

- `SelectedHistoryDay` at `src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml:30`: History selection disabledCanEditAction; open/count/safedrop separate permission/state gates. Toolbar many horizontal controls require available width; refresh icon unnamed.

- `CanSeeReconciliation` at `src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml:53`: Live reconciliation six equal cards show gross/refund/net/expense/custody/difference, permission-gated. Snapshot section separately labels immutable close-time values, close actor and snapshot/operationID. No client recompute of close result in markup.

- `HasBlockers` at `src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml:145`: Server blockers message/code lead active-day close section; zero blockers text says server rechecks at confirmation. IsEnabledCanClose not merely color; checklist required/completed icons and root command separate.

- `Exceptions` at `src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml:211`: Live exceptions message+severity, review decisions explicitly do not waive financial blocker. Severity rawEnglish text unlike otherlocalizedstatus tables. Open review gated permission.

- `RetryOperationCommand` at `src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml:255`: Generic pending retry exists but is inside ScrollViewer visibleHasBusinessDay. Recovered open-day pending with no transient/current day could hide retry unless VM restores modal/context; source hypothesis needs exact VM restart trace, not claimed confirmed here.

- `IsDayActionOpen` at `src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml:282`: Open optional YYYY-MM-DD with serverCairo default, reopen mandatoryreason, confirm disabled fields byCanEditAction.500max620 scroll. Close controls must preserve unknown result, same-process tests cover some action kinds.

- `IsSafeDropOpen` at `src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml:304`: Describes real internaldrawer→main transfer and open-shift requirement, amount/reason/conditionalmanagerPIN locked. Globalretained coordination belongsVM, no treasury writes executed.

- `ExceptionTarget` at `src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml:340`: Review overlay allowsstatus/reason and explicitly non-financial waiver; CashCount nestedlast with ownDataContext. Modal exclusivity/focus management not defined in XAML.

## Tests and limitations

NOT_EXECUTED. Need fresh-process open pending/no day retry visibility, live versus immutable snapshot, narrowtoolbar, all6cards and exceptions permissions. No callable declarations.

## Recommendations (not implemented)

Ensure recovery action remains visible independently of HasBusinessDay and transient modal state; keep server recheck and snapshot semantics explicit.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
