"""Persist manual cross-client API comparisons without re-auditing completed UI functions."""
import json
import collections
from knowledge_store import *

ledger_path=WORK/'Deep-Audit/Duplication-and-Architecture/clone-review-ledger.json'
ledger=json.loads(ledger_path.read_text(encoding='utf8'))
history=WORK/'Knowledge/History/financial_coordinator'/('clone-review-ledger.'+sha256(ledger_path.read_bytes())[:16]+'.json')
if not history.exists():atomic_json(history,ledger)
notes={
28:'Three string-base constructors (legacy WPF, CashierWinUI, CallCenter) construct HttpClient with same default localhost5151 and10second timeout. Injected-HttpClient overload/configuration can differ, so identical constructor body does not prove identical deployed host/session/lifetime. No runtime connection initiated.',
36:'Both WinUI image-URI helpers return null for missing version/base address; otherwise escape ID/version into same route. Cashier comments differ only. No financial amount calculated; generic gateway duplication only, preexisting image authentication candidate remains paused, not validated by this comparison.',
41:'Both WinUI PingHealth dispose response and return HTTP success, catch all including cancellation as false. It tests endpoint reachability/status, not DB/payment readiness. Legacy WPF health response lacks using and is not in this exact-body group; do not infer identical lifetime across legacy.',
56:'ReadResult shared body interprets success JSON or null outcome-unknown; error body parse failure falls back status message, >=500 unknown. Same known/unknown outcome policy for financial idempotent commands; malformed success is unknown, not proof rollback. Both share common Application request/response types where imported; local generic result/error wrappers remain separate.',
84:'SendAsync shared body disposes HTTP response, maps HTTP/cancellation/JSON/IO exceptions to financial outcome unknown and same-key retry wording. Does not itself retain/retry requests or ensure idempotency; server journal and calling view model do that. Cashier separate ReadAsync changes message for reads, CallCenter uses SendAsync more broadly.',
105:'GetSettings identical wrapper delegates to SendAsync and common Application SettingsDto; no local settings/database mutation. Transport failure classification shared even though read-only request.',
115:'Login bodies equal: obtain server-issued token, set HttpClient default Authorization and return projected current-user fields. Successful login mutates shared ApiClient auth before view-model followup. UI020 context remains qualified prior finding; helper equality is not proof identical callers or permissions. PIN values never captured or executed.',
133:'Logout bodies equal: attempt server revoke, swallow failure then clear local token/header. Local end does not prove remote invalidation on network failure; server expiry separate. No cross-client singleton/session sharing proven.',
147:'OpenShift wrappers equal route/request type. Neither calculates opening cash or enforces balance/permission locally; server OpenShift handler owns FIN011 lock issue.',
152:'PrepareShiftClose wrappers equal request/route and nullable result handling. Read-only preview represented POST; not actual close or movement. Same transport unknown classification does not post money itself.',
157:'CloseShift wrappers equal shared DTO and endpoint. Client body equality does not supply server concurrency/reconciliation or validated approval provenance; FIN017 authoritative server issue retained.',
162:'Checkout wrappers equal shared request/route/result; prices, fees, stock and totals resolved server-side. No client cash/callcenter alternate formula in these bodies. FIN016 server signed-fee boundary cannot be repaired merely by merging wrappers.',
167:'Refund wrappers equal shared DTO/route. Shared HTTP method does not establish same caller role, original/current shift attribution or refund eligibility; server FIN003/004 semantics independent.',
172:'Electronic-confirm wrappers equal common request/result and route. No payment-leg arithmetic in client wrapper; server FIN001 mixed-idempotency issue survives both clients.',
177:'Cancel-pending wrappers equal common request/result and route. Server FIN002 cancellation-versus-collected split cash concern is not a differing client formula.',
197:'IssueManagerApproval wrappers equal body/endpoint. Requests are server authority, not client-generated manager proof. Same client methods present does not mean CallCenter role may execute every financial API.',
202:'Expense wrappers equal route/common DTO. Server chooses real source account permission and validates amounts; no duplicated expense calculation here.',
209:'SafeDrop wrappers equal body/result/route; server owns drawer/account/shift authorization and transaction. Wrapper does not guard current shift status or overdraw independently.',
214:'Cashier cash movement wrappers equal DTO/route; server movement type/account logic remains one authoritative implementation.',
219:'Cash-drawer-open wrappers equal shared request/route. This comparison invoked no API/hardware; no physical drawer or print operation performed.',
233:'CreatePrintJobs wrappers equal route-escaped order ID/body/result. Queue request separate from financial success; no paper sent. This is narrow API mirror classification, not resumed printing audit.',
235:'Nested sender lambda within CreatePrintJobs identical. Captures its own HttpClient/order/request/ct; no shared instance or financial ledger effect inferred from token equality.'
}
reviewed=[]
for group in ledger['groups']:
    if not group['status'].startswith('PENDING') or not any(o['path'].endswith('/Services/ApiClient.cs') for o in group['occurrences']):continue
    cc=next(o for o in group['occurrences'] if o['path'].startswith('src/Falah.Client.CallCenterWinUI/'))
    assert cc['start_line'] in notes,cc
    evidence=[]
    for occurrence in group['occurrences']:
        source=load_symbols(occurrence['path'])
        assert sha256((ROOT/occurrence['path']).read_bytes())==source['worktree_sha256']
        symbol=next(s for s in source['symbols'] if s['stable_id']==occurrence['stable_id'])
        evidence.append({**occurrence,'signature':symbol.get('qualifiedSymbol') or symbol['name'],'worktree_sha256':source['worktree_sha256']})
    group.update(status='MANUALLY_COMPARED_CLIENT_API_TRANSPORT',reviewer='root financial_coordinator',reviewed_at_utc=now(),
        reasoning=notes[cc['start_line']],comparison_scope='Actual source bodies read for both WinUI occurrences and legacy constructor; contextual service headers and additional read-path differences inspected. Existing AST token equality re-used only after raw source hash validation.',
        recommended_improvement='Consider one shared transport/typed API contract library with compatible facade signatures; keep command outcome-unknown semantics distinct from read messaging and keep server business authority. No refactor implemented.',
        required_tests=['Shared HTTP response/null/malformed/4xx/5xx/cancellation cases','Existing same-key command retries and role/session isolation','Both client adapters same JSON Application contract','Read-only methods must not trigger monetary retry UX'],runtime_tested=False)
    reviewed.append({'group_id':group['group_id'],'reasoning':group['reasoning'],'occurrences':evidence})
if not reviewed:
    print(json.dumps({'new_comparison_groups':0,'existing_evidence_preserved':True}));raise SystemExit(0)
ledger['status_counts']=dict(collections.Counter(g['status'] for g in ledger['groups']))
ledger['at_utc']=now();ledger['next_action']='Remaining50application groups and24test groups need actual context triage. Divergent GetMenu/GetTreasuryBalance not exact clones: separately documented read-message difference. Entire semantic scope still incomplete.'
atomic_json(ledger_path,ledger)
atomic_json(WORK/'Deep-Audit/Duplication-and-Architecture/client-api-financial-mirror-review.json',{
    'at_utc':now(),'baseline':BASELINE,'reviewer':'root financial_coordinator','new_groups':len(reviewed),'groups':reviewed,
    'independently_source_read_not_new_function_coverage':'These callable IDs already have UI-review receipts; no original financial or global function count increment claimed.',
    'divergent_near_mirrors':[
        {'symbols':['CallCenter ApiClient.GetMenuAsync147','CashierWinUI ApiClient.GetMenuAsync189'],'difference':'CallCenter uses SendAsync read failure monetary retry wording; Cashier uses ReadAsync non-monetary message. Both have unknown flag true on transport failure. No financial write from GET.'},
        {'symbols':['CallCenter ApiClient.GetTreasuryBalanceAsync227','CashierWinUI ApiClient.GetTreasuryBalanceAsync705'],'difference':'Same server balance contract, SendAsync vs ReadAsync affects message; no client balance formula.'}
    ],'limits':'No HTTP/auth/DB/native operations. Generic image/print glue classified without reopening paused security-image/printing investigations. Other mirrored code outside these groups not complete.'
})
print(json.dumps({'new_comparison_groups':len(reviewed),'new_distinct_callable_identities':len({o['stable_id'] for r in reviewed for o in r['occurrences']}),'status_counts':ledger['status_counts']}))
