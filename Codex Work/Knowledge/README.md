# Persistent audit knowledge — operating contract

This directory is project memory, not a substitute for reading source. Reports are evidence-backed claims open to independent challenge. The authoritative scope remains the original 15-phase request. The latest instruction prohibits any new commit/push without explicit approval; earlier publication authorization is spent.

## Data ownership and persistence

- `Source-Symbols/<baseline-path>.json`: parent-owned immutable source/declaration identity map. `INDEXED_NOT_ANALYZED` is never an audit receipt. Stable IDs combine baseline Git-blob SHA-256, source path, Roslyn kind/qualified signature, and exact syntax span, distinguishing overloads, lambdas, constructors and accessors. Both current raw and Git-blob hashes are retained.
- `Reviews/<reviewer>/<baseline-path>.json`: independently owned reviewer shard. Every ANALYZED symbol requires actual reasoning, inputs/outputs/side effects, verified relationships, trust/DB/UI/printing/performance concerns, issues/hypotheses and missing tests. UNKNOWN/NOT_APPLICABLE must be explicit. References to another report are not automatic independent verification.
- `Flows/`: parent-owned cross-file evidence chains and entry-point matrices. New investigators should read related findings, then challenge original source rather than rediscovering or trusting blindly.
- `../Deep-Audit/MASTER_FINDINGS.json`: parent is the sole writer of the canonical ordinary audit registry. Source agent result files remain lossless historical/working evidence, never deleted or treated as another competing master. Preserve IDs and supersession/disagreement history.
- `../Sub-Agents/Checkpoints/<reviewer>.json`: reviewer-owned atomic checkpoint after each file/coherent small batch, before long messages and before yielding. Include last completed symbol/file, partial symbol/file, exact remaining IDs/paths or an exact machine-readable reference, new findings, pending independent challenges, blockers and next action.

Use `Verification-Tools/knowledge_store.py` for atomic, fsynced JSON writes and validation. A reviewer writes only its own shard/checkpoint and assigned existing reports. Parent alone updates global progress, registry, master findings and derived coverage. Do not run initial recovery/reset generators.

## Coverage and validation

Legacy full-file reports remain available and hash-checked. Legacy name-heading UI callable matches and whole-file labels are **not migrated into ANALYZED stable-symbol records automatically**. Reconcile exact signatures/spans and actual reasoning individually; record pending legacy evidence as such. Structured schema checks cannot prove review depth: independent reviewers must inspect reasoning and original source.

Do not silently remove or demote verified evidence. Save predecessor snapshots and reasoned correction/supersession records. If source hashes change, stop promotion and retain old evidence tied to its original baseline. No secrets, live sessions, customer data, database binaries, raw runtime logs or private printer output may enter knowledge files.

This audit permits source reads and pure isolated verification; no application fixes, package upgrades, configuration changes, DB operations or physical printing. Plugin failure does not block ordinary code-level analysis, but ordinary artifacts never impersonate plugin canonical results.
