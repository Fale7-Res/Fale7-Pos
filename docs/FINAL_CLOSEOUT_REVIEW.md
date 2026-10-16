# Final second-pass review — 2026-09-19

Work remains uncommitted. Existing accumulated changes were preserved. No physical printing,
production accounting updates, database cleanup, service installation, or Git reset was performed.

## Confirmed defects corrected

| Area | Trigger and correction |
| --- | --- |
| Pickup checkout | The form collected a name and phone but checkout discarded them unless the order was delivery. Pickup now sends both fields; the server can accept the order. |
| Manager approval | Two transactions could validate one unused approval before either consumed it. `Consumed` is now an EF concurrency token, so only one consumption can commit; an already consumed tracked approval is also rejected. No schema change is needed. |
| Last administrator | Concurrent permission removals/deactivations could each count the other administrator as surviving. Both handlers now perform the check and mutation in a serializable transaction. A competing transaction may be rejected and require retry, but cannot remove the final administrator. |
| Print route lock | Alternate spellings such as `receipt` and `Receipt` could name different mutexes. Valid routes are canonicalized before locking and polling; undefined enum values are rejected. |
| Print interruption | A crash/cancel between sending paper and saving the result could leave no local evidence. The agent now records an uncertain attempt before transport, then replaces it with the transport outcome before reporting. Restart re-reports rather than automatically printing again. |
| Print journal | A corrupt journal previously became an empty journal, risking duplicates. Loading now fails closed. Production journals are separate per route; legacy entries are preserved during upgrade. |
| Safe drop | An unresolved Control Center safe drop did not participate in the shared navigation/edit guard. It now retains the exact request, blocks editing/navigation, retries the same identity, and restores its modal on recovery. |
| Dispatch ticket | A queued first assignment could show the second driver's name after reassignment. Composition now uses the subsequent reassignment history to retain the earlier driver's identity. |
| Payment label | An unpaid cash-on-delivery cashier receipt could say paid. The receipt projection now supplies the persisted paid state; COD says payment on receipt until paid. No payment calculation was changed. |
| Daily statement date | Selecting another date during a load could display the prior date's response. A queued reload now fetches the final selected date. |
| Daily employee payments | Standalone `Settlement` payments were absent from employee day totals, and the displayed advance/withdrawal figure bound only advances. The statement now includes settlement and displays the sum of actual payment types. Running balance remains the existing ledger calculation. |
| Backup health | A successful but unverified/invalid backup could be called the last verified backup. The projection now requires valid verification and recorded file size. |
| Pickup display binding | Changing an order lookup did not notify the requested-pickup display properties. Notifications were added. |
| Branding | Cashier and Call Center WinUI login headings and window titles now read **فالح أبو العنبه**. Their station subtitles remain separate. |
| Deployment health check | The service-install script accepted any HTTP error, including 500, as healthy. Its default now uses `/health`, and failed responses remain failures. The script was not executed. |

## Independent assessment of the previous fixes

1. **Comp approvals:** the real persisted approval, matching action/approver and unused state are
   required even when the cashier already has comp permission. The original fix was sound for a
   sequential request; the consumption race above needed correction.
2. **One agent per route per PC:** the machine-wide mutex is the right scope and is acquired before
   processing. Route canonicalization closes the spelling bypass. This is not a server-side lease
   across multiple PCs; deploy only one agent per physical route. Uncertain output requires inspection.
3. **Printer configuration:** source remains bitmap mode, 576 dots, 24-dot margins, Windows
   `XP-80C` queue. Text thickening defaults on. Rebuild output is checked separately below.
4. **Menu performance:** products are ordered using a narrow ID projection, then full product rows
   and variants are loaded without the previous wide-row sort. Display order is restored in memory.
   This addresses the identified query-memory grant problem without extending timeouts. It does not
   guarantee every report stays fast under severe machine memory pressure. A direct read-only call
   to the production `MenuQueryService` returned 10 categories in 3.175 seconds cold / 0.034 seconds
   warm; a separate process measured 2.008 / 0.021 seconds. Cold timings include EF/process startup.
   These are service/database timings, not end-to-end HTTP or UI timings.
5. **Missing request parameters:** the exception handler preserves `BadHttpRequestException.StatusCode`
   instead of converting framework binding failures to 500. An authenticated request to
   `/api/audit/events` without `actorUserId` returned **400** from an isolated real test host.
   The smoke harness and output are in `artifacts/closeout-smoke` and `artifacts/closeout-tests/final-smoke.log`.
6. **Arabic/UI bindings:** labels remain separate from protocol enum values; affected binding paths,
   notification paths and shared theme resources were reviewed. XAML audit and both WinUI builds
   are part of verification. This is not a complete manual rehearsal of every screen and modal.
7. **Settings authorization:** manager access through `all` is intentional and consistent with the
   common authorization policy. No extra settings permission was invented.
8. **Financial behavior:** the accumulated diff is broader than presentation: it includes intentional
   standalone employee payment, delivery/refund, business-day and approval work. The review traced
   those paths and their transaction/idempotency tests. Receipt work changes projections/composition,
   not prices, tax, discounts, treasury posting or payment arithmetic. No production financial rows
   were changed by this review.

Review covered the tracked diff and new source across DTOs, domain rules, handlers/queries,
EF configuration/migrations, both WinUI clients, printing, tests and deployment tools. Generated
migration metadata was checked against the model changes; it was not treated as hand-authored logic.
Automated coverage cannot establish that no defects remain.

## Receipt concepts and selection

All previews use the **actual `BitmapTicketRenderer`**, 576-dot canvas, 24-dot margins and production
stroke thickening. The preview tool decodes the resulting ESC/POS raster bytes into PNGs; it never
opens the API, a printer queue or a transport. Each fixture contains four Arabic products, a long
name, quantities above one, an item note, embedded variants, cash 600 ج, total 545 ج and change 55 ج.

| Concept | Detailed raster | Assessment |
| --- | --- | --- |
| [01 — centered](../artifacts/receipt-closeout/01-centered.png) | 1461 dots / 182.8 mm | Large centered identity/order and a separate total stack. Easy to spot, but repeated dividers and vertical emphasis cost paper. |
| [02 — compact counter](../artifacts/receipt-closeout/02-counter.png) | 1307 dots / 163.5 mm | Centered brand/order, compact paired metadata, whitespace between items, one-row large total. Strongest balance of Arabic readability, alignment, hierarchy and paper use. **Selected.** |
| [03 — asymmetric](../artifacts/receipt-closeout/03-editorial.png) | 1342 dots / 167.9 mm | Right-aligned identity with an opposing order number and stacked metadata. Distinct and readable, but the split focal points are less immediate at the counter. |
| [Final production pair](../artifacts/receipt-closeout/production.png) | Detail 1296 dots / 162.2 mm | Selected compact concept using production composition, removing the unnecessary final item spacer. |

All lengths exclude the printer's cutter feed. The pickup ticket is 502 dots / 62.8 mm in every
concept. The gray gap in the combined PNG marks separation; it is not printed.

The detailed table has only **الصنف | الكمية | السعر**, allocated 62% / 16% / 22% of content width.
Variants are appended to names and notes immediately follow their item. Grand total is prominent
type on white; there are no boxed grids or filled bars. Extra subtotal/adjustment lines appear only
when relevant. Customer pickup identity and delivery information remain conditional.

One Receipt job still yields pickup raster → cut → detail raster → cut. The pickup has no item,
payment or QR content. Both segments are submitted in one transport operation, but physical delivery
is not atomic: loss of power or paper can leave a partial pair.

The locally generated QR uses exactly **https://fale7-res.vercel.app/**, including the trailing slash.
At these settings its 29-module symbol uses 8-dot modules: 232 dots / 29.0 mm, plus a four-module
white quiet zone on every side. QR bits are stamped after text thickening. Independent ZXing decoding
passed on all three concepts and final production pixels; renderer tests check whole-dot modules,
centering and isolation from dilation. A phone scan and hardware banding assessment remain pending.

Regenerate without printing:

```powershell
dotnet run --project tools/ReceiptPreview/ReceiptPreview.csproj -c Release --no-restore
```

## Demo conditions still requiring attention

Read-only SQL checks in this pass confirmed an open business day dated **2026-09-03**, **one open
shift**, **four pending electronic payments**, and **44 succeeded print jobs with none pending or
failed**. There are also **three open driver custodies, totaling 360 ج outstanding**. The production
settings contain the correct restaurant identity and exact menu URL. All existing migrations are
already recorded in Falah. Those records were left intact.
The earlier [demo-readiness document](DEMO_READINESS.md) also records driver custody, low stock and
test-era entries that can make the screens look untidy; those are operational data decisions.

The production server was not running when inspected. It must be started using the intended
deployment configuration before a live demo. No service was installed or production host started
automatically by this review. There was less than 1 GB free RAM during verification, and SQL's
query-memory pool was observed at 7 MB with zero waiters (below the existing 64 MB readiness
threshold). Idle build servers from verification were stopped to release memory. SQL settings and
timeouts were not changed. Check endpoint performance after closing heavy applications, using the
existing pre-demo check.

Physical receipt quality is **not approved yet**. Do not start the physical Receipt agent until the
preview has been approved. The subsequent step is the user's specified Release rebuild, empty-queue
check, one Receipt agent, one controlled test order, then stop and inspect the paper/phone scan.

## Final verification

Results are recorded in `artifacts/closeout-tests/`. Runtime edits were complete before the full
suite began; no repeated full-suite exploratory runs were used.

| Check | Result |
| --- | --- |
| Domain, Release, full suite | **371/371 passed** |
| Client, Release, full suite | **116/116 passed** |
| Integration, Release, full suite | **592/592 passed** (19:26–20:29; `final-Falah.Server.IntegrationTests.trx`) |
| Cashier WinUI x64 Release build | Passed, zero warnings/errors |
| Call Center WinUI x64 Release build | Passed, zero errors; NU1900 vulnerability-feed availability warning |
| Server and PrintAgent Release builds | Passed, zero warnings/errors |
| Rebuilt WinUI startup smoke | Both processes remained responsive and opened their correctly branded windows, then were closed; no login or sale was submitted |
| XAML audit | Clean |
| Missing required HTTP parameter | 400 from isolated SQL-backed host |
| Production menu service, read only | Warm reads 21–34 ms; first-process reads 2.01–3.18 s |
| Printer source/output config | Both bitmap=true, width=576, margin=24 |
| QR pixels | Exact URL decoded; 232×232 dots / 29.03 mm; all four 32-dot quiet zones checked in final PNG |
| Installer script | PowerShell parser passed; installation not executed |
| Diff whitespace check | Passed (`core.safecrlf=false` suppresses line-ending conversion warnings only) |

Focused runs before the full suite passed receipt composition (32), printing/QR/approval (26),
client regressions (3), dispatch/employee reporting (2), and administrator concurrency (1).
Integration tests use the existing fixture's disposable `FalahTest_*` SQL databases; they do not
run accounting operations against `Falah`. The production menu and state checks were read only.

Build/test warning NU1900 means package vulnerability data could not be fetched from NuGet in this
environment; it is not a compiler or test failure. Live end-to-end screen rehearsal, deployment as
a Windows service, and physical print quality are not established by these checks.

## Follow-up verification (after this review)

Every area above was re-checked against the code. Two small client-only gaps were closed; neither
touches the server, so the 592/592 integration run above remains authoritative.

| Area | Gap and correction |
| --- | --- |
| Daily statement date | The queued reload fetched the right date, but the late response for the previous date was still applied first and shown under the new date. A response whose date is no longer selected is now discarded. Added `Daily_statement_never_shows_a_response_for_a_date_no_longer_selected`. |
| Header branding | Both WinUI headers defaulted to "فلاح" until Settings loaded (and permanently if it could not be read). The default is now "فالح أبو العنبه". |

After these: Client tests **117/117**, Cashier and Call Center WinUI Release builds with 0 errors
(only NU1900), XAML audit clean.

Noted, not changed: a checkout that loses an approval-consumption race is correctly refused, but its
Arabic message speaks of an idempotency-key conflict; a last-administrator race victim sees the
generic retry error. Both are safe outcomes with imprecise wording.

