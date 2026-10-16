# src/Falah.Domain/Common/DomainException.cs

**path**: src/Falah.Domain/Common/DomainException.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 763b8c5644ed42f312f652daa85cb920474290175021e94b272ca4f02f90e00d

**git_blob_sha256**: 763b8c5644ed42f312f652daa85cb920474290175021e94b272ca4f02f90e00d

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 10]]

**functionality_and_reasoning**: Full10 lines read. Message-only user-facing domain exception, no inner/codes/localization separation. Host middleware must map consistently. General InvalidOperation/EF exceptions elsewhere won't be same category. No inherent defect in simple class.

**significant_methods**: Constructor

**interactions_and_risks**: Full10 lines read. Message-only user-facing domain exception, no inner/codes/localization separation. Host middleware must map consistently. General InvalidOperation/EF exceptions elsewhere won't be same category. No inherent defect in simple class.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared handler intentional DomainException paths.

**recommendations**: Stable machine error code optional future design, preserve Arabic operator message.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
