# src/Falah.Infrastructure/Orders/InventoryDeductionService.cs

**path**: src/Falah.Infrastructure/Orders/InventoryDeductionService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 2fe3c06b9088f36947671e498a2b4f555848cf889f08c77c9b792e6833e67cdc

**git_blob_sha256**: 84ebc845f35151158a80f2d08cd76e78e98989c8b540eb5232604bee96541222

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 98]]

**functionality_and_reasoning**: Stock per recipe line and one packaging movement in checkout context. Duplicate ingredient lines disambiguated by line index. Missing inactive recipes and missing Yield ratio silently skip stock. No availability balance guard or reversal. Packaging matches raw order-type string, risking alias mismatch. Unchecked quantity multiplication.

**significant_methods**: DeductForOrderAsync; DeductOrderPackagingAsync

**interactions_and_risks**: Stock per recipe line and one packaging movement in checkout context. Duplicate ingredient lines disambiguated by line index. Missing inactive recipes and missing Yield ratio silently skip stock. No availability balance guard or reversal. Packaging matches raw order-type string, risking alias mismatch. Unchecked quantity multiplication.

**findings**: []

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
