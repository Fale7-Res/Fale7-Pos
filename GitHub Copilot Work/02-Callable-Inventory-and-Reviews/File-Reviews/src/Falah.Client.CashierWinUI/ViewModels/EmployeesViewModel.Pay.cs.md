# src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.Pay.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `59c3a4791465fa5638e881076de6639a202dd2186f1187079ebcd24636ffd81f`; worktree SHA256 `59c3a4791465fa5638e881076de6639a202dd2186f1187079ebcd24636ffd81f`; LF-equivalent `True`.

Actual role: Standalone earned-balance employee payment.

## Function/resource review

- `OpenPay` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.Pay.cs:56`: Uses editable employee row, prefill only positive balance, main default account, new ID once; negative balance accurately no amount due. Known accounts Main/Bank/InstaPay exclude drawer.

- `SubmitPayAsync` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.Pay.cs:76`: Pending DTO branch before PayEmployee null, so core retry can execute without transient row; native button still hidden if dialog not restored. Fresh money positive<=shown balance; server cap must handle concurrency. Resolves real employees_ledger manager approval, optional reason.

- `CompletePayAsync` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.Pay.cs:79`: Durable helper preserves exact ID/actor/amount across uncertainty; server balance-after used, closes row, clears requestId and reloads. Error text unknown handled helper. PIN field not explicitly cleared on close/success until next open, retaining sensitive string longer than needed.

## Tests and limitations

Need reconstructed payment dialog/retry reachability and concurrent updated balance tests. No employee payout performed.

## Recommendations (not implemented)

Open recovered payment panel from retained employee identity; clear PIN fields on permitted close/resolution, keep server balance authority.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
