"""Add executable getter facets without rewriting existing source identities/reviews."""
import json
from collections import defaultdict, Counter
from knowledge_store import ROOT, WORK, BASELINE, atomic_json, now, sha256, stable_symbol_id, load_symbols


def main():
    directory = WORK / 'Knowledge/Source-Symbols-CallableFacets'
    raw = json.loads((directory / 'ast-facets.raw.json').read_text(encoding='utf8'))
    if raw['errors']:
        raise ValueError(raw['errors'])
    owners = {}
    for name, key, owner in [('financial', 'records', 'financial_integrity'), ('ui', 'files', 'ui_visual'),
                              ('printing', 'files', 'printing_workflows'), ('parent', 'records', 'parent')]:
        ledger = json.loads((WORK / f'Sub-Agents/Results/{name}-ledger.json').read_text(encoding='utf8'))
        for r in ledger[key] + ledger.get('remaining', []):
            if r['path'] in owners and owners[r['path']] != owner:
                raise ValueError('Overlapping ownership')
            owners[r['path']] = owner
    seen = set()
    byfile = defaultdict(lambda: {'facets': [], 'generated_behavior_inputs': []})
    for kind in ('facets', 'generated_behavior_inputs'):
        for entry in raw[kind]:
            entry = dict(entry)
            source = load_symbols(entry['path'])
            if sha256((ROOT / entry['path']).read_bytes()) != entry['worktree_sha256'] or source['git_blob_sha256'] != entry['source_git_blob_sha256']:
                raise ValueError('Source invalidation: ' + entry['path'])
            origin = entry.get('origin_declaration_stable_id')
            if origin and origin not in {s['stable_id'] for s in source['symbols']}:
                raise ValueError('Unknown origin identity')
            if not origin and entry['kind'] != 'CompilerTopLevelEntryPoint':
                raise ValueError('Missing origin identity')
            sid = stable_symbol_id(entry['path'], entry['source_git_blob_sha256'], entry)
            if sid in seen:
                raise ValueError('Duplicate supplementary identity')
            seen.add(sid)
            entry.update(stable_id=sid, owner=owners[entry['path']], review_status='NOT_ANALYZED',
                         counting_class='SOURCE_DERIVED_EXECUTABLE_GETTER' if kind == 'facets' else 'GENERATED_BEHAVIOR_CONTEXT_NOT_INDIVIDUAL_METHOD_COUNT')
            byfile[entry['path']][kind].append(entry)
    for path, value in byfile.items():
        atomic_json(directory / 'Files' / (path + '.json'), dict(baseline=BASELINE, path=path, **value))
    facets = [f for v in byfile.values() for f in v['facets']]
    generated = [f for v in byfile.values() for f in v['generated_behavior_inputs']]
    result = {'baseline': BASELINE, 'at_utc': now(), 'method': 'Roslyn AST additive facet inventory; no reasoning promotion',
              'original_syntax_callable_declarations': 11653, 'expression_getter_facets': len(facets),
              'mapped_source_callable_units': 11653 + len(facets),
              'getter_owner_counts': dict(Counter(f['owner'] for f in facets)),
              'generated_behavior_input_counts': dict(Counter(f['kind'] for f in generated)),
              'generated_behavior_owner_counts': dict(Counter(f['owner'] for f in generated)),
              'facets': facets, 'generated_behavior_inputs': generated,
              'limits': ['This is not an exact CLR/runtime generated-method denominator.',
                         'Primary constructors, record synthesized operations, top-level entry points and toolkit generation are separately accounted contextual behaviors, not silently absent or invented source methods.',
                         'Original11653 stable identities and all analysis receipts remain unchanged. Supplementary entries initially NOT_ANALYZED.',
                         'Inactive preprocessor regions and generated output semantic correspondence retain separate configuration limits.']}
    atomic_json(directory / 'index.json', result)
    print(json.dumps({k: v for k, v in result.items() if k not in ('facets', 'generated_behavior_inputs', 'limits')}))


if __name__ == '__main__':
    main()
