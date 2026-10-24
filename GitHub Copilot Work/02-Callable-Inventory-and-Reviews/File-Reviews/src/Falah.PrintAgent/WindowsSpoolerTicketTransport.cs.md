# src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs

RAW Windows queue submission.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `dbbefd768ce06c1f033a4ac80a56574a9564a9c35e58eba5684310a89bd38cf5`. Working SHA-256 `dbbefd768ce06c1f033a4ac80a56574a9564a9c35e58eba5684310a89bd38cf5`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-115.

Symbols indexed (index is not a claim every method was reviewed): this@8, WindowsSpoolerTicketTransport@21, DocInfo@88.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 25: `public WindowsSpoolerTicketTransport(string printerName) => _printerName = printerName;`
- Line 29: `public Task SendAsync(byte[] payload, CancellationToken ct)`
- Line 37: `internal static void Send(string printerName, byte[] payload)`
- Line 85: `private static string LastErrorMessage() => new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;`

OpenPrinter/StartDocPrinter/StartPagePrinter/WritePrinter use proper Unicode marshalling and check short writes. EndPagePrinter and EndDocPrinter return values are ignored; SendAsync blocks and ignores cancellation. StartDoc job ID is discarded, so acceptance cannot be correlated or monitored afterward. PRT-002 applies even when write succeeds: offline/out-of-paper queues can retain jobs indefinitely. Recommend return/check all completion results, preserve spooler job ID, bounded worker submission and operator-visible uncertain/queued state.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
