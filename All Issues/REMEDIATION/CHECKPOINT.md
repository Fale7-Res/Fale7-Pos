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
