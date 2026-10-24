# src/Falah.Infrastructure/Settings/SettingsQueryService.cs

**path**: src/Falah.Infrastructure/Settings/SettingsQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: c798b18a8beb69403b2392c9f12a7f567bd4a62568c7f8f5e0a533cb9ee8ae53

**git_blob_sha256**: c798b18a8beb69403b2392c9f12a7f567bd4a62568c7f8f5e0a533cb9ee8ae53

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 45]]

**functionality_and_reasoning**: Full45 lines read. In-memory real restaurant identity defaults when row absent, default auto receipt/preparationtrue, default openingfloat1000 EGP. HoldPreparationUntilPaymentConfirmedfalse contradicts ElectronicPayment record unconditional frozen claim. Settings read does not persist singleton. DTO includes dynamic copy count, payroll multipliers and URLs, consumers enforce bounds poorly.

**significant_methods**: Constructor; Defaults; GetAsync; ToDto

**interactions_and_risks**: Full45 lines read. In-memory real restaurant identity defaults when row absent, default auto receipt/preparationtrue, default openingfloat1000 EGP. HoldPreparationUntilPaymentConfirmedfalse contradicts ElectronicPayment record unconditional frozen claim. Settings read does not persist singleton. DTO includes dynamic copy count, payroll multipliers and URLs, consumers enforce bounds poorly.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared update and checkout/printing policy; no settings changed.

**recommendations**: Runtime setup must override print transport safely; document actual hold default, unify threshold fallback and validate all setting ranges.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
