# tests/Falah.Domain.Tests/PrintRulesTests.cs

**path**: tests/Falah.Domain.Tests/PrintRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 5f27a9a4e14f4f98cacbf6fcc6bb3b50ba05c3fc41e5e058577d9f39f365036c

**git_blob_sha256**: 7fc4808d3814ae22c425c3afffd0c198d3093d56969bb834a87851fc47264220

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 641]]

**functionality_and_reasoning**: All 641 source lines read in three bounded segments; 120 exact callable receipts saved. Pure tests compose ticket element models, never DB/renderer/printer. Coverage includes station partition, cut-separated stub/detail identity, item-note adjacency, menu QR placement and suppression, unsigned money and handover fixture arithmetic. TextOf drops QR/style/structure; route duplicate test counts one without verifying route; kitchen no-price checks only fixed formatting. Pickup time tests assert caption not actual time/timezone. Reprint actor assertion reuses original cashier name so cannot prove attribution added, timestamp unasserted. Three-column test filters rows to count3 then checks count3, excluding old four-column rows. Handover arithmetic proves handcrafted fixture, not production reconciliation, and does not assert every displayed numeric value. Comments about old four-column items remain outdated. No production defect inferred solely from weaker tests.

**significant_methods**: Text flattening/split/item/header helpers; route partition; customer/detail layout; QR/menu suppression; pickup/payment labels; reprint; shift and expense slips

**interactions_and_risks**: All 641 source lines read in three bounded segments; 120 exact callable receipts saved. Pure tests compose ticket element models, never DB/renderer/printer. Coverage includes station partition, cut-separated stub/detail identity, item-note adjacency, menu QR placement and suppression, unsigned money and handover fixture arithmetic. TextOf drops QR/style/structure; route duplicate test counts one without verifying route; kitchen no-price checks only fixed formatting. Pickup time tests assert caption not actual time/timezone. Reprint actor assertion reuses original cashier name so cannot prove attribution added, timestamp unasserted. Three-column test filters rows to count3 then checks count3, excluding old four-column rows. Handover arithmetic proves handcrafted fixture, not production reconciliation, and does not assert every displayed numeric value. Comments about old four-column items remain outdated. No production defect inferred solely from weaker tests.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 120 individual exact symbol records; source-only review. No physical output, test execution or runtime actions.

**recommendations**: Add structured all-item-row assertions, distinct reprint actor plus timestamp and original deep equality, exact pickup/Cairo boundary time, all slip amounts, rendering/QR decode validation in safe harness, arbitrary text and long ticket cases.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
