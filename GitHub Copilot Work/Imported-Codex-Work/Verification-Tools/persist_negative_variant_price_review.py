"""Persist a bounded source review of server-side product-variant price validation."""
from knowledge_store import *
import collections

scope = {
    'src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs': [(323, 339)],
    'src/Falah.Application/Menu/MenuManagementRequests.cs': [(20, 34)],
    'src/Falah.Infrastructure/Menu/ProductHandlers.cs': [(29, 58), (95, 144)],
    'src/Falah.Domain/Common/Money.cs': [(9, 36)],
    'src/Falah.Domain/Pricing/ItemPricingRules.cs': [(13, 28)],
    'src/Falah.Infrastructure/Orders/MenuPricingService.cs': [(25, 64)],
    'src/Falah.Domain/Orders/OrderItem.cs': [(10, 24)],
    'src/Falah.Domain/Orders/CheckoutRules.cs': [(32, 39), (68, 109)],
    'src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs': [(115, 154), (248, 280)],
    'src/Falah.Infrastructure/Persistence/FalahDbContext.cs': [(201, 207)],
    'src/Falah.Infrastructure/Persistence/Migrations/20260902120327_InitialCreate.cs': [(338, 352)],
    'tests/Falah.Server.IntegrationTests/MenuManagementVerticalSliceTests.cs': [(149, 215)],
}
baseline = {f['path']: f for f in json.loads((WORK/'Deep-Audit/Source-Inventory/verified-baseline-hashes.json').read_text(encoding='utf8'))['files']}
refs = []
for path, ranges in scope.items():
    raw = (ROOT/path).read_bytes()
    src = load_symbols(path)
    assert sha256(raw) == baseline[path]['worktree_sha256'] == src['worktree_sha256'], path
    lines = raw.decode('utf-8-sig').splitlines()
    assert all(1 <= a <= b <= len(lines) for a, b in ranges), path
    symbols = []
    for s in src['symbols']:
        if s['isCallable'] and any(s['startLine'] <= b and s['endLine'] >= a for a, b in ranges):
            symbols.append({'stable_id': s['stable_id'], 'signature': s.get('qualifiedSymbol') or s['name'],
                            'kind': s['kind'], 'span': [s['startLine'], s['startColumn'], s['endLine'], s['endColumn']]})
    refs.append({'path': path, 'worktree_sha256': sha256(raw), 'source_git_blob_sha256': baseline[path]['git_blob_sha256'],
                 'line_ranges': ranges, 'relevant_symbols': symbols,
                 'excerpts': [{'start': a, 'end': b, 'text': '\n'.join(lines[a-1:b])} for a, b in ranges]})

artifact_rel = 'Deep-Audit/Duplication-and-Architecture/negative-variant-price-semantic-comparison.json'
artifact = {
    'artifact_kind': 'manual_semantic_comparison', 'id': 'SEM-MIRROR-045', 'baseline': BASELINE,
    'created_at_utc': now(), 'reviewer': '/root coordinator',
    'scope': 'Bounded client/server catalog variant-price validation and downstream checkout amount/stock path; this is a targeted FIN016-adjacent validation lead, not a full catalog or pricing audit.',
    'decision': 'CashierWinUI rejects variant prices <= 0 in its editor, but ProductVariantInput has no validation attributes and both server CreateProductHandler and UpdateProductHandler persist variant input price without a positivity check. The initial schema and EF model establish a non-null bigint/key but no positive-price check. At checkout, MenuPricingService selects the stored variant price, ItemPricingRules preserves list price when no applicable rule overrides it, signed Money accepts negative values, and CheckoutRules sums item totals then clamps a negative taxable base/payable to zero. CheckoutOrderHandler persists the order and deducts stock even when the plan defers all money. Thus a negative catalog variant can understate a mixed order or produce a zero-payable order while stock is consumed, conditional on authorized catalog configuration and later selection. This is source-supported behavior; no API/DB reproduction was run.',
    'finding_candidate': 'FIN-CAND-NEGATIVE-VARIANT-PRICE-001; source-supported configuration-validation candidate, not independently challenged and not runtime-proven.',
    'severity': 'MEDIUM candidate: requires menu_manage catalog configuration; supports accidental or improper signed price storage and downstream undercharge/free-order behavior, while that permission already permits legitimate repricing. Do not describe as unauthenticated or exploit-confirmed.',
    'manual_reasoning': {
        'entry_and_server_validation': 'MenuEditorViewModel.CreateRequest rejects blank variant names, unparsable values, and amount <= 0. The request DTO is a plain record with long PricePiastres. CreateProductHandler validates only product name/base price and menu_manage authorization before mapping each variant input directly to ProductVariantEntity. UpdateProductHandler validates product-level fields then directly assigns each input price for both retained and new variants. The UI guard is therefore not a server trust boundary.',
        'persistence_and_checkout': 'ProductVariants initial migration declares PricePiastres as non-null bigint; FalahDbContext configures only the primary key for this entity, with no positive-value check in the inspected mapping/schema. MenuPricingService loads a selected variant and uses its stored amount as list price. ItemPricingRules returns that list amount unchanged when no valid rule applies. Money.FromPiastres and multiplication preserve signed values; CheckoutRules sums item totals, clamps negative taxable base and negative payable to zero, and treats nonpositive payable as deferring all money. CheckoutOrderHandler still creates/persists the order and unconditionally calls inventory deduction before skipping treasury posting for a fully deferred plan.',
        'workflow_precondition_and_effect': 'A menu_manage-authorized request must first create or update a variant to a negative price; a subsequent POS checkout selects that variant. A single negative item is predicted to become a zero-payable sale; in a mixed cart it reduces the positive subtotal. The checkout caller still requires the ordinary POS permission and open shift, and inventory deduction is part of the transaction. No live order, DB write, or direct HTTP request was performed.',
        'counterevidence_and_overlap': 'Price editing is intentionally privileged and can set low positive prices; owner policy for zero/negative variants is not separately documented. A valid positive price rule can replace the variant list price. The client normally rejects negative values. This is distinct in precondition from FIN-016 negative delivery-fee path, which concerns a POS actor and bypassed discount/comp permission; do not merge or claim independent verification. Related FIN016 overflow extension previously left this variant lead pending a full Update/client/DTO review; that bounded review is now source-corroborated.',
        'recommended_fix_design': 'If variant prices are required to be positive, enforce that invariant in both server command handlers or a shared domain validator before any persistence, and consider a database check constraint for defense in depth after evaluating legacy rows. Also reject nonpositive authoritative item prices in MenuPricingService/checkout before order or inventory writes. Preserve signed Money elsewhere for legitimate financial deltas. Decide explicitly whether zero-priced variants are allowed before choosing >0 versus >=0.',
        'required_tests': 'Unit tests for create and update with negative, zero, and positive variant prices; update-existing and add-new branches; direct request/API path bypassing the WinUI guard; checkout with a stored negative variant alone and mixed with positive items; ensure rejection occurs before order, stock, audit, journal, or treasury writes; verify positive variant and valid fixed-price-rule behavior remain unchanged; migration/data check against any legacy nonpositive variant rows in a separately approved disposable DB.',
        'acceptance_criteria': 'Server and persistence boundary enforce the owner-approved variant-price domain; invalid values cannot reach checkout resolution; an invalid selected stored variant fails before order/inventory effects; positive variants and valid pricing-rule overrides retain expected behavior; any legacy-data migration plan is tested before production deployment.'
    },
    'runtime_status': 'NOT_EXECUTED; static source comparison only',
    'limitations': ['No test was run; no database or application state was changed.', 'menu_manage policy permits intentional price edits, and owner policy for zero/negative prices is not explicit.', 'No real incidence, deployed schema, HTTP acceptance, or physical checkout was observed.', 'The original financial specialist is currently unavailable due to the platform usage limit; independent challenge remains pending.'],
    'source_references': refs
}
atomic_json(WORK/artifact_rel, artifact)

# Add one bounded semantic comparison without modifying existing comparison evidence.
registry_path = WORK/'Deep-Audit/Duplication-and-Architecture/semantic-comparison-registry.json'
registry = json.loads(registry_path.read_text(encoding='utf8'))
assert not any(g['id'] == artifact['id'] for g in registry['semantic_groups'])
registry['semantic_groups'].append({'id': artifact['id'], 'decision': 'Server variant-price validation gap reaches signed checkout price and inventory path; bounded configuration candidate, no runtime reproduction.', 'artifact': artifact_rel})
artifact_bytes = (WORK/artifact_rel).read_bytes()
registry['inputs'].append({'path': artifact_rel, 'sha256': sha256(artifact_bytes), 'source_references': len(refs)})
all_paths, all_ids, source_ref_records, ranges_count = set(), set(), 0, 0
for item in registry['inputs']:
    doc = json.loads((WORK/item['path']).read_text(encoding='utf8'))
    doc_refs = doc.get('source_references', doc.get('sources', []))
    source_ref_records += len(doc_refs)
    for ref in doc_refs:
        all_paths.add(ref['path'])
        ranges_count += len(ref.get('line_ranges', []))
        all_ids.update(x['stable_id'] for x in ref.get('relevant_symbols', []))
registry.update(at_utc=now(), referenced_source_paths=len(all_paths), referenced_callable_context_ids=len(all_ids), source_reference_records=source_ref_records, explicit_ranges=len(all_paths) if False else ranges_count)
atomic_json(registry_path, registry)

# Preserve the candidate in the canonical registry; do not merge it with FIN-016.
master_path = WORK/'Deep-Audit/MASTER_FINDINGS.json'
master = json.loads(master_path.read_text(encoding='utf8'))
assert not any(x.get('id') == 'FIN-CAND-NEGATIVE-VARIANT-PRICE-001' for x in master['findings'])
source_refs = []
for ref in refs:
    source_refs.append({'path': ref['path'], 'source_git_blob_sha256': ref['source_git_blob_sha256'],
                        'worktree_sha256': ref['worktree_sha256'], 'evidence_ranges': ref['line_ranges'],
                        'relevant_symbols': ref['relevant_symbols']})
candidate = {
    'id': 'FIN-CAND-NEGATIVE-VARIANT-PRICE-001', 'baseline': BASELINE, 'severity': 'MEDIUM',
    'confidence': 'SOURCE_SUPPORTED_CONFIGURATION_VALIDATION_CANDIDATE;NO_INDEPENDENT_CHALLENGE_OR_RUNTIME_REPRODUCTION',
    'original_status': 'SOURCE_CORROBORATED_CANDIDATE_PENDING_FINANCIAL_SPECIALIST_AND_OWNER_PRICE_POLICY',
    'title': 'Server accepts a negative product-variant price that can reduce checkout totals while stock is consumed',
    'source_references': source_refs,
    'source_record': {
        'sources': [{'path': ref['path'], 'line_ranges': ref['line_ranges'],
                     'worktree_sha256': ref['worktree_sha256'],
                     'source_git_blob_sha256': ref['source_git_blob_sha256']} for ref in refs],
        'method': 'Manual coordinator source trace across client guard, command DTO and handlers, persisted schema, authoritative checkout pricing, domain calculation and inventory/payment orchestration.',
        'runtime': 'Not executed; no database or application state changed.'
    },
    'independent_verification': {'status': 'PENDING', 'reviewer': None, 'note': 'Coordinator source trace only; original financial specialist unavailable due platform usage limit.'},
    'verification_classification': {'source_behavior': 'SOURCE_SUPPORTED', 'isolated_reproduction': 'NOT_RUN', 'end_to_end_runtime': 'NOT_RUN'},
    'origin': {'artifact': artifact_rel, 'semantic_group': 'SEM-MIRROR-045', 'related_existing_finding': 'FIN-016 adjacent signed monetary validation family; kept separate because actor capability and entry point differ.'},
    'implementation_readiness': {
        'current_behavior': 'WinUI rejects nonpositive variant prices, while server create/update handlers persist request values without the same check. Checkout uses selected stored variant price; absent valid price-rule override it remains effective. Negative line totals can lower subtotal; negative taxable base and payable are clamped to zero; order persistence and stock deduction still occur for zero payable.',
        'preconditions': 'A menu_manage-authorized actor creates or updates a product variant with a negative price, and a later POS actor selects that variant. No unauthenticated access or live exploit is claimed.',
        'impact': 'Potential undercharge or zero-payable order with stock consumption; actual financial incidence and owner policy are unverified.',
        'proposed_remediation': 'Confirm whether zero-priced variants are allowed. If prices must be positive, centralize the invariant at server command boundaries and checkout resolution, and evaluate a DB check constraint after legacy-row inventory. Preserve signed Money for legitimate ledger deltas.',
        'required_regression_tests': ['Create/update direct API requests with negative, zero and positive values, including existing and new variants.', 'Checkout with negative variant alone and in mixed cart; invalid selection must fail before order/stock/audit/ledger effects.', 'Positive variant and valid fixed-price rule remain unchanged.', 'Check legacy rows before any proposed database constraint rollout.'],
        'acceptance_criteria': ['Owner-approved variant price domain is enforced server-side and at checkout.', 'Invalid variant configuration cannot create underpriced/free stock-consuming checkout.', 'No financial side effect occurs on rejection.', 'Valid positive variants and approved prices remain compatible.'],
        'counterevidence': ['WinUI client rejects nonpositive variant prices.', 'Menu management is privileged and already permits intentional repricing.', 'A valid price rule can override list price.', 'Owner policy for zero or negative variant prices is not documented.', 'No test, DB, HTTP, or live checkout was executed.'],
        'runtime_status': 'NOT_EXECUTED; static source trace only'
    }
}
master['findings'].append(candidate)
master['finding_count'] = len(master['findings'])
master['updated_at_utc'] = now()
atomic_json(master_path, master)

# Update financial report with a source-backed candidate section.
report_path = WORK/'Deep-Audit/Database-and-Finance/DATABASE_AND_FINANCIAL_INTEGRITY.md'
report = report_path.read_text(encoding='utf8')
marker = '## Coordinator continuation — negative variant-price configuration path'
assert marker not in report
section = f'''\n\n{marker}\n\n**Candidate FIN-CAND-NEGATIVE-VARIANT-PRICE-001 — MEDIUM, source-supported; independent challenge and runtime reproduction pending.** [Evidence comparison](../Duplication-and-Architecture/negative-variant-price-semantic-comparison.json).\n\n`MenuEditorViewModel.CreateRequest` rejects variant prices at or below zero, but `ProductVariantInput` is a plain DTO and both `CreateProductHandler.HandleAsync` and `UpdateProductHandler.HandleAsync` copy variant prices to persistence without a corresponding positivity check. The initial migration defines `ProductVariants.PricePiastres` as non-null `bigint`; the inspected EF configuration adds only the key. `MenuPricingService.ResolveAsync` uses a selected variant price as the authoritative list price, and `ItemPricingRules.Resolve` preserves it when no valid rule overrides it. `Money` permits signed values. `CheckoutRules` aggregates line totals, clamps negative taxable base/payable to zero, and sets `DefersAllMoney` for nonpositive payable. `CheckoutOrderHandler` still persists the order and deducts inventory before it skips treasury posting for a fully deferred plan. This supports possible undercharge or a zero-payable stock-consuming order after privileged menu configuration; it does not establish live API acceptance or actual loss.\n\nProposed repair direction: obtain the owner decision on whether zero variant prices are valid, then enforce the chosen rule in both server mutation paths and checkout price resolution; assess a database check constraint only after checking legacy data. Preserve signed `Money` for legitimate accounting deltas. Regression tests should cover direct create/update requests, new and retained variants, negative/zero/positive values, a negative variant alone and in a mixed cart, no side effects on rejection, and positive price-rule override compatibility. No test or database operation was run. This remains a separate candidate from FIN-016 because its entry point is menu configuration and requires `menu_manage`; it is not counted as an independent challenge.\n'''
atomic_text(report_path, report+section)

# Record immediate exact continuation, including the unavailable agent without a retry.
next_path = WORK/'Conversation-Recovery/NEXT_ACTION.md'
old_next = next_path.read_text(encoding='utf8')
checkpoint_marker = '## Latest verified batch — negative variant price'
assert checkpoint_marker not in old_next
next_section = f'''\n\n{checkpoint_marker} ({now()})\n\n- Finance specialist status checked: `/root/financial_integrity_continuation` remains errored from explicit platform usage limit (resume time stated Nov 20, 2026 9:17 AM); original `/root/financial_integrity` remains unavailable. No replacement or repeated launch was attempted.\n- Main coordinator completed a bounded source trace for SEM-MIRROR-045 / FIN-CAND-NEGATIVE-VARIANT-PRICE-001. Evidence: `{artifact_rel}`; financial report section appended; candidate added to canonical MASTER_FINDINGS.json. Source references include current baseline hash/blob, line ranges and indexed callable IDs. No application/tests/DB/runtime modifications, no tests, no Git publication.\n- Candidate classification: source-supported server-side variant-price validation gap with a conditional undercharge/zero-payable stock-consumption path; privileged `menu_manage` precondition, client guard counterevidence, owner policy for zero prices unresolved, independent challenge pending. FIN-016 remains separate.\n- Next exact operation: run `validate_semantic_evidence.py`; verify master JSON candidate/source hashes and line ranges; then continue coordinator's bounded mirror queue at a distinct relevant group. When the same finance agent becomes available, send it the evidence artifact and request an independent challenge plus continuation at its saved finance queue.\n- Overall finance callable/file completion remains OPEN; all non-financial specialists remain paused; original full-audit backlog and publication restrictions remain unchanged.\n'''
atomic_text(next_path, old_next+next_section)

print(json.dumps({'artifact': artifact_rel, 'source_paths': len(refs), 'referenced_symbols': len({s['stable_id'] for r in refs for s in r['relevant_symbols']}), 'finding_count': master['finding_count'], 'candidate': candidate['id'], 'registry_sources': registry['referenced_source_paths'], 'registry_symbol_ids': registry['referenced_callable_context_ids']}))
