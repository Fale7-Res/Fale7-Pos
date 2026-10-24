# Mirrored / duplicated code audit status

**INCOMPLETE.** Updated UTC: 2026-10-18T04:03:47.638337+00:00

Original scope is repository-wide semantic and exact duplication/architecture (original Phase11). The saved AST scan only groups bodies with at least20tokens, preserving identifiers/literals and ignoring trivia. It excludes shorter bodies, inactive source and semantic equivalence with different names. A complete semantic-scope denominator is UNKNOWN;218 candidate groups are not218 confirmed defects or exhaustive mirror coverage.

| Saved class | Groups | Meaning |
|---|---:|---|
|Generated migration/model repetition|102|Intentional historical duplication accounted; not102 fresh manual semantic approvals|
|Typed Program endpoint adapters|13|52 occurrences source-challenged as distinct typed handlers, not duplicated business logic|
|Client retained-request helper|6|Mirrored local journal methods compared; client caller/lifecycle distinctions retained|
|Currency input helper|1|Equivalent parsing compared; server raw amount validation remains separate|
|Application groups pending|72|Need manual cross-file reasoning|
|Test fixture groups pending|24|Need context/intentional fixture triage|

Manually triaged or challenged20groups cover 5 distinct source paths and 66 distinct source callable IDs. These are narrow body/context comparisons, not whole-file exhaustive-review counts. Exact paths, symbol IDs and all96 remaining group occurrences are in [machine-readable status](../../../Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.json).

Comparisons already saved include Cashier/CallCenter RetainedRequestStore, currency parsing, typed server adapters and financial mirror supplements: [treasury operations](../../../Deep-Audit/Duplication-and-Architecture/financial-mirror-comparison-20261018.json), [day/approval behavior](../../../Deep-Audit/Duplication-and-Architecture/business-day-authorization-mirrors.json). Client/server comparison is partial: client parser rejects negative input while server fee/receipt boundaries independently admit problematic signed values (FIN016/018); this is not a complete comparison of all client/server business logic.

Financial differences include validated approval provenance in Deposit/Reopen versus optional presented name in CloseShift (FIN017); day-active predicates share Open/Closing/Reopened but reads do not provide a transactional lease; display balance reads and transaction-required spending helper must not be consolidated as identical behavior. Exact source reasoning/hashes remain in original review shards and linked evidence.

The final CODE_DUPLICATION_AND_ARCHITECTURE.md is not certified complete. Existing status/files do not close Phase11. Financial coordinator continues missing comparisons while unrelated specialists remain paused. Generated historical code must not be replaced by mutable shared current-model helpers.

## Continuation after interim checkpoint 2026-10-18T04:15:33.128274+00:00

22additional ApiClient groups(45unique callable IDs) actually compared, including financial checkout/refund/electronic/shift/expense/drawer routes and shared outcome handling; no new global function receipts because existing UI receipts already cover these IDs. Narrow compared scope now42groups,6files,111distinct callable IDs.50application+24test groups remain; original218groups unchanged. Final semantic duplicate audit remains INCOMPLETE.

[Source evidence and divergent near-mirror methods](../../../Deep-Audit/Duplication-and-Architecture/client-api-financial-mirror-review.json): Cashier ReadAsync versus CallCenter SendAsync on menu/balance reads changes wording, not financial amount calculation. Exact spans and source hashes persisted. These111 IDs are narrow comparison coverage, not111independently certified whole-file business workflows.
