import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('GitHub Copilot Work/11-Master-Findings/RECONCILED_MASTER_FINDINGS.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

findings = data.get('findings', [])

lines = []
lines.append("# Reconciled Master Findings Ledger")
lines.append("")
lines.append(f"- **Total Documented Findings:** **{len(findings)}**")
lines.append(f"- **Baseline Commit:** `{data.get('baseline_commit', 'c35816ecae49633674775dd32061c6ee92f94aaf')}`")
lines.append("- **Reconciliation Authority:** GitHub Copilot Independent Software Audit")
lines.append("- **Verification Standard:** Source-grounded, callable-traced, independent challenge verification.")
lines.append("")
lines.append("---")
lines.append("")
lines.append("## 1. Finding Dispositions Summary")
lines.append("")
lines.append("| Disposition | Count | Percentage | Description |")
lines.append("| :--- | :--- | :--- | :--- |")

disp_counts = {}
for f in findings:
    d = f.get('copilot_disposition', 'UNKNOWN')
    disp_counts[d] = disp_counts.get(d, 0) + 1

for d, c in sorted(disp_counts.items(), key=lambda x: -x[1]):
    pct = (c / len(findings)) * 100
    lines.append(f"| **{d}** | **{c}** | {pct:.1f}% | Categorized and verified disposition |")

lines.append("")
lines.append("---")
lines.append("")
lines.append("## 2. Findings Summary by Category and Severity")
lines.append("")
lines.append("| Category | High | Medium | Low | Total |")
lines.append("| :--- | :--- | :--- | :--- | :--- |")

cat_map = {}
for f in findings:
    cat = f.get('category', 'OTHER')
    sev = f.get('severity', 'MEDIUM').upper()
    if cat not in cat_map:
        cat_map[cat] = {'HIGH': 0, 'MEDIUM': 0, 'LOW': 0}
    if 'HIGH' in sev:
        cat_map[cat]['HIGH'] += 1
    elif 'LOW' in sev:
        cat_map[cat]['LOW'] += 1
    else:
        cat_map[cat]['MEDIUM'] += 1

tot_h, tot_m, tot_l = 0, 0, 0
for cat, counts in sorted(cat_map.items()):
    h, m, l = counts['HIGH'], counts['MEDIUM'], counts['LOW']
    tot_h += h; tot_m += m; tot_l += l
    lines.append(f"| **{cat}** | {h} | {m} | {l} | **{h+m+l}** |")

lines.append(f"| **TOTAL** | **{tot_h}** | **{tot_m}** | **{tot_l}** | **{len(findings)}** |")
lines.append("")
lines.append("---")
lines.append("")

# Group findings by category
categories = sorted(cat_map.keys())

for cat in categories:
    cat_findings = [f for f in findings if f.get('category') == cat]
    lines.append(f"## {cat.replace('_', ' ').title()}")
    lines.append("")
    
    for f in cat_findings:
        fid = f.get('id')
        title = f.get('title')
        sev = f.get('severity')
        disp = f.get('copilot_disposition')
        rc = f.get('root_cause')
        impact = f.get('impact')
        rem = f.get('remediation_plan')
        tests = f.get('required_tests', [])
        acc = f.get('acceptance_criteria', [])
        refs = f.get('source_references', [])
        
        lines.append(f"### {fid}: {title}")
        lines.append(f"- **Severity:** `{sev}` | **Disposition:** `{disp}`")
        
        if refs:
            ref_strs = []
            for r in refs:
                p = r.get('path', '')
                line_info = ""
                if r.get('lines'):
                    line_info = f" (lines {r['lines']})"
                ref_strs.append(f"`{p}`{line_info}")
            lines.append(f"- **Source References:** {', '.join(ref_strs)}")
            
        lines.append(f"- **Root Cause:** {rc}")
        lines.append(f"- **Impact:** {impact}")
        lines.append(f"- **Remediation Plan:** {rem}")
        
        if tests:
            lines.append("- **Required Regression Tests:**")
            for t in tests:
                lines.append(f"  - {t}")
                
        if acc:
            lines.append("- **Acceptance Criteria:**")
            if isinstance(acc, list):
                for a in acc:
                    lines.append(f"  - {a}")
            else:
                lines.append(f"  - {acc}")
                
        lines.append("")
        lines.append("---")
        lines.append("")

lines.append("Complete structured database: [`RECONCILED_MASTER_FINDINGS.json`](RECONCILED_MASTER_FINDINGS.json)")

with open('GitHub Copilot Work/11-Master-Findings/RECONCILED_MASTER_FINDINGS.md', 'w', encoding='utf-8') as f:
    f.write('\n'.join(lines))

print(f"Successfully generated RECONCILED_MASTER_FINDINGS.md with all {len(findings)} findings.")
