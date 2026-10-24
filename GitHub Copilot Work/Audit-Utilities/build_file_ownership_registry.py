import csv
import json
import os
import hashlib
import sys

sys.stdout.reconfigure(encoding='utf-8')

# Read SOURCE_INVENTORY.csv
inventory_path = 'GitHub Copilot Work/SOURCE_INVENTORY.csv'
with open(inventory_path, 'r', encoding='utf-8') as f:
    rows = list(csv.DictReader(f))

# Read CALLABLE_REVIEW_REGISTRY.json to get callable counts per file
callable_registry_path = 'GitHub Copilot Work/02-Callable-Inventory-and-Reviews/CALLABLE_REVIEW_REGISTRY.json'
with open(callable_registry_path, 'r', encoding='utf-8') as f:
    callables = json.load(f).get('records', [])

file_callable_map = {}
for c in callables:
    p = c.get('path', '').replace('\\', '/')
    file_callable_map.setdefault(p, []).append(c.get('id'))

def assign_agent(path, project):
    p_lower = path.lower()
    
    # 1. Security & Auth
    if any(k in p_lower for k in ['auth', 'pin', 'permission', 'security', 'session', 'user', 'employee']):
        return 'security-auditor'
    if 'program.cs' in p_lower and 'server' in p_lower:
        return 'security-auditor' # Server endpoints & middleware
        
    # 2. Financial & Treasury
    if any(k in p_lower for k in ['order', 'checkout', 'refund', 'payment', 'treasury', 'shift', 'purchase', 'expense', 'report', 'journal', 'cash', 'money', 'businessday', 'sales']):
        return 'financial-auditor'
        
    # 3. Printing & Hardware & Fulfillment
    if any(k in p_lower for k in ['print', 'ticket', 'receipt', 'delivery', 'pickup', 'spooler', 'escpos', 'raster']):
        return 'printing-workflow-auditor'
        
    # 4. Mirrored Code & Parity
    if 'falah.client.cashier/' in p_lower:
        return 'mirrored-code-auditor' # Legacy WPF client
    if any(k in p_lower for k in ['converter', 'parity', 'duplicate', 'mirror']):
        return 'mirrored-code-auditor'
        
    # 5. UI/UX & Maintainability
    if any(k in p_lower for k in ['.xaml', 'viewmodel', 'control', 'shell', 'card', 'theme', 'brush', 'appheader']):
        return 'ui-maintainability-auditor'
    if 'winui' in p_lower and ('view' in p_lower or 'asset' in p_lower or 'strings' in p_lower):
        return 'ui-maintainability-auditor'
        
    # 6. Tests & Infrastructure fallback
    if 'test' in p_lower:
        if any(k in p_lower for k in ['auth', 'user', 'employee']):
            return 'security-auditor'
        if any(k in p_lower for k in ['order', 'checkout', 'refund', 'payment', 'treasury', 'shift', 'purchase', 'financial']):
            return 'financial-auditor'
        if any(k in p_lower for k in ['print', 'delivery']):
            return 'printing-workflow-auditor'
        if any(k in p_lower for k in ['client', 'ui']):
            return 'ui-maintainability-auditor'
        return 'financial-auditor'
        
    # Default architecture coordinator
    return 'architecture-coordinator'

ownership_registry = {
    "timestamp_utc": "2026-10-24T16:20:00Z",
    "baseline_commit": "c35816ecae49633674775dd32061c6ee92f94aaf",
    "total_files": len(rows),
    "total_callables": len(callables),
    "agent_file_distribution": {},
    "agent_callable_distribution": {},
    "files": []
}

agent_file_counts = {}
agent_callable_counts = {}

for r in rows:
    rel_path = r['path'].replace('\\', '/')
    project = r['project']
    sha256 = r['sha256']
    baseline = r['baseline']
    
    agent = assign_agent(rel_path, project)
    c_list = file_callable_map.get(rel_path, [])
    c_count = len(c_list)
    
    agent_file_counts[agent] = agent_file_counts.get(agent, 0) + 1
    agent_callable_counts[agent] = agent_callable_counts.get(agent, 0) + c_count
    
    # Check if file review exists
    review_artifact = f"GitHub Copilot Work/FILE_REPORTS/{rel_path}.md"
    review_exists = os.path.exists(review_artifact)
    
    ownership_registry["files"].append({
        "path": rel_path,
        "project": project,
        "sha256": sha256,
        "baseline_commit": baseline,
        "callable_count": c_count,
        "callable_ids": c_list,
        "primary_agent": agent,
        "review_status": "REVIEWED" if review_exists else "PENDING",
        "review_artifact": review_artifact if review_exists else None
    })

ownership_registry["agent_file_distribution"] = agent_file_counts
ownership_registry["agent_callable_distribution"] = agent_callable_counts

with open('GitHub Copilot Work/01-Source-Baseline/COMPLETE_FILE_OWNERSHIP_REGISTRY.json', 'w', encoding='utf-8') as f:
    json.dump(ownership_registry, f, indent=2, ensure_ascii=False)

print("Generated COMPLETE_FILE_OWNERSHIP_REGISTRY.json")
print("Agent file distribution:")
for a, count in sorted(agent_file_counts.items(), key=lambda x: -x[1]):
    print(f"  {a}: {count} files ({agent_callable_counts.get(a, 0)} callables)")
