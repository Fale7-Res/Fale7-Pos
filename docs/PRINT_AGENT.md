# Print agent (`Falah.PrintAgent`)

The station-side process that turns queued print jobs into paper. Until it existed, the server
tracked print jobs correctly and no station could act on them - the queue simply accumulated.

## What it does

One agent serves **one route** on **one machine**, next to **one printer**:

1. Polls `GET /api/print-jobs/pending?route={Route}` - oldest first.
2. For each job: `GET /api/print-jobs/{id}/ticket` for the composed content.
3. Renders it to ESC/POS bytes with `Falah.Domain.Printing.EscPosTicketRenderer`.
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

## Configuration (`appsettings.json`)

| Key | Meaning |
| --- | --- |
| `ServerBaseAddress` | The Falah.Server this station talks to |
| `Route` | `Receipt`, `CallCenter`, `Charcoal` or `Grill` - this station's own printer |
| `ReportedByUserId` | The user print attempts are recorded against |
| `Transport` | `network`, `serial`, `windows` or `file` |
| `PrinterHost` / `PrinterPort` | Network printers - JetDirect raw port, 9100 by default |
| `SerialPort` / `SerialBaudRate` | Serial, including the virtual COM port some USB thermal printers expose |
| `WindowsPrinterName` | A printer installed as a Windows print queue - the usual shape for a USB thermal printer once its driver is installed (for example `XP-80C`) |
| `FileOutputDirectory` | Where the `file` transport writes tickets |
| `PollSeconds` | Queue poll interval |
| `TextEncoding` | Character encoding for ticket text - see the Arabic note below |

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

## Arabic on the printer is still unverified

`TextEncoding` is configurable precisely because this cannot be settled without the hardware. Some
ESC/POS firmwares accept UTF-8; others need a legacy Arabic codepage such as `windows-1256` selected
by its own command. Getting it wrong prints mojibake rather than raising an error, so **it must be
confirmed against the physical Xprinter before a live receipt is trusted.** The command structure
(init / bold / centre / size / cut) follows the published standard and is unit-tested for
byte-exactness; only the character-encoding step carries hardware risk.

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
