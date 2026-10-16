# Pickup and delivery: current implementation and required lifecycle

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`. Static trace only. No order, payment, customer, stock, print attempt or database record was created or modified during this review. Cross-file supporting reads complement the primary financial and UI reviewers; they are not counted as full Infrastructure/client file reviews here.

## Current checkout architecture

`CheckoutOrderRequest` contains product/variant/quantity references, payment method, amount paid, optional split legs, customer details and optional `RequestedPickupAtUtc`. It does not contain a draft/reservation/preparation/handover action. `CheckoutOrderHandler.HandleAsync` resolves current product prices and station snapshots, requires an open shift and POS permission, validates customer/address data, creates the order, allocates its sequence, deducts stock and posts applicable immediate payment legs in one transaction. The HTTP checkout endpoint then calls best-effort `OrderAutoPrintService` after commit. A successful checkout can therefore have a missing kitchen job if the second step fails or the process stops between commit and queueing. The response exposes `PrintJobsQueued`; it is neither paper confirmation nor a durable outbox guarantee.

Primary evidence: `src/Falah.Application/Orders/CheckoutOrderRequest.cs` (complete contract); `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:38-145,146-280`; `src/Falah.Server/Program.cs:1370-1380`; `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs:46-93`.

## Pickup: what is actually supported

Pickup is an order type with required customer phone/name and an optional requested time. The time is validated for the five-minute past grace and maximum fourteen-day lead, frozen on the order and included in receipt/kitchen composition. It does not create a preparation schedule or due-time dispatch worker.

Cash pickup is prepaid in the current rules. `CheckoutRules.IsCashOnDelivery` defers cash only for **Delivery**, and `BuildPlan` rejects insufficient payment for every non-COD order. Consequently a positive-value Pickup/Cash request with zero paid cannot create a legitimate unpaid pickup. Card is treated as immediate; InstaPay/wallet are recorded as pending electronic claims, not as a later cash collection workflow. There is no pickup fulfilment aggregate, ready-for-collection transition, pickup board, cash pickup payment endpoint or collection/handover confirmation in the server route inventory. An existing paid pickup can be read/reprinted, but those operations do not implement deferred payment.

Evidence: `CheckoutRules.cs:45-46,93-122,125-157`; `CheckoutOrderHandler.cs:91-100,190-191`; `OrderDetailsDto.cs` requested pickup field; server route inventory `Program.cs`.

**FLOW-001 is a requirement gap**, not a retroactive claim that the original prepaid workflow was supposed to support unpaid reservations. The requested restaurant operation—take a phone pickup, start kitchen preparation, collect money at arrival, then hand over—is not achieved by renaming the existing checkout button or sending a fictional AmountPaid value. Doing that would create false payment truth.

## Delivery: implemented state and payment timing

Delivery checkout requires customer identity, service, area and street. Address is recomposed from structured fields on the server. Checkout creates a New delivery and a DeliveryOrder print job in the same order transaction; unlike regular customer/kitchen auto-print this dedicated delivery job is not behind the auto-print setting switches. It deducts stock regardless of whether cash/electronic money has settled.

Delivery cash with a positive total is COD: no initial sale collection posts; the order is not paid merely because AmountPaid is provided on the request. Ready is an explicit transition. Assignment requires Ready, validates an active driver, transitions through Assigned to OutForDelivery and creates any COD custody plus a DriverDispatch print job in one transaction. Delivery result records Delivered/Failed/Returned; collection amount is defaulted to expected only when omitted and deviations require a reason. A later driver settlement reconciles expected/actual custody and posts cash into the restaurant treasury. Delivery success and cash settlement are different facts.

Evidence: `CheckoutOrderHandler.cs:219-248`; `CheckoutRules.cs:45-85`; `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs:61-141`; `RecordDeliveryResultHandler.cs:81-151`; delivery request contracts; financial reviewer owns settlement invariants and exceptional cases.

Standalone InstaPay/wallet defer money until manual confirmation. A mixed split posts its immediate cash/card legs at checkout and leaves one electronic leg pending. **FIN-001 independently corroborated**: that initial mixed checkout completes `sale-{CheckoutRequestId}`, but confirmation attempts the same journal key; it conflicts while the electronic leg remains pending. The canonical finding belongs to the financial report. This limits any claim that split payment is usable end to end.

Current cash/card/electronic values must not be used interchangeably to simulate the missing pickup lifecycle. Pending electronic delivery remains visibly pending; assignment code does not enforce a confirmed-payment gate. Whether to release food against an unconfirmed transfer is an explicit business policy still to decide.

## Preparation and payment coordination

Auto-print normally creates customer route plus item-derived kitchen routes immediately after checkout, including pending electronic orders. `HoldPreparationUntilPaymentConfirmed=true` suppresses kitchen routes for pending electronic money. It does not persist a releasable preparation state. Confirmation only changes payment and treasury; no automatic kitchen release call is present. The existing auto-print comment instructs a manual print from Order Lookup after confirmation. **FLOW-002** documents that manual dependency and the misleading stronger meaning of “hold until confirmed”.

Manual reprint is a new print request and must stay visibly marked; it must not repeat order creation, stock deduction or money movement. The existing routing reads frozen item preparation stations. Creation idempotency and print attempt idempotency are separate keys and do not solve cross-machine duplicate physical output (PRT-001).

## Required future design, not changes made

Keep payment and fulfilment separate. A proposed pickup fulfilment state graph is:

```text
Recorded -> SentToKitchen -> Preparing -> ReadyForCollection -> HandedOver
    \           \              \                \
     +---------------- Cancelled / NoShow --------+

Payment: Unpaid -> PartiallyPaid -> Paid
         |             |           |
         +---- PendingElectronic ---+ (confirmation is a real event)
```

The final graph requires owner decisions; it is not a prescription to add unsupported partial payment. An order creation command should allocate a stable order number, freeze prices/customer/pickup promise, reserve or consume stock according to the chosen kitchen policy, and atomically write a kitchen dispatch/outbox event. A separate payment command should post actual received legs against the same order with its own replay key. Handover should enforce agreed payment/override policy and record who handed over and when. Cancellation/no-show must define stock reversal versus food waste, any received-money refund, and immutable history. Business-day/shift close blockers must include unpaid ready orders according to policy.

Kitchen dispatch should have one stable event per preparation revision, never rely on payment checkout retry to repair a missing ticket. Order edits after kitchen release need delta/cancellation tickets with revision and clear operator instructions; blindly reprinting a whole kitchen order can cook food twice. Customer receipt, pickup token, kitchen ticket, delivery order and driver dispatch remain different documents.

Acceptance test plan for a future approved implementation:

1. Record unpaid cash pickup, restart client and server, verify same order and one kitchen dispatch with no collection ledger.
2. Ready, search by phone/order, collect cash once using retained request, verify drawer/shift attribution and receipt payment truth.
3. Duplicate/network retries at creation/payment/handover do not duplicate order, collection, stock or preparation instructions.
4. Pending electronic confirmation releases held preparation once; cancellation before release cannot release later.
5. No-show, cancellation after cooking, refund, partial handover and manager override have explicit reason/audit/stock treatment.
6. Concurrent stations, shift change and business-day close preserve unpaid order visibility and financial ownership.
7. All print tests distinguish simulated file, accepted spooler and operator-confirmed physical output.

No implementation, migration or workflow mutation was performed. See `reviews/printing-findings.json` and `PRINTING_RELIABILITY_AND_PREVIEW.md` for precise printing risks.
