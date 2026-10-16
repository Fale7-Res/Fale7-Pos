# src/Falah.Infrastructure/Falah.Infrastructure.csproj

**path**: src/Falah.Infrastructure/Falah.Infrastructure.csproj

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: da2f6a85d74663422ce703e3599276bf18c451c10becac10fc594de2c783ad42

**git_blob_sha256**: e2dc3ac0297542b05352f421318c9dd4b1df86849ea0c29ea904021c2f4232bf

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 25]]

**functionality_and_reasoning**: Full25 lines read. net8 references Domain+Application; EF SqlServer/Design8.0.11, HostingAbstractions8.0.1; Design PrivateAssetsall controls transitive build package. Native SQL assumptions throughout prevent SQLite/inmemory replacement integration. No SDK pin here root inventory handles props/globaljson.

**significant_methods**: ProjectReference and PackageReference XML/property declarations

**interactions_and_risks**: Full25 lines read. net8 references Domain+Application; EF SqlServer/Design8.0.11, HostingAbstractions8.0.1; Design PrivateAssetsall controls transitive build package. Native SQL assumptions throughout prevent SQLite/inmemory replacement integration. No SDK pin here root inventory handles props/globaljson.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: XML read no restore/package update.

**recommendations**: SBOM root should reflect exact/transitive runtime/native provider dependencies; no version recommendation without dedicated verified package evidence.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
