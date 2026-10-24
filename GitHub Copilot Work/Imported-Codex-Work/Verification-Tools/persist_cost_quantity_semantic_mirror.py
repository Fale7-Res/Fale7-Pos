"""Bounded manual shared-rule comparison, no application/test execution."""
from knowledge_store import *
existing_target=WORK/'Deep-Audit/Duplication-and-Architecture/cost-quantity-semantic-comparison.json'
if existing_target.exists():
 existing=json.loads(existing_target.read_text(encoding='utf8'))
 assert existing['id']=='SEM-MIRROR-005'
 print(json.dumps({'already_saved':str(existing_target.relative_to(WORK)),'repeated_append_skipped':True}))
 raise SystemExit(0)
refs=[]
for path,ranges in {
 'src/Falah.Domain/Inventory/PurchasingRules.cs':[(10,11),(74,75)],
 'src/Falah.Domain/Inventory/CostingRules.cs':[(119,128),(131,141),(154,189)],
 'src/Falah.Infrastructure/Orders/InventoryDeductionService.cs':[(21,45),(48,65),(79,99)],
 'src/Falah.Infrastructure/Reports/ReportingQueryService.cs':[(386,423)],
 'src/Falah.Infrastructure/Inventory/InventoryQueryService.cs':[(318,351)],
 'src/Falah.Domain/Common/Money.cs':[(22,26)],
 'tests/Falah.Domain.Tests/PurchasingRulesTests.cs':[(12,17)],
 'tests/Falah.Domain.Tests/CostingRulesTests.cs':[(77,90)]}.items():
 s=load_symbols(path);assert sha256((ROOT/path).read_bytes())==s['worktree_sha256']
 refs.append({'path':path,'source_git_blob_sha256':s['git_blob_sha256'],'worktree_sha256':s['worktree_sha256'],
 'line_ranges':ranges,'relevant_symbols':[{'stable_id':m['stable_id'],'signature':m.get('qualifiedSymbol') or m['name'],
 'span':[m['startLine'],m['startColumn'],m['endLine'],m['endColumn']]} for m in s['symbols']
 if m['isCallable'] and any(m['startLine']<=b and m['endLine']>=a for a,b in ranges)]})
record={'id':'SEM-MIRROR-005','at_utc':now(),'baseline':BASELINE,'reviewer':'/root coordinator',
 'status':'BOUNDED_MANUAL_SEMANTIC_COMPARISON_COMPLETE_NO_NEW_DEFECT',
 'title':'Recipe unit conversion and costing reused across inventory deduction, valuation and sales reports',
 'source_references':refs,
 'reasoning':[
 'InventoryDeduction builds RecipeLine and calls the exact CostingRules.BaseQuantityMilliUnits that ComputeRecipeCost and InventoryQuery.BuildCostLine call. Missing Yield ratio returnsnull; stock skips this line while cost marks it missing. These different outputs reflect operational posting versus known-cost coverage, not two divergent conversion formulas.',
 'Recipe/prep/order-packaging cost and inventory valuation route extended costs through PurchasingRules.PurchaseLineTotal. PurchaseLineTotal decimal quantity*unitcost/1000 differs intentionally in units from Money.FromEgp currency boundary decimal*100. Do not unify arithmetic merely because both round to long.',
 'Sold-line report multiplies already rounded per-recipe known cost by sold quantity; stock multiplies converted milliunit base quantity by sold quantity. They describe money versus stock, not interchangeable balances. Rounding per ingredient/portion versus at aggregate remains policy/test coverage question; no automatic source defect asserted.',
 'Order packaging is once per order: DeductForOrderAsync calls DeductOrderPackagingAsync before recipe loop; report applies OrderPackagingCost per order-type Count. Missing packaging completeness issue remains existing FIN-CAND-PACKAGING-COVERAGE-001, not a fresh duplicate finding.',
 'Existing test source covers499.5piastres->500 and exact100/4Yield->25 plus null ratio. These tests were read only, not executed, and do not establish all tie/overflow/multiple-portion or provider integration behavior.'
 ],
 'financial_cross_references':['FIN-016 unchecked quantity multiplication extension','FIN-CAND-PACKAGING-COVERAGE-001 missing cost completeness','FIN-020 unit metadata/movement admission'],
 'safe_recommendations':['Keep shared conversion and cost rules as authority; wrappers may expose typed known/missing outputs instead of recopying formulas.',
 'Specify explicit midpoint/aggregation and supported range policy before changing arithmetic; preserve milliunit versus piastre units and optional unknowns.',
 'Test multiportion fractional recipes and half-piastre ties across procurement/cost/stock views, plus per-order packaging once and incomplete cost behavior; use isolated authorized integration only when approved.'],
 'limits':'Targeted8sourcepaths, not full-file review or all costing semantics. Current recipe/purchase facts may restate historical reporting; this comparison alone does not certify historical valuation policy.',
 'new_financial_callable_receipts':0,'runtime_tests':'NOT_EXECUTED','new_canonical_findings':0}
rel='Deep-Audit/Duplication-and-Architecture/cost-quantity-semantic-comparison.json';atomic_json(WORK/rel,record)
p=WORK/'Deep-Audit/CODE_DUPLICATION_AND_ARCHITECTURE.md';text=p.read_text(encoding='utf8')
text+='\n## Additional semantic comparison — stock units and cost arithmetic\n\n[SEM-MIRROR-005](Duplication-and-Architecture/cost-quantity-semantic-comparison.json) traces the shared BaseQuantityMilliUnits and PurchaseLineTotal calls through recipe costing, stock deduction, inventory line projection and sold-cost reporting. Conversion is shared, not independently duplicated: unknown Yield conversion skips posting but marks costing incomplete. Money boundary conversion, stock milliunits and per-portion costs have distinct units/rounding roles. Per-order packaging runs once, while its completeness issue is already recorded separately. No new defect or financial callable count added; policy/rounding and isolated integration tests remain required. Existing tests were inspected, not run.\n'
atomic_text(p,text)
p=WORK/'Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.json';status=json.loads(p.read_text(encoding='utf8'))
status.setdefault('additional_semantic_groups',[]).append({'id':record['id'],'path':rel,'status':record['status']})
status['updated_at_utc']=now();atomic_json(p,status)
p=WORK/'Conversation-Recovery/NEXT_ACTION.md';text=p.read_text(encoding='utf8')
atomic_text(p,text+'\nLatest root semantic batch '+now()+': SEM005 shared recipe quantity/cost flow checked, no new defect; evidence `'+rel+'`. SEM004 independent challenge reconciled FIN019 extension; prepaid default retained guarded followup. FIN020 saved independently source-corroborated and merged, total58canonical records. Root next semantic/source quality gates; finance owns remaining current exact IDs. No source/runtime/Git change.\n')
print(json.dumps({'saved':rel,'source_paths':len(refs),'new_findings':0,'new_callables':0}))
