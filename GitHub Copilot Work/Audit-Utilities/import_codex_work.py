import os
import sys
import hashlib
import shutil
import json
import time
from pathlib import Path

def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest()

def main():
    src_root = Path('Codex Work')
    dst_root = Path('GitHub Copilot Work/Imported-Codex-Work')
    dst_root.mkdir(parents=True, exist_ok=True)

    def should_exclude(p: Path):
        parts = p.parts
        if any(part in ['bin', 'obj', '__pycache__'] for part in parts):
            return True, 'Build artifact or compiler/interpreter cache directory'
        suffix = p.suffix.lower()
        if suffix in ['.dll', '.exe', '.pdb', '.cache', '.pyc', '.tmp', '.up2date']:
            return True, f'Binary, temporary, or cache artifact ({suffix})'
        return False, ''

    all_files = list(src_root.rglob('*'))
    total_scanned = 0
    excluded_records = []
    imported_records = []
    integrity_failures = []

    print(f"Starting import scan of {src_root}...")
    start_time = time.time()

    for p in all_files:
        if not p.is_file():
            continue
        total_scanned += 1
        rel = p.relative_to(src_root)
        rel_str = str(rel).replace('\\', '/')
        excl, reason = should_exclude(p)

        if excl:
            excluded_records.append({
                "source_path": f"Codex Work/{rel_str}",
                "size_bytes": p.stat().st_size,
                "reason": reason
            })
            continue

        # Valid audit evidence - copy and verify
        src_sha = sha256_file(p)
        dst_path = dst_root / rel
        dst_path.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(p, dst_path)
        dst_sha = sha256_file(dst_path)

        if src_sha != dst_sha:
            integrity_failures.append({
                "source_path": f"Codex Work/{rel_str}",
                "dest_path": str(dst_path).replace('\\', '/'),
                "src_sha256": src_sha,
                "dst_sha256": dst_sha
            })
        else:
            imported_records.append({
                "source_path": f"Codex Work/{rel_str}",
                "dest_path": f"GitHub Copilot Work/Imported-Codex-Work/{rel_str}",
                "size_bytes": p.stat().st_size,
                "sha256": src_sha
            })

    elapsed = time.time() - start_time
    print(f"Import completed in {elapsed:.2f} seconds.")
    print(f"Total scanned files: {total_scanned}")
    print(f"Successfully imported: {len(imported_records)}")
    print(f"Excluded: {len(excluded_records)}")
    print(f"Integrity failures: {len(integrity_failures)}")

    # Write JSON manifest
    manifest_data = {
        "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "source_root": "Codex Work",
        "destination_root": "GitHub Copilot Work/Imported-Codex-Work",
        "total_scanned": total_scanned,
        "total_imported": len(imported_records),
        "total_excluded": len(excluded_records),
        "total_integrity_failures": len(integrity_failures),
        "integrity_verified": len(integrity_failures) == 0,
        "integrity_failures": integrity_failures,
        "exclusions": excluded_records,
        "imported_files": imported_records
    }

    manifest_path = dst_root / "IMPORT_EXCLUSIONS_AND_INTEGRITY.json"
    with open(manifest_path, 'w', encoding='utf-8') as f:
        json.dump(manifest_data, f, indent=2, ensure_ascii=False)
    print(f"Saved manifest to {manifest_path}")

    # Write Markdown summary
    summary_md = f"""# Codex Work Import and Integrity Verification Record

- **Import Date:** {manifest_data['timestamp']}
- **Source Directory:** `Codex Work`
- **Destination Directory:** `GitHub Copilot Work/Imported-Codex-Work`
- **Total Files Scanned:** {total_scanned}
- **Total Files Imported and Verified:** {len(imported_records)}
- **Total Excluded Artifacts:** {len(excluded_records)}
- **Integrity Status:** {'PASSED (100% SHA-256 match)' if len(integrity_failures) == 0 else 'FAILED'}

## Exclusion Policy and Rationale

In accordance with Section 1 (Workspace Isolation), no binaries, build outputs, compilers/interpreters caches, or temporary files are imported into the durable audit workspace:
- `bin/` and `obj/` directories (.NET compilation artifacts: `.dll`, `.exe`, `.pdb`, `.cache`)
- `__pycache__/` directories (Python bytecode `.pyc`)
- Temporary scratch files (`*.tmp`, `*.up2date`)
- Total excluded files: **{len(excluded_records)}**

All legitimate audit documentation, evidence ledgers, AST clone matrices, receipts, handoffs, markdown reports, and verification scripts have been preserved with their relative directory hierarchy and exact byte contents.

## Integrity Verification

Every imported file had its SHA-256 computed from `Codex Work` and re-computed from `GitHub Copilot Work/Imported-Codex-Work`.
- Number of mismatched hashes: **{len(integrity_failures)}**
- Full machine-readable manifest: [`IMPORT_EXCLUSIONS_AND_INTEGRITY.json`](IMPORT_EXCLUSIONS_AND_INTEGRITY.json)
"""
    summary_path = dst_root / "IMPORT_SUMMARY.md"
    with open(summary_path, 'w', encoding='utf-8') as f:
        f.write(summary_md)
    print(f"Saved summary to {summary_path}")

if __name__ == "__main__":
    main()
