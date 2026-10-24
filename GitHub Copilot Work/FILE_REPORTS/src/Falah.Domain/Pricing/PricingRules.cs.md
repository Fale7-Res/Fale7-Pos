# src/Falah.Domain/Pricing/PricingRules.cs

**path**: src/Falah.Domain/Pricing/PricingRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: d2959ea1c3fa702c47649369dbbe471792ec0e753a6ad61547395a302903ec7b

**git_blob_sha256**: d2959ea1c3fa702c47649369dbbe471792ec0e753a6ad61547395a302903ec7b

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 82]]

**functionality_and_reasoning**: Full82 lines read. Inclusive endpoints; start=end branch active only exact time although summary says <= crosses midnight. Cross-midnight day filter uses current calendar day not starting-day spill, may differ intended restaurant policy. Partial window ignored. Product list/fixed price and weekday bounds validated. Comp positive and <=order, reason whitelist.

**significant_methods**: IsActiveAt; ApplicableRules; ValidateAppliedRule; AssertCompAmount; CompReasonCodes; AssertCompReasonCode; AssertPriceRuleInput

**interactions_and_risks**: Full82 lines read. Inclusive endpoints; start=end branch active only exact time although summary says <= crosses midnight. Cross-midnight day filter uses current calendar day not starting-day spill, may differ intended restaurant policy. Partial window ignored. Product list/fixed price and weekday bounds validated. Comp positive and <=order, reason whitelist.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Static deterministic equal start/end inconsistency; no runtime tests.

**recommendations**: Specify equal-time and overnight weekday semantics; test both exact boundaries. Do not invent overlap rejection.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
