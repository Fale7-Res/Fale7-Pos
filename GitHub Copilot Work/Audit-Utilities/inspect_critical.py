import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('GitHub Copilot Work/11-Master-Findings/RECONCILED_MASTER_FINDINGS.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

f_list = data.get('findings', [])
critical_ids = ['SEC-001', 'SEC-002', 'SEC-003', 'FIN-001', 'FIN-002', 'FIN-003', 'FIN-005', 'FIN-006', 'FIN-013', 'FIN-016', 'PRT-001']

for item in f_list:
    if item.get('id') in critical_ids:
        print(f"=== {item.get('id')}: {item.get('title')} ===")
        print(f"Root Cause: {item.get('root_cause')}")
        print(f"Impact: {item.get('impact')}")
        print(f"Remediation: {item.get('remediation_plan')}")
        print(f"Required Tests: {item.get('required_tests')}")
        print(f"Files: {item.get('affected_files')}")
        print()
