# tests/Falah.Domain.Tests/DeliveryRulesTests.cs

**path**: tests/Falah.Domain.Tests/DeliveryRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: c7720a63722934baf1e3f3914054159c1e2810082451d3731a2f91d89472d918

**git_blob_sha256**: 1003a998fa68601d925c3a459b070a2aa69bc0d5096fbbf0b023d7d5eb4b69f1

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 201]]

**functionality_and_reasoning**: Cash responsibility, prepaid zero, row reuse, reassignment, scope, mismatch reasons and inactive driver guards tested. Expected and collected amounts are equal in settlement fixture so differing stages are not distinguished. Pending mixed cash and failed zero custody reporting are absent.

**significant_methods**: Custody, assignment, settlement, scope and address facts

**interactions_and_risks**: Cash responsibility, prepaid zero, row reuse, reassignment, scope, mismatch reasons and inactive driver guards tested. Expected and collected amounts are equal in settlement fixture so differing stages are not distinguished. Pending mixed cash and failed zero custody reporting are absent.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: No SQL assignment or real collection; address prefix duplication is literal expected behavior.

**recommendations**: Add pending split responsibilities, unequal expected/collected and failed-delivery report reconciliation.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
