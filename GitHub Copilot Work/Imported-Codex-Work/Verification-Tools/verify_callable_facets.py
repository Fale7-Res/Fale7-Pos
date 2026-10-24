"""Validate additive expression getter and generated-context review accounting."""
import json
from collections import Counter
from knowledge_store import ROOT, WORK, BASELINE, atomic_json, stable_symbol_id, now, sha256


def main():
    index = json.loads((WORK / 'Knowledge/Source-Symbols-CallableFacets/index.json').read_text(encoding='utf8'))
    facets = {x['stable_id']: x for x in index['facets']}
    generated = {x['stable_id']: x for x in index['generated_behavior_inputs']}
    errors, receipts = [], []
    for entry in [*facets.values(), *generated.values()]:
        if stable_symbol_id(entry['path'], entry['source_git_blob_sha256'], entry) != entry['stable_id']:
            errors.append('Unstable facet identity: ' + entry['path'])
        if sha256((ROOT / entry['path']).read_bytes()) != entry['worktree_sha256']:
            errors.append('Source hash changed: ' + entry['path'])
    required = ('reasoning', 'inputs', 'outputs', 'side_effects', 'trust_boundaries', 'database', 'printing_ui',
                'performance', 'duplicates_conflicts', 'issues_hypotheses', 'tests_missing', 'verified_callers_callees')
    reviewed, accounted = set(), set()
    folder = WORK / 'Knowledge/Reviews-CallableFacets'
    for p in sorted(folder.rglob('*.json')) if folder.exists() else []:
        review = json.loads(p.read_text(encoding='utf8'))
        for name, known, accepted, done in [('facets', facets, 'ANALYZED', reviewed),
                                             ('generated_behavior_inputs', generated, 'BEHAVIOR_ANALYZED', accounted)]:
            local = set()
            for r in review.get(name, []):
                sid = r['stable_id']
                if sid not in known or known[sid]['path'] != review['path'] or sid in local:
                    errors.append('Unknown/duplicate receipt: ' + str(p))
                    continue
                local.add(sid)
                if r.get('status') != accepted:
                    continue
                if any(k not in r for k in required) or len(r['reasoning'].strip()) < 50:
                    errors.append('Incomplete reasoning receipt: ' + sid)
                    continue
                done.add(sid)
                receipts.append({'stable_id': sid, 'path': review['path'], 'reviewer': review['reviewer'],
                                 'class': name, 'record': str(p.relative_to(WORK)).replace('\\', '/')})
    original = json.loads((WORK / 'Knowledge/coverage-index.json').read_text(encoding='utf8'))
    result = {'at_utc': now(), 'baseline': BASELINE,
              'original_syntax_callable_declarations': original['indexed_callables'],
              'original_author_callable_receipts': original['unique_analyzed_callable_receipts'],
              'expression_getter_facets': len(facets), 'getter_author_reasoning_receipts': len(reviewed),
              'getter_ids_without_receipts': sorted(set(facets) - reviewed),
              'generated_behavior_contexts': len(generated), 'generated_context_author_reasoning_receipts': len(accounted),
              'generated_context_ids_without_receipts': sorted(set(generated) - accounted),
              'mapped_source_callable_units': original['indexed_callables'] + len(facets),
              'mapped_source_callable_author_receipts': original['unique_analyzed_callable_receipts'] + len(reviewed),
              'generated_input_kinds': dict(Counter(x['kind'] for x in generated.values())),
              'total_runtime_generated_method_count': 'UNKNOWN_NOT_INFERRED_FROM_PARTIAL_SEMANTIC_MODEL',
              'independently_certified_depth_total': 'UNKNOWN; identity/schema checks are not semantic acceptance.',
              'receipts': receipts, 'errors': errors,
              'publication_gate': 'Source getter and generated-context gaps remain material; original11653 denominator alone cannot close audit.'}
    atomic_json(WORK / 'Knowledge/callable-facet-coverage.json', result)
    print(json.dumps({k: v for k, v in result.items() if k not in ('receipts', 'getter_ids_without_receipts', 'generated_context_ids_without_receipts')}))
    if errors:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
