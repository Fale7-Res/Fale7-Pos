# src/Falah.Domain/Printing/TicketLayout.cs

**path**: src/Falah.Domain/Printing/TicketLayout.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 39637ca707f513b00b85c4ba8995e381509ab6a4a99e3434091485f0217cd908

**git_blob_sha256**: a5e39989895acc03d2affe3a34cad4ef44252d072446a3dd53a34758dbbf9aa5

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 212]]

**functionality_and_reasoning**: Creates proportional RTL cell structures with fixed widths summing100 for priced, unpriced and cashier tables; quantity distinct from total price. Preserves item variant, notes and frozen offer labels. Money rows print supplied string without arithmetic or currency normalization. Pure helper cannot establish fit on58mm, glyph shaping or arbitrary long user names; renderer wrapping decides. Cashier price caption labels TotalPrice, so it is line total rather than per-unit price.

**significant_methods**: Brand; Caption; Tagline; DateTimeRow; BrandTitle; rule/line helpers; CashierItemHeader/Row/Note; LargeOrderNumber; Field; SlipTitle/Line; MoneyLine; ItemTableHeader/Row; VariantOf; ItemAnnotations

**interactions_and_risks**: Creates proportional RTL cell structures with fixed widths summing100 for priced, unpriced and cashier tables; quantity distinct from total price. Preserves item variant, notes and frozen offer labels. Money rows print supplied string without arithmetic or currency normalization. Pure helper cannot establish fit on58mm, glyph shaping or arbitrary long user names; renderer wrapping decides. Cashier price caption labels TotalPrice, so it is line total rather than per-unit price.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 212 lines read; no real ticket rendering.

**recommendations**: Verify multi-quantity amount labels and long Arabic/mixed digit content at supported paper widths through offline rendering.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
