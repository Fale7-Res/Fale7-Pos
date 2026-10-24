from pathlib import Path
import sys,json
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()))
import knowledge_store as k
p=k.WORK/'Deep-Audit/Database-and-Finance/DATABASE_AND_FINANCIAL_INTEGRITY.md';s=p.read_text(encoding='utf8')
needle='[Remediation regression risks](REMEDIATION_REGRESSION_RISKS.md)'
if needle not in s:
 h=k.WORK/'Knowledge/History/financial_integrity/financial-report'/('before-regression-link-'+k.sha256(s.encode())[:16]+'.json')
 k.atomic_json(h,dict(original_document=str(p.relative_to(k.WORK)),original_base=str(p.parent.relative_to(k.WORK)),snapshot_markdown=s,captured_at_utc=k.now()))
 s+='\n## Correction-design regression constraints\n\n'+needle+' records parent-reviewed preservation and regression requirements for the26 existing financial findings. This is engineering assessment; no fixes or extra runtime tests were performed. It does not close the remaining source/callable review gates.\n'
 k.atomic_text(p,s)
print('Finance report current and remediation constraints linked')
