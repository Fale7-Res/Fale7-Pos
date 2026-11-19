---
name: falah-financial-integrity
description: Use before changing or adding anything in Falah that computes, posts, or reports money - sales, refunds, voids, payments, treasury movements, expenses, payroll/salary accruals, driver custody, stock cost, dashboards, Daily Statement / Daily Summary / close snapshot. Gives the bases, source records, invariants and reconciliation checks that every figure must satisfy.
---

# Falah financial integrity

Every figure has one meaning, one formula, one date basis and one set of source records, written in
`docs/METRIC_DEFINITIONS.md`. A change that cannot be stated in those terms is not ready.

## Before writing code

1. **Name the figure** and find its row in `docs/METRIC_DEFINITIONS.md`. No row = add one first
   (meaning, formula, source, basis, recognition date, included/excluded states, VAT, refunds,
   pending items, how it reconciles).
2. **Pick the basis** — calendar day (Cairo, `BusinessClock.UtcRangeForBusinessDate`), business day
   (`BusinessDayId` on orders / movements / expenses; shifts via `BusinessDayShifts.Of`), or shift.
   One report = one basis. Never mix a frozen snapshot with live aggregates without saying so.
3. **Name the source records**: `Orders` (+ `OrderItems`, `ElectronicPayment`), refund events
   (`RefundEventQueries` — refund time, refund movement's business day, processing shift),
   `TreasuryMovements` (Status = Posted only), `Expenses`, `EmployeeLedgerEntries`
   (Salary accruals carry their earning date in `ReferenceId`), `StockMovements` (direction-signed;
   `UsageReversal` is an In), `DriverCustody`, `CashCounts`.
4. **Is it a policy question?** VAT on comps (S2), VAT registration (S1), proration, refund approval,
   who may see a balance (A1) — stop, record in `docs/ASSESSMENT.md` and `state.json` `owner_questions_pending`, change nothing.

## Invariants (each has tests; keep them green)

| Invariant | Where enforced / proven |
|---|---|
| Integer piastres only; no `double`/`decimal` money | `Money`, every DTO `*Piastres` |
| A sale counts when made; a refund when made; closed periods never move | `SalesRules`, `RefundDateBasisTests`, `CrossShiftRefundTests` |
| Unconfirmed electronic amounts are never collected cash | `SalesRules.PendingElectronicSales`, `CashFlowAndPendingTests` |
| Expected drawer = opening + cash sales − cash refunds from this drawer + in − out − expenses − drops | `ShiftReconciliation`, `ShiftCloseVerticalSliceTests` |
| Every shift and money movement belongs to a business day; a closed day's drawer nets to 0 | `BusinessDayAssociationTests` |
| A voided order is in no sales figure; its food is either restored or wasted, never both | `OrderVoidDispositionTests` |
| Ingredients are bought through Purchasing, never as an expense | `ExpenseCategories`, `ExpenseCategoryTests` |
| Wage cost = earnings − deductions; monthly salary accrues daily, paid once | `SalaryAccrualTests`, `ProfitabilityInputsTests` |
| Profit is on revenue ex-VAT and is an *estimate* while food cost uses current recipes × last purchase price | `OwnerDashboardQueryService`, `IsFoodCostComplete` |
| Every money command is idempotent by request id (replay check before validation) | each handler's `FindExisting*` |

## Reconciliation checks to run on any new figure

- Cash flow per account: opening + in − out = closing = balance at span end (`/api/reports/cash-flow`).
- Dashboard trend points sum to the headline net sales for the same span.
- Daily (calendar) totals over a month = the month report; business-day totals over the days =
  the same sales by `BusinessDayId`.
- Refund of last week's sale today: last week unchanged, today negative by the refund.
- Business-day close: snapshot equals the live reconciliation at close time; reopen + re-close
  writes a new revision.

## Verification

Write the failing integration test first (`tests/Falah.Server.IntegrationTests`, one
`FalahApiFactory` per test, seed helpers `SeedOpenBusinessDayWithFundedMainAsync`,
`SeedProductAsync`, `SeedProductWithRecipeAsync`). Assert exact piastres, not "> 0". Then run the
neighbours: Reporting, DailyStatement, OwnerDashboard, BusinessDayClose, ShiftClose, Refund,
CashFlowAndPending, Profitability classes.

## Done means

Formula documented, RED → GREEN recorded in the issue file (`All Issues/ISSUES/`), neighbours green,
estimates labelled as estimates in the API and on screen, open policy questions listed.
