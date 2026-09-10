# Final verification checkpoint — 2026-09-08

This is the current acceptance record. Earlier statements that all modules are
"built" describe screen presence, not complete workflow or visual acceptance.
No existing work was reset, reverted, stashed, deleted or discarded.

## Fixes in this pass

- Call Center retains the original checkout after an ambiguous response followed
  by a later rejection; editing and locking remain blocked until resolved.
  The root lock command now enforces this guard itself, including direct command
  invocation; it no longer relies only on the disabled header button.
- Call Center now retains separate drafts by operator, including when an operator
  switches away and returns. Direct clear/note commands respect operation guards.
- Initial receipt requests and reprints retain their original payload for retry.
  A later rejection cannot release an earlier ambiguous print outcome. Checkout
  success survives a print failure, and the confirmation reports queue acceptance
  rather than claiming that paper was printed. Print retry is available there.
- Call Center estimates now use the existing domain inclusive/exclusive tax
  calculation. An empty menu has an empty state, and cart mutation commands
  respect unresolved-operation and availability guards.
- Secondary-read failures are surfaced in reports, delivery, treasury,
  purchasing, inventory, menu/tax/offers, permission catalogs, employee balances,
  recipe costing, waste and print queues. They no longer silently become empty
  collections or zero financial figures in these load paths.
- Control Center cannot claim closing is available when blocker/checklist/
  exception reads failed or remain in flight. Failed content is hidden and retry
  remains available.
- Control Center checklist writes use the backend's `business_day_close`
  permission, not the screen's `control_center` read/navigation permission.
- Changing a report date during loading schedules a consistent reload instead
  of mixing dates. Print route changes and empty-state notifications are fixed.
- Payroll line reads reject stale selection responses and show unknown totals
  on failure. Transitions require the existing management permission in addition
  to approval permission; creation validates the month and guards permission.
- Treasury account balances wrap instead of forming an overflowing fixed strip.
- Treasury deposit/withdrawal/transfer, employee cash-out, inventory count/waste,
  supplier creation, purchase receipt/payment, delivery assignment/outcome/custody
  settlement and payroll creation/payment now retain the immutable submitted DTO.
  An ambiguous response followed by a rejection keeps the original request for
  retry; input changes, modal dismissal, navigation and locking cannot abandon it.
- Driver-account reads reject stale responses after switching or closing accounts;
  settlement confirmation survives the subsequent account refresh.
- Payroll has a real approved-run payment dialog using the existing permitted
  treasury accounts. Its non-idempotent status-transition endpoint is reconciled
  through the authoritative single-run read after a lost response.
- Cashier electronic confirmation/cancellation now retains its exact decision;
  the opposite action, new order and result dismissal are blocked while unresolved.
- Control Center now exposes day open/close/reopen and exception decisions, with
  retained operation retry and the existing backend permission gates. A read-only
  `/api/business-days` endpoint makes closed days discoverable for review/reopen.
- The checklist API adapter now accepts the server's empty HTTP 200 confirmation;
  it previously attempted to deserialize a list and falsely reported failure.
  The same contract mismatch was fixed for product availability, delivery readiness,
  inventory updates, user active/permission changes and exception review.
- Converter precedence and nullable warnings encountered in the touched source
  and integration assertions were corrected.
- A reproduced server configuration defect was fixed: PIN rate-limit options now
  resolve the final host configuration through DI rather than capturing defaults
  before test/host overrides arrive. The focused SQL regression fails before and
  passes after this change. Production defaults remain 10 requests / 60 seconds.

## Verification evidence

Generated logs and results live under `artifacts/verification/` (ignored by git).
The native probes are compile-time-only test entry points and are excluded from
normal builds. They do not sign in, issue API writes or replace product screens.

- Domain: 259 passed in this pass.
- Final Release client: 73 passed (`client-final-hardened.log`). This includes
  electronic decision mutual exclusion, retained management requests and all seven
  empty-success HTTP response adapters, plus direct root lock command coverage.
- Final Release domain: 259 passed (`domain-final-hardened.log`).
- Static XAML safety audit: clean (`python tools/xaml-audit.py`).
- Native construction/measurement: 198 Cashier cases (33 views x 6 viewports) and
  42 Call Center cases (7 views x 6 viewports), no construction exceptions.
  These use each real view without a populated DataContext; they exercise XAML
  resources/control construction, not data-template state coverage or visual QA.
- Viewports: 1920 x 1080 and 1366 x 768, with available DIP dimensions equivalent
  to 100%, 125% and 150%. Actual Windows DPI settings were not changed.
- The earlier populated footer probe passed 48 states; see
  `CURRENT_ORDER_FOOTER_VERIFICATION.md` for its exact coverage and limits.
- Seventeen reads against the running real server returned HTTP 200, including
  menu, settings, current shift/day, delivery board/drivers/services, recipes,
  inventory, suppliers/purchases, employees, backups, sales and treasury.
  Only routes/status/response sizes were saved in `live-api.json`.
- Both real station apps were observed unlocked with the manager identity, menu
  and current shift from the real API. No financial action was submitted during
  the live read-only inspection.
- The earlier `FinalVerification` Server was verified separately on port 5152.
  It has since been stopped; the current Server on 5151 is the `ProductionReady`
  Release build, including the business-day history endpoint.
- Both new `FinalVerification` station executables reached their real Arabic PIN
  screens. Cashier's first launch crashed in native `ntdll.dll` with heap error
  `0xc0000374`; a second launch of the identical binary succeeded. No managed
  XAML exception was recorded. This intermittent failure remains unresolved;
  a successful retry is not evidence that the crash has been resolved.
- Both subsequent `FinalSafety` executables launched successfully on their first
  attempt and exposed their real Arabic PIN screens. They remain locked pending
  manual authentication; this does not establish post-login visual acceptance.

The first SQL integration run inside the sandbox failed to establish encrypted
SQL connections. Running the same binaries with normal Windows SQL access
produced 436 passes and two failures: one post-login SQL timeout and one
rate-limit failure. Both passed on an initial focused rerun, but that was not
sufficient to establish the cause. The rate-limit failure lasted 81 seconds,
less than the purported 300-second override: a distinct two-permit regression
proved the host was ignoring the overrides. See `rate-limit-before.log` (fails)
and `rate-limit-after.log` (passes) for the actual correction.

A separate read-only SQL diagnostic with encryption explicitly requested reports
`encrypt_option=TRUE`. No encryption setting was disabled. The diagnostic found
no blocking session; one runnable test SQL command had elapsed 44 ms.

The two-thread rerun (`sql-bounded.log`) was cancelled after reporting two
failures because it did not actually limit async test-collection concurrency.
xUnit 2.5.3 uses the older aggressive scheduler; the suite now sets
`parallelizeTestCollections=false`. Explicit `Task.WhenAll` races inside tests
remain unchanged. [xUnit scheduling documentation](https://xunit.net/docs/running-tests-in-parallel).
An initial serial run (`sql-serial.log`) was superseded by the proven rate-limit
fix before completion; neither cancelled run is a passing acceptance run.

Builds use separate output trees so earlier executables are preserved. The latest
full `Release|Any CPU` solution build is `final-hardened-build.log`: **0 errors,
3 warnings**, outputs under `bin/FinalHardened/`, with both native probe flags
explicitly false. Warnings are two Windows App SDK legacy RID warnings
(NETSDK1206) and unreachable NuGet vulnerability metadata (NU1900), not suppressed
by this pass. `FinalVerification`, `FinalSafety`, `ProductionReady` and
`FinalStartup` logs describe earlier checkpoints. Do not run compiling jobs
concurrently: they share intermediate assemblies. Prefer completing the large
SQL suite before a fresh full build on this memory-constrained workstation.

## Visual acceptance remains blocked

Windows screenshot capture repeatedly returns
`SetIsBorderRequired failed: No such interface supported (0x80004002)`.
Mouse targeting reports `coordinate input geometry is unavailable`. Accessibility
content was readable, but this does **not** establish clipping, typography,
spacing, touch accuracy or visual consistency across every populated screen.
Native bitmap capture also returned zero pixels in the earlier footer pass.

## Implemented screens and remaining workflow scope

| Area | Implemented client scope | Remaining acceptance / software work |
|---|---|---|
| Cashier | Login, menu/categories/variants/offers, cart, delivery details, payment/split/comp, pending electronic actions, refunds, expenses, drawer requests, shifts | Populated visual/touch/DPI acceptance; physical denomination-count UI is distinct from drawer-balance inquiry |
| Call Center | Customer lookup/address intake, current-shift discovery, delivery checkout, retained print retries and per-operator drafts | Populated visual/touch/DPI acceptance and cross-station interruption exercise |
| Delivery/customers/orders | Live dispatch, readiness, assignment, outcomes, driver account/settlement, customer and order lookup | Driver/area/service authoring coverage; populated end-to-end UI exercise |
| Reports | Sales, expenses, top items, category sales and delivery summaries | Remaining report endpoints need corresponding UI coverage; visual acceptance |
| Treasury/purchasing/inventory | Account reads, deposits/withdrawals/transfers, suppliers/receipts/payments, inventory counts and waste | Immutable request retention and navigation/lock guards implemented and tested; populated real UI exercise remains |
| Menu/tax/offers; recipes | Read screens, product availability, recipe costing | Product/category/variant/price/tax/offer and recipe/preparation-recipe authoring are not implemented by these read screens |
| Employees/attendance/payroll | Roster/ledger/cash-out, attendance reads/corrections with retained retry, payroll creation/transitions/payment | Employee authoring remains absent; correction dialog native acceptance is pending; biometric integration is separate |
| Users/audit/settings | User creation, permission/active changes, administrator PIN reset and own-PIN change in Users; audit reads and settings editing | New dialogs await native compilation/runtime checks; own-PIN access for operators without the management screen and enforcement of all client-consumed settings need completion |
| Control Center | Day history, blockers/checklist, open/close/reopen and exception acknowledge/resolve | Native construction, history HTTP read and targeted SQL close/history/reopen regression passed; populated end-to-end UI acceptance remains |
| Backup | Create and verify | Restore UI is absent; isolated restore rehearsal remains required |
| Printing | Queue/status reads and job requests | Station agent and actual device transmission are not implemented; this is software work as well as hardware verification |

No new prices, financial policies or backend business rules were introduced in
this verification pass. Existing unused placeholder files were preserved; no
current navigation candidate resolves to them.

## Physical/environment verification

- Real 15-inch terminal at actual Windows 100/125/150% DPI, populated screens,
  long Arabic names, touch/keyboard focus and scroll behavior.
- Xprinter Arabic shaping/encoding, receipt/preparation routes, disconnect/retry
  behavior, and cash drawer kick independently of financial commit.
- Biometric device ingestion and duplicate event handling with the actual device.
- Restore rehearsal against an isolated SQL Server database/instance, never a
  production database containing committed financial history.
- Two-machine LAN operation and connection-loss/recovery with both stations.

This checkpoint is not a claim that the whole product is complete or accepted.

Retained requests are currently held in process memory. They protect retries and
navigation within the running station; durable recovery after process termination
or power loss is not implemented or claimed by these tests.

## Latest Release evidence (01:55 local checkpoint)

- Full `Release|Any CPU` solution build (WinUI projects mapped to x64):
  `production-ready-build.log`, 0 errors / 3 warnings. The two NETSDK1206 warnings
  concern the existing Windows App SDK RID assets; NU1900 is the unavailable
  NuGet vulnerability metadata endpoint. No warnings were suppressed.
- Fresh binaries are in each project's `bin/ProductionReady/` tree. The server
  now excludes all `bin/**` and `obj/**` from Web SDK content discovery, preventing
  nested copies of earlier verification outputs when BaseOutputPath is changed.
  Earlier failed/partial output trees were preserved.
- New Server PID 16144 runs on 5151. The original server was shut down gracefully,
  and the two older verification servers were stopped. Existing databases and
  binaries remain intact. Health, menu, current day and day history return 200
  (`production-api.json`). No live financial writes were submitted.
- Updated Cashier native probe: 198 construction/measurement cases, zero errors
  (`native-hardening-cashier.json`). DataContext and pixel/DPI limitations above
  still apply. Probe output is now isolated under its own binary directory.
- Final Call Center Release reached its Arabic PIN screen on first launch.
  Final Cashier Release crashed on first launch with `ntdll.dll 0xc0000374`
  (Windows Application event at 01:54:13), then reached its PIN screen on retry.
  This confirms the earlier intermittent startup defect remains unresolved.
  The protected WER archive could not be read even with normal unsandboxed
  access. No native stack trace is available; no speculative SDK change was made.
- Screenshot capture still fails with the same unsupported-interface error.
  Both production station apps are currently locked; their accessibility trees
  show the Arabic PIN interface. Post-login visual acceptance is not established.
- Full SQL run `sql-final.log` remains in progress. It has one identified
  test-fixture CREATE DATABASE timeout (Inventory stock-movement history), before
  the test body executed. A focused rerun is still required after suite completion.

## Subsequent runtime and command verification

- `FinalStartup` full Release build: 0 errors / 3 warnings; its client tests passed
  72/72. Both windows now construct their view models and attach initial content
  before maximizing. This ordering change has **not** resolved the native crash.
- Call Center `FinalStartup` produced Application Error events at 02:06:51 and
  02:10:04, both `ntdll.dll / 0xc0000374`. A previous `ProductionReady` Cashier also
  produced this event at 02:05:10 after having run successfully. The defect is a
  native application-lifetime reliability blocker, not a proven XAML parse error.
- The operator manually unlocked the running Cashier during inspection. Its
  delivery screen exposed real order/driver data in the accessibility tree.
  This establishes live content, not pixel, touch or DPI acceptance.
- Microsoft-signed portable ProcDump was downloaded into ignored verification
  artifacts. A monitored Call Center launch reached its Arabic PIN screen and
  a normal close exited with code 0 (`native-crash-capture.log`). A second launch
  was monitored by attachment. Successful debugger runs do not resolve the
  intermittent failures; no crash dump or causal native stack has been captured.
- The focused SQL regression for closed-day discovery and reopening passed
  (`day-history-sql.log`). It exercises the actual new read-only route.
- Root command regression coverage now links the real Call Center main/login
  view models. Only native timer scheduling/header clock are substituted in
  headless tests. All 73 client tests pass (`client-command-safety.log`).

## Final hardened binaries

- `final-hardened-build.log`: complete Release solution, 0 errors / 3 warnings,
  elapsed 4:55. Final-binary tests: client **73/73**, domain **259/259**; TRX files
  are under the corresponding `client-final-hardened` / `domain-final-hardened`
  artifact directories. Static XAML audit and `git diff --check` pass.
- Both `FinalHardened` station executables reached their real Arabic PIN screens
  on the first launch, without a new Application Error event in that launch
  window. Earlier reproduced native heap failures remain unresolved.
- The running `ProductionReady` Server assembly is byte-for-byte identical to
  `FinalHardened` (SHA-256
  `73EF6C670EA10733444BEBA58B1E3FD75E2A83978A0ED6E4C6BFDF208B26F184`). Health,
  menu, business-day history and current-day reads return 200
  (`final-live-api.json`).
- Final screenshot attempt still fails with `SetIsBorderRequired / 0x80004002`.
  The final instances are locked; no authentication was automated. The Computer
  Use skill's [guidance](C:/Users/i-seven/.codex/plugins/cache/openai-bundled/computer-use/26.901.41123/docs/guidance.md)
  says "Do not automate user authentication dialogs." It is required by the
  applied [SKILL.md](C:/Users/i-seven/.codex/plugins/cache/openai-bundled/computer-use/26.901.41123/skills/computer-use/SKILL.md).
  Earlier manual unlock observations do not establish complete screen acceptance.
- Both ProcDump-monitored Call Center instances closed normally with exit code 0;
  no dump was captured. Monitoring ended with those test processes. The portable
  tool remains in ignored verification artifacts; no global debugger registration
  or Windows security-setting change was made.

## SQL completion (before the subsequent user-management UI additions)

- Full serial SQL run completed: **437 passed / 1 failed / 438 total**
  (`sql-final.log`, `sql-final-summary.json`). The sole failure was a 30-second
  `CREATE DATABASE` fixture timeout at `FalahApiFactory.cs:56`, before inventory
  stock-movement assertions ran. The SqlClient message includes "Operation
  cancelled by user" as part of its timeout exception; the run was not cancelled.
- Unchanged inventory case, closed-day history/reopen regression and configured
  login-rate regression all passed against `FinalHardened` Release binaries:
  **3/3** (`sql-final-focused.log`). No timeout, encryption or business assertion
  was relaxed for the rerun. This supports an environment/setup timing failure;
  the original full-run failure remains recorded, not relabeled as a clean run.
- The full run contained the previous 438 cases. The added history regression is
  covered by the focused Release run, bringing distinct integration coverage to
  439 cases across the full run and focused verification.

## Management completion continued after SQL verification

- Users now has real creation and PIN dialogs over the existing backend contracts.
  Permission choices come from the server catalog, with Arabic display labels;
  no permissions are implicitly granted by the role field. Owner protection,
  privilege escalation and the existing domain PIN validation remain enforced.
- Creation, active-state and permission writes retain their original requests
  across ambiguous outcomes. User selection, other actions, navigation and lock
  cannot abandon an unresolved request. Current permissions can be revoked even
  when the actor cannot grant that permission to a new recipient.
- Own-PIN changes reconcile an ambiguous commit by proving that the intended new
  PIN resolves to the **same** user before another write. That proof does not
  switch the logged-in operator. PIN fields are masked and cleared after success
  or cancellation; unknown requests remain in memory until resolved.
- Both main windows now cancel normal system close requests while an operation
  is in flight or unresolved, using `AppWindow.Closing`, with an Arabic warning.
  This extends the existing navigation/lock guard to the close button and Alt+F4;
  it cannot protect against forced termination or power loss.
- Health and header timers stop on normal window shutdown; late health responses
  no longer update the closed view. This is lifecycle hardening, **not** a proven
  resolution of the earlier native heap failures.
- Headless client tests at this checkpoint: **82/82** (`lifetime-client.log`).
  `management-completion-build.log` completed with **0 errors / 3 warnings**,
  elapsed 4:24. Both `ManagementCompletion` apps launched. Cashier was manually
  unlocked and exposed real menu, operator and shift content; Call Center exposed
  its Arabic PIN screen. These observations do not clear the earlier native crash.

## Attendance and employee hardening continuation

- Proven backend defect: replaying an earlier correction could replace a newer
  attendance record, including when the first operation had no financial delta.
  Both regression cases fail in `attendance-before.log`. Correction now locks the
  record and commits the adjustment, approval consumption and immutable audit
  receipt together, using the existing AuditEvents table (no schema migration).
  A replay returns its recorded confirmation without reapplying old fields.
- The first corrected attendance suite passed **16/16** (`attendance-fixed.log`).
  Additional tests verify replay after server restart/consumed manager approval
  and concurrent same/different request IDs; those three cases passed in
  `attendance-balance-before.log`.
- A second regression in that run proved inconsistent running ledger balances
  when one command posts both a penalty and overtime before SaveChanges:
  expected 18000 piastres, actual 26000 on the later row. EmployeeLedgerPoster
  now includes earlier pending additions from the same context and deduplicates
  tracked entries. The broader attendance/employee SQL verification is ongoing.
- Correction UI uses the existing permission and endpoint, Arabic status choices
  and numeric input, an explicit reason, separate scrolling content and footer.
  Unknown outcomes retain the original request and block close/navigation/lock;
  confirmed retries reload current records instead of publishing an old snapshot.
- Employee ledger reads show explicit failure/retry/empty states, discard late
  responses after close or another selection, and cannot refresh away a pending
  cash-out. Attendance date reads and audit filter reads coalesce changes while
  loading and never publish a response for a superseded selection.
- Client tests passed **91/91** (`attendance-client.log`) before the additional
  audit-filter regression. XAML audit clean. A native probe now includes populated
  correction/user-creation dialogs at six DIP viewports; its build is in progress.
- The broader employee suite exposed HTTP 500 in the existing six-concurrent-
  advance regression. The assertion now retains response exception text for
  diagnosis; it has not been weakened or marked passed. Investigation continues.
- ProcDump is attached to the existing `ManagementCompletion` Call Center to
  capture a local dump if the native failure recurs. No SDK update was made from
  similar public crash reports: without a matching stack, they do not establish
  the cause of this application's earlier heap corruption.

Attendance receipts protect corrections committed after this fix. Legacy
zero-delta corrections have no historical receipt to reconstruct. Client request
retention remains in memory; forced termination/power-loss recovery is still a
separate unresolved software capability.

---

# Session checkpoint — 2026-09-09

## The intermittent native crash is resolved

`ntdll.dll 0xc0000374` was a defect in **Windows App SDK 1.5**, not in this codebase. A 30-line
WinUI application containing no Falah code reproduced it identically, and byte-identical binaries
flipped between crashing and clean purely on where they were placed - so no line of application code
could be responsible. Both clients now reference **1.7.260224002**; no source change was needed.

The 1.5 servicing branch does **not** contain the fix: overlaying 1.5.250108004 onto the reproducing
build still crashed 7/12.

Verification, in the directory shape that previously crashed: empty control app 8/12 crash on 1.5 ->
14/14 clean on 1.7; Cashier byte-identical copy 10/10 crash -> 14/14 clean; Call Center 12/12 clean.
Full evidence chain, including the crash-dump stack and the hypotheses that were disproved, is in
[NATIVE_CRASH_INVESTIGATION.md](NATIVE_CRASH_INVESTIGATION.md).

The WER post-mortem registry key used to capture the dump was removed afterwards. Twenty-six stale
SDK-1.5 output directories (2.55 GB) were deleted; every one of them still reproduced the crash.

## Modules completed this session

All built on endpoints, permissions and value sets that already existed server-side. No new business
rule, price or policy was introduced, and no value set was hardcoded in the UI that the domain or the
server already owns.

| Area | What was missing | What exists now |
| --- | --- | --- |
| Menu / pricing | `MenuEditorViewModel` was fully written but wired to no view at all | Authoring for products and sizes, categories, tax rules and price rules, each behind its own real permission |
| Employees | Read-only; a new hire could not be created from the product | Create and correct the full employee record, including the compensation fields payroll reads |
| Delivery directory | Driver and delivery-service routes existed with no client that could reach them | Add/retire drivers, author delivery services and fees, with the server's own approval fallback |
| Physical cash count | Only the drawer-balance inquiry, which is a different thing | Denomination count using the server's own Egyptian denomination list; the server computes the difference |
| Backup restore | Absent | Restore behind reason, the domain's literal confirmation phrase, and manager approval, with the Blocked outcome surfaced honestly |
| Reports | Four server reports had no UI | Tax, refunds, food cost (with its unknown-cost coverage warning) and significant actions |
| Printing | Jobs were tracked but nothing could print them | `Falah.PrintAgent` - see [PRINT_AGENT.md](PRINT_AGENT.md) |

`EscPosTicketRenderer` moved from `Falah.Infrastructure` to `Falah.Domain.Printing`. It is a pure
function over domain types, and the move lets a station agent use it without referencing the layer
that owns database access.

## Verification this session

- Full `Release` solution build: **0 errors**, 2 warnings (the pre-existing NETSDK1206 RID warnings).
- Native construction/measurement probe: **246 cases across 38 screens, zero errors**, at 1920x1080
  and 1366x768 with 100/125/150% scaling.
- Static XAML safety audit: clean.
- Print agent: 10 agent tests + 7 renderer tests pass; end-to-end drain of three real queued jobs
  against the running server produced structurally valid ESC/POS with correct Arabic.

## Honest limits that did not change

- Populated visual acceptance is still blocked by the screenshot-capture failure; construction and
  measurement are not visual QA.
- Physical verification still outstanding: Arabic on the real Xprinter, cash-drawer kick, biometric
  device, the 15-inch terminal at real DPI, and two-machine LAN operation.
- Retained requests remain in process memory; durable recovery after power loss is not implemented.
- One ticket line in the dev database renders as `(???? ???)`. That text is stored that way - it came
  from Arabic sent through a shell in an earlier session - and is a data artifact, not an encoding
  defect: the server returns the same characters over JSON.
- The print-agent end-to-end run marked three previously queued jobs as printed while using the file
  transport, so those three rows say Succeeded although no paper was produced.

## The last two software gaps are closed

**USB printers.** A thermal printer installed as a Windows print queue - the normal shape for USB
hardware once its driver is present - is now a first-class transport (`windows`), submitting with the
`RAW` datatype so the driver passes the ESC/POS stream through untouched. Verified against the real
`XP-80C` queue on this machine with the queue paused: the spooler accepted the job and spooled
exactly the 242 rendered bytes, then the job was deleted and the queue restored. No paper was used.
The agent also gained `--test-print`, a one-shot sample ticket for proving a station at install time
and for settling the Arabic encoding question against the actual printer.

**Call Center durability.** The order desk keeps its own hand-rolled retention rather than
`RetainedRequest`, so it was made durable in place rather than refactored - rewriting the checkout
control flow would have risked behaviour its tests already pin down, for a change orthogonal to it.
Its checkout and print-job requests are now written before they are sent and cleared once resolved,
and the desk recovers them in its constructor, including the original `CheckoutRequestId` so a retry
re-sends under the same idempotency key. Keys are per operator, and each client writes to its own
station directory.

### Verification after these changes

- Full `Release` solution build: **0 errors**, 2 warnings (the pre-existing NETSDK1206 RID warnings).
- Native construction probe, both clients: Cashier **246 cases / 38 screens**, Call Center
  **42 cases / 7 screens**, zero errors, at 1920x1080 and 1366x768 with 100/125/150% scaling.
- Static XAML safety audit: clean.
- Client tests: **109 passing**, including 12 Cashier durability cases and 5 Call Center ones.

### What is left is physical, not software

Arabic shaping on the real Xprinter with paper in it, the cash-drawer kick, the biometric device, the
15-inch terminal at real DPI, two-machine LAN operation, and populated visual acceptance (still
blocked by the screenshot-capture failure). The recovery path has been exercised against a stopped
and restarted process, not against a real power cut on the terminal.

### A real payroll concurrency defect, found by the suite

The full run surfaced `Six_concurrent_pay_requests_for_the_same_run_settle_exactly_once` failing with
two of six requests returning HTTP 422. It is a genuine race, not a flake, and the window is precise.

`PayPayrollHandler` looked up the treasury movement by its idempotency key, and only afterwards read
the payroll run. A request that read the movement table *before* a concurrent winner committed, and
the run row *after* it committed, saw `Status = Paid` and fell into
`throw new DomainException("يجب اعتماد مسير الرواتب قبل الدفع.")` - reporting a paid run as an
unapproved one.

That message would send an operator to pay a run that had already been paid.

The fix is the smallest domain-correct one: a run whose status is already `Paid` is a replay, not a
rejection, so the handler now performs the same idempotent-replay lookup at that point and returns
the result the winning request produced. Reading `Paid` proves the winner committed, and its movement
commits inside the same transaction, so the replay is guaranteed to be visible. The three duplicated
copies of that lookup were folded into one local function while doing it.

The window cannot be reached deterministically through the public surface - both lookups use the same
key, so they either both hit or both miss, and only a concurrent commit between them produces the
difference. Verification is therefore empirical: the previously failing test passed **8 consecutive
runs** after the change, on top of the full-suite run.

---

## Operations-audit follow-through: point of sale, product photos, payables

This pass built the items the six-persona operations audit left open. Each entry below says what was
wrong, not just what was added.

### Point of sale

**The checkout button lied about being available.** `FooterCheckoutButton` was bound to
`CanEditOperation`, so it rendered at full strength with an empty cart and refused on tap - the
command has always guarded `Cart.Count == 0` internally. A control that looks available and then
declines teaches an operator to distrust every control on the screen. It is now bound to
`CanOpenCheckout` (`CanEditOperation && Cart.Count > 0 && !IsLoadingMenu && MenuError is null &&
!HasCheckoutResult`), and `CheckoutBlockedReason` states the reason in the operator's own words in a
line above it. The line is absent, not blank, whenever the button works - a working footer carries no
reassurance text.

**Keyboard shortcuts existed nowhere in either client.** There were zero `KeyboardAccelerator` usages
in the codebase. F2 focuses the product search (and selects what is there, so a second press starts a
fresh search), F3 opens the cashier-operations menu, F12 opens checkout. They live on
`WorkspaceGrid`, which is collapsed until a shift is open, so they are inert behind the shift prompt;
each is additionally gated on `IsWorkspaceInteractive` so a key can never reach past an open modal
and change the order underneath it. F12 routes through the same `CanOpenCheckout` the button uses - a
key must never do what the disabled button refuses to.

The bar along the bottom of the catalogue now names those keys beside the controls they drive, in the
order those controls sit on it.

**Product cards.** The card grid target width went from 190 to 212px and the price from 18 to 21pt.
The price is the number an operator is checking against a customer's order; it no longer ties with
the product name for weight. The best-seller chip was given the station badge's exact geometry -
two badges on one card differing only in corner radius reads as an inconsistency, not a distinction.

### Product photos

New `ProductImages` table, keyed on the product id - one photo per product, and the bytes live in the
database so a restore brings the pictures back with the menu. It is a separate table rather than a
column because every menu read loads products and almost none of them want the bytes.

`ProductImageRules` (domain, tested) caps a photo at 512KB and accepts JPEG/PNG/WebP only, verified
against the file's own header. The cap is not arbitrary: every till downloads every menu photo and
holds them all for the length of a shift. A card is ~250px wide, so past a few hundred kilobytes it
is detail nobody will see. SVG is refused because it is a script host. The RIFF check reads the WEBP
tag too, not just the container - RIFF also wraps WAV and AVI.

The menu response carries a version stamp per product, never bytes; the clients build
`/api/products/{id}/image?v={stamp}` from it, so a replaced photo is a different URL and an image
cache cannot serve a stale one. `ImageUriToSourceConverter` holds one `BitmapImage` per URL, decoded
at 320px: the catalogue rebuilds its tiles on every keystroke in the search box, and without the
cache the cards would visibly blank and refill while somebody types.

A product with no photo falls back to its category's icon - that icon says something true about the
item, where a generic placeholder would say nothing. Both cases occupy the same fixed 104px band, so
a half-photographed menu still lays out as one even grid. The menu editor's photo panel says as much
in words rather than leaving the operator to guess that a missing photo is acceptable.

Upload and removal are their own commands, deliberately outside the editor's form save: they are
gated on `menu_manage` like every other menu-authoring command and audited, but they never enter
`RetainedRequest`, which exists so an ambiguous **write** can be replayed verbatim. Half a megabyte
of image bytes has no business in a durable retry file, and a photo that failed to upload is a photo
the user simply picks again.

Both clients render photos, from the same URL and with the same fallback. They show the same menu;
a picture that appeared on one till and not the other would read as a fault rather than a difference.

### Best sellers, computed - there is no favourite flag

The agreed replacement for a manual favourite star. The menu response carries the products actually
sold most over the last 14 business days, ranked, capped at 12, filtered to products still on the
menu. Fourteen days survives a quiet Tuesday and still moves when the menu or the season does; a
lifetime ranking would freeze whatever sold most in the restaurant's first year.

The shortcut appears as its own chip at the head of the category strip and is **not** added to
`Categories` - putting it there would make every one of its products appear twice in a search. A
restaurant with no sales history yet gets no chip at all rather than an empty one.

### Supplier due dates and payables aging

`PurchaseEntity.DueDate` is nullable and stays null unless somebody agreed a term. Neither the
receive form nor the editor prefills one, because a prefilled due date becomes an agreed one nobody
agreed to - which is exactly how a payables report starts inventing overdue invoices.

`PayablesAging` (domain, tested) keeps two questions apart. "How old is this invoice" is always
answerable from its receipt date. "Is it overdue" is only answerable when a due date exists; an
invoice without one is not overdue and is not on time either, and is reported in its own bucket. The
aging screen and the owner dashboard both state the unscheduled total separately for that reason -
a payables report that looked clean while those existed would be misleading. Buckets are the ordinary
accounting ones and are a way of reading the numbers, not a policy about when to pay.

`SetPurchaseDueDateHandler` exists because terms are frequently agreed after the goods arrive. It
moves no money, so it carries none of receiving's treasury machinery; it is audited because it
changes what the report calls overdue.

The invoice list is built from each purchase's own remaining balance rather than from the supplier
ledger: the ledger is the source of truth for what a **supplier** is owed, but it does not attribute
payments to individual invoices, so it cannot say which invoice is late. The two agree because every
command that changes a remaining balance recomputes it from the ledger at the moment of the write.

### A restore now backs up what it is about to destroy

`RestoreBackupHandler` takes a `COPY_ONLY` backup of the current database after every gate passes and
before anything is discarded, and a failure to take it **blocks the restore**. Refusing costs the
operator a retry; not refusing can cost the restaurant its books. Only the file survives the restore -
the `BackupRecords` row for it is written to the database about to be overwritten - so the safety
file's path is also recorded in the audit entry for the restore itself.

### Alerts the dashboard was not raising

Added to `RebuildAttention`: a backup that exists but is stale (with its age in hours, because the
number is the finding), failed backups, sold lines with no known cost at all (louder than an
incomplete cost - the profit figures cannot be produced, not merely understated), overdue supplier
invoices with the age of the oldest, invoices with no agreed due date, absences and missing
fingerprints, and print routes with tickets queued past ten minutes.

The two thresholds are stated as facts about consequence, not as policy: a backup older than a day
means a day of trade is unrecoverable, and a prep ticket queued past ten minutes arrives after the
food should already have left.

Hourly sales and the wage ratio were already on the dashboard from the previous pass; the audit
items for them were closed by inspection, not by new code.

### A cost of zero was being shown for a recipe nobody had priced

`RecipesViewModel.CostDisplay` printed `0.00 ج` for a recipe at 0% cost completeness, because the DTO
was non-null and its `FoodCostPiastres` was zero. Zero is not a cheap recipe, it is no answer. It now
reads as unknown at 0% coverage, and a partly-priced recipe reads as a floor ("from X") - the known
portion is a real lower bound, so throwing it away would be as wrong as printing it bare.

### Verification after these changes

- Full `Release` solution build: **0 errors**.
- Both WinUI clients built `x64` self-contained: **0 errors, 0 warnings**.
- Static XAML safety audit: clean.
- Native construction probe: **252 cases / 0 errors / 0 empty visual trees**, at 1920x1080 and
  1366x768 with 100/125/150% scaling.
- Domain tests: **308 passing**, including the new `PayablesAgingTests` and `ProductImageRulesTests`.

### Still not verified, and why

The pre-restore safety backup is not exercised end-to-end by an automated test, for the same reason
the restore itself is not: reaching it requires passing every gate, and the next statement forces the
shared SQL Server instance into single-user mode and kills every concurrently running test's
connection. The gates above it are covered; the safety backup and the restore sequence below it need
a dedicated, isolated SQL Server instance before this path is used against a real database.

Product photo upload is covered at the rules level and by the manual path through the editor; there
is no automated test that pushes real image bytes through the HTTP endpoint yet.
