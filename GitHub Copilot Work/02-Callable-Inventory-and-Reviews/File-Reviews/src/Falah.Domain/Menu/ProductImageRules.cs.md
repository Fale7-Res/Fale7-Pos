# src/Falah.Domain/Menu/ProductImageRules.cs

**path**: src/Falah.Domain/Menu/ProductImageRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 726d153f2fb151c7fd969c2c0dc199f30fdc2e36798552d79304ad1744b1de49

**git_blob_sha256**: 726d153f2fb151c7fd969c2c0dc199f30fdc2e36798552d79304ad1744b1de49

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 55]]

**functionality_and_reasoning**: Full55 lines read. Allows jpeg/png/webp, jpg normalized, max512KiB. PNG check only first4 despite length8, incomplete real signature; header-only junk accepted. JPEG3/WebP12 only also container signature not valid renderability/dimensions. No decompression pixel bound. This is format sanity guard not actual safe decoder verification.

**significant_methods**: NormalizeContentType; AssertAcceptableSize; AssertMatchesContentType

**interactions_and_risks**: Full55 lines read. Allows jpeg/png/webp, jpg normalized, max512KiB. PNG check only first4 despite length8, incomplete real signature; header-only junk accepted. JPEG3/WebP12 only also container signature not valid renderability/dimensions. No decompression pixel bound. This is format sanity guard not actual safe decoder verification.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: All source branching reasoned; image dimensions tests not yet inspected.

**recommendations**: Validate actual decode and width*height caps using platform intended decoder, test valid signature malformed contents/huge canvas and exact MIME.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
