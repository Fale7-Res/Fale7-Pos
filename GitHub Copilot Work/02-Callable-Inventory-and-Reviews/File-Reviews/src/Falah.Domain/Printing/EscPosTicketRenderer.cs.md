# src/Falah.Domain/Printing/EscPosTicketRenderer.cs

**path**: src/Falah.Domain/Printing/EscPosTicketRenderer.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 01f404ef1b4785c2668d7263a1beaae5e8ea16ca1fc04d814d333ad69ae0f5be

**git_blob_sha256**: e803e6486ec94d4695b8b1efb00a52d6a20037a3d9d6470963e1c49f68c19446

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 146]]

**functionality_and_reasoning**: Reference character-mode encoder initializes then emits text/rows/rules, QR as plain payload, five linefeeds and cut at every explicit/final boundary. Fonts/title modes map firmware scaling; row layout intentionally degraded to spaces. No Arabic shaping, physical encoding guarantee or text control-byte filtering. Standard Start maps ESC justification0; bitmap actual runtime path differs, so limitations are not proof production ticket defect. Unknown future TicketElement silently ignored in switch. Pair write still cannot be physically atomic.

**significant_methods**: Render; WriteResetFeedAndCut; SelectCodePageTable; Justify; Bold; CharacterSize

**interactions_and_risks**: Reference character-mode encoder initializes then emits text/rows/rules, QR as plain payload, five linefeeds and cut at every explicit/final boundary. Fonts/title modes map firmware scaling; row layout intentionally degraded to spaces. No Arabic shaping, physical encoding guarantee or text control-byte filtering. Standard Start maps ESC justification0; bitmap actual runtime path differs, so limitations are not proof production ticket defect. Unknown future TicketElement silently ignored in switch. Pair write still cannot be physically atomic.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 146 lines read; no transport/hardware access or encoding experiments.

**recommendations**: Keep fallback explicit, cover unknown element behavior and raw control characters if this mode is enabled; physical firmware validation requires separate user approval.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
