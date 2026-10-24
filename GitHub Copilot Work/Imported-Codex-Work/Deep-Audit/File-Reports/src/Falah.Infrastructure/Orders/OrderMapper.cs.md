# src/Falah.Infrastructure/Orders/OrderMapper.cs

**path**: src/Falah.Infrastructure/Orders/OrderMapper.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 37619395348335292dd8dc7469e4f21488033d3816b097ad9b55451a6af20a9d

**git_blob_sha256**: 5c422ab8a49ac985c1100ef28c17d38529ae973a4df1bd215025e461e9ba2f8c

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 204]]

**functionality_and_reasoning**: Full204 lines read. Copies all monetary snapshot fields inclmanualdiscount/tax, creates domain item ID from DBint (original GUID discarded). ToEntityOrder omits Refunds collection intentionally new orders only; using on reconstituted order would lose refunds. ToDomain assumes Items/Refunds loaded yet reporting loads Items only and uses RefundAmount total, acceptable report subset but aggregate Refunds incomplete. SplitJSON malformed or literalnull throws; electronic amount missing defaults0 silently. Status parse exact lower unlike SQL filters uppercase.

**significant_methods**: ToActor; ToDto; ToDomain; ToEntity Order/Refund/Treasury overloads; ParseOrderStatus; ParseElectronicPaymentStatus; WireSplit

**interactions_and_risks**: Full204 lines read. Copies all monetary snapshot fields inclmanualdiscount/tax, creates domain item ID from DBint (original GUID discarded). ToEntityOrder omits Refunds collection intentionally new orders only; using on reconstituted order would lose refunds. ToDomain assumes Items/Refunds loaded yet reporting loads Items only and uses RefundAmount total, acceptable report subset but aggregate Refunds incomplete. SplitJSON malformed or literalnull throws; electronic amount missing defaults0 silently. Status parse exact lower unlike SQL filters uppercase.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared checkout/refund and report calls, no mapping tests run.

**recommendations**: Document required includes and DTO/dedup identity, validate durable JSON/leg fields and full roundtrip tests across every snapshot field.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
