# 04 — Audit Coverage Matrix

## Previous coverage claims, checked

The resilience audit reported 22 workflows: 10 deep, 8 partial, 4 light (inventory, menu, reports, customers).
That claim is consistent with its own files. Its scenario matrix and workflow table cover what they say, and the
four light workflows had only historical findings and performance notes.

This consolidation (Mission A, 2026-11-05) re-audited the four light workflows and supplier interactions.
Coverage levels below are the **combined** level after all four audits.

**Levels:**
- **Deep:** every relevant failure class considered, with code traced.
- **Partial:** main paths traced; some failure classes not considered.
- **Light:** only historical findings or performance considered.

## Matrix

| # | Workflow | Functions and endpoints inspected | Failure scenarios considered | Important invariants | Coverage | Findings | Remaining unknowns |
|---|---|---|---|---|---|---|---|
| W1 | Cash/card checkout | `CheckoutOrderHandler`, `/api/orders/checkout`, `OrderAutoPrintService` | Crash at each boundary, duplicate, double-click, concurrent close, negative inputs, timeout | One order per request; sale in an open shift | Deep | FIN-0010, FIN-0015, API-0004 | Real latency |
| W2 | Split payment and confirm | Checkout, `ConfirmElectronicPaymentHandler`, journal | Confirm, retry, two electronic legs | Each leg collected once | Deep | FIN-0001, FIN-0004, FIN-0003 | Frequency |
| W3 | Cancel pending electronic | `CancelPendingElectronicPaymentHandler` | Split cancel, repeat cancel | Drawer equals reality | Deep | FIN-0002 | — |
| W4 | Refund | `RefundOrderHandler`, `Order.ApplyRefund` | Same-id races, uncollected, cross-shift, split | Refund ≤ collected | Deep | FIN-0007, FIN-0025, FIN-0005 | — |
| W5 | Shift open/close | Open/Close/PrepareClose, reconciliation | Crash, retry, concurrent sale, expense/safe drop to a closed shift | Expected = actual inputs | Deep | FIN-0010, FIN-0011, FIN-0023, PRT-0017 | — |
| W6 | Business day | Open/Close, evaluator, gate | Concurrent open/close, late confirmation, snapshot consistency | Closed periods never move | Deep | FIN-0013, FIN-0014, FIN-0021, FIN-0022, RPT-0002, PERF-0003 | Race windows |
| W7 | Treasury commands | Expense, safe drop, transfer, withdrawal, deposit | Lock placement, overdraw, inverse transfers | No negative protected balance | Partial | FIN-0011, PERF-0001, DB-0010 | Deadlocks |
| W8 | Cash drawer | Queue, kicker | Backlog, printer off | Prompt opening | Deep | HW-0001, HW-0002 | Hardware |
| W9 | Delivery and custody | `DeliveryFactsComposer`, result, settle | Split custody, failed paid delivery | Driver owes uncollected only | Partial | FIN-0003, FIN-0024, FIN-0036, FIN-0042 | Crash boundaries in custody handlers |
| W10 | Printing | Create, claim, report, agent, transports, tool | Crash, lease expiry, stale report, paper out, offline, journal failure, start-up | Each ticket once; no silent loss | Deep | PRT-0001..0018 | Printer firmware |
| W11 | Payroll | Create, transition, pay | Concurrent pay/cancel, UTC month, adjustments, overlap | Paid ⇔ money posted | Partial | PAYR-0001..0008, 0017, 0019 | Policy |
| W12 | Employee ledger and attendance | Ledger poster, daily wage, attendance, corrections | Negative input, stale balance, approval replay, UTC | Ledger = earnings − payments | Partial | PAYR-0009..0016 | — |
| W13 | Purchasing / suppliers | Receive, pay, supplier list | Concurrent payments, negative lines, stock receipt during adjustment | Supplier balance correct | Partial (re-checked) | FIN-0012, INV-0009, PERF-0004, FIN-0011 | — |
| **W14** | **Inventory** | Item list, balance, movements, cost, overview, count adjustments, waste, recipes, prep recipes, packaging rules (`Program.cs:985-1229`; `StockMovementHandlers`; `InventoryViewModel` count sheet) | Stale count against sales, concurrent unit change, prep cycles, cost-data gates, refund/cancel stock, rounding, date filters | Stock = physical after a count; costs gated | **Partial → Deep (this audit)** | INV-0001..0013, INV-0007 (new), INV-0008 (new) | Recipe association (INV-0013) |
| **W15** | **Menu and pricing** | Product create/update, variants, categories (soft delete), price rules, tax rules, images, availability (`ProductHandlers`, `CategoryHandlers`, `SavePriceRuleHandler`, `TaxRuleHandlers`, `MenuEditorViewModel`) | Removing sold sizes, negative prices, price change during an open cart, concurrent edits, image upload race, durable request stuck | History preserved; prices > 0 | **Light → Partial (this audit)** | MENU-0001, MENU-0002 (new), MENU-0003 (new, unverified), UI-0017, FIN-0031 | Concurrent menu edits (no rowversion on products: not traced) |
| **W16** | **Customers** | Lookup, get, upsert (`Program.cs:1231-1256`; `UpsertCustomerHandler`; `CustomerQueryService`) | Who may read or edit, duplicates, deletion, privacy | Customer PII limited | **Light → Deep (this audit)** | CUS-0001 (new), CUS-0002 (new), SEC-0004, SEC-0008 | Legal requirements |
| **W17** | **Reports** | Report endpoints (`Program.cs:2171-2283`), `ReportingQueryService.Range`, food cost, snapshot composition | Reversed or unbounded ranges, multi-statement consistency, historical cost basis, gates | Period totals reconcile; closed periods fixed | **Light → Partial (this audit)** | RPT-0001 (confirmed), RPT-0002 (new), INV-0011, FIN-0043, RPT-0005 | Period consistency (FIN-0043) |
| W18 | Login and session | `SessionService`, `LoginAttemptGuard` | Expiry, deactivation, permission change | Session = identity | Partial | SEC-0007, SEC-0013, UI-0026 | — |
| W19 | Approvals | Issue, registry | Expiry, reuse, guessing | Single-use, bound | Partial | SEC-0017, SEC-0018, SEC-0019 | Full-suite verification |
| W20 | Users and PINs | Create, change PIN, activate | Uniqueness race, reactivation, role casing, PIN on disk | Unique PINs | Partial | SEC-0008..0014, SEC-0020 | — |
| W21 | Backup and restore | Create, verify, restore, scheduler | Name collision, prune, writers during restore, flag | Restorable recent backup | Deep | DB-0004..0006, UI-0012, OPS-0002 | Disk full |
| W22 | Start-up and migration | `Program.cs:225-288` | DB not ready, crash mid-migration, auto-migrate | Schema matches binary | Deep | DB-0001, DB-0002, DB-0007 | Production state |

## Cross-system checks (Mission A4)

| Interaction | Result | Issues |
|---|---|---|
| Inventory ↔ sales | Deduction inside the checkout transaction (`CheckoutOrderHandler.cs:251`); count sheet stale against sales | INV-0007 |
| Inventory ↔ refunds/cancellations | Cancellation disposition exists; refunds do not restock by design | INV-0005 (not a defect) |
| Menu pricing ↔ historical orders | Order lines snapshot names and prices; variants FK-protected, but removal locks the station | MENU-0002 |
| Customers ↔ delivery orders | Customer upsert by phone; ticket PII open | CUS-0001, SEC-0004 |
| Reports ↔ ledgers | Statement-level snapshots; food cost on current recipes | RPT-0002, INV-0011 |
| Shift close ↔ late payments | Confirmation stamps the original shift and day | FIN-0014 |
| Permissions ↔ financial data | Several reads ungated | SEC-0001, SEC-0002, INV-0008 |
| Printing ↔ order state | Held tickets not released on confirmation; reprints | PRT-0016, PRT-0014 |
| Background workers ↔ transactions | The backup scheduler is the only hosted service; restore has no writer barrier | DB-0005 |

## Remaining blind spots
- Concurrent edits of the same menu product (no concurrency token observed; not traced).
- Delivery custody handlers' crash boundaries.
- The Call Center client beyond checkout, print and lookup.
- The legacy WPF client (reference only).
- Every runtime behaviour: no test, build or application run was allowed.
- Hardware: printers, drawer, spooler.
- Production configuration and data.
