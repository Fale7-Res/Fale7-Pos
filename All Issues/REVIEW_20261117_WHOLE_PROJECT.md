# Whole-project review — 2026-11-17

Branch `closeout-20261117` (HEAD dc08460 plus the working-tree fixes below). The owner asked for a
review of every file and a run of everything before being told the system is OK.

## How it was done

- **Code review:** twelve read-only reviewers, one per slice. The slices were Domain, Application + Server, Infrastructure (money, security, delivery + printing, stock + staff + reports), the print agent + call center, the cashier view models (two halves), the views, the services, and the two newest migrations. The old WPF client and the tests were excluded.
- **Verification:** every *Important* finding was re-checked against the code, the issue registry, `docs/METRIC_DEFINITIONS.md` and the pending owner questions in `REMEDIATION/state.json` before anything was changed. Findings that are owner policy were recorded and the behaviour was left unchanged.
- **Fixes:** each fix started with a failing test, which was run and seen to fail for the stated reason. The fix was then applied and the class and its neighbours were re-run.

## Run evidence

- **Final complete suite on the final code** (a code-only copy of the tree, 15:37–17:25): Domain 546/546, Client 417/417, Integration **1027/1027**. No test databases were left behind.

- **Build:** `Falah.sln` builds with 0 errors; the only warnings are NU1900 from the offline NuGet feed. The WinUI Cashier and Call Center (x64), the print agent and the server each built separately with 0 errors after the fixes.
- **Domain:** 546/546 (544 before the review plus the new comp test cases).
- **Client:** 417/417 (403 before the review plus the new tests).
- **Integration, targeted on the fixed code** (classes in the same tree run one at a time): UserManagement + PinChoice 62/62, Treasury + cash movement 34/34, Purchasing 21/21, Settings 25/25, PrintAgent + claims + drawer 51/51, Checkout/Electronic/Comp/Shift/Treasury 216/216, Authentication + read-gate + actor identity 32/32, read-gate + ReportExport 13/13.
- **Real-process smoke:** `Falah.Server.dll` ran in Production mode on a throwaway `FalahSmoke_20261117_153242` (files in `E:\SqlData\Tests`, backup off, `127.0.0.1:5199`). It first refused to start, because Production does not migrate on its own, which is the intended guard. With `Database:MigrateOnStartup=true` it applied every migration, including the two new ones, to the empty database. A scripted trading day then passed 17/17:
  - health check, and anonymous callers refused
  - demo logins; business day and shift opened
  - cash sale, and the same sale replayed once
  - InstaPay sale pending, then confirmed
  - safe drop; a drawer transfer refused
  - print jobs queued
  - expected cash = sale − drop; shift closed with no difference
  - daily statement read; the menu photo URL is not 401

  Both WinUI apps were launched against it and rendered their PIN screens. Logging in through the UI could not be driven, because Windows keeps a background script from holding focus on the app. The server was then stopped and the database dropped (confirmed: 0 remaining). The real `Falah` database was never addressed.

## Fixed in this review

| ID | Defect | Fix | Evidence |
|---|---|---|---|
| SEC-0036 | A `users_manage` holder could reset the PIN of an account holding more permissions (for example a manager with `all`), then log in as that account | A PIN reset now applies the same no-escalation guard as create/grant (`ChangePinHandler.AssertResetIsNoEscalation`), both before and inside the transaction | `UserManagementVerticalSliceTests.A_limited_admin_cannot_reset_the_pin_of_an_account_holding_more_than_they_do`. Red: 200 OK. Green: 62/62 with PinChoiceOracleTests |
| FIN-0052 | Cash in/out checked that the shift was open only before the admission lock, so a close committing in between could receive a movement its frozen statement never counts | The shift is re-read once admitted, as `SafeDropHandler` already did | Static plus the existing cash-movement tests (34/34 with Treasury). The race itself is not reproducible in a test |
| FIN-0053 | A treasury transfer into or out of `tre-drawer` carried no shift, so the shift's expected cash never saw it. At close it showed as the cashier's shortage and was debited a second time | Transfers refuse the drawer with an Arabic message pointing to the cashier screen's safe drop and cash in/out, and the Treasury screen's pickers no longer offer the drawer | `TreasuryVerticalSliceTests.A_treasury_transfer_cannot_touch_the_cashier_drawer` (both directions). Red without the fix, green with it |
| INV-0014 | An invoice listing the same ingredient on two lines failed as a "needs manual review" conflict (duplicate stock-movement key) | Repeats of an ingredient get a line suffix on the stock key, and the first line keeps the established key | `PurchasingVerticalSliceTests.An_invoice_listing_the_same_ingredient_on_two_lines_is_received_whole`. Red with the conflict message, green 21/21 |
| OPS-0007 | Settings accepted negative fees, rates, thresholds and multipliers, zero receipt copies, and zero workday hours (payroll divides by it) | `UpdateSettingsHandler.AssertPlausibleNumbers` refuses them in Arabic before anything is written | `SettingsValidationTests` (11). Red 10/10, green 25/25 with DefaultOpeningCash and StationReadPermission |
| FIN-0054 | A fully comped InstaPay or wallet order opened a 0 ج "pending" transfer that held the kitchen ticket and blocked the day close | `CheckoutRules.SelectElectronicLeg` returns no leg when nothing is payable | `CheckoutRulesTests.A_fully_comped_electronic_order_has_no_pending_transfer_and_is_paid`. Domain 546/546 |
| PRT-0019 | The cash-drawer step ran first and unguarded, so a drawer endpoint that kept failing stopped every receipt on that station | The drawer step's failure is logged and the tickets still print | `PrintAgentTests.A_failing_drawer_step_does_not_stop_the_tickets_from_printing`. Green 51/51 with the claim and drawer tests |
| UI-0048 | Call center: a kept checkout whose retry the server definitively refused froze the desk (no edit, clear or close) until the file was deleted by hand | The refusal is shown with a "تأكدت - تجاهل الطلب السابق" button. The request is still kept until the operator lets it go | `CallCenterDurabilityTests.A_kept_checkout_refused_on_retry_can_be_discarded_after_review` |
| UI-0049 | Cashier: a treasury correction or supplier credit in flight at a crash came back invisible. It could not be retried or reviewed and silently blocked the next one | Both are now in the recovery list. A pending supplier credit also blocks purchasing edits, like its siblings | `TreasuryDurabilityTests.A_correction_left_in_flight_…` and `…A_supplier_credit_left_in_flight_…` |
| UI-0050 | Cashier: a transfer confirm or cancel in flight at a crash blocked every till operation with no control to resolve it | It is surfaced in the kept-request banner to be checked and let go. A refused in-session retry goes there too. The banner title is now neutral | `CashierDurabilityTests.A_recovered_transfer_confirm_or_cancel_no_longer_freezes_the_till` (both) |
| UI-0051 | Cash count: a quantity typed with Arabic digits («٣») was read as 0, under-reporting the count | `CashInput.TryParseCount` reads Arabic-Indic and Persian digits | `CashCountDigitsTests` (9). Red 3/9, green |
| UI-0052 | Menu photos never showed: the image control downloads without the session token, so every photo got 401 | `GET`/`HEAD /api/products/{id}/image` alone is readable without a session. A menu photo is public menu content. Uploading and every other product read still need a session. **This decision is the owner's to keep or reverse** | `AuthenticationTests.A_menu_photo_is_readable_by_an_image_control_but_nothing_else_about_products_is` |
| TEST-0003 | `EndpointReadGateCoverageTests` failed: the report-export routes added in dc08460 were not in its probe or its allow-list | The probe now names a report and format, so those routes hit their permission gate. The catalog, which is self-filtered, is listed with its reason | The class plus ReportExportTests, 13/13 |

## Owner trial requests (2026-11-17/18, the trial run on `FalahSmoke_Demo_*`)

| Request | Change | Evidence |
|---|---|---|
| Tapping «فلافل صمون» again added a second line | On the till and at the call center, the same product, choice and offer raises the quantity. A line with its own kitchen note stays separate | `CartMergeTests` (3): red 2/3, then green |
| Two orders at once («عدد سلات») | A first try (a «تعليق الأوردر» button with chips) was rejected by the owner. He chose fixed basket tabs above the cart: «سلة 1 \| سلة 2 \| … \| +». Each basket keeps its own items, type and customer/delivery details. Tap a tab to work on it, «+» opens an empty basket, and × closes an empty basket that is not the last. Switching is refused on the payment screen or while an operation is unresolved. Screen-only: no server write, no money, lost on close or logout | `HeldOrderTests` (4). Client 424/424 |
| Delivery printed two papers into the restaurant | Checkout no longer queues the `DeliveryOrder` ticket (owner chose «تحضير طلب» only). See PRINT_AGENT.md Amendment 3 | `DeliveryVerticalSliceTests` rewritten for the rule. Delivery, intake, printing, dashboard and driver-fee classes 127/127 |
| A till delivery printed a priced receipt as well as the driver's paper | `PrintRules.ComposeReceiptTicket`: a delivery order on the `Receipt` route prints the «تحضير طلب» prep paper on the till's printer. Takeaway and pickup receipts are unchanged | `DeliveryPapersTests.A_delivery_order_rung_up_at_the_till_prints_the_prep_paper_on_the_till_printer` (red, then green); Domain 547/547; delivery, printing, receipt and ticket integration classes 209/209 |
| Cashier receipt: remove the logo and the text at the end; the driver paper unchanged | `TicketLayout.CustomerHeader(withLogo)`; the cashier receipt has no logo and no brand sign-off, so the name and sub-name print once at the top. The driver paper keeps its logo | `CustomerPaperTests` (red, then green), `The_driver_paper_still_carries_the_logo`; Domain 548/548 |
| Nothing printed | The `XP-80C` queue was offline on USB001 (another, unplugged printer). A temporary `XP-80C-Trial` queue was added on USB002 for the trial. It is to be removed when the trial ends | Spooler queue drained after the reprint of #0128 |

## Checked and not a defect (rejected)

- **Restore without a manager PIN for a `backup_manage` holder:** documented design ("permission-or-approval", `RestoreBackupRequest`).
- **Expense `BusinessDate` is the Cairo calendar date entered:** documented calendar-day basis in `METRIC_DEFINITIONS.md`. Business-day reports use `BusinessDayId`.
- **Cancelling a split order's pending leg ignores its cash leg:** already fixed and tested as FIN-0002. The domain reviewer saw only the domain half.
- **Reviewer false positives on view bindings:** every bound member exists (the source generator renames them).

## Recorded, not changed (owner policy, already registered, or minor)

**Owner or accountant decisions.** Behaviour is unchanged; record any new question in `REMEDIATION/state.json`.

- **SEC-0037:** attendance check-in/out has no permission and an optional request id, by design, for the biometric device. Any session can clock in an employee and post a day's wage. *Question: should check-in require a station permission?*
- **DRV-LIAB (existing question):** a final driver settlement where the restaurant owes the driver records the unpaid amount as an "overage".
- **PAYR-0021:** the dashboard wage cost excludes attendance-correction `Adjustment` entries, which payroll counts. `METRIC_DEFINITIONS` lists the wage types without it.
- **B07 Q1 (existing):** the recipe editor needs `reports_view` (cost detail), so a `menu_manage`-only user cannot edit recipes.

**Already in the registry, still open.** PAYR-0004 (standalone payments plus payroll can pay twice), PAYR-0006/0007/0017, PRT-0013 (a failed print job is never retried automatically).

**New minor items for later:**

| ID | Item |
|---|---|
| SEC-0038 | Restore accepts any backup record (kind or status not checked; the file must exist) |
| SEC-0039 | Reprint and create-print endpoints, and print-job reads, need no permission |
| SEC-0040 | A PIN change or reset does not end the account's other sessions |
| FIN-0055 | A manager approval is spent before some commands are refused (close day, reopen, refund, transfer) |
| FIN-0056 | A cash count stores the client-supplied expected figure (FIN-0020 question) |
| FIN-0057 | An expense can be partly reversed only once |
| FIN-0058 | Checkout picks an arbitrary tax rule if two are enabled |
| FIN-0059 | A split order's change due is not checked against the splits |
| FIN-0060 | Tax and refund rounding uses banker's rounding |
| PAYR-0022 | Manual ledger entries have no request id (documented) |
| PAYR-0023 | Manual and biometric daily wage can both post for one day |
| PAYR-0024 | Payroll overtime and lateness use double arithmetic |
| INV-0015 | The supplier-return limit is read outside the lock |
| INV-0016 | Low-stock counts differ between inventory and the dashboard |
| RPT-0001 | PDF export holds every page bitmap in memory (a year-long report) |
| RPT-0002 | Negative money in PDF/PNG has no bidi isolation |
| RPT-0003 | DOCX child elements are out of schema order (Word tolerance unverified) |
| RPT-0004 | Export fingerprints of closed periods include current-state values |
| RPT-0005 | A dashboard open-shift count misses a shift left open from an earlier day |
| UI-0053 | A call-center electronic amount comes from the client's estimate of the total |
| UI-0054 | Several screens show machine-local time instead of Cairo time (Treasury, Payroll, Printing, Dashboard, Recipes) |
| UI-0055 | The Shifts screen clears the session shift on a transient read failure |
| UI-0056 | The Daily statement's "today" is fixed at login |
| UI-0057 | Financial control keeps the previous period's figures when a reload fails |
| UI-0058 | A purchase line with no item chosen is silently dropped |
| UI-0059 | Recipe save is not retained and uses a new id per press |
| UI-0060 | The open-shift and business-day commands are kept in memory only |
| UI-0061 | Settings integer fields ignore Arabic digits |
| UI-0062 | A failed product-photo load is cached until restart |
| OPS-0008 | Customer phone normalisation keeps Arabic digits, so the same customer is not matched |
| OPS-0009 | The control-center discrepancy severity uses `>=` where the approval uses `>` |

The migrations slice reviewed both new migrations against the snapshot and the entities and found
no defect. Both are additive, every non-null column has a default, and the new unique indexes are on
a new, empty table.
