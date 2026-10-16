from pathlib import Path
import json,re
p=Path('Codex Work/Deep-Audit/Database-and-Finance/financial-summary.md');t=p.read_text(encoding='utf-8-sig');fs=json.loads(Path('Codex Work/Sub-Agents/Results/financial-findings.json').read_text(encoding='utf-8-sig'))
a=t.index('- **FIN-001');b=t.index('[Detailed findings',a)
t=t[:a]+''.join(f"- **{f['id']} {f['severity']}**: {f['title']} — {f['status']}\n" for f in fs)+'\n'+t[b:]
t=re.sub(r'\nAdditional findings:.*','',t,flags=re.S);p.write_text(t,encoding='utf-8');print('Summary synchronized with 13 canonical statuses')