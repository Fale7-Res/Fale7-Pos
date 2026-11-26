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

The owner explicitly authorized taking over implementation of the whole project. UI-REQ-002 remains the first priority. Its opening/closing modal visibility is now fixed and verified in 72 native fixture cases (UI-0035); the closing slip now includes the shift name. The complete handover reporting/SQL workflow and UI-REQ-001 are not complete. The takeover checkpoint it pointed to (CODEX_TAKEOVER_20261108.md) was removed on 2026-11-19 and is in git history. Existing business-policy questions remain unchanged.

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
`docs/PAPER_DESIGN.md`. Design and evidence summary:
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

## Till: no confirmation card after a sale — Claude, 2026-11-19

- **Request (owner, trying the till on the trial server):** remove the "تم تسجيل الطلب" card and its
  "طلب جديد" button that appeared after every payment.
- **Done:** a sale that went through returns the till straight to an empty order
  (`CashierViewModel.CompleteCheckoutAsync`). The card still appears when the cashier has
  something left to do or know: an InstaPay/wallet transfer to confirm or cancel, a cancelled
  order with cash to hand back, or tickets the server could not queue (reprint warning).
- **Evidence:** `CashierCorrectnessTests.After_payment_the_confirmation_card_shows_only_when_something_is_left_to_do`
  (red first, then green); Client 441/441; Cashier x64 builds; relaunched on the trial server for the owner.
- **Not changed:** the Call Center desk's own post-order card.

## Real-server trial requests — Claude, 2026-11-23/24

- **Call Center manages deliveries (owner choice "1-ب").** The Call Center station now has three
  sections in a bar under its header: تسجيل طلب (the order desk, unchanged), الطلبات (order
  history, the cashier's screen: open shift only without `orders_view_all`, dates with it) and
  التوصيل والسائقون (the cashier's dispatch board: mark ready, assign a driver, record the result,
  cancel before dispatch, driver account and settlement, driver statistics, the driver daily
  statement and its paper). The delivery section is offered only with `delivery_manage`; the server
  checks every action as before. Adding or retiring drivers and delivery services stays in the
  cashier's Control Center. The screens are ported from `Falah.Client.CashierWinUI`
  (same view models, same endpoints, same request ids); a delivery command left unanswered by an
  earlier run is kept on the station's disk, blocks the lock, and is retried from a banner on the
  board with its own request id.
  Evidence: `CallCenterDeliverySectionTests` (4: tab needs `delivery_manage`; dispatcher reaches the
  board; a retained assignment blocks the lock until retried with its original id; the next operator
  does not see the last one's orders); Client 482/482; Call Center x64 builds; run on the trial server
  2026-11-24: board, driver account and statistics, driver statement and order history all loaded
  as the manager. No delivery write was made on the real database from this check.
- **Call Center desk: delivery or pickup, and payment that says who is paid (2026-11-24).** The owner
  read the cash button "عند الاستلام" as "pickup" and could not find pickup at all: the desk only
  ever sent deliveries. Now the first choice on the desk is the order type, "توصيل للبيت" or
  "استلام من المطعم"; the title, the customer form and the submit button follow it. A pickup asks
  only the name and phone, plus an optional day and time; it carries no delivery fee. Payment is
  "كاش مع الطيار" (delivery) or "يدفع في المطعم" (pickup, sent as `PayOnCollection`: the order goes to
  the kitchen now and the cashier takes the money when the customer comes, UI-0025), "تحويل
  إنستاباي" or "محفظة إلكترونية"; one line under the buttons says what happens to the money. The
  totals show the items, the delivery fee (delivery only) and the total. The desk's panel was
  tightened so the items keep their room. The pickup pickers are set from code-behind, not bound
  (bound, they showed "1/1/1926" and "12:00 AM", as the cashier's did on 2026-11-08).
  Evidence: `CallCenterPickupOrderTests` (4, red first: pickup paid at the counter is sent as an
  unpaid pickup with no address and no fee; pickup by InstaPay is a claimed transfer; delivery still
  needs its address and says "كاش مع الطيار"; the pickers are not bound); Client 486/486; Call Center
  x64 builds; both modes and the pickup form checked on the trial server. No order was placed on the
  real database from this check.
- **Call Center: no payment buttons, and the till's look (2026-11-24, later the same night).** The owner
  found the three payment buttons clutter: the desk now shows one line instead ("الدفع: كاش مع الطيار
  لما الأكل يوصل" / "الدفع: في المطعم لما العميل ييجي ياخد الطلب"), and every phone order is cash to the
  driver or paid at the counter. InstaPay and wallet are no longer taken by phone (the view model still
  sends them if set; nothing on screen sets them). The owner also asked for one look across both
  stations: the Call Center's order panel was rewritten in the shape of the till's `CartPanelView` -
  same header and count badge, same order-type segments ("استلام من المطعم" / "توصيل"), same
  details button, same two-row items, same footer rows (الأصناف, التوصيل, الإجمالي) and buttons.
  Client 486/486; Call Center x64 builds; checked on the trial server with two items in the cart
  (removed again; nothing was sent).
- **Cashier cart reorganised (request 2).** Two rows per item instead of four (name, line total and ×;
  then quantity, a "ملاحظات" button holding the kitchen notes, and the unit price), a smaller title
  and empty state, a tighter footer, and the last sale's number and change beside "إلغاء الطلب".
  Every touch target stays 44 px. Built; not yet confirmed by the owner on the till.
- **Safe drop removed from عمليات الكاشير (request 4).** The menu item is gone; the dialog and its
  view model stay so a safe drop left in flight by an earlier run can still be resolved.
- **Printing after a permission change.** Fixed server-side (`IsolationLevelResetInterceptor`); see
  `IsolationLevelLeakTests`.
  Before handover (2026-11-24) the owner asked for protection against more defects of this kind.
  Every SQL session setting in the server was searched: only those two SERIALIZABLE commands change
  one, and the interceptor resets every connection, so the class is closed. `MixedStationTrafficTests`
  now sends every everyday station call (till, call center, dispatcher, all six print routes, the
  screens' reads) as the very next request after every back-office action, and the same mix
  concurrently. Red proof: without the interceptor it fails with exactly the 12 print claims after
  the permission change and the deactivation; with it, 2/2 green. Full integration suite on commit
  `ffa0b09` (scratch copy, 2026-11-24): 1137/1137 passed in 2 h 18 m (the two new mixed-traffic tests
  were run separately, above).

### Full UI review before handover — Claude, 2026-11-24

Owner: "review everything in the interface - every screen and every button." Run on a Development server
against its own database `FalahUiReview_20261124` (demo seed; never `Falah`), both clients driven with
`winapp ui` (screenshots and element dumps under the session scratchpad, `uireview\shots`).

- **Static pass.** Every `Command` binding resolves (Cashier 627 controls / 47 views, Call Center 116 / 14);
  every `{Binding}` name exists (script false positives checked by hand).
- **Every Cashier page opens** (24 back-office pages + till) with its empty state, then again with data.
- **One trading day through the screens:** open day -> treasury deposit -> open shift (refused first with a
  clear message: main treasury empty) -> cash sale with quantity +, change 85 -> sized item + card sale ->
  expense -> drawer cash-in -> drawer balance 1,185 (= 1,000 + 115 - 30 + 100) -> order lookup, reprint,
  refund 25 -> add driver and delivery service -> Call Center delivery (fee 15) and two pickups -> ready,
  assign, delivered, driver settlement 45, driver statement -> pickups collected at the till -> shift close
  expected 1,295 = counted, difference 0 -> day checklist -> day closed -> dashboard, reports, daily
  statement, daily summary, financial control, shifts, audit, printing. Every figure agreed across screens
  (net 295, main treasury 4,195 = 5,000 - 1,000 float - 100 + 295).

Fixed (test first, red then green):
- Call Center confirmation said "السائق يحصّل المبلغ عند التسليم" for a pickup; lookup of an unpaid pickup
  said "تحصيل عند التسليم". Now from the registered order (`CallCenterPickupOrderTests`, 3 new).
- Shift close statement listed a settled cash delivery under "طلبات غير مدفوعة" (IsPaid stays false after
  the driver settles). Now uses the settled-custody rule `OrderReadProjection` already uses
  (`DriverFeeSettlementAndHandoverTests.A_settled_cash_delivery_is_not_listed_as_unpaid...`; shift
  statement classes 22/22).
- Expense and drawer cash-in/out closed silently; the till's last-action line now confirms
  ("تم تسجيل مصروف 12.00 ج") (`CashierCorrectnessTests`, 2 new).
- Control Center kept the closed day's "تقرير الإغلاق المحفوظ" on a newly opened day (clearing the snapshot
  did not notify the card) (`ControlCenterWriteSafetyTests`, 1 new); the day picker now shows the state
  ("2026-11-24 · مغلق") because a 24-hour day can share its date with the next.
- Drawer modals (cash in/out, drawer balance, cash drawer, safe drop) stretched to full height; centred.
  Daily summary's net-sales caption was clipped; wraps.
Client tests 492/492; solution builds with 0 errors. All verified on screen after the fix.

Seen, not changed (owner's call):
- Orders never leave "قيد التحضير" (no kitchen step moves them; a delivered, settled order still reads
  so in the Orders lists). Display-only change possible if wanted.
- Payment-method rows (نقدًا/بطاقة…) exclude cash a driver collected; they add to 235 against net 295 on
  this day's sheets. The figures are as defined in `METRIC_DEFINITIONS.md`; a "مع السائقين" row is a
  question for the accountant.
- Audit log details show raw piastres and internal ids ("tre-drawer", "129500"); its two filters start
  blank rather than "الكل". With no delivery service defined, the delivery-service list is empty with no
  hint. The Control Center's "عدّ النقدية" refuses after the last shift closes (the closing checklist's
  "عد درج النقدية" is a tick, not that dialog).

Committed as `b254c6a`. Full integration suite on that commit (scratch copy, 2026-11-24): 1140/1140 passed
in 2 h 14 m; domain 606/606, client 492/492, solution 0 errors. The Setup.exe on disk before this
(`F20DECCB3A21A4A9`, 2026-11-22) predates `ffa0b09` and this commit; installing the new one applies four
additive migrations (wage history, customer hidden, frozen food cost, pickup pay on collection), so it
follows `docs/DEPLOYMENT.md` §0 (verified backup first, service stopped outside a shift).
New installer from `b254c6a` (clean tree): `artifacts/install/Falah-Setup-8D86E5EB054CFE52.exe` (417 MB,
SHA-256 `DE635B3941473BC6D982F453663645B96EB3E8DA6CE8CE1FD3D941BC13AE0266`); server version
`8D86E5EB054CFE52-b254c6a`; its `migrations-idempotent.sql` includes `20261123064520_AddPickupPayOnCollection`.

**Commit IDs rewritten before the first push (2026-11-24).** GitHub refused the branch because
`docs/archive/documentation-20261108.zip` (107 MB, ignored since 2026-11-10) was in its history; the 16
unpushed commits were rewritten without that one file (trees otherwise identical; old history kept locally
as tag `backup/closeout-before-rewrite-20261124`). Earlier documents and the installer's version stamp use
the old IDs: `76a7d5f`→`81e5385`, `14f1726`→`a18556c`, `d386164`→`7e902ba`, `e0a097f`→`e731e8c`,
`ebbe217`→`1657d0b`, `a023f7a`→`9c474cf`, `ffa0b09`→`a348366`, `b254c6a`→`25eec06`, `e80d963`→`14dc168`.

### Delivery and pickup audit, and its four fixes — Claude, 2026-11-24

A read-only audit of the delivery and pickup workflow (code, models, handlers, both clients, stock and money)
found four defects where money or custody went wrong. The owner decided each one the same day:

| # | Defect | Owner decision | Fix |
|---|---|---|---|
| D1 | A delivery whose InstaPay/wallet transfer was not confirmed opened a cash custody for the whole order on the driver; confirming the transfer later posted the same money to the bank as well | The driver cannot take it until the transfer is confirmed | `AssignDeliveryHandler` refuses a pending transfer (and a cancelled order) |
| D2 | Cancelling that transfer cancelled the order but left its delivery on the board, assignable | (follows D1) | `CancelPendingElectronicPaymentHandler` cancels a delivery still at the restaurant (New/Preparing/Ready) with an event; refuses one already out ("record the outcome first") |
| D3 | A split payment on a delivery posted its cash leg into the till's drawer at the sale, while the driver collects it; no custody | Refuse a split on a delivery | `CheckoutRules.BuildPlan` refuses `multi` on delivery; the till disables the split toggle for a delivery (`CanSplitPayment`) |
| D4 | The day closed over a cash delivery that had not left (no custody yet); its failure the next day voided a sale of the closed day | Every delivery ends and its driver is settled before the day closes | New close blocker `UNFINISHED_DELIVERY` (`BusinessDayRules`, `BusinessDayCloseEvaluator`) for any delivery not Settled/Cancelled, except a cancelled order's delivery that never left |

"Card" on a delivery means paid by card before it goes out (owner) - the existing behaviour, unchanged.

Tests (RED first, then green): `DeliveryPaymentIntegrityTests` (7: 5 red for the right reason - assignment
accepted, split accepted, no blocker, delivery not cancelled; 2 guards green before and after),
`CheckoutRulesTests.A_split_payment_is_refused_on_a_delivery_order` (red, then green),
`CashierCorrectnessTests.A_delivery_order_cannot_be_split_between_two_payment_methods` (written with the fix, not
seen red). Domain 607/607, client 493/493.

**Deployment note (D4).** Any delivery left unfinished or unsettled in the restaurant's database - trial orders
included - now blocks the day close until it is assigned and resolved, cancelled, or its driver settled. Check
the dispatch board and the drivers' accounts before the first close after installing this build; this is part
of the trial-data decision (`docs/GO_LIVE_CLEANUP_CHECKLIST.md`).

Audit findings not changed (owner's call or later work): the delivered-outcome dialog pre-fills the full total for
a prepaid delivery (server expects 0 and asks for a reason); a cash delivery with an approved collection shortfall
can never be refunded (`RefundOrderHandler` requires the full total collected - linked to AUD-03-CLS); order status
never moves past checkout (the delivery row carries the real state); a pickup paid up front has no "collected"
record; a collection receipt is not queued on a replayed collect; an overage on delivery needs a reason only;
Call Center orders ride the open till shift (no shift open = no phone orders).

Neighbours after D1-D4: the 33 integration classes that touch delivery, payments or the day close - 383/385; the
two failures asserted the behaviour the owner changed (a never-ready delivery not blocking the close; a pending
split delivery going out) and were rewritten to the decisions
(`BusinessDayCloseVerticalSliceTests.A_delivery_never_made_ready_is_a_control_exception_and_blocks_the_close`,
`ConfirmElectronicPaymentTests.A_split_delivery_from_before_the_rules_does_not_go_out_while_its_transfer_is_pending`).

### Operating rules, group A — Claude, 2026-11-24

Owner's "governed day" sheet, group A, as decided the same day. Limits live in the server's `appsettings.json`
section `OperatingLimits` (piastres; 0 switches one off) - no schema change.

| Rule | Behaviour | Where |
|---|---|---|
| Safe-drop warning, 3,000 ج | After a sale or a pickup collection, if the drawer holds more, the till shows a warning (the sale is never refused) and the crossing is recorded once in the audit log; a safe drop clears it, the next crossing is recorded again | `DrawerCashAlert`, `CheckoutOrderResult.DrawerCashAlert`, till `DrawerCashAlert` InfoBar |
| Driver custody cap, 1,500 ج | Assigning a cash order that takes the driver past it is refused; a manager's PIN (approval `driver_custody_limit`) lets that one order go, recorded in the audit log; a prepaid order always goes | `AssignDeliveryHandler`; PIN field in both assign dialogs |
| PIN changed every 90 days | Age read from the control audit trail (`pin_changed`, `user_created`; an account with neither starts its clock at its first login under the rule - `pin_age_baseline`). An expired PIN still logs in, the login screen asks for a new PIN twice, and the server refuses every other request of that session until it is changed. Print-agent-only accounts are never asked | `PinAge`, `AuthenticationMiddleware`, `LoginResultDto.PinChangeRequired`, both login screens |

Not taken (owner): a second person to close the day, a witness at the shift close, a surprise drawer count.

Tests: `OperatingLimitsTests` (5) - written with the code, not seen red. Run together with `DeliveryPaymentIntegrityTests`
and the two rewritten tests: 14/14; domain 607/607, client 493/493. The full suite has not been run since group A
(the PIN check runs on every request).

### Delivery and pickup workflow redesign — Claude, 2026-11-24

Baseline before it: `b1fc586` ("chore: checkpoint before delivery pickup workflow redesign"). Owner decisions the
same day, applied as given:

| Topic | Decision | Where |
|---|---|---|
| Delivery payment | Cash with the driver, InstaPay or wallet - never card, never split. Refused by the server; the till hides card and split for a delivery | `CheckoutRules.BuildPlan`, `CheckoutView.xaml`, `CashierViewModel.CanPayByCard` |
| Kitchen paper | Printed automatically when the order is saved, as before. "Printed" is read from the print queue (first paper Sending/Succeeded) - no status stored | `KitchenTicket` |
| Cancel | Unpaid orders only (a paid one is refunded). Before the kitchen paper: no admin code, every ingredient back. After it: manager's PIN (`delivery_override` / `pickup_cancel`) and "was it prepared?" (yes = stays out of stock as waste, no = back to stock) | `CancelUndispatchedDeliveryHandler`, `CancelUncollectedPickupHandler`, `CancelPendingElectronicPaymentHandler` (new approval fields) |
| Pickup | Two steps after it is saved: kitchen paper (a "تحضير طلب" slip, like a delivery) and, when the customer takes it, the customer's receipt. An unpaid pickup is paid and handed over in one step; a prepaid one has its own "handed over" press. Either ends it: `HandedOverAtUtc`/`HandedOverBy`, status completed, out of the active queue, row kept | `HandOverPickupHandler`, `CollectPickupPaymentHandler`, migration `AddPickupHandover`, `PrintDocumentKind.PickupReceipt` |
| Delivery failure | Didn't answer, phone switched off, refused, wrong address, other (typed, required) | `RecordDeliveryResultHandler`, both outcome dialogs |
| Paid delivery failed / returned | Refunded by hand; the queue marks it "needs a refund" | `OrderQueueQueryService` |
| Short-collected cash delivery | Refundable after the driver's settlement, up to what was collected | `RefundOrderHandler` |
| Order status | A delivery or pickup reads "preparing" until delivered / handed over, then "completed" | `Order.Create`, `RecordDeliveryResultHandler`, pickup handover |

**The order queue.** `GET /api/orders/queue` (pos or delivery_manage) maps the records the system already keeps to a
stage and one next action - delivery: new (kitchen paper not printed) → preparing → ready → out → delivered / failed
/ returned until the driver is settled; pickup: new → preparing → completed (handed over in the open day). Both
clients' "طابور الطلبات والتوصيل" page shows deliveries and pickups side by side, grouped by stage, each card with
its customer, address, amount, payment state, attention line and the next step; Cancel only when the server allows
it. The till hands pickups over and collects; the Call Center follows pickups and cancels unpaid ones (money is taken
at the till). The page is open to `pos` as well as `delivery_manage` on the till; dispatch actions stay
`delivery_manage` on the server.

Unchanged on purpose: Call Center has no payment choice (delivery = cash with the driver, pickup = paid at the
counter); the existing delivery dialogs (assign with the custody-cap PIN, result, driver settlement) do the work.

Open question recorded in `state.json`: WF-PICKUP-CLOSE - should a prepaid pickup never marked handed over block the
day close? Today it does not.

**Deployment.** One more additive migration (`20261124194004_AddPickupHandover`: three nullable columns on Orders and
a filtered unique index) - backup first, as for the others. Pickups taken before this build have no handover record:
the queue shows a pickup not handed over only for the open day or the last 24 hours.

Tests: `DeliveryPickupWorkflowTests` (14), `OrderQueueScreenTests` (6), `CheckoutRulesTests.A_card_payment_is_refused_on_a_delivery_order`,
`CashierCorrectnessTests.A_delivery_order_offers_no_card_payment` - written with the code (not seen red). Card-paid
delivery tests in `DeliveryVerticalSliceTests`, `DriverFeeSettlementAndHandoverTests` and `DeliveryPaymentIntegrityTests`
now use a confirmed transfer (the only prepaid delivery left).

**Owner review the same evening, with the printer unplugged:** "assign is gone". The first queue made the kitchen
paper a gate - an order whose paper had not printed offered only "print kitchen paper", so no ready, no assign, no
handover. Fixed: a missing kitchen paper is a warning line and a secondary button (`QueueOrderDto.SecondaryAction`),
never a gate; the main step is always there; the paper still decides only whether a cancellation needs a manager.
Test `A_printer_that_is_off_never_holds_an_order_back`.

Neighbours: the 42 classes touching delivery, pickup, payments, printing or the day close ran 436/443 before that fix;
the 7 failures asserted rules the owner changed (card deliveries ×3, cancel always needing a manager ×3, a
short-collected delivery never refundable ×1) and were rewritten to the decisions. After the fix the five affected
classes (`PickupPayOnCollectionTests`, `DriverDailyStatementTests`, `OrderVoidDispositionTests`,
`DeliveryVerticalSliceTests`, `DeliveryPickupWorkflowTests`) ran 91/91; client 500/500, domain 608/608.

**Audit fix, 2026-11-25: a fully refunded prepaid pickup could be "handed over".** `HandOverPickupHandler` refused a
cancelled pickup but not a refunded one, and the queue kept it with its "hand over" button; pressing it overwrote
`refunded` with `completed` and recorded a handover for an order whose money had gone back (no money moved). Now the
handler refuses it (422) and the queue drops it, like a cancelled one; a partly refunded pickup is unchanged. Test
`DeliveryPickupWorkflowTests.A_prepaid_pickup_refunded_in_full_is_not_offered_or_accepted_for_handover_and_stays_refunded`
seen red twice for the right reasons (queue still listed it; then handover answered 200 instead of 422), then green.
`DeliveryPickupWorkflowTests`, `PickupPayOnCollectionTests`, `OrderVoidDispositionTests` and the `RefundOrder*`
classes ran 35/35 (in a scratchpad copy of the tree: a running `Falah.Server` locked `src/Falah.Server/bin`).

### Trading-day audit on the real apps — Claude, 2026-11-25

Owner: "fix all findings; test the whole UI button by button as a full working day". Run on this machine against an
isolated server (Development, port 5252, databases `FalahTest_ui_20261125` and `FalahTest_ui2_20261125`, automatic
backup off) with the real Cashier and Call Center apps (`FALAH_SERVER_URL`, `FALAH_CLIENT_DATA_ROOT`, `FALAH_LOG_DIR`
pointing at the scratchpad), driven through Windows UI Automation. The owner's own server (port 5151, `Falah`) was not
touched. The day: open day → owner deposit → shift → cash, card, InstaPay (confirmed later), comp with a wrong then a
right manager PIN, refund, drawer expense, prepaid and pay-on-collection pickups → delivery (driver and service set up,
ready, assigned, delivered, settled) → blind count, re-count, close shift → checklist → close day → reports, then every
management screen opened once. The slip, reconciliation, daily statement and dashboard figures matched by hand
(gross 460, refunds 45, net 415, cash 245 + card 45 + InstaPay 45, drawer 780).

Fixed (test first unless noted; client tests 500 → 521, domain 608 → 615):

| # | What the day showed | Fix |
|---|---|---|
| G1 | A cashier holding only `pos` lands on the till, never the shell, so the order queue - the only place a prepaid pickup was handed over - was out of reach: the pickup stayed "preparing" and its customer's receipt never printed | Order lookup hands it over (`CanHandOverPickup`, `HandOverPickupCommand`; `OrderDetailsDto.HandedOverAtUtc/HandedOverBy`). `LookupHandoverAndRefundLineTests`; run on screen |
| 4 | A handover request id reused for another order answered with the first order's handover | Refused (`HandOverPickupHandler.ReplayAsync`). `DeliveryPickupWorkflowTests.A_handover_request_id_used_for_one_pickup_is_refused_for_another_and_leaves_it_open` |
| F-19 | Recipes, stock balance and movements, drivers, delivery timelines and print status readable by any session | Gated on the permissions of the screens that use them. `StationReadPermissionTests.Recipes_stock_drivers_timelines_and_print_status_need_a_role_that_uses_them`; allow-list trimmed in `EndpointReadGateCoverageTests`; `PrintingVerticalSliceTests` reads the overview as `settings_manage` |
| U29 | A re-count equal to the expected cash (difference 0.00) was told "the difference is over the limit" | `PrepareShiftCloseResult.IsRecount`; the till says a re-count needs a manager. `ShiftCloseVerticalSliceTests` (extended), client test; run on screen |
| U28 | Close-shift confirm/back buttons scrolled out of sight under the handover section | Pinned below the scrolling content; run on screen |
| U21 | A comp lowered the total 90 → 45 but "amount received" stayed 90: the screen showed 45 change never paid | The till's own pre-fill follows the total until the cashier types |
| U15 / U14 | The lookup never showed a partial refund; the refund amount defaults to the whole balance | "اترجّع 45.00 ج — المتبقي 45.00 ج"; the button says "تأكيد استرداد 45.00 ج" |
| U9 / U20 | Green tick on a transfer NOT received and on a cancelled order; a later transfer confirmation was titled "تم تسجيل الطلب" | ⏳ / ✕ / ✓ marks; "تم تأكيد استلام التحويل" |
| U10 | "#0130" laid out as "0130#" in Arabic text (footer, result card), dates as "25-11-2026" | Left-to-right marks (U+200E; WinUI ignored the isolates U+2066/2069 tried first); run on screen |
| U11 | The menu search kept the last word after a sale, even across a lock and another login | Cleared after each sale |
| U18 / U8 | A pay-on-collection pickup showed payment methods and "amount received 50.00"; with a card the keypad looked live and did nothing | Hidden for pay-on-collection; quick amounts and keypad disabled unless the amount can be typed |
| U1 / U33 / U32 | Opening a day: a raw "YYYY-MM-DD" box and no word on which day opens. Closing: only a date and "تأكيد العملية". "عدّ النقدية" stayed the main action with no shift open and refused after a full count | The dialog says which day opens; the close lists net, refunds, expenses and shift difference and its button names the day; the count is disabled with a reason when no shift is open. `ControlCenterTradingDayTests`; run on screen (these three were written with the code, not seen red) |
| U2 | Header pill "مغلق" (= no shift) read as "the restaurant is closed" | "لا توجد وردية مفتوحة" (both apps) |
| U3 / U30 / U12 / U13 | Zero refunds/expenses in red; checklist rows and expense-dialog labels on the left of a right-to-left page ("Right" is mirrored); expense categories ragged | `NonZeroToBrushConverter`; "Left" (reading start) alignment; categories in three full rows with the purchasing note below |
| U26 | Queue card and receipts: "الحي السابع · المنطقة الحي السابع، ش شارع النصر" | `DeliveryRules.ComposeDeliveryAddress` does not prefix a part that already names itself (the old tests pinned "ش شارع 9", "عمارة عمارة 5" and were corrected); the card names the area once |
| U27 | Driver settlement never said the cash goes into the open shift's drawer - the cashier's blind count came up 65 short | One line in both apps' settlement dialog |
| U25 | Call Center kept "السلة فارغة" with items, and "عميل جديد" after saving the details | Cleared. `CallCenterDeskHintTests` |
| U4 / U17 | The unlock button had no automation id; the pickup dialog's cursor started in the name, not the phone | `Login_Submit`; phone focused |
| 5 | Compiler warnings CS0105, CS8625, CA1416 | Removed (`WindowsOnly.cs` for the Windows-only test suite) |

**Second round, same day - the owner answered the open questions ("كمّل وصلّح الحاجات اللي فاضلة"):**

| # | Owner's answer | Done |
|---|---|---|
| G2 | A takeaway is complete when it is paid | `Order.Create`: only delivery and pickup start "preparing". `OrderInitialStatusTests` (seen red: Preparing) |
| R1 | A separate "كاش توصيل" payment line | `SalesRules.DeliveryCash` / `SalesReportDto.DeliveryCashPiastres`: settled COD at full value; cash line unchanged, so no reported figure moves. Reports screen, daily statement and the export's payment table (whose note about the difference became true). `SalesRulesTests` (seen red: 0.00), `DeliveryVerticalSliceTests` extended; `METRIC_DEFINITIONS.md` row |
| U19 | Pending-transfer list on the till | `GET /api/orders/pending-transfers` (pos) and "تحويلات إنستاباي/محفظة مستنية تأكيد" in the order lookup; a row opens the order where confirm/cancel already are. `ConfirmElectronicPaymentTests`, `PendingTransfersListTests`; run on screen |
| U24 | Driver phone | Migration `AddDriverPhone` (nullable `Drivers.Phone nvarchar(20)`; generated with the pinned tool against `FalahDesignTime_DoNotConnect`, snapshot diff = that column only; `DEPLOYMENT.md`), `DeliveryRules.NormalizeDriverPhone`, phone on add, `PUT /api/delivery/drivers/{id}/phone`, shown in the drivers list and the queue's drivers column (both apps). `DriverPhoneTests`, `DeliveryRulesTests`, `DriverPhoneDirectoryTests`; run on screen |
| U7 | Keep the shift names | No change |
| U5 | Not chosen | No change |

Also fixed in the second round: U16 - the framework time picker was replaced, in the till's pickup details and the
Call Center's editor, by two lists (hour 00-23, minute every 5 - a held odd minute is kept) with Arabic placeholders;
`PickupPickerBindingTests` and the Call Center counterpart now check the lists stay unbound and wired to the
code-behind. U6 - amounts read thousands-separated ("5,000.00 ج"): both apps' money converters and a display-only
`CashInput.Display` replacing `CashInput.Format` wherever an amount is followed by " ج" (119 places); `Format` stays the
input form that `TryParse` reads. Client tests 521 → 525, domain 615 → 626.

First-round note, superseded above: U16 (the pickup time picker's empty fields read "hour"/"minute" - framework strings; the
control's `Language` and rewriting its template parts were both tried and both overridden, the code was removed; an
app-wide UI language would also change date-picker digits, so it is left for a decision), U6 (no thousands separator
in amounts - the shared formatter feeds receipts and tests; left as a separate change), U22 (Call Center submit looks
active with an empty cart but explains itself - kept). Owner questions recorded in `state.json`: G2 (takeaway with grill
items stays "preparing" for ever), U7 (shift names on a 24-hour restaurant), R1 (payment lines exclude settled
cash-on-delivery, with DRV-FEE-04), U19/U24 (pending-transfer list, driver phone), U5 (first-day float vs an empty main
treasury). Not changed here: F-17 (needs production data checked first), F-20 (print-agent PIN, after an on-site trial),
backup encryption (needs a certificate kept off the machine).

### Owner's trial on a copy of the restaurant's data — Claude, 2026-11-25

Release candidate `1a25612` (full integration suite 1175/1175 over two runs after a memory failure on this laptop,
domain 626/626, client 525/525) run by the owner on `FalahTest_OwnerTrial_20261125`, a restore of a COPY_ONLY backup
of `Falah` he took at 17:03 (`E:\falah-restaurant\Backups\Falah_owner-trial_20261125.bak`, verified). The copy was
updated with the release's idempotent script (48 → 50 migrations, applied twice: the second a no-op; orders, shifts,
treasury movements and their sums identical before and after) and served by the release server in Production mode.
`Falah` itself was not touched and is still at 48 migrations. Seen on startup: the two active accounts still use the
published demo PINs - to change before go-live (acceptance §2 item 2).

The owner's notes, all three fixed the same day (client-only; no server, schema or money rule changed):

| # | Owner's note | Fix |
|---|---|---|
| 1 | The queue said "لا توجد طلبات توصيل جارية" on top of two delivered orders waiting for their driver's settlement | `HasNoDeliveries` read the old dispatch board, which does not list them; it now reads the queue's delivery column (both apps) |
| 2 | An order handed to one driver cannot be given to another | The server has always done it (`POST /api/delivery/{id}/reassign`: same custody to the new driver, dispatch slip, audit with both names and the reason); no screen called it. "نقل لسائق تاني" on a queue card that is out, for a dispatcher (`delivery_manage`); new driver (not the current one) and a reason required; kept on disk until its answer is known like the assignment, so a lost answer is resent with the same request id. After the outcome is recorded the server needs `delivery_override` - not offered on this screen |
| 3 | "شيل اللي بتظهر دي من أي حاجة" - the "اتسجل الطلب / طلب جديد" card | Till: no card after any sale. A transfer to confirm, a pickup paid at collection, or tickets not queued leave a warning above the footer (`PostSaleNotice`) naming the order and where to finish it, until the next payment opens; the card still opens from the order lookup to confirm or cancel a transfer. Call Center: the full-screen confirmation is a small card at the bottom (number, payment line, print warning, ✕), and the next order goes out without pressing "طلب جديد"; a print whose answer was lost keeps its retry there |

Tests (seen red for the right reasons, then green): `OwnerTrialUiNotesTests` (7: empty state in both apps with an
empty-queue guard; reassign in both apps; only a dispatcher and only an order that is out; a lost answer resent with
the same request id), `CashierCorrectnessTests.After_payment_no_card_stops_the_till_and_what_is_left_to_do_is_a_warning`
(rewritten from the 2026-11-19 rule that kept the card), `PickupCollectionClientTests.A_pickup_paid_on_collection_is_registered_with_no_money`
(rewritten likewise), `CallCenterPickupOrderTests` (2 new). Client 534/534; both WinUI apps build x64 with 0 warnings.

**Release candidate and installer (2026-11-25).** Branch `release/rc-20261125` (main unchanged, nothing pushed):
`1a25612` (the working tree audited and frozen at 12:20, 154 files) then `d88e567` (the three trial notes above,
client-only - `git diff 1a25612 d88e567` touches nothing under Server, Infrastructure, Domain, Application,
PrintAgent, deploy or the integration tests). Evidence on these commits, each on a fingerprint-checked copy:
solution build 0 errors (1 nullable warning in a test file); domain 626/626; client 534/534 on `d88e567`;
integration 1175/1175 on `1a25612`'s server code - 1074 in the full run (13:06-15:48; the test host then died of
an out-of-memory condition on this 8 GB laptop - SQL error 701 at 15:46, no test failed) and the 101 not reached
re-run after a SQL Server restart, 110/110. A run between those two, on SQL degraded by the memory failure, failed
15 tests with 500s and 30 s timeouts; the same tests passed once SQL was restarted - environment, not code.
Installer `artifacts/install/Falah-Setup-019D209C680D1F5F.exe` (417 MB, SHA-256
`DA4312441870D95018CBFD51C7438B34F972D6B547ED126C1D81EE19C0F746AA`); package `Falah-Install-019D209C680D1F5F`,
`VERSION.txt` "Source commit d88e567" (clean tree), all 13 Falah assemblies stamped
`019D209C680D1F5F-d88e567`, manifest 1571/1571; `migrations-idempotent.sql` has 50 migrations including
`AddPickupHandover` and `AddDriverPhone` and is byte-identical (SHA-256 `9635C451…`) to the script rehearsed on the
copy of the restaurant's data. Installing it on `Falah` applies those two migrations: `DEPLOYMENT.md` §0 (verified
backup first, service stopped outside a shift). Still open before go-live: acceptance §2 on site, the §4 owner
decisions, the demo PINs on the two active accounts, trial-data cleanup.

### Owner's requests on the laptop trial — Claude, 2026-11-25/26

Run from the working tree on `FalahLocal_20261125` (a restore of the same 17:03 COPY_ONLY backup; served in
Development, automatic backup off; 48 → 51 migrations on the copy only; `Falah` not touched). The owner tried every
item on screen and confirmed it ("كلو شغال"). Owner-facing guide for offers: [OFFERS_GUIDE.md](OFFERS_GUIDE.md).

| # | Owner's request | Change |
|---|---|---|
| 1 | A saved address picked from the list showed without floor and apartment | The data was complete; the list showed only the street (till) or area and street (call center), so two addresses on one street looked alike. `CustomerAddressDto.FullText` (the server's own `ComposeDeliveryAddress` wording, not on the wire) in both lists and on the customers page |
| 2 | No settlement step in the delivery queue | Delivered / failed / returned carry no next action; a driver is settled from his account (his name) as before |
| 3 | «ترحيل»: clear the queue's screen for the next shift | `POST /api/orders/queue/clear` (delivery, pickup or both; replays by request id): one audit row per kind (`order_queue_clear`); the queue hides an order taken up to then once its cycle has finished (delivered/failed/returned with no refund owed; a pickup handed over). One still in progress stays, with a warning in the window, and leaves by itself when it finishes. Nothing deleted; no schema change |
| 4 | Item notes off the driver's paper | Printed on the prep paper only; the delivery note stays on the driver's paper |
| 5 | Waste valued as unknown for 51 of 52 ingredients (none bought through the system) | Owner decision: last purchase cost, else the item's estimated cost (the order food cost already uses), frozen when posted - logged waste and a prepared cancellation alike (`PurchasingRules.LossUnitCost`) |
| 6 | Offers "almost not working" | Cause: the till and the desk loaded the menu - and the offers - once per shift, so a new, edited, day- or hour-bound offer did not change until the next shift, and an expired one was silently charged at list price. Menu refreshed every minute (quiet, keeps the section); an offer no longer allowed is refused at checkout with its name |
| 7 | Offers "customizable", per the owner's choices | Kinds: fixed price on items (one price for every size), percentage on items, amount or percentage off whole categories, gift on an order over an amount (tiers, highest only; threshold on what the customer pays with or without the delivery fee; added by itself or by a button, per offer). Conditions: days, hours (across midnight, on the day it opened), dates, order types, stations, limits per order / day / overall. Every offer optional. Migration `AddFlexibleOffers` (additive: 12 columns with defaults on `PriceRules`, tables `PriceRuleCategories`, `PriceRuleGiftTiers`; snapshot +122 lines, nothing removed); existing offers read as fixed price unchanged. Gift lines priced 0 with their list price kept, so "قيمة العروض" includes them; printed «هدية: …» |
| 8 | A number pad for quantity | Tap the quantity on a cart line (both apps): 0-9, clear, delete, keyboard; 1-999 |

Tests: domain `OfferRulesTests` (15), `DeliveryPapersTests` (+2); integration `FlexibleOffersTests` (15),
`DeliveryPickupWorkflowTests` (+3 «ترحيل», settlement assertion changed), `InventoryVerticalSliceTests` (+2),
`MenuPricingInventoryTests` (an inactive offer is now refused, not charged at list price); client
`OwnerRequestsQueueAddressTests` (11), `QuantityPadTests` (10), `OfferClientTests` (6), `OfferEditorTests` (3).
Domain 643/643, client 565/565. Full integration suite on a copy of this tree (2026-11-26 11:47-14:17, no code
change after the copy): 1195/1195 in 2 h 29 m.
