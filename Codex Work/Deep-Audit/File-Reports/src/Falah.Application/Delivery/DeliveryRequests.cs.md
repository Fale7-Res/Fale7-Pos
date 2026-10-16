# src/Falah.Application/Delivery/DeliveryRequests.cs

Delivery API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `149b0caa8ca0114aa461db8104c8276b1995cc65e1a5ecfdf0853f866b3926a8`. Working SHA-256 `1b3cfd319183da61331af6b771f45ef6b660a5ba10c88f3b348b1dcb07aef61c`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-123.

Symbols indexed (index is not a claim every method was reviewed): CreateDriverRequest@3, DriverDto@4, SetDriverActiveRequest@12, MarkDeliveryReadyRequest@14, AssignDeliveryRequest@20, AssignDeliveryResult@24, ReassignDeliveryRequest@31, ReassignDeliveryResult@35, RecordDeliveryResultRequest@44, RecordDeliveryResultResult@48, SettleDriverCustodyRequest@54, SettleDriverCustodyResult@58, DeliveryDto@71, DeliveryEventDto@78, DriverOperationalStatus@85, BlockingOrderDto@87, DriverStatisticsDto@95, DriverAccountDto@109, serialises@111, DeliveryConflictException@120.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 3: `public sealed record CreateDriverRequest(string RequestId, string Name, string CreatedByUserId);`
- Line 4: `public sealed record DriverDto(string Id, string Name, bool Active);`
- Line 12: `public sealed record SetDriverActiveRequest(string DriverId, bool Active, string ActorUserId);`
- Line 14: `public sealed record MarkDeliveryReadyRequest(string DeliveryId, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);`
- Line 20: `public sealed record AssignDeliveryRequest(`
- Line 24: `public sealed record AssignDeliveryResult(`
- Line 31: `public sealed record ReassignDeliveryRequest(`
- Line 35: `public sealed record ReassignDeliveryResult(string DeliveryId, string DriverId, string DriverName, bool WasReplayed, bool WasNoOp);`
- Line 44: `public sealed record RecordDeliveryResultRequest(`
- Line 48: `public sealed record RecordDeliveryResultResult(string DeliveryId, string Status, bool WasReplayed);`
- Line 54: `public sealed record SettleDriverCustodyRequest(`
- Line 58: `public sealed record SettleDriverCustodyResult(`
- Line 71: `public sealed record DeliveryDto(`
- Line 78: `public sealed record DeliveryEventDto(`
- Line 87: `public sealed record BlockingOrderDto(string DeliveryId, string OrderId, string OrderNumber, string Status);`
- Line 95: `public sealed record DriverStatisticsDto(`
- Line 109: `public sealed record DriverAccountDto(`
- Line 122: `public DeliveryConflictException(string message) : base(message) { }`

Driver active status, readiness, assignment, result and custody settlement are separate contracts. Request IDs cover consequential events; amount collection remains distinct from customer payment settlement. DeliveryDto exposes pending electronic state. Validate status/amount/reason and snapshot names server-side; no pickup fulfilment contract exists here.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
