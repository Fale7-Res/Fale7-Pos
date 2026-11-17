# Print agent (`Falah.PrintAgent`)

The station-side process that turns queued print jobs into paper. Until it existed, the server
tracked print jobs correctly and no station could act on them - the queue simply accumulated.

## What it does

One agent serves **one route** on **one machine**, next to **one printer**:

1. Polls `GET /api/print-jobs/pending?route={Route}` - oldest first.
2. For each job: `GET /api/print-jobs/{id}/ticket` for the composed content.
3. Renders it to ESC/POS raster bytes with `BitmapTicketRenderer` when `RenderAsImage=true`
   (the restaurant configuration); otherwise uses `Falah.Domain.Printing.EscPosTicketRenderer`.
4. Transmits the bytes over the configured transport.
5. Reports the outcome with `POST /api/print-jobs/{id}/report-result` - **every attempt, success or
   failure**.

It talks HTTP only. It deliberately does not reference `Falah.Infrastructure`: a station process
must never hold a database connection.

## Why every attempt is reported

A job's `Status` is what the operator sees and what a reprint decision is made from. An unreported
failure leaves a job Pending forever with no explanation; a failure reported as success claims paper
came out when none did. Both are worse than a visible error.

Each report carries a `RequestId` minted **before** transmission and reused if the report itself has
to be retried - reporting is the one step that can fail after the printer has already printed, and
the server needs to recognise that replay rather than count a second physical attempt.

The agent saves an uncertain attempt **before transmission** in
`unreported-print-attempts-{Route}.json` beside the executable. On restart it reports that attempt
without sending the payload again. Inspect actual paper before manually retrying an interrupted job.
A corrupt journal blocks printing; do not discard it to make the agent start. Preserve journal files
when deploying. The machine-wide route mutex uses the canonical route name.

The final receipt concepts and the no-print preview tool are documented in
[FINAL_CLOSEOUT_REVIEW.md](archive/documentation-20261108.zip "Archived entry: docs/FINAL_CLOSEOUT_REVIEW.md"). Physical approval remains pending.

## Configuration (`appsettings.json`)

| Key | Meaning |
| --- | --- |
| `ServerBaseAddress` | The Falah.Server this station talks to |
| `Route` | `Receipt`, `CallCenter`, `Charcoal`, `Grill`, `DeliveryOrder` or `DriverDispatch` - this station's own printer |
| `ReportedByUserId` | Obsolete: attempts are recorded against the user the PIN logs in as (the server accepts no other). Ignored with a warning when it names someone else |
| `Pin` | **Never in this file** (it is tracked by git). The PIN of a user that holds only `print_agent`, from the `FALAH_PRINT_AGENT_PIN` environment variable (wins) or `{"Pin": "..."}` in `appsettings.local.json` beside the agent (git-ignored; survives copying a new build over). The agent refuses to start without one. If the session later ends, the agent logs in again on its own; after a refused login it waits 5 minutes (or the server's `Retry-After`) so a dead PIN cannot stop logins on its machine |
| `CashDrawerKickEnabled` | `false` by default. On **one** station only - the one wired to the till's drawer - it sends the drawer pulse when a cashier opens the drawer from the till. Requests older than `CashDrawer:KickMaxAgeSeconds` (server, default 30) expire instead of opening the drawer late |
| `CashDrawerKickBytes` | Hex bytes of the pulse, default `1B 70 00 19 FA` (ESC p, pin 2). Settle with `--test-drawer` on the real printer first; alternatives `1B 70 01 19 FA` (pin 5), `10 14 01 00 05` (DLE DC4) |
| `Transport` | `network`, `serial`, `windows` or `file` |
| `PrinterHost` / `PrinterPort` | Network printers - JetDirect raw port, 9100 by default |
| `SerialPort` / `SerialBaudRate` | Serial, including the virtual COM port some USB thermal printers expose |
| `WindowsPrinterName` | A printer installed as a Windows print queue - the usual shape for a USB thermal printer once its driver is installed (for example `XP-80C`) |
| `FileOutputDirectory` | Where the `file` transport writes tickets |
| `PollSeconds` | Queue poll interval |
| `TextEncoding` | Character encoding for ticket text in character mode only |
| `RenderAsImage` | **Must be `true` on the restaurant's Xprinter.** Tickets are drawn as a raster image, which is the only way Arabic prints correctly on it |
| `ImageWidthDots` / `ImageMarginDots` | `576` / `24` - the physically proven 80 mm values |
| `ThickenStrokes` | Defaults to `true`: 1-dot stroke thickening for text. The menu QR is never thickened |

Invalid configuration is rejected at startup with a list of problems, rather than starting an agent
that silently prints nowhere.

## USB printers

A USB thermal printer normally appears as a Windows print queue once its driver is installed, so the
`windows` transport submits to that queue with the `RAW` datatype. RAW is what makes the driver pass
the stream straight through; anything else turns ESC/POS command bytes into printed characters.

Going through the spooler also means a ticket queued while the printer is off or out of paper is held
by Windows and printed when the device comes back, rather than being lost.

## The `file` transport is for verification, not production

It writes each ticket's ESC/POS bytes to a `.escpos` file and prints nothing. That makes the whole
pipeline inspectable byte for byte on a machine with no printer attached. A station configured this
way logs a warning on startup and on every ticket.

## What a job can print

Most jobs are an order's tickets. Three are not, and they ride the same queue, the same route and
the same agent - only the composition differs, chosen by the job's `DocumentKind`:

| DocumentKind | Paper | Queued by |
| --- | --- | --- |
| `Order` (or empty, for every job written before slips existed) | Customer receipt (one piece, with the logo), kitchen ticket, delivery/dispatch ticket | Checkout, or an explicit print/reprint |
| `ShiftOpen` | **Retired 2026-11-17 (owner).** No new opening slip is queued and it cannot be reprinted (`reprint-document` answers 422). A job queued before that still composes and prints | — |
| `ShiftClose` | Handover slip: the whole cash story, the count, the difference, warnings | Closing a shift, in the same transaction |
| `Expense` | Expense voucher: amount, category, description, employee, account | Recording an expense, in the same transaction |

A slip's job id is derived from the document's own id, so replaying the business request cannot
queue a second one. The print agent is not aware slips exist.

**Reprints.** Paper produced by a reprint carries "إعادة طباعة" at the top of every piece, with the
time and who asked for it, and the reprint is written to the audit trail. Reprinting never re-posts
the document it reproduces: no order, payment, stock movement or treasury entry is created. An order
is reprinted through `POST /api/orders/{id}/print-jobs` with `isReprint: true` (the till sends
`includeKitchenRoutes: false`, so a customer's copy is never a second instruction to the kitchen);
a slip through `POST /api/print-jobs/reprint-document`.

## Arabic on the printer - settled: image mode

Verified on the physical XP-80C (2026-09). Its firmware has no Arabic code page, and five code pages
tried on the hardware all printed garbage. So tickets are rendered as a 576-dot bitmap through
Windows' own text shaping and sent as an ESC/POS raster (`RenderAsImage: true`). Only these commands
are proven on this firmware: `ESC @`, `GS v 0`, line feed, `GS V 0`. **Do not add density commands
(`ESC 7`, `DC2 #`):** they desynchronise the stream and the whole raster prints as garbage.

Character mode (`RenderAsImage: false`) is kept for other printers, but prints unreadable Arabic on
this one.

## Paper design (redesign approved 2026-11-17)

Spec: `docs/superpowers/specs/2026-11-17-paper-design-design.md`; plan:
`docs/superpowers/plans/2026-11-17-paper-design.md`.

- **Typeface: Dubai** (Medium for normal text, Bold for bold) at 23/30/40/64/88 px, chosen by the owner
  on paper over Segoe UI and Tahoma. Lines sit on a fixed 1.5 em advance (Dubai's own line height is
  42 dots at 23 px for 32 dots of ink). Dubai ships with Windows 10/11. **A station without it prints
  in the pre-redesign Segoe UI faces and sizes, and the agent logs a WARNING at start.** Check the log
  on each station.
- **Logo** on the customer receipt and the call-center copy only: `src/Falah.PrintAgent/Assets/receipt-logo.png`
  (256 × 256 dots, ~32 mm), embedded in the agent and never stroke-thickened. It is generated from
  `docs/assets/fale7-logo-original.png` by `tools/ReceiptLogo`. The owner chose "variant C": green
  and the rim print as ink, red and yellow print as paper, except the red «ale7».
- **One visual system:**
  - customer header: logo, name, the Settings sub-name;
  - internal header: name, the paper's title, the order number at 64 px, a heavy rule;
  - label/value lines whose label never wraps (`TicketRow.FitStart`);
  - amounts as `12,500 ج`;
  - dates `dd-MM-yyyy` and times `hh:mm ص/م`, Cairo;
  - order numbers isolated so `(#0281)` never flips.
- **The cashier receipt is one piece.** The separate customer-number ticket is gone; the customer
  collects with the large number on the receipt.
- **Amendment 1 (owner, same day, approved on paper):**
  - every section opens on a black band with its title in white (`TicketLayout.SectionBand`);
  - item tables are ruled grids, with each item's notes and offer inside the grid under it;
  - the takeaway receipt no longer carries the menu QR (Amendment 2 below says where it went);
  - the Cashier app's on-screen shift-close preview shows bands as centred headings and ignores
    the logo.
- **Amendment 2 (owner, same day, approved on paper): the two delivery papers swap roles.**
  - The **call-center copy**, printed when the order is taken, goes into the restaurant. It is now a
    small preparation paper, «تحضير طلب»: number, customer, area and address, items and notes.
    No logo, prices, totals or QR. It keeps the unconfirmed-transfer warning.
  - The **driver dispatch paper**, printed when a driver takes the order, goes to the customer. It is
    now the customer's full receipt: logo, number, driver, customer and phone, the priced item grid,
    totals, what to collect, delivery notes, the **menu QR**, and the settings footer. It keeps the
    «إعادة إسناد» marker. The server builds its header and totals with the same code as the takeaway
    receipt (`PrintJobQueryService.ReceiptPartsAsync`).
- **Wire compatibility:** new element kind `logo` and row flag `fitStart`. An agent older than the
  server prints a blank line for the logo and wraps labels as before. Deploy server and agents together.
- **Kitchen tickets are longer** than before the redesign: about 70 mm instead of 55 mm for the review
  sample, because of the larger owner-chosen type and the 64 px order number. `TicketTypographyTests`
  pins today's length so it cannot creep.
- **Previews:** `dotnet run --project tools/ReceiptPreview -- <dir>` renders all ten papers with the
  production code and decodes the menu QR.

**Physical acceptance (2026-11-17, XP-D200N on the development laptop, queue `XP-D200N-Charcoal`).**
The owner printed and approved, on paper, each of the ten redesigned papers: cashier receipt,
call-center copy, Charcoal and Grill kitchen tickets, delivery order, driver dispatch, driver statement
summary and detail, shift close statement, and expense voucher. He confirmed that the Arabic was
readable, nothing was clipped, the logo was clear, and every paper was cut. Logo variants and
typefaces were also chosen on that printer. Not yet done: the same check on each restaurant station
PC (Dubai present, agent log has no WARNING).

## Verification performed

- 10 agent tests plus 7 renderer tests, in `tests/Falah.Server.IntegrationTests`, covering: reporting
  a printed job, reporting a transport failure, reporting a ticket-composition failure, draining only
  this station's route, queue order, surviving a server that cannot be reached for the report, per
  attempt request ids, and DTO-to-domain mapping including Arabic and formatting flags.
- End-to-end against the running server with the `file` transport: three real queued receipt jobs
  were drained, composed, rendered and reported. The produced files begin with `ESC @`, end with
  `GS V 0` (full cut) and contain correct Arabic.

- The Windows spooler transport, against the real `XP-80C` queue on this machine, with the queue
  paused so no paper was consumed: the spooler accepted the job with the `RAW` datatype and the
  spooled job held **242 bytes - exactly the rendered payload**, which is what proves the bytes pass
  through untouched rather than being rendered as a document. The job was then deleted and the queue
  restored. Re-runnable with `tools/verify-windows-printer-transport.ps1 -PrinterName XP-80C`.

**Not verified:** what the paper actually looks like - Arabic shaping on the physical printer above
all - plus transmission over network or serial, and the cash-drawer kick. Those need the device
running with paper in it.

## Proving a station at install time

```bash
dotnet run --project src/Falah.PrintAgent -- path/to/appsettings.json --test-print
```

Sends one sample ticket through the configured transport and exits. It carries Arabic, Arabic-Indic
digits and Latin digits on purpose: this is the command that settles the `TextEncoding` question
against the actual printer, which no automated test can answer.

```bash
dotnet run --project src/Falah.PrintAgent -- path/to/appsettings.json --test-drawer
```

Sends `CashDrawerKickBytes` once through the configured transport and exits (exit 2 if the transport
refuses). Whether the drawer opened is something only a person at the counter can see - record the
printer model and the bytes that worked before setting `CashDrawerKickEnabled`.

## Running it

```bash
dotnet run --project src/Falah.PrintAgent -- path/to/appsettings.json
```

The config path argument is optional; it defaults to the `appsettings.json` beside the executable.

Start the server first. Exit codes:

| Code | Meaning |
| --- | --- |
| 1 | Invalid configuration (the problems are listed) |
| 2 | `--test-print` / `--test-drawer` could not reach the printer |
| 3 | Could not log in - usually the server is not running yet. The agent does not retry; start it again once the server is up |
| 4 | Another agent is already running for the same route on this machine. Two agents would print every ticket twice, so the second refuses to start |


## Runtime recovery and Windows service (Codex continuation)

- Startup waits for the API: network failures back off 2, 4, 8 seconds up to 60 seconds; a refused login waits five minutes. Ctrl+C also cancels startup. Fix a refused station credential promptly: repeated genuine wrong PINs still count toward the server's daily allowance. No PIN or login-response body is logged.
- DataDirectory defaults to %ProgramData%\Falah\PrintAgent. An explicit relative directory resolves against the executable directory. Per-route journals live there; logs live in its logs subdirectory. Each log rolls at 5 MiB and keeps five archives. Operational errors reach both the console and file; an unavailable file log warns once and falls back to console. Configuration errors before logger creation remain on stderr.
- Startup probes journal write/move/delete access before logging in. Old per-route/shared journals beside the executable are merged without deleting their originals or replacing entries already in the new directory. A corrupt journal fails closed. If persistence fails before transport, no paper is sent and the job is reported failed; later persistence errors do not hide the transport result or stall the queue.
- Exit codes: 0 normal shutdown; 1 invalid configuration/platform; 2 explicit test-print/test-drawer failure; 3 retired; 4 route mutex unavailable/already owned; 5 unusable journal.

On the station, publish the agent to a stable administrator-controlled directory, separate from the source/bin folders. Prepare appsettings.local.json with the dedicated print_agent account's PIN in the configuration directory (default %ProgramData%\Falah\PrintAgent\<Route>). Do not put a PIN in the base/tracked appsettings.json. Prefer the secrets file to an environment variable for a newly installed service; machine environment changes can require a restart before services inherit them.

Example registration, from an elevated PowerShell on that station:

~~~powershell
.\tools\install-falah-print-agent-service.ps1 -Route Receipt -PublishPath 'C:\Program Files\Falah\PrintAgent'
~~~

The installer preserves existing configuration and journals, checks that no agent owns the route, restricts the secrets file to SYSTEM/Administrators, and registers automatic recovery under LocalSystem. Windows-queue transport depends on Spooler. It does not overwrite a registered service; uninstall explicitly before changing registration. The service keeps the route mutex on the original main thread and cancels/waits up to 20 seconds when stopped. A Running service can still be waiting for login: inspect its log.

**On-site verification still required:** confirm the LocalSystem printer queue and any LocalMachine HTTPS trust; stop/start while the API is down; check cancelled output recovery; print one real ticket; rehearse the power-cut drill. No service was installed and no physical ticket was printed by this remediation. Uninstall preserves files:

~~~powershell
.\tools\install-falah-print-agent-service.ps1 -Route Receipt -Uninstall
~~~
