import json
import sys
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')

with open('GitHub Copilot Work/11-Master-Findings/RECONCILED_MASTER_FINDINGS.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

findings = data['findings']

lines = []
lines.append("# Codex Findings to Copilot Review Outcomes — Reconciliation Index\n")
lines.append("This document tracks every original Codex finding, its source references, prior status, and independent Copilot audit outcome.\n")
lines.append("| ID | Severity | Title | Codex Prior Status | Copilot Disposition | Files Verified |")
lines.append("| :--- | :--- | :--- | :--- | :--- | :--- |")

for item in findings:
    fid = item['id']
    sev = item['severity']
    title = item['title'].replace('|', '/')
    prior = item['codex_prior_status'] or 'REPORTED'
    disp = item['copilot_disposition']
    files_count = len(item['affected_files'])
    lines.append(f"| **{fid}** | `{sev}` | {title} | `{prior}` | **{disp}** | {files_count} files |")

out_path = Path("GitHub Copilot Work/11-Master-Findings/FINDINGS_RECONCILIATION_INDEX.md")
with open(out_path, 'w', encoding='utf-8') as f:
    f.write('\n'.join(lines))

print(f"Saved {out_path} with {len(findings)} index entries.")
