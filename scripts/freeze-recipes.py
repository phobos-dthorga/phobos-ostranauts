#!/usr/bin/env python3
"""Freeze published process-recipe revisions and crops.

Every mods/<Mod>/framework/process-recipes.json has a sibling
frozen-process-recipes.json holding a SHA-256 of each published machine@revision.
The Framework loader refuses a shipped or player entry whose frozen revision hashes
differently, and refuses a pack that drops a frozen revision, so changing a recipe
means adding a revision. The hash is over the entry JSON with its top-level notes
removed, keys sorted at every level and no whitespace; the C# side
(PhobosFramework.Data.RecipeFreeze) computes the same.

A crops pack (mods/<Mod>/framework/crops.json) is frozen the same way into
frozen-crops.json, keyed by crop name: a saved planting stores only that name, so a
published crop can never change or disappear; a change adds a crop beside it.

Usage: freeze-recipes.py [--check] [--format json] [packs...]
--check verifies without writing (exit 1 when a freeze file is stale or a frozen
revision changed); without it, new revisions are added to the freeze files and
existing frozen hashes are never rewritten.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def relative(path):
    resolved = Path(path).resolve()
    return resolved.relative_to(ROOT.resolve()).as_posix() if resolved.is_relative_to(ROOT.resolve()) else str(path)


def canonical(entry):
    trimmed = {k: v for k, v in entry.items() if k != 'notes'}
    return json.dumps(trimmed, sort_keys=True, separators=(',', ':'), ensure_ascii=False)


def entry_hash(entry):
    return hashlib.sha256(canonical(entry).encode('utf-8')).hexdigest()


def published(pack):
    out = {}
    if pack.get('schema') == 'crops':
        for crop_id, entry in pack.get('crops', {}).items():
            out[crop_id] = (crop_id, entry_hash(entry))
        return out
    for recipe_id, entry in pack.get('recipes', {}).items():
        key = f"{entry.get('machine', '')}@{entry.get('revision', '')}"
        out[key] = (recipe_id, entry_hash(entry))
    return out


FROZEN_NAMES = {'process-recipes': 'frozen-process-recipes.json', 'crops': 'frozen-crops.json'}


def freeze_path(pack_path, schema='process-recipes'):
    return pack_path.with_name(FROZEN_NAMES[schema])


def process(pack_path, check):
    pack = json.loads(pack_path.read_text(encoding='utf-8-sig'))
    if pack.get('schema') not in FROZEN_NAMES:
        return None
    current = published(pack)
    path = freeze_path(pack_path, pack['schema'])
    frozen = json.loads(path.read_text(encoding='utf-8-sig')) if path.exists() else {'schemaVersion': 1, 'revisions': {}}
    problems, added = [], []
    for key, digest in frozen.get('revisions', {}).items():
        if key not in current:
            problems.append(f'{key} is frozen but missing from the pack (published revisions can never be removed)')
        elif current[key][1] != digest:
            problems.append(f"{key} ({current[key][0]}) changed after publication; add a new revision instead")
    for key, (recipe_id, digest) in current.items():
        if key not in frozen['revisions']:
            added.append(key)
            if not check:
                frozen['revisions'][key] = digest
    if check and added:
        problems.append('unfrozen revisions: ' + ', '.join(sorted(added)) + ' (run scripts/freeze-recipes.py)')
    if not check and not problems and added:
        frozen['revisions'] = dict(sorted(frozen['revisions'].items()))
        path.write_text(json.dumps(frozen, indent=2) + '\n', encoding='utf-8')
    return {'pack': relative(pack_path), 'frozen': relative(path), 'published': len(current), 'added': added, 'problems': problems}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('packs', nargs='*')
    parser.add_argument('--check', action='store_true')
    parser.add_argument('--format', choices=('text', 'json'), default='text')
    args = parser.parse_args()
    paths = [Path(p).resolve() for p in args.packs] or sorted(
        list((ROOT / 'mods').glob('*/framework/process-recipes.json')) + list((ROOT / 'mods').glob('*/framework/crops.json')))
    results = [r for r in (process(p, args.check) for p in paths) if r]
    errors = [p for r in results for p in r['problems']]
    report = {'schemaVersion': 1, 'status': 'valid' if not errors else 'invalid', 'packs': results}
    if args.format == 'json':
        print(json.dumps(report, indent=2))
    else:
        for r in results:
            state = 'ok' if not r['problems'] else 'ERR'
            print(f"{state} {r['pack']}: {r['published']} published, {len(r['added'])} {'to freeze' if args.check else 'frozen now'}")
            for problem in r['problems']:
                print('    ' + problem)
    return 1 if errors else 0


if __name__ == '__main__':
    sys.exit(main())
