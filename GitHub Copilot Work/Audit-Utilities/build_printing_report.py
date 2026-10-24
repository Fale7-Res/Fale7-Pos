import sys
from pathlib import Path

def build_printing_report():
    out_dir = Path("GitHub Copilot Work/06-Printing")
    out_dir.mkdir(parents=True, exist_ok=True)

    report_md = """# Printing Reliability, ESC/POS Subsystem, and Invoice Preview Audit

- **Audit Target:** Fale7 POS Printing Pipeline, Thermal Hardware Reliability, Arabic Shaping & 6 Routes
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit
- **Status:** Complete Static Code Audit of Printing Handlers, Renderers, and PrintAgent Service.

---

## 1. Printing Subsystem Architecture & Six Operational Routes

The POS system dispatches print jobs through six specialized routes mapped to physical thermal printers (typically 80mm Xprinter XP-D200N series):

```mermaid
graph TD
    Order["Order Created / Updated"] --> Dispatcher["OrderAutoPrintService / Handlers"]
    Dispatcher -->|"Creates PrintJobEntity"| DB[("SQL Server PrintJobs Table")]

    subgraph Routes["Six Dedicated Print Routes"]
        R1["1. Receipt (Cashier Till Customer Receipt)"]
        R2["2. CallCenter (Call Center Multi-branch Receipt)"]
        R3["3. Charcoal (Kitchen Charcoal Station Ticket)"]
        R4["4. Grill (Kitchen Grill Station Ticket)"]
        R5["5. DriverDispatch (Driver Custody Slip)"]
        R6["6. DeliveryOrder (Delivery Packing Ticket)"]
    end

    DB --> Routes
    Routes --> Agent["Falah.PrintAgent (Background Worker)"]
    Agent --> Renderer["BitmapTicketRenderer (Arabic Shaping & Raster)"]
    Renderer --> Transport["RawPrinterHelper (Windows Spooler / TCP Socket)"]
    Transport --> PhysicalPrinter["80mm Thermal Hardware"]
```

### Route Specifications:
1. **Receipt (`PrintRoute.Receipt`):** Triggered upon completed checkout. Prints customer invoice including fiscal details, QR code, itemized table, taxes, discounts, tender, and change.
2. **CallCenter (`PrintRoute.CallCenter`):** Triggered when a phone delivery order is dispatched from the centralized call center interface.
3. **Charcoal (`PrintRoute.Charcoal`):** Operational kitchen ticket routed exclusively to the charcoal grill prep station, displaying item notes, timestamps, and order sequence number.
4. **Grill (`PrintRoute.Grill`):** Operational kitchen ticket routed to the grill prep station.
5. **DriverDispatch (`PrintRoute.DriverDispatch`):** Printed when a delivery order is assigned to a specific delivery driver. Contains driver name, customer address, phone number, and COD custody amount to collect.
6. **DeliveryOrder (`PrintRoute.DeliveryOrder`):** Printed immediately upon checkout for delivery orders to facilitate kitchen bagging and packaging before driver assignment.

---

## 2. Arabic Rendering & ESC/POS Protocol Forensics

### The Text-Mode vs Bitmap Raster Dilemma
In thermal receipt printing for Arabic locales, native ESC/POS character mode (`stream.Write(encoding.GetBytes(text))`) **fails completely** on standard thermal hardware (e.g. Xprinter, Epson, Bixolon):
- Standard thermal firmware lacks an internal OpenType Arabic shaping engine. Emitting raw Arabic codepage bytes (CP864 or Windows-1256) outputs isolated, disconnected glyphs in reverse order (LTR instead of RTL).
- The codebase explicitly acknowledges this in `src/Falah.Domain/Printing/EscPosTicketRenderer.cs:63-67`:
  > *"Character mode has no column engine and cannot get one: this printer's text mode is a single left-to-right character grid, and Arabic needs shaping the firmware does not do... real column alignment is the image renderer's job, which is the path this restaurant actually prints on."*

### Bitmap Ticket Renderer (`BitmapTicketRenderer.cs`):
- High-fidelity printing is achieved by rasterizing the ticket into an uncompressed monochrome 1-bit bitmap in memory using Windows DirectWrite/GDI+.
- Glyphs are shaped, ligatures applied, and RTL alignment resolved natively by the Windows typography engine.
- The rasterized image is then converted to ESC/POS raster bit image commands (`GS v 0` / `0x1D 0x76 0x30 0x00`).
- **Feed-Before-Cut Distance:** The cutter blade is physically located 15–20mm below the thermal print head. The engine correctly issues five trailing line feeds (`0x0A, 0x0A, 0x0A, 0x0A, 0x0A`) before the full cut command (`0x1D, 0x56, 0x00`) to prevent slicing through the footer or barcode.

---

## 3. Forensic Printing Deficiencies & Findings

### PRT-001: Multi-Terminal Race Condition Submits Duplicate Kitchen Jobs
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source Files:** `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`, `src/Falah.Server/Endpoints/OrderEndpoints.cs`
- **Flaw Mechanism:** When an order is created from one terminal and modified or updated from another, both terminals can trigger automatic kitchen printing simultaneously. The `PrintJobs` table lacks a composite unique constraint on `(OrderId, Route, DocumentKind, IsReprint)`. Both jobs are inserted with `Pending` status, causing the kitchen station printer to spit out duplicate preparation tickets.
- **Remediation:** Enforce database-level uniqueness for active pending print jobs, and employ `UPDLOCK` when queuing automated print jobs.

### PRT-002: Transport Spooler Acceptance Conflated with Physical Print Success
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source Files:** `src/Falah.PrintAgent/Workers/PrintPollingWorker.cs`, `src/Falah.Infrastructure/Printing/RawPrinterHelper.cs`
- **Flaw Mechanism:** `RawPrinterHelper.SendBytesToPrinter` submits the byte stream to the Windows Print Spooler (`WritePrinter`). The moment the Windows spooler buffers the job into its local queue, the function returns success. `PrintPollingWorker` immediately marks `PrintJobEntity.Status = Succeeded`. If the physical printer is offline, out of paper, or has a paper jam, the job sits silently in the spooler or is discarded, while the POS system reports that printing succeeded.
- **Remediation:** Query Windows spooler job completion status API (`JOB_STATUS_PRINTED`, `PRINTER_STATUS_PAPER_OUT`), and expose an explicit "Printer Offline / Out of Paper" notification in the cashier status bar.

### PRT-003: Blocking Spooler Submission Ignores Cancellation Tokens
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source Files:** `src/Falah.PrintAgent/Workers/PrintPollingWorker.cs:RawPrinterHelper`
- **Flaw Mechanism:** `RawPrinterHelper` invokes blocking Win32 P/Invoke APIs (`OpenPrinter`, `StartDocPrinter`, `WritePrinter`). If the network printer IP is unreachable or unresponsive, the thread blocks for the OS socket timeout (up to 45 seconds) without honoring the worker's `CancellationToken`.
- **Remediation:** Wrap network printer sockets in asynchronous, cancellable .NET `TcpClient` streams with configurable short timeouts (3–5 seconds).

### PRT-004: PrintAgent Never Re-Authenticates After Session Loss
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source Files:** `src/Falah.PrintAgent/Services/PrintAgentSessionManager.cs`
- **Flaw Mechanism:** If the POS server restarts or invalidates the agent's authentication token, `PrintPollingWorker` receives HTTP 401 Unauthorized responses in its polling loop but does not re-invoke the login handshake. The agent becomes permanently unresponsive until manually restarted.
- **Remediation:** Add automated re-authentication backoff policy upon receiving 401 responses.

---

## 4. Invoice Preview Fidelity & Physical Testing Plan

### Static Preview Fidelity:
The desktop clients (`CashierWinUI` and `Cashier WPF`) feature an in-app receipt preview panel (`ReceiptPreview.xaml`).
- Review confirms that the preview renders the exact same `TicketElement` model as the print pipeline.
- Fonts, borders, and itemized tables align with the thermal layout.

### Physical Testing Protocol (Authorized Execution Only):
In accordance with Section 14, **no physical print jobs were sent during this static audit**.
When authorized by the restaurant owner, physical verification must follow this matrix:
1. **Paper Roll Width:** Verify 80mm (576 dots @ 203 DPI) and 58mm (384 dots) scaling.
2. **Arabic Shaping Stress Test:** Print names with extensive diacritics, long customer names, and mixed Arabic/English numbers.
3. **Cutter Alignment:** Verify 5-feed spacing leaves exactly 5mm margin above cut.
4. **Offline Resilience:** Power off printer, trigger checkout, verify error banner, power on, verify single clean output without duplicates.
"""

    with open(out_dir / "PRINTING_RELIABILITY_AND_PREVIEW.md", 'w', encoding='utf-8') as f:
        f.write(report_md)
    print("Saved PRINTING_RELIABILITY_AND_PREVIEW.md.")

if __name__ == '__main__':
    build_printing_report()
