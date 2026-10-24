# src/Falah.Domain/Reports/ProfitabilityRules.cs

**path**: src/Falah.Domain/Reports/ProfitabilityRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 912310fad5a965cf46b0b84c20c78b44059e61475bfc60569d950014527b038f

**git_blob_sha256**: 35427d67c57471aba5867956f172da8a16b3868d699b016757f6065f827f34b6

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 107]]

**functionality_and_reasoning**: Full107 lines read. Unknown all lines yields null; partial known cost gives upper-bound profit and completenessfalse. Completeness uses only knownLineCount, ignores unknown packaging and missing wage postings, so operating IsComplete may true despite WagePercentnull. Inputs arbitrary negative counts not checked (trusted callers). Percent null when sales<=0 correct.

**significant_methods**: ComputeGrossProfit; ComputeOperatingMargin; local Percent/WagePercent

**interactions_and_risks**: Full107 lines read. Unknown all lines yields null; partial known cost gives upper-bound profit and completenessfalse. Completeness uses only knownLineCount, ignores unknown packaging and missing wage postings, so operating IsComplete may true despite WagePercentnull. Inputs arbitrary negative counts not checked (trusted callers). Percent null when sales<=0 correct.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared ReportingQueryService packaging missing list not counted; no financial writes.

**recommendations**: Carry explicit coverage for packaging/labor plus approximate current-price costing; add missing packaging with all dish costs known and missing wages tests.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
