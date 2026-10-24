import sys
from pathlib import Path

def main():
    base = Path('GitHub Copilot Work')
    required_19 = [
        ('1. EXECUTIVE_SUMMARY.md', base / 'EXECUTIVE_SUMMARY.md'),
        ('2. ARCHITECTURE_AND_SOURCE_MAP.md', base / 'ARCHITECTURE_AND_SOURCE_MAP.md'),
        ('3. SOURCE_INVENTORY.csv', base / 'SOURCE_INVENTORY.csv'),
        ('4. FUNCTION_INDEX.json', base / 'FUNCTION_INDEX.json'),
        ('5. SECURITY_AND_BACKDOOR_AUDIT.md', base / '05-Security/SECURITY_AND_BACKDOOR_AUDIT.md'),
        ('6. DEPENDENCY_SECURITY_AND_SBOM.md', base / '08-Performance-and-Dependencies/DEPENDENCY_SECURITY_AND_SBOM.md'),
        ('7. PICKUP_AND_DELIVERY_WORKFLOW_AUDIT.md', base / '09-Pickup-and-Delivery/PICKUP_AND_DELIVERY_WORKFLOW_AUDIT.md'),
        ('8. PRINTING_RELIABILITY_AND_PREVIEW.md', base / '06-Printing/PRINTING_RELIABILITY_AND_PREVIEW.md'),
        ('9. UI_UX_VISUAL_AUDIT.md', base / '07-UI-UX/UI_UX_VISUAL_AUDIT.md'),
        ('10. DATABASE_AND_FINANCIAL_INTEGRITY.md', base / '03-Financial-Integrity/DATABASE_AND_FINANCIAL_INTEGRITY.md'),
        ('11. PERFORMANCE_AND_MINIMUM_HARDWARE.md', base / '08-Performance-and-Dependencies/PERFORMANCE_AND_MINIMUM_HARDWARE.md'),
        ('12. CODE_DUPLICATION_AND_ARCHITECTURE.md', base / '04-Mirrored-Code-and-Architecture/CODE_DUPLICATION_AND_ARCHITECTURE.md'),
        ('13. TEST_COVERAGE_AND_MISSING_TESTS.md', base / '10-Test-Coverage/TEST_COVERAGE_AND_MISSING_TESTS.md'),
        ('14. IMMEDIATE_FIXES_AND_FEATURES.md', base / '12-Implementation-Handoff/IMMEDIATE_FIXES_AND_FEATURES.md'),
        ('15. FUTURE_IMPROVEMENTS_AND_FEATURES.md', base / '12-Implementation-Handoff/FUTURE_IMPROVEMENTS_AND_FEATURES.md'),
        ('16. MASTER_FINDINGS.json', base / 'MASTER_FINDINGS.json'),
        ('17. AUDIT_COVERAGE.json', base / 'AUDIT_COVERAGE.json'),
        ('18. IMPLEMENTATION_ROADMAP.md', base / '12-Implementation-Handoff/IMPLEMENTATION_ROADMAP.md'),
        ('19. FILE_REPORTS/', base / 'FILE_REPORTS')
    ]

    print("Verifying the 19 Required Deliverables in GitHub Copilot Work:")
    all_ok = True
    for name, p in required_19:
        if p.exists():
            if p.is_dir():
                count = len(list(p.rglob('*.md')))
                print(f"  [PASS] {name} -> Directory containing {count} markdown file reports")
            else:
                size = p.stat().st_size
                print(f"  [PASS] {name} -> File size: {size:,} bytes")
        else:
            print(f"  [FAIL] {name} -> Path does not exist: {p}")
            all_ok = False

    print("\nVerifying Handoff & Quality Files:")
    handoff_files = [
        base / 'FINAL_AUDIT_STATUS.md',
        base / 'README.md',
        base / '02-Callable-Inventory-and-Reviews/CALLABLE_REVIEW_REGISTRY.json',
        base / '11-Master-Findings/RECONCILED_MASTER_FINDINGS.json',
        base / '11-Master-Findings/FINDINGS_RECONCILIATION_INDEX.md',
        base / '12-Implementation-Handoff/COMPLETE_FIX_IMPLEMENTATION_HANDOFF.md',
        base / '13-Independent-Verification/CODEX_VERIFICATION_MANIFEST.json',
        base / '13-Independent-Verification/CODEX_VERIFICATION_INSTRUCTIONS.md',
        base / 'Checkpoints/COMPLETION_MATRIX.json',
        base / 'Checkpoints/CALLABLE_COVERAGE.json',
        base / 'Checkpoints/CURRENT_REVIEW_STATE.json',
        base / 'Checkpoints/NEXT_ACTION.md'
    ]
    for hp in handoff_files:
        if hp.exists():
            print(f"  [PASS] {hp.name} -> Size: {hp.stat().st_size:,} bytes")
        else:
            print(f"  [FAIL] {hp.name} -> MISSING!")
            all_ok = False

    if all_ok:
        print("\n>>> ALL QUALITY GATES PASSED (100% OF 19 DELIVERABLES AND HANDOFF FILES PRESENT) <<<")
    else:
        print("\n>>> QUALITY GATES FAILED <<<")

if __name__ == '__main__':
    main()
