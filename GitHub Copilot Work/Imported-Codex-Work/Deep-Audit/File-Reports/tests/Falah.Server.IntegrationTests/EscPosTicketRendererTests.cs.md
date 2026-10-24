# tests/Falah.Server.IntegrationTests/EscPosTicketRendererTests.cs

**path**: tests/Falah.Server.IntegrationTests/EscPosTicketRendererTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 499eac3185ff90e6edd85196e8329426991f3269e0f972784e2ab0a41ba9a1c8

**git_blob_sha256**: 7e17cc7ee695e2e96dba9365c82d8d117f5c6d03a1927f698475edfc778d0639

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 95]]

**functionality_and_reasoning**: Pure byte tests housed in integration project. Init/final cut exact and reset-before-cut asserted; bold ordering tested, center/size only presence despite names asserting preceding. Encoding override test uses ASCII equal under Latin1/UTF8 so passes if parameter ignored, contrary comment77-78. Helpers scan bounded array subsequences. No Arabic shape, row/cut pair/QR/codepage/unknown element cases here.

**significant_methods**: SimpleTicket; init/cut/bold/center/large/reset/encoding facts; IndexOf; LastIndexOf

**interactions_and_risks**: Pure byte tests housed in integration project. Init/final cut exact and reset-before-cut asserted; bold ordering tested, center/size only presence despite names asserting preceding. Encoding override test uses ASCII equal under Latin1/UTF8 so passes if parameter ignored, contrary comment77-78. Helpers scan bounded array subsequences. No Arabic shape, row/cut pair/QR/codepage/unknown element cases here.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 95 lines fully read; no byte test execution or hardware calls.

**recommendations**: Use nonASCII discriminator for encoding, assert local command ordering, test explicit cuts and formatting reset; separate these pure tests for safer isolated execution.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
