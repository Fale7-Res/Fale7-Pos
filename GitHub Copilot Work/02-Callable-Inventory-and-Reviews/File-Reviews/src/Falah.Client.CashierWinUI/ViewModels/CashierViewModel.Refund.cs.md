# src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `b6427c10ddcf4e6307ead2d5f77b7bd306811e1a5c30ca9009274ff7c9037e96`

Worktree SHA-256: `bc399716e95c2a9b42e3c5db4ad92d6e3fe9879f2b042fa539ec6298d6adea7a`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Lookup payment summary, partial refund, review existing pending electronic payment.

## Source paths and function/resource analysis

- `CanRefundOrder` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:20`: Allows only paid non-multi non-cancelled order with positive remaining amount. Split refund is deliberately unavailable, not silently implemented.

- `LookupPaymentBreakdown` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:28`: Uses recorded split amounts with Arabic method labels, avoiding misleading multi-only display; unknown method preserved visibly.

- `OpenRefund` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:75`: Prefills maximum refundable amount and requires editable valid order. CancelRefund refuses while unknown, clears manager PIN only after permitted close.

- `SubmitRefundAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:87`: Validates positive amount within recorded remaining ceiling, reason, and server manager approval if required. Real server must independently enforce final remaining ceiling under concurrency.

- `CompleteRefundAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:104`: Retains immutable request only in memory until resolved (UI-001), rotates key on success, updates lookup details from authoritative response and clears PIN.

- `ReviewPendingOrder` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:124`: Builds transient LastCheckoutResult from lookup and sets PrintJobsQueued false unconditionally. This causes HasPrintWarning to report missing tickets for an existing order without querying actual print state: plausible misleading warning, not proof of lost tickets.

- `DismissCheckoutResult` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs:132`: Preserves live cart customer details while reviewing historic order; ordinary completed checkout resets shared delivery fields but not optional pickup time (UI-006).

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing restart refund test, concurrent server remaining-balance integration evidence, and existing pending-order false print-warning test.

## Recommendations (not implemented)

Persist refund DTO before send; model unknown historic print state separately from false; reset optional pickup schedule with other order-owned fields.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
