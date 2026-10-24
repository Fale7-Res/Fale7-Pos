# src/Falah.Domain/Orders/OrderItem.cs

**path**: src/Falah.Domain/Orders/OrderItem.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 40564bd8e0dfa5ed1009d804ecf9a731c00d60c87cc6dd08cb092cea5a18007b

**git_blob_sha256**: 40564bd8e0dfa5ed1009d804ecf9a731c00d60c87cc6dd08cb092cea5a18007b

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 26]]

**functionality_and_reasoning**: Full26 lines read. Name/prices/offer/prep station frozen, robust historical print data. Category/recipe/cost/unit not frozen, so historical category/cost reports intentionally current. Record accepts inconsistent Quantity*Unit vsTotal if direct construction; server pricing computes correct for normal values, unchecked overflow.

**significant_methods**: OrderItem record

**interactions_and_risks**: Full26 lines read. Name/prices/offer/prep station frozen, robust historical print data. Category/recipe/cost/unit not frozen, so historical category/cost reports intentionally current. Record accepts inconsistent Quantity*Unit vsTotal if direct construction; server pricing computes correct for normal values, unchecked overflow.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared menu authoritative pricing and report current recipe behavior.

**recommendations**: Explicit snapshot provenance contract; validate construction arithmetic if allowing alternate producers.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
