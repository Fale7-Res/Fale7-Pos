from pathlib import Path
import json
p=Path('Codex Work/Sub-Agents/Results/financial-findings.json');f=json.loads(p.read_text(encoding='utf-8-sig'))
for r in f:
 if r['id'] in ['FIN-011','FIN-012','FIN-013']:
  r['status']='CONFIRMED_STATIC_INDEPENDENT_REVIEW'
  r['independent_challenge']='printing_workflows independently confirmed static source path and searched counterevidence, saved Codex Work/Sub-Agents/Handoffs/printing-progress-assessment.md, Read-only financial challenges completed on continuation. No runtime reproduction.'
 if r['id']=='FIN-011':
  r['title']='Shift opening and purchase receipt check protected account balance before their transactions'
  path='src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs';ls=Path(path).read_text(encoding='utf-8-sig').splitlines()
  if path not in r['related_paths']:
   r['related_paths'].append(path);r['sources'].append(dict(path=path,start=73,end=97,source_excerpt='\n'.join(ls[72:97])))
  r['counterevidence_and_limits']='Normal HTTP routes have no ambient transaction. Explicit caller outer transaction could change lock duration; current endpoints provide none. Same-request replay/single open shift constraints do not serialize distinct account spend. No runtime race executed.'
 if r['id']=='FIN-012':
  r['counterevidence_and_limits']='Correction must exist within selected payroll period before run creation. Frozen lines after run creation are separate policy. Independent CreatePayrollHandler43-60 trace passes Type/Credit/Debit unchanged. No runtime writes.'
p.write_text(json.dumps(f,indent=2),encoding='utf-8')