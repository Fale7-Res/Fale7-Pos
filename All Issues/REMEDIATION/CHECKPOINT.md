# Remediation checkpoint — 2026-11-07 (Claude, owner account i-seven)

**Owner brief:** `docs/CLAUDE_FULL_VERIFICATION_AND_UI_MASTER.md`.
**Rules:** no commits; never touch the production Falah DB; main agent only.

## Environment (checked once)
- Account: desktop-lokrnd1\i-seven; .NET SDK 8.0.424.
- SQL Express: encrypted connection OK.
- DPAPI CurrentUser: OK.
- winapp 0.7.1 can drive and capture both native clients. Use `--capture-screen` with `-w <main hwnd>`, and ASCII output file names.

## Done today, with evidence
- Client suite: 239/239. Fixed a test-isolation flake and 9 UI defects; see `docs/UI_VERIFICATION_AND_CHANGES.md`.
- Domain suite: 449/449.
- B34 / PAYR-0013 accepted: employee lock group 97/97.
- B36 accepted with residuals:
  - DB-0005 PARTIALLY_FIXED: backup and maintenance 19/19, after fixing a ScheduledBackupService sync-scope crash.
  - DB-0006 FIXED: new PhysicalRestoreTests 3/3, real RESTORE on a FalahTest_ database only.
  - UI-0012 FIXED.
- B38: native UI review of 26 screens in light and dark at 1920, and light at 1366, against the isolated real server.
- Reports:
  - `docs/PROJECT_OVERVIEW_AR.md`
  - `docs/UI_VERIFICATION_AND_CHANGES.md`
  - `docs/FINAL_VERIFICATION_REPORT.md`

## Final run (23:12, 2026-11-07)

**Results on source fingerprint `2921FB17397921A0`:**

| Suite | Result |
|---|---|
| Domain | 453/453 |
| Client | 249/249 |
| Integration | 919/919 |

**Builds:**
- `Falah.sln`: 0 errors. The only warnings are NU1900.
- Both WinUI x64 clients: 0 warnings, 0 errors.

**Registry:** TEST-0001 is now FIXED. Totals: 128 FIXED, 18 PARTIALLY_FIXED, 44 NOT_FIXED_YET, 14 UNVERIFIED, 11 NOT_A_DEFECT.

**Report:** `docs/FINAL_VERIFICATION_REPORT.md`

**Cleanup:** the UI backend was stopped and its FalahTest_ database dropped. No FalahTest_ databases remain.

**Scripts:** `E:\DevCaches\Temp\claude\df5d42d76b05a4cc\6372eeb5-b791-4cfd-911a-e4153ef34efe\scratchpad\ui\`

**Next:**
- UI-0001 for the shift screens
- DB-0010
- Native modals and the UI-0032 assign dialog
- 125%/150% scaling
- Owner decisions

## Remaining work
- UI not yet covered:
  - 125% and 150% display scaling;
  - every modal opened natively;
  - the Call Center beyond its main desk;
  - Call Center dark theme (not implemented).
- Done since this list was written: AutomationIds on the PIN keypads (PinKey0–9, PinClear, PinBackspace).
- DB-0005 residuals: two server processes, and startup writers.
- Owner decisions (state.json `owner_questions_pending`):
  - B17: void before dispatch;
  - B07 Q1: reports_view cost reads;
  - B07 Q4: payroll-rule visibility.
- On-site: printers and drawer, service install, power-cut drill, HTTPS certificate, off-machine backup.

## Takeover pass (2026-11-08, Claude, owner account)

The sections above are the 2026-11-07 record, kept as written.

**Environment.** On the owner account, SQL (encrypted) and DPAPI both work, so Codex's sandbox blocker does not
apply. One laptop sleep (07:25 → 15:32) interrupted an integration run. The run was discarded and restarted.

**Owner requests.** Status and evidence: `docs/UI_OWNER_REQUESTS.md` → "Implementation status — Claude, 2026-11-08".
- **UI-REQ-002 (shift close statement):** implemented. Verified natively on the POS and the Shifts screen, through
  the API and the database, and by integration tests:
  - lost response, forced restart and retry;
  - print failure after close;
  - frozen reprint;
  - business day across midnight.
- **UI-REQ-001 (driver daily statement):** implemented. Verified natively (filters, summary and detail papers)
  and by integration tests:
  - other drivers;
  - reassignment;
  - settlement and refund;
  - cancelled orders;
  - midnight.

**Issues.**
- PRT-0017 is fixed by the frozen statement.
- New and fixed: UI-0036 … UI-0046. These cover offer pills, accessible names, Escape, Dark toasts, typed order
  numbers, the 1926 pickup date, duplicate note chips, the Call Center lookup address, cancel confirmation, Shifts
  reprint feedback, and checklist state names.

**UI matrix.** `docs/UI_CONTROL_VERIFICATION_MATRIX.csv`:
- Cashier: 178 of 583 rows verified or fixed-and-verified.
- Call Center: 54 of 62.
- Still `NOT_TESTED`, and not relabelled: the remaining management screens (menu editor, employees and payroll,
  purchasing, settings, inventory, backup), error and retry states that need fault injection, scaling, and
  Narrator.

**Owner decisions still pending.** No behaviour was changed for any of these:
- PAYR-0004: settlements versus payroll;
- B17 / FIN-0051: void before dispatch;
- B07 Q1 and Q4;
- FIN-0030: VAT on comps.

**Test resources.** All are on `FalahTest_*` databases. Station storage is `E:\SqlData\Tests\ui-client-state`.

**Integration result (recorded 2026-11-09).** `Falah.Server.IntegrationTests` reported **927 passed, 0 failed,
0 skipped** (1 h 40 m). The run finished 2026-11-08 18:30 on the earlier tested source, fingerprint
`F8945DE9C3A2914B`. That fingerprint covers src, tests and the solution build files, tracked and untracked
alike, with bin/obj excluded. Every later change is client-only: menu row names, the shared style
dictionaries, product card heights and their client tests. None of it was covered by this run.

## Cashier shift handover without a manager login (2026-11-09)

Requirement and evidence: `docs/UI_OWNER_REQUESTS.md` → "Cashier shift handover clarification" →
"Implementation and verification — Claude, 2026-11-09".

**Change.** An authorized cashier's shift opening now reuses the active accounting day, or creates one in the
same transaction (`BusinessDayGate.ResolveForShiftOpenAsync`):
- creation is serialised by its own exclusive lock, taken after the shared admission lock;
- a closed date is refused;
- no new permission is granted;
- standalone day management and close approvals are unchanged.

**Tests**
- `CashierShiftStartupTests`: 7/7.
- Neighbours: 70/70.
- `BusinessDayAssociationTests`: the superseded "refused without a day" test now asserts that every shift
  belongs to a business day.

**Native.** The real Cashier app on isolated SQL:
- with no day, أحمد opened with 990, sold, blind-counted (1010 against 1015, −5 with a reason) and closed with the
  statement;
- سارة signed in and opened her own shift on the same day;
- a lost reply, forced restart and recovery recorded the opening exactly once.

**Full suite on `B806353952F13D1C`: 933 passed, 1 failed, 0 skipped (1 h 44 m).**
- **The failure:** `ExpenseVerticalSliceTests.An_expense_requires_an_open_business_day_even_when_the_shift_itself_is_open`.
  Its setup asserted the superseded "a shift cannot open without a day" (422).
- **The update:** the setup now asserts that no business day exists before the legacy open shift is seeded. The
  expense-gate assertion (422) is unchanged.
- **Result:** that class plus `CashierShiftStartupTests` and `BusinessDayAssociationTests` pass 27/27.
- **Next:** a confirming full run on the final source has started.

**UI acceptance (paused for this task)**
- Pass A (fault states) ran. Its one real defect is fixed: the login screen said "رمز PIN غير صحيح." when the
  server was unreachable. Now `LoginFailureMessageTests` 3/3 and Client 374/374.
- Pass B (till daily paths) stopped without results and must be re-run.
- Pass C (print retry) is written but not yet run.

## Verification resume (2026-11-11)

**Source.** The main tree fingerprint is `D72061804B380A62` (1,231 files: src, tests and solution build files,
tracked and untracked alike). It is unchanged since the 2026-11-10 full run started.
- The scratch copy differed afterwards only by `src/Falah.Server/falah.db-wal`. That is an empty, git-ignored
  SQLite journal from 2026-09, not source.

**Full run 1 on `D72061804B380A62`: 932 passed, 2 failed, 0 skipped of 934 (3 h 19 m, ended 2026-11-10 23:46).**
- Evidence: `scratchpad/full-final/integration-D7206180-run1.log` and `.trx`.
- Both failures are in `OrderVoidDispositionTests`:
  - `An_undelivered_cash_order_is_voided_its_custody_closed_and_its_food_restored("Failed","customer_unreachable")`,
    which ran for 3 h and timed out in a query;
  - `Cancelling_without_saying_whether_the_food_was_prepared_is_refused`, which timed out while migrating its
    database during setup.
- The Windows System log shows sleep or hibernate from 2026-11-10 00:17 to 22:24, then lid sleeps at 22:38 and
  22:51, all inside the run.
- Re-run on isolated databases: `OrderVoidDispositionTests` passed 9/9 twice (1 m 5 s and 1 m 17 s;
  `artifacts/claude-20261111/OrderVoidDispositionTests*.trx`). Conclusion: an environmental interruption, not
  reproduced. No assertion, timeout or production code was changed.

**Current results.** Domain 475/475 and Client 374/374 (`artifacts/claude-20261111/Domain.trx`, `Client.trx`).
Full run 2 on the same snapshot is in progress; its result is recorded below.

**Owner accounting requirements (static inspection of current source; nothing implemented in this task)**

| Requirement | Classification | Evidence |
|---|---|---|
| Driver settlement deducts the driver's full area-based delivery fee exactly once | **Missing** | `DeliveryRules.CalculateTripSettlement` sets expected = Σ custody collected and deducts no fee. The driver statement (UI-REQ-001) shows fees separately but still prints "…وليست أجر السائق", which contradicts the owner's later confirmation that the driver keeps the full charge. That line must change together with the settlement rule. |
| Driver's net cash received into the actual cashier drawer and receiving shift | **Missing** | `SettleDriverCustodyHandler` posts `driver_settlement` with destination `TreasuryAccounts.Main` and `shiftId: null`. |
| Shift close distinguishes counted cash, the retained 1,000 EGP float and the amount physically handed to the manager | **Missing** (counted, expected and difference are implemented) | `CloseShiftHandler` posts the whole counted amount Drawer → Main as `shift_handover`. The frozen statement's handed-over amount equals the count. The next shift draws its float Main → Drawer again. There is no retained-float or manager-receipt split. |

**UI acceptance**
- Done:
  - the consistency pass (frozen);
  - pass A, fault states, where the login message defect was fixed;
  - Call Center Light/Dark;
  - Cashier Dark sweep of all 26 screens.
- Pending:
  - pass B (till daily paths): stalled at start, then ended with exit 1 and no results; re-run it;
  - pass C (print failure and retry): written, not run;
  - Cashier Light re-sweep;
  - real 150% scaling, which needs the owner's display setting changed;
  - Narrator.

**Full run 2 on `D72061804B380A62`: 934 passed, 0 failed, 0 skipped, 0 aborted (1 h 47 m, 2026-11-11
20:05–21:53).**
- Evidence: `artifacts/claude-20261111/integration/full-D7206180-run2.log`, `.trx` and `-times.txt`. Run 1's log
  and TRX are kept beside them.
- The main-tree fingerprint was `D72061804B380A62` before and after the run.
- The scratch copy again lost only the empty, git-ignored `src/Falah.Server/falah.db-wal`. No source or test file
  changed.
- The keep-awake request at the start failed (a PowerShell cast error), so it was not in effect. The run simply
  was not interrupted.

## Driver fee settlement and physical handover (2026-11-12)

The three "Missing" owner accounting requirements above are implemented. Details are in
`docs/UI_OWNER_REQUESTS.md` (section "Driver fee settlement and physical shift handover").

**Final run on `4329469C4AD05E58`** (1,239 files), isolated copy, fingerprint identical before and after, keep-awake
set and released by the run's own process (`artifacts/claude-20261112/final-2/`, logs and TRX):
- Domain 485/485.
- Client 381/381.
- Integration **944/944**, 0 failed, 0 skipped, 1 h 46 m.

The run before it, on `BA4E0B6CF8AFD64F`, was 942/944 (`artifacts/claude-20261112/final/`). Two guard tests caught
real gaps in the new code:
- the two new read endpoints had no permission gate. They are now gated (`shifts_open_own`/`shifts_manage` and
  `shifts_blind_close`/`shifts_manage`).
- `CloseShiftRequest.HandoverReceiverUserId` was not classified. It is now listed as the user the action is about; the
  server accepts only an active `shifts_manage` holder.

Neither test was weakened. The two classes plus their neighbours passed 39/39 before the second full run.

**Other evidence**
- The native demo on both apps (19 checks): `artifacts/claude-20261112/demo/SUMMARY.md`.
- The upgrade, backup/restore and rollback rehearsal, plus the production script path:
  `artifacts/claude-20261112/upgrade-rehearsal/`.
- The release candidate (not deployed): `artifacts/release-candidate/4329469C4AD05E58/`.
  - Manifest SHA-256: `656B83B7…`.
  - Migration script SHA-256: `771ACDBD…`.

**Defects found and fixed in this pass:**
- the driver dialog's statistics went stale after a settlement;
- the dashboard's reconciling note ignored the fees drivers keep;
- the two ungated endpoints above.

`docs/DEPLOYMENT.md` §0 also wrongly said production migrates at startup; it does not (`MigrateOnStartup=false`).

**Owner decisions recorded** (docs/ASSESSMENT.md, state.json): DRV-FEE-01..04, DRV-AUTH-01, HND-01.
