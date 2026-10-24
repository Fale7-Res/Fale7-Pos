# src/Falah.Application/Delivery/DeliveryServiceRequests.cs

Delivery API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `4ba55a39d229b6c4ae2ac34f03a94122ddb5ae3e066c1320cdac129071ecee59`. Working SHA-256 `4ba55a39d229b6c4ae2ac34f03a94122ddb5ae3e066c1320cdac129071ecee59`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-15.

Symbols indexed (index is not a claim every method was reviewed): CreateDeliveryServiceRequest@10, UpdateDeliveryServiceRequest@12, DeliveryServiceDto@15.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 10: `public sealed record CreateDeliveryServiceRequest(string RequestId, string Name, long? FeePiastres, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);`
- Line 12: `public sealed record UpdateDeliveryServiceRequest(`
- Line 15: `public sealed record DeliveryServiceDto(string Id, string Name, long? FeePiastres, bool Active, string CreatedBy, DateTimeOffset CreatedAtUtc);`

Driver active status, readiness, assignment, result and custody settlement are separate contracts. Request IDs cover consequential events; amount collection remains distinct from customer payment settlement. DeliveryDto exposes pending electronic state. Validate status/amount/reason and snapshot names server-side; no pickup fulfilment contract exists here.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
