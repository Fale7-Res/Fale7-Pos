# src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `f5423f850838c567c7e347cc2e54100bd8ac7c873349f712d297c4d214af596c`

Worktree SHA-256: `f5423f850838c567c7e347cc2e54100bd8ac7c873349f712d297c4d214af596c`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Read-only server-scoped order history and detail selection.

## Source paths and function/resource analysis

- `ApplyFilter` at `src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:35`: Filters already-authorized loaded records by number/name/phone CurrentCultureIgnoreCase. Clears/repopulates observable list per search; no server query/pagination.

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:54`: Single IsLoading guard reads actual server list with current actor ID; retains old rows on failed refresh and exposes LoadError. No cancellation/epoch if view abandoned; no ledger writes.

- `OpenOrder` at `src/Falah.Client.CashierWinUI/ViewModels/OrdersViewModel.cs:79`: Selects passed DTO for details; CloseOrder clears it. This is history, not an active pickup queue and has no stage/payment transition UI-003.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing large history paging and stale refresh-after-navigation tests; screenshot layout not observed.

## Recommendations (not implemented)

Preserve history independently from a future operational pending pickup list; add bounded/paginated history for scale.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
