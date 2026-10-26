import json
import os

def build_fix_specs_markdown():
    with open('GitHub Copilot Work/Final-Handoff/FIX_IMPLEMENTATION_SPECIFICATIONS.json', 'r', encoding='utf-8') as f:
        data = json.load(f)

    specs = data.get('specifications', [])

    # Group by priority
    p0 = [s for s in specs if s.get('priority') == 'P0_CRITICAL_STOP_SHIP']
    p1 = [s for s in specs if s.get('priority') == 'P1_HIGH_CORE_OPERATIONS']
    p2 = [s for s in specs if s.get('priority') == 'P2_STABILITY_AND_MAINTAINABILITY']

    md = []
    md.append("# Detailed Implementation Specifications & Remediation Blueprints")
    md.append("")
    md.append(f"- **Total Actionable Specifications:** **{len(specs)}**")
    md.append(f"- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`")
    md.append("- **Audit Mode:** Pure Forensic Blueprint Specification (Zero Application Code Modified)")
    md.append("- **Target Audience:** Implementation Engineers, System Architects & QA Automation Specialists")
    md.append("- **Specification Standard:** Atomic, test-driven, source-referenced blueprints with exact acceptance gates.")
    md.append("")
    md.append("---")
    md.append("")
    md.append("## Executive Implementation Matrix")
    md.append("")
    md.append("| Priority Level | Scope & Operational Risk | Count | Immediate Action Required |")
    md.append("| :--- | :--- | :---: | :--- |")
    md.append(f"| **P0_CRITICAL_STOP_SHIP** | Financial loss, hard crashes, till lockout, corrupted ledger entries | **{len(p0)}** | Immediate remediation before any production release |")
    md.append(f"| **P1_HIGH_CORE_OPERATIONS** | Daily settlement errors, shift closeout races, security bypasses, UI lockouts | **{len(p1)}** | Stage 1 Sprint: core transaction and workflow integrity |")
    md.append(f"| **P2_STABILITY_AND_MAINTAINABILITY** | Memory bloat, N+1 scans, background async errors, rounding drift | **{len(p2)}** | Stage 2 Sprint: performance optimization and code health |")
    md.append(f"| **TOTAL** | **Comprehensive Engineering Backlog** | **{len(specs)}** | **Full Audit Resolution** |")
    md.append("")
    md.append("---")
    md.append("")

    def format_spec(s):
        fid = s.get('finding_id', '')
        title = s.get('title', '')
        sev = s.get('severity', '')
        cat = s.get('category', '')
        prio = s.get('priority', '')
        files = s.get('affected_files', [])
        rc = s.get('root_cause', '')
        impact = s.get('impact', '')
        bp = s.get('implementation_blueprint', '')
        tests = s.get('required_regression_tests', [])
        acc = s.get('acceptance_criteria', [])

        lines = []
        lines.append(f"### Spec: {fid} — {title}")
        lines.append(f"- **Severity:** `{sev}` | **Category:** `{cat}` | **Priority:** `{prio}`")
        
        file_links = []
        for f in files:
            norm_f = f.replace('\\', '/')
            file_links.append(f"[`{norm_f}`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/{norm_f})")
        lines.append(f"- **Target Files:** {', '.join(file_links)}")
        lines.append("")
        lines.append(f"**Root Cause Diagnosis:**  \n{rc}")
        lines.append("")
        lines.append(f"**Operational Impact:**  \n{impact}")
        lines.append("")
        
        bp_str = bp
        if isinstance(bp, list):
            bp_str = "\n".join(f"- {x}" for x in bp)
        lines.append(f"**Engineering Implementation Blueprint:**  \n{bp_str}")
        lines.append("")
        
        lines.append("**Required Verification & Regression Tests:**")
        if isinstance(tests, list):
            for t in tests:
                lines.append(f"- {t}")
        elif isinstance(tests, str):
            lines.append(f"- {tests}")
        lines.append("")

        lines.append("**Acceptance Criteria:**")
        if isinstance(acc, list):
            for a in acc:
                lines.append(f"- {a}")
        elif isinstance(acc, str):
            lines.append(f"- {acc}")
        lines.append("")
        lines.append("---")
        lines.append("")
        return "\n".join(lines)

    # Section P0
    md.append(f"## 1. P0 CRITICAL STOP SHIP ({len(p0)} Specifications)")
    md.append("")
    md.append("> [!CAUTION]")
    md.append("> **STOP-SHIP MANDATE:** Deploying the application without remediating these 9 critical defects will result in direct till drawer cash discrepancies, split payment lockouts, silent supplier overpayments, or unrecoverable journal collisions.")
    md.append("")
    for s in p0:
        md.append(format_spec(s))

    # Section P1
    md.append(f"## 2. P1 HIGH CORE OPERATIONS ({len(p1)} Specifications)")
    md.append("")
    md.append("> [!IMPORTANT]")
    md.append("> **CORE OPERATIONS MANDATE:** These 35 specifications resolve critical daily operational workflows, including daylight saving time calendar boundary crashes (Cairo DST spring-forward), payroll cash advance recovery omissions, authorization token validation, and shift closing races.")
    md.append("")
    for s in p1:
        md.append(format_spec(s))

    # Section P2
    md.append(f"## 3. P2 STABILITY AND MAINTAINABILITY ({len(p2)} Specifications)")
    md.append("")
    md.append("> [!NOTE]")
    md.append("> **STABILITY & PERFORMANCE MANDATE:** These 56 specifications address memory-intensive table scans, missing database filtered indexes, arithmetic overflow boundary protections, rounding drift on yield-ratio recipe lines, and UI responsiveness.")
    md.append("")
    for s in p2:
        md.append(format_spec(s))

    out_path = 'GitHub Copilot Work/Final-Handoff/FIX_IMPLEMENTATION_SPECIFICATIONS.md'
    with open(out_path, 'w', encoding='utf-8') as out_f:
        out_f.write("\n".join(md))

    print(f"Successfully generated {out_path} with {len(specs)} specifications ({len(p0)} P0, {len(p1)} P1, {len(p2)} P2)!")

if __name__ == '__main__':
    build_fix_specs_markdown()
