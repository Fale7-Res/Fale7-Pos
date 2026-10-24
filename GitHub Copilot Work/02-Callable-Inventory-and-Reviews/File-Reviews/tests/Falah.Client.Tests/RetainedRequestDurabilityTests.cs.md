# tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `1e92f68fe0139c707e9dad043c33b58a1b293173058e38491a00318dca85dc50`

Worktree SHA-256: `06c57924b6c8de149eefab9b0fba14bc3d075312a165c192ea1aad4f65fe2bb5`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Unit tests for generic cashier retained helper and JSON store.

## Source paths and function/resource analysis

- `An_ambiguous_outcome_survives_the_process_that_submitted_it` at `tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:39`: Recreates helper with same store and equality-checks exact DTO, valid simulation of reconstructed helper state but not process kill/fsync or real cashier checkout adoption.

- `A_rejection_after_an_ambiguous_outcome_keeps_the_request_on_disk` at `tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:80`: Confirms later rejection does not erase earlier unknown request. Confirming/re-sent-byte-for-byte tests correctly verify identity semantics.

- `Owners_do_not_recover_each_others_requests` at `tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:130`: Uses arbitrary owner.a/owner.b; does not verify production owner keys actually include user ID. Electronic keys do not.

- `A_corrupt_pending_file_does_not_block_the_station` at `tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:184`: Asserts corrupt data returns no pending. This encodes recovery-evidence loss as accepted behavior rather than proving financial safety.

- `A_store_that_cannot_write_still_protects_the_current_session` at `tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:196`: Makes directory unwritable by replacing with ordinary file; confirms in-memory pending. Does not assert user warned about loss of crash protection; actual App supplies no callback.

- `Dispose` at `tests/Falah.Client.Tests/RetainedRequestDurabilityTests.cs:27`: Deletes test temp folder only; tests themselves not run during this reviewer pass. Unknown mixed type and ownerless helper tests cover those documented contracts.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

This is itself test source reviewed; runtime result NOT_TESTED by this reviewer. Missing concrete cashier checkout/expense/refund restart UI-001 and electronic retry reachability UI-005.

## Recommendations (not implemented)

Expand from helper invariants to real view-model/process lifecycle and corrupt-list/multi-process tests; do not label helper unit tests whole-app durability proof.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
