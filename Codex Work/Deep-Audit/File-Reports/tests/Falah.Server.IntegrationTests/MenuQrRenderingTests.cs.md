# tests/Falah.Server.IntegrationTests/MenuQrRenderingTests.cs

**path**: tests/Falah.Server.IntegrationTests/MenuQrRenderingTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 6f79719658b72e4cfa9728041ae0500d10e311843772a21c6aad220e849d3378

**git_blob_sha256**: 6f79719658b72e4cfa9728041ae0500d10e311843772a21c6aad220e849d3378

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 179]]

**functionality_and_reasoning**: Offline production bitmap renderer bytes are decoded by independent ZXing reader, materially stronger than visual-only QR assertions. Tests576 width24margin, integer modules, centered size and top quiet zone, no stroke dilation of QR and DTO roundtrip. Rasters helper assumes complete trusted renderer byte stream and indexes payload without corruption length checks; acceptable fixture parser, not production validation. Huge-payload fallback asserts undecodable and heightpositive, not that readable original payload actually appears. Single short ASCII URL leaves Unicode/medium-size/max boundary and bottom/side quietzone untested. Does not prove printer hardware resolution/thermal behavior or target website availability.

**significant_methods**: Ticket; Rasters; Decode; InkBounds; wire contract, decode, thickening, module geometry, oversize fallback and character mode facts

**interactions_and_risks**: Offline production bitmap renderer bytes are decoded by independent ZXing reader, materially stronger than visual-only QR assertions. Tests576 width24margin, integer modules, centered size and top quiet zone, no stroke dilation of QR and DTO roundtrip. Rasters helper assumes complete trusted renderer byte stream and indexes payload without corruption length checks; acceptable fixture parser, not production validation. Huge-payload fallback asserts undecodable and heightpositive, not that readable original payload actually appears. Single short ASCII URL leaves Unicode/medium-size/max boundary and bottom/side quietzone untested. Does not prove printer hardware resolution/thermal behavior or target website availability.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 179 lines fully read; URLs are literal fixture strings, none fetched; no render/test execution.

**recommendations**: Add encoded payload length/Unicode boundaries and fallback text assertion; keep decoded-raster evidence distinct from physical paper validation.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
