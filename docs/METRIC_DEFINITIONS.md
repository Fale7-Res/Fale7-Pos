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
| Leakage | offers, comps, manual discounts, refunds (amount and count), waste (value and count, and how many have no known cost), expenses, delivery collection shortfall (amount and count) | sales report buckets; refunds amount and count both from the period's refund events; waste = loss-type stock movements' `LossValuePiastres` — a movement with no known cost adds nothing to the value and is counted in `WasteUnknownValueCount` (AUD-14), so the value is a floor; expenses by `BusinessDate` (a reversal is a negative line, so a reversed expense nets to zero); **delivery collection shortfall** = Σ (custody expected − collected) over the period's non-cancelled orders (by sale time) delivered for less than was due — approved, see below (AUD-03) |
| Profitability | net sales ex VAT, known food cost, gross profit and %, wages and %, operating result and % | **revenue** = `NetSales − NetTax` (assumes the restaurant is VAT-registered; unchanged with no tax rule). **Food cost** = sold lines × today's recipe × each ingredient's last purchase price, plus order packaging — an *estimate*; withheld entirely when no sold line has a known cost; `IsFoodCostComplete = false` when any line is unknown (then profit is an upper bound). **Wages** = Σ (credit − debit) of DailyWage, Salary (incl. daily monthly-salary accruals), Overtime, ExtraShift, Bonus, OtherEarning, Deduction recorded in the range; % withheld when nothing was recorded. **Driver pay** (`DriverCompensationPiastres`, AUD-02) = Σ `DeliveryFee` of the period's non-cancelled orders (by sale time, the same selection as net sales) whose delivery's final outcome is Delivered (a return after delivery earns nothing — `DriverSettlementRules.FeeEntitlement`); the fee is inside net sales and the driver keeps all of it (owner rule 2026-11-08), so it is a cost. Deliveries not resolved yet are reported as `DriverCompensationPendingPiastres` / `Count` and not deducted. **Operating result** = revenue − food cost − expenses − wages − driver pay − delivery collection shortfalls classified as approved discount or write-off (unresolved and still-owed shortfalls are shown beside it, never inside it — AUD-03); deliberately not called net profit (rent, depreciation etc. are not recorded). Before 2026-11-13 driver pay and shortfalls were not deducted, which overstated the result by every completed delivery's fee. |
| Attention | open shifts, shift differences, unauthorised differences, unsettled custody, pending electronic count / **amount** / oldest age, open exceptions, close blockers, open-day age, unusual cost changes | shifts by calendar `BusinessDate` in range; pending electronic = the pending-electronic report (current) |
| Stock | tracked items, low-stock items | balance = direction-signed sum of all stock movements (In +, Out −); low = balance ≤ minimum (minimum > 0) |
| Obligations | supplier outstanding, purchases in range, employees owed, overdue / unscheduled invoices | supplier ledger (debit − credit); employee ledger (credit − debit > 0); invoice aging as of today |
| Trend | net sales, collected, cash with drivers, expenses, orders vs the same span one week earlier; hourly (one day) or daily series | the same reports for the comparable span; series points = sales at full value minus refunds, bucketed by the instant each happened (Cairo hour / date), so they add up to the headline net sales |

### What "collected" on the dashboard is — and is not (AUD-05 / AUD-07)

`CollectedSales` is sales-based: net sales less pending electronic and less cash-on-delivery money not yet
settled. A settled delivery counts at its full total, although the driver kept his fee out of it and an
approved collection shortfall was never paid. It is therefore **not** the cash that reached the treasury; the
screen labels it "المبيعات المحصّلة" and shows `SettledFromDriversPiastres`, `DriverFeesRetainedPiastres` and the
shortfall on their own lines. For the cash that actually moved, read the cash-flow report below.

## Delivery fee (checkout, AUD-01)

The delivery charge on an order is the named delivery service's (zone's) configured `FeePiastres` (none
configured = 0), decided by the server. A request whose fee differs is refused with the zone's fee in the
message (the customer was quoted the screen's total, so it is not silently replaced); an unknown or retired
service is refused; only delivery orders carry a fee. The service name frozen on the order is the server's.
The fee stays fixed on the order afterwards: re-pricing a zone never changes a past order or its driver pay.

## Delivery collection shortfall (AUD-03)

Recording a delivery as delivered with less collected than the driver had to collect needs a reason **and** the
authority that approves a settlement difference (`delivery_settlement_approve`, or a manager's approval issued
for it). The custody then expects what was collected, so the settlement shows no difference and the gap is
counted exactly once. Collecting more than was due still needs only a reason.

**A shortfall is not a loss by itself (2026-11-13).** Until a manager classifies it
(`POST /api/delivery/{deliveryId}/collection-shortfall/classify`, `delivery_settlement_approve` or a manager's
approval, evidence required, once per delivery, stored in `CollectionShortfallResolutions`) it is **unresolved**:

| Classification | Meaning | In the operating result | Shown as |
|---|---|---|---|
| (none) | unresolved | **not deducted** | `DeliveryShortfallUnresolvedPiastres`, owner report "فروق تحصيل غير محسومة" (status partial), control-center warning `shortfall-{id}` |
| `customer_receivable` | the customer still owes it | not deducted | receivable "مديونية على العملاء" |
| `driver_liability` | the driver answers for it | not deducted; **not** added to his settlement (owner decision DRV-LIAB) | receivable "مديونية على السائقين" |
| `approved_discount`, `write_off` | given up by an authorised decision | **deducted** (`ProfitabilityDto.DeliveryCollectionShortfallPiastres`) | realised reduction |

Leakage `DeliveryCollectionShortfallPiastres` is all shortfalls of the period's sales (by sale time); the delivery
outcomes report's `CollectionShortfallPiastres` is all shortfalls by delivery time. The accounting effect of each
classification is provisional until the accountant confirms it (AUD-03-CLS in docs/ASSESSMENT.md).

## Expense reversal (AUD-04, revised 2026-11-13)

A mistaken expense is cancelled by `POST /api/expenses/{id}/reverse`, never deleted: a linked expense row
`exp-rev-{id}` of the same category, shift, account and date with a negative amount, and a movement crediting the
account it was charged to. Every expense total nets the pair. **What happened to the money is required**
(`CashDisposition`), because only money that is physically there may be credited back:

| Disposition | Meaning | Movement | Category (MoneyFlowRules) |
|---|---|---|---|
| `not_paid` | posting error — the money never left | `expense_correction` | ExpenseCorrection — not income |
| `cash_returned` | it left and has come back in hand | `expense_refund` | ExpenseReimbursement — real cash in, not a sale |

Money that left and stays gone is not reversible (the expense is true). `ReversedAmountPiastres` reverses part
(5000 typed, 500 paid: reverse 4500 — the drawer is never credited for the 500 that left). Allowed only while
the original's shift is open; a drawer expense needs `shifts_manage` or a manager's approval, a main-treasury/bank
expense needs `expenses_main_treasury`. One reversal per expense; a retry replays it. The drawer's expected cash
reads the expense rows, so after a posting-error correction the physical count matches with no difference
(`ExpenseReversalTests`).

## Treasury entry correction (P3)

`POST /api/treasury/movements/{id}/correct` posts the linked opposite (`movement_correction`, id `tm-corr-{id}`,
`ReferenceType = treasury_movement`) of a hand-typed entry (`transfer`, `safe_drop`, `cash_in`, `cash_out`,
`owner_withdrawal`, deposits) that **did not physically happen** — the operator must confirm it; money that moved
and came back is a new ordinary entry. Part of the amount may be corrected. Once per entry; `treasury_manage` or a
manager's approval; the source of the correction must hold the money; a shift-tagged entry only while that shift is
open, and `ShiftReconciliation` counts the correction by direction (into the drawer = cash in, out = withdrawal).
Sales, refunds, settlements, payroll, supplier payments and close postings are corrected in their own workflow.

## Supplier credit / purchase correction (P3)

`POST /api/purchases/{id}/credits` records a credit on the supplier ledger linked to the invoice:
`invoice_error` → `purchase_correction`, `supplier_credit` → `supplier_credit`, `goods_returned` →
`supplier_return` (the returned quantities leave stock as `SupplierReturn` Out at the invoice's own unit cost, and
the credit is their value — never more than received minus earlier returns). The invoice as received (total,
lines, payments) is kept; `RemainingAmount` and supplier outstanding (debit − credit) fall by the credit. No money
moves. Never more than is still owed: a credit on a paid invoice means the supplier owes cash back — owner decision
SUP-REFUND. Food cost keeps using the received unit cost (AUD-06).

## Cash handover receipt (P4)

The close still moves the counted cash drawer → main (retries and recovery unchanged), but naming a receiver is
not a receipt. `Shifts.HandoverReceiptStatus`: `pending` until the receiver confirms
(`POST /api/shifts/{id}/handover/acknowledge`) from their own session or with their PIN typed at the till (an
approval for `shifts_manage` whose approver must be the named receiver); never the cashier who handed it over.
`acknowledged` when the counted amount equals the handover; `disputed` (note required, critical audit) when not —
no money is posted for the difference (owner decision HANDOVER-DIFF). A closer who names themself as receiver is
acknowledged at the close. `null` with a handover = closed before receipts existed ("not_recorded"). Pending and
disputed handovers are control-center exceptions (not close blockers — HANDOVER-BLOCK) and owner-report lines.

## Money flow classification (`MoneyFlowRules`, P1)

Every posted movement type is one category: CustomerCollection (`sale_collection`), DriverRemittance
(`driver_settlement`), CustomerRefund (`refund`, `void_return`), Expense, ExpenseCorrection, ExpenseReimbursement,
SupplierPayment, Payroll, DriverPay (`driver_fee_payout`), OwnerDrawing, ExternalFunding (`owner_deposit`,
`capital_injection`, `other_external_inflow`), InternalTransfer (`transfer`, `safe_drop`, `shift_handover`,
`shift_opening_float`, `cash_in`, `cash_out`), CashCountDifference, EntryCorrection, or **Unclassified** (a new type
nobody classified — counted and shown, never folded into collections). **Customer receipts** = CustomerCollection +
DriverRemittance only; cash = into drawer/main, electronic = into bank/instapay/wallet (confirmed only).

## Owner financial report (`/api/reports/owner-financial`, calendar basis, P5)

Sections: sales, actual collections, balances now, drivers, receivables, liabilities, expenses, cash
discrepancies, open financial exceptions, estimated profitability. Each KPI carries `Definition`, `Basis` (sale /
movement / now / shift / business_date), `Sources` and `Status`: `actual`, `estimate` (profit), `partial` (inputs
missing; a floor, with a note), `unavailable` (**amount null — never 0**, e.g. profit when food cost is incomplete).
Figures shared with the dashboard are read from it, so the two cannot disagree.

**Sales bridge** — the period's non-cancelled orders at full value:
`Gross = ReceivedThroughTreasury (sale_collection posted for them, any time) + CodSettled + CodHeldByDrivers +
CodNotYetCollected + PendingElectronic + ShortfallUnresolved + ShortfallReceivable + ShortfallRealised + Residual`;
Residual must be 0 (`OwnerFinancialReportTests`). COD lines use the custody (collected, capped at due; above due is
reported separately and is not a sale). Net sales = gross − refunds made in the period.

**Money flow** — the period's posted movements, from the restaurant as a whole: inflow = no source, outflow = no
destination, both = moved internally. `Closing = Opening + In − Out`; Residual = Closing − Σ account balances at the
period's end = 0. Timing notes state collections in the period for earlier sales and this period's sales received
after it.

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

## Exported reports (`/api/reports/export`, Excel / PDF / Word / PNG)

The exported files compute no figures of their own.

**How a file is produced**

1. `ReportSnapshotBuilder` builds one immutable `ReportDocument` (`Falah.Application.Reports.Export`).
2. It reads the same query services that the report screens read:
   - `ReportingQueryService`
   - `OwnerFinancialReportQueryService`
   - `PurchasingQueryService`
   - the collection-shortfall and handover queries
3. Every writer (`XlsxReportWriter`, `PdfReportWriter`, `DocxReportWriter`, `PngSummaryWriter`) only lays that
   document out. No writer adds, subtracts or rounds.
4. Money is carried in piastres and formatted by integer arithmetic (`ReportText.Money`). Excel cells hold
   pounds as numbers with an `ج.م` number format.

**Snapshot fingerprint**

The fingerprint is SHA-256 of the document without its generation time and user. It is stamped in each file:

- Excel and Word: core properties, keyword `falah-snapshot:`.
- PDF: Info `/Keywords`.
- PNG: a `tEXt` chunk.
- The HTTP header `X-Falah-Snapshot`.

The same report and period give the same fingerprint in every format. A closed period gives the same
fingerprint later, because a later price change does not move a past sale.

**Bases per report**

Every table names its basis.

| Report key | Summary basis | Tables |
|---|---|---|
| owner | the owner financial report (calendar days) | bridge, money flow, definitions |
| sales | calendar days, by time of sale | channels, order types, shifts (per business date), business days (`BusinessDayId`) |
| cash | posted treasury movements by calendar day | per-account cash flow (opening + in − out = closing) |
| expenses | calendar days | expense lines, reversals |
| refunds | refund time | refunds, voids |
| delivery | business days of the period | driver statements, collection shortfalls |
| products | calendar days | product performance |
| exceptions | current state plus the period | pending electronic payments, unconfirmed handovers, shortfalls, and similar |
| suppliers | current balances | supplier balances, purchases and payments in the period |

**Limits**

- The period may be at most 366 days and may not end after today (Cairo). The server refuses anything else.
- A table is cut at 5,000 rows. The file then carries a warning that it is incomplete.
- Figures that are estimates (for example profit while the food-cost data is incomplete) keep the status
  `تقديري`.
- Figures the system cannot compute keep the status `غير متاح`. They are never shown as zero.

**Permissions**

- Exports are checked on the server by `ReportExportCatalog`: `reports_view` or `all`.
- The suppliers report is for the owner (`all`) only.
- Every export writes an audit event. The event holds the report, format and period, and no figures.

**Not exported yet**

Payroll, inventory, waste and promotions reports. They have no report service to snapshot. They are
listed in `docs/ASSESSMENT.md`.
