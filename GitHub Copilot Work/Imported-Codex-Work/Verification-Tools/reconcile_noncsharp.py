"""AST-based supplementary callable identities. Does not execute/import audited scripts."""
from knowledge_store import *
import ast

ps=json.loads((WORK/'Local-Verification/powershell-ast-temporary.json').read_text(encoding='utf-8-sig'))
legacy=json.loads((WORK/'Sub-Agents/Results/printing-noncsharp-function-gap.json').read_text(encoding='utf8'))
legacy_by={(r['path'],r['name']):r for r in legacy['functions']}
notes={
 ('tools/install-falah-service.ps1','Assert-Administrator'):'Independently read36-42. Windows identity/principal Administrator test throws on insufficient role; it does not elevate or authorize changing an arbitrary named service. Reading this function did not execute it.',
 ('tools/install-falah-service.ps1','Stop-AndRemoveService'):'Independently read44-58. Missing service returns false; otherwise force stop, wait30seconds, sc.exe delete without checking native exit, then sleep2seconds and true. Return true certifies attempted removal, not confirmed deletion. No service call executed.',
 ('tools/pre-demo-check.ps1','Add-Result'):'Independently read28-30. Builds caller-supplied PASS/FAIL object and appends captured list; it does not determine test truth. PowerShell subexpression if is not a separate callable declaration. Arbitrary Detail/Fix can carry sensitive output if script is run.',
 ('tools/pre-demo-check.ps1','Invoke-Sql'):'Independently read31-34. sqlcmd uses captured instance and integrated identity with arbitrary Query, no explicit database or exit-code check. Merged stderr/stdout is filtered and joined; read-only behavior depends on caller queries, not enforced by helper. Not invoked.',
 ('tools/repro-native-crash.ps1','Get-CrashCount'):'Independently read25-32 and call sites. Event log filter by Application Error and start time then wildcard image-name substring count; denied query with SilentlyContinue can look like zero crashes. Event provider/name matching is weaker than exact process correlation. No event log queried.',
 ('tools/xaml-audit.py','load_defined'):'Read42-53. Reads only immediate Styles/*.xaml and App.xaml; regex requires exact double-quoted Style key-before-TargetType layout. Last duplicate key overwrites target; resources from external/nested dictionaries and implicit/theme scopes are not modeled. Returns targets/keys, no writes.',
 ('tools/xaml-audit.py','audit_structure'):'Read61-83. ElementTree parses direct Views/Controls/App XML and catches ParseError, then counts non-property children of three single-child container names. It validates XML shape only, not WinUI semantic bindings; inaccessible files or non-ParseError IO failures propagate. No native UI construction.',
 ('tools/xaml-audit.py','audit_converters'):'Read86-100. If App or converter file missing returns empty list. Regex identifies only public sealed IValueConverter declarations with exact formatting and conv prefix in App; alternate namespace/class shapes can cause false positives or misses. Diagnostics line counts are textual, no type compilation.',
 ('tools/xaml-audit.py','audit'):'Read103-144. Independent regex checks for collection converter binding, XML comment hyphens, IsEnabled on named noncontrols, style target exactness and StaticResource names. Inheritance/framework/theme resources and formatting are not resolved semantically; diagnostics can be false positives or omissions. Reads immediate files, not nested/native states.',
 ('tools/xaml-audit.py','main'):'Read147-160 and module entry. Checks only two hardcoded project directories if present, concatenates three checker results and deduplicates sorted strings. Zero checked projects still prints clean/returns0; caller must separately verify scope. No build, login, app launch, source write or physical output.'}
block_notes={
 ('tools/pre-demo-check.ps1',33):('Filters merged command output by truthiness and nonblank Trim. Does not distinguish SQL error text from data and can throw if a nonstring object lacks Trim.', 'Each merged sqlcmd output item', 'Boolean predicate used to retain output; no new external call'),
 ('tools/pre-demo-check.ps1',117):('Counts only results whose Result equals FAIL. This is a summary of caller-recorded flags, not independent verification; omitted checks cannot contribute failure.', 'Recorded result object', 'Boolean predicate for final failure count'),
 ('tools/repro-native-crash.ps1',31):('Wildcard substring matches event Message against captured ImageName. Names containing wildcard syntax can broaden matches and unrelated same-image events are not process-ID correlated.', 'Application event object and captured image name', 'Boolean predicate; event reads belong to surrounding helper'),
 ('tools/repro-native-crash.ps1',87):('Formats each grouped Mode/Phase name and count after completed harness runs. It only writes console text and does not itself start or kill processes.', 'Grouped harness result', 'Console summary line'),
 ('tools/repro-native-crash.ps1',92):('Formats grouped Exit code frequency ordered by count. Unknown n/a and successful exits remain report values; this aggregation does not independently decide crash cause.', 'Grouped exit result', 'Console frequency line'),
 ('tools/verify-windows-printer-transport.ps1',52):('Prefixes agent stdout with agent label. Unfiltered output may contain paths/device details; does not check the process exit code or spooler acceptance.', 'Agent stdout item', 'Console text'),
 ('tools/verify-windows-printer-transport.ps1',61):('Selects every queue job with generic DocumentName Falah ticket, not only the current verification submission. Surrounding pipeline removes all selected jobs, supporting PRT005 broad cleanup risk. No queue queried or changed.', 'Print job metadata', 'Boolean selector used by destructive Remove-PrintJob downstream')}
records=[];reviews=[];errors=[]
def add(path,kind,name,a,b,c,d,parser,signature=''):
    src=load_symbols(path)
    s={'kind':kind,'name':name,'qualifiedSymbol':path+'::'+name,'signature':signature,
       'startLine':a,'endLine':b,'startColumn':c,'endColumn':d,'isCallable':True,'parser':parser}
    s['stable_id']=stable_symbol_id(path,src['git_blob_sha256'],s)
    s.update({'path':path,'source_git_blob_sha256':src['git_blob_sha256'],'worktree_sha256':src['worktree_sha256']})
    if sha256((ROOT/path).read_bytes())!=src['worktree_sha256']:raise ValueError('Changed script '+path)
    records.append(s)
    if kind=='PowerShellScriptBlockExpression':
        reason,inputs,outputs=block_notes[(path,a)]
        r={'stable_id':s['stable_id'],'status':'ANALYZED_SOURCE_ONLY','reasoning':reason,'inputs':inputs,'outputs':outputs,
           'side_effects':'Console/predicate behavior as specified; all audited scripts NOT_EXECUTED',
           'trust_boundaries':'Caller-provided diagnostic/result metadata; no application authentication or runtime data accessed',
           'database':'No direct database operation in this scriptblock; surrounding script context remains distinct',
           'printing_ui':'PRT005 applies to selector61; other blocks are diagnostics, not paper/native UI verification',
           'performance':'Linear per-item/group predicate/formatting; no measured runtime or throughput',
           'duplicates_conflicts':'Generic names/aggregation can conflate records as detailed in reasoning',
           'issues_hypotheses':'See reasoning; no physical reproduction',
           'tests_missing':'Mock output/event/job objects and scope/exit failures without executing script top-level effects',
           'verified_callers_callees':[f'{path}:{a}-{b} actual pipeline and surrounding script source read']}
    else:
        orig=legacy_by[(path,name)]
        if orig['source_span']!=[a,b]:errors.append({'path':path,'name':name,'error':'LEGACY_SPAN_MISMATCH'})
        if orig['source_git_blob_sha256']!=src['git_blob_sha256'] or orig['worktree_sha256']!=src['worktree_sha256']:
            raise ValueError('Legacy hash mismatch '+path)
        r={k:v for k,v in orig.items() if k not in ('stable_id','status')}
        r.update({'stable_id':s['stable_id'],'status':'ANALYZED_SOURCE_ONLY','parent_independent_reasoning':notes[(path,name)],
                  'historical_reasoning_source':'Sub-Agents/Results/printing-noncsharp-function-gap.json'})
    reviews.append(r)
for file in ps:
    if file['parseErrors']:raise ValueError('PowerShell parse failure '+file['path'])
    for f in file['functions']:
        add(file['path'],'PowerShellFunctionDefinition',f['name'],f['startLine'],f['endLine'],f['startColumn'],f['endColumn'],
            'System.Management.Automation.Language.Parser.ParseFile',f.get('signature') or '')
    for s in file['scriptBlockExpressions']:
        add(file['path'],'PowerShellScriptBlockExpression','scriptblock@'+str(s['startLine'])+':'+str(s['startColumn']),
            s['startLine'],s['endLine'],s['startColumn'],s['endColumn'],'System.Management.Automation.Language.Parser.ParseFile')
path='tools/xaml-audit.py';src=(ROOT/path).read_text(encoding='utf-8-sig');tree=ast.parse(src,filename=path)
for node in ast.walk(tree):
    if isinstance(node,(ast.FunctionDef,ast.AsyncFunctionDef,ast.Lambda)):
        name=node.name if hasattr(node,'name') else '<lambda>'
        add(path,type(node).__name__,name,node.lineno,node.end_lineno,node.col_offset+1,node.end_col_offset+1,'Python3.14 ast.parse',ast.unparse(node.args))
if errors:raise ValueError(errors)
if len({s['stable_id'] for s in records})!=len(records):raise ValueError('Duplicate identity')
atomic_json(WORK/'Knowledge/Source-Symbols-NonCSharp/callable-index.json',
            {'baseline':BASELINE,'at_utc':now(),'status':'AST_INDEX_WITH_SEPARATE_SOURCE_REASONING','csharp_index_unchanged':True,
             'scope':'All6baselinePowerShell files parsed without invocation; Python xaml-audit parsed without importing. Named functions plus explicit scriptblock expressions. Top-level script effects remain file-level concerns, not additional function definitions.',
             'column_units':'PowerShell extent columns UTF16; Python AST columns UTF8 byte offsets converted to1based; stable IDs preserve parser-specific span.',
             'callable_count':len(records),'records':records,'errors':errors})
atomic_json(WORK/'Knowledge/Reviews-NonCSharp/parent-and-original-printing.json',
            {'baseline':BASELINE,'at_utc':now(),'reviewed_callable_count':len(reviews),'records':reviews,
             'limits':'17 exact source-only receipts: original10 named functions independently source-read by parent;7 explicit scriptblocks newly reasoned. No script invoked, installation/authentication/SQL/eventlog/printing performed. This supplements, never silently changes, C#11653 denominator.'})
atomic_json(WORK/'Sub-Agents/Checkpoints/parent-noncsharp.json',{'at_utc':now(),'status':'17_AST_IDENTITIES_AND_SOURCE_REASONING_SAVED',
            'last_file':'tools/verify-windows-printer-transport.ps1','last_symbol_line':61,
            'remaining':'No named/scriptblock identity gap in this parsed scope; whole15phase audit still incomplete',
            'next_action':'Independent validator checks17 source hashes/IDs/schema; update original10gap resolution without erasing original evidence.'})
print(json.dumps({'noncsharp_callables':len(records),'source_reasoning_receipts':len(reviews),'executed_scripts':0}))
