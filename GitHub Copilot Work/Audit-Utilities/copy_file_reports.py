import shutil
from pathlib import Path

src_reports = Path("GitHub Copilot Work/Imported-Codex-Work/Deep-Audit/File-Reports")
dst_reports = Path("GitHub Copilot Work/FILE_REPORTS")
dst_reports.mkdir(parents=True, exist_ok=True)

copied = 0
for p in src_reports.rglob("*.md"):
    if p.is_file():
        rel = p.relative_to(src_reports)
        target = dst_reports / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(p, target)
        copied += 1

print(f"Copied {copied} file reports to {dst_reports}.")
