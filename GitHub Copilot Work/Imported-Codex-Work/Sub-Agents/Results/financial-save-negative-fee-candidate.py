import sys,json
from pathlib import Path
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()))
import knowledge_store as k
p=k.WORK/'Sub-Agents/Results/financial-new-candidates.json'
d=json.loads(p.read_text(encoding='utf8'))
key='NEGATIVE_DELIVERY_FEE_BYPASSES_PRICE_REDUCTION_PERMISSIONS'
paths=['src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs','src/Falah.Domain/Orders/CheckoutRules.cs','src/Falah.Domain/Common/Money.cs','src/Falah.Application/Orders/CheckoutOrderRequest.cs','src/Falah.Server/Program.cs']
sources=[]
for path in paths:
 s=k.load_symbols(path);sources.append(dict(path=path,source_git_blob_sha256=s['git_blob_sha256'],worktree_sha256=k.sha256((k.ROOT/path).read_bytes())))
record=dict(candidate_key=key,baseline=k.BASELINE,severity='HIGH_CANDIDATE',confidence='HIGH_STATIC_TRACE_NO_RUNTIME',status='CANDIDATE_PENDING_INDEPENDENT_CHALLENGE',independent_pending=True,sources=sources,
 trigger='An authorized active cashier with pos but without orders_discount/orders_comp submits Takeaway/Cash checkout with valid open shift, actual server-priced items and unique request key, discount=0, comp=0, fee=-tax-final cart total, amountPaid=0.',
 evidence=['Request DeliveryFeePiastres is unannotated long at29; Program1370-1374 directly delegates to handler without validation filter found in Server source search.','Handler66 constructs signed Money,112 checks only positive discount permission,116+ checks only positive comp; Takeaway avoids Delivery address/customer requirements at80-96.','CheckoutRules35-38 adds fee and clamps negative payable to0; BuildPlan102 accepts tender0 for payable0; DefersAllMoney70-71 true and ComputeIsPaid77-78 true for non-electronic non-COD.','Handler251 deducts inventory regardless,253 skips treasury for deferred/zero plan then278-279 saves and commits.'],
 root_cause='A signed client delivery-fee field is included in payable computation without nonnegative/order-type/server-service pricing validation; reductions through that field bypass permission and approval controls attached only to Discount and Comp.',
 impact='Zero or reduced payable order and stock consumption without required discount/comp permission or manager comp approval; no financial API write performed to reproduce.',
 counterevidence=['Product price itself is server resolved; ordinary positive manual discount and comp have explicit gates.','Client UI may prevent entering a negative delivery fee; trigger is direct existing API use.','Actor identity binding and network authentication are owned by parent security review; this candidate does not require inventing broader privileges.','No claim a cashier can choose negative fee through ordinary current UI.'],
 correction_design='Validate all monetary input ranges at server domain boundary. Reject negative delivery fee, ignore/reject fee for non-delivery, and freeze server-configured delivery service charge for delivery if that is intended policy. Preserve authorized discount/comp pathways explicitly.',
 regression_tests=['pos-only actor with otherwise valid cart cannot reduce total via negative fee; verify no order, inventory or treasury writes commit.','Zero legitimate delivery fee accepted; valid positive configured fee included correctly.','Non-delivery fee policy enforced; real permitted discount and approved comp still work.'],
 validation='Source-only complete request -> endpoint -> handler -> domain -> persistence trace; awaiting independent challenge.',updated_at_utc=k.now())
d['candidates']=[x for x in d['candidates'] if x.get('candidate_key')!=key]+[record]
k.atomic_json(p,d)
cp=k.WORK/'Sub-Agents/Checkpoints/financial_integrity.json';c=json.loads(cp.read_text(encoding='utf8'));c['independent_pending']=[x['candidate_key'] for x in d['candidates'] if x.get('independent_pending')];c['next_action']='FinancialRules and Money original callable/facet receipts, then ReceivePurchase/PayPurchase and remaining original IDs; no runtime writes.';c['updated_at_utc']=k.now();k.atomic_json(cp,c)
print('Saved negative fee candidate and checkpoint')
