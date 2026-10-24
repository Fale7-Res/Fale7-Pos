# src/Falah.Infrastructure/Persistence/Migrations/20260919234725_AddSettingsDefaultOpeningCash.cs

**path**: src/Falah.Infrastructure/Persistence/Migrations/20260919234725_AddSettingsDefaultOpeningCash.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 37716c0cdfd79f1d4be37d046bee9f01591f6645cfa075d4b970afb801db1d5f

**git_blob_sha256**: e32607e6358a0944fd99551c5b5ad5e080b270a54bb4a4c4b45a836076abec38

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 32]]

**functionality_and_reasoning**: Adds nonnullable default float100000 piastres (1000 EGP) and backfills existing settings by column default. This changes suggested opening value, not account balances or automatically opens shifts. Restaurant-specific default is explicit; handler still must protect Main funds FIN-011. Down drops preference only.

**significant_methods**: Up; Down

**interactions_and_risks**: Adds nonnullable default float100000 piastres (1000 EGP) and backfills existing settings by column default. This changes suggested opening value, not account balances or automatically opens shifts. Restaurant-specific default is explicit; handler still must protect Main funds FIN-011. Down drops preference only.

**findings**: ["FIN-011"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Full32 lines inspected; no treasury or settings writes.

**recommendations**: Verify migration preference default and safe insufficient-funds open behavior.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
