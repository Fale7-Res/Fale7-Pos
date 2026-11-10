---
name: falah-operations-e2e
description: Use when verifying a Falah restaurant workflow end to end - a trading day, shift close, business-day close, electronic payments, refunds and voids, delivery and driver custody, crash/restart recovery, midnight/month-end boundaries - or when writing an end-to-end integration scenario test or an on-site acceptance checklist.
---

# Falah operations end to end

The day, step by step, with where each step is enforced and proven, is in
`docs/DAILY_OPERATIONS_VERIFICATION.md`. Keep that table current when behaviour changes.

## Writing a scenario test

One `FalahApiFactory` per scenario (`tests/Falah.Server.IntegrationTests`), driven only through the
HTTP API like a till would (`CreateAuthenticatedClient()`; actor fields pick the session). Seed only
what the scenario is not about (`SeedOpenBusinessDayWithFundedMainAsync`, `SeedProductAsync`,
`SeedProductWithRecipeAsync`, `SeedUserAsync` with the exact permissions a real role has).

Assert the books, not just status codes, after every phase:
- treasury balances per account (`/api/treasury/balance`, `/api/reports/cash-flow`),
- shift expected vs counted (`/api/shifts/prepare-close` as a `shifts_blind_close` holder),
- business-day reconciliation (`/api/reports/business-day/{id}`) and the close snapshot,
- calendar reports (`/api/reports/daily-statement`, `/api/reports/owner-dashboard`),
- stock balances and movements, driver custody, employee ledger.

Exact piastres. A scenario that ends with every account reconciled to the counted cash is the goal.

## Scenarios

| Scenario | Covers | Existing coverage to extend |
|---|---|---|
| A cash day | open day → shift → cash sales → print → expense → safe drop → blind count → close shift → close day → reports | `ShiftCloseVerticalSliceTests`, `BusinessDayCloseVerticalSliceTests`, `SlipPrintingAndReprintTests` |
| B electronic / split | pending, confirm late, cancel (prepared / not), retry with the same id, restart between | `ConfirmElectronicPaymentTests`, `OrderVoidDispositionTests`, `CashFlowAndPendingTests` |
| C refunds / voids | same shift, other shift, previous day, approvals | `CrossShiftRefundTests`, `RefundDateBasisTests`, `RefundVerticalSliceTests` |
| D delivery | assign → custody → delivered / failed / returned → settlement | `DeliveryVerticalSliceTests`, `OrderVoidDispositionTests` |
| E crash / restart | client retry with same request id, server restart (`reuseDatabaseName`), agent re-login, approval across restart | `CashierDurabilityTests` (client), `ManagerApprovalTests`, `PrintAgentSessionTests` |
| F time | after midnight, month end, salary accrual catch-up | `BusinessDayMembershipTests`, `SalaryAccrualTests` |

Time-dependent steps: prefer explicit dates in requests; where the server reads the clock, swap
`TimeProvider` in a test host (see `falah-security-review`).

## On site (cannot be automated here)

Printer output (Arabic, cut, QR), two agents on one route, cash drawer kick, pulling a till's power
mid-payment, server restart mid-shift, HTTPS from a second PC, restore of last night's backup into a
copy, a full trading day reconciled against counted cash. Mark each VERIFIED / PENDING HARDWARE /
PENDING OWNER / FAILED — never "passed" without having done it.

## Done means

The scenario test is green with book-level assertions, the lifecycle table cites it, and anything
needing hardware is listed with its manual steps.
