# src/Falah.Domain/Common/Money.cs

**path**: src/Falah.Domain/Common/Money.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: da6aa95727609eef384a975cefd4d124bf6f211f6a98a6a75828b42b414c5a5f

**git_blob_sha256**: da6aa95727609eef384a975cefd4d124bf6f211f6a98a6a75828b42b414c5a5f

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 50]]

**functionality_and_reasoning**: Integer-piastre value type; decimal boundary rounds AwayFromZero. Operators unchecked so extreme input can overflow; Abs long.MinValue throws. No business maxima here. Formatting current-culture dependent. Ordinary values remain exact.

**significant_methods**: FromPiastres; FromEgp; arithmetic/comparison operators; CompareTo; Abs; ToString

**interactions_and_risks**: Integer-piastre value type; decimal boundary rounds AwayFromZero. Operators unchecked so extreme input can overflow; Abs long.MinValue throws. No business maxima here. Formatting current-culture dependent. Ordinary values remain exact.

**findings**: []

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
