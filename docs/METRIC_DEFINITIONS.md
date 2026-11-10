# Metric definitions

What every financial figure the server reports means: where it comes from, how it is computed, and
which time basis decides what "today" includes. Amounts are integer piastres everywhere (1 ج = 100
piastres); instants are stored in UTC and turned into Cairo dates only when grouping.

Kept current by the remediation work in `REMEDIATION_PLAN.md`. A figure not listed here has no
agreed definition yet and should not be relied on for reconciliation.

## Time bases

There are three, and every report uses exactly one of them. They answer different questions and
will legitimately disagree for a restaurant that trades past midnight.

| Basis | What "the period" contains | Used by |
|---|---|---|
| **Calendar day** (Cairo) | Events whose own timestamp falls between two Cairo midnights (`BusinessClock.UtcRangeForBusinessDate`). Sales by order creation time, refunds by refund time, expenses by their `BusinessDate` (stamped from the Cairo date they were entered), stock and treasury movements by their timestamp. | Daily Statement (`/api/reports/daily-statement`), Owner Dashboard, the sales / tax / expense / refund / top-items / category / hourly / food-cost reports when called with a date or date range |
| **Business day** | Everything posted under one business day record, from opening to close (and again after a reopen): orders and treasury movements by their `BusinessDayId`, refunds by their movement's `BusinessDayId`, expenses by `BusinessDayId`, shifts by the business day they were opened under (`BusinessDayShifts`). | Business-day reconciliation (`/api/reports/business-day/{id}`), Daily Summary (`/api/reports/daily-summary/{id}`), the business-day close snapshot, close blockers and close exceptions |
| **Shift** | The shift's own orders for sales; refunds processed in the shift; movements tagged with the shift. | Shift summaries and history, the blind-close expected cash, the handover slip |

Rules that hold on every basis:

- **A sale counts in the period it was made; a refund counts in the period it was made**, whichever
  period the refunded sale belongs to. A period already reported never changes when an old sale is
  refunded later. A period with a refund of an earlier sale can therefore show negative net sales.
- Cancelled orders are not sales anywhere.
- Every shift opens under an open business day, and a business day cannot close while a shift is open,
  so every sale made through a shift carries a business day. (Before 2026-10-24 a zero-float shift
  could open without one; such orders have `BusinessDayId = null` and appear only on the calendar and
  shift bases.)

## Sales report (`SalesRules.ComputeSalesReport`)

Inputs: the period's non-cancelled orders, the period's refund events (`RefundEventQueries`), and
which cash-on-delivery orders are still unsettled.

| Field | Meaning | Calculation |
|---|---|---|
| `OrderCount` | Sales made in the period | count of the period's orders |
| `GrossSales` | What the period's sales charged | Σ `Order.Total` (after discount and comp, **including VAT and delivery fee**) |
| `Refunds` | Money handed back in the period | Σ refunds made in the period |
| `NetSales` | Revenue of the period, net of its refunds | `GrossSales − Refunds` |
| `ListSales` | The period's sales at menu prices | `GrossSales + OffersValue + CompsValue + ManualDiscounts` |
| `OffersValue` | Price-rule reductions | Σ per line `max(0, list − charged) × qty` |
| `CompsValue` | Hospitality given away (manager-approved) | Σ `Order.CompAmount` |
| `ManualDiscounts` | Cashier discounts | Σ `Order.Discount` |
| `PendingElectronicSales` | InstaPay / wallet claims not yet confirmed | Σ the pending electronic leg's own amount (the whole order for a standalone electronic sale, only the electronic part of a split) |
| `CashWithDrivers` | Cash-on-delivery money not yet in the restaurant's custody | Σ `Order.Total` of the period's COD orders with no settled custody |
| `CollectedSales` | What the restaurant actually holds from the period's trade | `NetSales − PendingElectronicSales − CashWithDrivers` |
| `AverageOrderValue` | | `NetSales / OrderCount` (0 for no orders) |
| `Cash` / `Card` / `InstaPay` / `Wallet` | Channel totals | the period's sales through the channel (split legs as-is; electronic legs only once confirmed; single-method orders only once paid) **minus** the period's refunds through that channel |
| `Pickup` / `Takeaway` / `Delivery` | Order-type totals | the period's sales of that type minus the period's refunds of orders of that type |

`NetSales` is not profit, not cash flow and not collections: it includes VAT and delivery fees, and
money not yet received.

## Expected drawer cash (`ShiftReconciliation`)

`opening + gross cash sales − cash refunds paid from this drawer + cash in − drawer expenses − cash
outs / employee advances / withdrawals − safe drops`.

- Gross cash sales: the shift's own paid cash orders (and cash legs of splits) at full value.
- Cash refunds: posted `refund` movements from `tre-drawer` tagged with this shift — including
  refunds of orders sold in an earlier, closed shift.
- The close's own reconciliation and handover movements are not inputs.
- The expected figure a shift was closed with is stored on the shift and never recomputed.
- The close also freezes the whole closing statement in the same transaction. That covers sales,
  payment breakdown, expenses, cash, handover, open items and the business day. It is stored as JSON on
  the close's audit event (`ShiftCloseStatements`, UI-REQ-002).
- A reprint prints that frozen copy, so a later confirmation, refund or expense never changes a closed
  shift's paper.
- Only a shift closed before the statement existed is rebuilt from current records, and its paper says so.
- The statement's business day is the business day the shift ran in (`BusinessDayId`), not the calendar
  date of the opening.

## Tax report (`SalesRules.ComputeTaxReport`)

Each order's tax as frozen at checkout (never recomputed from today's rule). `RefundTaxImpact` is
the proportional tax handed back by the period's refunds (`orderTax × refund / orderTotal`, on the
refund's date); `NetTax = Tax − RefundTaxImpact`. Open question (S2): tax is computed before the
comp is subtracted, so a fully comped order records tax on a payable of zero — pending the
accountant.

## Owner Dashboard (`/api/reports/owner-dashboard`, calendar basis)

| Section | Figures | Source and calculation |
|---|---|---|
| Cash position | as the sales report, plus `TreasuryNow` and `SettledFromDrivers` | sales report for the range; balances are **current** (all posted movements ever), not the range's; settled-from-drivers = posted `driver_settlement` movements in the range |
| Leakage | offers, comps, manual discounts, refunds (amount and count), waste (value and count), expenses | sales report buckets; refunds amount and count both from the period's refund events; waste = loss-type stock movements' `LossValuePiastres`; expenses by `BusinessDate` |
| Profitability | net sales ex VAT, known food cost, gross profit and %, wages and %, operating result and % | **revenue** = `NetSales − NetTax` (assumes the restaurant is VAT-registered; unchanged with no tax rule). **Food cost** = sold lines × today's recipe × each ingredient's last purchase price, plus order packaging — an *estimate*; withheld entirely when no sold line has a known cost; `IsFoodCostComplete = false` when any line is unknown (then profit is an upper bound). **Wages** = Σ (credit − debit) of DailyWage, Salary (incl. daily monthly-salary accruals), Overtime, ExtraShift, Bonus, OtherEarning, Deduction recorded in the range; % withheld when nothing was recorded. **Operating result** = revenue − food cost − expenses − wages; deliberately not called net profit (rent, depreciation etc. are not recorded). |
| Attention | open shifts, shift differences, unauthorised differences, unsettled custody, pending electronic count / **amount** / oldest age, open exceptions, close blockers, open-day age, unusual cost changes | shifts by calendar `BusinessDate` in range; pending electronic = the pending-electronic report (current) |
| Stock | tracked items, low-stock items | balance = direction-signed sum of all stock movements (In +, Out −); low = balance ≤ minimum (minimum > 0) |
| Obligations | supplier outstanding, purchases in range, employees owed, overdue / unscheduled invoices | supplier ledger (debit − credit); employee ledger (credit − debit > 0); invoice aging as of today |
| Trend | net sales, collected, cash with drivers, expenses, orders vs the same span one week earlier; hourly (one day) or daily series | the same reports for the comparable span; series points = sales at full value minus refunds, bucketed by the instant each happened (Cairo hour / date), so they add up to the headline net sales |

## Cash flow (`/api/reports/cash-flow`, calendar basis)

Per account (main, drawer, bank, InstaPay, wallet): opening = all posted movements before the span;
in / out by movement type within the span; closing = opening + in − out (equal to the balance at the
span's end). A transfer is an outflow of one account and an inflow of the other. This is the figure a
physical count of each account is checked against. Also on the Daily Statement (`CashFlow`).

## Pending electronic payments (`/api/reports/pending-electronic`, current state)

Every order whose electronic leg is still pending: the leg's own amount (whole order for a
standalone InstaPay / wallet sale, the electronic part of a split), when it was rung up, age in
minutes, shift. Count, total and oldest age also on the dashboard's Attention section. A pending
payment blocks the business-day close.

## Voids (`/api/reports/voids`, calendar basis by void time)

Orders voided in the span: an unconfirmed electronic sale cancelled, or an undelivered cash-on-delivery
order whose delivery failed or came back. Value, reason, who, when, and what became of the food
(`prepared` — recorded as waste at cost; `not_prepared` — ingredients returned to stock; null — a void
recorded before the operator was asked). A voided order is in no sales figure. Also on the Daily
Statement (`Voids`).

## Refund activity (`/api/reports/refunds`)

Every refund made in the span (by refund time) or processed in a shift, with the original sale's time
(`OriginalSaleAtUtc`), the shift the sale belonged to and the shift that processed the refund.

## Business-day reconciliation, Daily Summary and close snapshot (business-day basis)

| Figure | Source |
|---|---|
| Sales | the sales report over orders with this `BusinessDayId` and refunds whose movement carries it |
| Expenses | expenses with this `BusinessDayId` |
| Treasury | net of posted movements carrying this `BusinessDayId` per account — includes shift close handovers and differences (since 2026-10-24) and refunds made in the day; a fully closed day's drawer nets to zero |
| Shifts | shifts opened under the day (`BusinessDayShifts`); expected / counted / shortage / overage from each shift's stored close |
| Driver custody | unsettled custody of the day's orders; settled = posted `driver_settlement` movements of the day |
| Close snapshot | the reconciliation, exceptions and checklist serialized at close; immutable; a reopen + re-close writes a new revision |

## Daily Statement (`/api/reports/daily-statement`, calendar basis)

Composition only — every section is one of the reports above for one Cairo date (sales, tax, products,
hourly, expenses + lines, shifts opened that date, treasury movements, current balances, employees'
day, inventory movements, delivery, refunds, audit trail, printing, backup, cash flow, voids). It is
the calendar-day view; for the day as closed, read the business-day close snapshot.

## Driver daily statement (`/api/delivery/drivers/{id}/daily-statement`, business-day basis)

UI-REQ-001. The statement's rows are the driver's orders whose own `BusinessDayId` belongs to the selected
business date. The driver is the one currently on the delivery, so a reassigned order moves with it and its
row names the previous driver.

**Per row**

| Figure | Definition |
|---|---|
| Food | `Total − DeliveryFee` |
| Delivery charge | The fee charged to the customer. It is not driver pay. |
| To collect | `DeliveryRules.DeliveryCashResponsibility` |
| Prepaid | `max(0, Total − Refunded − ToCollect)` |
| Collected | The amount recorded on the delivery result |
| Settled | The driver's custody for the delivery is settled |

**Voided orders** are listed with to collect = prepaid = 0 and are excluded from every total.

**Totals** are sums over the non-voided rows. Refunds are shown separately and never netted into sales.
Failed and returned deliveries are counted over all rows.

The payment filter matches the order's own payment method. The screen and both papers use the same server
rows.
