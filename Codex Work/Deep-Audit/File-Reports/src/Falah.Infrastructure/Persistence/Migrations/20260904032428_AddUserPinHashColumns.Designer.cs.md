# src/Falah.Infrastructure/Persistence/Migrations/20260904032428_AddUserPinHashColumns.Designer.cs

**path**: src/Falah.Infrastructure/Persistence/Migrations/20260904032428_AddUserPinHashColumns.Designer.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 753bb76c2c5de269cbe191a144cfa85b34b0d78ed8340c5d153fa8ca1f177c1b

**git_blob_sha256**: 774d3f551e7d90787650b61b43b8abdd4d4886764b9b43caa6e231e39ca46137

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**coverage_classification**: CHANGED_BLOCK_REVIEW_PLUS_VERIFIED_EXACT_SOURCE_EQUIVALENCE

**reviewed_line_spans**: [[1, 2401]]

**functionality_and_reasoning**: Intermediate Users target makesplaintextPin nullablemax/removesuniqueindex, introducesnullableHash/Salt/Iterations. Noiterationsminimum/hashalgorithm/credentialformat oruniquePIN schema checks. ActualUp/Down whollyread hasnohashconversion beforelaterdrop; currentstartup repairmustbeverified. Wholechangedmodel/currentparent checked;74exactoldblocks,2manualchanged. No credentials read orprinted, noDB/authentication run.

**significant_methods**: BuildTargetModel plus individually mapped generated entity/index/FK/navigation lambdas; exact stable identities in Knowledge shard.

**interactions_and_risks**: Intermediate Users target makesplaintextPin nullablemax/removesuniqueindex, introducesnullableHash/Salt/Iterations. Noiterationsminimum/hashalgorithm/credentialformat oruniquePIN schema checks. ActualUp/Down whollyread hasnohashconversion beforelaterdrop; currentstartup repairmustbeverified. Wholechangedmodel/currentparent checked;74exactoldblocks,2manualchanged. No credentials read orprinted, noDB/authentication run.

**findings**: []

**evidence**: Current parent model applicability/registration order/relationships and all changed blocks manually inspected. 74 exact source spans verified by SHA256 AND actual text equality to analyzed predecessor; 2 changed blocks manually reasoned. Reused evidence is NOT fresh independent rereading or runtime validation.

**tests_and_evidence**: Static only; no migration/DB/test execution. Exact equivalence evidence: Sub-Agents/Results/financial-model-equivalence-ledger.json

**recommendations**: Trace actualbackfillinvocation beforeDropPlaintext, preservepopulatedusers viaapprovedisolatedupgradefixtures, enforcevalidhashiterations andapplicationPIN uniqueness; parentSecurity ownscredentialvulnerability conclusions.

**uncertainties**: Historical target metadata is not applied database state or proof of upgrade safety. Independent depth total UNKNOWN; current handler/callpath coverage separate.
