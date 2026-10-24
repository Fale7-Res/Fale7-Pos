# src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Close.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `22bf59a61b7ce3adf137dc1862e158bd8f07e46c23fe2b7d8fe5acd5fe9b88c3`; worktree SHA256 `22bf59a61b7ce3adf137dc1862e158bd8f07e46c23fe2b7d8fe5acd5fe9b88c3`; LF-equivalent `True`.

Actual role: Management two-step blind close; separate presentation of existing handler.

## Function/resource review

- `OpenCloseShiftModal` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Close.cs:38`: Checks CanCloseShift and active shift; clears preview/count/reason/PIN. Does not check other management write busy/pending because no shared gate defined here.

- `PrepareCloseShiftAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Close.cs:59`: Valid nonnegative count and active shift, server expected preview. Busy guard only IsClosingShift; BackToCloseShiftCount unconditionally clears preview while async confirmation can run, property changes can disrupt live method reads.

- `ConfirmCloseShiftAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Close.cs:91`: Manager discrepancy reason+approval validated; creates fresh Guid every submit and no immutable retained DTO/outcomeUnknown history. ActiveShift and preview reread after awaited approval could be changed by reload/other UI; server validates but wrong target/context risk. Normal cancellation closes if !busy only, then new attempt uses new identity.

- `CancelCloseShiftModal` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Close.cs:50`: Busy blocks cancel while request active, but after unknown failure busy false allows discard; no global session pending registration. Success clears shared shift and reloads authoritative history.

## Tests and limitations

No actual shift write run; need fake HTTP commit/lost-response retry identity tests and UI lock/navigation assertions. Same domain handler does not mean same client safety as cashier.

## Recommendations (not implemented)

Freeze close request once, durable owner state, global pending gate and immutable target/preview snapshot across approval await.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
