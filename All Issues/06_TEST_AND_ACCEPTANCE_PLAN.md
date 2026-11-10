# 06 — Test and Acceptance Plan

No test was written or run during the audits. Everything below is a specification.

**Isolation (mandatory, from CLAUDE.md):**
- Integration tests use `FalahApiFactory`, which creates a `FalahTest_<guid>` database (never `Falah`) and has
  automatic backup off.
- Run them in a scratch copy of the tree without `bin`/`obj`/`.git`, and never two runs at once.
- Hardware tests (H-*) follow `docs/DAILY_OPERATIONS_VERIFICATION.md` on site.
- A test must fail first ("RED") for the stated reason before the fix.

## A. Baseline

| ID | Test | Issues | Pass condition |
|---|---|---|---|
| B-01 | Rebuild the solution; run `tests/Falah.Domain.Tests`, `tests/Falah.Client.Tests` | TEST-0001 | Green |
| B-02 | Re-run `BusinessDayCloseVerticalSliceTests`, `EmployeeVerticalSliceTests`, `OrderVoidDispositionTests`, `InventoryVerticalSliceTests`, `MenuManagementVerticalSliceTests`, `PrintAgentIdentityTests` | TEST-0001, SEC-0017, SEC-0026, FIN-0036 | Green (they failed on harness permission drift) |
| B-03 | Run the never-executed `CorsTests`, `CashDrawerKickTests`, `OperatingDayScenarioTests`, `SplitOrderVoidTests` | SEC-0025, HW-0002, OPS-0005, FIN-0002 | Green, except `SplitOrderVoidTests`, which should be RED until FIN-0002 is fixed |
| B-04 | Full integration suite | All FIXED issues | Green |

## B. Financial (RED first)

| ID | Name | Setup | Action / fault | Assertions | Invariant | Environment | Issues |
|---|---|---|---|---|---|---|---|
| T-FIN-01 | `Confirming_the_electronic_leg_of_a_split_order_posts_it_once` | Open day and shift; split 100 ج cash + 200 ج InstaPay | Checkout; confirm; confirm again | 200; one InstaPay movement of 200 ج; order paid; the second call replays | Each leg once | Integration | FIN-0001 |
| T-FIN-02 | `A_split_with_two_electronic_methods_is_refused` | InstaPay + Wallet | Checkout | 422 (or two tracked legs) | No lost leg | Integration | FIN-0004 |
| T-FIN-03 | `Negative_fee_discount_or_comp_is_refused` (Theory) | 50 ج product | −5000 in each field | 422; no order | Signed input | Integration | FIN-0015 |
| T-FIN-04 | `An_uncollected_order_cannot_be_refunded` | Undelivered COD; pending InstaPay | Refund | 422; balances unchanged | Refund ≤ collected | Integration | FIN-0007 |
| T-FIN-05 | `Concurrent_same_id_partial_refunds_produce_one_refund` | Paid order | Two same-id refunds in parallel | One `OrderRefunds` row | Idempotency | Integration | FIN-0025 |
| T-FIN-06 | `A_same_id_retry_racing_a_full_refund_returns_the_refund` | Paid order; hold the order lock from a second connection | Retry with the same id | 200, `wasReplayed` | Idempotency | Integration with a lock hook | FIN-0025 |
| T-FIN-07 | `A_late_confirmation_never_changes_a_closed_day` | Pending order stamped with day A (simulated race); close A; open B | Confirm | Day A report = its snapshot | Closed periods | Integration | FIN-0014, FIN-0013 |
| T-FIN-08 | `Payroll_month_uses_cairo_dates` | Entry at 2026-10-31 22:30Z | Create October and November runs | In November only | Cairo grouping | Integration | PAYR-0008 |
| T-FIN-09 | `Split_void_reverses_the_cash_leg` (= `SplitOrderVoidTests`) | Split order | Cancel the pending leg | Drawer expected excludes the cash leg | Drawer = reality | Integration | FIN-0002 |
| T-FIN-10 | `Generic_transition_cannot_mark_payroll_paid` | Approved run | Transition to Paid | 422 | Paid ⇔ money | Integration | PAYR-0001 |
| T-FIN-11 | `Adjustment_credit_increases_net_pay` | Correction credit in the period | Create payroll | Net includes the credit | Payroll completeness | Integration | PAYR-0002 |
| T-FIN-12 | `Ledger_credit_retry_with_spent_approval_replays` | Overtime posted | Same request again | Replay, not refusal | Replay first | Integration | PAYR-0014 |

## C. Concurrency

| ID | Name | Fault insertion | Expected | Issues |
|---|---|---|---|---|
| T-CONC-01 | `A_sale_cannot_land_in_a_shift_that_has_closed` | Barrier between the checkout's shift read and its commit; run the close | Checkout refused, or included in the handover | FIN-0010 |
| T-CONC-02 | `Shift_open_float_cannot_overdraw_main` | Parallel float + withdrawal > balance | One refused; main ≥ 0 | FIN-0011 |
| T-CONC-03 | `Concurrent_invoice_payments_never_exceed_outstanding` | Two 60% payments | One refused | FIN-0012 |
| T-CONC-04 | `Pay_and_cancel_payroll_concurrently` | Parallel | One terminal state | PAYR-0003 |
| T-CONC-05 | `Two_business_day_opens_create_one_day` | Parallel | One active day | FIN-0022 |
| T-CONC-06 | `Two_closes_create_one_revision` | Parallel | Unique revision | FIN-0021 |
| T-CONC-07 | `Count_sheet_during_sale_leaves_counted_quantity` | Sale between the sheet load and its submission | Final balance = counted | INV-0007 |

## D. Printing and hardware

| ID | Name | Setup / fault | Expected | Environment | Issues |
|---|---|---|---|---|---|
| T-PRT-01 | `A_report_from_an_agent_that_lost_its_lease_does_not_change_the_job` | A claims; lease expires; B reports Succeeded; A reports Failed | Stays Succeeded | Integration (short lease) | PRT-0002 |
| T-PRT-02 | `A_backlog_longer_than_the_lease_is_not_printed_twice` | Lease 2 s; 5 jobs; fake transport 1 s each; two processors | Each job sent once | Integration (fake transport) | PRT-0003 |
| T-PRT-03 | `An_unwritable_journal_stops_startup_or_reports_failed` | Read-only journal directory | No endless "poll failed" | Unit | PRT-0010 |
| T-PRT-04 | `Paper_out_status_marks_job_uncertain` | Fake status | Not Succeeded | Unit | PRT-0004 |
| T-PRT-05 | `Agent_retries_login_until_server_up` | Server started 10 s later | Agent logs in and prints | Integration | PRT-0012 |
| T-PRT-06 | `Two_first_print_requests_create_one_kitchen_job` | Two request ids for one order | One job per route | Integration | PRT-0001 |
| T-PRT-07 | `Call_center_reprint_excludes_kitchen_routes` | — | No kitchen job | Client + integration | PRT-0014 |
| T-HW-01 | `Drawer_kick_served_within_max_age_during_backlog` | Five slow jobs plus a kick | Kick sent before expiry | Integration (fake transport) | HW-0001 |
| H-01 | Paper out, network transport | Remove the paper; sell; replace the paper | Records whether the ticket printed and what the status said | Real printer | PRT-0004 |
| H-02 | Paper out, spooler transport | As H-01 | Spooler holds the job, then prints | Real printer | PRT-0004 |
| H-03 | Printer off for 10 min | Five kitchen items | Failed; time to recover via reprint | Real printer | PRT-0013 |
| H-04 | One kitchen printer off | Mixed order | Grill prints; Charcoal listed Failed | Real printers | PRT-0013 |
| H-05 | Drawer with the printer on and off | Request a drawer open | Opens / clear expiry message | Real drawer | HW-0001, HW-0002 |
| H-06 | USB selective suspend | Idle 30 min, then sell | Ticket prints | Real PC | OPS-0003 |
| H-07 | **Power-cut drill** | Cut power to the server and stations | Time to the first kitchen ticket, with no manual step | Site | PRT-0012, OPS-0003 |

## E. Security, menu, inventory, reports

| ID | Name | Expected | Issues |
|---|---|---|---|
| T-SEC-01 | `Role_is_normalised_or_refused` | "Owner"/"Cashier" refused or lower-cased; cashier scoping holds | SEC-0014 |
| T-SEC-02 | `Endpoint_enumeration_has_no_ungated_sensitive_reads` | Lists none | ARCH-0001, SEC-0001..0005, INV-0008, CUS-0001 |
| T-SEC-03 | `Cashier_session_cannot_read_employees` (four routes) | 422 | SEC-0001 |
| T-SEC-04 | `Pending_pin_change_file_contains_no_pin` | No PIN in the store | SEC-0008 |
| T-SEC-05 | `Pin_proof_does_not_replace_session` | Bearer unchanged | SEC-0009 |
| T-SEC-06 | `Reactivation_with_colliding_pin_is_refused` | 422 | SEC-0012 |
| T-MENU-01 | `Removing_a_sold_variant_is_refused_with_a_clear_error_or_deactivates_it` | 409/422 or deactivation; no 500; no pending kept request | MENU-0002, API-0003 |
| T-MENU-02 | `Negative_variant_price_is_refused` | 422 | MENU-0001 |
| T-INV-01 | `Negative_purchase_line_is_refused` | 422 | INV-0009 |
| T-INV-02 | `Movement_rows_respect_date_range` | Only in-range rows | INV-0012 |
| T-RPT-01 | `Reversed_report_range_equals_forward_range` | Same result | RPT-0001 |
| T-RPT-02 | `Monthly_total_equals_sum_of_daily_totals_per_basis` | Equal | FIN-0043 |
| T-TIME-01 | `Business_date_range_on_cairo_dst_start_does_not_throw` (2026-04-24, 2027-04-30 and the previous days) | Valid 23 h range | FIN-0028 |
| T-TIME-02 | `Business_date_range_on_dst_end_is_contiguous` | 25 h; contiguous | FIN-0028 |

## F. Crash, network, database

| ID | Name | Expected | Environment | Issues |
|---|---|---|---|---|
| T-CRASH-01 | `Kept_checkout_survives_restart_and_completes_once` | One order | Client + integration | UI-0002 |
| T-CRASH-02 | `Replayed_checkout_creates_missing_print_jobs` | Jobs exist once | Integration | — (safeguard regression) |
| T-CRASH-03 | `Kept_shift_close_completes_once_after_restart` | One handover | Integration | — (safeguard regression) |
| T-NET-01 | `Close_and_cash_commands_within_client_timeout_on_large_dataset` | Within the budget | Load (`FalahTest_*` with about 200k movements) | API-0004, PERF-0001, PERF-0003 |
| T-NET-02 | `Every_command_reports_outcome_unknown_on_transport_failure` | `OutcomeUnknown` | Client unit | API-0003 |
| T-DB-09 | `Two_backups_in_one_second_have_distinct_files_and_prune_keeps_newest` | Distinct; newest exists | Integration (test database) | DB-0004 |
| T-DB-10 | `Connection_drop_after_commit_then_retry_replays` | Replay | Integration + TCP proxy | — (safeguard regression) |
| T-DB-11 | `Restore_blocks_writers` | Writes refused during restore | Integration (test database only) | DB-0005, DB-0006 |

## G. Acceptance before production
1. All of B-01..B-04 green.
2. All Phase 1 tests (section B and T-CONC-01..03) green.
3. T-SEC-02 green.
4. H-01..H-07 executed on site, with results recorded in `docs/DAILY_OPERATIONS_VERIFICATION.md` (owner edits).
5. HTTPS in use (SEC-0006); demo PINs rotated (SEC-0022).
