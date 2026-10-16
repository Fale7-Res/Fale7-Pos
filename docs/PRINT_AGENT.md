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
[FINAL_CLOSEOUT_REVIEW.md](FINAL_CLOSEOUT_REVIEW.md). Physical approval remains pending.

## Configuration (`appsettings.json`)

| Key | Meaning |
| --- | --- |
| `ServerBaseAddress` | The Falah.Server this station talks to |
| `Route` | `Receipt`, `CallCenter`, `Charcoal`, `Grill`, `DeliveryOrder` or `DriverDispatch` - this station's own printer |
| `ReportedByUserId` | The user print attempts are recorded against |
| `Pin` | That user's PIN - the agent logs in like any other client. Keep it only in each station's deployed config (see `GO_LIVE_CLEANUP_CHECKLIST.md` §12.2) |
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
| `Order` (or empty, for every job written before slips existed) | Customer receipt, kitchen ticket, delivery/dispatch ticket | Checkout, or an explicit print/reprint |
| `ShiftOpen` | Shift opening slip: cashier, shift, date, time, opening float | Opening a shift, in the same transaction |
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

## Running it

```bash
dotnet run --project src/Falah.PrintAgent -- path/to/appsettings.json
```

The config path argument is optional; it defaults to the `appsettings.json` beside the executable.

Start the server first. Exit codes:

| Code | Meaning |
| --- | --- |
| 1 | Invalid configuration (the problems are listed) |
| 2 | `--test-print` could not reach the printer |
| 3 | Could not log in - usually the server is not running yet. The agent does not retry; start it again once the server is up |
| 4 | Another agent is already running for the same route on this machine. Two agents would print every ticket twice, so the second refuses to start |
