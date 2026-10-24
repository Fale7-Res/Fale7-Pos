# src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `c9e49112b32f8415f630a9f885695186d2b7bcc37ad2e7017d77315a6bed6792`; worktree SHA256 `27b80026d0447e03faf17e6eaad32fe5fde3ee3cd004f9503d561dfd6adc6822`; LF-equivalent `True`.

Actual role: Dispatch board, real driver directory, assignment/outcome/custody settlement.

## Function/resource review

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs:34`: Real board then active drivers, no auto-refresh timer/version/cancellation; row elapsed/slow are computed DateTimeUtc getters but no timer notifications so delay ages only repaint/reload. BoardSummary strings real counts, cannot claim realtime updates continuously.

- `BoardRow` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs:49`: Customer/address/phone from actual delivery DTO, distinguishes pendingElectronic vs prepaid vsCOD. Pending compare exact Pending enum-style unlike checkout lowercasepending; server DTO uses enum names parent cross-check. Amount0shows dash even prepaid explained text.

- `MarkReadyAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs:174`: Permission+edit gate, real status transition no retained DTO/outcomeUnknown identity but intended monotonic state setter; first unknown after committed ready should reconcile board rather than blindly error. Session busy protects in-flight via OnIsBusyChange.

- `ConfirmAssignAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs:221`: Retained exact assignment branch before transient target, stable GUIDperdialog, requires dispatcher. Same pattern outcome and settle. Field initializers restore global per-command keys but constructor no target/modal hydration/initial pending registration, actual native retry reachability needs view validation.

- `ConfirmOutcomeAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs:319`: Known result UI defaultsdelivered, parses amount ifdelivered; failedreason whitelist options provided but method does not require selected reason or amount<=expected locally, server validates. DTO frozen with requestId/actor before send; no fake delivery event.

- `OpenDriverAccountAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs:392`: Sets target/clears account/stats, begins accountread then stats. SelectedStatsPeriod change can start its own statsread too; version protects most stale responses. However after first await without currentdriver check it launches olddriver stats with newest statisticsVersion, can replace newdriver stats if account switched/closed midway. Parent statisticspartial alone guard not sufficient across outer sequence.

- `LoadDriverAccountAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs:407`: Version protects account response/finally. CloseAccount increments both account/statistics generations, clears targets; correct baseclose behavior, but outer Open continuation can start newstats after close and bypass invalidation.

- `SettleDriverAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs:444`: Pending exact request branch; fresh parses money/difference vs actual custody, reason ifdiff, stacked manager approval only ifdiff and missing permission. After success reload authoritative account+board; can reset _settleRequestId before refresh, but helper cleared only confirmed result.

- `CompleteOutcomeAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs:325`: Completes2otherhelper actions with serverDTO/board reload; no direct SQLorprinter. Cash disbursement/ledger atomicity remains handler review. Driver PIN fields not cleared success/close until nextopen.

## Tests and limitations

Need fake delayed accountAopenB/close outer stats continuation, reconstructed assign/outcome/settle visible retry, board refresh-age behavior, multi-user global journal scope. No delivery/settlement writes or physical print.

## Recommendations (not implemented)

Bind all async work to driver/session generation after EACHawait, hydrate pending operation panel, refresh aging intentionally and clear sensitive fields.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
