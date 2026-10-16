# tests/Falah.Server.IntegrationTests/PayablesAgingVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/PayablesAgingVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: d045dc58b7e1c82985a1270a9a33128b0cd262aba1a89773997bd495967a9bd6

**git_blob_sha256**: 2383839c7d82f439330c5181fb81453115eca114eb7545baea3528e27f4d8537

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 227]]

**functionality_and_reasoning**: Validates null due versus overdue,40day bucket,future term, full settlement exclusion, late agreement/clearing and past-term intake. Dashboard equality verifies two views agree but not independent supplier ledger truth; concurrent invoice cache corruption FIN005 not exercised. GET aging helper uses implicit authenticated fallback actor so cannot establish report permission policy. Repeated current date calls can cross midnight.

**significant_methods**: All15callables individually mapped and reasoned

**interactions_and_risks**: Validates null due versus overdue,40day bucket,future term, full settlement exclusion, late agreement/clearing and past-term intake. Dashboard equality verifies two views agree but not independent supplier ledger truth; concurrent invoice cache corruption FIN005 not exercised. GET aging helper uses implicit authenticated fallback actor so cannot establish report permission policy. Repeated current date calls can cross midnight.

**findings**: ["FIN-005"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 227lines fully read;15stable callable receipts persisted atomically; no runtime tests.

**recommendations**: Add independent ledger amount oracle, partial settlements, clock-controlled boundaries, old due-request replay and denied edit state invariance.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
