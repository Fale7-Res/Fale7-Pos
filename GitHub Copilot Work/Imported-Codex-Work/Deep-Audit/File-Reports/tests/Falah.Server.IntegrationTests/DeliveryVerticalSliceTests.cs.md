# tests/Falah.Server.IntegrationTests/DeliveryVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/DeliveryVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 2cf833e812d290b7bfcb7c48307d02c36daa9a072ea9394120a12c8690c8134f

**git_blob_sha256**: e6787959b6203df697cc3431fe45649d45592da17f6ec4e12534486fc31c06e3

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 1192]]

**functionality_and_reasoning**: All 1192 source lines read in six bounded segments. Lifecycle, COD vs prepaid custody, explicit distinct-key assignment/settlement concurrency, old/new driver account transfer and frozen dispatch driver snapshots have substantive assertions. Races launch tasks without barriers; rejected writes often lack no-side-effect checks. Service-fee freeze test999-1016 creates/edits a DIFFERENT service from hardcoded svc-1 on order and checks only serviceID, not fee; cannot prove used-service fee snapshot stability. Ticket tests inspect DTO text/PrintJobs only, no hardware. Driver statistics use current clock, no boundary/reassignment range cases. No-activity statistics title allzero checks only two counters and nullable fields. Shift-with-custody fact833-860 explicitly permits close and preserves custody with slipwarning, superseding contrary stale comments159-163 in BusinessDayClose tests. COD reporting1095-1131 verifies20000 stays excluded until settlement; pure InstaPay1135 does not cover mixed pending bugFIN010. Board terminal exclusion covers Delivered only, not Failed/Returned/Settled. Full strict callable receipts in progress, not inferred from this file receipt.

**significant_methods**: Lifecycle/assignment/results/reassignment; order and dispatch ticket snapshots; driver statistics/settlement; shift custody; board/accounts/services/read-model tests

**interactions_and_risks**: All 1192 source lines read in six bounded segments. Lifecycle, COD vs prepaid custody, explicit distinct-key assignment/settlement concurrency, old/new driver account transfer and frozen dispatch driver snapshots have substantive assertions. Races launch tasks without barriers; rejected writes often lack no-side-effect checks. Service-fee freeze test999-1016 creates/edits a DIFFERENT service from hardcoded svc-1 on order and checks only serviceID, not fee; cannot prove used-service fee snapshot stability. Ticket tests inspect DTO text/PrintJobs only, no hardware. Driver statistics use current clock, no boundary/reassignment range cases. No-activity statistics title allzero checks only two counters and nullable fields. Shift-with-custody fact833-860 explicitly permits close and preserves custody with slipwarning, superseding contrary stale comments159-163 in BusinessDayClose tests. COD reporting1095-1131 verifies20000 stays excluded until settlement; pure InstaPay1135 does not cover mixed pending bugFIN010. Board terminal exclusion covers Delivered only, not Failed/Returned/Settled. Full strict callable receipts in progress, not inferred from this file receipt.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Source-only review, no API/database or printer execution. File-level review complete; individual callable receipts separately tracked.

**recommendations**: Use same real service in priced checkout then edit fee and assert frozen fee/total/ticket; race barriers and no-write-on-denial; report clock/range edges; failed/returned board, partialcash/shortage/overage and printing renderer verification.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
