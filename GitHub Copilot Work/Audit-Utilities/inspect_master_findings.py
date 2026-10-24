import json

with open('Codex Work/Deep-Audit/MASTER_FINDINGS.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

findings = data.get('findings', [])
print(f"Total findings in MASTER_FINDINGS.json: {len(findings)}")
categories = {}
severities = {}
for item in findings:
    fid = item.get('id', 'unknown')
    cat = item.get('category', item.get('functional_area', 'unknown'))
    sev = item.get('severity', 'unknown')
    categories[cat] = categories.get(cat, 0) + 1
    severities[sev] = severities.get(sev, 0) + 1

print("\nSeverities:")
for s, c in severities.items():
    print(f"  {s}: {c}")

print("\nCategories:")
for k, c in categories.items():
    print(f"  {k}: {c}")

print("\nAll findings by ID and Title:")
for item in findings:
    print(f"  {item.get('id')}: [{item.get('severity')}] {item.get('title')}")
