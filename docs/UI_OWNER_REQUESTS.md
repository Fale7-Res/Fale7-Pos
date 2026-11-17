# Owner UI and feature requests

## Cashier shift handover clarification — 2026-11-09

Owner request: Ahmed finishes his own shift, enters the physically counted cash blind, then sees the server's expected amount and difference, completes the normal close/handover, and the next cashier signs in with their own PIN and opens their own shift with the cash actually received. Normal shift opening needs no manager approval. Only one shift may be open at a time. Never force-close another cashier's shift, invent a count/handover or expose expected cash before counting. Existing discrepancy approvals are unchanged.

The reported obstruction is the separate business-day prerequisite: the cashier currently has to switch to a manager account to open the accounting day, then switch back. The requested startup experience must avoid this account roundtrip while retaining valid accounting-day association. Changing cashiers must not close or recreate the business day. Cashier permissions must not be broadened to management close/reopen operations.

### Current source findings and verification

- OpenShiftHandler already accepts shifts_open_own or shifts_manage, has no manager-approval parameter, and rejects an existing open shift; the database's filtered unique index is the concurrency backstop.
- CashierViewModel.ShiftClose already separates blind count, PrepareShiftClose preview and confirmed close, then displays the statement. Close approval for a discrepancy remains a separate policy.
- OpenShiftHandler currently calls BusinessDayGate.RequireOpenAsync; OpenBusinessDayHandler separately requires business_day_open. The default cashier lacks that permission. This is the source-supported reason for the manager login roundtrip, not a missing permission to open their own shift.
- Fresh capability probe on 2026-11-09: one existing SQL test failed in FalahApiFactory.CreateDatabase at connection.Open, BEFORE database creation or any business assertion. Message: The instance of SQL Server you attempted to connect to requires encryption but this machine does not support it. This execution identity is desktop-lokrnd1\codexsandboxoffline. It is an environment blocker, not a demonstrated product test failure.
- The existing compiled Domain ShiftTests and AuthorizationTests passed 29/29. These establish only the covered pure rules, not this complete SQL/native handover flow. No new financial implementation or production access was performed.
- Evidence: artifacts/codex-shift-workflow-20261109/sql-preflight.log and sql-preflight.trx; existing-shift-rules.log and existing-shift-rules.trx.

### Concrete implementation target for a SQL-capable continuation

Review a narrowly scoped create-or-reuse accounting-day path inside authorized shift opening, so a cashier can start through their own session without a separate manager login. Serialize creation with the existing transaction-owned FinancialAdmission lock, preserve the single active day/shift constraints and deterministic request replay, and roll back day/float/shift/print effects together on failure. Keep standalone day management permissions and closed-day history intact; do not automatically reopen a closed period or bypass a day currently closing. Reuse the existing open-day date policy rather than introduce a fixed shift schedule or midnight split. Verify this path before accepting it; it is a proposed implementation direction, not implemented behavior.

Acceptance on a disposable FalahTest database and isolated station storage:
1. Existing open day: cashier A opens with actual float, no manager session/PIN; blind count then preview then close; cashier B signs in and opens their own shift on the same day.
2. No active day: authorized cashier startup resolves the day without account swapping; denied user creates no day, shift, float or paper. Do not publish the change until this case is agreed and verified end to end.
3. A still-open shift blocks B; two competing opens cannot create two shifts/days. No automatic close occurs.
4. Invalid opening, insufficient float funds or failed commit leave no partial day/shift/money/print effects.
5. Lost response, app restart and retry preserve the original request and record each operation once, including the case where day creation was needed.
6. Crossing midnight keeps the existing business day and shift identity; difference approval, frozen close paper and safe reprint continue to work.

Status (Codex investigation, kept for the record): implementation blocked by that account's SQL verification capability; not marked FIXED. No business-day/shift handler, permission, seeder, migration or running server was modified by that investigation.

### Implementation and verification — Claude, 2026-11-09 (owner Windows account, SQL available)

**Change (server only; no migration, no permission or seeder change)**
- `BusinessDayGate.ResolveForShiftOpenAsync` replaces `RequireOpenAsync` inside `OpenShiftHandler`:
  - **Reuse:** the active day is reused (Open, Reopened or Closing, the same set as before).
  - **Create:** otherwise a day is created for the server's existing business date (`BusinessClock.BusinessDate`), inside the shift-opening transaction.
- **Serialisation:** creation takes an exclusive app lock (`FalahBusinessDayCreation`), always after the shared `FinancialAdmission`, so the lock order is fixed. A concurrent opener waits, re-reads and reuses the committed day.
- **Atomicity and replay:** the day's request id is `<shift request id>:day`. Day, opening float, shift and opening slip commit or roll back together. A retried request replays the shift before reaching this code.
- **Closed dates:** a date whose day is already Closed is refused with a message pointing to a manager. A closed period is never reopened or duplicated by a cashier.
- **Unchanged:**
  - the actor is still checked by `AuthorizeShiftOpen` (`shifts_open_own` or `shifts_manage`), so no new permission;
  - standalone `/api/business-day/open|close|reopen` and their permissions;
  - close discrepancy thresholds and approvals;
  - the submitted opening amount (990 stays 990).
- **Clients:** no change needed. Both POS and management already send the opening to the server and show its answer.

**Tests**
- **`CashierShiftStartupTests` (new, 7/7, isolated `FalahTest_*`):**
  - opening with no day: a day is created by the cashier, 990 recorded, float and slip on the same day, and the cashier is still refused at `/api/business-day/open`;
  - opening on an existing day: reused, and the handover keeps the same day;
  - an open shift blocks the next one (409), with no automatic close;
  - an unauthorised user creates no day, shift, float or slip;
  - insufficient float funds or a negative amount leave nothing;
  - the same request twice: replayed, everything recorded once;
  - six concurrent opens: one day, one shift;
  - a Closed day for today: refused, no new day.
- **`BusinessDayAssociationTests`:** the superseded "refused without a day" test now asserts the guarantee it protected: no shift, even with a zero float, exists outside a business day, and its sales carry that day.
- **Neighbours 70/70:** business-day association and membership (after midnight), financial admission, business-day close, shift close (including discrepancy approval), the frozen statement and reprint, statements across midnight, control centre, read gate and authorization.

**Native, real Cashier app, isolated SQL** (`artifacts/claude-20261109/shift-handover/`)
1. With no business day, أحمد (PIN 1234) opened with 990 without any manager: a day was created by أحمد, the shift is 99,000 piastres, and one float and one slip were recorded.
2. He sold one 25 EGP item.
3. He blind-counted: no expected amount was visible before submitting (`h03`). After 1010 he saw expected 1015, counted 1010 and difference −5 (`h04`).
4. He closed with a reason, and the closing statement appeared with business day 09-11-2026 (`h05`).
5. He locked the till. سارة (PIN 5827) signed in and opened her own shift with 1000 on the same day; the database still has one day.
6. Lost reply on opening: the till held the request ("تعذر التحقق من نتيجة الاتصال…") with the amount frozen (`h07`). After a forced kill and restart it resumed أحمد's open shift (`h08`). Shifts, floats and slips each rose by exactly one; the amount stayed 990; there is still one day.


## Latest owner-confirmed operating workflow — 2026-11-08

Authority: direct owner discussion after the original implementation record below. This section supersedes the earlier uncertainty about driver fee entitlement and the assumption that all counted drawer cash is physically handed to Main Treasury. It records agreed requirements and a static comparison with current source, not a new runtime acceptance or a claim that these changes are implemented. Preserve the existing verified statements, recovery work and historical evidence.

### Confirmed shift, drawer and handover rules (UI-REQ-002)

1. A cashier signs in under their own identity, counts the received cash and confirms the ACTUAL opening amount. Shifts start and end when operators open/close them, with no fixed scheduled start. A shift may cross midnight; use its existing identity/business-day relationship, not an automatic midnight split.
2. The target float is EGP 1,000 (100,000 piastres), not a compulsory claim of cash physically present. Opening with EGP 990 must record 990. Existing settings already support a default float; do not create a second unrelated setting or silently substitute 1,000 for the count.
3. Expenses in the supplied handwritten reference are paid FROM THE CASHIER DRAWER. Every outflow needs an amount, description/purpose, responsible recorder, time and shift, plus the employee/supplier/recipient where relevant. Keep each transaction's true accounting type: an employee advance or supplier payment must not become a second generic expense as well. Other payment sources may continue to exist, but cannot reduce this drawer's expected cash.
4. At closing the cashier first enters the physical count without seeing expected cash. The manager reviews the reconciliation and recorded outflows, separates the target float for the next cashier, and confirms what is actually handed over. Expected cash and counted cash are distinct; do not subtract already-paid expenses from the physical count a second time. Sales totals are not interchangeable with cash physically collected.
5. A manager can complete the float: 990 actually received plus a real manager addition of 10 becomes 1,000 for the next handover. Record the addition once with source, actor, time and shift. It is not a sale. An opening shortfall is not a loss caused by the receiving cashier. A genuine shortage DURING the shift remains recorded even if the manager later restores the float.
6. The closing statement must distinguish actual opening, manager additions, cash receipts, drawer outflows, expected/count/difference, cash reserved for the next shift and cash actually handed to the manager. Keep non-cash payments separate and identify the receiver. If cash is insufficient, show the unresolved amount; never invent a top-up or a receipt acknowledgment.
7. The next cashier confirms their own actual receipt. Preserve request replay, restart recovery, frozen close statements and reprint identity. The physical reserve must not become two ledger movements representing cash handed out twice. Review existing close/open bookkeeping together before changing it.

### Confirmed counter and delivery workflow (UI-REQ-001)

- Counter: cashier records the customer's requested items, prints the customer's ticket, and the customer hands that ticket to the preparation counter. Confirm actual printing/settings on site. The owner did not specify a new payment-timing policy or request duplicate kitchen copies; preserve existing checkout semantics until clarified.
- Delivery: the operator records the phone order and customer/address details, prints its preparation paper, and the driver views ready orders and confirms receipt/assignment under their name. Preserve authorized operator identity separately from selected driver identity; this description does not authorize giving every driver management permissions.
- Each driver settles with the cashier of the LAST SHIFT at the end of the restaurant business day. Their work spans cashier shifts; changing cashier must not reset the driver's order list or create a fictional settlement.
- The owner explicitly confirmed that the driver keeps the FULL customer delivery charge, whose amount depends on the order's area. For the agreed completed-delivery case, the order's captured delivery charge is the driver's entitlement. Preserve captured historical pricing; changing an area's current price must not reprice a prior order.
- Show gross cash the driver collected, applicable delivery entitlement, any already-recorded settlement/payment of that entitlement, expected NET cash payable, actual receipt and shortage/overage separately. A legitimate fee deduction is not a shortage, refund, discount or comp.
- Cash received from the driver by the last-shift cashier belongs to that receiving drawer/shift. It is not another sale and must not rewrite a previous cashier's frozen close. Keep the originating order's sales attribution intact.
- A prepaid order has no cash to collect again. Its applicable delivery entitlement is still accounted for once. If net settlement is negative, show that money is payable TO the driver and use an authorized real payout path; do not clamp it to zero, create negative cash received or fabricate a shortage approval.
- The driver/day statement and settlement preview/receipt must reconcile using the same identified orders and validated figures. A read-only payment filter must not silently change the scope of an all-orders settlement command. Show outstanding amounts from another business day explicitly rather than dropping them.

### Comparison with current source (static inspection only)

| Area | Existing behavior / evidence | Remaining gap against latest agreement |
|---|---|---|
| Actual opening and flexible timing | OpenShiftHandler uses the submitted OpeningCashPiastres and actual time; ShiftsViewModel.Open offers a default editable float | Preserve this; prove 990 opening and real 10 top-up through UI/API/SQL |
| Closing statement | ShiftCloseStatements stores a frozen statement; existing UI and reprint evidence is recorded later in this file | Extend its schema/contents safely for retained float and actual receipt; preserve old statement compatibility |
| Closing cash movement | CloseShiftHandler posts ALL counted cash Drawer -> Main; ShiftCloseStatements labels all counted cash handed over | No physical reserved-float/actual-manager-receipt split currently represented by this path; review close AND next open together |
| Cash expenses | ShiftReconciliation counts drawer expenses and typed employee cash outflows; statement lists Expense rows with descriptions | Verify a complete cash-out listing includes relevant typed employee/supplier outflows without duplicating ledger entries |
| Driver assignment and changing shifts | AssignDeliveryHandler records per-order receipt; DeliveryRules intentionally permits changing shifts with open custody | Preserve; verify the last cashier can settle earlier-shift orders |
| Driver/day report | New DriverStatement feature, business-day/payment filters and print evidence exist | Latest owner rule supersedes the printed/current-code disclaimer that customer delivery fees are not driver pay; report still needs entitlement/net settlement figures |
| Settlement calculation and UI | DeliveryRules.CalculateTripSettlement expects the full collected custody; DeliveryViewModel compares actual cash against that gross amount | No entitlement deduction here; correct net tender must not trigger a false shortage |
| Receiving account | SettleDriverCustodyHandler posts actual cash to Main Treasury with shiftId:null | Does not match receipt into the last cashier's drawer; receipt and drawer reconciliation must both account for the new path once |

Primary source anchors: src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs; CloseShiftHandler.cs; ShiftCloseStatements.cs; ShiftReconciliation.cs; src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs; src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs; AssignDeliveryHandler.cs; DeliveryServiceHandlers.cs; src/Falah.Domain/Delivery/DeliveryRules.cs; DriverDailyStatement.cs; src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs.

### Required acceptance examples for the new rules — NOT EXECUTED

All examples are synthetic EGP amounts and require actual UI/API/SQL reconciliation; they are not results from current code.

| Case | Required result |
|---|---|
| Float received 990; cash receipts 2,000; drawer expenses 300; manager genuinely adds 10 before closing; no other movements | Expected and counted 2,700; reserve 1,000; actual handover 1,700; no shortage caused by the original 990; paper lists the 10 addition once |
| Actual opening 1,000; receipts 2,000; expenses 300; pre-top-up count 2,680; manager then adds 20 | Original shortage 20 stays recorded; final physical cash 2,700; reserve 1,000; handover 1,700; top-up does not erase the shortage |
| Successful cash deliveries: food 5,000 plus delivery charges 600 | Customer cash 5,600; driver entitlement 600; net handed to cashier 5,000; discrepancy zero |
| Same cash deliveries plus a successfully delivered prepaid order: food 1,000 plus charge 100, already confirmed paid | Gross cash held 5,600; total entitlement 700; net cash received 4,900; electronic receipt 1,100 remains separate; no duplicated charge/payout |
| Only successful prepaid deliveries, charges 70, no prior entitlement payment or other cash custody | No customer cash to collect; 70 payable to driver via authorized payout; no fabricated cash receipt |
| Driver has completed orders from two shifts and settles with last-shift cashier | No reset at shift change; actual net cash enters receiving shift once; older frozen statement/sales remain unchanged |
| Response lost, restart, retry/reprint; area price changes after order | Same settlement/top-up/close result with no duplicated money; original order charge retained; reprint uses the recorded close/settlement |

### Decisions that remain open

- Entitlement for failed/returned/cancelled deliveries, waived/discounted delivery charges and refunds, and its allocation after reassignment. Do not extend the completed-order rule to these cases by guesswork.
- Exact treatment of the cash reserve when the manager cannot restore 1,000, and the operational transition while the next cashier has not yet acknowledged receipt. Do not invent cash or conceal the shortfall.
- Driver self-service authentication versus an authorized cashier confirming receipt under a selected driver's name; no permission expansion is implied.
- Calendar/business-day close boundary, multiple simultaneous drawers, partial/advance settlements and other previously undecided policies remain unchanged unless separately confirmed. The owner confirmed shifts have flexible times, not a new business-day cutoff.

### Instructions for continuing implementation

Implement only within a context that can meaningfully verify the financial paths on isolated FalahTest databases. Keep the existing tested implementation as a starting point; do not remove it or re-audit the whole project. Update the current registry/UI matrix and this document with scoped evidence after verification. The status of the older software delivery below does not establish acceptance of these later business rules. No production access, deployment, historical restatement or financial change was performed by this consolidation.


## UI-REQ-001 — Daily delivery statement for each driver

Received: 2026-11-08. Source: four owner-provided photographs of the legacy daily-posting screen and its summary/detail print previews.

### Confirmed intent

The owner says this is for each delivery driver separately. Provide a selectable driver's statement for a selected restaurant business day, with payment-method filtering, summary and detailed views, and printable output. Treat the photographs as functional references; the owner has not requested copying the legacy visual style.

Observed in the references:
- Driver selector; selected daily/business date; payment-method filter.
- Summary versus detail output.
- A list of the selected driver's orders with their order numbers, customer/contact/address details and totals.
- Summary containing sales excluding delivery services, delivery service charges, combined total and invoice count.
- Report header identifying the driver, selected daily date, user, payment method and print timestamp.
- In the photographed example: sales excluding delivery services 4,557.50 EGP + delivery charges 735.00 EGP = 5,292.50 EGP, with 18 invoices. These are illustrative reference figures, not test data or project records.
- The daily date and print timestamp are separate concepts. Do not infer a date format or accounting-period rule from the legacy screenshots.

The legacy screen also contains broader daily totals and ancillary entries. Their meaning and whether they belong in this per-driver feature remain unconfirmed. Do not copy every legacy field or assign accounting meaning by assumption.

### Current project presence assessment

Status (2026-11-08, superseded): IMPLEMENTED AND VERIFIED in software; see "Implementation status — Claude, 2026-11-08" at the end. The assessment below is the pre-implementation record.

Original status: PRESENT IN PART — static source review only. No runtime verification or implementation performed for this request.

| Capability | Source evidence | Assessment |
|---|---|---|
| Individual driver account and custody/settlement view | src/Falah.Application/Delivery/DeliveryRequests.cs — DriverAccountDto; src/Falah.Infrastructure/Delivery/DeliveryQueryService.cs — GetDriverAccountAsync; CashierWinUI DeliveryModalsView.xaml | Present in source; not runtime-verified for this request |
| Individual driver statistics with selectable date period | DriverStatisticsDto; GetDriverStatisticsAsync; DeliveryModalsView.xaml statistics period/date controls | Present in source; dates are event/range based and must not be assumed equivalent to the requested business-day statement |
| Order values, collected cash, current custody and settlement differences | DriverStatisticsDto and DriverAccountDto | Present in source; these are different accounting measures, not interchangeable |
| Delivery-fee total | ReportingQueryService.GetDeliveryOutcomesReportAsync | Present in the overall delivery report; this method is not filtered to one driver |
| One printable driver/day statement combining food sales, fees, total and invoice count | No matching statement/print implementation identified in the reviewed delivery/report/printing sources | Not found in reviewed scope; confirm with complete route/command/renderer inventory before implementing |
| Detailed printable selected-driver order list and matching payment filter | No complete end-to-end implementation identified in reviewed scope | Not found in reviewed scope; not claimed absent from every file |

### Required follow-up

1. Confirm the exact existing UI entry point and reusable queries, print routes and report components.
2. Explain the match and gaps to the owner before treating existing driver statistics as this complete feature.
3. Separate a reporting/printing addition from visual polish; link backend/report work if needed.
4. Define which records count: business day versus calendar period, payment confirmation, refunds, failed/returned deliveries, reassigned drivers and already settled orders. Preserve current policies until clarified; do not silently use whichever timestamp is easiest to query.
5. Do not assume delivery service charges are the driver's wage or commission. Customer charges, cash to collect, prepaid value and the driver's settlement balance must remain distinct.
6. Use the same validated query result for screen, summary and detail prints. Prove that rows, counts and totals reconcile for the same driver and filters, including orders around midnight and data from other drivers.
7. Check printed Arabic, wrapping and pagination using synthetic customer data. Do not reproduce real customer contact details from the photographs in fixtures, logs or documentation.
8. Demonstrate the actual feature before marking this request verified. No code change is authorized solely by the presence assessment; implement within the owner's agreed workflow once the remaining accounting semantics are established.

### Open semantics

The owner has confirmed per-driver scope. Exact treatment of fees, adjustments, refunds, reassignment, selected daily date and settlement still requires matching the owner's intended workflow. Do not block independent UI verification while gathering these details.

## UI-REQ-002 — Priority: smooth shift opening, closing and handover paper

Received: 2026-11-08. The owner says the driver statement above and, most importantly, a smooth shift opening/closing experience with everything handed over and a closing paper explaining expenses, totals and what happened during the shift.

Priority: above other owner-requested UI additions. This is an operational workflow with reporting/printing dependencies, not cosmetic work alone.

### Intended operator experience

- Opening: identify the cashier and shift, clearly record the received opening float, and confirm successful opening. Reuse current financial rules; do not silently carry the previous drawer balance into the new shift.
- During the shift: relevant sales, expenses, refunds and cash movements are recorded once and attributed to the correct shift. Money and unresolved operations must remain explainable at handover.
- Closing: a short, understandable workflow to enter the physically counted cash, review the resulting reconciliation, resolve or explain outstanding items under existing policy, and confirm handover. Preserve blind counting: do not reveal expected cash before the initial count.
- After successful closing: queue a printable closing/handover statement; show whether printing is queued, failed or awaiting confirmation. Printer failure must not create a second financial close. Provide a safe reprint path marked as a reprint.
- The incoming cashier has a clear starting state and opening float, without ambiguous ownership of pending work. Whether unresolved orders or custody block closing or transfer responsibility must follow existing policy or an explicit owner decision.

### Proposed contents to satisfy "everything the shop needs to know"

The owner explicitly requested expenses, totals and the shift's events. Review the following practical sections with existing data and policy; do not silently redefine metrics:

| Section | Contents |
|---|---|
| Identity | Restaurant, shift reference, business date, actual opening/closing dates and times, cashier, recorded closer/approver where applicable |
| Sales | Order count, gross/net sales clearly labelled, discounts/comps/refunds/voids without double counting |
| Payment breakdown | Cash, card, confirmed InstaPay/wallet and split allocations; pending electronic amounts shown separately from collected money |
| Expenses | Actual expense entries with description/category and amount, total, payer/account; distinguish drawer-paid expenses from expenses that do not affect drawer reconciliation |
| Cash reconciliation | Opening float, cash received, refunds paid out, additions, withdrawals/safe drops, expected cash, counted cash, shortage/overage and recorded explanation/approval |
| Handover | Cash actually handed over, recorded destination/recipient when available, outstanding delivery custody/transfers/operations with clear ownership or unresolved status |
| Sign-off | Cashier/responsible recipient signature spaces and relevant notes |

Keep a compact readable printed summary; put long itemized detail in an attached/detail report if necessary. Show the operator what will be printed. Use the same reconciled values across UI and print. Reprinting a historical shift must not silently replace its original closing figures with today's values.

### Current source assessment

Status (2026-11-08, superseded): IMPLEMENTED AND VERIFIED in software, natively on both entry points; physical paper pending on site. See "Implementation status — Claude, 2026-11-08" at the end. The assessment below is the pre-implementation record.

Original status: PRESENT IN PART; source inspection only for this request, no new native or physical-printer verification performed here.

- Shift opening/closing and reconciliation already exist, including blind count and manager approval when required.
- OpenShiftHandler queues a ShiftOpen print document. CloseShiftHandler queues a ShiftClose document in the same transaction as closing.
- PrintRules.ComposeShiftOpenTicket already includes cashier, shift reference, opening date/time, float and signature.
- PrintRules.ComposeShiftCloseTicket already includes opening float, cash sales, additions, expenses, refunds, withdrawals, safe drops, expected/counted cash, difference, expense-category totals, notes, warnings and handover signature spaces.
- ShiftsViewModel exposes reprint actions for opening and closing slips.
- ShiftSummaryDto carries sales figures, expense totals, identity/timing and cash differences. These are reusable inputs, not evidence that the existing printed slip contains a complete sales/payment report.
- The reviewed closing composer does not accept a full sales/payment breakdown or individual expense descriptions: its expense list is category totals. The complete owner-requested report needs a gap check, not an assumption that the existing cash slip fully satisfies it.

Source anchors: src/Falah.Domain/Printing/PrintRules.cs (ComposeShiftOpenTicket/ComposeShiftCloseTicket); PrintRecords.cs (ShiftCashBreakdown/ExpenseSummaryLine); src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs and CloseShiftHandler.cs; src/Falah.Infrastructure/Printing/PrintJobQueryService.cs; src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs; src/Falah.Application/Reports/ReportingDtos.cs.

### Acceptance evidence

Exercise one seeded shift end to end: opening float, several payment methods, actual expense entries, refund, relevant cash movement, closing count, handover and print/reprint. Reconcile UI, persisted records and printed report. Include a shift crossing midnight, shortage/overage under existing approval rules, ambiguous-success retry, and a print failure after a successful close. Prove that retries/reprints do not duplicate closing, handover or money movements. Test all active entry points, including embedded POS controls and the management shift screen.

Use an isolated real server and native clients for workflow proof. Software print composition/preview and actual paper/device acceptance are separate evidence levels. No production data, financial policy, automatic balance transfer or payroll/delivery-fee entitlement is changed by this request document.


## Implementation update — Codex takeover, 2026-11-08

The owner explicitly authorized taking over implementation of the whole project. UI-REQ-002 remains the first priority. Its opening/closing modal visibility is now fixed and verified in 72 native fixture cases (UI-0035); the closing slip now includes the shift name. The complete handover reporting/SQL workflow and UI-REQ-001 are not complete. Read CODEX_TAKEOVER_20261108.md for evidence, current environment blockers and the remaining work. Existing business-policy questions remain unchanged.

## Implementation status — Claude, 2026-11-08

Evidence folder: `artifacts/claude-20261107/ui/controls/`. Native runs used the real server against an isolated
`FalahTest_ui_*` database and synthetic customers only. "Paper" below means the composed ticket the print agent
renders (read back through `/api/print-jobs/{id}/ticket`). A physical printer has not printed it yet; that is the on-site check
in `docs/DAILY_OPERATIONS_VERIFICATION.md`.

### UI-REQ-002 — shift close statement

**What was built**
- **Frozen statement.** The close builds a complete closing statement in the same transaction that closes the
  shift. It covers:
  - identity: restaurant, shift reference and name, business day, opened/closed times, cashier, closer, approver;
  - sales: list, offers, comps, discounts, gross, refunds, net, cancelled count;
  - payment breakdown, with pending electronic payments shown apart from collected money;
  - each expense with its description, drawer or account, author and time;
  - the cash reconciliation;
  - the handover amount and destination;
  - items still open at the close: pending electronic payments, unpaid orders, and open driver custody;
  - signature spaces.

  It is stored as JSON on the close's audit event, so no migration was needed.
- **Same rules as the reports.** The figures come from the shift report's sales rules
  (`SalesRules.ComputeSalesReport`) and the drawer reconciliation (`ShiftReconciliation`). The statement
  computes no money of its own.
- **Reprints use the frozen copy.** A later confirmation, refund or expense does not change a closed shift's paper.
  - A shift closed before this change has no frozen copy. Its paper is rebuilt from current records and carries a
    printed banner saying so.
- **"يوم العمل" is the business day the shift ran in.** It is not the calendar date of the opening, so a shift
  opened after midnight in yesterday's business day prints yesterday.
- **Statement dialog.** It opens automatically after a successful close on both entry points:
  - the POS (`CashierView`);
  - the management Shifts screen, including after a recovered close.

  History rows have "ورقة الإغلاق". The dialog shows the print status: queued, sending, printed, or failed with
  the agent's error and attempt count. It offers a reprint marked as a reprint, and Escape closes it.
- **Endpoint.** `GET /api/shifts/{id}/close-statement` allows the shift's opener or closer, `shifts_manage` or
  `reports_view`. Blind counting is unchanged: the expected cash stays hidden until the first count.
  - The endpoint refuses before it says whether a shift exists, so an unknown id answers exactly like someone
    else's shift.
  - `EndpointReadGateCoverageTests` found the first version answering 404 to a permissionless session; it now
    answers 403.

**Verified**

| Check | Evidence |
|---|---|
| POS: open with float, cash sale with change, card sale, drawer expense with validation, refund with reason validation, blind count, then reveal and a −5 shortage with reason | `shift-01` … `shift-11` |
| Statement shown automatically after the POS close; reprint queued | `shift-12`, `shift-13` |
| Print failure after a successful close: the agent failed the paper, the shift stayed closed exactly once, and history shows the failure text with a reprint path | `shift-14`, `shift-15` |
| Screen, API, database and paper reconcile | `shift-statement-api.json`, `shift-statement-db.txt` |
| Lost close response on the management screen, the app force-killed, relaunched and retried with the same close id: `wasReplayed:true`, 1 handover, 1 statement, 1 close paper, 0 pending files | `shift-17` … `shift-19`, `proxy-shift-close.log`, `shift-recovered-close-db.txt` |
| Integration tests | `ShiftCloseStatementTests` (reconciliation with the shift report and the shift row; frozen reprint after a later confirmation; retry replay; pre-change shift path), `StatementsAcrossMidnightTests` (business day after midnight), `ShiftCloseVerticalSliceTests` (replay guards) |

### UI-REQ-001 — driver daily statement

**What was built**
- **Entry point.** Delivery screen → "كشف يومية السائق".
- **Filters.** Driver, business day and payment method: all, cash, card, InstaPay, wallet, or split.
- **Summary cards.** Invoice count, food sales, delivery charges, total, prepaid, to collect, collected, unsettled
  and refunds.
- **Order table.** Number, time, customer, phone and address, method, food, charge, total, to collect, collected
  (with a settled mark), and notes (voided, failed or returned, refunded, reassigned from).
- **Printing.** Summary and detailed Arabic papers come from the same server rows (`DriverStatement` print jobs).
  The print request is idempotent by request id.
- **Endpoints.**
  - `GET /api/delivery/drivers/{id}/daily-statement`
  - `POST …/daily-statement/print`

  Both need `reports_view` or `delivery_manage`.

**Rules applied.** These follow existing policy; nothing was redefined.
- **Which orders count:** the order's own business day (`BusinessDayId`), not a timestamp. An after-midnight
  order belongs to the business day it was sold in.
- **Reassigned orders** belong to the driver who ended with them, with a note naming the previous driver.
- **Voided orders**, such as a failed cash-on-delivery order that was never prepared, are listed but excluded
  from all totals. They show 0 to collect and 0 prepaid. Failed and returned deliveries are counted and keep
  their outcome after settlement.
- **Delivery charges** are shown as what customers paid for delivery, with a printed note that they are not the
  driver's pay.
- **Refunds** are shown in their own column, never netted into the sale.
- **To collect** comes from `DeliveryRules.DeliveryCashResponsibility`. Prepaid is the remainder.

**Verified**

| Check | Evidence |
|---|---|
| Native: driver A's statement reconciles. 3 invoices; food 315 + charges 45 = 360; prepaid 115; to collect 245; collected 245; unsettled 0; reassignment note shown | `driver-01-statement.png` |
| Native: card filter shows 1 order; cash filter shows 2 invoices, 215 + 30 = 245, plus the voided row | `driver-02-card-filter.png`, `driver-03-cash-filter.png` |
| Native: the voided row shows 0 to collect; the table fits the dialog and its notes wrap | `driver-04-notes-wrap.png` |
| Both prints queued; detailed paper read back with header, summary and per-order blocks | `driver-statement-detail-paper.txt` |
| Integration: other drivers never leak in, reassignment, settlement, refund, filter, another day empty, paper matches rows, print replay | `DriverDailyStatementTests` |
| Integration: after-midnight order in yesterday's business day, today's date empty | `StatementsAcrossMidnightTests` |

Defects found and fixed during the native runs:
- the voided row showed its total as "to collect";
- the notes column was clipped, because the table had unbounded width under horizontal scrolling.

### Open owner questions

None block these features. If the owner wants a different rule, the choices that could change are:
- voided orders listed versus hidden;
- refunds shown versus netted;
- which driver gets a reassigned order.

Each is a one-place change in `DeliveryQueryService.GetDriverDailyStatementAsync` or `DriverStatementRules`.

## Driver fee settlement and physical shift handover — Claude, 2026-11-12

Implements the owner's accounting rules recorded as "Missing" in `All Issues/REMEDIATION/CHECKPOINT.md` (2026-11-11).
Source fingerprint `4329469C4AD05E58`.

### What the system now does

- **Driver earnings.** The full delivery fee recorded on each successfully delivered order is the driver's
  (`DriverSettlementRules.FeeEntitlement`, `DeliveryRules.FinalOutcome == Delivered`). Failed and returned deliveries
  earn nothing. Fees are recognised once, in the settlement that settles those deliveries.
- **Settlement figures.** Due = carried-in balance + cash collected − fees. Received cash goes into the drawer of the
  shift open at that moment, attributed to it (`driver_settlement`, destination `tre-drawer`, `ShiftId`). It is
  counted at that shift's blind close; it is not a sale.
- **Partial settlement.** «تسوية جزئية» carries the remainder forward (owed by the driver, or owed to him). A carried
  balance blocks the business-day close (`UNSETTLED_DRIVER_BALANCE`) and is listed on the close statement.
- **Prepaid deliveries.** They are never collected again. When fees exceed the cash a driver brings, the restaurant's
  debt to him is paid out of the drawer explicitly (`driver_fee_payout`, checked against the drawer balance) or carried.
  It is never clamped to zero.
- **Unchanged controls.** A final settlement that differs from the due amount needs a reason and
  `delivery_settlement_approve`, as before. Duplicate and concurrent requests post once: the idempotency key, the
  row locks, and a new per-driver app lock for balance-only settlements.
- **Close split.** After the blind count, the cashier states the float left in the drawer (offered as the configured
  default float, never more than counted) and names the manager who received the rest. Counted = retained + handed
  over. Only the handed-over cash moves drawer → main. A shortage or overage stays with the shift that had it.
- **Next opening.** A shift opens on what the last close left (`InheritedFloatPiastres`). No second float is issued
  from the treasury. The opening figure is what the cashier actually counted. A gap against what was left is recorded
  separately (`opening_float_shortfall` / `_overage`, no shift, referencing the previous shift), and the new cashier
  is answerable only for what was really there. Topping up to 1,000 is the existing «إضافة نقدية» (`cash_in` from main).
- **Statements.** The close statement (schema 2) adds driver cash, driver payouts, the inherited float and any gap,
  top-ups from the treasury, the float left, and the receiver. Older statements still read as "all counted cash handed
  over". The driver statement and its paper now say the fee is the driver's earning, and show his fees and the net.
  The owner dashboard reconciles collected = channels + driver cash handed in + fees drivers kept.

### Evidence

- Domain 485/485, Client 381/381.
- New integration class `DriverFeeSettlementAndHandoverTests` (10). It covers:
  - two shifts, different area fees, cash and prepaid, partial then final settlement;
  - concurrent settlements, and a lost response replayed after a server restart;
  - 990 plus a 10 top-up, retained float and handover, shortage and overage, the next opening;
  - midnight;
  - print failure after commit, then a reprint.
- Updated tests that encoded the old rule:
  - DeliveryVerticalSlice ×2: cash now goes into the receiving drawer;
  - DriverDailyStatement: settles net of fees;
  - Reporting delivery outcomes: settles net of fees;
  - client close fixtures: now name the receiver.
- Native demo on both apps: `artifacts/claude-20261112/demo/SUMMARY.md`.
- Upgrade and backup/restore rehearsal: `artifacts/claude-20261112/upgrade-rehearsal/`.

### Decisions still the owner's (recorded in docs/ASSESSMENT.md)

DRV-FEE-01 refunds after delivery; DRV-FEE-02 waived or reduced fees; DRV-FEE-03 reassignment; DRV-FEE-04 accounting
presentation of driver fees; DRV-AUTH-01 whether cashiers get `delivery_manage`; HND-01 who bears a gap found at opening.

## Paper redesign and Charcoal printer — Claude, 2026-11-17

The owner asked for a professional, consistent design for every printed paper, and for the new
Charcoal-station XP-D200N to be integrated. Spec (with Amendments 1 and 2):
`docs/superpowers/specs/2026-11-17-paper-design-design.md`. Plan:
`docs/superpowers/plans/2026-11-17-paper-design.md`. Design and evidence summary:
`docs/PRINT_AGENT.md` § "Paper design".

### What the owner decided, on paper

- One visual system:
  - the **Dubai** typeface, slightly larger, with a lighter bold;
  - the logo (black-and-white "variant C") on customer papers only;
  - section titles on black bands, and item tables as ruled grids;
  - amounts as `12,500 ج`, and times as `hh:mm ص/م`.
- The **takeaway receipt is one paper**, with no QR.
- **Delivery has two papers.**
  - At order-taking, «تحضير طلب» goes into the restaurant: no money, no logo, no QR.
  - At driver pickup, the driver's paper is the customer's full receipt, with the **menu QR**.
- The **shift-opening slip is retired**. Jobs queued before deployment still print.
- The sub-name on paper should read «ألذ سندوتش». It is a Settings value: the owner changes it in
  Settings; no code holds it.

### Evidence

- **Charcoal printer** (serial `1C80CF9C0000`), on a temporary queue `XP-D200N-Charcoal` on the
  development laptop. The real workflow was run against an isolated server on a throwaway database:
  - 12 physical tests passed with the owner looking at the paper: Arabic, mixed text, long names,
    notes, quantities, a 32 cm ticket, the cutter, a reprint, a duplicate request, an agent
    restart, and USB unplug and replug.
  - USB unplug showed PRT-0004 in practice: the job was reported Succeeded while the spooler held
    it, and it printed once when the USB came back.
- **Paper design:** every paper was printed and approved by the owner after each round (initial
  design, Amendment 1, Amendment 2).
- **Automated tests:** Domain, Client and the printing/affected integration classes are green.
  Counts are in `.superpowers/sdd/2026-11-17-paper-design/progress.md` and the closeout run.

### Still the owner's or on site

- Set up each restaurant station PC for its printer:
  - a Windows queue on that PC's USB port;
  - the agent service with a dedicated `print_agent` user;
  - check that Dubai is installed (the agent logs a WARNING when it is not).
- Deploy the server and the print agents together: new element kinds `logo` and `fitStart`.
- Kitchen tickets are about 15 mm longer than before (larger type, chosen by the owner). A smaller
  order number would save about 5 mm.
