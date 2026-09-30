#!/usr/bin/env python3
"""Golden comparison of two item-reference exports (docs/item-reference-data.json).

A data-pack migration is correct when every prepared definition, job, offer and
source is unchanged. Source hashes and the audited assembly hash are ignored,
because moving a value between a C# file and a JSON file changes them by design;
mod version strings are ignored too unless --strict is given.

Usage: compare-item-export.py BEFORE AFTER [--format json]
Exit code 0 when equal, 1 when different (differences are listed), 2 on error.
"""
import argparse
import json
import sys

IGNORED = ('sourceHashes', 'nativeAssemblySha256')


def strip(data, strict=False):
    data = {k: v for k, v in data.items() if k not in IGNORED}
    if not strict and isinstance(data.get('mods'), list):
        # A migration always bumps versions; the definitions are what must match.
        data['mods'] = [{k: v for k, v in mod.items() if k != 'version'} for mod in data['mods']]
    return data


def walk(before, after, path, out, limit):
    if len(out) >= limit:
        return
    if type(before) is not type(after):
        out.append({'path': path, 'before': before, 'after': after})
    elif isinstance(before, dict):
        for key in sorted(set(before) | set(after)):
            if key not in before:
                out.append({'path': f'{path}/{key}', 'before': None, 'after': after[key]})
            elif key not in after:
                out.append({'path': f'{path}/{key}', 'before': before[key], 'after': None})
            else:
                walk(before[key], after[key], f'{path}/{key}', out, limit)
    elif isinstance(before, list):
        if len(before) != len(after):
            out.append({'path': path, 'before': f'{len(before)} entries', 'after': f'{len(after)} entries'})
        for i, (b, a) in enumerate(zip(before, after)):
            label = b.get('id') if isinstance(b, dict) and 'id' in b else i
            walk(b, a, f'{path}/{label}', out, limit)
    elif before != after:
        out.append({'path': path, 'before': before, 'after': after})


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('before')
    parser.add_argument('after')
    parser.add_argument('--format', choices=('text', 'json'), default='text')
    parser.add_argument('--limit', type=int, default=50)
    parser.add_argument('--strict', action='store_true', help='also compare mod version strings')
    args = parser.parse_args()
    try:
        before = strip(json.load(open(args.before, encoding='utf-8-sig')), args.strict)
        after = strip(json.load(open(args.after, encoding='utf-8-sig')), args.strict)
    except (OSError, ValueError) as error:
        print(f'error: {error}', file=sys.stderr)
        return 2
    differences = []
    walk(before, after, '', differences, args.limit)
    if args.format == 'json':
        print(json.dumps({'status': 'equal' if not differences else 'different', 'differences': differences}, indent=2))
    elif differences:
        print(f'{len(differences)} difference(s) (showing up to {args.limit}):')
        for d in differences:
            print(f"  {d['path']}: {d['before']!r} -> {d['after']!r}")
    else:
        print('Golden export unchanged (source hashes ignored).')
    return 1 if differences else 0


if __name__ == '__main__':
    sys.exit(main())
