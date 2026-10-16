# Printing reliability and faithful preview

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Static evidence only. No printer queue was paused, resumed, written to or cleared. No physical ticket was printed. No user-active test database was changed.

## Real end-to-end pipeline

Cashier/Call Center -> HTTP checkout -> SQL order/payment/stock transaction -> best-effort automatic CreatePrintJobs -> SQL Pending jobs -> authenticated agent GET pending by route -> GET ticket -> Domain/DTO mapping -> bitmap or text ESC/POS renderer -> transport -> local outcome journal -> HTTP report-result -> SQL attempt and Succeeded/Failed state.

Dedicated DeliveryOrder job is inserted inside delivery checkout transaction, and DriverDispatch inside assignment. Shift and expense documents have separate print jobs. A job stores identity/route and re-composes contents when fetched: items/prices/station are frozen on the order, but live restaurant settings and mutable payment/delivery facts can change appearance between initial queue and later fetch. Therefore “reprint” is a reconstruction, not guaranteed immutable byte-for-byte copy of the first paper. If archival exactness is required, store a document revision and/or content snapshot plus rendered-byte hash at creation.

API links are local endpoint paths, not fetched during audit:

| Purpose | Endpoint |
|---|---|
| Queue order/reprint | POST `/api/orders/{id}/print-jobs` |
| Read queue status | GET `/api/orders/{id}/print-jobs` |
| Route polling | GET `/api/print-jobs/pending?route=...` |
| Content DTO | GET `/api/print-jobs/{id}/ticket` |
| Attempt/result | POST `/api/print-jobs/{id}/report-result` |
| Route overview | GET `/api/print-jobs/overview` |
| Shift/expense reprint | POST `/api/print-jobs/reprint-document` |

The routes are Receipt, CallCenter, Charcoal, Grill, DeliveryOrder and DriverDispatch. The server overview includes all six. Independently confirmed UI discrepancy: `PrintingViewModel.cs:118-124` builds four route cards only although route labels at 84-85 recognize the delivery routes. Missing delivery route cards/filter selection can hide actionable per-route visibility; the UI reviewer owns the canonical finding. This does not prove every delivery failure is invisible because NeedsAttention and aggregate counts can still include it.

## What existing safeguards really prove

Order/create-print keys avoid same-request job duplication. Report attempt ID is minted once and reused for delivery retries. The agent writes an uncertain attempt before send, persists the final observation, and re-reports instead of reprinting after report failure/restart. Corrupt journal fails closed. Same-machine Global route mutex rejects duplicate agents on that machine. Frozen station/item data protects historical kitchen routing from current menu edits. Failed jobs are not automatically retried by Pending polling: a person must decide how to recover without duplicate food.

Those safeguards are useful, but not exactly-once physical printing. A named mutex cannot protect another PC; an unclaimed GET can give both agents the same job. Two independent journals do not know the other station printed. The processor also drains a snapshot; server state can change after fetch without a pre-send ownership check. PRT-001 captures this risk.

File completion proves a file exists. TCP completion proves socket submission, serial completion proves local write/flush, Windows RAW completion proves at most spooler submission. None verifies paper is present, legible, complete or collected. The current job Succeeded and last-success health do not carry that distinction (PRT-002). The serial/Windows paths ignore cooperative cancellation and Windows finalization return codes (PRT-003). Session loss after startup leaves persistent 401 polling without reauthentication (PRT-004).

The report journal temp-file move improves ordinary crash safety, but does not prove power-loss durability (no explicit disk flush) or survive loss of that station disk. A safe deployment needs stable journal storage, visible uncertain outcomes, and backup/recovery procedures that never replay physical sends blindly.

## Arabic, receipt composition and preview fidelity

`BitmapTicketRenderer` uses Windows GDI+ shaping with RTL Segoe fonts, 576-dot paper and 24-dot side margins by default. It measures wrapping, lays out rows right to left, applies protected inverse-region stroke thickening, stamps QR integer modules after thickening, and emits GS v 0 raster bytes with feed/cut boundaries. Receipt is two physical segments separated by TicketCut; a single job need not mean a single piece of paper. Long Arabic item names, modifiers, order numbers, prices, negative values and mixed Latin digits require visual verification.

The text renderer fallback is a different layout/encoding path. Configured code page alone is insufficient proof of contextual Arabic shaping. Non-Windows RenderAsImage currently falls back to text; it does not produce a faithful image equivalent. Fonts, DPI/width, firmware, thermal density and paper remain device-dependent. Existing code comments claim historical physical experiments; this audit did not repeat or independently validate them.

The tracked `tools/ReceiptPreview/Program.cs` creates synthetic receipt/reprint/shift/expense fixtures using production composition and bitmap renderer, writes .escpos, decodes the finished raster bytes into PNG and checks QR with ZXing. This is valuable renderer evidence. It is **not a preview of an arbitrary actual saved order** and does not cover all kitchen/pickup/delivery cases. No embedded native receipt-paper preview action is demonstrated by the inspected PrintingViewModel; order details are not paper layout.

A faithful live-order preview should, after future approval, read the real job/ticket DTO, run the exact station render settings, and decode those resulting bytes to separate PNG/PDF pages. It must show route, order, revision, paper width, margins and whether the content is current reconstruction or archived original. It must never create a print job, report success, modify payment/stock or send printer bytes. An optional preview of an already emitted file should decode that exact saved byte stream and identify its hash. This is a proposed design, not a new component implemented during audit.

Raster-preview limits: feed distance and cut location need explicit visualization; GDI pixels do not predict thermal darkness, missing dots or mechanical cut position. QR pixel decode is software proof only; camera scan from real paper remains a separately authorized hardware test. Renderer uses whole-ticket images and several per-pixel buffers, so huge receipts can exhaust memory or exceed 16-bit raster height. Width/margin and content-length bounds should be tested before future production rollout.

## Safe test strategy and prohibited shortcuts

No audited operations tool was executed. `verify-windows-printer-transport.ps1` is unsafe on live queues: it ignores pause return values, removes every generic Falah ticket document match (including old restaurant work), and resumes in finally. A dedicated test queue and exact spooler job ID are prerequisites, with explicit authorization. `--test-print` sends directly through the configured transport without login and default config selects a real printer. Neither is a read-only probe.

Do not drain a real backlog through File transport to make dashboard green; those jobs become Succeeded despite no paper and will no longer be Pending when physical transport returns. Do not clear attempt journals to resolve warnings without reconciling actual output. Do not rerun checkout to repair printing. Use isolated fake transport tests for concurrency, cancellation, lost acknowledgements and retry; use offline real-content rendering for visual checks; reserve physical output testing for an approved dedicated queue/device.

Recommended verification matrix (not executed here):

| Layer | Evidence required |
|---|---|
| DTO mapping | Every text/row/rule/cut/QR property survives round trip |
| Layout | Long Arabic/Latin/notes, amounts, all routes and every segment visually inspected |
| QR | Exact payload independently decoded from final raster; bad/oversized link fallback checked |
| Queue | Stable creation key, concurrent claim fencing, old pending backlog visibility |
| Recovery | Crash before send, mid-send, after send/before report, after report/before forget; no blind resend |
| Transport | Explicit acceptance level, exact spooler ID, failure/short write/finalization/cancellation |
| Physical | Authorized paper readability, clipping, darkness, feed/cut, offline/paper-out behavior |

Full per-file provenance and static coverage are in `reviews/printing-ledger.json`; no runtime claim is implied by a full static file review.
