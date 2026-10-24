# Printing/workflow review summary

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Audit-only; no source changes, DB writes, API authentication, physical printing, installations, global changes or live UI interactions performed by this reviewer.

Assigned roots contain **71 tracked files**, all have per-file artifacts: **71 full static file reads/reviews**, **zero partial files**. Program.cs was completed in bounded reads: all 188 route bodies and explicit utility functions now have per-endpoint reasoning. Historical original partial artifacts remain immutable. No runtime tests executed by this reviewer. Baseline/working SHA-256 are both recorded; CRLF checkout versus LF Git blobs is normalized text equality, not tampering.

## PRT-001 — Two machines can submit the same pending kitchen job

Severity: HIGH. Confidence: high-static; independent challenge pending.

GET pending is a read-only unclaimed snapshot; each processor sends every returned job. Global mutex protects only processes on one Windows machine.

Evidence: src/Falah.Infrastructure/Printing/PrintJobQueryService.cs:42-47; src/Falah.PrintAgent/PrintJobProcessor.cs:68-78,87-108; src/Falah.PrintAgent/Program.cs:95-108; src/Falah.Infrastructure/Printing/ReportPrintJobResultHandler.cs:44-58.

Root cause: No server-side atomic claim, station ownership, lease or fencing token exists. Report idempotency protects reporting replay, not independent physical attempts.

Reproduction/test design: In an isolated fake/server harness, let two agents with separate journals and machines request the same route before either reports. Both obtain the same Pending job, both send, then each reports with a different attempt ID. No physical reproduction performed. Existing PrintAgentTests cover route selection and one-agent report retry only; add concurrent two-agent claim/fencing test, and replacement-agent unknown-outcome recovery.

Recommendation: Introduce server-side station/route registration and atomic claimed dispatch with fencing; require explicit operator handling for uncertain physical outcomes. Lease expiry alone cannot guarantee exactly-once paper.

Limits: Duplicate deployment is the trigger; one machine mutex plus one correctly configured agent per route limits exposure. Physical exactly-once remains impossible without printer acknowledgement/operator reconciliation.

## PRT-002 — Succeeded and print health collapse transport acceptance into physical completion

Severity: MEDIUM. Confidence: high-static.

Processor sets Succeeded after SendAsync returns. File write, TCP buffer acceptance and Windows RAW submission all satisfy this. Server overview derives last success from that flag; owner DTO comment describes physical output.

Evidence: src/Falah.PrintAgent/PrintJobProcessor.cs:108-112; src/Falah.PrintAgent/Transports.cs:23-33,82-86; src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:29-32,54-80; src/Falah.Application/Reports/OwnerDashboardDtos.cs:178-188.

Root cause: Boolean transport contract has no acknowledgement level or spooler submission ID; job status has no accepted/unknown/confirmed distinction.

Reproduction/test design: With file transport, a ticket is written to .escpos and report Succeeded=true despite no printer. With paused/offline spooler, accepted RAW job remains queued while app reports success. Scenario not executed on live environment. Existing success test verifies only fake transport Sent list. Add file-simulation status, paused spooler status under explicit authorization, and operator-confirmation tests.

Recommendation: Use distinct Simulated, Submitted, SpoolerQueued, Failed, OutcomeUnknown and PhysicallyConfirmed observations. Never drain real pending jobs to file as production completion.

Limits: No hardware status protocol is currently implemented; even spooler completion is not definitive paper proof.

## PRT-003 — Blocking spooler submission ignores cancellation and completion failures

Severity: MEDIUM. Confidence: high-static.

Windows SendAsync calls blocking Send and discards cancellation. EndPagePrinter/EndDocPrinter results are ignored. Serial Open/Flush also block without cooperative cancellation.

Evidence: src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:29-32,78-80; src/Falah.PrintAgent/Transports.cs:51-63; src/Falah.PrintAgent/AgentOptions.cs:47-48.

Root cause: Timeout configuration applies to HTTP/TCP/serial write but not full native spooler lifecycle; unmanaged result handling is incomplete.

Reproduction/test design: Shim native APIs in isolated test: return false from EndDocPrinter after successful WritePrinter, or block OpenPrinter. Current method can return success or fail to shut down within configured timeout. Not run against real queue. Add injectable spooler API tests for failure and hang, short writes already checked in implementation; separate integration verification from physical output.

Recommendation: Check finalization results, retain submission job ID, move blocking native submission to a controlled worker with bounded shutdown/uncertain outcome semantics.

Limits: Actual driver hang frequency was not measured; timeout must not cause automatic physical resend.

## PRT-004 — Agent never authenticates again after session loss

Severity: MEDIUM. Confidence: high-static.

Login occurs once before poll loop. All subsequent 401 failures are logged and retried with same token indefinitely.

Evidence: src/Falah.PrintAgent/Program.cs:111-120,136-151; src/Falah.PrintAgent/PrintServerClient.cs:33-40,42-63.

Root cause: No token-expiry or reauthentication recovery path; agent lifecycle assumes continuously sliding valid session.

Reproduction/test design: Isolated server/session harness: start agent, invalidate its session or let it expire during downtime, restore server, observe 401 polls and undrained queue until process restarted. Not run on active server. Add 401-then-authenticate recovery, deactivated-account terminal failure, and queued outcome reporting across reauth tests.

Recommendation: Add controlled reauthentication and explicit offline/authentication alarm without losing attempt journal or blindly resending uncertain jobs.

Limits: A continuously active sliding session avoids ordinary expiry; session invalidation or prolonged downtime triggers exposure.

## PRT-005 — Printer verification tool can delete preexisting restaurant tickets

Severity: HIGH. Confidence: high-static; independent challenge pending.

Tool pauses printer without checking success, sends generic Falah ticket, then removes every job with DocumentName Falah ticket and resumes queue.

Evidence: tools/verify-windows-printer-transport.ps1:19-22,61-65,70-73; src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs:47-50.

Root cause: Generic document-name matching replaces exact submission ownership; ignored pause result invalidates no-paper guarantee.

Reproduction/test design: Static fixture: prepopulate queue snapshot with a preexisting Falah ticket and new test Falah ticket; $mine selects both. Tool removes both. If pause returns failure, test can reach physical printer. No tool execution performed. Add fake queue fixture preserving preexisting IDs, failure-to-pause abort, failed-cleanup no-resume policy. Only isolated queue integration after approval.

Recommendation: Use dedicated test queue and exact returned job ID, verify pause state, never delete by document name; keep all live printer operations gated.

Limits: Current audit did not execute tool, submit any job or touch queues.

## PRT-006 — Saved printer width and receipt copy count do not control inspected print pipeline

Severity: LOW. Confidence: high-static.

Settings accepts PrinterWidth and NumberOfReceiptCopies, but auto-print creates one job per route and agent uses its own ImageWidthDots. Operators can save values that do not change output.

Evidence: src/Falah.Application/Settings/SettingsRequests.cs:14-15,42-43; src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs:77-82; src/Falah.PrintAgent/AgentOptions.cs:67-74; src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs:57-69.

Root cause: Persistence/UI configuration has no mapped consumer in render/dispatch implementation.

Reproduction/test design: Static reachability: assignments exist in Settings handler; no usage in Infrastructure/Printing or PrintAgent. Changing setting then queuing remains one job per customer route. No settings changed. Add intended copy/width contract tests or disable unsupported setting UI.

Recommendation: Define per-station paper capability and copy semantics; explicitly label unsupported settings until implemented. Copies must not multiply kitchen prep accidentally.

Limits: Client preview/report exports may use own dimensions; this finding concerns actual inspected thermal pipeline.

## FLOW-001 — Requested two-stage pickup preparation and final payment lifecycle is absent

Severity: HIGH_REQUIREMENT_GAP. Confidence: high-static.

Pickup exists as OrderType plus customer/time metadata, but shares checkout BuildPlan. Cash pickup must provide full payable amount before order creation and automatic kitchen job enqueue. Electronic pickup can be pending, but that is unverified payment, not pay-on-collection.

Evidence: src/Falah.Domain/Orders/CheckoutRules.cs:45-46,100-109; src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:91-100,142-145,278-280; src/Falah.Server/Program.cs:1370-1380; src/Falah.Application/Orders/CheckoutOrderRequest.cs:77-81.

Root cause: Creation, stock consumption and payment checkout share one use case; only Delivery Cash has explicit deferred cash rule.

Reproduction/test design: Pure rules: Pickup+Cash+positive total+AmountPaid=0 hits amountPaid<payableTotal. There is no create-unpaid-pickup/settle-pickup/handover endpoint in server route inventory. No database/API write executed. Recommend lifecycle acceptance tests: unpaid reservation queues kitchen once, payment later posts once, handover only after policy gate, cancellation/restock/close blockers, network replay at each stage.

Recommendation: Design separate pickup fulfilment state and payment state with server-authoritative transitions. Treat this as future approved product change, not a fix performed during audit.

Limits: Requirement gap against requested operation, not an assertion that existing prepaid pickup implementation violates its original specification.

## FLOW-002 — Held electronic preparation is not automatically released on confirmation

Severity: MEDIUM. Confidence: high-static.

HoldPreparationUntilPaymentConfirmed suppresses initial kitchen routes. Confirmation posts money/status but does not enqueue held preparation. Auto-print documentation directs operator to Order Lookup.

Evidence: src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs:59-72; src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:75-103; src/Falah.Server/Program.cs:1416-1434; src/Falah.Application/Settings/SettingsRequests.cs:17-21.

Root cause: Suppression is a one-time checkout decision without durable preparation-release state or confirmation outbox event.

Reproduction/test design: Read-only trace: pending electronic checkout with hold setting true creates customer route only; POST confirm handler contains no queue call; kitchen remains absent until manual print. Not executed. Add hold->confirm release exactly once, replay, mixed payment failure, cancellation-before-release and manual reprint tests.

Recommendation: Explicitly communicate manual release now; future approved implementation should persist held state and transactionally enqueue release with stable key on confirmation.

Limits: Manual release is documented existing behavior; issue is operational expectation and missing automated guarantee, not evidence payment confirmation always fails.

## Independent financial challenges

FIN-001 corroborated: mixed split with electronic leg starts/completes sale-{checkoutId} during initial nondeferred portion, then confirmation tries the same journal key and conflicts. Financial reviewer owns canonical report.

FIN-006 corroborated: Program.cs 830-839 forwards arbitrary transition status; PayrollRules permits Approved->Paid; TransitionPayrollHandler updates run status with employees_manage only, without payment ledger/treasury. Ordinary UI NextStatus intentionally stops at Approved, so this is API-reachable rather than a normal UI click flow. No writes used to validate.

## Continued review and independent challenges

All Program.cs endpoint bodies completed after baseline reconfirmation; exact endpoint notes are in its File-Reports entry and printing ledger. UI-001 (memory-only actual cashier checkout/Treasury request keys), UI-005 (durable electronic request with transient LastCheckoutResult recovery dead end), CallCenter reprint kitchen routes, and hidden delivery print route cards independently corroborated. Financial FIN-001/002/003/004/005/006 independently challenged and corroborated statically; respective financial/UI reviewers retain canonical IDs. No runtime reproduction or database writes.
