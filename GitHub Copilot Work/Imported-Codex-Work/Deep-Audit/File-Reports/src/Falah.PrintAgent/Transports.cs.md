# src/Falah.PrintAgent/Transports.cs

TCP, serial and file destinations.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `2d4e75988715b1e930cdccd7f79532f16f8333ec74a4b850a187d3b9da800fa1`. Working SHA-256 `2d4e75988715b1e930cdccd7f79532f16f8333ec74a4b850a187d3b9da800fa1`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-87.

Symbols indexed (index is not a claim every method was reviewed): NetworkTicketTransport@8, SerialTicketTransport@36, FileTicketTransport@70.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 14: `public NetworkTicketTransport(string host, int port, TimeSpan timeout)`
- Line 23: `public async Task SendAsync(byte[] payload, CancellationToken ct)`
- Line 42: `public SerialTicketTransport(string portName, int baudRate, TimeSpan timeout)`
- Line 51: `public Task SendAsync(byte[] payload, CancellationToken ct)`
- Line 74: `public FileTicketTransport(string directory)`
- Line 82: `public async Task SendAsync(byte[] payload, CancellationToken ct)`

TCP uses linked cancellation+timeout for connect/write/flush; completion only proves host/socket acceptance. Serial uses blocking Open/Write/Flush, ignores cancellation, and has no physical acknowledgement. File transport writes timestamp-based .escpos files, reports the same success contract, and never prints. Millisecond filename collisions can overwrite in concurrent/fast use. File success must stay distinguishable from paper success (PRT-002); backlog must not be drained to files and later assumed physically printed.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
