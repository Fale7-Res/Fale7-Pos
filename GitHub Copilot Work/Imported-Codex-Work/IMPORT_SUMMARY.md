# Codex Work Import and Integrity Verification Record

- **Import Date:** 2026-10-24T13:53:52Z
- **Source Directory:** `Codex Work`
- **Destination Directory:** `GitHub Copilot Work/Imported-Codex-Work`
- **Total Files Scanned:** 3873
- **Total Files Imported and Verified:** 3668
- **Total Excluded Artifacts:** 205
- **Integrity Status:** PASSED (100% SHA-256 match)

## Exclusion Policy and Rationale

In accordance with Section 1 (Workspace Isolation), no binaries, build outputs, compilers/interpreters caches, or temporary files are imported into the durable audit workspace:
- `bin/` and `obj/` directories (.NET compilation artifacts: `.dll`, `.exe`, `.pdb`, `.cache`)
- `__pycache__/` directories (Python bytecode `.pyc`)
- Temporary scratch files (`*.tmp`, `*.up2date`)
- Total excluded files: **205**

All legitimate audit documentation, evidence ledgers, AST clone matrices, receipts, handoffs, markdown reports, and verification scripts have been preserved with their relative directory hierarchy and exact byte contents.

## Integrity Verification

Every imported file had its SHA-256 computed from `Codex Work` and re-computed from `GitHub Copilot Work/Imported-Codex-Work`.
- Number of mismatched hashes: **0**
- Full machine-readable manifest: [`IMPORT_EXCLUSIONS_AND_INTEGRITY.json`](IMPORT_EXCLUSIONS_AND_INTEGRITY.json)
