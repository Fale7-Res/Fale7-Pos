import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
WORKSPACE = os.path.join(REPO_ROOT, 'GitHub Copilot Work')
HANDOFFS_DIR = os.path.join(WORKSPACE, 'Checkpoints', 'AGENT_HANDOFFS')

def sync_reports():
    mappings = [
        {
            "handoff": "FINANCIAL_AUDITOR_HANDOFF.md",
            "targets": [
                os.path.join(WORKSPACE, "03-Financial-Integrity", "DATABASE_AND_FINANCIAL_INTEGRITY.md"),
                os.path.join(WORKSPACE, "03-Financial-Integrity", "FINANCIAL_WORKFLOWS_DEEP_AUDIT.md")
            ]
        },
        {
            "handoff": "SECURITY_AUDITOR_HANDOFF.md",
            "targets": [
                os.path.join(WORKSPACE, "05-Security", "SECURITY_AND_BACKDOOR_AUDIT.md"),
                os.path.join(WORKSPACE, "05-Security", "SECURITY_AUDIT.md")
            ]
        },
        {
            "handoff": "PRINTING_WORKFLOW_AUDITOR_HANDOFF.md",
            "targets": [
                os.path.join(WORKSPACE, "06-Printing", "PRINTING_RELIABILITY_AND_PREVIEW.md")
            ]
        },
        {
            "handoff": "MIRRORED_CODE_AUDITOR_HANDOFF.md",
            "targets": [
                os.path.join(WORKSPACE, "04-Mirrored-Code-and-Architecture", "CODE_DUPLICATION_AND_ARCHITECTURE.md"),
                os.path.join(WORKSPACE, "04-Mirrored-Code-and-Architecture", "MIRROR_RECONCILIATION.md")
            ]
        },
        {
            "handoff": "UI_MAINTAINABILITY_AUDITOR_HANDOFF.md",
            "targets": [
                os.path.join(WORKSPACE, "07-UI-UX", "UI_UX_VISUAL_AUDIT.md")
            ]
        }
    ]

    for m in mappings:
        hpath = os.path.join(HANDOFFS_DIR, m["handoff"])
        if not os.path.exists(hpath):
            print(f"Warning: Handoff file not found: {hpath}")
            continue

        with open(hpath, 'r', encoding='utf-8') as f:
            content = f.read()

        for target in m["targets"]:
            os.makedirs(os.path.dirname(target), exist_ok=True)
            with open(target, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Updated {target} ({len(content)} bytes) from {m['handoff']}.")

if __name__ == '__main__':
    sync_reports()
    print("SUCCESS: Synchronized all domain master reports with specialist agent handoffs.")
