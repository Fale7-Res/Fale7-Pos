# src/Falah.Domain/Authorization/PinHasher.cs

**path**: src/Falah.Domain/Authorization/PinHasher.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: dea709e111adf03d56624a80fcf330ba2cb63540f3d70acaa97e91bdba1f40b6

**git_blob_sha256**: dea709e111adf03d56624a80fcf330ba2cb63540f3d70acaa97e91bdba1f40b6

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 66]]

**functionality_and_reasoning**: PBKDF2 random salt/fixed-time compare, new PIN ASCII 4-6 and repeated digit rejection. Verify trusts stored Base64/iteration/hash lengths; invalid migrated metadata throws rather than returns false, affecting whole PIN-only candidate scan. HashWithoutValidation explicitly reserved backfill yet only comments reference backfill; legacy conversion absence under review. No PIN values output.

**significant_methods**: Hash; HashWithoutValidation; Verify; AssertValidPin

**interactions_and_risks**: PBKDF2 random salt/fixed-time compare, new PIN ASCII 4-6 and repeated digit rejection. Verify trusts stored Base64/iteration/hash lengths; invalid migrated metadata throws rather than returns false, affecting whole PIN-only candidate scan. HashWithoutValidation explicitly reserved backfill yet only comments reference backfill; legacy conversion absence under review. No PIN values output.

**findings**: []

**evidence**: Full bounded source read and cross-path reasoning, no runtime/user data mutation.

**recommendations**: Coordinate approval/role/PIN session incidental leads with dedicated Security coordinator; add concurrency and malformed-state regressions only after approved design.

**uncertainties**: No runtime reproduction. Endpoint authorization/rate limits owned root. No Roslyn semantic coverage claim.
