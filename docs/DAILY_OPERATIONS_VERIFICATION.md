# Daily operations verification

One restaurant day traced from start-up to the next morning: what happens at each step, where it
is enforced, what proves it, and what still has to be checked by hand on the real machines.
Status as of the October 2026 remediation (`REMEDIATION_PLAN.md`).

Evidence key: **auto** = covered by an automated test against a real SQL Server database (class
named); **code** = traced in code, no automated test; **manual** = needs the physical hardware or a
second machine.

## The lifecycle

| # | Step | What happens | Evidence | Gaps / notes |
|---|---|---|---|---|
| 1 | Server start | Windows service `FalahServer`; applies pending migrations, turns on READ_COMMITTED_SNAPSHOT, seeds demo data **only into an empty database**; nightly-style backup worker starts (age-based) | code (`Program.cs`); backup: auto (`BackupVerticalSliceTests`) | A fresh database gets demo users with PINs **1234** (cashier) and **4321** (manager, all permissions): deactivate them before go-live (DEPLOYMENT.md §3). Two new migrations (`AddShiftBusinessDayId`, `AddPrintJobClaim`) apply on the next start — both additive and nullable. |
| 2 | Login | PIN → session (16 h sliding); rate-limited; the session is the identity, request actor fields must match it (any casing, every occurrence) | auto (`AuthenticationTests`, `ActorIdentityValidationTests`) | — |
| 3 | Open business day | Manager, from the Control Center | auto (`BusinessDayCloseVerticalSliceTests`) | Only one day can be active. |
| 4 | Open shift | Requires an open business day (whatever the float); float moves main → drawer; the shift records its business day | auto (`BusinessDayAssociationTests`, `ShiftCloseVerticalSliceTests`, `DefaultOpeningCashTests`) | A cashier cannot open a zero-float shift before the day is opened (deliberate, C4). |
| 5 | Orders and payment | Server prices every line; one transaction for order, payment movements, stock usage, delivery row; idempotent by request id; the cashier keeps the request on disk until answered | auto (`CheckoutVerticalSliceTests`, `MenuPricingInventoryTests`); durability: auto (`CashierDurabilityTests`) | Power loss mid-payment: manual acceptance (pull the plug, restart, retry). |
| 6 | Electronic payments | InstaPay / wallet is a claim until confirmed; cancelling asks whether the food was prepared (waste) or not (back to stock); pending payments block the day's close and are listed with amount and age | auto (`ConfirmElectronicPaymentTests`, `OrderVoidDispositionTests`, `CashFlowAndPendingTests`) | — |
| 7 | Printing | Tickets queued after the sale commits; each agent **claims** its jobs (two machines on one route never print a ticket twice); re-logs in when its session ends; reports every attempt idempotently | auto (`PrintingVerticalSliceTests`, `PrintAgentTests`, `PrintJobClaimTests`, `PrintAgentSessionTests`) | Thermal printer output (Arabic raster, cut, QR): **manual** on each printer (`--test-print`). An agent that printed and crashed before reporting may have its job reprinted by another machine after the lease. |
| 8 | Delivery | Ready → assign (COD creates custody) → delivered / failed / returned; a failed or returned **unpaid COD** order is voided, custody closed at zero, food per the operator's answer; settlement into main | auto (`DeliveryVerticalSliceTests`, `OrderVoidDispositionTests`) | A failed delivery of an already-paid order needs a manual refund. |
| 9 | Refunds | Permission or manager approval; clamped to what remains; cash only if the drawer holds it; counted in the shift and business day it is made in; requires an open business day; with no shift open, a card refund of a closed shift's order is refused plainly | auto (`RefundVerticalSliceTests`, `CrossShiftRefundTests`, `RefundDateBasisTests`) | — |
| 10 | Inventory | Usage per sale; waste/spoilage/breakage with loss value; counts; voids reverse usage | auto (`InventoryVerticalSliceTests`, `OrderVoidDispositionTests`) | Food cost is an estimate (current recipe × last purchase price). Opening stock must be entered before go-live (`OPENING_STOCK_CHECKLIST.md`). |
| 11 | Expenses | Fixed category list without ingredients; drawer expenses lower expected cash; need an open business day | auto (`ExpenseVerticalSliceTests`, `ExpenseCategoryTests`) | Ingredients bought with drawer cash must go through Purchasing now; train staff. |
| 12 | Cash counts | Denomination counts can be saved per business day / shift | code (`SaveCashCountHandler`) | **Not compared with any balance** — the count is a record, the reconciliation is the blind shift close. |
| 13 | Cash drawer | "Open drawer" is recorded with reason / approval | auto (`CashDrawerVerticalSliceTests`) | **No hardware agent opens the physical drawer** (the command is recorded only). Manual: decide how the drawer is kicked (printer-driven pulse) before go-live. |
| 14 | Shift close | Blind count → expected cash recomputed (including refunds made in this shift for earlier shifts' orders) → approval above the threshold → shortage/overage and handover movements under the business day → handover slip | auto (`ShiftCloseVerticalSliceTests`, `CrossShiftRefundTests`, `BusinessDayAssociationTests`) | Count the drawer physically; the slip is the record. |
| 15 | Business-day close | Blockers (open shift, unsettled custody, pending electronic, journal, checklist, unauthorised drawer difference); snapshot frozen; then each monthly salary's daily share is accrued | auto (`BusinessDayCloseVerticalSliceTests`, `BusinessDayMembershipTests`, `SalaryAccrualTests`) | — |
| 16 | Daily reporting | Daily Statement (calendar day), business-day reconciliation / Daily Summary / snapshot (business day), dashboard, cash flow, pending electronic, voids | auto (`DailyStatementVerticalSliceTests`, `OwnerDashboardVerticalSliceTests`, `ReportingVerticalSliceTests`, `CashFlowAndPendingTests`, `RefundDateBasisTests`, `ProfitabilityInputsTests`) | The new cash-flow, pending-electronic and voids figures are API / Daily Statement only — **no WinUI screen shows them yet**. |
| 17 | Backup | Automatic when the newest good backup is older than 20 h; verified; 60 kept; manual backup / verify / restore | auto (`BackupVerticalSliceTests`) | Off-machine copy is outside the system (DEPLOYMENT.md §2). Restore drill: manual, on a copy. |
| 18 | Next-day restart | Sessions survive a server restart; the cashier logs in again; unanswered requests come back on their screens; the print agent re-authenticates by itself | auto (`ManagerApprovalTests` restart, `CashierDurabilityTests`, `PrintAgentSessionTests`) | The server PC must not sleep during trading (a full test run slept twice and lost database connections across it). |

## Daily operations readiness

| Area | Ready | Condition |
|---|---|---|
| Sales, payments, refunds, voids | yes | — |
| Drawer reconciliation and shift close | yes | — |
| Business-day close and snapshot | yes | — |
| Owner reporting (API) | yes | WinUI does not yet display cash flow / pending / voids |
| Payroll and wages | yes | monthly staff accrue daily from the first close after deployment; part-month proration is not handled by payroll (pre-existing) |
| Printing | yes, after hardware acceptance | per-printer test print; two-machine route test |
| Delivery | yes | paid-order failures need a manual refund |
| Cash drawer hardware | **no** | no component opens the physical drawer |
| Multi-PC LAN | after procedure | `LAN_DEPLOYMENT.md`; HTTPS strongly recommended before PINs cross the network |
| Credentials | **no, until done** | deactivate demo users; remove the PIN from the tracked agent config and rotate it |

## Manual acceptance tests still required

1. **Printers** — on each station: `Falah.PrintAgent --test-print`; check Arabic shaping, column
   alignment, the large order number, the cut, the menu QR scans.
2. **Two agents, one route, two machines** — print 20 tickets; each prints exactly once.
3. **Power loss mid-payment** — pull the till's power after pressing pay; restart; the checkout comes
   back on screen; retry; one order exists.
4. **Server restart mid-shift** — restart `FalahServer`; tills reconnect; the print agent keeps
   printing without a restart.
5. **LAN + HTTPS** — a second PC reaches the server by name over HTTPS with the certificate trusted;
   plain HTTP is closed.
6. **Cash drawer** — decide and test how the drawer opens.
7. **Backup restore drill** — restore last night's backup into a copy database and open it.
8. **A full day** — open day, two shifts, sales of every payment type, a delivery failure, a refund of
   yesterday's sale, a cancelled InstaPay, close both shifts, close the day; reconcile the Daily
   Statement's cash flow against counted cash in each account.


## Print-agent service acceptance pending

Run on each station after explicit deployment: verify its dedicated print account and LocalSystem queue access; stop/start while the server is down; confirm successful delayed login; send one real ticket; cancel/interrupt a send and inspect its uncertain journal before any retry; simulate an unwritable DataDirectory with real ACLs; rehearse a power cut and verify that queued output is recovered without duplicate paper. These physical/service checks have not been run in development. See PRINT_AGENT.md.
