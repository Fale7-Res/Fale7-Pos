import os
import sys
import json
import hashlib

sys.stdout.reconfigure(encoding='utf-8')

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
WORKSPACE = os.path.join(REPO_ROOT, 'GitHub Copilot Work')
REGISTRY_JSON = os.path.join(WORKSPACE, '02-Callable-Inventory-and-Reviews', 'CALLABLE_REVIEW_REGISTRY.json')
QUEUE_JSON = os.path.join(WORKSPACE, 'Checkpoints', 'UNFINISHED_CALLABLE_QUEUE.json')
QUEUE_SUMMARY_MD = os.path.join(WORKSPACE, 'Checkpoints', 'UNFINISHED_CALLABLE_QUEUE.md')

def get_file_sha256(path):
    full = os.path.join(REPO_ROOT, path)
    if not os.path.exists(full):
        return "FILE_NOT_FOUND"
    h = hashlib.sha256()
    with open(full, 'rb') as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest()

def assign_specialist(path):
    p = path.replace('\\', '/')
    if p.startswith('src/Falah.Server') or 'AuthenticationMiddleware' in p or '/Users/' in p or '/Security/' in p:
        return "security-auditor"
    if any(k in p for k in ['/Orders/', '/Treasury/', '/Shifts/', '/Purchasing/', '/Payroll/', '/Persistence/', '/CashDrawer/', '/Delivery/', '/Employees/', '/Pricing/']):
        return "financial-auditor"
    if 'Cashier' in p or 'CallCenter' in p:
        if any(k in p for k in ['ViewModel', 'Service']):
            return "mirrored-code-auditor"
        return "ui-maintainability-auditor"
    if 'Print' in p:
        return "printing-workflow-auditor"
    return "architecture-coordinator"

def main():
    print("Building Unfinished Callable Review Queue...")

    with open(REGISTRY_JSON, 'r', encoding='utf-8') as f:
        registry = json.load(f)

    records = registry.get('records', [])
    sha_cache = {}

    queue_entries = []
    specialist_counts = {}
    status_counts = {}

    for r in records:
        path = r.get('path', '')
        if path not in sha_cache:
            sha_cache[path] = get_file_sha256(path)
        fsha = sha_cache[path]

        cid = r.get('id', '')
        sym = r.get('symbol', r.get('name', ''))
        lines = r.get('lines', [0, 0])
        status = r.get('status', 'PRIOR_REASONING_UNVERIFIED')
        specialist = assign_specialist(path)

        verdict = {
            'GENERATED_OR_TRIVIAL_JUSTIFIED': 'JUSTIFIED_TRIVIAL',
            'INDEPENDENTLY_VERIFIED': 'ACCEPTED_VERIFIED',
            'INDEXED': 'INDEXED_TEST_OR_TOOL',
            'SUBSTANTIVE_SOURCE_REVIEWED': 'ACCEPTED_SUBSTANTIVE',
            'PRIOR_REASONING_UNVERIFIED': 'PENDING_SUBSTANTIVE_REVIEW'
        }.get(status, 'PENDING_SUBSTANTIVE_REVIEW')

        entry = {
            "callable_id": cid,
            "symbol": sym,
            "kind": r.get('kind', ''),
            "source_path": path,
            "source_file_sha256": fsha,
            "line_range": lines,
            "assigned_specialist": specialist,
            "current_status": status,
            "acceptance_verdict": verdict,
            "review_artifact": f"GitHub Copilot Work/02-Callable-Inventory-and-Reviews/File-Reviews/{path}.md"
        }
        queue_entries.append(entry)

        status_counts[status] = status_counts.get(status, 0) + 1
        if verdict == 'PENDING_SUBSTANTIVE_REVIEW':
            specialist_counts[specialist] = specialist_counts.get(specialist, 0) + 1

    queue_data = {
        "timestamp_utc": "2026-10-24T19:50:00Z",
        "total_callables": len(queue_entries),
        "status_breakdown": status_counts,
        "pending_by_specialist": specialist_counts,
        "total_pending_review": sum(specialist_counts.values()),
        "callables": queue_entries
    }

    print(f"Writing {len(queue_entries)} entries to {QUEUE_JSON}...")
    with open(QUEUE_JSON, 'w', encoding='utf-8') as f:
        json.dump(queue_data, f, indent=2)

    # Write Markdown Summary
    md_lines = [
        "# Central Unfinished Callable Review Queue",
        "",
        f"- **Total Indexed Callables:** **{len(queue_entries)}**",
        f"- **Total Pending Substantive Review:** **{queue_data['total_pending_review']}**",
        "- **Date & UTC:** `2026-10-24T19:50:00Z`",
        "",
        "---",
        "",
        "## 1. Pending Substantive Reviews by Specialist Worker",
        "",
        "| Assigned Specialist Worker | Pending Callables | Priority Queue Domain |",
        "| :--- | :--- | :--- |",
    ]
    for spec, cnt in sorted(specialist_counts.items(), key=lambda x: -x[1]):
        domain = {
            "security-auditor": "Security, Middleware, Session Tokens, User Roles",
            "financial-auditor": "Orders, Checkout, Cash Drawer, Shifts, Treasury, Purchasing, Payroll",
            "mirrored-code-auditor": "WPF / WinUI ViewModels, Services, Parity & Validation Divergence",
            "printing-workflow-auditor": "Print Agents, Win32 Spooler, Bitmap Rasterizer, ESC/POS",
            "ui-maintainability-auditor": "UI Views, Ergonomics, Virtualization, Async Void Handlers",
            "architecture-coordinator": "Domain Entities, Application Requests, Cross-Cutting Services"
        }.get(spec, "Cross-cutting Architecture")
        md_lines.append(f"| **`{spec}`** | **{cnt}** | {domain} |")

    md_lines.append("")
    md_lines.append("---")
    md_lines.append("")
    md_lines.append("## 2. Review Execution Standard")
    md_lines.append("")
    md_lines.append("To promote a callable from `PRIOR_REASONING_UNVERIFIED` to `SUBSTANTIVE_SOURCE_REVIEWED`:")
    md_lines.append("1. The callable must receive dedicated source-grounded analysis in its corresponding `File-Reviews/<path>.md`.")
    md_lines.append("2. Behavior, trust boundaries, state mutations, error paths, and concurrency implications must be explicitly stated.")
    md_lines.append("3. For any defect or vulnerability, verbatim original code excerpts with exact line numbers and SHA-256 must be provided.")
    md_lines.append("4. No mass-generated generic templates are permitted.")
    md_lines.append("")

    with open(QUEUE_SUMMARY_MD, 'w', encoding='utf-8') as f:
        f.write('\n'.join(md_lines))

    print("SUCCESS: Unfinished review queue built and saved.")

if __name__ == '__main__':
    main()
