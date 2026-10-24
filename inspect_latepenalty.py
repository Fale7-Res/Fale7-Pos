from pathlib import Path
import json,hashlib
sid='sym-00c8631835d24ba42da8db099abc45ebf54f664042ebc6dc2e1d7214acb63529'
files={
 'src/Falah.Domain/Payroll/PayrollRules.cs':[(29,52)],
 'src/Falah.Infrastructure/Employees/AttendanceHandlers.cs':[(30,115)],
 'src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs':[(80,125)],
 'src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs':[(35,95)],
 'src/Falah.Infrastructure/Settings/SettingsQueryService.cs':[(20,46)],
 'src/Falah.Infrastructure/Settings/UpdateSettingsHandler.cs':[(45,80),(98,110)],
 'tests/Falah.Domain.Tests/PayrollRulesTests.cs':[(1,50)],
 'tests/Falah.Server.IntegrationTests/AttendanceVerticalSliceTests.cs':[(50,90),(179,310)],
 'tests/Falah.Server.IntegrationTests/PayrollVerticalSliceTests.cs':[(90,112)],
}
for f,ranges in files.items():
 p=Path(f)
 if not p.exists():print('\nMISSING',f);continue
 b=p.read_bytes();ls=b.decode('utf-8-sig').splitlines();print('\nFILE',f,'SHA',hashlib.sha256(b).hexdigest())
 for a,z in ranges:
  for i in range(a,min(z,len(ls))+1):print(f'{i}: {ls[i-1]}')
rp=Path('Codex Work/Knowledge/Reviews/financial_integrity/src/Falah.Domain/Payroll/PayrollRules.cs.json')
rb=rp.read_bytes();r=json.loads(rb.decode('utf-8-sig'));print('\nRECEIPT',hashlib.sha256(rb).hexdigest())
for x in r.get('symbols',[]):
 if x.get('stable_id')==sid:print(json.dumps(x,ensure_ascii=False,indent=2))
op=Path('Codex Work/Deep-Audit/Database-and-Finance/financial-callable-evidence-quality-classification.json')
ob=op.read_bytes();o=json.loads(ob.decode('utf-8-sig'));print('\nOVERLAY',hashlib.sha256(ob).hexdigest())
for e in o['entries']:
 if e['stable_id']==sid:print(json.dumps(e,ensure_ascii=False))
