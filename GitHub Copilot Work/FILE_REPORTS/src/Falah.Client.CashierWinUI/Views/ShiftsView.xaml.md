# src/Falah.Client.CashierWinUI/Views/ShiftsView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `9cd690b909f3dcde298eea52fae41c5ff33ed4afe4b3ae0a7f82fe5078f3dc48`; worktree SHA256 `8556d0c5e0e5b562b4550a9fff6684265f97a9da8905f50a3a0c0720183bcc3f`; LF-equivalent `True`.

Actual role: Management active shift, history, blind-close and open-shift UI.

## Function/resource review

- `CanSeeShiftTotals` at `src/Falah.Client.CashierWinUI/Views/ShiftsView.xaml:82`: Management-only live opening/sales/payment/expense/order cards; restricted notice for cashier. Seven horizontal min130 cards plus gaps require970px before darkcardpadding; no horizontal scrolling/wrapping on active card.

- `History` at `src/Falah.Client.CashierWinUI/Views/ShiftsView.xaml:154`: History explicitly horizontally scrollable, unlike many management tables. Ten fixed columns total1390px plus gaps/padding despite StackPanelMin1020; horizontal scroll accommodates. Closed/open states, actor/timestamps and deficit/surplus textual badges distinct.

- `ReprintShiftOpenCommand` at `src/Falah.Client.CashierWinUI/Views/ShiftsView.xaml:246`: Opening/handover reprint buttons have explicit automationIDs/names; close print only closed. These queue real output, no preview or physical print invoked by review.

- `IsCloseShiftModalOpen` at `src/Falah.Client.CashierWinUI/Views/ShiftsView.xaml:277`: Close overlay Grid has no Grid.Row or RowSpan although root has Auto header row and star body; therefore defaults to header row0, potentially grows/covers header instead of full workspace. Same open-shift overlay omission. Native layout not observed; source structural inconsistency against other Grid.RowSpan2 dialogs.

- `IsCloseShiftCounting` at `src/Falah.Client.CashierWinUI/Views/ShiftsView.xaml:289`: Blind first stage accepts counted amount before preview; second stage reveals expected/actual/difference, mandatory reason/managerapproval. Reason/PIN/count fields have no IsEnabled busy binding; commands must freeze unknown/inflight state. UI016 separate fresh request ID issue.

- `CancelCloseShiftModalCommand` at `src/Falah.Client.CashierWinUI/Views/ShiftsView.xaml:283`: Close/header/cancel/recount actions all remain in markup during busy. Must rely model guards; previous source observed unconditional cancel on some management flow. No actual shift mutated.

- `IsOpenShiftModalOpen` at `src/Falah.Client.CashierWinUI/Views/ShiftsView.xaml:369`: Open fixed440 unscrollable stack can overflow with treasurywarning/error. Shiftname/openingfloat fields, optional authorized mainbalance and emptytreasury0openinginstruction. Static submit caption gives no retained retry context.

## Tests and limitations

NOT_EXECUTED. Need native row0 overlay geometry,7card width, blind count anti-leak, busy navigation/cancel and exact restart/retry. No callable declarations.

## Recommendations (not implemented)

Constrain overlays to full root RowSpan2, adapt cards and coherent pending locks only under approved source fix. Preserve management-versus-cashier visibility and server blind-close enforcement.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
